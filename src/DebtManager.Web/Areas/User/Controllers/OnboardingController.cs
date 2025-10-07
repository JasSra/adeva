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
        if (TempData.TryGetValue("OnboardingInfo", out var msg) && msg is string s && !string.IsNullOrWhiteSpace(s))
            ViewBag.OnboardingInfo = s;

        // Pre-fill from claims for convenience
        var model = new DebtorOnboardingVm
        {
            FirstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
            LastName = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty
        };
        return View(model);
    }

    // Safety net: handle posts mistakenly hitting Index to prevent 405
    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Index(DebtorOnboardingVm vm, CancellationToken ct)
        => Create(vm, ct);

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

        profile.FirstName = vm.FirstName;
        profile.LastName = vm.LastName;

        // Ensure a platform org exists
        var platformOrg = await _db.Organizations.FirstOrDefaultAsync(o => o.Subdomain == "platform" || o.Name == "Platform", ct);
        if (platformOrg == null)
        {
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

        // Create Debtor for this user if not exists
        var existingDebtor = await _db.Debtors.FirstOrDefaultAsync(d => d.Email == (user.Email ?? "") && d.OrganizationId == platformOrg.Id, ct);
        if (existingDebtor == null)
        {
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
            profile.DebtorId = debtor.Id;
        }
        else
        {
            existingDebtor.UpdatePersonalDetails(vm.FirstName, vm.LastName, vm.FirstName, null, null);
            await _db.SaveChangesAsync(ct);
            profile.DebtorId = existingDebtor.Id;
        }

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
