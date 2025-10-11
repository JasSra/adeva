using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Data;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Debts;
using Bogus;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public partial class ScenariosController : Controller
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public ScenariosController(AppDbContext context, UserManager<ApplicationUser> userManager, RoleManager<ApplicationRole> roleManager)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IActionResult> Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Test Scenarios";

        // Check database connectivity
        try
        {
            ViewBag.DatabaseConnected = await _context.Database.CanConnectAsync();
        }
        catch
        {
            ViewBag.DatabaseConnected = false;
        }

        // Faker defaults for quick form
        var faker = new Faker("en_AU");
        var first = faker.Name.FirstName();
        var last = faker.Name.LastName();
        ViewBag.QuickOrg = faker.Company.CompanyName();
        ViewBag.QuickFirst = first;
        ViewBag.QuickLast = last;
        ViewBag.QuickEmail = faker.Internet.Email(first.ToLowerInvariant(), last.ToLowerInvariant(), "example.local").ToLowerInvariant();
        ViewBag.QuickPhone = faker.Phone.PhoneNumber("+61#########");
        ViewBag.QuickAmount = faker.Finance.Amount(800, 6000, 2);

        // Check if dummy data already exists
        var hasDummyData = await _context.Organizations.AnyAsync(o => o.TagsCsv.Contains("dummy"));
        ViewBag.HasDummyData = hasDummyData;

        if (hasDummyData)
        {
            // Get counts of dummy data
            ViewBag.OrganizationCount = await _context.Organizations.CountAsync(o => o.TagsCsv.Contains("dummy"));
            ViewBag.DebtorCount = await _context.Debtors.CountAsync(d => d.TagsCsv.Contains("dummy"));
            ViewBag.DebtCount = await _context.Debts.CountAsync(d => d.TagsCsv.Contains("dummy"));
            ViewBag.PaymentPlanCount = await _context.PaymentPlans.CountAsync(p => p.TagsCsv.Contains("dummy"));
            ViewBag.TransactionCount = await _context.Transactions
                .CountAsync(t => _context.Debts.Any(d => d.Id == t.DebtId && d.TagsCsv.Contains("dummy")));

            // Get scenario counts
            ViewBag.PendingOrgCount = await _context.Organizations.CountAsync(o => o.TagsCsv.Contains("scenario:pending-approval"));
            ViewBag.RejectedOrgCount = await _context.Organizations.CountAsync(o => o.TagsCsv.Contains("scenario:rejected"));
            ViewBag.ActiveOrgCount = await _context.Organizations.CountAsync(o => o.TagsCsv.Contains("scenario:active-established"));
            
            ViewBag.NewCustomerCount = await _context.Debtors.CountAsync(d => d.TagsCsv.Contains("scenario:new-customer"));
            ViewBag.ActiveCustomerCount = await _context.Debtors.CountAsync(d => d.TagsCsv.Contains("scenario:active-paying"));
            ViewBag.DelinquentCustomerCount = await _context.Debtors.CountAsync(d => d.TagsCsv.Contains("scenario:delinquent-non-responsive"));
            ViewBag.SettledCustomerCount = await _context.Debtors.CountAsync(d => d.TagsCsv.Contains("scenario:settled"));
            
            ViewBag.NewDebtCount = await _context.Debts.CountAsync(d => d.TagsCsv.Contains("scenario:new-debt"));
            ViewBag.ActiveDebtCount = await _context.Debts.CountAsync(d => d.TagsCsv.Contains("scenario:active-on-plan"));
            ViewBag.ArrearsDebtCount = await _context.Debts.CountAsync(d => d.TagsCsv.Contains("scenario:in-arrears-high-risk"));
            ViewBag.DisputedDebtCount = await _context.Debts.CountAsync(d => d.TagsCsv.Contains("scenario:disputed"));
            ViewBag.SettledDebtCount = await _context.Debts.CountAsync(d => d.TagsCsv.Contains("scenario:settled"));
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seed()
    {
        try
        {
            // Test database connectivity first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                TempData["ErrorMessage"] = "❌ Cannot connect to database. Please ensure SQL Server is running on localhost:1433. Run: cd deploy && docker compose up -d";
                return RedirectToAction(nameof(Index));
            }

            await DummyDataSeeder.SeedDummyDataAsync(_context);
            TempData["SuccessMessage"] = "Dummy data seeded successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error seeding dummy data: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear()
    {
        try
        {
            // Test database connectivity first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                TempData["ErrorMessage"] = "❌ Cannot connect to database. Please ensure SQL Server is running on localhost:1433. Run: cd deploy && docker compose up -d";
                return RedirectToAction(nameof(Index));
            }

            // Remove all dummy data
            var dummyOrgs = await _context.Organizations.Where(o => o.TagsCsv.Contains("dummy")).ToListAsync();
            var dummyDebtors = await _context.Debtors.Where(d => d.TagsCsv.Contains("dummy")).ToListAsync();
            var dummyDebts = await _context.Debts.Where(d => d.TagsCsv.Contains("dummy")).ToListAsync();
            var dummyPlans = await _context.PaymentPlans.Where(p => p.TagsCsv.Contains("dummy")).ToListAsync();
            
            // Get transactions linked to dummy debts
            var dummyDebtIds = dummyDebts.Select(d => d.Id).ToList();
            var dummyTransactions = await _context.Transactions.Where(t => dummyDebtIds.Contains(t.DebtId)).ToListAsync();

            _context.Transactions.RemoveRange(dummyTransactions);
            _context.PaymentPlans.RemoveRange(dummyPlans);
            _context.Debts.RemoveRange(dummyDebts);
            _context.Debtors.RemoveRange(dummyDebtors);
            _context.Organizations.RemoveRange(dummyOrgs);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "All dummy data cleared successfully!";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error clearing dummy data: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate([FromForm] ScenarioRequest req)
    {
        try
        {
            // Test database connectivity first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                TempData["ErrorMessage"] = "❌ Cannot connect to database. Please ensure SQL Server is running on localhost:1433. Run: cd deploy && docker compose up -d";
                return RedirectToAction(nameof(Index));
            }

            var result = await ScenarioGenerator.GenerateAsync(_context, req);
            await CreateScenarioActorsAsync(result);
            TempData["SuccessMessage"] = $"Generated pack {result.PackId} ({result.PackName}) – Orgs: {result.OrganizationIds.Count}, Debtors: {result.DebtorIds.Count}, Debts: {result.DebtIds.Count}. Impersonation users created.";
        }
        catch (DbUpdateException dbEx)
        {
            var inner = dbEx.InnerException?.Message ?? "(no inner)";
            var entities = string.Join(", ", dbEx.Entries.Select(e => e.Entity.GetType().Name));
            TempData["ErrorMessage"] = $"Error generating scenarios: {dbEx.Message}<br/>Inner: {inner}<br/>Entities: {entities}<br/>Stack: {dbEx.StackTrace?.Substring(0, Math.Min(500, dbEx.StackTrace?.Length ?? 0))}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error generating scenarios: {ex.Message}<br/>Stack: {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}";
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task CreateScenarioActorsAsync(ScenarioResult result)
    {
        // Ensure roles
        async Task EnsureRole(string role)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new ApplicationRole(role));
            }
        }
        await EnsureRole("Admin");
        await EnsureRole("Client");
        await EnsureRole("User");

        // Admin user (one-off)
        var adminEmail = $"scenario.admin@{result.PackId}.local";
        await EnsureUserAsync(adminEmail, externalId: $"admin-{result.PackId}", roles: new[] { "Admin" });

        // Client users for first 2 orgs
        var orgs = await _context.Organizations.Where(o => result.OrganizationIds.Contains(o.Id)).Take(2).ToListAsync();
        foreach (var org in orgs)
        {
            var email = $"ops@{(org.Subdomain ?? org.Id.ToString("N").Substring(0,8))}.local".ToLowerInvariant();
            var u = await EnsureUserAsync(email, externalId: $"org-{org.Id}", roles: new[] { "Client" });
            // Link profile
            await EnsureProfileAsync(u.Id, organizationId: org.Id);
        }

        // Debtor users for first 5 debtors
        var debtors = await _context.Debtors.Where(d => result.DebtorIds.Contains(d.Id)).Take(5).ToListAsync();
        foreach (var d in debtors)
        {
            var email = string.IsNullOrWhiteSpace(d.Email) ? $"{d.ReferenceId.ToLowerInvariant()}@debtor.local" : d.Email.ToLowerInvariant();
            var u = await EnsureUserAsync(email, externalId: $"debtor-{d.Id}", roles: new[] { "User" });
            await EnsureProfileAsync(u.Id, debtorId: d.Id, organizationId: d.OrganizationId);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuickDebtorCase(string orgName, string debtorEmail, string debtorFirstName, string debtorLastName, string phone, decimal amount)
    {
        // Log to verify this method is being called
        System.Diagnostics.Debug.WriteLine($"QuickDebtorCase called: orgName={orgName}, email={debtorEmail}");
        
        try
        {
            // Test database connectivity first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                TempData["ErrorMessage"] = "❌ Cannot connect to database. Please ensure SQL Server is running on localhost:1433. Run: cd deploy && docker compose up -d";
                return RedirectToAction(nameof(Index));
            }

            // Auto-populate missing inputs with Faker defaults
            var faker = new Faker("en_AU");
            if (string.IsNullOrWhiteSpace(orgName)) orgName = faker.Company.CompanyName();
            if (string.IsNullOrWhiteSpace(debtorFirstName)) debtorFirstName = faker.Name.FirstName();
            if (string.IsNullOrWhiteSpace(debtorLastName)) debtorLastName = faker.Name.LastName();
            if (string.IsNullOrWhiteSpace(debtorEmail)) debtorEmail = faker.Internet.Email(debtorFirstName.ToLowerInvariant(), debtorLastName.ToLowerInvariant(), "example.local").ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(phone)) phone = faker.Phone.PhoneNumber("+61#########");
            if (amount <= 0) amount = faker.Finance.Amount(800, 6000, 2);

            var org = await _context.Organizations.FirstOrDefaultAsync(o => o.Name == orgName);
            if (org == null)
            {
                org = new Organization(orgName, orgName + " Pty Ltd", Guid.NewGuid().ToString("N")[..11], "AUD", "#1e3a8a", "#3b82f6", $"support@{orgName.Replace(' ', '-').ToLowerInvariant()}.local", "1300000000", "Australia/Sydney", subdomain: orgName.Replace(' ', '-').ToLowerInvariant());
                org.Approve();
                org.MarkOnboarded();
                org.SetTags(new[] { "dummy", "scenario:quick-case" });
                _context.Organizations.Add(org);
                await _context.SaveChangesAsync();
            }

            var debtor = new Debtor(org.Id, $"{org.Subdomain?.ToUpperInvariant() ?? org.Id.ToString("N")[..6]}-QA-{Random.Shared.Next(1,999):000}", debtorEmail, phone, debtorFirstName, debtorLastName);
            debtor.SetStatus(DebtorStatus.Invited);
            debtor.SetTags(new[] { "dummy", "scenario:quick-case" });
            _context.Debtors.Add(debtor);
            await _context.SaveChangesAsync();

            var debt = new Debt(org.Id, debtor.Id, amount, org.DefaultCurrency, $"EXT-{debtor.ReferenceId}", $"REF-{debtor.ReferenceId}");
            debt.SetDueDate(DateTime.UtcNow.AddDays(14));
            debt.SetStatus(DebtStatus.PendingAssignment);
            debt.SetTags(new[] { "dummy", "scenario:quick-case" });
            _context.Debts.Add(debt);
            await _context.SaveChangesAsync();

            // Create client org user and debtor portal user
            await EnsureRoleExists("Client");
            await EnsureRoleExists("User");

            var clientEmail = $"ops@{(org.Subdomain ?? org.Id.ToString("N")[..8])}.local".ToLowerInvariant();
            var clientUser = await EnsureUserAsync(clientEmail, externalId: $"org-{org.Id}", roles: new[] { "Client" });
            await EnsureProfileAsync(clientUser.Id, organizationId: org.Id);

            var portalUser = await EnsureUserAsync(debtorEmail, externalId: $"debtor-{debtor.Id}", roles: new[] { "User" });
            await EnsureProfileAsync(portalUser.Id, debtorId: debtor.Id, organizationId: org.Id);

            // Store the debt ID and debtor email in TempData for easy access
            TempData["SuccessMessage"] = $"✅ Quick case created successfully!";
            TempData["TestDebtId"] = debt.Id.ToString();
            TempData["TestDebtorEmail"] = debtorEmail;
            TempData["TestClientEmail"] = clientEmail;
            TempData["AcceptUrl"] = $"/User/Accept/{debt.Id}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error creating quick case: {ex.Message}<br/><br/>Inner: {ex.InnerException?.Message}<br/><br/>Stack: {ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClientOrg(string name, string contactEmail)
    {
        try
        {
            // Test database connectivity first
            var canConnect = await _context.Database.CanConnectAsync();
            if (!canConnect)
            {
                TempData["ErrorMessage"] = "❌ Cannot connect to database. Please ensure SQL Server is running on localhost:1433. Run: cd deploy && docker compose up -d";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(contactEmail))
            {
                TempData["ErrorMessage"] = "Organization name and contact email are required.";
                return RedirectToAction(nameof(Index));
            }

            var existing = await _context.Organizations.FirstOrDefaultAsync(o => o.Name == name);
            if (existing != null)
            {
                TempData["ErrorMessage"] = "An organization with that name already exists.";
                return RedirectToAction(nameof(Index));
            }

            var org = new Organization(name, name + " Pty Ltd", Guid.NewGuid().ToString("N")[..11], "AUD", "#2563eb", "#3b82f6", $"support@{name.Replace(' ', '-').ToLowerInvariant()}.local", "1300000000", "Australia/Sydney", subdomain: name.Replace(' ', '-').ToLowerInvariant(), tradingName: name);
            org.Approve();
            org.MarkOnboarded();
            org.SetTags(new[] { "dummy", "scenario:client-org" });
            _context.Organizations.Add(org);
            await _context.SaveChangesAsync();

            // Create client user
            await EnsureRoleExists("Client");
            var clientUser = await EnsureUserAsync(contactEmail.ToLowerInvariant(), externalId: $"org-{org.Id}", roles: new[] { "Client" });
            await EnsureProfileAsync(clientUser.Id, organizationId: org.Id);

            TempData["SuccessMessage"] = $"Client organization created and approved. Org={org.Name}, Client={contactEmail}";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error creating client org: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task EnsureRoleExists(string role)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            await _roleManager.CreateAsync(new ApplicationRole(role));
        }
    }

    private async Task<ApplicationUser> EnsureUserAsync(string email, string externalId, string[] roles)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                ExternalAuthId = externalId
            };
            await _userManager.CreateAsync(user);
        }
        else
        {
            user.ExternalAuthId = user.ExternalAuthId ?? externalId;
            await _userManager.UpdateAsync(user);
        }

        foreach (var role in roles)
        {
            if (!await _userManager.IsInRoleAsync(user, role))
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        return user;
    }

    private async Task EnsureProfileAsync(Guid userId, Guid? debtorId = null, Guid? organizationId = null)
    {
        var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
        {
            profile = new DebtManager.Infrastructure.Identity.UserProfile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DebtorId = debtorId,
                OrganizationId = organizationId
            };
            _context.UserProfiles.Add(profile);
        }
        else
        {
            profile.DebtorId = profile.DebtorId ?? debtorId;
            profile.OrganizationId = profile.OrganizationId ?? organizationId;
        }
        await _context.SaveChangesAsync();
    }
}
