using DebtManager.Contracts.Notifications;
using DebtManager.Contracts.Configuration;

namespace DebtManager.Infrastructure.Notifications;

public class SmsSender(IAppConfigService config) : ISmsSender
{
    public async Task SendSmsAsync(string toPhoneE164, string message, CancellationToken ct = default)
    {
        var accountSid = await config.GetAsync("Twilio:AccountSid", ct);
        var authToken = await config.GetAsync("Twilio:AuthToken", ct);
        var fromNumber = await config.GetAsync("Twilio:FromNumber", ct) ?? "+10000000000";
        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            Console.WriteLine($"[SmsSender] To={toPhoneE164} MsgLen={message?.Length ?? 0}");
            await Task.CompletedTask;
            return;
        }
        // TODO: integrate Twilio SDK
        Console.WriteLine($"[SmsSender:SIMULATED] To={toPhoneE164} Msg='{message}' From={fromNumber}");
        await Task.CompletedTask;
    }
}
