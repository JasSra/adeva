using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DebtManager.Contracts.External;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Organizations;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Web.Services;
using System.Text.Json;

namespace DebtManager.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Policy = "RequireClientScope")]
public class OnboardingController : Controller
{
    private readonly IBusinessLookupService _businessLookup;
    private readonly IOrganizationRepository _orgRepo;
    private readonly AppDbContext _db;
    private readonly IOnboardingNotificationService _notifier;

    private const string TempDataKey = "ClientOnboardingVm";

    public OnboardingController(
        IBusinessLookupService businessLookup,
        IOrganizationRepository orgRepo,
        AppDbContext db,
        IOnboardingNotificationService notifier)
    {
        _businessLookup = businessLookup;
        _orgRepo = orgRepo;
        _db = db;
        _notifier = notifier;
    }

    /// <summary>
    /// Step 1: ABN/ACN Validation
    /// </summary>
    [HttpGet]
    public IActionResult Index()
    {
        if (TempData.TryGetValue("OnboardingInfo", out var msg) && msg is string s && !string.IsNullOrWhiteSpace(s))
            ViewBag.OnboardingInfo = s;
        return View(new ClientOnboardingVm());
    }

    // Safety net: prevent 405 when a form posts to Index by mistake
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Index(ClientOnboardingVm vm, CancellationToken ct) => ValidateBusiness(vm, ct);

    /// <summary>
    /// Step 2: Validate ABN/ACN and extract business info
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateBusiness(ClientOnboardingVm vm, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(vm.Abn))
        {
            ModelState.AddModelError("Abn", "ABN is required.");
            return View("Index", vm);
        }

        // Lookup ABN and details via business service
        var lookup = await _businessLookup.SearchByAbnAsync(vm.Abn);
        vm.IsValidAbn = lookup != null && string.IsNullOrWhiteSpace(lookup.Exception) && lookup.Abns.Any();
        if (vm.IsValidAbn != true)
        {
            ModelState.AddModelError("Abn", "Invalid or inactive ABN. Please check and try again.");
            return View("Index", vm);
        }

        // Extract fields from lookup result
        vm.ExtractedAbn = lookup.Abns.FirstOrDefault() ?? vm.Abn;
        var mainName = lookup.MainNames.FirstOrDefault()?.OrganisationName;
        var anyName = mainName ?? lookup.EntityNames.FirstOrDefault();
        var trading = lookup.MainTradingNames.FirstOrDefault()?.OrganisationName ?? lookup.TradingNames.FirstOrDefault();
        vm.ExtractedBusinessName = anyName;
        vm.ExtractedLegalName = anyName; // ABR payload doesn't clearly separate legal vs business; use main name
        vm.ExtractedTradingName = trading;
        vm.ExtractedAcn = lookup.Acn;

        // Validate ACN if provided
        if (!string.IsNullOrWhiteSpace(vm.Acn))
        {
            vm.IsValidAcn = await _businessLookup.IsIdentifierActiveAsync(vm.Acn);
            if (vm.IsValidAcn != true)
            {
                ModelState.AddModelError("Acn", "Invalid ACN. Please check and try again.");
                return View("Index", vm);
            }
        }

        // Pre-fill user details from claims
        var firstName = User.FindFirstValue(ClaimTypes.GivenName);
        var lastName = User.FindFirstValue(ClaimTypes.Surname);
        var email = User.FindFirstValue(ClaimTypes.Email);

        vm.ContactFirstName = firstName ?? string.Empty;
        vm.ContactLastName = lastName ?? string.Empty;
        vm.ContactEmail = email ?? string.Empty;

        // PRG: store VM and redirect
        TempData[TempDataKey] = JsonSerializer.Serialize(vm);
        return RedirectToAction(nameof(ConfirmDetails));
    }

    /// <summary>
    /// Step 3: Review extracted business info and user details
    /// </summary>
    [HttpGet]
    public IActionResult ConfirmDetails()
    {
        if (TempData.TryGetValue("OnboardingInfo", out var msg) && msg is string s && !string.IsNullOrWhiteSpace(s))
            ViewBag.OnboardingInfo = s;
        if (TempData.TryGetValue("ClientOnboardingVm", out var jsonObj) && jsonObj is string json && !string.IsNullOrWhiteSpace(json))
        {
            var vm = System.Text.Json.JsonSerializer.Deserialize<ClientOnboardingVm>(json) ?? new ClientOnboardingVm();
            return View(vm);
        }
        // Direct access: back to step 1
        return RedirectToAction(nameof(Index));
    }

    // Safety net: prevent 405 if a form posts to ConfirmDetails instead of Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ConfirmDetails(ClientOnboardingVm vm)
    {
        // If it got here via POST, go back to step 2 view to let the user submit properly
        return View(vm);
    }

    /// <summary>
    /// Step 4: Create organization and send notifications
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientOnboardingVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View("ConfirmDetails", vm);
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(vm.ContactFirstName) || string.IsNullOrWhiteSpace(vm.ContactLastName))
        {
            ModelState.AddModelError(string.Empty, "Contact first name and last name are required.");
            return View("ConfirmDetails", vm);
        }

        // Check for duplicate ABN
        if (!string.IsNullOrWhiteSpace(vm.Abn))
        {
            var existingAbn = await _orgRepo.GetByAbnAsync(vm.Abn, ct);
            if (existingAbn != null)
            {
                ModelState.AddModelError("Abn", "An organization with this ABN already exists.");
                return View("Index", vm);
            }
        }

        // Check for duplicate subdomain
        if (!string.IsNullOrWhiteSpace(vm.Subdomain))
        {
            var existingSubdomain = await _orgRepo.GetBySubdomainAsync(vm.Subdomain, ct);
            if (existingSubdomain != null)
            {
                ModelState.AddModelError("Subdomain", "This subdomain is already in use.");
                return View("ConfirmDetails", vm);
            }
        }

        // Create organization (pending approval)
        var org = Organization.CreatePending(
            name: vm.ExtractedBusinessName ?? vm.Name,
            legalName: vm.ExtractedLegalName ?? vm.Name,
            abn: vm.Abn!,
            defaultCurrency: "AUD",
            primaryColorHex: "#1e3a8a",
            secondaryColorHex: "#3b82f6",
            supportEmail: vm.SupportEmail ?? vm.ContactEmail ?? "support@debtmanager.local",
            supportPhone: vm.SupportPhone ?? "1300000000",
            timezone: vm.Timezone ?? "Australia/Sydney",
            subdomain: vm.Subdomain,
            tradingName: vm.ExtractedTradingName ?? vm.TradingName
        );

        await _orgRepo.AddAsync(org, ct);
        await _orgRepo.SaveChangesAsync(ct);

        // Attach organization to current user's profile
        var externalId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FirstAsync(u => u.ExternalAuthId == externalId, ct);
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        
        if (profile == null)
        {
            profile = new UserProfile 
            { 
                Id = Guid.NewGuid(), 
                UserId = user.Id,
                FirstName = vm.ContactFirstName,
                LastName = vm.ContactLastName
            };
            _db.UserProfiles.Add(profile);
        }
        else
        {
            profile.FirstName = vm.ContactFirstName;
            profile.LastName = vm.ContactLastName;
        }

        profile.OrganizationId = org.Id;
        await _db.SaveChangesAsync(ct);

        // Notifications centralized in service
        await _notifier.QueueClientWelcomeAsync(org.Id, vm.ContactFirstName, vm.ContactLastName, vm.ContactEmail, ct);
        await _notifier.QueueAdminNewClientAlertAsync(org.Id, vm.ContactFirstName, vm.ContactLastName, vm.ContactEmail, ct);

        // Redirect to "What's Next" page
        return RedirectToAction(nameof(WhatsNext), new { orgId = org.Id });
    }

    /// <summary>
    /// Step 5: What's Next - Pending approval message
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> WhatsNext(Guid orgId, CancellationToken ct)
    {
        if (TempData.TryGetValue("OnboardingInfo", out var msg) && msg is string s && !string.IsNullOrWhiteSpace(s))
            ViewBag.OnboardingInfo = s;
        var org = await _orgRepo.GetAsync(orgId, ct);
        if (org == null) return NotFound();
        var vm = new WhatsNextVm
        {
            OrganizationName = org.Name,
            TradingName = org.TradingName,
            SupportEmail = org.SupportEmail,
            SupportPhone = org.SupportPhone,
            IsApproved = org.IsApproved
        };
        return View(vm);
    }
}

public class ClientOnboardingVm
{
    // Step 1: ABN/ACN Input
    [Required(ErrorMessage = "ABN is required")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "ABN must be 11 digits")]
    public string Abn { get; set; } = string.Empty;

    [RegularExpression(@"^\d{9}$", ErrorMessage = "ACN must be 9 digits")]
    public string? Acn { get; set; }

    // Step 1: Validation Results
    public bool? IsValidAbn { get; set; }
    public bool? IsValidAcn { get; set; }

    // Step 2: Extracted Business Info
    public string? ExtractedBusinessName { get; set; }
    public string? ExtractedLegalName { get; set; }
    public string? ExtractedTradingName { get; set; }
    public string? ExtractedAbn { get; set; }
    public string? ExtractedAcn { get; set; }

    // Step 2: User Contact Details
    [Required(ErrorMessage = "First name is required")]
    [StringLength(100)]
    public string ContactFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100)]
    public string ContactLastName { get; set; } = string.Empty;

    [EmailAddress]
    public string? ContactEmail { get; set; }

    // Step 3: Additional Organization Details
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(200)]
    public string? TradingName { get; set; }

    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Lowercase letters, numbers and hyphen only")]
    [StringLength(63)]
    public string? Subdomain { get; set; }

    [EmailAddress]
    public string? SupportEmail { get; set; }

    [Phone]
    public string? SupportPhone { get; set; }

    public string? Timezone { get; set; }
}

public class WhatsNextVm
{
    public string OrganizationName { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string SupportEmail { get; set; } = string.Empty;
    public string SupportPhone { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
}
