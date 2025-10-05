using DebtManager.Domain.Debtors;
using DebtManager.Domain.Debts;
using DebtManager.Domain.Organizations;
using DebtManager.Domain.Payments;
using DebtManager.Infrastructure.Persistence;
using DebtManager.Web.Data;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DebtManager.Tests;

public class DummyDataSeederTests
{
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Tagged_Organizations()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var organizations = await context.Organizations.ToListAsync();
        Assert.That(organizations.Count, Is.GreaterThan(0));
        Assert.That(organizations.All(o => o.TagsCsv.Contains("dummy")), Is.True);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Organizations_With_Different_Scenarios()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var organizations = await context.Organizations.ToListAsync();
        
        var pendingOrg = organizations.FirstOrDefault(o => o.TagsCsv.Contains("scenario:pending-approval"));
        Assert.That(pendingOrg, Is.Not.Null);
        Assert.That(pendingOrg!.IsApproved, Is.False);

        var rejectedOrg = organizations.FirstOrDefault(o => o.TagsCsv.Contains("scenario:rejected"));
        Assert.That(rejectedOrg, Is.Not.Null);
        Assert.That(rejectedOrg!.IsApproved, Is.False);

        var activeOrg = organizations.FirstOrDefault(o => o.TagsCsv.Contains("scenario:active-established"));
        Assert.That(activeOrg, Is.Not.Null);
        Assert.That(activeOrg!.IsApproved, Is.True);
        Assert.That(activeOrg.ApprovedAtUtc, Is.Not.Null);
        Assert.That(activeOrg.OnboardedAtUtc, Is.Not.Null);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Tagged_Debtors()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var debtors = await context.Debtors.ToListAsync();
        Assert.That(debtors.Count, Is.GreaterThan(0));
        Assert.That(debtors.All(d => d.TagsCsv.Contains("dummy")), Is.True);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Debtors_With_Different_States()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var debtors = await context.Debtors.ToListAsync();
        
        Assert.That(debtors.Any(d => d.Status == DebtorStatus.New), Is.True);
        Assert.That(debtors.Any(d => d.Status == DebtorStatus.Invited), Is.True);
        Assert.That(debtors.Any(d => d.Status == DebtorStatus.Active), Is.True);
        Assert.That(debtors.Any(d => d.Status == DebtorStatus.Delinquent), Is.True);
        Assert.That(debtors.Any(d => d.Status == DebtorStatus.Settled), Is.True);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Tagged_Debts()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var debts = await context.Debts.ToListAsync();
        Assert.That(debts.Count, Is.GreaterThan(0));
        Assert.That(debts.All(d => d.TagsCsv.Contains("dummy")), Is.True);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Debts_With_Different_States()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var debts = await context.Debts.ToListAsync();
        
        Assert.That(debts.Any(d => d.Status == DebtStatus.PendingAssignment), Is.True);
        Assert.That(debts.Any(d => d.Status == DebtStatus.Active), Is.True);
        Assert.That(debts.Any(d => d.Status == DebtStatus.InArrears), Is.True);
        Assert.That(debts.Any(d => d.Status == DebtStatus.Settled), Is.True);
        Assert.That(debts.Any(d => d.Status == DebtStatus.Disputed), Is.True);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Tagged_PaymentPlans()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var paymentPlans = await context.PaymentPlans.ToListAsync();
        Assert.That(paymentPlans.Count, Is.GreaterThan(0));
        Assert.That(paymentPlans.All(p => p.TagsCsv.Contains("dummy")), Is.True);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_PaymentPlans_With_Different_States()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var paymentPlans = await context.PaymentPlans.ToListAsync();
        
        Assert.That(paymentPlans.Any(p => p.Status == PaymentPlanStatus.Draft), Is.True);
        Assert.That(paymentPlans.Any(p => p.Status == PaymentPlanStatus.Active), Is.True);
        Assert.That(paymentPlans.Any(p => p.Status == PaymentPlanStatus.Completed), Is.True);
        Assert.That(paymentPlans.Any(p => p.Status == PaymentPlanStatus.Defaulted), Is.True);
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Transactions()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var transactions = await context.Transactions.ToListAsync();
        Assert.That(transactions.Count, Is.GreaterThan(0));
        Assert.That(transactions.All(t => t.Status == TransactionStatus.Succeeded), Is.True);
    }

    [Test]
    public async Task SeedDummyDataAsync_Does_Not_Seed_Twice()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);
        var firstCount = await context.Organizations.CountAsync();
        
        await DummyDataSeeder.SeedDummyDataAsync(context);
        var secondCount = await context.Organizations.CountAsync();

        // Assert
        Assert.That(firstCount, Is.EqualTo(secondCount));
    }

    [Test]
    public async Task SeedDummyDataAsync_Creates_Scenario_New_Customer_With_New_Debt()
    {
        // Arrange
        using var context = CreateInMemoryContext();
        await context.Database.EnsureCreatedAsync();

        // Act
        await DummyDataSeeder.SeedDummyDataAsync(context);

        // Assert
        var newCustomer = await context.Debtors
            .FirstOrDefaultAsync(d => d.TagsCsv.Contains("scenario:new-customer"));
        Assert.That(newCustomer, Is.Not.Null);
        
        var newDebt = await context.Debts
            .FirstOrDefaultAsync(d => d.DebtorId == newCustomer!.Id && d.TagsCsv.Contains("scenario:new-debt"));
        Assert.That(newDebt, Is.Not.Null);
        Assert.That(newDebt!.Status, Is.EqualTo(DebtStatus.PendingAssignment));
    }
}
