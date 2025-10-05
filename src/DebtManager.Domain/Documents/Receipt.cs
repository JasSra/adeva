using DebtManager.Domain.Common;
using DebtManager.Domain.Payments;

namespace DebtManager.Domain.Documents;

public class Receipt : Entity
{
    public Guid TransactionId { get; private set; }
    public Guid DebtorId { get; private set; }
    public Guid? OrganizationId { get; private set; }
    public string ReceiptNumber { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }
    public PaymentMethod PaymentMethod { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public Guid? DocumentId { get; private set; }
    public string? GeneratedByUserId { get; private set; }
    public DateTime? EmailSentAtUtc { get; private set; }
    public string? EmailSentTo { get; private set; }

    public Transaction? Transaction { get; private set; }
    public Document? Document { get; private set; }

    private Receipt()
    {
        ReceiptNumber = string.Empty;
        Currency = "AUD";
        IssuedAtUtc = DateTime.UtcNow;
    }

    public Receipt(
        Guid transactionId,
        Guid debtorId,
        Guid? organizationId,
        string receiptNumber,
        decimal amount,
        string currency,
        PaymentMethod paymentMethod,
        string? referenceNumber = null)
        : this()
    {
        TransactionId = transactionId;
        DebtorId = debtorId;
        OrganizationId = organizationId;
        ReceiptNumber = receiptNumber;
        Amount = amount;
        Currency = currency;
        PaymentMethod = paymentMethod;
        ReferenceNumber = referenceNumber;
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

    public void SetGeneratedBy(string userId)
    {
        GeneratedByUserId = userId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkEmailSent(string emailAddress, DateTime? sentAtUtc = null)
    {
        EmailSentTo = emailAddress;
        EmailSentAtUtc = sentAtUtc ?? DateTime.UtcNow;
        UpdatedAtUtc = EmailSentAtUtc;
    }
}
