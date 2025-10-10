using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Contracts.Notifications;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using DebtManager.Contracts.Configuration;
using Microsoft.Extensions.Configuration;

namespace DebtManager.Web.Controllers;

[Authorize]
public class SecurityController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISmsSender _smsSender;
    private readonly AppDbContext _db;
    private readonly IAppConfigService _cfg;
    private readonly IConfiguration _configuration;

    public SecurityController(
        UserManager<ApplicationUser> userManager, 
        ISmsSender smsSender, 
        AppDbContext db, 
        IAppConfigService cfg,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _smsSender = smsSender;
        _db = db;
        _cfg = cfg;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Setup()
    {
        var user = await GetCurrentUserAsync();

        // Determine scope/mode
        var isAdmin = User.IsInRole("Admin");
        var showTotp = isAdmin;            // Admins: TOTP only
        var showSms = !isAdmin;            // Client/User: SMS OTP only

        string key = string.Empty;
        string otpauth = string.Empty;
        string qrDataUrl = string.Empty;
        if (showTotp)
        {
            key = await EnsureAuthenticatorKey(user);
            otpauth = BuildOtpAuthUri("AdevaPlus", user.Email ?? user.UserName ?? user.Id.ToString(), key);
            qrDataUrl = GenerateQrPngDataUrl(otpauth);
        }

        var (heading, subheading, tips) = BuildCopyForScope(isAdmin);
        
        // Dev mode: Check if bypass is enabled from appsettings.json
        var bypass = _configuration.GetValue<bool>("Security:BypassOtpVerification");

        // Persisted phone from last SendSms
        var lastSmsPhone = TempData.Peek("SmsPhone") as string;

        // Pre-fill phone: prefer last sent phone > confirmed user phone > dev default
        var prefillPhone = !string.IsNullOrWhiteSpace(lastSmsPhone)
            ? lastSmsPhone
            : (user.PhoneNumber ?? string.Empty);

        if (string.IsNullOrWhiteSpace(prefillPhone) && bypass)
        {
            prefillPhone = "+61400000000"; // Dev default
        }

        var vm = new TotpSetupVm
        {
            AuthenticatorKey = showTotp ? FormatKey(key) : string.Empty,
            OtpauthUri = showTotp ? otpauth : string.Empty,
            PhoneNumber = prefillPhone,
            TotpEnabled = user.TwoFactorEnabled,
            QrCodeDataUrl = showTotp ? qrDataUrl : string.Empty,
            ShowTotp = showTotp,
            ShowSms = showSms,
            Heading = heading,
            SubHeading = subheading,
            Tips = tips,
            DevMode = bypass,
            DevSmsCode = bypass ? "000000" : string.Empty
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendSms([FromForm] string phoneNumber)
    {
        var user = await GetCurrentUserAsync();
        var isAdmin = User.IsInRole("Admin");
        if (isAdmin)
        {
            TempData["Error"] = "SMS verification is not required for administrator setup.";
            return RedirectToAction(nameof(Setup));
        }
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            TempData["Error"] = "Phone number is required.";
            return RedirectToAction(nameof(Setup));
        }

        // Normalize and persist phone for the next step so token matches exactly
        var normalizedPhone = NormalizePhone(phoneNumber);
        TempData["SmsPhone"] = normalizedPhone;

        // Dev mode: Check if bypass is enabled from appsettings.json
        var bypass = _configuration.GetValue<bool>("Security:BypassOtpVerification");
        if (!bypass)
        {
            var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, normalizedPhone);
            await _smsSender.SendSmsAsync(normalizedPhone, $"Adeva Plus verification code: {token}");
        }
        else
        {
            await _smsSender.SendSmsAsync(normalizedPhone, "Adeva Plus verification code: 000000");
        }

        TempData["Message"] = "Verification code sent via SMS.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete([FromForm] TotpCompleteVm vm)
    {
        var user = await GetCurrentUserAsync();
        var isAdmin = User.IsInRole("Admin");
        var isClient = User.IsInRole("Client");
        var isUser = User.IsInRole("User");
        var showTotp = isAdmin;    // Admins: TOTP only
        var showSms = !isAdmin;    // Client/User: SMS OTP only

        // Dev mode: Check if bypass is enabled from appsettings.json
        var bypass = _configuration.GetValue<bool>("Security:BypassOtpVerification");

        bool phoneVerified = false;
        bool totpVerified = false;

        // Conditional validation for SMS
        if (showSms)
        {
            // Prefer the last phone number we sent the SMS to
            var persistedPhone = TempData.Peek("SmsPhone") as string;
            var postedPhone = vm.PhoneNumber;
            var phoneToVerify = NormalizePhone(!string.IsNullOrWhiteSpace(persistedPhone) ? persistedPhone : postedPhone);

            if (string.IsNullOrWhiteSpace(phoneToVerify))
            {
                ModelState.AddModelError("PhoneNumber", "Phone number is required.");
            }
            else if (string.IsNullOrWhiteSpace(vm.SmsCode))
            {
                ModelState.AddModelError("SmsCode", "SMS verification code is required.");
            }
            else
            {
                if (bypass)
                {
                    // Bypass mode - accept any code (e.g., 000000) and auto-verify
                    user.PhoneNumber = phoneToVerify;
                    user.PhoneNumberConfirmed = true;
                    phoneVerified = true;
                    vm.PhoneNumber = phoneToVerify;
                }
                else
                {
                    // Atomically set + confirm phone using Identity helper
                    var result = await _userManager.ChangePhoneNumberAsync(user, phoneToVerify, vm.SmsCode!);
                    if (!result.Succeeded)
                    {
                        foreach (var err in result.Errors)
                        {
                            ModelState.AddModelError("SmsCode", err.Description);
                        }
                    }
                    else
                    {
                        phoneVerified = true;
                        vm.PhoneNumber = phoneToVerify;
                    }
                }
            }
        }

        // Conditional validation for TOTP
        if (showTotp)
        {
            if (string.IsNullOrWhiteSpace(vm.TotpCode))
            {
                ModelState.AddModelError("TotpCode", "Authenticator app code is required.");
            }
            else
            {
                if (bypass)
                {
                    // Bypass mode - accept any code and auto-verify
                    totpVerified = true;
                }
                else
                {
                    var verified = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, vm.TotpCode!);
                    if (!verified)
                    {
                        ModelState.AddModelError("TotpCode", "Invalid authenticator code.");
                    }
                    else
                    {
                        totpVerified = true;
                    }
                }
            }
        }

        // Check for any validation errors
        if (!ModelState.IsValid)
        {
            // Store validation errors in TempData for display
            var validationErrors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            
            TempData["ValidationErrors"] = JsonSerializer.Serialize(validationErrors);
            
            return RedirectToAction(nameof(Setup));
        }

        // Apply changes if verified (for bypass path, Identity helper already saved in normal path)
        if (phoneVerified && bypass)
        {
            await _userManager.UpdateAsync(user);
            TempData["SmsPhone"] = vm.PhoneNumber;
        }

        if (totpVerified)
        {
            await _userManager.SetTwoFactorEnabledAsync(user, true);
        }

        // Persist any remaining changes
        await _userManager.UpdateAsync(user);
        
        TempData["Message"] = isAdmin
            ? "Authenticator app set up successfully. TOTP is now required for admin sign-in."
            : "Phone verified successfully. We'll use SMS one-time codes to keep your account secure.";

        // Route to appropriate onboarding after security setup
        if (isAdmin) 
            return Redirect("/Admin");
        
        if (isClient)
        {
            // Client scope ? Organization onboarding
            var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile?.OrganizationId == null)
            {
                TempData["Message"] = "Phone verified! Let's set up your organization.";
                return Redirect("/Client/Onboarding");
            }
            TempData["Message"] = "Phone verified! Welcome back.";
            return Redirect("/Client");
        }
        
        if (isUser)
        {
            // User scope ? Debtor onboarding
            var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile?.DebtorId == null)
            {
                TempData["Message"] = "Phone verified! Let's complete your profile.";
                return Redirect("/User/Onboarding");
            }
            TempData["Message"] = "Phone verified! Welcome back.";
            return Redirect("/User");
        }

        // Fallback - if no role matched
        TempData["Error"] = $"Unable to determine user role. Admin: {isAdmin}, Client: {isClient}, User: {isUser}";
        return Redirect("/");
    }

    private static string NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
        var p = phone.Trim();
        // Remove spaces, dashes, and parentheses
        p = new string(p.Where(c => char.IsDigit(c) || c == '+').ToArray());
        // Ensure it starts with '+' for E.164 if it looks like AU number
        if (!p.StartsWith("+") && p.StartsWith("0"))
        {
            // naive AU normalization 04xxxxxxxx -> +614xxxxxxxx
            if (p.StartsWith("04") && p.Length == 10)
            {
                p = "+61" + p.Substring(1);
            }
        }
        return p;
    }

    private async Task<ApplicationUser> GetCurrentUserAsync()
    {
        // Prefer lookup by ExternalAuthId from OIDC claims
        var externalId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(externalId))
        {
            var byExternal = await _db.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == externalId);
            if (byExternal != null) return byExternal;
        }

        var name = User.Identity?.Name;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var byName = await _userManager.FindByNameAsync(name);
            if (byName != null) return byName;
        }

        var byCookie = await _userManager.GetUserAsync(User);
        if (byCookie != null) return byCookie;

        throw new InvalidOperationException("User not found");
    }

    private async Task<string> EnsureAuthenticatorKey(ApplicationUser user)
    {
        var key = await _userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrEmpty(key))
        {
            await _userManager.ResetAuthenticatorKeyAsync(user);
            key = await _userManager.GetAuthenticatorKeyAsync(user);
        }
        return key!;
    }

    private static string BuildOtpAuthUri(string issuer, string account, string secret)
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(account)}?secret={secret}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
    }

    private static string FormatKey(string key)
    {
        var groups = new List<string>();
        for (int i = 0; i < key.Length; i += 4)
        {
            groups.Add(key.Substring(i, Math.Min(4, key.Length - i)));
        }
        return string.Join(" ", groups);
    }

    private static string GenerateQrPngDataUrl(string text)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(20);
        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    private static (string heading, string subheading, List<string> tips) BuildCopyForScope(bool isAdmin)
    {
        if (isAdmin)
        {
            return (
                heading: "Secure your admin account with an Authenticator App",
                subheading: "Admins must use Time�based One�Time Passwords (TOTP). Scan the QR code and enter the 6�digit code from your app to finish.",
                tips: new List<string>
                {
                    "Use any authenticator app (Microsoft, Google, Authy).",
                    "Back up your recovery codes in your password manager.",
                    "TOTP will be required on every admin sign�in."
                }
            );
        }
        return (
            heading: "Verify your phone number",
            subheading: "We send a one�time SMS code to confirm it's you. This keeps your account secure without needing an authenticator app.",
            tips: new List<string>
            {
                "Enter a mobile number where you can receive SMS messages.",
                "If you don't receive a code, check the number and try again.",
                "You can update your phone later from your profile."
            }
        );
    }
}

public class TotpSetupVm
{
    public string AuthenticatorKey { get; set; } = string.Empty;
    public string OtpauthUri { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public bool TotpEnabled { get; set; }
    public string QrCodeDataUrl { get; set; } = string.Empty;

    // UI mode
    public bool ShowTotp { get; set; }
    public bool ShowSms { get; set; }

    // Copy
    public string Heading { get; set; } = string.Empty;
    public string SubHeading { get; set; } = string.Empty;
    public List<string> Tips { get; set; } = new();
    
    // Dev mode
    public bool DevMode { get; set; }
    public string DevSmsCode { get; set; } = string.Empty;
}

public class TotpCompleteVm
{
    // All optional; validated conditionally based on role
    public string? PhoneNumber { get; set; }
    public string? SmsCode { get; set; }
    public string? TotpCode { get; set; }
}
