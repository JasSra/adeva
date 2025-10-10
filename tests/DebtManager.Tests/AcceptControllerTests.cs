using DebtManager.Contracts.Payments;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Debtors;
using DebtManager.Domain.Organizations;
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
    private Mock<IPaymentPlanGenerationService> _paymentPlanServiceMock = null!;
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
        _paymentPlanServiceMock = new Mock<IPaymentPlanGenerationService>();
        _controller = new AcceptController(_dbContext, _loggerMock.Object, _paymentPlanServiceMock.Object);

        // Setup HttpContext with a test user
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim("oid", "test-external-id")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        
        var httpContext = new DefaultHttpContext { User = principal };
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        
        // Initialize TempData
        _controller.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(
            httpContext,
            Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>()
        );
    }

    [TearDown]
    public void TearDown()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Test]
    public void Index_WithoutId_ShowsReferenceInputForm()
    {
        // Act
        var result = _controller.Index() as ViewResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(_controller.ViewBag.ShowReferenceInput, Is.True);
        Assert.That(_controller.ViewBag.Title, Is.EqualTo("Accept Debt"));
    }

    [Test]
    public async Task FindByReference_WithValidClientReference_ReturnsDebt()
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
        await _dbContext.Debtors.AddAsync(debtor);

        var profile = new UserProfile
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

        // Act
        var result = await _controller.FindByReference("REF-001", CancellationToken.None) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
        
        var data = result.Value as dynamic;
        Assert.That(data, Is.Not.Null);
    }

    [Test]
    public async Task FindByReference_WithValidGeneratedReference_ReturnsDebt()
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
        await _dbContext.Debtors.AddAsync(debtor);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DebtorId = debtor.Id
        };
        await _dbContext.UserProfiles.AddAsync(profile);

        var debt = new Debt(
            org.Id, debtor.Id, 1000m, "AUD", "ACC-001", null
        );
        await _dbContext.Debts.AddAsync(debt);
        await _dbContext.SaveChangesAsync();

        var generatedRef = "D-" + debt.Id.ToString().Substring(0, 8);

        // Act
        var result = await _controller.FindByReference(generatedRef, CancellationToken.None) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task FindByReference_WithEmptyReference_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.FindByReference("", CancellationToken.None) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(400));
    }

    [Test]
    public async Task FindByReference_WithNonExistentReference_ReturnsNotFound()
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
        await _dbContext.Debtors.AddAsync(debtor);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DebtorId = debtor.Id
        };
        await _dbContext.UserProfiles.AddAsync(profile);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.FindByReference("NONEXISTENT-REF", CancellationToken.None) as NotFoundObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(404));
    }

    [Test]
    public async Task FindByReference_WithSettledDebt_ReturnsBadRequest()
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
        await _dbContext.Debtors.AddAsync(debtor);

        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DebtorId = debtor.Id
        };
        await _dbContext.UserProfiles.AddAsync(profile);

        var debt = new Debt(
            org.Id, debtor.Id, 1000m, "AUD", "ACC-001", "REF-SETTLED"
        );
        debt.SetStatus(DebtStatus.Settled);
        await _dbContext.Debts.AddAsync(debt);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.FindByReference("REF-SETTLED", CancellationToken.None) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.StatusCode, Is.EqualTo(400));
    }
}
