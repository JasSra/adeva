namespace DebtManager.Contracts.Notifications;

public interface ISmsSender
{
    Task SendSmsAsync(string toPhoneE164, string message, CancellationToken ct = default);
}
