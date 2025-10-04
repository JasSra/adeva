using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Payments;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using DomainPaymentMethod = DebtManager.Domain.Payments.PaymentMethod;

namespace DebtManager.Infrastructure.Payments;

public class StripeWebhookProcessor : IWebhookProcessor
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<StripeWebhookProcessor> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public StripeWebhookProcessor(
        IConfiguration configuration,
        ILogger<StripeWebhookProcessor> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _configuration = configuration;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    public async Task ProcessStripeWebhookAsync(string payload, string signature, CancellationToken ct = default)
    {
        var webhookSecret = _configuration["Stripe:WebhookSecret"];
        
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            _logger.LogWarning("Stripe webhook secret not configured");
            throw new InvalidOperationException("Stripe webhook secret not configured");
        }

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload,
                signature,
                webhookSecret
            );
            
            _logger.LogInformation("Received Stripe webhook: {EventType} - {EventId}", 
                stripeEvent.Type, stripeEvent.Id);
        }
        catch (StripeException e)
        {
            _logger.LogError(e, "Failed to validate Stripe webhook signature");
            throw;
        }

        // Queue job for background processing
        _backgroundJobClient.Enqueue<PaymentWebhookJob>(
            job => job.ProcessPaymentEventAsync(stripeEvent.Id, stripeEvent.Type, payload, CancellationToken.None));

        await Task.CompletedTask;
    }
}

public class PaymentWebhookJob
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IDebtRepository _debtRepository;
    private readonly ILogger<PaymentWebhookJob> _logger;

    public PaymentWebhookJob(
        ITransactionRepository transactionRepository,
        IDebtRepository debtRepository,
        ILogger<PaymentWebhookJob> logger)
    {
        _transactionRepository = transactionRepository;
        _debtRepository = debtRepository;
        _logger = logger;
    }

    public async Task ProcessPaymentEventAsync(
        string eventId,
        string eventType,
        string payload,
        CancellationToken ct)
    {
        _logger.LogInformation("Processing payment webhook event {EventId} of type {EventType}", 
            eventId, eventType);

        try
        {
            var stripeEvent = Newtonsoft.Json.JsonConvert.DeserializeObject<Event>(payload);
            if (stripeEvent == null)
            {
                _logger.LogWarning("Failed to deserialize Stripe event {EventId}", eventId);
                return;
            }

            switch (eventType)
            {
                case "payment_intent.succeeded":
                    await HandlePaymentSucceededAsync(stripeEvent, ct);
                    break;

                case "payment_intent.payment_failed":
                    await HandlePaymentFailedAsync(stripeEvent, ct);
                    break;

                case "payment_intent.canceled":
                    await HandlePaymentCanceledAsync(stripeEvent, ct);
                    break;

                default:
                    _logger.LogInformation("Unhandled event type: {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook event {EventId}", eventId);
            throw;
        }
    }

    private async Task HandlePaymentSucceededAsync(Event stripeEvent, CancellationToken ct)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            _logger.LogWarning("PaymentIntent not found in event data");
            return;
        }

        if (!paymentIntent.Metadata.TryGetValue("debt_id", out var debtIdStr) ||
            !Guid.TryParse(debtIdStr, out var debtId))
        {
            _logger.LogWarning("Debt ID not found in payment intent metadata");
            return;
        }

        var debt = await _debtRepository.GetWithDetailsAsync(debtId, ct);
        if (debt == null)
        {
            _logger.LogWarning("Debt {DebtId} not found", debtId);
            return;
        }

        // Check if transaction already exists
        var existingTx = await _transactionRepository.GetByProviderReferenceAsync(paymentIntent.Id, ct);
        if (existingTx != null)
        {
            _logger.LogInformation("Transaction already exists for payment intent {IntentId}", paymentIntent.Id);
            
            if (existingTx.Status != TransactionStatus.Succeeded)
            {
                existingTx.MarkSettled(DateTime.UtcNow, paymentIntent.Id);
                await _transactionRepository.SaveChangesAsync(ct);
            }
            return;
        }

        // Create new transaction
        var amount = paymentIntent.Amount / 100m;
        var transaction = new Transaction(
            debtId: debtId,
            debtorId: debt.DebtorId,
            paymentPlanId: null,
            paymentInstallmentId: null,
            amount: amount,
            currency: paymentIntent.Currency.ToUpperInvariant(),
            direction: TransactionDirection.Inbound,
            method: DomainPaymentMethod.Card,
            provider: "Stripe",
            providerRef: paymentIntent.Id
        );

        transaction.MarkSettled(DateTime.UtcNow, paymentIntent.Id);
        await _transactionRepository.AddAsync(transaction, ct);

        // Update debt
        debt.ApplyPayment(amount, DateTime.UtcNow);
        
        if (debt.OutstandingPrincipal <= 0)
        {
            debt.SetStatus(Domain.Debts.DebtStatus.Settled, "Fully paid via Stripe");
        }

        await _transactionRepository.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Payment succeeded for debt {DebtId}, amount {Amount} {Currency}, transaction {TxId}",
            debtId, amount, transaction.Currency, transaction.Id);
    }

    private async Task HandlePaymentFailedAsync(Event stripeEvent, CancellationToken ct)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        if (!paymentIntent.Metadata.TryGetValue("debt_id", out var debtIdStr) ||
            !Guid.TryParse(debtIdStr, out var debtId))
        {
            return;
        }

        var debt = await _debtRepository.GetWithDetailsAsync(debtId, ct);
        if (debt == null) return;

        var existingTx = await _transactionRepository.GetByProviderReferenceAsync(paymentIntent.Id, ct);
        if (existingTx != null)
        {
            existingTx.MarkFailed(paymentIntent.LastPaymentError?.Message ?? "Payment failed");
            await _transactionRepository.SaveChangesAsync(ct);
        }
        else
        {
            var amount = paymentIntent.Amount / 100m;
            var transaction = new Transaction(
                debtId: debtId,
                debtorId: debt.DebtorId,
                paymentPlanId: null,
                paymentInstallmentId: null,
                amount: amount,
                currency: paymentIntent.Currency.ToUpperInvariant(),
                direction: TransactionDirection.Inbound,
                method: DomainPaymentMethod.Card,
                provider: "Stripe",
                providerRef: paymentIntent.Id
            );

            transaction.MarkFailed(paymentIntent.LastPaymentError?.Message ?? "Payment failed");
            await _transactionRepository.AddAsync(transaction, ct);
            await _transactionRepository.SaveChangesAsync(ct);
        }

        _logger.LogWarning("Payment failed for debt {DebtId}, intent {IntentId}", debtId, paymentIntent.Id);
    }

    private async Task HandlePaymentCanceledAsync(Event stripeEvent, CancellationToken ct)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null) return;

        if (!paymentIntent.Metadata.TryGetValue("debt_id", out var debtIdStr) ||
            !Guid.TryParse(debtIdStr, out var debtId))
        {
            return;
        }

        var existingTx = await _transactionRepository.GetByProviderReferenceAsync(paymentIntent.Id, ct);
        if (existingTx != null)
        {
            existingTx.Cancel("Payment canceled by user or system");
            await _transactionRepository.SaveChangesAsync(ct);
        }

        _logger.LogInformation("Payment canceled for debt {DebtId}, intent {IntentId}", debtId, paymentIntent.Id);
    }
}
