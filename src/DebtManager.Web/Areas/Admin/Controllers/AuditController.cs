using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class AuditController : Controller
{
    private readonly IAuditLogRepository _auditLogRepository;

    public AuditController(IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public async Task<IActionResult> Index(string? search, string? entity, DateTime? fromDate, DateTime? toDate, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Audit Log";
        ViewBag.Search = search;
        ViewBag.Entity = entity;
        ViewBag.FromDate = fromDate;
        ViewBag.ToDate = toDate;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var skip = (page - 1) * pageSize;
        var logs = await _auditLogRepository.SearchAsync(search, entity, fromDate, toDate, skip, pageSize);
        var totalCount = await _auditLogRepository.GetCountAsync(search, entity, fromDate, toDate);
        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return View(logs);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Audit Entry Details";
        
        var auditLog = await _auditLogRepository.GetAsync(id);
        if (auditLog == null)
        {
            return NotFound();
        }

        return View(auditLog);
    }
}
