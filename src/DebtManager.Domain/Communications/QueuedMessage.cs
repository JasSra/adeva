using DebtManager.Domain.Common;

namespace DebtManager.Domain.Communications;

public enum QueuedMessageStatus
{
    Pending = 0,
    Processing = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// Represents a message queued for delivery (email, SMS, etc.)
/// </summary>
public class QueuedMessage : Entity
{
    public string RecipientEmail { get; private set; } = string.Empty;
    public string? RecipientPhone { get; private set; }
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public MessageChannel Channel { get; private set; }
    public QueuedMessageStatus Status { get; private set; }
    public DateTime QueuedAtUtc { get; private set; }
    public DateTime? SentAtUtc { get; private set; }
    public DateTime? FailedAtUtc { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public string? ProviderMessageId { get; private set; }

    private QueuedMessage() 
    {
        QueuedAtUtc = DateTime.UtcNow;
        Status = QueuedMessageStatus.Pending;
    }

    public QueuedMessage(
        string recipientEmail,
        string subject,
        string body,
        MessageChannel channel,
        string? relatedEntityType = null,
        Guid? relatedEntityId = null,
        string? recipientPhone = null)
        : this()
    {
        if (string.IsNullOrWhiteSpace(recipientEmail) && string.IsNullOrWhiteSpace(recipientPhone))
            throw new ArgumentException("Either email or phone must be provided");

        RecipientEmail = recipientEmail ?? string.Empty;
        RecipientPhone = recipientPhone;
        Subject = subject;
        Body = body;
        Channel = channel;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
    }

    public void MarkAsProcessing()
    {
        Status = QueuedMessageStatus.Processing;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void MarkAsSent(DateTime? sentAtUtc = null, string? providerMessageId = null)
    {
        Status = QueuedMessageStatus.Sent;
        SentAtUtc = sentAtUtc ?? DateTime.UtcNow;
        ProviderMessageId = providerMessageId;
        UpdatedAtUtc = SentAtUtc.Value;
    }

    public void MarkAsFailed(string errorMessage, DateTime? failedAtUtc = null)
    {
        Status = QueuedMessageStatus.Failed;
        ErrorMessage = errorMessage;
        FailedAtUtc = failedAtUtc ?? DateTime.UtcNow;
        RetryCount++;
        UpdatedAtUtc = FailedAtUtc.Value;
    }

    public void MarkAsCancelled()
    {
        Status = QueuedMessageStatus.Cancelled;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public bool CanRetry(int maxRetries = 3)
    {
        return Status == QueuedMessageStatus.Failed && RetryCount < maxRetries;
    }
}
