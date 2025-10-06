using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DebtManager.Domain.Debtors;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
public class OnboardingController : Controller
{
    private readonly AppDbContext _db;

    public OnboardingController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new DebtorOnboardingVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DebtorOnboardingVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", vm);
        }

        var externalId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = await _db.Users.FirstAsync(u => u.ExternalAuthId == externalId, ct);
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (profile == null)
        {
            profile = new UserProfile { Id = Guid.NewGuid(), UserId = user.Id };
            _db.UserProfiles.Add(profile);
        }

        // Update profile name
        profile.FirstName = vm.FirstName;
        profile.LastName = vm.LastName;

        // Create a Debtor entity for this user
        // Note: Debtor requires an OrganizationId. For user-initiated debtor creation (self-service),
        // we need a "default" or "platform" organization, OR wait for a debt to be assigned.
        // For now, we'll create a debtor without org (org will be set when debt is created by admin/client).
        // However, Debtor domain model requires OrganizationId. We need to handle this properly.

        // Strategy: Create a platform-level "Self-Service" organization for user-initiated debtors
        // OR: Only create Debtor when first debt is assigned (handled by admin/client workflows)
        // For this fix, let's assume there's a "platform" org or we create a stub debtor record.

        // TEMPORARY FIX: Create a Debtor linked to the first available organization (or create a platform org)
        // Ideally, debtor should only exist when a debt is assigned by a creditor organization.
        // But to unblock onboarding, we'll create a minimal debtor profile.

        var platformOrg = await _db.Organizations.FirstOrDefaultAsync(o => o.Subdomain == "platform" || o.Name == "Platform", ct);
        if (platformOrg == null)
        {
            // Create a platform organization for self-service debtors
            platformOrg = new DebtManager.Domain.Organizations.Organization(
                name: "Platform",
                legalName: "Adeva Plus Platform",
                abn: "00000000000",
                defaultCurrency: "AUD",
                primaryColorHex: "#0ea5e9",
                secondaryColorHex: "#3b82f6",
                supportEmail: "support@adevaplus.local",
                supportPhone: "1300000000",
                timezone: "Australia/Sydney",
                subdomain: "platform",
                tradingName: "Adeva Plus"
            );
            platformOrg.Approve(DateTime.UtcNow);
            platformOrg.MarkOnboarded(DateTime.UtcNow);
            await _db.Organizations.AddAsync(platformOrg, ct);
            await _db.SaveChangesAsync(ct);
        }

        // Create Debtor for this user
        var debtor = new Debtor(
            organizationId: platformOrg.Id,
            referenceId: $"USR-{user.Id.ToString("N")[..8].ToUpperInvariant()}",
            email: user.Email ?? $"user-{user.Id}@platform.local",
            phone: user.PhoneNumber ?? "+61000000000",
            firstName: vm.FirstName,
            lastName: vm.LastName
        );
        debtor.UpdatePersonalDetails(vm.FirstName, vm.LastName, vm.FirstName, null, null);
        debtor.SetStatus(DebtorStatus.New);
        debtor.EnablePortalAccess();

        await _db.Debtors.AddAsync(debtor, ct);
        await _db.SaveChangesAsync(ct);

        // Link Debtor to UserProfile
        profile.DebtorId = debtor.Id;
        await _db.SaveChangesAsync(ct);

        TempData["Message"] = "Profile created successfully.";
        return Redirect("/User");
    }
}

public class DebtorOnboardingVm
{
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;
}
