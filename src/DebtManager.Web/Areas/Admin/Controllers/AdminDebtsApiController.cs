using DebtManager.Contracts.Audit;
using DebtManager.Contracts.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
[ApiController]
[Route("api/admin/debts")] 
public class AdminDebtsApiController : ControllerBase
{
    private readonly IDebtRepository _debtRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminDebtsApiController> _logger;

    public AdminDebtsApiController(IDebtRepository debtRepository, IAuditService auditService, ILogger<AdminDebtsApiController> logger)
    {
        _debtRepository = debtRepository;
        _auditService = auditService;
        _logger = logger;
    }

    [HttpGet("{id:guid}/summary")]
    public async Task<IActionResult> GetSummary([FromRoute] Guid id)
    {
        if (id == Guid.Empty) return BadRequest(new { error = "Invalid debt ID" });
        var debt = await _debtRepository.GetWithDetailsAsync(id);
        if (debt == null) return NotFound(new { error = "Debt not found" });
        return Ok(FullSummary(debt));
    }

    [HttpPost("add-fee")]
    public async Task<IActionResult> AddFee([FromBody] AddFeeRequest req)
    {
        if (req.DebtId == Guid.Empty) return BadRequest(new { error = "Invalid debt ID" });
        if (req.Amount <= 0) return BadRequest(new { error = "Amount must be greater than zero" });
        if (string.IsNullOrWhiteSpace(req.Reason)) return BadRequest(new { error = "Reason is required" });

        var debt = await _debtRepository.GetAsync(req.DebtId);
        if (debt == null) return NotFound(new { error = "Debt not found" });

        try
        {
            debt.AddFee(req.Amount, req.Reason, DateTime.UtcNow);
            await _debtRepository.SaveChangesAsync();

            await _auditService.LogAsync("DEBT_FEE_ADDED", "Debt", debt.Id.ToString(), $"Fee {debt.Currency} {req.Amount:F2} added. Reason: {req.Reason}");
            _logger.LogInformation("Fee added to debt {DebtId}", debt.Id);
            return Ok(Summary(debt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding fee to debt {DebtId}", req.DebtId);
            return StatusCode(500, new { error = "Failed to add fee" });
        }
    }

    [HttpPost("propose-settlement")]
    public async Task<IActionResult> ProposeSettlement([FromBody] ProposeSettlementRequest req)
    {
        if (req.DebtId == Guid.Empty) return BadRequest(new { error = "Invalid debt ID" });
        if (req.Amount <= 0) return BadRequest(new { error = "Amount must be greater than zero" });
        if (req.ExpiresAtUtc == null || req.ExpiresAtUtc <= DateTime.UtcNow) return BadRequest(new { error = "Expiry must be in the future (UTC)" });

        var debt = await _debtRepository.GetAsync(req.DebtId);
        if (debt == null) return NotFound(new { error = "Debt not found" });

        try
        {
            debt.ProposeSettlement(req.Amount, req.ExpiresAtUtc.Value);
            await _debtRepository.SaveChangesAsync();

            await _auditService.LogAsync("DEBT_SETTLEMENT_PROPOSED", "Debt", debt.Id.ToString(), $"Settlement proposed {debt.Currency} {req.Amount:F2} expiring {req.ExpiresAtUtc:u}");
            _logger.LogInformation("Settlement proposed for debt {DebtId}", debt.Id);
            return Ok(Summary(debt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proposing settlement for debt {DebtId}", req.DebtId);
            return StatusCode(500, new { error = "Failed to propose settlement" });
        }
    }

    [HttpPost("resolve-dispute")] 
    public async Task<IActionResult> ResolveDispute([FromBody] ResolveDisputeRequest req)
    {
        if (req.DebtId == Guid.Empty) return BadRequest(new { error = "Invalid debt ID" });
        var debt = await _debtRepository.GetAsync(req.DebtId);
        if (debt == null) return NotFound(new { error = "Debt not found" });

        try
        {
            debt.ResolveDispute();
            await _debtRepository.SaveChangesAsync();

            await _auditService.LogAsync("DEBT_DISPUTE_RESOLVED", "Debt", debt.Id.ToString(), "Dispute resolved");
            _logger.LogInformation("Dispute resolved for debt {DebtId}", debt.Id);
            return Ok(Summary(debt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving dispute for debt {DebtId}", req.DebtId);
            return StatusCode(500, new { error = "Failed to resolve dispute" });
        }
    }

    [HttpPost("flag-dispute")]
    public async Task<IActionResult> FlagDispute([FromBody] FlagDisputeRequest req)
    {
        if (req.DebtId == Guid.Empty) return BadRequest(new { error = "Invalid debt ID" });
        if (string.IsNullOrWhiteSpace(req.Reason)) return BadRequest(new { error = "Reason is required" });
        var debt = await _debtRepository.GetAsync(req.DebtId);
        if (debt == null) return NotFound(new { error = "Debt not found" });

        try
        {
            debt.FlagDispute(req.Reason);
            await _debtRepository.SaveChangesAsync();

            await _auditService.LogAsync("DEBT_DISPUTE_FLAGGED", "Debt", debt.Id.ToString(), $"Dispute reason: {req.Reason}");
            _logger.LogInformation("Dispute flagged for debt {DebtId}", debt.Id);
            return Ok(Summary(debt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging dispute for debt {DebtId}", req.DebtId);
            return StatusCode(500, new { error = "Failed to flag dispute" });
        }
    }

    [HttpPost("write-off")]
    public async Task<IActionResult> WriteOff([FromBody] WriteOffRequest req)
    {
        if (req.DebtId == Guid.Empty) return BadRequest(new { error = "Invalid debt ID" });
        if (string.IsNullOrWhiteSpace(req.Reason)) return BadRequest(new { error = "Reason is required" });
        var debt = await _debtRepository.GetAsync(req.DebtId);
        if (debt == null) return NotFound(new { error = "Debt not found" });

        try
        {
            debt.WriteOff(req.Reason);
            await _debtRepository.SaveChangesAsync();

            await _auditService.LogAsync("DEBT_WRITTEN_OFF", "Debt", debt.Id.ToString(), $"Write-off: {req.Reason}");
            _logger.LogInformation("Debt {DebtId} written off", debt.Id);
            return Ok(Summary(debt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error writing off debt {DebtId}", req.DebtId);
            return StatusCode(500, new { error = "Failed to write off debt" });
        }
    }

    [HttpPost("accept-settlement")]
    public async Task<IActionResult> AcceptSettlement([FromBody] ResolveDisputeRequest req)
    {
        if (req.DebtId == Guid.Empty) return BadRequest(new { error = "Invalid debt ID" });
        var debt = await _debtRepository.GetAsync(req.DebtId);
        if (debt == null) return NotFound(new { error = "Debt not found" });

        try
        {
            debt.AcceptSettlement();
            await _debtRepository.SaveChangesAsync();

            await _auditService.LogAsync("DEBT_SETTLEMENT_ACCEPTED", "Debt", debt.Id.ToString(), "Settlement accepted");
            _logger.LogInformation("Settlement accepted for debt {DebtId}", debt.Id);
            return Ok(Summary(debt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting settlement for debt {DebtId}", req.DebtId);
            return StatusCode(500, new { error = "Failed to accept settlement" });
        }
    }

    [HttpPost("reject-settlement")]
    public async Task<IActionResult> RejectSettlement([FromBody] FlagDisputeRequest req)
    {
        if (req.DebtId == Guid.Empty) return BadRequest(new { error = "Invalid debt ID" });
        var debt = await _debtRepository.GetAsync(req.DebtId);
        if (debt == null) return NotFound(new { error = "Debt not found" });

        try
        {
            debt.RejectSettlement(req.Reason);
            await _debtRepository.SaveChangesAsync();

            await _auditService.LogAsync("DEBT_SETTLEMENT_REJECTED", "Debt", debt.Id.ToString(), $"Settlement rejected: {req.Reason}");
            _logger.LogInformation("Settlement rejected for debt {DebtId}", debt.Id);
            return Ok(Summary(debt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting settlement for debt {DebtId}", req.DebtId);
            return StatusCode(500, new { error = "Failed to reject settlement" });
        }
    }

    private static object Summary(DebtManager.Domain.Debts.Debt d) => new
    {
        id = d.Id,
        status = d.Status.ToString(),
        currency = d.Currency,
        original = d.OriginalPrincipal,
        outstanding = d.OutstandingPrincipal,
        interest = d.AccruedInterest,
        fees = d.AccruedFees,
        settlement = new
        {
            amount = d.SettlementOfferAmount,
            expiresAtUtc = d.SettlementOfferExpiresAtUtc
        },
        notes = d.Notes,
        updatedAtUtc = d.UpdatedAtUtc
    };

    private static object FullSummary(DebtManager.Domain.Debts.Debt d) => new
    {
        id = d.Id,
        status = d.Status.ToString(),
        currency = d.Currency,
        original = d.OriginalPrincipal,
        outstanding = d.OutstandingPrincipal,
        interest = d.AccruedInterest,
        fees = d.AccruedFees,
        settlement = new
        {
            amount = d.SettlementOfferAmount,
            expiresAtUtc = d.SettlementOfferExpiresAtUtc
        },
        notes = d.Notes,
        dueDateUtc = d.DueDateUtc,
        openedAtUtc = d.OpenedAtUtc,
        lastPaymentAtUtc = d.LastPaymentAtUtc,
        nextActionAtUtc = d.NextActionAtUtc,
        updatedAtUtc = d.UpdatedAtUtc,
        paymentPlans = d.PaymentPlans?.Select(p => new {
            reference = p.Reference,
            type = p.Type.ToString(),
            status = p.Status.ToString(),
            totalPayable = p.TotalPayable,
            installmentCount = p.InstallmentCount
        }),
        transactions = d.Transactions?
            .OrderByDescending(t => t.ProcessedAtUtc)
            .Select(t => new {
                id = t.Id,
                processedAtUtc = t.ProcessedAtUtc,
                currency = t.Currency,
                amount = t.Amount,
                method = t.Method.ToString(),
                status = t.Status.ToString(),
                hasReceipt = t.Status == DebtManager.Domain.Payments.TransactionStatus.Succeeded
            })
    };

}

public record AddFeeRequest(Guid DebtId, decimal Amount, string Reason);
public record ProposeSettlementRequest(Guid DebtId, decimal Amount, DateTime? ExpiresAtUtc);
public record ResolveDisputeRequest(Guid DebtId);
public record FlagDisputeRequest(Guid DebtId, string Reason);
public record WriteOffRequest(Guid DebtId, string Reason);
