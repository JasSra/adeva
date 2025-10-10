using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DebtManager.Contracts.Payments;
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
    private readonly IPaymentPlanGenerationService _paymentPlanService;

    public AcceptController(
        AppDbContext db, 
        ILogger<AcceptController> logger,
        IPaymentPlanGenerationService paymentPlanService)
    {
        _db = db;
        _logger = logger;
        _paymentPlanService = paymentPlanService;
    }

    // Fallback without id - show reference input form
    [HttpGet]
    public IActionResult Index()
    {
        var theme = HttpContext.Items[BrandingResolverMiddleware.ThemeItemKey] as BrandingTheme;
        ViewBag.ThemeName = theme?.Name ?? "Default";
        ViewBag.Title = "Accept Debt";
        ViewBag.ShowReferenceInput = true;
        
        return View(new AcceptDebtVm());
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

        // Generate payment plan options using the payment plan service
        var paymentPlanOptions = await _paymentPlanService.GeneratePaymentPlanOptionsAsync(debt, ct);

        var vm = new AcceptDebtVm
        {
            DebtId = debt.Id,
            Reference = string.IsNullOrWhiteSpace(debt.ClientReferenceNumber) ? ("D-" + debt.Id.ToString().Substring(0, 8)) : debt.ClientReferenceNumber!,
            Outstanding = debt.OutstandingPrincipal,
            OriginalAmount = debt.OriginalPrincipal,
            DueDateUtc = debt.DueDateUtc,
            Status = debt.Status.ToString(),
            OrganizationName = debt.Organization?.TradingName ?? debt.Organization?.Name ?? (theme?.Name ?? "Organization"),
            PaymentPlanOptions = paymentPlanOptions.ToList()
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
            // Reload payment plan options for display
            var debt0 = await _db.Debts.Include(d => d.Organization).FirstOrDefaultAsync(d => d.Id == vm.DebtId, ct);
            if (debt0 == null) return NotFound();
            
            var options0 = await _paymentPlanService.GeneratePaymentPlanOptionsAsync(debt0, ct);
            var reVm = new AcceptDebtVm
            {
                DebtId = debt0.Id,
                Reference = string.IsNullOrWhiteSpace(debt0.ClientReferenceNumber) ? ("D-" + debt0.Id.ToString().Substring(0, 8)) : debt0.ClientReferenceNumber!,
                Outstanding = debt0.OutstandingPrincipal,
                OriginalAmount = debt0.OriginalPrincipal,
                DueDateUtc = debt0.DueDateUtc,
                Status = debt0.Status.ToString(),
                OrganizationName = debt0.Organization?.TradingName ?? debt0.Organization?.Name ?? (theme?.Name ?? "Organization"),
                PaymentPlanOptions = options0.ToList()
            };
            return View(reVm);
        }

        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null)
        {
            return Redirect($"/User/Onboarding?returnUrl=/User/Accept/{vm.DebtId}");
        }

        var debt = await _db.Debts
            .Include(d => d.PaymentPlans)
            .Include(d => d.Organization)
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

        // Handle dispute option separately
        if (vm.IsDispute)
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

        // Get the selected payment plan option from the submitted index
        var paymentPlanOptions = await _paymentPlanService.GeneratePaymentPlanOptionsAsync(debt, ct);
        if (vm.SelectedPlanIndex < 0 || vm.SelectedPlanIndex >= paymentPlanOptions.Count)
        {
            ModelState.AddModelError("", "Invalid payment plan selection");
            var reVm = new AcceptDebtVm
            {
                DebtId = debt.Id,
                Reference = string.IsNullOrWhiteSpace(debt.ClientReferenceNumber) ? ("D-" + debt.Id.ToString().Substring(0, 8)) : debt.ClientReferenceNumber!,
                Outstanding = debt.OutstandingPrincipal,
                OriginalAmount = debt.OriginalPrincipal,
                DueDateUtc = debt.DueDateUtc,
                Status = debt.Status.ToString(),
                OrganizationName = debt.Organization?.TradingName ?? debt.Organization?.Name ?? (theme?.Name ?? "Organization"),
                PaymentPlanOptions = paymentPlanOptions.ToList()
            };
            return View(reVm);
        }

        var selectedOption = paymentPlanOptions[vm.SelectedPlanIndex];
        
        // Create payment plan from the selected option using the service
        var plan = await _paymentPlanService.CreatePaymentPlanFromOptionAsync(debt, selectedOption, userId, ct);
        
        _db.PaymentPlans.Add(plan);
        debt.AttachPaymentPlan(plan);
        plan.Activate(userId);
        debt.AppendNote($"Plan {plan.Reference} accepted by user {userId} (Type: {selectedOption.Type})");

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

        // Redirect based on payment plan type
        if (selectedOption.Type == PaymentPlanType.FullSettlement)
        {
            return Redirect($"/User/Payments/MakePayment?debtId={debt.Id}");
        }

        return Redirect("/User");
    }

    /// <summary>
    /// Find debt by reference number for the authenticated user
    /// </summary>
    [HttpGet("User/Accept/api/find-by-reference")]
    public async Task<IActionResult> FindByReference([FromQuery] string reference, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reference))
        {
            return BadRequest(new { error = "Reference is required" });
        }

        // Get current debtor
        var debtorId = await GetCurrentDebtorIdAsync(ct);
        if (debtorId == null)
        {
            return Unauthorized(new { error = "Please complete onboarding first" });
        }

        // Search by client reference number or generated ID (D-xxxxxxxx format)
        var debts = await _db.Debts
            .Include(d => d.Organization)
            .Where(d => d.DebtorId == debtorId.Value)
            .ToListAsync(ct);

        Debt? debt = null;
        
        // Try to match by ClientReferenceNumber first
        debt = debts.FirstOrDefault(d => 
            !string.IsNullOrWhiteSpace(d.ClientReferenceNumber) && 
            d.ClientReferenceNumber.Equals(reference, StringComparison.OrdinalIgnoreCase));

        // If not found, try to match by generated D-xxx reference
        if (debt == null)
        {
            debt = debts.FirstOrDefault(d => 
                ("D-" + d.Id.ToString().Substring(0, 8)).Equals(reference, StringComparison.OrdinalIgnoreCase));
        }

        if (debt == null)
        {
            return NotFound(new { error = "Debt not found with the provided reference" });
        }

        // Check if debt is acceptable
        if (debt.Status == DebtStatus.Settled || debt.Status == DebtStatus.Disputed)
        {
            return BadRequest(new { error = "This debt is not available for acceptance" });
        }

        return Ok(new
        {
            debtId = debt.Id,
            reference = string.IsNullOrWhiteSpace(debt.ClientReferenceNumber) 
                ? ("D-" + debt.Id.ToString().Substring(0, 8)) 
                : debt.ClientReferenceNumber,
            outstanding = debt.OutstandingPrincipal,
            organizationName = debt.Organization?.TradingName ?? debt.Organization?.Name ?? "Organization"
        });
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
    public List<PaymentPlanOption> PaymentPlanOptions { get; set; } = new();
}

public class AcceptDebtPostVm
{
    [Required]
    public Guid DebtId { get; set; }

    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Please select a payment plan option")]
    public int SelectedPlanIndex { get; set; } = -1;

    // For dispute option (special case, not a payment plan)
    public bool IsDispute { get; set; }
    
    [MaxLength(500)]
    public string? DisputeReason { get; set; }
}
