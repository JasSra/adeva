using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using DomainPaymentMethod = DebtManager.Domain.Payments.PaymentMethod;

namespace DebtManager.Infrastructure.Payments;

public class StripePaymentService : IPaymentService
{
    private readonly IDebtRepository _debtRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripePaymentService> _logger;

    public StripePaymentService(
        IDebtRepository debtRepository,
        IConfiguration configuration,
        ILogger<StripePaymentService> logger)
    {
        _debtRepository = debtRepository;
        _configuration = configuration;
        _logger = logger;

        var apiKey = configuration["Stripe:SecretKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            StripeConfiguration.ApiKey = apiKey;
        }
    }

    public async Task<PaymentIntentResult> CreatePaymentIntentAsync(
        Guid debtId,
        decimal amount,
        string currency = "AUD",
        CancellationToken ct = default)
    {
        var debt = await _debtRepository.GetWithDetailsAsync(debtId, ct);
        if (debt == null)
        {
            throw new InvalidOperationException($"Debt {debtId} not found");
        }

        var amountInCents = (long)(amount * 100);

        var options = new PaymentIntentCreateOptions
        {
            Amount = amountInCents,
            Currency = currency.ToLowerInvariant(),
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
            },
            Metadata = new Dictionary<string, string>
            {
                { "debt_id", debtId.ToString() },
                { "debtor_id", debt.DebtorId.ToString() },
                { "organization_id", debt.OrganizationId.ToString() }
            },
            Description = $"Payment for debt #{debt.ClientReferenceNumber}",
        };

        var service = new PaymentIntentService();
        var intent = await service.CreateAsync(options, cancellationToken: ct);

        _logger.LogInformation(
            "Created Stripe payment intent {IntentId} for debt {DebtId}, amount {Amount} {Currency}",
            intent.Id, debtId, amount, currency);

        return new PaymentIntentResult
        {
            IntentId = intent.Id,
            ClientSecret = intent.ClientSecret,
            Amount = amount,
            Currency = currency,
            SupportedMethods = new[]
            {
                DomainPaymentMethod.Card,
                DomainPaymentMethod.BankTransfer
            }
        };
    }

    public Task<IReadOnlyList<PaymentMethodConfig>> GetAvailablePaymentMethodsAsync(
        CancellationToken ct = default)
    {
        var methods = new List<PaymentMethodConfig>
        {
            new()
            {
                Method = DomainPaymentMethod.Card,
                DisplayName = "Credit/Debit Card",
                IsEnabled = true,
                SupportsWallets = true
            },
            new()
            {
                Method = DomainPaymentMethod.BankTransfer,
                DisplayName = "Bank Transfer",
                IsEnabled = true,
                SupportsWallets = false
            }
        };

        return Task.FromResult<IReadOnlyList<PaymentMethodConfig>>(methods);
    }

    public async Task<bool> ConfirmPaymentAsync(
        string paymentIntentId,
        CancellationToken ct = default)
    {
        var service = new PaymentIntentService();
        var intent = await service.GetAsync(paymentIntentId, cancellationToken: ct);
        
        return intent.Status == "succeeded";
    }

    public async Task<string> GetPaymentStatusAsync(
        string paymentIntentId,
        CancellationToken ct = default)
    {
        var service = new PaymentIntentService();
        var intent = await service.GetAsync(paymentIntentId, cancellationToken: ct);
        
        return intent.Status;
    }
}
