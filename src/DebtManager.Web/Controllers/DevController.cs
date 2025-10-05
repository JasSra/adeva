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
    public async Task<IActionResult> FakeSignin(string? returnUrl = null)
    {
        if (!DevAuthEnabled) return NotFound();

        // Ensure no existing session. Sign out and redirect so layout reflects unauth state.
        if (User?.Identity?.IsAuthenticated ?? false)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            // Clear the principal for current request as well to avoid showing Sign out in layout
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
            return RedirectToAction(nameof(FakeSignin), new { returnUrl });
        }

        var faker = new Faker("en_AU");
        var first = faker.Name.FirstName();
        var last = faker.Name.LastName();
        var email = faker.Internet.Email(first.ToLowerInvariant(), last.ToLowerInvariant(), "example.dev");
        var vm = new FakeSigninVm
        {
            FirstName = first,
            LastName = last,
            Email = email.ToLowerInvariant(),
            Issuer = "https://b2c.local/fake", // display only
            IssuerId = Guid.NewGuid().ToString(),
            ScopeClient = false,
            ScopeUser = true,
            ScopeAdmin = false,
            ForceSecuritySetup = true,
            CreateClientOrganization = false,
            CreateDebtor = false,
            ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FakeSignin([FromForm] FakeSigninVm vm)
    {
        if (!DevAuthEnabled) return NotFound();

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
            // keep details fresh for demos
            user.Email = vm.Email;
            user.UserName = vm.Email;
            await _userManager.UpdateAsync(user);
        }

        // Ensure roles exist and assign based on selected scopes
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

        // Guarded Admin
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

        // Optional org/debtor creation for scopes
        var faker = new Faker("en_AU");
        if (vm.ScopeClient && vm.CreateClientOrganization)
        {
            var org = new Organization(
                name: faker.Company.CompanyName(),
                legalName: faker.Company.CompanyName() + " Pty Ltd",
                abn: faker.Random.ReplaceNumbers("###########"),
                defaultCurrency: "AUD",
                primaryColorHex: "#1e40af",
                secondaryColorHex: "#3b82f6",
                supportEmail: faker.Internet.Email("support"),
                supportPhone: "1300" + faker.Random.ReplaceNumbers("####"),
                timezone: "Australia/Sydney",
                subdomain: faker.Internet.DomainWord() + "-dev",
                tradingName: faker.Company.CompanyName()
            );
            org.SetTags(new[] { "dummy", "dev:fake-signin", $"owner:{user.Id}" });
            _db.Organizations.Add(org);
            await _db.SaveChangesAsync();
            profile.OrganizationId = org.Id;
            await _db.SaveChangesAsync();
        }

        if (vm.ScopeUser && vm.CreateDebtor)
        {
            // ensure there's an org to attach debtor to
            var orgId = profile.OrganizationId ?? await _db.Organizations
                .OrderByDescending(o => o.CreatedAtUtc)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync() ?? Guid.Empty;

            Organization org;
            if (orgId == Guid.Empty)
            {
                org = new Organization(
                    name: faker.Company.CompanyName(),
                    legalName: faker.Company.CompanyName() + " Pty Ltd",
                    abn: faker.Random.ReplaceNumbers("###########"),
                    defaultCurrency: "AUD",
                    primaryColorHex: "#059669",
                    secondaryColorHex: "#10b981",
                    supportEmail: faker.Internet.Email("support"),
                    supportPhone: "1300" + faker.Random.ReplaceNumbers("####"),
                    timezone: "Australia/Sydney",
                    subdomain: faker.Internet.DomainWord() + "-dev",
                    tradingName: faker.Company.CompanyName()
                );
                org.SetTags(new[] { "dummy", "dev:fake-signin", $"owner:{user.Id}" });
                _db.Organizations.Add(org);
                await _db.SaveChangesAsync();
            }
            else
            {
                org = await _db.Organizations.FindAsync(orgId) ?? throw new InvalidOperationException("Organization not found");
            }

            var refPrefix = (org.Subdomain ?? org.Id.ToString("N").Substring(0, 8)).ToUpperInvariant();
            var debtor = new Debtor(org.Id, $"{refPrefix}-USR-{faker.Random.Int(100, 999)}", vm.Email, "+614" + faker.Random.ReplaceNumbers("########"), vm.FirstName, vm.LastName);
            debtor.SetTags(new[] { "dummy", "dev:fake-signin", $"owner:{user.Id}" });
            _db.Debtors.Add(debtor);
            await _db.SaveChangesAsync();
            profile.DebtorId = debtor.Id;
            await _db.SaveChangesAsync();
        }

        // Keep profile names synced if fields exist
        try
        {
            profile.FirstName = vm.FirstName;
            profile.LastName = vm.LastName;
            await _db.SaveChangesAsync();
        }
        catch { /* ignore if profile doesn't have these columns */ }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        // Choose landing
        if (vm.ForceSecuritySetup)
        {
            return Redirect("/Security/Setup");
        }

        string landing = "/";
        if (vm.ScopeAdmin) landing = "/Admin";
        else if (vm.ScopeClient) landing = "/Client";
        else if (vm.ScopeUser) landing = "/User";

        return Redirect(string.IsNullOrWhiteSpace(vm.ReturnUrl) ? landing : vm.ReturnUrl);
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
        // Re-issue dev admin identity
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
