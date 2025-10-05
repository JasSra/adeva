using Hangfire;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Documents;

namespace DebtManager.Web.Jobs;

public static class NightlyJobs
{
    public static void ConfigureRecurringJobs()
    {
        RecurringJob.AddOrUpdate("nightly-reminders", () => SendReminders(), Cron.Daily);
        RecurringJob.AddOrUpdate("process-pending-invoices", () => ProcessPendingInvoices(), Cron.Hourly);
        RecurringJob.AddOrUpdate("retry-failed-invoices", () => RetryFailedInvoices(), Cron.Daily);
    }

    public static Task SendReminders()
    {
        // Placeholder: send SMS/email reminders
        Console.WriteLine($"[{DateTime.UtcNow:o}] Running nightly reminders...");
        return Task.CompletedTask;
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
