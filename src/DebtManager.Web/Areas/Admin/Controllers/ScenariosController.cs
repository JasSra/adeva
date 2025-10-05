using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Data;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public partial class ScenariosController : Controller
{
    private readonly AppDbContext _context;

    public ScenariosController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Test Scenarios";

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
            var result = await ScenarioGenerator.GenerateAsync(_context, req);
            TempData["SuccessMessage"] = $"Generated pack {result.PackId} ({result.PackName}) — Orgs: {result.OrganizationIds.Count}, Debtors: {result.DebtorIds.Count}, Debts: {result.DebtIds.Count}.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Error generating scenarios: {ex.Message}";
        }
        return RedirectToAction(nameof(Index));
    }
}
