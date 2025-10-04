using System.Security.Claims;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Controllers;

[AllowAnonymous]
public class DevController : Controller
{
    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public DevController(
        IHostEnvironment env,
        IConfiguration config,
        AppDbContext db,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _env = env;
        _config = config;
        _db = db;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (!_env.IsDevelopment()) return NotFound();
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        if (!_env.IsDevelopment()) return NotFound();

        if (username == "admin" && password == "admin")
        {
            await IssueDevAdminAsync();
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid dev credentials");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Impersonate()
    {
        if (!_env.IsDevelopment()) return NotFound();
        var users = await _db.Users
            .Select(u => new DevUserVm
            {
                Id = u.Id,
                Email = u.Email ?? string.Empty,
                UserName = u.UserName ?? string.Empty,
                ExternalAuthId = u.ExternalAuthId
            })
            .OrderBy(u => u.Email)
            .ToListAsync();

        return View(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> ImpersonateById(Guid id)
    {
        if (!_env.IsDevelopment()) return NotFound();
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "User not found");
            return await Impersonate();
        }

        var roles = await _userManager.GetRolesAsync(user);
        var scopes = _config.GetSection("AzureB2CScopes");
        var roleToScope = new Dictionary<string, string?>
        {
            ["Admin"] = scopes["Admin"],
            ["Client"] = scopes["Client"],
            ["User"] = scopes["User"],
        };

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Email ?? user.UserName ?? "Impersonated User"),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("oid", user.ExternalAuthId ?? user.Id.ToString()),
            new("impersonating", "true"),
            new("impersonated_user_id", user.Id.ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            if (roleToScope.TryGetValue(role, out var scope) && !string.IsNullOrEmpty(scope))
            {
                claims.Add(new Claim("scp", scope!));
            }
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Redirect("/");
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> StopImpersonation()
    {
        if (!_env.IsDevelopment()) return NotFound();
        // Re-issue dev admin identity
        await IssueDevAdminAsync();
        return Redirect("/");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Danger()
    {
        if (!_env.IsDevelopment()) return NotFound();
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDatabase()
    {
        if (!_env.IsDevelopment()) return NotFound();

        await _db.Database.EnsureDeletedAsync();
        await _db.Database.MigrateAsync();
        await DebtManager.Web.Data.ArticleSeeder.SeedArticlesAsync(_db);
        await DebtManager.Web.Auth.IdentitySeed.EnsureRolesAsync(HttpContext.RequestServices);

        TempData["Message"] = "Database reset and migrations applied.";
        return RedirectToAction(nameof(Danger));
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        if (!_env.IsDevelopment()) return NotFound();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/");
    }

    private async Task IssueDevAdminAsync()
    {
        var adminScope = _config.GetValue<string>("AzureB2CScopes:Admin") ?? string.Empty;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "Dev Admin"),
            new(ClaimTypes.Email, "dev.admin@local.test"),
            new("oid", "dev-admin"),
            new(ClaimTypes.Role, "Admin"),
        };
        if (!string.IsNullOrEmpty(adminScope))
        {
            claims.Add(new Claim("scp", adminScope));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}

public class DevUserVm
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? ExternalAuthId { get; set; }
}
