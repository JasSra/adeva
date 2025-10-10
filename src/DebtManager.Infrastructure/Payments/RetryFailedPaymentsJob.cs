using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Audit;
using DebtManager.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace DebtManager.Infrastructure.Payments;

/// <summary>
/// Background job for retrying failed payments
/// </summary>
public class RetryFailedPaymentsJob
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<RetryFailedPaymentsJob> _logger;

    public RetryFailedPaymentsJob(
        ITransactionRepository transactionRepository,
        IAuditService auditService,
        ILogger<RetryFailedPaymentsJob> logger)
    {
        _transactionRepository = transactionRepository;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Finds and processes failed payments eligible for retry
    /// </summary>
    public async Task ProcessFailedPaymentsAsync(CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting failed payments retry process");

            // Get failed transactions from the last 7 days
            var failedTransactions = await _transactionRepository.GetFailedTransactionsAsync(
                DateTime.UtcNow.AddDays(-7), ct);

            _logger.LogInformation("Found {Count} failed transactions to process", failedTransactions.Count);

            var retriedCount = 0;
            var skippedCount = 0;

            foreach (var transaction in failedTransactions)
            {
                // Check if transaction is eligible for retry (e.g., card errors, temporary issues)
                if (IsEligibleForRetry(transaction))
                {
                    // Mark for manual review or automated retry
                    _logger.LogInformation(
                        "Transaction {TransactionId} marked for retry. Reason: {Reason}",
                        transaction.Id, transaction.FailureReason);

                    await _auditService.LogAsync(
                        "PAYMENT_RETRY_FLAGGED",
                        "Transaction",
                        transaction.Id.ToString(),
                        $"Failed payment flagged for retry. Original failure: {transaction.FailureReason}",
                        ct);

                    retriedCount++;
                }
                else
                {
                    skippedCount++;
                }
            }

            _logger.LogInformation(
                "Failed payments processing completed. Flagged: {Retried}, Skipped: {Skipped}",
                retriedCount, skippedCount);

            await _auditService.LogAsync(
                "FAILED_PAYMENTS_PROCESSED",
                "System",
                null,
                $"Processed {failedTransactions.Count} failed payments. Flagged: {retriedCount}, Skipped: {skippedCount}",
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing failed payments");
            throw;
        }
    }

    private bool IsEligibleForRetry(Transaction transaction)
    {
        if (transaction.Status != TransactionStatus.Failed)
            return false;

        // Check failure reason for temporary issues
        var reason = transaction.FailureReason?.ToLower() ?? "";

        // Retry eligible errors
        var retryableErrors = new[]
        {
            "insufficient funds",
            "card declined",
            "processing error",
            "network error",
            "timeout",
            "temporary"
        };

        return retryableErrors.Any(error => reason.Contains(error));
    }
}
