using DebtManager.Domain.Common;

namespace DebtManager.Domain.Communications;

public enum MessagePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}

public enum InternalMessageStatus
{
    Unread = 0,
    Read = 1,
    Archived = 2
}

/// <summary>
/// Represents an internal platform message for admins/users
/// </summary>
public class InternalMessage : Entity
{
    private readonly List<InternalMessageRecipient> _recipients;

    public string Title { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public MessagePriority Priority { get; private set; }
    public string? Category { get; private set; }
    public Guid? SenderId { get; private set; }
    public DateTime SentAtUtc { get; private set; }
    public string? RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public bool IsSystemGenerated { get; private set; }

    public IReadOnlyCollection<InternalMessageRecipient> Recipients => _recipients.AsReadOnly();

    private InternalMessage() 
    {
        _recipients = new List<InternalMessageRecipient>();
        SentAtUtc = DateTime.UtcNow;
        Priority = MessagePriority.Normal;
    }

    public InternalMessage(
        string title,
        string content,
        MessagePriority priority = MessagePriority.Normal,
        string? category = null,
        Guid? senderId = null,
        bool isSystemGenerated = true)
        : this()
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required", nameof(title));
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content is required", nameof(content));

        Title = title;
        Content = content;
        Priority = priority;
        Category = category;
        SenderId = senderId;
        IsSystemGenerated = isSystemGenerated;
    }

    public void AddRecipient(Guid userId)
    {
        if (_recipients.Any(r => r.UserId == userId))
            return;

        var recipient = new InternalMessageRecipient(Id, userId);
        _recipients.Add(recipient);
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetRelatedEntity(string entityType, Guid entityId)
    {
        RelatedEntityType = entityType;
        RelatedEntityId = entityId;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents a recipient of an internal message with their read status
/// </summary>
public class InternalMessageRecipient : Entity
{
    public Guid InternalMessageId { get; private set; }
    public Guid UserId { get; private set; }
    public InternalMessageStatus Status { get; private set; }
    public DateTime? ReadAtUtc { get; private set; }
    public DateTime? ArchivedAtUtc { get; private set; }

    public InternalMessage? InternalMessage { get; private set; }

    private InternalMessageRecipient() 
    {
        Status = InternalMessageStatus.Unread;
    }

    public InternalMessageRecipient(Guid internalMessageId, Guid userId)
        : this()
    {
        InternalMessageId = internalMessageId;
        UserId = userId;
    }

    public void MarkAsRead(DateTime? readAtUtc = null)
    {
        if (Status == InternalMessageStatus.Unread)
        {
            Status = InternalMessageStatus.Read;
            ReadAtUtc = readAtUtc ?? DateTime.UtcNow;
            UpdatedAtUtc = ReadAtUtc.Value;
        }
    }

    public void MarkAsArchived(DateTime? archivedAtUtc = null)
    {
        Status = InternalMessageStatus.Archived;
        ArchivedAtUtc = archivedAtUtc ?? DateTime.UtcNow;
        UpdatedAtUtc = ArchivedAtUtc.Value;
    }

    public void MarkAsUnread()
    {
        if (Status != InternalMessageStatus.Unread)
        {
            Status = InternalMessageStatus.Unread;
            ReadAtUtc = null;
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }
}
