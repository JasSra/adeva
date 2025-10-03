namespace DebtManager.Contracts.Notifications;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}
