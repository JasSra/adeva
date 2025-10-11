using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Audit;
using DebtManager.Domain.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DebtManager.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "RequireAdminScope")]
[ApiController]
[Route("api/admin/payments")]
public class AdminPaymentApiController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly IDebtorRepository _debtorRepository;
    private readonly IReceiptService _receiptService;
    private readonly IAuditService _auditService;
    private readonly ILogger<AdminPaymentApiController> _logger;

    public AdminPaymentApiController(
        ITransactionRepository transactionRepository,
        IDebtRepository debtRepository,
        IDebtorRepository debtorRepository,
        IReceiptService receiptService,
        IAuditService auditService,
        ILogger<AdminPaymentApiController> logger)
    {
        _transactionRepository = transactionRepository;
        _debtRepository = debtRepository;
        _debtorRepository = debtorRepository;
        _receiptService = receiptService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Creates an adhoc manual payment entry
    /// </summary>
    [HttpPost("adhoc")]
    public async Task<IActionResult> CreateAdhocPayment([FromBody] CreateAdhocPaymentRequest request)
    {
        if (request.DebtId == Guid.Empty)
        {
            return BadRequest(new { error = "Invalid debt ID" });
        }

        if (request.Amount <= 0)
        {
            return BadRequest(new { error = "Amount must be greater than zero" });
        }

        try
        {
            var debt = await _debtRepository.GetWithDetailsAsync(request.DebtId);
            if (debt == null)
            {
                return NotFound(new { error = "Debt not found" });
            }

            // Create manual transaction
            var transaction = new Transaction(
                debtId: request.DebtId,
                debtorId: debt.DebtorId,
                paymentPlanId: null,
                paymentInstallmentId: null,
                amount: request.Amount,
                currency: request.Currency ?? "AUD",
                direction: TransactionDirection.Inbound,
                method: ParsePaymentMethod(request.Method),
                provider: "Manual",
                providerRef: $"MANUAL-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
            );

            transaction.MarkSettled(DateTime.UtcNow, transaction.ProviderRef);

            if (!string.IsNullOrWhiteSpace(request.Notes))
            {
                transaction.AttachMetadata($"{{\"notes\":\"{request.Notes}\"}}");
            }

            await _transactionRepository.AddAsync(transaction);

            // Update debt
            debt.ApplyPayment(request.Amount, DateTime.UtcNow);
            
            if (debt.OutstandingPrincipal <= 0)
            {
                debt.SetStatus(Domain.Debts.DebtStatus.Settled, "Fully paid via manual entry");
            }

            await _transactionRepository.SaveChangesAsync();

            await _auditService.LogAsync(
                "ADHOC_PAYMENT_CREATED",
                "Transaction",
                transaction.Id.ToString(),
                $"Manual payment created: {request.Amount:C} {request.Currency} for debt {request.DebtId}. Method: {request.Method}. Notes: {request.Notes}");

            _logger.LogInformation(
                "Adhoc payment created: Transaction {TransactionId}, Debt {DebtId}, Amount {Amount}",
                transaction.Id, request.DebtId, request.Amount);

            return Ok(new
            {
                transactionId = transaction.Id,
                debtId = transaction.DebtId,
                amount = transaction.Amount,
                currency = transaction.Currency,
                status = transaction.Status.ToString(),
                processedAt = transaction.ProcessedAtUtc
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating adhoc payment for debt {DebtId}", request.DebtId);
            return StatusCode(500, new { error = "Failed to create adhoc payment" });
        }
    }

    /// <summary>
    /// Gets failed transactions eligible for retry
    /// </summary>
    [HttpGet("failed")]
    public async Task<IActionResult> GetFailedPayments([FromQuery] int days = 30)
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            var failedTransactions = await _transactionRepository.GetFailedTransactionsAsync(fromDate);

            var result = failedTransactions.Select(t => new
            {
                id = t.Id,
                debtId = t.DebtId,
                amount = t.Amount,
                currency = t.Currency,
                method = t.Method.ToString(),
                failureReason = t.FailureReason,
                processedAt = t.ProcessedAtUtc,
                providerRef = t.ProviderRef
            });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving failed payments");
            return StatusCode(500, new { error = "Failed to retrieve failed payments" });
        }
    }

    /// <summary>
    /// Downloads a receipt for a transaction
    /// </summary>
    [HttpGet("{transactionId}/receipt")]
    public async Task<IActionResult> DownloadReceipt(Guid transactionId)
    {
        try
        {
            var transaction = await _transactionRepository.GetAsync(transactionId);
            if (transaction == null)
            {
                return NotFound(new { error = "Transaction not found" });
            }

            if (transaction.Status != TransactionStatus.Succeeded)
            {
                return BadRequest(new { error = "Receipt only available for successful transactions" });
            }

            var receipt = await _receiptService.GetReceiptAsync(transactionId);
            if (receipt == null)
            {
                return NotFound(new { error = "Receipt not found" });
            }

            await _auditService.LogAsync(
                "RECEIPT_DOWNLOADED",
                "Transaction",
                transactionId.ToString(),
                $"Receipt downloaded for transaction {transactionId}");

            // Return as HTML for now (can be converted to PDF in production)
            return File(receipt, "text/html", $"receipt-{transactionId}.html");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading receipt for transaction {TransactionId}", transactionId);
            return StatusCode(500, new { error = "Failed to download receipt" });
        }
    }

    private PaymentMethod ParsePaymentMethod(string? method)
    {
        return method?.ToLower() switch
        {
            "card" => PaymentMethod.Card,
            "bank" or "banktransfer" => PaymentMethod.BankTransfer,
            "directdebit" => PaymentMethod.DirectDebit,
            "cash" => PaymentMethod.Cash,
            "cheque" or "check" => PaymentMethod.Cheque,
            _ => PaymentMethod.ManualAdjustment
        };
    }
}

public class CreateAdhocPaymentRequest
{
    public Guid DebtId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string? Method { get; set; }
    public string? Notes { get; set; }
}
