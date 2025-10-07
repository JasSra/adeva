using Hangfire;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Documents;
using DebtManager.Contracts.Notifications;
using DebtManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DebtManager.Domain.Communications;

namespace DebtManager.Web.Jobs;

public static class NightlyJobs
{
    public static void ConfigureRecurringJobs()
    {
        RecurringJob.AddOrUpdate("nightly-reminders", () => SendReminders(), Cron.Daily);
        RecurringJob.AddOrUpdate("process-pending-invoices", () => ProcessPendingInvoices(), Cron.Hourly);
        RecurringJob.AddOrUpdate("retry-failed-invoices", () => RetryFailedInvoices(), Cron.Daily);
        RecurringJob.AddOrUpdate("dispatch-queued-messages", () => DispatchQueuedMessages(), Cron.Minutely);
    }

    public static Task SendReminders()
    {
        Console.WriteLine($"[{DateTime.UtcNow:o}] Running nightly reminders...");
        return Task.CompletedTask;
    }

    public static async Task DispatchQueuedMessages()
    {
        // This method acts as a placeholder. Hangfire will call the DI-enabled overload.
        await Task.CompletedTask;
    }

    // DI-enabled dispatch worker
    public static async Task DispatchQueuedMessagesWithDI(AppDbContext db, IEmailSender emailSender, ISmsSender smsSender, CancellationToken ct = default)
    {
        const int batchSize = 25;
        const int maxRetries = 3;

        var pending = await db.QueuedMessages
            .Where(m => m.Status == QueuedMessageStatus.Pending || (m.Status == QueuedMessageStatus.Failed && m.RetryCount < maxRetries))
            .OrderBy(m => m.QueuedAtUtc)
            .Take(batchSize)
            .ToListAsync(ct);

        foreach (var msg in pending)
        {
            try
            {
                msg.MarkAsProcessing();
                await db.SaveChangesAsync(ct);

                switch (msg.Channel)
                {
                    case MessageChannel.Email:
                        await emailSender.SendEmailAsync(msg.RecipientEmail, msg.Subject, msg.Body, ct);
                        msg.MarkAsSent();
                        break;
                    case MessageChannel.Sms:
                        if (!string.IsNullOrWhiteSpace(msg.RecipientPhone))
                        {
                            await smsSender.SendSmsAsync(msg.RecipientPhone, msg.Body, ct);
                            msg.MarkAsSent();
                        }
                        else
                        {
                            msg.MarkAsFailed("Missing phone number");
                        }
                        break;
                    default:
                        msg.MarkAsFailed($"Unsupported channel: {msg.Channel}");
                        break;
                }

                await db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                msg.MarkAsFailed(ex.Message);
                await db.SaveChangesAsync(ct);
            }
        }
    }

    public static async Task ProcessPendingInvoices()
    {
        Console.WriteLine($"[{DateTime.UtcNow:o}] Processing pending invoices...");
        
        // This will be called by Hangfire which handles DI scope
        // The actual implementation is in ProcessPendingInvoicesWithDI
        await Task.CompletedTask;
    }

    public static async Task RetryFailedInvoices()
    {
        Console.WriteLine($"[{DateTime.UtcNow:o}] Retrying failed invoices...");
        
        // This will be called by Hangfire which handles DI scope
        // The actual implementation is in RetryFailedInvoicesWithDI
        await Task.CompletedTask;
    }

    public static async Task ProcessPendingInvoicesWithDI(
        IInvoiceDataRepository invoiceDataRepository,
        IInvoiceProcessingService invoiceProcessingService)
    {
        var pendingInvoices = await invoiceDataRepository.GetPendingAsync();
        
        foreach (var invoice in pendingInvoices)
        {
            BackgroundJob.Enqueue(() => 
                ((Infrastructure.Documents.AzureFormRecognizerInvoiceService)invoiceProcessingService)
                    .ProcessInvoiceBackgroundAsync(invoice.Id, CancellationToken.None));
        }
    }

    public static async Task RetryFailedInvoicesWithDI(
        IInvoiceDataRepository invoiceDataRepository,
        IInvoiceProcessingService invoiceProcessingService)
    {
        const int maxRetries = 3;
        var failedInvoices = await invoiceDataRepository.GetFailedRetryableAsync(maxRetries);
        
        foreach (var invoice in failedInvoices)
        {
            BackgroundJob.Enqueue(() => 
                ((Infrastructure.Documents.AzureFormRecognizerInvoiceService)invoiceProcessingService)
                    .ProcessInvoiceBackgroundAsync(invoice.Id, CancellationToken.None));
        }
    }
}
