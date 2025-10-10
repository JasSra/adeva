using DebtManager.Web.Services;
using DebtManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";

        // Get dashboard statistics from the database
        var pendingApplicationsCount = await _db.Organizations.CountAsync(o => !o.IsApproved);
        var totalDebtsCount = await _db.Debts.CountAsync();
        var activeOrganizationsCount = await _db.Organizations.CountAsync(o => o.IsApproved);
        
        // Get today's transaction total
        var today = DateTime.UtcNow.Date;
        var todayTransactionsTotal = await _db.Transactions
            .Where(t => t.CreatedAtUtc >= today && t.CreatedAtUtc < today.AddDays(1))
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        ViewBag.PendingApplicationsCount = pendingApplicationsCount;
        ViewBag.TotalDebtsCount = totalDebtsCount;
        ViewBag.ActiveOrganizationsCount = activeOrganizationsCount;
        ViewBag.TodayTransactionsTotal = todayTransactionsTotal;

        return View();
    }
}
