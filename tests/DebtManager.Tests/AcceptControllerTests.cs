using DebtManager.Domain.Debts;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using DebtManager.Infrastructure.Identity;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Areas.User.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace DebtManager.Tests;

[TestFixture]
public class AcceptControllerTests
{
    private AppDbContext _dbContext = null!;
    private Mock<ILogger<AcceptController>> _loggerMock = null!;
    private AcceptController _controller = null!;

    [SetUp]
    public void Setup()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _dbContext = new AppDbContext(options);

        _loggerMock = new Mock<ILogger<AcceptController>>();
        _controller = new AcceptController(_dbContext, _loggerMock.Object);

        // Setup HttpContext with a test user
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("oid", "test-external-id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public void Index_WithoutId_RedirectsToUserWithError()
    {
        // Act
        var result = _controller.Index() as RedirectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Url, Is.EqualTo("/User"));
        Assert.That(_controller.TempData["Error"], Is.Not.Null);
    }

    [Test]
    public async Task Index_WithInvalidDebtId_ReturnsNotFound()
    {
        // Arrange
        var invalidId = Guid.NewGuid();

        // Act
        var result = await _controller.Index(invalidId, CancellationToken.None);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    public async Task Index_WithSettledDebt_RedirectsWithError()
    {
        // Arrange
        var org = Organization.CreatePending(
            "Test Org", "Test Legal", "12345678901", "AUD", 
            "#000000", "#ffffff", "test@test.com", "+1234567890", "UTC"
        );
        await _dbContext.Organizations.AddAsync(org);

        var debtor = new Debtor(
            org.Id, "DEBTOR-001", "john@test.com", "+1234567890", 
            "John", "Doe"
        );
        debtor.UpdateAddress("123 Test St", null, "Test City", "TS", "12345", "AU");
        await _dbContext.Debtors.AddAsync(debtor);

        var debt = new Debt(
            org.Id, debtor.Id, 1000m, "AUD", "ACC-001", "REF-001"
        );
        debt.SetStatus(DebtStatus.Settled);
        await _dbContext.Debts.AddAsync(debt);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Index(debt.Id, CancellationToken.None) as RedirectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Url, Is.EqualTo("/User"));
        Assert.That(_controller.TempData["Error"], Is.EqualTo("This debt is not available for acceptance."));
    }

    [Test]
    public async Task Index_WithDisputedDebt_RedirectsWithError()
    {
        // Arrange
        var org = Organization.CreatePending(
            "Test Org", "Test Legal", "12345678901", "AUD", 
            "#000000", "#ffffff", "test@test.com", "+1234567890", "UTC"
        );
        await _dbContext.Organizations.AddAsync(org);

        var debtor = new Debtor(
            org.Id, "DEBTOR-001", "john@test.com", "+1234567890", 
            "John", "Doe"
        );
        debtor.UpdateAddress("123 Test St", null, "Test City", "TS", "12345", "AU");
        await _dbContext.Debtors.AddAsync(debtor);

        var debt = new Debt(
            org.Id, debtor.Id, 1000m, "AUD", "ACC-001", "REF-001"
        );
        debt.FlagDispute("Test dispute");
        await _dbContext.Debts.AddAsync(debt);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.Index(debt.Id, CancellationToken.None) as RedirectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Url, Is.EqualTo("/User"));
        Assert.That(_controller.TempData["Error"], Is.EqualTo("This debt is not available for acceptance."));
    }

    [Test]
    public async Task Post_WithDisputeOption_CreatesDispute()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com",
            ExternalAuthId = "test-external-id"
        };
        await _dbContext.Users.AddAsync(user);

        var org = Organization.CreatePending(
            "Test Org", "Test Legal", "12345678901", "AUD", 
            "#000000", "#ffffff", "test@test.com", "+1234567890", "UTC"
        );
        await _dbContext.Organizations.AddAsync(org);

        var debtor = new Debtor(
            org.Id, "DEBTOR-001", "john@test.com", "+1234567890", 
            "John", "Doe"
        );
        debtor.UpdateAddress("123 Test St", null, "Test City", "TS", "12345", "AU");
        await _dbContext.Debtors.AddAsync(debtor);

        var profile = new DebtManager.Infrastructure.Identity.UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DebtorId = debtor.Id
        };
        await _dbContext.UserProfiles.AddAsync(profile);

        var debt = new Debt(
            org.Id, debtor.Id, 1000m, "AUD", "ACC-001", "REF-001"
        );
        await _dbContext.Debts.AddAsync(debt);
        await _dbContext.SaveChangesAsync();

        var vm = new AcceptDebtPostVm
        {
            DebtId = debt.Id,
            SelectedOption = AcceptOption.Dispute,
            DisputeReason = "I don't recognize this debt"
        };

        // Act
        var result = await _controller.Index(vm, CancellationToken.None) as RedirectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Url, Does.Contain("/User/Debts/Dispute/"));
        
        var updatedDebt = await _dbContext.Debts.FindAsync(debt.Id);
        Assert.That(updatedDebt!.Status, Is.EqualTo(DebtStatus.Disputed));
    }

    [Test]
    public async Task Post_WithPayInFullOption_CreatesFullPaymentPlan()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com",
            ExternalAuthId = "test-external-id"
        };
        await _dbContext.Users.AddAsync(user);

        var org = Organization.CreatePending(
            "Test Org", "Test Legal", "12345678901", "AUD", 
            "#000000", "#ffffff", "test@test.com", "+1234567890", "UTC"
        );
        await _dbContext.Organizations.AddAsync(org);

        var debtor = new Debtor(
            org.Id, "DEBTOR-001", "john@test.com", "+1234567890", 
            "John", "Doe"
        );
        debtor.UpdateAddress("123 Test St", null, "Test City", "TS", "12345", "AU");
        await _dbContext.Debtors.AddAsync(debtor);

        var profile = new DebtManager.Infrastructure.Identity.UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DebtorId = debtor.Id
        };
        await _dbContext.UserProfiles.AddAsync(profile);

        var debt = new Debt(
            org.Id, debtor.Id, 1000m, "AUD", "ACC-001", "REF-001"
        );
        await _dbContext.Debts.AddAsync(debt);
        await _dbContext.SaveChangesAsync();

        var vm = new AcceptDebtPostVm
        {
            DebtId = debt.Id,
            SelectedOption = AcceptOption.PayInFull
        };

        // Act
        var result = await _controller.Index(vm, CancellationToken.None) as RedirectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Url, Does.Contain("/User/Payments/MakePayment"));
        
        var plan = await _dbContext.PaymentPlans.FirstOrDefaultAsync(p => p.DebtId == debt.Id);
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan!.Type, Is.EqualTo(PaymentPlanType.FullSettlement));
        Assert.That(plan.InstallmentCount, Is.EqualTo(1));
        Assert.That(plan.Frequency, Is.EqualTo(PaymentFrequency.OneOff));
    }

    [Test]
    public async Task Post_WithInstallmentsOption_CreatesInstallmentPlan()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com",
            ExternalAuthId = "test-external-id"
        };
        await _dbContext.Users.AddAsync(user);

        var org = Organization.CreatePending(
            "Test Org", "Test Legal", "12345678901", "AUD", 
            "#000000", "#ffffff", "test@test.com", "+1234567890", "UTC"
        );
        await _dbContext.Organizations.AddAsync(org);

        var debtor = new Debtor(
            org.Id, "DEBTOR-001", "john@test.com", "+1234567890", 
            "John", "Doe"
        );
        debtor.UpdateAddress("123 Test St", null, "Test City", "TS", "12345", "AU");
        await _dbContext.Debtors.AddAsync(debtor);

        var profile = new DebtManager.Infrastructure.Identity.UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DebtorId = debtor.Id
        };
        await _dbContext.UserProfiles.AddAsync(profile);

        var debt = new Debt(
            org.Id, debtor.Id, 1000m, "AUD", "ACC-001", "REF-001"
        );
        await _dbContext.Debts.AddAsync(debt);
        await _dbContext.SaveChangesAsync();

        var vm = new AcceptDebtPostVm
        {
            DebtId = debt.Id,
            SelectedOption = AcceptOption.Installments,
            Frequency = PaymentFrequency.Monthly,
            InstallmentCount = 10
        };

        // Act
        var result = await _controller.Index(vm, CancellationToken.None) as RedirectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Url, Is.EqualTo("/User"));
        
        var plan = await _dbContext.PaymentPlans.FirstOrDefaultAsync(p => p.DebtId == debt.Id);
        Assert.That(plan, Is.Not.Null);
        Assert.That(plan!.Type, Is.EqualTo(PaymentPlanType.SystemGenerated));
        Assert.That(plan.InstallmentCount, Is.EqualTo(10));
        Assert.That(plan.Frequency, Is.EqualTo(PaymentFrequency.Monthly));
    }

    [Test]
    public async Task Post_WithInvalidInstallmentCount_ReturnsViewWithErrors()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = "testuser",
            Email = "test@test.com",
            ExternalAuthId = "test-external-id"
        };
        await _dbContext.Users.AddAsync(user);

        var org = Organization.CreatePending(
            "Test Org", "Test Legal", "12345678901", "AUD", 
            "#000000", "#ffffff", "test@test.com", "+1234567890", "UTC"
        );
        await _dbContext.Organizations.AddAsync(org);

        var debtor = new Debtor(
            org.Id, "DEBTOR-001", "john@test.com", "+1234567890", 
            "John", "Doe"
        );
        debtor.UpdateAddress("123 Test St", null, "Test City", "TS", "12345", "AU");
        await _dbContext.Debtors.AddAsync(debtor);

        var profile = new DebtManager.Infrastructure.Identity.UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DebtorId = debtor.Id
        };
        await _dbContext.UserProfiles.AddAsync(profile);

        var debt = new Debt(
            org.Id, debtor.Id, 1000m, "AUD", "ACC-001", "REF-001"
        );
        await _dbContext.Debts.AddAsync(debt);
        await _dbContext.SaveChangesAsync();

        var vm = new AcceptDebtPostVm
        {
            DebtId = debt.Id,
            SelectedOption = AcceptOption.Installments,
            Frequency = PaymentFrequency.Monthly,
            InstallmentCount = 100 // Invalid: exceeds max of 48
        };

        // Act
        var result = await _controller.Index(vm, CancellationToken.None) as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.ModelState.IsValid, Is.False);
        Assert.That(_controller.ModelState.ContainsKey(nameof(vm.InstallmentCount)), Is.True);
    }
}
