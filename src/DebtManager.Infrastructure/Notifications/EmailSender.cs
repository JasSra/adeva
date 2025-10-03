using DebtManager.Contracts.Notifications;
using Microsoft.Extensions.Configuration;

namespace DebtManager.Infrastructure.Notifications;

public class EmailSender(IConfiguration config) : IEmailSender
{
    public async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default)
    {
        var apiKey = config["Notifications:SendGridApiKey"];
        var from = config["Notifications:FromEmail"] ?? "no-reply@example.com";
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            // Safe fallback: log to console for dev
            Console.WriteLine($"[EmailSender] To={toEmail} Subject={subject} BodyLen={body?.Length ?? 0}");
            await Task.CompletedTask;
            return;
        }
        // TODO: integrate SendGrid SDK
        Console.WriteLine($"[EmailSender:SIMULATED] To={toEmail} Subject={subject}");
        await Task.CompletedTask;
    }
}
