using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Root.Controllers;

[Area("Root")]
[Authorize(Roles = "SuperAdmin")]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewBag.Title = "Super Admin";
        return View();
    }
}

[Area("Root")]
[Authorize(Roles = "SuperAdmin")]
public class AccountsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AppDbContext _db;

    public AccountsController(UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager, AppDbContext db)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q = null)
    {
        var usersQuery = _db.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            usersQuery = usersQuery.Where(u => (u.Email ?? "").Contains(q) || (u.UserName ?? "").Contains(q));
        }

        var users = await usersQuery
            .OrderBy(u => u.Email)
            .Select(u => new AccountVm
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                UserName = u.UserName ?? string.Empty
            })
            .ToListAsync();

        var model = new AccountsIndexVm
        {
            Query = q,
            Users = users
        };
        ViewBag.Title = "User Accounts";
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GrantAdmin(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            TempData["Error"] = "User not found";
            return RedirectToAction(nameof(Index));
        }
        if (!await _roleManager.RoleExistsAsync("Admin"))
        {
            await _roleManager.CreateAsync(new ApplicationRole("Admin"));
        }
        if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var res = await _userManager.AddToRoleAsync(user, "Admin");
            if (!res.Succeeded)
            {
                TempData["Error"] = string.Join("; ", res.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Message"] = $"Granted Admin to {user.Email ?? user.UserName}";
            }
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeAdmin(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());
        if (user == null)
        {
            TempData["Error"] = "User not found";
            return RedirectToAction(nameof(Index));
        }
        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var res = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (!res.Succeeded)
            {
                TempData["Error"] = string.Join("; ", res.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["Message"] = $"Revoked Admin from {user.Email ?? user.UserName}";
            }
        }
        return RedirectToAction(nameof(Index));
    }
}

[Area("Root")]
[Authorize(Roles = "SuperAdmin")]
public class SystemController : Controller
{
    private readonly AppDbContext _db;

    public SystemController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Activities()
    {
        ViewBag.Title = "System Activities";
        return View();
    }
}

public record AccountVm
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string UserName { get; init; } = string.Empty;
}

public record AccountsIndexVm
{
    public string? Query { get; init; }
    public List<AccountVm> Users { get; init; } = new();
}
