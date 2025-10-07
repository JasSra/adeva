using System.Security.Claims;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Payments;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Filters;
using DebtManager.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
[RequireDebtorOnboarded]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Title = "User Dashboard";
        try
        {
            var debtorId = await GetCurrentDebtorIdAsync(ct);
            if (debtorId == null)
            {
                return Redirect("/User/Onboarding");
            }

            var openStatuses = new[] { DebtStatus.Active, DebtStatus.InArrears, DebtStatus.Disputed };

            var totalOwed = await _db.Debts
                .Where(d => d.DebtorId == debtorId && openStatuses.Contains(d.Status))
                .Select(d => (decimal?)d.OutstandingPrincipal)
                .SumAsync(ct) ?? 0m;

            var activeDebts = await _db.Debts
                .Where(d => d.DebtorId == debtorId && openStatuses.Contains(d.Status))
                .CountAsync(ct);

            var accStatus = await _db.Debtors
                .Where(x => x.Id == debtorId)
                .Select(x => x.Status)
                .FirstOrDefaultAsync(ct);

            // Next payment due (first upcoming unpaid installment)
            var nextInstallment = await _db.PaymentInstallments
                .Include(pi => pi.PaymentPlan!)
                    .ThenInclude(pp => pp.Debt!)
                .Where(pi => pi.PaymentPlan!.Debt!.DebtorId == debtorId
                             && (pi.Status == PaymentInstallmentStatus.Scheduled || pi.Status == PaymentInstallmentStatus.Partial)
                             && (pi.AmountDue - pi.AmountPaid) > 0)
                .OrderBy(pi => pi.DueAtUtc)
                .Select(pi => new { pi.DueAtUtc, AmountRemaining = pi.AmountDue - pi.AmountPaid, DebtId = pi.PaymentPlan!.DebtId, DebtRef = pi.PaymentPlan!.Debt!.ClientReferenceNumber })
                .FirstOrDefaultAsync(ct);

            var nowUtc = DateTime.UtcNow;

            // Overdue installments count (unpaid and past due)
            var overdueCount = await _db.PaymentInstallments
                .Include(pi => pi.PaymentPlan!)
                    .ThenInclude(pp => pp.Debt!)
                .Where(pi => pi.PaymentPlan!.Debt!.DebtorId == debtorId
                             && (pi.AmountDue - pi.AmountPaid) > 0
                             && pi.DueAtUtc < nowUtc)
                .CountAsync(ct);

            // Upcoming top 3 installments
            var upcoming = await _db.PaymentInstallments
                .Include(pi => pi.PaymentPlan!)
                    .ThenInclude(pp => pp.Debt!)
                .Where(pi => pi.PaymentPlan!.Debt!.DebtorId == debtorId
                             && (pi.Status == PaymentInstallmentStatus.Scheduled || pi.Status == PaymentInstallmentStatus.Partial)
                             && (pi.AmountDue - pi.AmountPaid) > 0)
                .OrderBy(pi => pi.DueAtUtc)
                .Take(3)
                .Select(pi => new PaymentReminderVm
                {
                    DebtId = pi.PaymentPlan!.DebtId,
                    PaymentPlanId = pi.PaymentPlan!.Id,
                    DebtReference = string.IsNullOrEmpty(pi.PaymentPlan!.Debt!.ClientReferenceNumber)
                        ? ("D-" + pi.PaymentPlan!.Debt!.Id.ToString().Substring(0, 8))
                        : pi.PaymentPlan!.Debt!.ClientReferenceNumber,
                    DueAtUtc = pi.DueAtUtc,
                    AmountRemaining = pi.AmountDue - pi.AmountPaid,
                    Overdue = pi.DueAtUtc < nowUtc
                })
                .ToListAsync(ct);

            // Due soon count (within 7 days)
            var soonUtc = nowUtc.AddDays(7);
            var dueSoonCount = await _db.PaymentInstallments
                .Include(pi => pi.PaymentPlan!)
                    .ThenInclude(pp => pp.Debt!)
                .Where(pi => pi.PaymentPlan!.Debt!.DebtorId == debtorId
                             && (pi.AmountDue - pi.AmountPaid) > 0
                             && pi.DueAtUtc >= nowUtc && pi.DueAtUtc <= soonUtc)
                .CountAsync(ct);

            // Recent failed payments (last 30 days)
            var since = nowUtc.AddDays(-30);
            var failedPayments = await _db.Transactions
                .Include(t => t.Debt)
                .Where(t => t.DebtorId == debtorId && t.Status == TransactionStatus.Failed && t.ProcessedAtUtc >= since)
                .OrderByDescending(t => t.ProcessedAtUtc)
                .Take(3)
                .Select(t => new FailedPaymentVm
                {
                    TransactionId = t.Id,
                    DebtReference = t.Debt != null && !string.IsNullOrEmpty(t.Debt.ClientReferenceNumber)
                        ? t.Debt.ClientReferenceNumber!
                        : ("D-" + t.DebtId.ToString().Substring(0, 8)),
                    Amount = t.Amount,
                    Currency = t.Currency,
                    ProcessedAtUtc = t.ProcessedAtUtc
                })
                .ToListAsync(ct);

            // Unread internal messages
            int unreadMessages = 0;
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out var userGuid))
            {
                unreadMessages = await _db.InternalMessageRecipients
                    .Where(r => r.UserId == userGuid && r.Status == Domain.Communications.InternalMessageStatus.Unread)
                    .CountAsync(ct);
            }

            // Recent transactions (keep as activity feed)
            var recentTx = await _db.Transactions
                .Include(t => t.Debt)
                .Where(t => t.DebtorId == debtorId)
                .OrderByDescending(t => t.ProcessedAtUtc)
                .Take(5)
                .Select(t => new ActivityVm
                {
                    Kind = "payment",
                    Title = t.Status == TransactionStatus.Succeeded ? "Payment Received" : t.Status.ToString(),
                    Description = t.Debt != null && !string.IsNullOrEmpty(t.Debt.ClientReferenceNumber)
                        ? $"Debt #{t.Debt.ClientReferenceNumber} - {t.Currency} {t.Amount:F2}"
                        : $"{t.Currency} {t.Amount:F2}",
                    TimestampUtc = t.ProcessedAtUtc
                })
                .ToListAsync(ct);

            // Compute account health
            var hasOverdue = overdueCount > 0;
            var isZeroOwed = totalOwed <= 0m;
            var hasFailed = failedPayments.Count > 0;
            var dueSoon = nextInstallment != null && nextInstallment.DueAtUtc <= nowUtc.AddDays(5);

            string accountHealth;
            string healthTone; // success | warning | danger
            string healthMessage;
            string? primaryCtaUrl = null;
            string? primaryCtaText = null;

            if (isZeroOwed && !hasOverdue && !hasFailed)
            {
                accountHealth = "All Clear";
                healthTone = "success";
                healthMessage = "You have no outstanding balance.";
                primaryCtaUrl = "/User/Debts";
                primaryCtaText = "View My Debts";
            }
            else if (hasOverdue)
            {
                accountHealth = "Overdue";
                healthTone = "danger";
                healthMessage = $"{overdueCount} installment(s) overdue.";
                primaryCtaUrl = "/User/Payments";
                primaryCtaText = "Resolve Now";
            }
            else if (dueSoon || hasFailed)
            {
                accountHealth = "Action Needed";
                healthTone = "warning";
                if (nextInstallment != null)
                {
                    healthMessage = $"Next payment {nextInstallment.AmountRemaining:C} due {nextInstallment.DueAtUtc.ToLocalTime():MMM d}";
                    if (nextInstallment.DebtId != Guid.Empty)
                    {
                        primaryCtaUrl = $"/User/Payments/MakePayment?debtId={nextInstallment.DebtId}";
                        primaryCtaText = "Pay Now";
                    }
                }
                else if (hasFailed)
                {
                    healthMessage = "A recent payment failed.";
                    primaryCtaUrl = "/User/Payments";
                    primaryCtaText = "Retry Payment";
                }
                else
                {
                    healthMessage = "Upcoming payment due soon.";
                    primaryCtaUrl = "/User/Payments";
                    primaryCtaText = "Make a Payment";
                }
            }
            else
            {
                accountHealth = "Good";
                healthTone = "success";
                healthMessage = "You're on track.";
                primaryCtaUrl = "/User/Payments";
                primaryCtaText = "Make a Payment";
            }

            // Sticky pay bar decision
            string? stickyUrl = null;
            string? stickyText = null;
            if (hasOverdue)
            {
                stickyUrl = "/User/Payments";
                stickyText = $"{overdueCount} overdue - resolve now";
            }
            else if (nextInstallment != null)
            {
                stickyUrl = nextInstallment.DebtId != Guid.Empty ? $"/User/Payments/MakePayment?debtId={nextInstallment.DebtId}" : "/User/Payments";
                stickyText = $"Pay {nextInstallment.AmountRemaining:C} by {nextInstallment.DueAtUtc.ToLocalTime():MMM d}";
            }

            // Build approximate outstanding sparkline for last 14 days based on payments
            var startDate = nowUtc.Date.AddDays(-13);
            var endDate = nowUtc.Date;
            var paymentsByDay = await _db.Transactions
                .Where(t => t.DebtorId == debtorId && t.Status == TransactionStatus.Succeeded && t.Direction == TransactionDirection.Inbound && t.ProcessedAtUtc.Date >= startDate && t.ProcessedAtUtc.Date <= endDate)
                .GroupBy(t => t.ProcessedAtUtc.Date)
                .Select(g => new { Day = g.Key, Amount = g.Sum(x => x.Amount) })
                .ToListAsync(ct);
            var map = paymentsByDay.ToDictionary(x => x.Day, x => x.Amount);
            var totalPayments = paymentsByDay.Sum(x => x.Amount);
            var spark = new List<decimal>();
            decimal paidToDate = 0m;
            for (var d = startDate; d <= endDate; d = d.AddDays(1))
            {
                var outstanding = totalOwed + (totalPayments - paidToDate);
                spark.Add(Math.Max(0, outstanding));
                if (map.TryGetValue(d, out var amt))
                {
                    paidToDate += amt;
                }
            }

            var vm = new DashboardVm
            {
                TotalOwed = Math.Max(0, totalOwed),
                NextPaymentAmount = nextInstallment?.AmountRemaining,
                NextPaymentDueDateUtc = nextInstallment?.DueAtUtc,
                NextPaymentDebtId = nextInstallment?.DebtId,
                NextPaymentDebtReference = string.IsNullOrEmpty(nextInstallment?.DebtRef) ? null : nextInstallment!.DebtRef,
                ActiveDebts = activeDebts,
                AccountStatus = accStatus.ToString(),
                OverdueInstallments = overdueCount,
                UnreadMessages = unreadMessages,
                Upcoming = upcoming,
                FailedPayments = failedPayments,
                Activities = recentTx,
                AccountHealth = accountHealth,
                HealthTone = healthTone,
                HealthMessage = healthMessage,
                PrimaryCtaUrl = primaryCtaUrl,
                PrimaryCtaText = primaryCtaText,
                DueSoonCount = dueSoonCount,
                StickyPayUrl = stickyUrl,
                StickyPayText = stickyText,
                OutstandingSparkline = spark
            };

            return View(vm);
        }
        catch (Exception)
        {
            TempData["Error"] = "We couldn't load your dashboard right now. Please try again shortly.";
            // Return with safe defaults
            return View(new DashboardVm());
        }
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

public class DashboardVm
{
    public decimal TotalOwed { get; set; }
    public decimal? NextPaymentAmount { get; set; }
    public DateTime? NextPaymentDueDateUtc { get; set; }
    public Guid? NextPaymentDebtId { get; set; }
    public string? NextPaymentDebtReference { get; set; }
    public int ActiveDebts { get; set; }
    public string AccountStatus { get; set; } = "Unknown";
    public int OverdueInstallments { get; set; }
    public int UnreadMessages { get; set; }
    public List<PaymentReminderVm> Upcoming { get; set; } = new();
    public List<FailedPaymentVm> FailedPayments { get; set; } = new();
    public List<ActivityVm> Activities { get; set; } = new();
    // Health banner
    public string AccountHealth { get; set; } = "";
    public string HealthTone { get; set; } = "info"; // success | warning | danger | info
    public string HealthMessage { get; set; } = string.Empty;
    public string? PrimaryCtaUrl { get; set; }
    public string? PrimaryCtaText { get; set; }
    // Notifications
    public int DueSoonCount { get; set; }
    // Sticky mobile pay bar
    public string? StickyPayUrl { get; set; }
    public string? StickyPayText { get; set; }
    // Sparkline values (oldest -> newest)
    public List<decimal> OutstandingSparkline { get; set; } = new();
}

public class ActivityVm
{
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
}

public class PaymentReminderVm
{
    public Guid DebtId { get; set; }
    public Guid PaymentPlanId { get; set; }
    public string DebtReference { get; set; } = string.Empty;
    public DateTime DueAtUtc { get; set; }
    public decimal AmountRemaining { get; set; }
    public bool Overdue { get; set; }
}

public class FailedPaymentVm
{
    public Guid TransactionId { get; set; }
    public string DebtReference { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
