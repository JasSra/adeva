namespace DebtManager.Contracts.Payments;

public interface IWebhookProcessor
{
    /// <summary>
    /// Processes a Stripe webhook event
    /// </summary>
    Task ProcessStripeWebhookAsync(string payload, string signature, CancellationToken ct = default);
}
