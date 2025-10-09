using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Audit;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
public class OrganizationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IAuditService _auditService;

    public OrganizationsController(AppDbContext db, IOrganizationRepository organizationRepository, IAuditService auditService)
    {
        _db = db;
        _organizationRepository = organizationRepository;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Organizations Management";
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var query = _db.Organizations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.Name.Contains(search) || 
                                    o.LegalName.Contains(search) ||
                                    o.Abn.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.Equals("approved", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => o.IsApproved);
            }
            else if (status.Equals("pending", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(o => !o.IsApproved);
            }
        }

        var totalCount = await query.CountAsync();
        var organizations = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await _auditService.LogAsync("VIEW_ORGANIZATIONS", "Organizations", details: $"Searched: {search}, Status: {status}");

        return View(organizations);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Organization Details";
        
        var organization = await _organizationRepository.GetAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_ORGANIZATION_DETAILS", "Organization", id.ToString(), $"Organization: {organization.Name}");

        return View(organization);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Edit Organization";
        
        var organization = await _organizationRepository.GetAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_ORGANIZATION_EDIT", "Organization", id.ToString(), $"Organization: {organization.Name}");

        return View(organization);
    }
}
