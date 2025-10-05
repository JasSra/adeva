using DebtManager.Domain.Common;

namespace DebtManager.Domain.Documents;

public enum InvoiceProcessingStatus
{
    Pending = 0,
    Processing = 1,
    Completed = 2,
    Failed = 3,
    ManualReviewRequired = 4
}

public class InvoiceData : Entity
{
    public Guid DocumentId { get; private set; }
    public Document? Document { get; private set; }

    public InvoiceProcessingStatus Status { get; private set; }
    
    // Extracted invoice information
    public string? InvoiceNumber { get; private set; }
    public DateTime? InvoiceDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public decimal? TotalAmount { get; private set; }
    public string? Currency { get; private set; }
    
    // Customer/Vendor details
    public string? VendorName { get; private set; }
    public string? VendorAddress { get; private set; }
    public string? VendorAbn { get; private set; }
    public string? CustomerName { get; private set; }
    public string? CustomerAddress { get; private set; }
    
    // Raw extracted data as JSON for additional fields
    public string? ExtractedDataJson { get; private set; }
    
    // Processing metadata
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public decimal? ConfidenceScore { get; private set; }

    private InvoiceData()
    {
    }

    public static InvoiceData Create(Guid documentId)
    {
        return new InvoiceData
        {
            DocumentId = documentId,
            Status = InvoiceProcessingStatus.Pending,
            RetryCount = 0
        };
    }

    public void MarkAsProcessing()
    {
        Status = InvoiceProcessingStatus.Processing;
    }

    public void MarkAsCompleted(
        string? invoiceNumber,
        DateTime? invoiceDate,
        DateTime? dueDate,
        decimal? totalAmount,
        string? currency,
        string? vendorName,
        string? vendorAddress,
        string? vendorAbn,
        string? customerName,
        string? customerAddress,
        string? extractedDataJson,
        decimal? confidenceScore)
    {
        Status = InvoiceProcessingStatus.Completed;
        InvoiceNumber = invoiceNumber;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        TotalAmount = totalAmount;
        Currency = currency;
        VendorName = vendorName;
        VendorAddress = vendorAddress;
        VendorAbn = vendorAbn;
        CustomerName = customerName;
        CustomerAddress = customerAddress;
        ExtractedDataJson = extractedDataJson;
        ConfidenceScore = confidenceScore;
        ProcessedAtUtc = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = InvoiceProcessingStatus.Failed;
        ErrorMessage = errorMessage;
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsManualReviewRequired(string reason)
    {
        Status = InvoiceProcessingStatus.ManualReviewRequired;
        ErrorMessage = reason;
        ProcessedAtUtc = DateTime.UtcNow;
    }

    public void IncrementRetryCount()
    {
        RetryCount++;
    }
}
