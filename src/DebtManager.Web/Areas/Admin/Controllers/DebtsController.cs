using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Audit;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DebtsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IDebtRepository _debtRepository;
    private readonly IAuditService _auditService;

    public DebtsController(AppDbContext db, IDebtRepository debtRepository, IAuditService auditService)
    {
        _db = db;
        _debtRepository = debtRepository;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Debts Management";
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var query = _db.Debts.Include(d => d.Debtor).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.ClientReferenceNumber.Contains(search) ||
                                    (d.Debtor != null && (d.Debtor.FirstName.Contains(search) || d.Debtor.LastName.Contains(search))));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = status.ToLower() switch
            {
                "active" => query.Where(d => d.Status == DebtManager.Domain.Debts.DebtStatus.Active),
                "settled" => query.Where(d => d.Status == DebtManager.Domain.Debts.DebtStatus.Settled),
                "inarrears" => query.Where(d => d.Status == DebtManager.Domain.Debts.DebtStatus.InArrears),
                _ => query
            };
        }

        var totalCount = await query.CountAsync();
        var debts = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await _auditService.LogAsync("VIEW_DEBTS", "Debts", details: $"Searched: {search}, Status: {status}");

        return View(debts);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Debt Details";
        
        var debt = await _debtRepository.GetAsync(id);
        if (debt == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_DEBT_DETAILS", "Debt", id.ToString(), $"Debt Ref: {debt.ClientReferenceNumber}");

        return View(debt);
    }

    public async Task<IActionResult> Create()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Create Debt";
        
        await _auditService.LogAsync("VIEW_CREATE_DEBT", "Debt");
        
        return View();
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Edit Debt";
        
        var debt = await _debtRepository.GetAsync(id);
        if (debt == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_EDIT_DEBT", "Debt", id.ToString(), $"Debt Ref: {debt.ClientReferenceNumber}");

        return View(debt);
    }
}
