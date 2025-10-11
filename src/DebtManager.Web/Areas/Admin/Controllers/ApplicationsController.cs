using Microsoft.AspNetCore.Mvc;
using DebtManager.Web.Services;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Audit;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
public class ApplicationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IAuditService _auditService;

    public ApplicationsController(AppDbContext db, IOrganizationRepository organizationRepository, IAuditService auditService)
    {
        _db = db;
        _organizationRepository = organizationRepository;
        _auditService = auditService;
    }

    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Pending Applications";
        ViewBag.Search = search;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var query = _db.Organizations.Where(o => !o.IsApproved).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.Name.Contains(search) || 
                                    o.LegalName.Contains(search) ||
                                    o.Abn.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var organizations = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        ViewBag.TotalCount = totalCount;
        ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        await _auditService.LogAsync("VIEW_APPLICATIONS", "Applications", details: $"Searched: {search}");

        return View(organizations);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Application Details";
        
        var organization = await _organizationRepository.GetAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        await _auditService.LogAsync("VIEW_APPLICATION_DETAILS", "Application", id.ToString(), $"Organization: {organization.Name}");

        return View(organization);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var organization = await _organizationRepository.GetAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        organization.Approve();
        await _organizationRepository.SaveChangesAsync();

        await _auditService.LogAsync("APPROVE_APPLICATION", "Application", id.ToString(), $"Approved organization: {organization.Name}");

        TempData["Message"] = $"Application for {organization.Name} approved successfully";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveWithDetails(Guid id, string? approvalNote, string? bankAccountName, string? bankBsb, string? bankAccountNumber, string? phoneVerificationNotes)
    {
        var organization = await _organizationRepository.GetAsync(id);
        if (organization == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(bankAccountNumber) && (bankAccountNumber.Length < 4 || bankAccountNumber.Length > 20))
        {
            TempData["ErrorMessage"] = "Invalid bank account number length.";
            return RedirectToAction("Details", new { id });
        }

        organization.SetApprovalNote(approvalNote);
        organization.SetBankDetails(bankAccountName, bankBsb, bankAccountNumber);
        if (!string.IsNullOrWhiteSpace(phoneVerificationNotes))
        {
            var who = User?.Identity?.Name ?? "admin";
            organization.RecordPhoneVerification(phoneVerificationNotes, who);
        }
        organization.Approve();
        await _organizationRepository.SaveChangesAsync();
        await _auditService.LogAsync("APPROVE_APPLICATION", "Application", id.ToString(), $"Approved with details for org: {organization.Name}");
        TempData["Message"] = $"Application for {organization.Name} approved.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reject(Guid id, string reason)
    {
        var organization = await _organizationRepository.GetAsync(id);
        if (organization == null)
        {
            return NotFound();
        }

        organization.SetRejectionReason(reason);
        await _auditService.LogAsync("REJECT_APPLICATION", "Application", id.ToString(), $"Rejected organization: {organization.Name}. Reason: {reason}");

        TempData["Message"] = $"Application for {organization.Name} rejected";
        return RedirectToAction("Index");
    }
}
