using System.Text;
using DebtManager.Contracts.Documents;

namespace DebtManager.Infrastructure.Documents;

public class DocumentGenerationService : IDocumentGenerationService
{
    public Task<byte[]> GenerateReceiptPdfAsync(ReceiptData receiptData)
    {
        var html = GenerateReceiptHtmlAsync(receiptData).Result;
        return Task.FromResult(Encoding.UTF8.GetBytes(html));
    }

    public Task<string> GenerateReceiptHtmlAsync(ReceiptData receiptData)
    {
        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Receipt {receiptData.ReceiptNumber}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            color: #333;
        }}
        .header {{
            display: flex;
            justify-content: space-between;
            align-items: center;
            border-bottom: 3px solid {receiptData.PrimaryColor};
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .logo {{
            max-width: 200px;
            max-height: 80px;
        }}
        .company-info {{
            text-align: right;
            font-size: 12px;
        }}
        h1 {{
            color: {receiptData.PrimaryColor};
            margin: 0;
            font-size: 28px;
        }}
        .receipt-info {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
        }}
        .info-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 8px;
        }}
        .label {{
            font-weight: bold;
            color: #666;
        }}
        .value {{
            color: #333;
        }}
        .amount-section {{
            background: {receiptData.PrimaryColor};
            color: white;
            padding: 20px;
            border-radius: 5px;
            text-align: center;
            margin: 30px 0;
        }}
        .amount {{
            font-size: 36px;
            font-weight: bold;
            margin: 10px 0;
        }}
        .debtor-info {{
            margin: 20px 0;
            padding: 15px;
            border-left: 3px solid {receiptData.PrimaryColor};
            background: #f8f9fa;
        }}
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 11px;
            color: #666;
            text-align: center;
        }}
        .adeva-branding {{
            margin-top: 20px;
            font-size: 10px;
            color: #999;
            text-align: center;
        }}
        .notes {{
            background: #fff3cd;
            padding: 15px;
            border-left: 3px solid #ffc107;
            margin: 20px 0;
        }}
        .status-badge {{
            display: inline-block;
            padding: 5px 15px;
            background: #28a745;
            color: white;
            border-radius: 20px;
            font-size: 12px;
            margin-left: 10px;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <div>
            <h1>PAYMENT RECEIPT</h1>
            {(receiptData.IsPartialPayment ? "<span class=\"status-badge\">PARTIAL PAYMENT</span>" : "<span class=\"status-badge\">PAID IN FULL</span>")}
        </div>
        <div class=""company-info"">
            {(!string.IsNullOrEmpty(receiptData.OrganizationLogo) ? $"<img src=\"{receiptData.OrganizationLogo}\" class=\"logo\" alt=\"Logo\"/>" : "")}
            <div><strong>{receiptData.OrganizationName ?? "Adeva Debt Management"}</strong></div>
            {(!string.IsNullOrEmpty(receiptData.OrganizationAddress) ? $"<div>{receiptData.OrganizationAddress}</div>" : "")}
            {(!string.IsNullOrEmpty(receiptData.OrganizationPhone) ? $"<div>Phone: {receiptData.OrganizationPhone}</div>" : "")}
            {(!string.IsNullOrEmpty(receiptData.OrganizationEmail) ? $"<div>Email: {receiptData.OrganizationEmail}</div>" : "")}
        </div>
    </div>

    <div class=""receipt-info"">
        <div class=""info-row"">
            <span class=""label"">Receipt Number:</span>
            <span class=""value"">{receiptData.ReceiptNumber}</span>
        </div>
        <div class=""info-row"">
            <span class=""label"">Date Issued:</span>
            <span class=""value"">{receiptData.IssuedDate:MMMM dd, yyyy}</span>
        </div>
        <div class=""info-row"">
            <span class=""label"">Payment Method:</span>
            <span class=""value"">{receiptData.PaymentMethod}</span>
        </div>
        {(!string.IsNullOrEmpty(receiptData.ReferenceNumber) ? $@"
        <div class=""info-row"">
            <span class=""label"">Reference Number:</span>
            <span class=""value"">{receiptData.ReferenceNumber}</span>
        </div>" : "")}
        {(!string.IsNullOrEmpty(receiptData.DebtReference) ? $@"
        <div class=""info-row"">
            <span class=""label"">Debt Reference:</span>
            <span class=""value"">{receiptData.DebtReference}</span>
        </div>" : "")}
    </div>

    <div class=""debtor-info"">
        <h3 style=""margin-top: 0; color: {receiptData.PrimaryColor};"">Payment Received From:</h3>
        <div><strong>{receiptData.DebtorName}</strong></div>
        {(!string.IsNullOrEmpty(receiptData.DebtorEmail) ? $"<div>Email: {receiptData.DebtorEmail}</div>" : "")}
        {(!string.IsNullOrEmpty(receiptData.DebtorAddress) ? $"<div>{receiptData.DebtorAddress}</div>" : "")}
    </div>

    <div class=""amount-section"">
        <div>Amount Paid</div>
        <div class=""amount"">{receiptData.Currency} {receiptData.Amount:N2}</div>
        {(receiptData.IsPartialPayment && receiptData.RemainingBalance.HasValue ? $"<div style=\"font-size: 14px; margin-top: 10px;\">Remaining Balance: {receiptData.Currency} {receiptData.RemainingBalance.Value:N2}</div>" : "")}
    </div>

    {(!string.IsNullOrEmpty(receiptData.Notes) ? $@"
    <div class=""notes"">
        <strong>Notes:</strong>
        <div>{receiptData.Notes}</div>
    </div>" : "")}

    <div class=""footer"">
        <p>This receipt confirms payment received. Please retain for your records.</p>
        {(!string.IsNullOrEmpty(receiptData.OrganizationEmail) ? $"<p>For inquiries, contact us at {receiptData.OrganizationEmail}</p>" : "")}
    </div>

    <div class=""adeva-branding"">
        <p>Powered by Adeva Debt Management Platform</p>
    </div>
</body>
</html>";

        return Task.FromResult(html);
    }

    public Task<byte[]> GenerateInvoicePdfAsync(InvoiceData invoiceData)
    {
        var html = GenerateInvoiceHtmlAsync(invoiceData).Result;
        return Task.FromResult(Encoding.UTF8.GetBytes(html));
    }

    public Task<string> GenerateInvoiceHtmlAsync(InvoiceData invoiceData)
    {
        var lineItemsHtml = new StringBuilder();
        foreach (var item in invoiceData.LineItems)
        {
            lineItemsHtml.AppendLine($@"
                <tr>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd;"">{item.Description}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd; text-align: center;"">{item.Quantity:N2}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd; text-align: right;"">{invoiceData.Currency} {item.UnitPrice:N2}</td>
                    <td style=""padding: 10px; border-bottom: 1px solid #ddd; text-align: right; font-weight: bold;"">{invoiceData.Currency} {item.Amount:N2}</td>
                </tr>");
        }

        var html = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Invoice {invoiceData.InvoiceNumber}</title>
    <style>
        body {{
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 20px;
            color: #333;
        }}
        .header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            border-bottom: 3px solid {invoiceData.PrimaryColor};
            padding-bottom: 20px;
            margin-bottom: 30px;
        }}
        .logo {{
            max-width: 200px;
            max-height: 80px;
        }}
        .company-info {{
            text-align: right;
            font-size: 12px;
        }}
        h1 {{
            color: {invoiceData.PrimaryColor};
            margin: 0;
            font-size: 32px;
        }}
        .invoice-details {{
            background: #f8f9fa;
            padding: 15px;
            border-radius: 5px;
            margin-bottom: 20px;
        }}
        .detail-row {{
            display: flex;
            justify-content: space-between;
            margin-bottom: 8px;
        }}
        .label {{
            font-weight: bold;
            color: #666;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin: 20px 0;
        }}
        th {{
            background: {invoiceData.PrimaryColor};
            color: white;
            padding: 12px;
            text-align: left;
        }}
        th:nth-child(2), th:nth-child(3), th:nth-child(4) {{
            text-align: right;
        }}
        .totals {{
            margin-top: 20px;
            text-align: right;
        }}
        .totals-row {{
            display: flex;
            justify-content: flex-end;
            padding: 8px 0;
        }}
        .totals-label {{
            width: 150px;
            font-weight: bold;
            text-align: right;
            padding-right: 20px;
        }}
        .totals-value {{
            width: 150px;
            text-align: right;
        }}
        .total-amount {{
            background: {invoiceData.PrimaryColor};
            color: white;
            padding: 15px;
            border-radius: 5px;
            font-size: 18px;
            font-weight: bold;
        }}
        .notes {{
            background: #f8f9fa;
            padding: 15px;
            border-left: 3px solid {invoiceData.PrimaryColor};
            margin: 20px 0;
        }}
        .terms {{
            font-size: 11px;
            color: #666;
            margin-top: 30px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
        }}
        .footer {{
            margin-top: 40px;
            padding-top: 20px;
            border-top: 1px solid #ddd;
            font-size: 11px;
            color: #666;
            text-align: center;
        }}
        .adeva-branding {{
            margin-top: 20px;
            font-size: 10px;
            color: #999;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class=""header"">
        <div>
            <h1>INVOICE</h1>
            <div style=""font-size: 14px; margin-top: 10px;"">{invoiceData.InvoiceNumber}</div>
        </div>
        <div class=""company-info"">
            {(!string.IsNullOrEmpty(invoiceData.OrganizationLogo) ? $"<img src=\"{invoiceData.OrganizationLogo}\" class=\"logo\" alt=\"Logo\"/>" : "")}
            <div><strong>Adeva Debt Management</strong></div>
            <div>On behalf of: <strong>{invoiceData.OrganizationName}</strong></div>
            {(!string.IsNullOrEmpty(invoiceData.OrganizationAbn) ? $"<div>ABN: {invoiceData.OrganizationAbn}</div>" : "")}
            {(!string.IsNullOrEmpty(invoiceData.OrganizationAddress) ? $"<div>{invoiceData.OrganizationAddress}</div>" : "")}
        </div>
    </div>

    <div class=""invoice-details"">
        <div class=""detail-row"">
            <span class=""label"">Invoice Number:</span>
            <span>{invoiceData.InvoiceNumber}</span>
        </div>
        <div class=""detail-row"">
            <span class=""label"">Issue Date:</span>
            <span>{invoiceData.IssuedDate:MMMM dd, yyyy}</span>
        </div>
        {(invoiceData.DueDate.HasValue ? $@"
        <div class=""detail-row"">
            <span class=""label"">Due Date:</span>
            <span>{invoiceData.DueDate.Value:MMMM dd, yyyy}</span>
        </div>" : "")}
        <div class=""detail-row"">
            <span class=""label"">Billed To:</span>
            <span><strong>{invoiceData.OrganizationName}</strong></span>
        </div>
    </div>

    {(!string.IsNullOrEmpty(invoiceData.Description) ? $@"
    <div style=""margin: 20px 0;"">
        <h3 style=""color: {invoiceData.PrimaryColor};"">Description</h3>
        <p>{invoiceData.Description}</p>
    </div>" : "")}

    <table>
        <thead>
            <tr>
                <th style=""text-align: left;"">Description</th>
                <th style=""text-align: center;"">Quantity</th>
                <th style=""text-align: right;"">Unit Price</th>
                <th style=""text-align: right;"">Amount</th>
            </tr>
        </thead>
        <tbody>
            {lineItemsHtml}
        </tbody>
    </table>

    <div class=""totals"">
        <div class=""totals-row"">
            <div class=""totals-label"">Subtotal:</div>
            <div class=""totals-value"">{invoiceData.Currency} {invoiceData.Subtotal:N2}</div>
        </div>
        <div class=""totals-row"">
            <div class=""totals-label"">Tax (GST):</div>
            <div class=""totals-value"">{invoiceData.Currency} {invoiceData.TaxAmount:N2}</div>
        </div>
        <div class=""totals-row total-amount"">
            <div class=""totals-label"">Total Amount:</div>
            <div class=""totals-value"">{invoiceData.Currency} {invoiceData.Total:N2}</div>
        </div>
    </div>

    {(!string.IsNullOrEmpty(invoiceData.PaymentInstructions) ? $@"
    <div class=""notes"">
        <h3 style=""margin-top: 0; color: {invoiceData.PrimaryColor};"">Payment Instructions</h3>
        <div>{invoiceData.PaymentInstructions}</div>
    </div>" : "")}

    {(!string.IsNullOrEmpty(invoiceData.Notes) ? $@"
    <div class=""notes"">
        <strong>Notes:</strong>
        <div>{invoiceData.Notes}</div>
    </div>" : "")}

    {(!string.IsNullOrEmpty(invoiceData.TermsAndConditions) ? $@"
    <div class=""terms"">
        <h4>Terms and Conditions</h4>
        <p>{invoiceData.TermsAndConditions}</p>
    </div>" : "")}

    <div class=""footer"">
        <p>Thank you for your business.</p>
    </div>

    <div class=""adeva-branding"">
        <p>Generated by Adeva Debt Management Platform</p>
    </div>
</body>
</html>";

        return Task.FromResult(html);
    }
}
