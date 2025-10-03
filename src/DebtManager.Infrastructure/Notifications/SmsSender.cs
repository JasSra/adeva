using DebtManager.Contracts.Notifications;
using Microsoft.Extensions.Configuration;

namespace DebtManager.Infrastructure.Notifications;

public class SmsSender(IConfiguration config) : ISmsSender
{
    public async Task SendSmsAsync(string toPhoneE164, string message, CancellationToken ct = default)
    {
        var accountSid = config["Notifications:Twilio:AccountSid"];
        var authToken = config["Notifications:Twilio:AuthToken"];
        var fromNumber = config["Notifications:Twilio:FromNumber"] ?? "+10000000000";
        if (string.IsNullOrWhiteSpace(accountSid) || string.IsNullOrWhiteSpace(authToken))
        {
            Console.WriteLine($"[SmsSender] To={toPhoneE164} MsgLen={message?.Length ?? 0}");
            await Task.CompletedTask;
            return;
        }
        // TODO: integrate Twilio SDK
        Console.WriteLine($"[SmsSender:SIMULATED] To={toPhoneE164} Msg='{message}'");
        await Task.CompletedTask;
    }
}
