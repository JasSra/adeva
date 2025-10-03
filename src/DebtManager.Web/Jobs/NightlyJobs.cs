using Hangfire;

namespace DebtManager.Web.Jobs;

public static class NightlyJobs
{
    public static void ConfigureRecurringJobs()
    {
        RecurringJob.AddOrUpdate("nightly-reminders", () => SendReminders(), Cron.Daily);
    }

    public static Task SendReminders()
    {
        // Placeholder: send SMS/email reminders
        Console.WriteLine($"[{DateTime.UtcNow:o}] Running nightly reminders...");
        return Task.CompletedTask;
    }
}
