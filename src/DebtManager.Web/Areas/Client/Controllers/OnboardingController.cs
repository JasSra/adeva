using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DebtManager.Contracts.External;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Organizations;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Policy = "RequireClientScope")]
public class OnboardingController : Controller
{
    private readonly IAbrValidator _abr;
    private readonly IBusinessLookupService _businessLookup;
    private readonly IOrganizationRepository _orgRepo;
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public OnboardingController(IAbrValidator abr, IBusinessLookupService businessLookup, IOrganizationRepository orgRepo, AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _abr = abr;
        _businessLookup = businessLookup;
        _orgRepo = orgRepo;
        _db = db;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ClientOnboardingVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateAbn(ClientOnboardingVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", vm);
        }
        vm.IsValidAbn = await _abr.ValidateAsync(vm.Abn);
        if (!vm.IsValidAbn.Value)
        {
            ModelState.AddModelError("Abn", "ABN did not validate.");
        }

        if (!string.IsNullOrWhiteSpace(vm.Acn))
        {
            vm.IsValidAcn = await _businessLookup.ValidateAcnAsync(vm.Acn);
            if (vm.IsValidAcn != true)
            {
                ModelState.AddModelError("Acn", "ACN did not validate.");
            }
        }
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientOnboardingVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", vm);
        }
        if (vm.IsValidAbn != true)
        {
            ModelState.AddModelError("Abn", "Please validate ABN before continuing.");
            return View("Index", vm);
        }

        if (!string.IsNullOrWhiteSpace(vm.Subdomain))
        {
            var exists = await _orgRepo.GetBySubdomainAsync(vm.Subdomain, ct);
            if (exists != null)
            {
                ModelState.AddModelError("Subdomain", "Subdomain already in use.");
                return View("Index", vm);
            }
        }
        if (!string.IsNullOrWhiteSpace(vm.Abn))
        {
            var existingAbn = await _orgRepo.GetByAbnAsync(vm.Abn, ct);
            if (existingAbn != null)
            {
                ModelState.AddModelError("Abn", "An organization with this ABN already exists.");
                return View("Index", vm);
            }
        }

        var org = Organization.CreatePending(
            name: vm.Name,
            legalName: vm.Name,
            abn: vm.Abn,
            defaultCurrency: "AUD",
            primaryColorHex: "#1e3a8a",
            secondaryColorHex: "#3b82f6",
            supportEmail: vm.SupportEmail ?? "support@debtmanager.local",
            supportPhone: vm.SupportPhone ?? "1300000000",
            timezone: vm.Timezone ?? "Australia/Sydney",
            subdomain: vm.Subdomain,
            tradingName: vm.TradingName
        );
        await _orgRepo.AddAsync(org, ct);
        await _orgRepo.SaveChangesAsync(ct);

        // Attach to current user's profile
        var externalId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FirstAsync(u => u.ExternalAuthId == externalId, ct);
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (profile == null)
        {
            profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id };
            _db.UserProfiles.Add(profile);
        }
        profile.OrganizationId = org.Id;
        await _db.SaveChangesAsync(ct);

        TempData["Message"] = "Organization created and linked to your profile. Pending approval.";
        return Redirect("/Client");
    }
}

public class ClientOnboardingVm
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? TradingName { get; set; }

    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Lowercase letters, numbers and hyphen only.")]
    [StringLength(63)]
    public string? Subdomain { get; set; }

    [Required]
    [RegularExpression("^\\d{11}$", ErrorMessage = "ABN must be 11 digits.")]
    public string Abn { get; set; } = string.Empty;

    [RegularExpression("^\\d{9}$", ErrorMessage = "ACN must be 9 digits.")]
    public string? Acn { get; set; }

    [EmailAddress]
    public string? SupportEmail { get; set; }

    public string? SupportPhone { get; set; }

    public string? Timezone { get; set; }

    public bool? IsValidAbn { get; set; }
    public bool? IsValidAcn { get; set; }
}
