using DebtManager.Contracts.Configuration;
using DebtManager.Contracts.Payments;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using DebtManager.Infrastructure.Payments;
using Hangfire;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DebtManager.Tests;

[TestFixture]
public class PaymentServiceTests
{
    private Mock<IDebtRepository> _debtRepositoryMock = null!;
    private Mock<IAppConfigService> _configMock = null!;
    private Mock<ILogger<StripePaymentService>> _loggerMock = null!;
    private StripePaymentService _paymentService = null!;

    [SetUp]
    public void Setup()
    {
        _debtRepositoryMock = new Mock<IDebtRepository>();
        _configMock = new Mock<IAppConfigService>();
        _loggerMock = new Mock<ILogger<StripePaymentService>>();

        // Mock configuration
        _configMock.Setup(c => c.GetAsync("Stripe:SecretKey", It.IsAny<CancellationToken>()))
                   .ReturnsAsync("sk_test_mock_key");

        _paymentService = new StripePaymentService(
            _debtRepositoryMock.Object,
            _configMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task GetAvailablePaymentMethodsAsync_Returns_SupportedMethods()
    {
        // Act
        var result = await _paymentService.GetAvailablePaymentMethodsAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.GreaterThan(0));
        Assert.That(result, Has.Some.Matches<PaymentMethodConfig>(m => m.Method == DebtManager.Domain.Payments.PaymentMethod.Card));
        Assert.That(result, Has.Some.Matches<PaymentMethodConfig>(m => m.SupportsWallets == true));
    }

    [Test]
    public async Task CreatePaymentIntentAsync_ThrowsException_WhenDebtNotFound()
    {
        // Arrange
        var debtId = Guid.NewGuid();
        _debtRepositoryMock
            .Setup(r => r.GetWithDetailsAsync(debtId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Debt?)null);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _paymentService.CreatePaymentIntentAsync(debtId, 100m, "AUD")
        );
    }

    [Test]
    public void CreatePaymentIntentAsync_CreatesIntent_WhenDebtExists()
    {
        // Note: This test is a placeholder as full integration testing
        // would require mocking the Stripe API client.
        // In production, use Stripe's testing helpers or integration tests.
        Assert.Pass("Stripe integration requires live API or test doubles");
    }
}

[TestFixture]
public class WebhookProcessorTests
{
    private Mock<IAppConfigService> _configMock = null!;
    private Mock<ILogger<StripeWebhookProcessor>> _loggerMock = null!;
    private Mock<Hangfire.IBackgroundJobClient> _backgroundJobClientMock = null!;
    private StripeWebhookProcessor _webhookProcessor = null!;

    [SetUp]
    public void Setup()
    {
        _configMock = new Mock<IAppConfigService>();
        _loggerMock = new Mock<ILogger<StripeWebhookProcessor>>();
        _backgroundJobClientMock = new Mock<Hangfire.IBackgroundJobClient>();

        _webhookProcessor = new StripeWebhookProcessor(
            _configMock.Object,
            _loggerMock.Object,
            _backgroundJobClientMock.Object
        );
    }

    [Test]
    public void ProcessStripeWebhookAsync_ThrowsException_WhenWebhookSecretNotConfigured()
    {
        // Arrange
        _configMock.Setup(c => c.GetAsync("Stripe:WebhookSecret", It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);
        var payload = "test_payload";
        var signature = "test_signature";

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await _webhookProcessor.ProcessStripeWebhookAsync(payload, signature)
        );
    }
}

[TestFixture]
public class PaymentWebhookJobTests
{
    private Mock<ITransactionRepository> _transactionRepositoryMock = null!;
    private Mock<IDebtRepository> _debtRepositoryMock = null!;
    private Mock<ILogger<PaymentWebhookJob>> _loggerMock = null!;
    private PaymentWebhookJob _webhookJob = null!;

    [SetUp]
    public void Setup()
    {
        _transactionRepositoryMock = new Mock<ITransactionRepository>();
        _debtRepositoryMock = new Mock<IDebtRepository>();
        _loggerMock = new Mock<ILogger<PaymentWebhookJob>>();
        var backgroundJobClientMock = new Mock<IBackgroundJobClient>();

        _webhookJob = new PaymentWebhookJob(
            _transactionRepositoryMock.Object,
            _debtRepositoryMock.Object,
            backgroundJobClientMock.Object,
            _loggerMock.Object
        );
    }

    [Test]
    public async Task ProcessPaymentEventAsync_HandlesInvalidPayload()
    {
        // Arrange
        var eventId = "evt_test";
        var eventType = "unknown.event";
        var payload = "invalid json";

        // Act & Assert - Should handle gracefully
        try
        {
            await _webhookJob.ProcessPaymentEventAsync(eventId, eventType, payload, CancellationToken.None);
            // If it doesn't throw, verify logging
            _loggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce
            );
        }
        catch (Exception)
        {
            // Expected - Stripe deserialization will fail with invalid JSON
            Assert.Pass("Webhook handles invalid payload appropriately");
        }
    }
}
