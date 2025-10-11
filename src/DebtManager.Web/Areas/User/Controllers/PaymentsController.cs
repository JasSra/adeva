using System.Security.Claims;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DebtManager.Web.Filters;
using DebtManager.Domain.Payments;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
[RequireDebtorOnboarded]
public class PaymentsController : Controller
{
    private readonly AppDbContext _db;

    public PaymentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Payment History";
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        ViewBag.Search = search;
        ViewBag.From = from;
        ViewBag.To = to;

        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null)
        {
            return Redirect("/User/Onboarding");
        }

        var query = _db.Transactions
            .AsNoTracking()
            .Include(t => t.Debt)
            .Where(t => t.DebtorId == debtorId)
            .OrderByDescending(t => t.ProcessedAtUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => (t.Debt!.ClientReferenceNumber ?? string.Empty).Contains(search));
        }
        if (from.HasValue)
        {
            var f = from.Value.Date;
            query = query.Where(t => t.ProcessedAtUtc >= f);
        }
        if (to.HasValue)
        {
            var tmax = to.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(t => t.ProcessedAtUtc <= tmax);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new PaymentListItemVm
            {
                Id = t.Id,
                ProcessedAtUtc = t.ProcessedAtUtc,
                DebtReference = t.Debt != null && !string.IsNullOrEmpty(t.Debt.ClientReferenceNumber)
                    ? t.Debt.ClientReferenceNumber!
                    : ("D-" + t.DebtId.ToString().Substring(0, 8)),
                Amount = t.Amount,
                Currency = t.Currency,
                Method = t.Method,
                Status = t.Status
            })
            .ToListAsync(ct);

        var model = new PaymentsIndexVm
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
        return View(model);
    }

    [HttpGet]
    public IActionResult MakePayment(int? debtId = null)
    {
        // For payment page, use organization's branding if debt is provided
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Make a Payment";
        ViewBag.DebtId = debtId;
        return View();
    }

    [HttpGet]
    public IActionResult ViewPlan(int debtId)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Payment Plan";
        ViewBag.DebtId = debtId;
        return View();
    }

    [HttpGet]
    public IActionResult ChangePlan(int debtId)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Change Payment Plan";
        ViewBag.DebtId = debtId;
        return View();
    }

    [HttpGet]
    public IActionResult Upcoming(int page = 1, int pageSize = 20)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Upcoming Payments";
        ViewBag.Page = page;
        ViewBag.PageSize = pageSize;
        return View();
    }

    [HttpGet]
    public IActionResult Success()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Payment Successful";
        return View();
    }

    [HttpGet]
    public IActionResult Failed()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Payment Failed";
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
}

public class PaymentsIndexVm
{
    public List<PaymentListItemVm> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}

public class PaymentListItemVm
{
    public Guid Id { get; set; }
    public DateTime ProcessedAtUtc { get; set; }
    public string DebtReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
    public TransactionStatus Status { get; set; }
}
