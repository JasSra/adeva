using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Audit;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class TransactionsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAuditService _auditService;

    public TransactionsController(AppDbContext db, ITransactionRepository transactionRepository, IAuditService auditService)
    {
        _db = db;
        _transactionRepository = transactionRepository;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, string? type, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Transactions";
        ViewBag.Search = search;
        ViewBag.Type = type;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var query = _db.Transactions.Include(t => t.Debtor).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => (t.ProviderRef != null && t.ProviderRef.Contains(search)) ||
                                    (t.Debtor != null && (t.Debtor.FirstName.Contains(search) || t.Debtor.LastName.Contains(search))));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = type.ToLower() switch
            {
                "inbound" => query.Where(t => t.Direction == DebtManager.Domain.Payments.TransactionDirection.Inbound),
                "outbound" => query.Where(t => t.Direction == DebtManager.Domain.Payments.TransactionDirection.Outbound),
                _ => query
            };
        }

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.ProcessedAtUtc >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.ProcessedAtUtc <= toDate.Value);
        }

        var totalCount = await query.CountAsync();
        var transactions = await query
            .OrderByDescending(t => t.ProcessedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await _auditService.LogAsync("VIEW_TRANSACTIONS", "Transactions", details: $"Searched: {search}, Type: {type}");

        return View(transactions);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Transaction Details";
        
        var transaction = await _transactionRepository.GetAsync(id);
        if (transaction == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_TRANSACTION_DETAILS", "Transaction", id.ToString(), $"Transaction: {transaction.ProviderRef}");

        return View(transaction);
    }
}
