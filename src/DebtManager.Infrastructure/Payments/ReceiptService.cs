using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Persistence;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DebtManager.Infrastructure.Payments;

/// <summary>
/// Service for generating payment receipts
/// </summary>
public class ReceiptService : IReceiptService
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IDebtorRepository _debtorRepository;
    private readonly ILogger<ReceiptService> _logger;

    public ReceiptService(
        ITransactionRepository transactionRepository,
        IDebtRepository debtRepository,
        IOrganizationRepository organizationRepository,
        IDebtorRepository debtorRepository,
        ILogger<ReceiptService> logger)
    {
        _transactionRepository = transactionRepository;
        _debtRepository = debtRepository;
        _organizationRepository = organizationRepository;
        _debtorRepository = debtorRepository;
        _logger = logger;
    }

    public async Task<byte[]> GenerateReceiptPdfAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, ct);
        if (transaction == null)
        {
            throw new InvalidOperationException($"Transaction {transactionId} not found");
        }

        var debt = await _debtRepository.GetWithDetailsAsync(transaction.DebtId, ct);
        if (debt == null)
        {
            throw new InvalidOperationException($"Debt {transaction.DebtId} not found");
        }

        var organization = await _organizationRepository.GetAsync(debt.OrganizationId, ct);
        if (organization == null)
        {
            throw new InvalidOperationException($"Organization {debt.OrganizationId} not found");
        }

        var debtor = await _debtorRepository.GetAsync(transaction.DebtorId, ct);
        if (debtor == null)
        {
            throw new InvalidOperationException($"Debtor {transaction.DebtorId} not found");
        }

        // Generate HTML receipt
        var html = GenerateReceiptHtml(transaction, debt, organization, debtor);

        // For now, return HTML as bytes (in production, use a PDF library like IronPDF, SelectPdf, or DinkToPdf)
        // This is a placeholder implementation
        var bytes = Encoding.UTF8.GetBytes(html);

        _logger.LogInformation("Generated receipt for transaction {TransactionId}", transactionId);

        return bytes;
    }

    public async Task<byte[]?> GetReceiptAsync(Guid transactionId, CancellationToken ct = default)
    {
        // In production, this would check if receipt exists in storage and return it
        // For now, generate on-demand
        try
        {
            return await GenerateReceiptPdfAsync(transactionId, ct);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> HasReceiptAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await _transactionRepository.GetByIdAsync(transactionId, ct);
        return transaction != null && transaction.Status == Domain.Payments.TransactionStatus.Succeeded;
    }

    private string GenerateReceiptHtml(
        Domain.Payments.Transaction transaction,
        Domain.Debts.Debt debt,
        Domain.Organizations.Organization organization,
        Domain.Debtors.Debtor debtor)
    {
        var receiptNumber = $"RCP-{transaction.Id.ToString().Substring(0, 8).ToUpper()}";
        var transactionDate = transaction.ProcessedAtUtc.ToString("MMMM dd, yyyy");
        var transactionTime = transaction.ProcessedAtUtc.ToString("hh:mm tt");

        return $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8' />
    <title>Payment Receipt - {receiptNumber}</title>
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif;
            line-height: 1.6;
            color: #333;
            max-width: 800px;
            margin: 0 auto;
            padding: 40px 20px;
            background: #f5f5f5;
        }}
        .receipt {{
            background: white;
            padding: 40px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        .header {{
            text-align: center;
            border-bottom: 2px solid #0ea5e9;
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .header h1 {{
            margin: 0;
            color: #0ea5e9;
            font-size: 28px;
        }}
        .header .subtitle {{
            color: #666;
            font-size: 14px;
            margin-top: 5px;
        }}
        .receipt-number {{
            background: #0ea5e9;
            color: white;
            padding: 8px 16px;
            border-radius: 4px;
            display: inline-block;
            font-weight: bold;
            margin-top: 10px;
        }}
        .section {{
            margin: 25px 0;
        }}
        .section-title {{
            font-size: 14px;
            font-weight: bold;
            color: #666;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 10px;
        }}
        .info-grid {{
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 15px;
        }}
        .info-item {{
            padding: 12px;
            background: #f9fafb;
            border-radius: 4px;
        }}
        .info-label {{
            font-size: 12px;
            color: #666;
            margin-bottom: 4px;
        }}
        .info-value {{
            font-size: 14px;
            font-weight: 600;
            color: #111;
        }}
        .amount-box {{
            background: linear-gradient(135deg, #0ea5e9 0%, #3b82f6 100%);
            color: white;
            padding: 20px;
            border-radius: 8px;
            text-align: center;
            margin: 25px 0;
        }}
        .amount-label {{
            font-size: 14px;
            opacity: 0.9;
            margin-bottom: 5px;
        }}
        .amount-value {{
            font-size: 36px;
            font-weight: bold;
        }}
        .status-badge {{
            display: inline-block;
            padding: 6px 12px;
            background: #10b981;
            color: white;
            border-radius: 4px;
            font-size: 12px;
            font-weight: bold;
        }}
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #e5e7eb;
            text-align: center;
            color: #666;
            font-size: 12px;
        }}
        .footer-note {{
            background: #fef3c7;
            border-left: 4px solid #f59e0b;
            padding: 12px;
            margin-top: 20px;
            font-size: 12px;
            color: #92400e;
        }}
        @media print {{
            body {{
                background: white;
                padding: 0;
            }}
            .receipt {{
                box-shadow: none;
                padding: 20px;
            }}
        }}
    </style>
</head>
<body>
    <div class='receipt'>
        <div class='header'>
            <h1>{organization.Name}</h1>
            <div class='subtitle'>Payment Receipt</div>
            <div class='receipt-number'>{receiptNumber}</div>
        </div>

        <div class='section'>
            <div class='section-title'>Payment Information</div>
            <div class='info-grid'>
                <div class='info-item'>
                    <div class='info-label'>Transaction ID</div>
                    <div class='info-value'>{transaction.Id}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Date & Time</div>
                    <div class='info-value'>{transactionDate} at {transactionTime}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Payment Method</div>
                    <div class='info-value'>{transaction.Method}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Status</div>
                    <div class='info-value'>
                        <span class='status-badge'>{transaction.Status}</span>
                    </div>
                </div>
            </div>
        </div>

        <div class='amount-box'>
            <div class='amount-label'>Amount Paid</div>
            <div class='amount-value'>${transaction.Amount:N2} {transaction.Currency}</div>
        </div>

        <div class='section'>
            <div class='section-title'>Debt Information</div>
            <div class='info-grid'>
                <div class='info-item'>
                    <div class='info-label'>Reference Number</div>
                    <div class='info-value'>{debt.ClientReferenceNumber ?? "N/A"}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Account ID</div>
                    <div class='info-value'>{debt.ExternalAccountId ?? "N/A"}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Original Amount</div>
                    <div class='info-value'>${debt.OriginalPrincipal:N2} {debt.Currency}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Outstanding Balance</div>
                    <div class='info-value'>${debt.OutstandingPrincipal:N2} {debt.Currency}</div>
                </div>
            </div>
        </div>

        <div class='section'>
            <div class='section-title'>Payer Information</div>
            <div class='info-grid'>
                <div class='info-item'>
                    <div class='info-label'>Name</div>
                    <div class='info-value'>{debtor.FirstName} {debtor.LastName}</div>
                </div>
                <div class='info-item'>
                    <div class='info-label'>Email</div>
                    <div class='info-value'>{debtor.Email}</div>
                </div>
            </div>
        </div>

        <div class='footer-note'>
            <strong>Important:</strong> Please keep this receipt for your records. If you have any questions about this payment, 
            please contact {organization.Name} at {organization.SupportEmail} or {organization.SupportPhone}.
        </div>

        <div class='footer'>
            <p>This is an official payment receipt from {organization.Name}</p>
            <p>Receipt generated on {DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' hh:mm tt 'UTC'")}</p>
        </div>
    </div>
</body>
</html>";
    }
}
