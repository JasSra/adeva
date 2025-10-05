namespace DebtManager.Contracts.Documents;

public class InvoiceExtractionResult
{
    public bool Success { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? Currency { get; set; }
    public string? VendorName { get; set; }
    public string? VendorAddress { get; set; }
    public string? VendorAbn { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerAddress { get; set; }
    public Dictionary<string, string> AdditionalFields { get; set; } = new();
    public decimal? ConfidenceScore { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IInvoiceProcessingService
{
    /// <summary>
    /// Extracts invoice data from a document using AI/OCR
    /// </summary>
    Task<InvoiceExtractionResult> ExtractInvoiceDataAsync(Guid documentId, CancellationToken ct = default);
    
    /// <summary>
    /// Queue an invoice for background processing
    /// </summary>
    Task<Guid> QueueInvoiceProcessingAsync(Guid documentId, CancellationToken ct = default);
}
