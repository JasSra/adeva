using DebtManager.Contracts.Notifications;
using DebtManager.Domain.Communications;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DebtManager.Web.Jobs;

public class MessageDispatchJob
{
    private readonly AppDbContext _db;
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly ILogger<MessageDispatchJob> _logger;

    public MessageDispatchJob(AppDbContext db, IEmailSender email, ISmsSender sms, ILogger<MessageDispatchJob> logger)
    {
        _db = db;
        _email = email;
        _sms = sms;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        const int batchSize = 50;
        const int maxRetries = 3;

        var pending = await _db.QueuedMessages
            .Where(m => m.Status == QueuedMessageStatus.Pending || (m.Status == QueuedMessageStatus.Failed && m.RetryCount < maxRetries))
            .OrderBy(m => m.QueuedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        if (pending.Count == 0) return;

        foreach (var msg in pending)
        {
            try
            {
                msg.MarkAsProcessing();
                await _db.SaveChangesAsync(ct);

                switch (msg.Channel)
                {
                    case MessageChannel.Email:
                        await _email.SendEmailAsync(msg.RecipientEmail, msg.Subject, msg.Body, ct);
                        msg.MarkAsSent();
                        break;
                    case MessageChannel.Sms:
                        if (string.IsNullOrWhiteSpace(msg.RecipientPhone))
                        {
                            msg.MarkAsFailed("Missing phone number");
                        }
                        else
                        {
                            await _sms.SendSmsAsync(msg.RecipientPhone, msg.Body, ct);
                            msg.MarkAsSent();
                        }
                        break;
                    default:
                        msg.MarkAsFailed($"Unsupported channel {msg.Channel}");
                        break;
                }

                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to dispatch message {Id}", msg.Id);
                msg.MarkAsFailed(ex.Message);
                await _db.SaveChangesAsync(ct);
            }
        }
    }
}
