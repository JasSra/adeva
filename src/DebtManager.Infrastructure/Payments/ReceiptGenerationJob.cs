using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Audit;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Notifications;
using Microsoft.Extensions.Logging;

namespace DebtManager.Infrastructure.Payments;

/// <summary>
/// Background job for generating payment receipts and sending notifications
/// </summary>
public class ReceiptGenerationJob
{
    private readonly IReceiptService _receiptService;
    private readonly IAuditService _auditService;
    private readonly ITransactionRepository _transactionRepository;
    private readonly IDebtorRepository _debtorRepository;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<ReceiptGenerationJob> _logger;

    public ReceiptGenerationJob(
        IReceiptService receiptService,
        IAuditService auditService,
        ITransactionRepository transactionRepository,
        IDebtorRepository debtorRepository,
        IEmailSender emailSender,
        ILogger<ReceiptGenerationJob> logger)
    {
        _receiptService = receiptService;
        _auditService = auditService;
        _transactionRepository = transactionRepository;
        _debtorRepository = debtorRepository;
        _emailSender = emailSender;
        _logger = logger;
    }

    /// <summary>
    /// Generates a receipt for a successful payment transaction and emails it to the debtor
    /// </summary>
    public async Task GenerateReceiptAsync(Guid transactionId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Starting receipt generation for transaction {TransactionId}", transactionId);

            // Get transaction details
            var transaction = await _transactionRepository.GetByIdAsync(transactionId, ct);
            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found for receipt generation", transactionId);
                return;
            }

            // Get debtor details for email
            var debtor = await _debtorRepository.GetAsync(transaction.DebtorId, ct);
            if (debtor == null)
            {
                _logger.LogWarning("Debtor {DebtorId} not found for transaction {TransactionId}", 
                    transaction.DebtorId, transactionId);
                return;
            }

            // Generate receipt
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

            // Email receipt to debtor
            if (!string.IsNullOrWhiteSpace(debtor.Email))
            {
                try
                {
                    var emailSubject = $"Payment Receipt - Transaction {transaction.ProviderRef}";
                    var emailBody = GenerateReceiptEmailBody(transaction, debtor);

                    await _emailSender.SendEmailAsync(debtor.Email, emailSubject, emailBody, ct);

                    await _auditService.LogAsync(
                        "RECEIPT_EMAIL_SENT",
                        "Transaction",
                        transactionId.ToString(),
                        $"Receipt emailed to {debtor.Email}",
                        ct);

                    _logger.LogInformation(
                        "Receipt email sent to {Email} for transaction {TransactionId}",
                        debtor.Email, transactionId);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, 
                        "Failed to send receipt email for transaction {TransactionId}", transactionId);
                    
                    await _auditService.LogAsync(
                        "RECEIPT_EMAIL_FAILED",
                        "Transaction",
                        transactionId.ToString(),
                        $"Failed to email receipt: {emailEx.Message}",
                        ct);
                }
            }
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

    private string GenerateReceiptEmailBody(Domain.Payments.Transaction transaction, Domain.Debtors.Debtor debtor)
    {
        var receiptNumber = $"RCP-{transaction.Id.ToString().Substring(0, 8).ToUpper()}";
        
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: linear-gradient(135deg, #0ea5e9 0%, #3b82f6 100%); color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f9fafb; padding: 30px; border-radius: 0 0 8px 8px; }}
        .receipt-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border: 2px solid #e5e7eb; }}
        .info-row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #e5e7eb; }}
        .info-label {{ color: #6b7280; font-weight: 600; }}
        .info-value {{ color: #111827; font-weight: bold; }}
        .amount-box {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 20px; text-align: center; border-radius: 8px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6b7280; font-size: 12px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1 style='margin: 0;'>Payment Confirmation</h1>
            <p style='margin: 10px 0 0 0; opacity: 0.9;'>Receipt #{receiptNumber}</p>
        </div>
        <div class='content'>
            <p>Dear {debtor.FirstName} {debtor.LastName},</p>
            <p>Thank you for your payment. This email confirms that we have received your payment successfully.</p>
            
            <div class='amount-box'>
                <div style='font-size: 14px; opacity: 0.9;'>Amount Paid</div>
                <div style='font-size: 32px; font-weight: bold; margin-top: 5px;'>${transaction.Amount:N2} {transaction.Currency}</div>
            </div>

            <div class='receipt-box'>
                <h3 style='margin-top: 0;'>Payment Details</h3>
                <div class='info-row'>
                    <span class='info-label'>Transaction ID:</span>
                    <span class='info-value'>{transaction.ProviderRef ?? transaction.Id.ToString()}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Payment Date:</span>
                    <span class='info-value'>{transaction.ProcessedAtUtc:MMMM dd, yyyy 'at' hh:mm tt}</span>
                </div>
                <div class='info-row'>
                    <span class='info-label'>Payment Method:</span>
                    <span class='info-value'>{transaction.Method}</span>
                </div>
                <div class='info-row' style='border-bottom: none;'>
                    <span class='info-label'>Status:</span>
                    <span class='info-value' style='color: #10b981;'>✓ Completed</span>
                </div>
            </div>

            <p style='background: #fef3c7; border-left: 4px solid #f59e0b; padding: 15px; border-radius: 4px; font-size: 14px;'>
                <strong>Important:</strong> Please keep this email for your records. If you have any questions about this payment, please contact our support team.
            </p>

            <div class='footer'>
                <p>This is an automated message. Please do not reply to this email.</p>
                <p>&copy; {DateTime.UtcNow.Year} Debt Management System. All rights reserved.</p>
            </div>
        </div>
    </div>
</body>
</html>";
    }
}
