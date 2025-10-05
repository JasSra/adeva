using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Hangfire;
using Hangfire.States;
using DebtManager.Contracts.Persistence;
using DebtManager.Contracts.Configuration;
using DebtManager.Contracts.Analytics;
using DebtManager.Infrastructure.Documents;
using DebtManager.Domain.Documents;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DebtManager.Tests;

[TestFixture]
public class InvoiceProcessingServiceTests
{
    private Mock<IDocumentRepository> _documentRepositoryMock = null!;
    private Mock<IInvoiceDataRepository> _invoiceDataRepositoryMock = null!;
    private Mock<IAppConfigService> _configServiceMock = null!;
    private Mock<IMetricService> _metricServiceMock = null!;
    private Mock<ILogger<AzureFormRecognizerInvoiceService>> _loggerMock = null!;
    private Mock<IBackgroundJobClient> _backgroundJobClientMock = null!;
    private AzureFormRecognizerInvoiceService _invoiceService = null!;

    [SetUp]
    public void Setup()
    {
        _documentRepositoryMock = new Mock<IDocumentRepository>();
        _invoiceDataRepositoryMock = new Mock<IInvoiceDataRepository>();
        _configServiceMock = new Mock<IAppConfigService>();
        _metricServiceMock = new Mock<IMetricService>();
        _loggerMock = new Mock<ILogger<AzureFormRecognizerInvoiceService>>();
        _backgroundJobClientMock = new Mock<IBackgroundJobClient>();

        _invoiceService = new AzureFormRecognizerInvoiceService(
            _documentRepositoryMock.Object,
            _invoiceDataRepositoryMock.Object,
            _configServiceMock.Object,
            _metricServiceMock.Object,
            _loggerMock.Object,
            _backgroundJobClientMock.Object
        );
    }

    [Test]
    public async Task ExtractInvoiceDataAsync_ReturnsError_WhenDocumentNotFound()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _documentRepositoryMock
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Document?)null);

        // Act
        var result = await _invoiceService.ExtractInvoiceDataAsync(documentId);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Document not found"));
    }

    [Test]
    public async Task ExtractInvoiceDataAsync_UsesStubExtraction_WhenAzureNotConfigured()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var document = new Document(
            "invoice.pdf",
            "application/pdf",
            1024,
            DocumentType.Invoice,
            "https://storage.example.com/invoice.pdf",
            null,
            Guid.NewGuid(),
            null
        );

        _documentRepositoryMock
            .Setup(r => r.GetAsync(documentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(document);

        _configServiceMock
            .Setup(c => c.GetAsync("AzureFormRecognizer:Endpoint", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        _configServiceMock
            .Setup(c => c.GetAsync("AzureFormRecognizer:ApiKey", It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _invoiceService.ExtractInvoiceDataAsync(documentId);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.InvoiceNumber, Is.Not.Null);
        Assert.That(result.TotalAmount, Is.GreaterThan(0));
        Assert.That(result.Currency, Is.EqualTo("AUD"));
        Assert.That(result.ConfidenceScore, Is.GreaterThan(0));

        // Verify metric was recorded
        _metricServiceMock.Verify(
            m => m.RecordMetricAsync(
                "invoice.extraction.success",
                It.IsAny<Domain.Analytics.MetricType>(),
                1,
                It.IsAny<string>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task QueueInvoiceProcessingAsync_CreatesInvoiceDataAndQueuesJob()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var invoiceDataId = Guid.NewGuid();

        _invoiceDataRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<InvoiceData>(), It.IsAny<CancellationToken>()))
            .Callback<InvoiceData, CancellationToken>((inv, ct) =>
            {
                // Simulate the entity getting an ID after being added
                typeof(InvoiceData).GetProperty("Id")!.SetValue(inv, invoiceDataId);
            })
            .Returns(Task.CompletedTask);

        _invoiceDataRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _invoiceService.QueueInvoiceProcessingAsync(documentId);

        // Assert
        Assert.That(result, Is.EqualTo(invoiceDataId));

        _invoiceDataRepositoryMock.Verify(
            r => r.AddAsync(It.Is<InvoiceData>(i => i.DocumentId == documentId), It.IsAny<CancellationToken>()),
            Times.Once);

        _invoiceDataRepositoryMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _backgroundJobClientMock.Verify(
            b => b.Create(It.IsAny<Hangfire.Common.Job>(), It.IsAny<IState>()),
            Times.Once);
    }
}

[TestFixture]
public class MetricServiceTests
{
    private Mock<IMetricRepository> _metricRepositoryMock = null!;
    private Infrastructure.Analytics.MetricService _metricService = null!;

    [SetUp]
    public void Setup()
    {
        _metricRepositoryMock = new Mock<IMetricRepository>();
        _metricService = new Infrastructure.Analytics.MetricService(_metricRepositoryMock.Object);
    }

    [Test]
    public async Task RecordMetricAsync_AddsMetricAndSaves()
    {
        // Arrange
        var key = "test.metric";
        var value = 42.5m;
        var organizationId = Guid.NewGuid();

        _metricRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Domain.Analytics.Metric>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _metricRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _metricService.RecordMetricAsync(key, Domain.Analytics.MetricType.Counter, value, null, organizationId);

        // Assert
        _metricRepositoryMock.Verify(
            r => r.AddAsync(
                It.Is<Domain.Analytics.Metric>(m => m.Key == key && m.Value == value && m.OrganizationId == organizationId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _metricRepositoryMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task GetAggregatedMetricsAsync_ReturnsAggregatedData()
    {
        // Arrange
        var fromUtc = DateTime.UtcNow.AddDays(-7);
        var toUtc = DateTime.UtcNow;
        var expectedData = new Dictionary<string, decimal>
        {
            { "metric1", 100m },
            { "metric2", 200m }
        };

        _metricRepositoryMock
            .Setup(r => r.GetAggregatedMetricsAsync(fromUtc, toUtc, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        // Act
        var result = await _metricService.GetAggregatedMetricsAsync(fromUtc, toUtc);

        // Assert
        Assert.That(result, Is.EqualTo(expectedData));
        Assert.That(result.Count, Is.EqualTo(2));
    }
}
