using System.ComponentModel.DataAnnotations;
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
public class ReceiptsController : Controller
{
    private readonly AppDbContext _db;

    public ReceiptsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, DateTime? from = null, DateTime? to = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Receipts";
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
            .Where(t => t.DebtorId == debtorId && t.Status == TransactionStatus.Succeeded);

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

        query = query.OrderByDescending(t => t.ProcessedAtUtc);

        var total = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new ReceiptListItemVm
            {
                Id = t.Id,
                ReceiptNumber = "R-" + t.Id.ToString().Substring(0, 8),
                ProcessedAtUtc = t.ProcessedAtUtc,
                DebtReference = t.Debt != null && !string.IsNullOrEmpty(t.Debt.ClientReferenceNumber)
                    ? t.Debt.ClientReferenceNumber!
                    : ("D-" + t.DebtId.ToString().Substring(0, 8)),
                Amount = t.Amount,
                Currency = t.Currency,
                Method = t.Method
            })
            .ToListAsync(ct);

        var model = new ReceiptsIndexVm
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            Total = total
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> View(Guid id, CancellationToken ct)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Receipt Details";

        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null) return Redirect("/User/Onboarding");

        var tx = await _db.Transactions
            .Include(t => t.Debt)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id && t.DebtorId == debtorId, ct);
        if (tx == null) return NotFound();

        var vm = new ReceiptDetailVm
        {
            Id = tx.Id,
            ReceiptNumber = "R-" + tx.Id.ToString().Substring(0, 8),
            ProcessedAtUtc = tx.ProcessedAtUtc,
            Amount = tx.Amount,
            Currency = tx.Currency,
            Method = tx.Method.ToString(),
            Status = tx.Status.ToString(),
            DebtReference = tx.Debt != null && !string.IsNullOrEmpty(tx.Debt.ClientReferenceNumber)
                ? tx.Debt.ClientReferenceNumber!
                : ("D-" + tx.DebtId.ToString().Substring(0, 8)),
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null) return Redirect("/User/Onboarding");

        var tx = await _db.Transactions.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id && t.DebtorId == debtorId, ct);
        if (tx == null) return NotFound();

        // If receipts are stored as generated documents, look up by related id/tagging (not implemented here).
        // For now, return 404 or a simple generated text file as a placeholder.
        var bytes = System.Text.Encoding.UTF8.GetBytes($"Receipt {id}\nAmount: {tx.Currency} {tx.Amount:N2}\nDate: {tx.ProcessedAtUtc:u}\n");
        return File(bytes, "text/plain", $"receipt-{id}.txt");
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

public class ReceiptsIndexVm
{
    public List<ReceiptListItemVm> Items { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int Total { get; set; }
}

public class ReceiptListItemVm
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
    public string DebtReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentMethod Method { get; set; }
}

public class ReceiptDetailVm
{
    public Guid Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
    public string DebtReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
