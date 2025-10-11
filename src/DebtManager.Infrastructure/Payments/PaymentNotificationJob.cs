using DebtManager.Contracts.Audit;
using DebtManager.Contracts.Notifications;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Payments;
using Microsoft.Extensions.Logging;

namespace DebtManager.Infrastructure.Payments;

/// <summary>
/// Background job for sending payment success/failure notifications
/// </summary>
public class PaymentNotificationJob
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly IDebtorRepository _debtorRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IAuditService _auditService;
    private readonly ILogger<PaymentNotificationJob> _logger;

    public PaymentNotificationJob(
        ITransactionRepository transactionRepository,
        IDebtRepository debtRepository,
        IDebtorRepository debtorRepository,
        IOrganizationRepository organizationRepository,
        IEmailSender emailSender,
        ISmsSender smsSender,
        IAuditService auditService,
        ILogger<PaymentNotificationJob> logger)
    {
        _transactionRepository = transactionRepository;
        _debtRepository = debtRepository;
        _debtorRepository = debtorRepository;
        _organizationRepository = organizationRepository;
        _emailSender = emailSender;
        _smsSender = smsSender;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Sends payment success notification to debtor
    /// </summary>
    public async Task SendPaymentSuccessNotificationAsync(Guid transactionId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Sending payment success notification for transaction {TransactionId}", transactionId);

            var transaction = await _transactionRepository.GetByIdAsync(transactionId, ct);
            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                return;
            }

            var debtor = await _debtorRepository.GetAsync(transaction.DebtorId, ct);
            if (debtor == null)
            {
                _logger.LogWarning("Debtor {DebtorId} not found", transaction.DebtorId);
                return;
            }

            var debt = await _debtRepository.GetWithDetailsAsync(transaction.DebtId, ct);
            var organization = debt != null ? await _organizationRepository.GetAsync(debt.OrganizationId, ct) : null;

            // Send email notification
            if (!string.IsNullOrWhiteSpace(debtor.Email))
            {
                var subject = "Payment Successful - Thank You";
                var body = GenerateSuccessEmailBody(transaction, debtor, debt, organization);
                
                await _emailSender.SendEmailAsync(debtor.Email, subject, body, ct);
                
                await _auditService.LogAsync(
                    "PAYMENT_SUCCESS_EMAIL_SENT",
                    "Transaction",
                    transactionId.ToString(),
                    $"Success notification sent to {debtor.Email}",
                    ct);

                _logger.LogInformation("Payment success email sent to {Email}", debtor.Email);
            }

            // Send SMS notification if phone number is available
            if (!string.IsNullOrWhiteSpace(debtor.Phone))
            {
                var smsMessage = $"Payment of ${transaction.Amount:N2} {transaction.Currency} received. Thank you! Ref: {transaction.ProviderRef}";
                
                await _smsSender.SendSmsAsync(debtor.Phone, smsMessage, ct);
                
                await _auditService.LogAsync(
                    "PAYMENT_SUCCESS_SMS_SENT",
                    "Transaction",
                    transactionId.ToString(),
                    $"Success SMS sent to {debtor.Phone}",
                    ct);

                _logger.LogInformation("Payment success SMS sent to {Phone}", debtor.Phone);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment success notification for transaction {TransactionId}", transactionId);
            
            await _auditService.LogAsync(
                "PAYMENT_SUCCESS_NOTIFICATION_FAILED",
                "Transaction",
                transactionId.ToString(),
                $"Failed to send notification: {ex.Message}",
                ct);
        }
    }

    /// <summary>
    /// Sends payment failure notification to debtor
    /// </summary>
    public async Task SendPaymentFailureNotificationAsync(Guid transactionId, CancellationToken ct)
    {
        try
        {
            _logger.LogInformation("Sending payment failure notification for transaction {TransactionId}", transactionId);

            var transaction = await _transactionRepository.GetByIdAsync(transactionId, ct);
            if (transaction == null)
            {
                _logger.LogWarning("Transaction {TransactionId} not found", transactionId);
                return;
            }

            var debtor = await _debtorRepository.GetAsync(transaction.DebtorId, ct);
            if (debtor == null)
            {
                _logger.LogWarning("Debtor {DebtorId} not found", transaction.DebtorId);
                return;
            }

            var debt = await _debtRepository.GetWithDetailsAsync(transaction.DebtId, ct);
            var organization = debt != null ? await _organizationRepository.GetAsync(debt.OrganizationId, ct) : null;

            // Send email notification
            if (!string.IsNullOrWhiteSpace(debtor.Email))
            {
                var subject = "Payment Failed - Action Required";
                var body = GenerateFailureEmailBody(transaction, debtor, debt, organization);
                
                await _emailSender.SendEmailAsync(debtor.Email, subject, body, ct);
                
                await _auditService.LogAsync(
                    "PAYMENT_FAILURE_EMAIL_SENT",
                    "Transaction",
                    transactionId.ToString(),
                    $"Failure notification sent to {debtor.Email}",
                    ct);

                _logger.LogInformation("Payment failure email sent to {Email}", debtor.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment failure notification for transaction {TransactionId}", transactionId);
            
            await _auditService.LogAsync(
                "PAYMENT_FAILURE_NOTIFICATION_FAILED",
                "Transaction",
                transactionId.ToString(),
                $"Failed to send notification: {ex.Message}",
                ct);
        }
    }

    private string GenerateSuccessEmailBody(
        Transaction transaction,
        Domain.Debtors.Debtor debtor,
        Domain.Debts.Debt? debt,
        Domain.Organizations.Organization? organization)
    {
        var orgName = organization?.Name ?? "Debt Management System";
        var supportEmail = organization?.SupportEmail ?? "support@example.com";
        var supportPhone = organization?.SupportPhone ?? "1-800-000-0000";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: white; padding: 40px 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9fafb; }}
        .success-icon {{ font-size: 48px; margin-bottom: 10px; }}
        .details-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #e5e7eb; }}
        .detail-row {{ padding: 12px 0; border-bottom: 1px solid #f3f4f6; }}
        .detail-row:last-child {{ border-bottom: none; }}
        .label {{ color: #6b7280; font-size: 14px; }}
        .value {{ color: #111827; font-weight: 600; margin-top: 4px; }}
        .button {{ display: inline-block; background: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6b7280; font-size: 12px; padding: 20px; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='success-icon'>✓</div>
        <h1 style='margin: 0;'>Payment Successful!</h1>
        <p style='margin: 10px 0 0 0; opacity: 0.9;'>Your payment has been received</p>
    </div>
    <div class='content'>
        <p>Dear {debtor.FirstName} {debtor.LastName},</p>
        <p>We have successfully received your payment. Thank you for your prompt payment!</p>
        
        <div class='details-box'>
            <h3 style='margin-top: 0; color: #111827;'>Payment Summary</h3>
            <div class='detail-row'>
                <div class='label'>Amount Paid</div>
                <div class='value' style='color: #10b981; font-size: 24px;'>${transaction.Amount:N2} {transaction.Currency}</div>
            </div>
            <div class='detail-row'>
                <div class='label'>Transaction ID</div>
                <div class='value'>{transaction.ProviderRef ?? transaction.Id.ToString()}</div>
            </div>
            <div class='detail-row'>
                <div class='label'>Payment Date</div>
                <div class='value'>{transaction.ProcessedAtUtc:MMMM dd, yyyy 'at' hh:mm tt}</div>
            </div>
            <div class='detail-row'>
                <div class='label'>Payment Method</div>
                <div class='value'>{transaction.Method}</div>
            </div>
            {(debt != null ? $@"
            <div class='detail-row'>
                <div class='label'>Remaining Balance</div>
                <div class='value'>${debt.OutstandingPrincipal:N2} {debt.Currency}</div>
            </div>" : "")}
        </div>

        <p style='background: #dbeafe; border-left: 4px solid #3b82f6; padding: 15px; border-radius: 4px; font-size: 14px;'>
            <strong>What's Next?</strong><br/>
            A detailed receipt has been sent to you in a separate email. You can also view your payment history and download receipts anytime from your account dashboard.
        </p>

        <div style='text-align: center;'>
            <a href='https://yourdomain.com/User/Payments' class='button'>View Payment History</a>
        </div>

        <p style='margin-top: 30px; font-size: 14px;'>
            If you have any questions, please contact us:<br/>
            Email: {supportEmail}<br/>
            Phone: {supportPhone}
        </p>
    </div>
    <div class='footer'>
        <p>This is an automated notification from {orgName}</p>
        <p>&copy; {DateTime.UtcNow.Year} {orgName}. All rights reserved.</p>
    </div>
</body>
</html>";
    }

    private string GenerateFailureEmailBody(
        Transaction transaction,
        Domain.Debtors.Debtor debtor,
        Domain.Debts.Debt? debt,
        Domain.Organizations.Organization? organization)
    {
        var orgName = organization?.Name ?? "Debt Management System";
        var supportEmail = organization?.SupportEmail ?? "support@example.com";
        var supportPhone = organization?.SupportPhone ?? "1-800-000-0000";
        var failureReason = transaction.FailureReason ?? "Unknown error";

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; }}
        .header {{ background: linear-gradient(135deg, #ef4444 0%, #dc2626 100%); color: white; padding: 40px 20px; text-align: center; }}
        .content {{ padding: 30px 20px; background: #f9fafb; }}
        .error-icon {{ font-size: 48px; margin-bottom: 10px; }}
        .details-box {{ background: white; padding: 20px; border-radius: 8px; margin: 20px 0; border: 1px solid #e5e7eb; }}
        .alert-box {{ background: #fef2f2; border-left: 4px solid #ef4444; padding: 15px; border-radius: 4px; margin: 20px 0; }}
        .tip-box {{ background: #dbeafe; border-left: 4px solid #3b82f6; padding: 15px; border-radius: 4px; margin: 20px 0; }}
        .button {{ display: inline-block; background: #3b82f6; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; margin: 20px 0; }}
        .footer {{ text-align: center; color: #6b7280; font-size: 12px; padding: 20px; }}
        ul {{ margin: 10px 0; padding-left: 20px; }}
        li {{ margin: 8px 0; }}
    </style>
</head>
<body>
    <div class='header'>
        <div class='error-icon'>⚠</div>
        <h1 style='margin: 0;'>Payment Failed</h1>
        <p style='margin: 10px 0 0 0; opacity: 0.9;'>We couldn't process your payment</p>
    </div>
    <div class='content'>
        <p>Dear {debtor.FirstName} {debtor.LastName},</p>
        <p>We attempted to process your payment but it was unsuccessful. Please review the details below and try again.</p>
        
        <div class='alert-box'>
            <strong>Reason for Failure:</strong><br/>
            {failureReason}
        </div>

        <div class='details-box'>
            <h3 style='margin-top: 0; color: #111827;'>Attempted Payment</h3>
            <p><strong>Amount:</strong> ${transaction.Amount:N2} {transaction.Currency}</p>
            <p><strong>Date:</strong> {transaction.ProcessedAtUtc:MMMM dd, yyyy 'at' hh:mm tt}</p>
            <p><strong>Payment Method:</strong> {transaction.Method}</p>
        </div>

        <div class='tip-box'>
            <strong>What you can do:</strong>
            <ul>
                <li>Verify your payment details are correct</li>
                <li>Check that you have sufficient funds available</li>
                <li>Try a different payment method</li>
                <li>Contact your bank if the issue persists</li>
            </ul>
        </div>

        <div style='text-align: center;'>
            <a href='https://yourdomain.com/User/Payments/MakePayment' class='button'>Try Payment Again</a>
        </div>

        <p style='margin-top: 30px; font-size: 14px;'>
            <strong>Need Help?</strong><br/>
            Our support team is here to assist you:<br/>
            Email: {supportEmail}<br/>
            Phone: {supportPhone}
        </p>
    </div>
    <div class='footer'>
        <p>This is an automated notification from {orgName}</p>
        <p>&copy; {DateTime.UtcNow.Year} {orgName}. All rights reserved.</p>
    </div>
</body>
</html>";
    }
}
