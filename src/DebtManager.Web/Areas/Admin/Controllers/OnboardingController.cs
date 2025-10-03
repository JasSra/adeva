using DebtManager.Contracts.External;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Organizations;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class OnboardingController(IAbrValidator abrValidator, IOrganizationRepository orgRepo) : Controller
{
    [HttpGet]
    public IActionResult Index()
    {
        return View(new OnboardingVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ValidateAbn(OnboardingVm vm)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", vm);
        }
        vm.IsValidAbn = await abrValidator.ValidateAsync(vm.Abn);
        return View("Index", vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(OnboardingVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", vm);
        }
        // Require ABN validated before create
        if (vm.IsValidAbn != true)
        {
            ModelState.AddModelError("Abn", "Please validate ABN before continuing.");
            return View("Index", vm);
        }

        // Basic uniqueness checks
        if (!string.IsNullOrWhiteSpace(vm.Subdomain))
        {
            var existing = await orgRepo.GetBySubdomainAsync(vm.Subdomain, ct);
            if (existing != null)
            {
                ModelState.AddModelError("Subdomain", "Subdomain already in use.");
                return View("Index", vm);
            }
        }
        if (!string.IsNullOrWhiteSpace(vm.Abn))
        {
            var existingAbn = await orgRepo.GetByAbnAsync(vm.Abn, ct);
            if (existingAbn != null)
            {
                ModelState.AddModelError("Abn", "An organization with this ABN already exists.");
                return View("Index", vm);
            }
        }

        // Create pending organization with default values for required fields
        // These can be updated by the organization after approval via their settings
        var org = Organization.CreatePending(
            name: vm.Name,
            legalName: vm.Name,  // Use Name as LegalName initially
            abn: vm.Abn,
            defaultCurrency: "AUD",  // Default currency for Australian context
            primaryColorHex: "#1e3a8a",  // Default dark blue
            secondaryColorHex: "#3b82f6",  // Default lighter blue
            supportEmail: "support@debtmanager.local",  // Placeholder
            supportPhone: "1300000000",  // Placeholder
            timezone: "Australia/Sydney",  // Default timezone
            subdomain: vm.Subdomain,
            tradingName: null  // Optional trading name
        );
        
        await orgRepo.AddAsync(org, ct);
        await orgRepo.SaveChangesAsync(ct);
        TempData["OnboardedOrgId"] = org.Id.ToString();
        TempData["Message"] = "Organization submitted for approval.";
        return RedirectToAction("Index");
    }
}

public class OnboardingVm
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [RegularExpression("^[a-z0-9-]+$", ErrorMessage = "Lowercase letters, numbers and hyphen only.")]
    [StringLength(63)]
    public string? Subdomain { get; set; }

    [Required]
    [RegularExpression("^\\d{11}$", ErrorMessage = "ABN must be 11 digits.")]
    public string Abn { get; set; } = string.Empty;
    public bool? IsValidAbn { get; set; }
}
