using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Audit;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class DebtorsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IDebtorRepository _debtorRepository;
    private readonly IAuditService _auditService;

    public DebtorsController(AppDbContext db, IDebtorRepository debtorRepository, IAuditService auditService)
    {
        _db = db;
        _debtorRepository = debtorRepository;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Debtors Management";
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var query = _db.Debtors.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.FirstName.Contains(search) || 
                                    d.LastName.Contains(search) ||
                                    d.Email.Contains(search) ||
                                    d.ReferenceId.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var debtors = await query
            .OrderByDescending(d => d.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await _auditService.LogAsync("VIEW_DEBTORS", "Debtors", details: $"Searched: {search}");

        return View(debtors);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Debtor Details";
        
        var debtor = await _debtorRepository.GetAsync(id);
        if (debtor == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_DEBTOR_DETAILS", "Debtor", id.ToString(), $"Debtor: {debtor.FirstName} {debtor.LastName}");

        return View(debtor);
    }

    public async Task<IActionResult> Create()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Create Debtor";
        
        await _auditService.LogAsync("VIEW_CREATE_DEBTOR", "Debtor");
        
        return View();
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Edit Debtor";
        
        var debtor = await _debtorRepository.GetAsync(id);
        if (debtor == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_EDIT_DEBTOR", "Debtor", id.ToString(), $"Debtor: {debtor.FirstName} {debtor.LastName}");

        return View(debtor);
    }
}
