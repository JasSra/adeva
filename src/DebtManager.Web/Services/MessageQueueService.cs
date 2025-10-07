using DebtManager.Domain.Communications;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Services;

public interface IMessageQueueService
{
    Task QueueInternalAsync(string title, string content, IEnumerable<Guid> recipientUserIds, MessagePriority priority = MessagePriority.Normal, string? category = null, string? relatedEntityType = null, Guid? relatedEntityId = null, Guid? senderId = null, bool systemGenerated = false, CancellationToken ct = default);
    Task QueueEmailAsync(string subject, string body, IEnumerable<string> recipientEmails, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default);
    Task QueueSmsAsync(string body, IEnumerable<string> recipientPhones, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default);
}

public class MessageQueueService : IMessageQueueService
{
    private readonly AppDbContext _db;

    public MessageQueueService(AppDbContext db)
    {
        _db = db;
    }

    public async Task QueueInternalAsync(string title, string content, IEnumerable<Guid> recipientUserIds, MessagePriority priority = MessagePriority.Normal, string? category = null, string? relatedEntityType = null, Guid? relatedEntityId = null, Guid? senderId = null, bool systemGenerated = false, CancellationToken ct = default)
    {
        var msg = new InternalMessage(title, content, priority, category, senderId, isSystemGenerated: systemGenerated);
        if (!string.IsNullOrWhiteSpace(relatedEntityType) && relatedEntityId.HasValue)
        {
            msg.SetRelatedEntity(relatedEntityType!, relatedEntityId.Value);
        }
        foreach (var uid in recipientUserIds.Distinct())
        {
            msg.AddRecipient(uid);
        }
        _db.Set<InternalMessage>().Add(msg);
        await _db.SaveChangesAsync(ct);
    }

    public async Task QueueEmailAsync(string subject, string body, IEnumerable<string> recipientEmails, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default)
    {
        foreach (var email in recipientEmails.Where(e => !string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var queued = new QueuedMessage(
                recipientEmail: email.Trim(),
                subject: subject,
                body: body,
                channel: MessageChannel.Email,
                relatedEntityType: relatedEntityType,
                relatedEntityId: relatedEntityId
            );
            _db.Set<QueuedMessage>().Add(queued);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task QueueSmsAsync(string body, IEnumerable<string> recipientPhones, string? relatedEntityType = null, Guid? relatedEntityId = null, CancellationToken ct = default)
    {
        foreach (var phone in recipientPhones.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct())
        {
            var queued = new QueuedMessage(
                recipientEmail: string.Empty,
                subject: string.Empty,
                body: body,
                channel: MessageChannel.Sms,
                relatedEntityType: relatedEntityType,
                relatedEntityId: relatedEntityId,
                recipientPhone: phone.Trim()
            );
            _db.Set<QueuedMessage>().Add(queued);
        }
        await _db.SaveChangesAsync(ct);
    }
}
