namespace DebtManager.Contracts.Documents;

public class ReceiptGenerationData
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AUD";
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string DebtorName { get; set; } = string.Empty;
    public string? DebtorEmail { get; set; }
    public string? DebtorAddress { get; set; }
    public string? DebtReference { get; set; }
    public string? OrganizationName { get; set; }
    public string? OrganizationLogo { get; set; }
    public string? OrganizationAddress { get; set; }
    public string? OrganizationPhone { get; set; }
    public string? OrganizationEmail { get; set; }
    public string? Notes { get; set; }
    public string PrimaryColor { get; set; } = "#0066cc";
    public bool IsPartialPayment { get; set; }
    public decimal? RemainingBalance { get; set; }
}

public class InvoiceGenerationData
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssuedDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "AUD";
    public string OrganizationName { get; set; } = string.Empty;
    public string? OrganizationAbn { get; set; }
    public string? OrganizationAddress { get; set; }
    public string? OrganizationLogo { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public string? TermsAndConditions { get; set; }
    public List<InvoiceLineItem> LineItems { get; set; } = new();
    public string PrimaryColor { get; set; } = "#0066cc";
    public string? PaymentInstructions { get; set; }
}

public class InvoiceLineItem
{
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public interface IDocumentGenerationService
{
    Task<byte[]> GenerateReceiptPdfAsync(ReceiptGenerationData receiptData);
    Task<string> GenerateReceiptHtmlAsync(ReceiptGenerationData receiptData);
    Task<byte[]> GenerateInvoicePdfAsync(InvoiceGenerationData invoiceData);
    Task<string> GenerateInvoiceHtmlAsync(InvoiceGenerationData invoiceData);
    Task SendReceiptEmailAsync(ReceiptGenerationData receiptData, string toEmail, string? ccEmail = null);
    Task SendInvoiceEmailAsync(InvoiceGenerationData invoiceData, string toEmail, string? ccEmail = null);
    Task SendBatchReceiptEmailsAsync(List<(ReceiptGenerationData receipt, string email)> receipts);
}
