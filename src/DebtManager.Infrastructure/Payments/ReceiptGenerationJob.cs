using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Audit;
using Microsoft.Extensions.Logging;

namespace DebtManager.Infrastructure.Payments;

/// <summary>
/// Background job for generating payment receipts
/// </summary>
public class ReceiptGenerationJob
{
    private readonly IReceiptService _receiptService;
    private readonly IAuditService _auditService;
    private readonly ILogger<ReceiptGenerationJob> _logger;

    public ReceiptGenerationJob(
        IReceiptService receiptService,
        IAuditService auditService,
        ILogger<ReceiptGenerationJob> logger)
    {
        _receiptService = receiptService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Generates a receipt for a successful payment transaction
    /// </summary>
    public async Task GenerateReceiptAsync(Guid transactionId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting receipt generation for transaction {TransactionId}", transactionId);

            var receipt = await _receiptService.GenerateReceiptPdfAsync(transactionId, ct);

            await _auditService.LogAsync(
                "RECEIPT_GENERATED",
                "Transaction",
                transactionId.ToString(),
                $"Generated receipt for transaction {transactionId}",
                ct);

            _logger.LogInformation(
                "Successfully generated receipt for transaction {TransactionId}, size: {Size} bytes",
                transactionId, receipt.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate receipt for transaction {TransactionId}", transactionId);

            await _auditService.LogAsync(
                "RECEIPT_GENERATION_FAILED",
                "Transaction",
                transactionId.ToString(),
                $"Failed to generate receipt: {ex.Message}",
                ct);

            throw;
        }
    }
}
