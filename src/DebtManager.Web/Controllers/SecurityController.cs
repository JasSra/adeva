using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DebtManager.Contracts.Notifications;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace DebtManager.Web.Controllers;

[Authorize]
public class SecurityController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISmsSender _smsSender;
    private readonly AppDbContext _db;

    public SecurityController(UserManager<ApplicationUser> userManager, ISmsSender smsSender, AppDbContext db)
    {
        _userManager = userManager;
        _smsSender = smsSender;
        _db = db;
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

        var vm = new TotpSetupVm
        {
            AuthenticatorKey = showTotp ? FormatKey(key) : string.Empty,
            OtpauthUri = showTotp ? otpauth : string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            TotpEnabled = user.TwoFactorEnabled,
            QrCodeDataUrl = showTotp ? qrDataUrl : string.Empty,
            ShowTotp = showTotp,
            ShowSms = showSms,
            Heading = heading,
            SubHeading = subheading,
            Tips = tips
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
        var token = await _userManager.GenerateChangePhoneNumberTokenAsync(user, phoneNumber);
        await _smsSender.SendSmsAsync(phoneNumber, $"Adeva Plus verification code: {token}");
        TempData["Message"] = "Verification code sent via SMS.";
        return RedirectToAction(nameof(Setup));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Complete([FromForm] TotpCompleteVm vm)
    {
        var user = await GetCurrentUserAsync();
        var isAdmin = User.IsInRole("Admin");
        var showTotp = isAdmin;    // Admins: TOTP only
        var showSms = !isAdmin;    // Client/User: SMS OTP only

        // Conditional validation
        if (showSms)
        {
            if (string.IsNullOrWhiteSpace(vm.PhoneNumber))
                ModelState.AddModelError("PhoneNumber", "Phone number is required.");

            if (string.IsNullOrWhiteSpace(vm.SmsCode))
                ModelState.AddModelError("SmsCode", "SMS verification code is required.");

            if (ModelState.IsValid)
            {
                var validSms = await _userManager.VerifyChangePhoneNumberTokenAsync(user, vm.SmsCode!, vm.PhoneNumber!);
                if (!validSms)
                {
                    ModelState.AddModelError("SmsCode", "Invalid SMS verification code.");
                }
                else
                {
                    user.PhoneNumber = vm.PhoneNumber;
                    user.PhoneNumberConfirmed = true;
                }
            }
        }

        if (showTotp)
        {
            if (string.IsNullOrWhiteSpace(vm.TotpCode))
                ModelState.AddModelError("TotpCode", "Authenticator app code is required.");

            if (ModelState.IsValid)
            {
                var verified = await _userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, vm.TotpCode!);
                if (!verified)
                {
                    ModelState.AddModelError("TotpCode", "Invalid authenticator code.");
                }
                else
                {
                    await _userManager.SetTwoFactorEnabledAsync(user, true);
                }
            }
        }

        if (!ModelState.IsValid)
        {
            TempData["Error"] = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return RedirectToAction(nameof(Setup));
        }

        await _userManager.UpdateAsync(user);
        TempData["Message"] = isAdmin
            ? "Authenticator app set up successfully. TOTP is now required for admin sign-in."
            : "Phone verified successfully. We'll use SMS one-time codes to keep your account secure.";

        // Redirect by role
        if (User.IsInRole("Admin")) return Redirect("/Admin");
        if (User.IsInRole("Client")) return Redirect("/Client");
        if (User.IsInRole("User")) return Redirect("/User");
        return Redirect("/");
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
                subheading: "Admins must use Time?based One?Time Passwords (TOTP). Scan the QR code and enter the 6?digit code from your app to finish.",
                tips: new List<string>
                {
                    "Use any authenticator app (Microsoft, Google, Authy).",
                    "Back up your recovery codes in your password manager.",
                    "TOTP will be required on every admin sign?in."
                }
            );
        }
        return (
            heading: "Verify your phone number",
            subheading: "We send a one?time SMS code to confirm it's you. This keeps your account secure without needing an authenticator app.",
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
}

public class TotpCompleteVm
{
    // All optional; validated conditionally based on role
    public string? PhoneNumber { get; set; }
    public string? SmsCode { get; set; }
    public string? TotpCode { get; set; }
}
