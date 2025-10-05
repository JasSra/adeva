using DebtManager.Domain.Common;

namespace DebtManager.Domain.Documents;

public enum InvoiceStatus
{
    Draft,
    Issued,
    Paid,
    Cancelled
}

public enum InvoiceType
{
    Remittance,
    Service,
    Other
}

public class Invoice : Entity
{
    public Guid OrganizationId { get; private set; }
    public string InvoiceNumber { get; private set; }
    public InvoiceType Type { get; private set; }
    public InvoiceStatus Status { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }
    public DateTime? DueDateUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public string Currency { get; private set; }
    public string? Description { get; private set; }
    public string? Notes { get; private set; }
    public string? PaymentReference { get; private set; }
    public Guid? DocumentId { get; private set; }

    public Document? Document { get; private set; }

    private Invoice()
    {
        InvoiceNumber = string.Empty;
        Currency = "AUD";
        Status = InvoiceStatus.Draft;
        IssuedAtUtc = DateTime.UtcNow;
    }

    public Invoice(
        Guid organizationId,
        string invoiceNumber,
        InvoiceType type,
        decimal subtotal,
        decimal taxAmount,
        string currency,
        string? description = null)
        : this()
    {
        OrganizationId = organizationId;
        InvoiceNumber = invoiceNumber;
        Type = type;
        Subtotal = subtotal;
        TaxAmount = taxAmount;
        Total = subtotal + taxAmount;
        Currency = currency;
        Description = description;
    }

    public void Issue(DateTime? issuedAtUtc = null, DateTime? dueDate = null)
    {
        Status = InvoiceStatus.Issued;
        IssuedAtUtc = issuedAtUtc ?? DateTime.UtcNow;
        DueDateUtc = dueDate;
        UpdatedAtUtc = IssuedAtUtc;
    }

    public void MarkPaid(DateTime? paidAtUtc = null, string? paymentReference = null)
    {
        Status = InvoiceStatus.Paid;
        PaidAtUtc = paidAtUtc ?? DateTime.UtcNow;
        PaymentReference = paymentReference;
        UpdatedAtUtc = PaidAtUtc;
    }

    public void Cancel()
    {
        Status = InvoiceStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AttachDocument(Guid documentId)
    {
        DocumentId = documentId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddNotes(string notes)
    {
        Notes = notes;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetDueDate(DateTime dueDate)
    {
        DueDateUtc = dueDate;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
