using DebtManager.Domain.Communications;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net.Mail;

namespace DebtManager.Web.Jobs;

public class QueuedMessageDispatcher
{
    private readonly AppDbContext _db;
    private readonly ILogger<QueuedMessageDispatcher> _logger;

    public QueuedMessageDispatcher(AppDbContext db, ILogger<QueuedMessageDispatcher> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Hangfire entry: enqueue or recurring call
    public async Task ProcessAsync(CancellationToken ct = default)
    {
        var batch = await _db.QueuedMessages
            .Where(m => m.Status == QueuedMessageStatus.Pending)
            .OrderBy(m => m.QueuedAtUtc)
            .Take(50)
            .ToListAsync(ct);

        foreach (var msg in batch)
        {
            try
            {
                msg.MarkAsProcessing();
                await _db.SaveChangesAsync(ct);

                switch (msg.Channel)
                {
                    case MessageChannel.Email:
                        await SendEmailAsync(msg, ct);
                        break;
                    default:
                        // mark as failed for unsupported channel
                        msg.MarkAsFailed($"Unsupported channel: {msg.Channel}");
                        break;
                }

                if (msg.Status == QueuedMessageStatus.Processing)
                {
                    msg.MarkAsSent();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queued message {Id}", msg.Id);
                msg.MarkAsFailed(ex.Message);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private Task SendEmailAsync(QueuedMessage msg, CancellationToken ct)
    {
        // placeholder delivery; integrate with provider in infrastructure service
        _logger.LogInformation("[EmailQueue] To={To} Subject={Subject} Len={Len}", msg.RecipientEmail, msg.Subject, msg.Body?.Length ?? 0);
        // basic format check
        try { var _ = new MailAddress(msg.RecipientEmail); } catch { throw new Exception("Invalid recipient email"); }
        return Task.CompletedTask;
    }
}
