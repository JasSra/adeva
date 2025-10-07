using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DebtManager.Domain.Communications;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Services;
using HandlebarsDotNet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Areas.User.Controllers;

[Area("User")]
[Authorize(Policy = "RequireUserScope")]
public class AcceptController : Controller
{
    private readonly AppDbContext _db;
    private readonly ILogger<AcceptController> _logger;

    public AcceptController(AppDbContext db, ILogger<AcceptController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Fallback without id to avoid 404 when reached from sidebar
    [HttpGet]
    public IActionResult Index()
    {
        TempData["Error"] = "Open the acceptance link from your email to continue.";
        return Redirect("/User");
    }

    [HttpGet("User/Accept/{id}")]
    public async Task<IActionResult> Index(Guid id, CancellationToken ct)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Review & Accept Debt";

        // Ensure user has a debtor profile; if not, send them to onboarding and return here after
        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null)
        {
            return Redirect($"/User/Onboarding?returnUrl=/User/Accept/{id}");
        }

        var debt = await _db.Debts
            .Include(d => d.PaymentPlans)
            .Include(d => d.Organization)
            .FirstOrDefaultAsync(d => d.Id == id, ct);
        if (debt == null)
        {
            return NotFound();
        }

        // Basic validations: can't accept settled or disputed debts
        if (debt.Status == DebtStatus.Settled || debt.Status == DebtStatus.Disputed)
        {
            TempData["Error"] = "This debt is not available for acceptance.";
            return Redirect("/User");
        }

        // Debt must belong to current debtor
        var debtorIdCurrent = debtorId.Value;
        if (debt.DebtorId != debtorIdCurrent)
        {
            TempData["Error"] = "This debt is associated with a different account.";
            return Redirect("/User");
        }

        // Compute full-payment discounted suggestion
        var cfg = await _db.OrganizationFeeConfigurations.FirstOrDefaultAsync(c => c.OrganizationId == debt.OrganizationId, ct);
        decimal? fullDiscountPct = cfg?.FullPaymentDiscountPercentage;
        decimal discounted = debt.OutstandingPrincipal;
        if (fullDiscountPct.HasValue && fullDiscountPct.Value > 0)
        {
            discounted = Math.Round(debt.OutstandingPrincipal * (1 - (fullDiscountPct.Value / 100m)), 2, MidpointRounding.AwayFromZero);
        }

        var vm = new AcceptDebtVm
        {
            DebtId = debt.Id,
            Reference = string.IsNullOrWhiteSpace(debt.ClientReferenceNumber) ? ("D-" + debt.Id.ToString().Substring(0, 8)) : debt.ClientReferenceNumber!,
            Outstanding = debt.OutstandingPrincipal,
            OriginalAmount = debt.OriginalPrincipal,
            DueDateUtc = debt.DueDateUtc,
            Status = debt.Status.ToString(),
            OrganizationName = debt.Organization?.TradingName ?? debt.Organization?.Name ?? (theme?.Name ?? "Organization"),
            FullPaymentSuggested = discounted,
            FullPaymentDiscountPercent = fullDiscountPct ?? 0
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(AcceptDebtPostVm vm, CancellationToken ct)
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Review & Accept Debt";

        if (!ModelState.IsValid)
        {
            // reload summary to render again
            var debt0 = await _db.Debts.Include(d => d.Organization).FirstOrDefaultAsync(d => d.Id == vm.DebtId, ct);
            if (debt0 == null) return NotFound();
            var cfg0 = await _db.OrganizationFeeConfigurations.FirstOrDefaultAsync(c => c.OrganizationId == debt0.OrganizationId, ct);
            var discPct0 = cfg0?.FullPaymentDiscountPercentage ?? 0m;
            var discAmt0 = discPct0 > 0 ? Math.Round(debt0.OutstandingPrincipal * (1 - (discPct0 / 100m)), 2) : debt0.OutstandingPrincipal;
            var reVm = new AcceptDebtVm
            {
                DebtId = debt0.Id,
                Reference = string.IsNullOrWhiteSpace(debt0.ClientReferenceNumber) ? ("D-" + debt0.Id.ToString().Substring(0, 8)) : debt0.ClientReferenceNumber!,
                Outstanding = debt0.OutstandingPrincipal,
                OriginalAmount = debt0.OriginalPrincipal,
                DueDateUtc = debt0.DueDateUtc,
                Status = debt0.Status.ToString(),
                OrganizationName = debt0.Organization?.TradingName ?? debt0.Organization?.Name ?? (theme?.Name ?? "Organization"),
                FullPaymentSuggested = discAmt0,
                FullPaymentDiscountPercent = discPct0
            };
            return View(reVm);
        }

        // Additional validation for installments option
        if (vm.SelectedOption == AcceptOption.Installments)
        {
            if (!vm.Frequency.HasValue)
            {
                ModelState.AddModelError(nameof(vm.Frequency), "Please select a payment frequency");
            }
            if (!vm.InstallmentCount.HasValue || vm.InstallmentCount.Value < 2 || vm.InstallmentCount.Value > 48)
            {
                ModelState.AddModelError(nameof(vm.InstallmentCount), "Please enter a valid number of installments (2-48)");
            }
            
            if (!ModelState.IsValid)
            {
                // reload summary to render again with errors
                var debt0 = await _db.Debts.Include(d => d.Organization).FirstOrDefaultAsync(d => d.Id == vm.DebtId, ct);
                if (debt0 == null) return NotFound();
                var cfg0 = await _db.OrganizationFeeConfigurations.FirstOrDefaultAsync(c => c.OrganizationId == debt0.OrganizationId, ct);
                var discPct0 = cfg0?.FullPaymentDiscountPercentage ?? 0m;
                var discAmt0 = discPct0 > 0 ? Math.Round(debt0.OutstandingPrincipal * (1 - (discPct0 / 100m)), 2) : debt0.OutstandingPrincipal;
                var reVm = new AcceptDebtVm
                {
                    DebtId = debt0.Id,
                    Reference = string.IsNullOrWhiteSpace(debt0.ClientReferenceNumber) ? ("D-" + debt0.Id.ToString().Substring(0, 8)) : debt0.ClientReferenceNumber!,
                    Outstanding = debt0.OutstandingPrincipal,
                    OriginalAmount = debt0.OriginalPrincipal,
                    DueDateUtc = debt0.DueDateUtc,
                    Status = debt0.Status.ToString(),
                    OrganizationName = debt0.Organization?.TradingName ?? debt0.Organization?.Name ?? (theme?.Name ?? "Organization"),
                    FullPaymentSuggested = discAmt0,
                    FullPaymentDiscountPercent = discPct0
                };
                return View(reVm);
            }
        }

        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null)
        {
            return Redirect($"/User/Onboarding?returnUrl=/User/Accept/{vm.DebtId}");
        }

        var debt = await _db.Debts
            .Include(d => d.PaymentPlans)
            .FirstOrDefaultAsync(d => d.Id == vm.DebtId, ct);
        if (debt == null) return NotFound();

        if (debt.Status == DebtStatus.Settled || debt.Status == DebtStatus.Disputed)
        {
            TempData["Error"] = "This debt is not available for acceptance.";
            return Redirect("/User");
        }

        if (debt.DebtorId != debtorId.Value)
        {
            TempData["Error"] = "This debt is associated with a different account.";
            return Redirect("/User");
        }

        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "user";

        if (vm.SelectedOption == AcceptOption.Dispute)
        {
            if (!string.IsNullOrWhiteSpace(vm.DisputeReason))
            {
                debt.FlagDispute(vm.DisputeReason);
                debt.AppendNote($"Dispute initiated: {vm.DisputeReason}");
            }
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Debt {DebtId} dispute initiated by user {UserId}", debt.Id, userId);
            return Redirect($"/User/Debts/Dispute/{debt.Id}");
        }

        // Build a payment plan based on selection
        PaymentPlan plan;
        var startDate = DateTime.UtcNow.Date.AddDays(1);
        if (vm.SelectedOption == AcceptOption.PayInFull)
        {
            // Apply org-configured discount if present
            var cfg = await _db.OrganizationFeeConfigurations.FirstOrDefaultAsync(c => c.OrganizationId == debt.OrganizationId, ct);
            var pct = cfg?.FullPaymentDiscountPercentage ?? 0m;
            var discounted = pct > 0 ? Math.Round(debt.OutstandingPrincipal * (1 - (pct / 100m)), 2, MidpointRounding.AwayFromZero) : debt.OutstandingPrincipal;

            plan = new PaymentPlan(
                debt.Id,
                reference: $"PLAN-{Guid.NewGuid():N}".Substring(0, 12),
                type: PaymentPlanType.FullSettlement,
                frequency: PaymentFrequency.OneOff,
                startDateUtc: startDate,
                installmentAmount: discounted,
                installmentCount: 1);
            if (pct > 0)
            {
                plan.ApplyDiscount(debt.OutstandingPrincipal - discounted);
            }
        }
        else // Installments
        {
            var count = Math.Clamp(vm.InstallmentCount ?? 6, 2, 48);
            var freq = vm.Frequency ?? PaymentFrequency.Monthly;
            var perInstallment = Math.Round(Math.Max(1, debt.OutstandingPrincipal / count), 2, MidpointRounding.AwayFromZero);
            plan = new PaymentPlan(
                debt.Id,
                reference: $"PLAN-{Guid.NewGuid():N}".Substring(0, 12),
                type: PaymentPlanType.SystemGenerated,
                frequency: freq,
                startDateUtc: startDate,
                installmentAmount: perInstallment,
                installmentCount: count);
        }

        plan.SetCreatedBy(userId);
        _db.PaymentPlans.Add(plan);
        debt.AttachPaymentPlan(plan);
        plan.Activate(userId);
        debt.AppendNote($"Plan {plan.Reference} accepted by user {userId} ({vm.SelectedOption})");

        // Prepare messaging content with templates
        var debtor = await _db.Debtors.FirstOrDefaultAsync(d => d.Id == debt.DebtorId, ct);
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == debt.OrganizationId, ct);
        var debtRef = string.IsNullOrWhiteSpace(debt.ClientReferenceNumber) ? ("D-" + debt.Id.ToString().Substring(0, 8)) : debt.ClientReferenceNumber;

        var templateData = new Dictionary<string, object?>
        {
            ["DebtorFirstName"] = debtor?.FirstName,
            ["DebtorLastName"] = debtor?.LastName,
            ["DebtorEmail"] = debtor?.Email,
            ["OrganizationName"] = org?.TradingName ?? org?.Name,
            ["DebtReference"] = debtRef,
            ["Currency"] = debt.Currency,
            ["OutstandingAmount"] = debt.OutstandingPrincipal,
            ["PlanReference"] = plan.Reference,
            ["Frequency"] = plan.Frequency.ToString(),
            ["InstallmentAmount"] = plan.InstallmentAmount,
            ["InstallmentCount"] = plan.InstallmentCount,
            ["StartDate"] = plan.StartDateUtc.ToLocalTime().ToString("MMM d, yyyy"),
        };

        // Debtor email
        if (!string.IsNullOrWhiteSpace(debtor?.Email))
        {
            var (subj, body) = await RenderTemplateAsync("debtor-plan-accepted", templateData, ct)
                ?? ($"Your payment plan is active - {debtRef}",
                    $"Hello {debtor?.FirstName},<br/>Your payment plan {plan.Reference} has been set up. First payment due {plan.StartDateUtc.ToLocalTime():MMM d, yyyy}. Amount {debt.Currency} {plan.InstallmentAmount:F2} x {plan.InstallmentCount}.");
            var qm = new QueuedMessage(debtor!.Email, subj, body, MessageChannel.Email, relatedEntityType: nameof(PaymentPlan), relatedEntityId: plan.Id);
            _db.QueuedMessages.Add(qm);
            debt.AppendNote($"Queued email to debtor {debtor.Email} for plan {plan.Reference}");
        }

        // Organization email + internal message
        var orgEmail = org?.SupportEmail;
        if (!string.IsNullOrWhiteSpace(orgEmail))
        {
            var (subj, body) = await RenderTemplateAsync("org-plan-accepted", templateData, ct)
                ?? ($"Debtor accepted a payment plan - {debtRef}",
                    $"Debtor {debtor?.FirstName} {debtor?.LastName} accepted plan {plan.Reference}. Installments: {plan.InstallmentCount} {plan.Frequency}, amount {debt.Currency} {plan.InstallmentAmount:F2}.");
            var qm = new QueuedMessage(orgEmail!, subj, body, MessageChannel.Email, relatedEntityType: nameof(PaymentPlan), relatedEntityId: plan.Id);
            _db.QueuedMessages.Add(qm);
            debt.AppendNote($"Queued org notification email to {orgEmail}");
        }

        // Internal message to admins
        var admins = await _db.AdminUsers.Where(a => a.IsActive).ToListAsync(ct);
        if (admins.Count > 0)
        {
            var title = $"Payment plan accepted - {debtRef}";
            var content = $"Debtor {debtor?.FirstName} {debtor?.LastName} accepted plan {plan.Reference}. {plan.InstallmentCount} {plan.Frequency} installment(s) of {debt.Currency} {plan.InstallmentAmount:F2}.";
            var im = new InternalMessage(title, content, MessagePriority.High, category: "Payments", senderId: null, isSystemGenerated: true);
            im.SetRelatedEntity(nameof(PaymentPlan), plan.Id);
            foreach (var a in admins)
            {
                im.AddRecipient(a.Id);
            }
            _db.InternalMessages.Add(im);
            debt.AppendNote("Internal admin notification created");
        }

        await _db.SaveChangesAsync(ct);

        if (vm.SelectedOption == AcceptOption.PayInFull)
        {
            return Redirect($"/User/Payments/MakePayment?debtId={debt.Id}");
        }

        return Redirect("/User");
    }

    private async Task<(string subject, string body)?> RenderTemplateAsync(string templateCode, IDictionary<string, object?> data, CancellationToken ct)
    {
        var tmpl = await _db.MessageTemplates.FirstOrDefaultAsync(t => t.Code == templateCode && t.IsActive, ct);
        if (tmpl == null)
        {
            _logger.LogWarning("Message template {Code} not found or inactive", templateCode);
            return null;
        }
        try
        {
            var compiledBody = Handlebars.Compile(tmpl.BodyTemplate);
            var compiledSubject = Handlebars.Compile(string.IsNullOrWhiteSpace(tmpl.Subject) ? "" : tmpl.Subject);
            var body = compiledBody(data);
            var subject = compiledSubject(data);
            return (string.IsNullOrWhiteSpace(subject) ? tmpl.Name : subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to render template {Code}", templateCode);
            return null;
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

public class AcceptDebtVm
{
    public Guid DebtId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal Outstanding { get; set; }
    public DateTime? DueDateUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public decimal FullPaymentSuggested { get; set; }
    public decimal FullPaymentDiscountPercent { get; set; }
}

public class AcceptDebtPostVm
{
    [Required]
    public Guid DebtId { get; set; }

    [Required]
    public AcceptOption SelectedOption { get; set; }

    // For installment option
    public PaymentFrequency? Frequency { get; set; }
    [Range(2, 48)]
    public int? InstallmentCount { get; set; }

    // For dispute option
    [MaxLength(500)]
    public string? DisputeReason { get; set; }
}

public enum AcceptOption
{
    PayInFull = 1,
    Installments = 2,
    Dispute = 3
}
