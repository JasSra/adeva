using DebtManager.Domain.Payments;

namespace DebtManager.Contracts.Payments;

public class PaymentIntentResult
{
    public required string IntentId { get; set; }
    public required string ClientSecret { get; set; }
    public required decimal Amount { get; set; }
    public required string Currency { get; set; }
    public PaymentMethod[] SupportedMethods { get; set; } = Array.Empty<PaymentMethod>();
}

public class PaymentMethodConfig
{
    public PaymentMethod Method { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool SupportsWallets { get; set; }
}

public interface IPaymentService
{
    /// <summary>
    /// Creates a Stripe payment intent for a debt payment
    /// </summary>
    Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid debtId, 
        decimal amount, 
        string currency = "AUD",
        CancellationToken ct = default);

    /// <summary>
    /// Gets available payment methods for the tenant
    /// </summary>
    Task<IReadOnlyList<PaymentMethodConfig>> GetAvailablePaymentMethodsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Confirms a payment intent
    /// </summary>
    Task<bool> ConfirmPaymentAsync(
        string paymentIntentId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves payment status
    /// </summary>
    Task<string> GetPaymentStatusAsync(
        string paymentIntentId,
        CancellationToken ct = default);
}
