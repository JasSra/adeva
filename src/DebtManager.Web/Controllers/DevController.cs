using System.Security.Claims;
using Bogus;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;
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

    private bool DevAuthEnabled => _env.IsDevelopment() && _config.GetValue<bool>("DevAuth:EnableFakeSignin");

    [HttpGet]
    public IActionResult Login(string? returnUrl = null, bool? fake = null)
    {
        if (!DevAuthEnabled) return NotFound();
        if (fake == true) return RedirectToAction(nameof(FakeSignin), new { returnUrl });
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
    {
        if (!DevAuthEnabled) return NotFound();

        if (username == "admin" && password == "admin")
        {
            await IssueDevAdminAsync();
            return Redirect(string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl);
        }

        ModelState.AddModelError(string.Empty, "Invalid dev credentials");
        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    // ===== Development Fake B2C sign-in =====
    [HttpGet]
    public async Task<IActionResult> FakeSignin(string? role = null, string? returnUrl = null)
    {
        if (!DevAuthEnabled) return NotFound();

        // Ensure no existing session. Sign out and redirect so layout reflects unauth state.
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        // Auto mode: role provided (client|user) -> immediately issue fake sign-in just like OIDC
        if (!string.IsNullOrWhiteSpace(role))
        {
            var faker = new Faker("en_AU");
            var first = faker.Name.FirstName();
            var last = faker.Name.LastName();
            var email = faker.Internet.Email(first.ToLowerInvariant(), last.ToLowerInvariant(), "example.dev").ToLowerInvariant();

            var vm = new FakeSigninVm
            {
                FirstName = first,
                LastName = last,
                Email = email,
                Issuer = "https://b2c.local/fake",
                IssuerId = Guid.NewGuid().ToString(),
                ScopeClient = string.Equals(role, "client", StringComparison.OrdinalIgnoreCase),
                ScopeUser = string.Equals(role, "user", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(role),
                ScopeAdmin = false,
                ForceSecuritySetup = false,
                CreateClientOrganization = false,
                CreateDebtor = false,
                ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
            };

            return await PerformFakeSigninAsync(vm);
        }

        // Manual mode: render the form with prefilled random values
        var f = new Faker("en_AU");
        var firstName = f.Name.FirstName();
        var lastName = f.Name.LastName();
        var vm2 = new FakeSigninVm
        {
            FirstName = firstName,
            LastName = lastName,
            Email = f.Internet.Email(firstName.ToLowerInvariant(), lastName.ToLowerInvariant(), "example.dev").ToLowerInvariant(),
            Issuer = "https://b2c.local/fake",
            IssuerId = Guid.NewGuid().ToString(),
            ScopeClient = false,
            ScopeUser = true,
            ScopeAdmin = false,
            ForceSecuritySetup = true,
            CreateClientOrganization = false,
            CreateDebtor = false,
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
        };

        return View(vm2);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FakeSignin([FromForm] FakeSigninVm vm)
    {
        if (!DevAuthEnabled) return NotFound();

        return await PerformFakeSigninAsync(vm);
    }

    private async Task<IActionResult> PerformFakeSigninAsync(FakeSigninVm vm)
    {
        // Ensure no existing session
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        }

        if (string.IsNullOrWhiteSpace(vm.Email) || string.IsNullOrWhiteSpace(vm.IssuerId))
        {
            ModelState.AddModelError(string.Empty, "Email and Issuer ID are required.");
            return View(vm);
        }

        // Create or update the application user keyed by ExternalAuthId (oid)
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == vm.IssuerId);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                ExternalAuthId = vm.IssuerId,
                Email = vm.Email,
                UserName = vm.Email,
                EmailConfirmed = true
            };
            await _userManager.CreateAsync(user);
        }
        else
        {
            user.Email = vm.Email;
            user.UserName = vm.Email;
            await _userManager.UpdateAsync(user);
        }

        async Task EnsureRoleAsync(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole(role));
            }
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        var scopes = _config.GetSection("AzureB2CScopes");
        var clientScope = scopes["Client"] ?? string.Empty;
        var userScope = scopes["User"] ?? string.Empty;
        var adminScope = scopes["Admin"] ?? string.Empty;

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, $"{vm.FirstName} {vm.LastName}".Trim()),
            new(ClaimTypes.Email, vm.Email),
            new("oid", vm.IssuerId),
            new("iss", vm.Issuer ?? "dev-fake"),
        };

        if (vm.ScopeClient)
        {
            await EnsureRoleAsync("Client");
            claims.Add(new Claim(ClaimTypes.Role, "Client"));
            if (!string.IsNullOrEmpty(clientScope)) claims.Add(new Claim("scp", clientScope));
        }
        if (vm.ScopeUser)
        {
            await EnsureRoleAsync("User");
            claims.Add(new Claim(ClaimTypes.Role, "User"));
            if (!string.IsNullOrEmpty(userScope)) claims.Add(new Claim("scp", userScope));
        }
        if (vm.ScopeAdmin && _config.GetValue<bool>("DevAuth:AllowAdminScope"))
        {
            await EnsureRoleAsync("Admin");
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            if (!string.IsNullOrEmpty(adminScope)) claims.Add(new Claim("scp", adminScope));
        }

        // Ensure profile exists
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
        if (profile == null)
        {
            profile = new UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
            };
            _db.UserProfiles.Add(profile);
            await _db.SaveChangesAsync();
        }

        try
        {
            profile.FirstName = vm.FirstName;
            profile.LastName = vm.LastName;
            await _db.SaveChangesAsync();
        }
        catch { }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        if (vm.ForceSecuritySetup)
        {
            return Redirect("/Security/Setup");
        }

        // Mimic OIDC: prefer returnUrl when provided
        if (!string.IsNullOrWhiteSpace(vm.ReturnUrl))
        {
            return Redirect(vm.ReturnUrl);
        }

        // Choose landing by role
        if (vm.ScopeAdmin) return Redirect("/Admin");
        if (vm.ScopeClient) return Redirect("/Client");
        if (vm.ScopeUser) return Redirect("/User");
        return Redirect("/");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Impersonate()
    {
        if (!DevAuthEnabled) return NotFound();
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
        if (!DevAuthEnabled) return NotFound();
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
        if (!DevAuthEnabled) return NotFound();
        await IssueDevAdminAsync();
        return Redirect("/");
    }

    [Authorize(Roles = "Admin")]
    [HttpGet]
    public IActionResult Danger()
    {
        if (!DevAuthEnabled) return NotFound();
        return View();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetDatabase()
    {
        if (!DevAuthEnabled) return NotFound();

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
        if (!DevAuthEnabled) return NotFound();
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

public class FakeSigninVm
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string IssuerId { get; set; } = string.Empty; // mapped to oid
    public bool ScopeClient { get; set; }
    public bool ScopeUser { get; set; } = true;
    public bool ScopeAdmin { get; set; }

    // Options
    public bool ForceSecuritySetup { get; set; }
    public bool CreateClientOrganization { get; set; }
    public bool CreateDebtor { get; set; }

    public string? ReturnUrl { get; set; }
}
