using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Audit;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class PaymentsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IAuditService _auditService;

    public PaymentsController(AppDbContext db, IAuditService auditService)
    {
        _db = db;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Payments";
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var query = _db.Transactions
            .Include(t => t.Debtor)
            .Where(t => t.Direction == DebtManager.Domain.Payments.TransactionDirection.Inbound)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => (t.ProviderRef != null && t.ProviderRef.Contains(search)) ||
                                    (t.Debtor != null && (t.Debtor.FirstName.Contains(search) || t.Debtor.LastName.Contains(search))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = status.ToLower() switch
            {
                "succeeded" => query.Where(t => t.Status == DebtManager.Domain.Payments.TransactionStatus.Succeeded),
                "pending" => query.Where(t => t.Status == DebtManager.Domain.Payments.TransactionStatus.Pending),
                "failed" => query.Where(t => t.Status == DebtManager.Domain.Payments.TransactionStatus.Failed),
                _ => query
            };
        }

        var totalCount = await query.CountAsync();
        var payments = await query
            .OrderByDescending(t => t.ProcessedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await _auditService.LogAsync("VIEW_PAYMENTS", "Payments", details: $"Searched: {search}, Status: {status}");

        return View(payments);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Payment Details";
        
        var payment = await _db.Transactions.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_PAYMENT_DETAILS", "Payment", id.ToString(), $"Payment: {payment.ProviderRef}");

        return View(payment);
    }

    public IActionResult CreateAdhoc()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Create Adhoc Payment";
        return View();
    }

    public IActionResult ProcessStripePayment()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Process Stripe Payment";
        return View();
    }

    public IActionResult TestStripePayment()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Test Stripe Payment";
        return View();
    }

    public IActionResult PaymentSuccess()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Payment Successful";
        return View();
    }

    public IActionResult TestSuccess()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Test Payment Successful";
        return View();
    }

    public async Task<IActionResult> RetryFailed()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Retry Failed Payments";

        var failedTransactions = await _db.Transactions
            .Include(t => t.Debtor)
            .Include(t => t.Debt)
            .Where(t => t.Status == DebtManager.Domain.Payments.TransactionStatus.Failed)
            .Where(t => t.ProcessedAtUtc >= DateTime.UtcNow.AddDays(-30))
            .OrderByDescending(t => t.ProcessedAtUtc)
            .ToListAsync();

        await _auditService.LogAsync("VIEW_FAILED_PAYMENTS", "Payments", details: $"Viewed {failedTransactions.Count} failed payments");

        return View(failedTransactions);
    }
}
