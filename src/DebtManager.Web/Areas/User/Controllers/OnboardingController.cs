using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
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
        profile.FirstName = vm.FirstName;
        profile.LastName = vm.LastName;
        await _db.SaveChangesAsync(ct);

        TempData["Message"] = "Profile updated.";
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
