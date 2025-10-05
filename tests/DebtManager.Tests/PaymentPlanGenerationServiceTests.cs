using DebtManager.Application.Payments;
using DebtManager.Contracts.AI;
using DebtManager.Contracts.Persistence;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using Microsoft.Extensions.Logging;
using Moq;

namespace DebtManager.Tests;

[TestFixture]
public class PaymentPlanGenerationServiceTests
{
    private Mock<IOrganizationRepository> _mockOrgRepo = null!;
    private Mock<IPaymentPlanAIService> _mockAIService = null!;
    private Mock<ILogger<PaymentPlanGenerationService>> _mockLogger = null!;
    private PaymentPlanGenerationService _service = null!;

    [SetUp]
    public void Setup()
    {
        _mockOrgRepo = new Mock<IOrganizationRepository>();
        _mockAIService = new Mock<IPaymentPlanAIService>();
        _mockLogger = new Mock<ILogger<PaymentPlanGenerationService>>();
        
        _service = new PaymentPlanGenerationService(
            _mockOrgRepo.Object,
            _mockLogger.Object,
            _mockAIService.Object);
    }

    [Test]
    public async Task GeneratePaymentPlanOptionsAsync_ReturnsThreeOptions()
    {
        // Arrange
        var organization = new Organization(
            "Test Org",
            "Test Legal Name",
            "12345678901",
            "AUD",
            "#FF0000",
            "#00FF00",
            "test@example.com",
            "1234567890",
            "UTC");

        var debt = new Debt(
            organization.Id,
            Guid.NewGuid(),
            5000m,
            "AUD",
            "ACC-001",
            "REF-001");

        _mockOrgRepo.Setup(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(organization);

        _mockAIService.Setup(ai => ai.OptimizeInstallmentScheduleAsync(
            It.IsAny<Debt>(),
            It.IsAny<int>(),
            It.IsAny<decimal>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InstallmentScheduleRecommendation
            {
                ConfidenceScore = 0.5m,
                Schedule = new List<InstallmentPreview>()
            });

        // Act
        var options = await _service.GeneratePaymentPlanOptionsAsync(debt);

        // Assert
        Assert.That(options, Is.Not.Null);
        Assert.That(options.Count, Is.EqualTo(3));
        
        var fullPaymentOption = options.FirstOrDefault(o => o.Type == PaymentPlanType.FullSettlement);
        Assert.That(fullPaymentOption, Is.Not.Null);
        Assert.That(fullPaymentOption!.DiscountAmount, Is.GreaterThan(0));
        Assert.That(fullPaymentOption.IsRecommended, Is.True);
        
        var systemOption = options.FirstOrDefault(o => o.Type == PaymentPlanType.SystemGenerated);
        Assert.That(systemOption, Is.Not.Null);
        Assert.That(systemOption!.DiscountAmount, Is.GreaterThan(0));
        
        var customOption = options.FirstOrDefault(o => o.Type == PaymentPlanType.Custom);
        Assert.That(customOption, Is.Not.Null);
        Assert.That(customOption!.RequiresApproval, Is.True);
    }
}
