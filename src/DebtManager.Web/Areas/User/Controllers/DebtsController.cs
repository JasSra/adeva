using System.Security.Claims;
using DebtManager.Domain.Debts;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Web.Filters;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
[RequireDebtorOnboarded]
public class DebtsController : Controller
{
    private readonly AppDbContext _db;

    public DebtsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? search, string? status, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "My Debts";
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;

        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null)
        {
            return Redirect("/User/Onboarding");
        }

        var query = _db.Debts.Where(d => d.DebtorId == debtorId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => (d.ClientReferenceNumber ?? string.Empty).Contains(search) || (d.Category ?? string.Empty).Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && TryParseStatus(status, out var st))
        {
            query = query.Where(d => d.Status == st);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(d => d.UpdatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DebtListItemVm
            {
                Id = d.Id,
                Reference = string.IsNullOrWhiteSpace(d.ClientReferenceNumber) ? ("D-" + d.Id.ToString().Substring(0, 8)) : d.ClientReferenceNumber,
                OriginalAmount = d.OriginalPrincipal,
                Outstanding = d.OutstandingPrincipal,
                DueDateUtc = d.DueDateUtc,
                Status = d.Status
            })
            .ToListAsync(ct);

        var model = new DebtsIndexVm
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
        return View(model);
    }

    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Debt Details";

        // Keep existing view placeholder but ensure we pass a friendly reference
        var debt = await _db.Debts.FirstOrDefaultAsync(d => d.Id == id, ct);
        var refId = debt?.ClientReferenceNumber ?? ("D-" + id.ToString().Substring(0, 8));
        ViewBag.DebtId = refId;
        return View();
    }

    public IActionResult Dispute(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Dispute Debt";
        ViewBag.DebtId = id;
        return View();
    }

    public IActionResult RequestExtension(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Request Extension";
        ViewBag.DebtId = id;
        return View();
    }

    public IActionResult Cancel(Guid id)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Cancel Debt Request";
        ViewBag.DebtId = id;
        return View();
    }

    private async Task<Guid?> GetCurrentDebtorIdAsync(CancellationToken ct)
    {
        var externalId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(externalId)) return null;
        var user = await _db.Users.FirstOrDefaultAsync(u => u.ExternalAuthId == externalId, ct);
        if (user == null) return null;
        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        return profile?.DebtorId;
    }

    private static bool TryParseStatus(string status, out DebtStatus parsed)
    {
        parsed = default; // ensure definite assignment
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        switch (status.Trim().ToLowerInvariant())
        {
            case "active":
                parsed = DebtStatus.Active; return true;
            case "in_arrears":
            case "inarrears":
                parsed = DebtStatus.InArrears; return true;
            case "settled":
                parsed = DebtStatus.Settled; return true;
            case "disputed":
                parsed = DebtStatus.Disputed; return true;
            case "pendingassignment":
            case "pending_assignment":
                parsed = DebtStatus.PendingAssignment; return true;
            default:
                return Enum.TryParse(status, true, out parsed);
        }
    }
}

public class DebtsIndexVm
{
    public List<DebtListItemVm> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}

public class DebtListItemVm
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal Outstanding { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public DebtStatus Status { get; set; }
}
