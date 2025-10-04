# Development Guide

This guide covers development workflows, coding standards, and best practices for the Debt Management Platform.

## Table of Contents

- [Development Environment](#development-environment)
- [Coding Standards](#coding-standards)
- [Git Workflow](#git-workflow)
- [Testing Guidelines](#testing-guidelines)
- [Database Migrations](#database-migrations)
- [Debugging](#debugging)
- [Code Review Guidelines](#code-review-guidelines)

---

## Development Environment

### Required Tools

- **.NET 8 SDK** - Latest version
- **Visual Studio 2022** or **Visual Studio Code** with C# extension
- **Node.js 20+** - For Tailwind CSS build
- **Docker Desktop** - For SQL Server and containerized services
- **Git** - Version control
- **Postman or similar** - API testing

### Optional but Recommended

- **Azure CLI** - For Azure AD B2C management
- **Stripe CLI** - For webhook testing
- **SQL Server Management Studio** - Database management
- **Azure Data Studio** - Cross-platform database tool

### IDE Setup

**Visual Studio 2022:**

1. Install workloads:
   - ASP.NET and web development
   - .NET desktop development
   - Data storage and processing

2. Extensions:
   - ReSharper (optional, for enhanced refactoring)
   - CodeMaid (code cleanup)
   - SonarLint (code quality)

**Visual Studio Code:**

1. Install extensions:
   - C# (Microsoft)
   - C# Dev Kit
   - NuGet Gallery
   - GitLens
   - Prettier
   - Tailwind CSS IntelliSense

### User Secrets Configuration

Store sensitive configuration locally:

```bash
cd src/DebtManager.Web
dotnet user-secrets init
dotnet user-secrets set "Stripe:SecretKey" "sk_test_..."
dotnet user-secrets set "Twilio:AuthToken" "your_token"
```

---

## Coding Standards

### C# Style Guide

**Naming Conventions:**

- **PascalCase** - Classes, methods, properties, public fields
- **camelCase** - Local variables, parameters, private fields
- **_camelCase** - Private instance fields (with underscore prefix)
- **UPPER_SNAKE_CASE** - Constants

**Examples:**

```csharp
public class PaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private const int MAX_RETRY_ATTEMPTS = 3;
    
    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, CancellationToken ct)
    {
        var paymentId = Guid.NewGuid();
        // Implementation
    }
}
```

### File Organization

**One class per file** - File name matches class name

**Namespace structure:**

```csharp
namespace DebtManager.Domain.Payments;

public class Payment : Entity
{
    // Implementation
}
```

**Using directives:**

1. System namespaces first
2. Third-party namespaces
3. Project namespaces
4. Alphabetical order within groups

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using DebtManager.Domain.Common;
using DebtManager.Contracts.Persistence;
```

### Domain-Driven Design Patterns

**Entities:**

```csharp
public class Debt : Entity
{
    public string ReferenceId { get; private set; }
    public decimal Amount { get; private set; }
    public DebtStatus Status { get; private set; }
    
    // Private constructor for EF
    private Debt() { }
    
    // Factory method
    public static Debt Create(string referenceId, decimal amount)
    {
        // Validation
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
            
        return new Debt
        {
            Id = Guid.NewGuid(),
            ReferenceId = referenceId,
            Amount = amount,
            Status = DebtStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }
    
    // Business logic
    public void Accept()
    {
        if (Status != DebtStatus.Pending)
            throw new InvalidOperationException("Debt already processed");
            
        Status = DebtStatus.Accepted;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

**Value Objects:**

```csharp
public record Money(decimal Amount, string Currency)
{
    public static Money AUD(decimal amount) => new(amount, "AUD");
    
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot add different currencies");
            
        return new Money(Amount + other.Amount, Currency);
    }
}
```

### CQRS Pattern

**Commands:**

```csharp
public record CreateDebtCommand(
    Guid DebtorId,
    decimal Amount,
    string Description
) : IRequest<Guid>;

public class CreateDebtCommandHandler : IRequestHandler<CreateDebtCommand, Guid>
{
    private readonly IDebtRepository _debtRepository;
    
    public async Task<Guid> Handle(CreateDebtCommand request, CancellationToken ct)
    {
        var debt = Debt.Create(
            GenerateReferenceId(),
            request.Amount
        );
        
        await _debtRepository.AddAsync(debt, ct);
        await _debtRepository.SaveChangesAsync(ct);
        
        return debt.Id;
    }
}
```

**Queries:**

```csharp
public record GetDebtByIdQuery(Guid Id) : IRequest<DebtDto?>;

public class GetDebtByIdQueryHandler : IRequestHandler<GetDebtByIdQuery, DebtDto?>
{
    private readonly IDebtRepository _debtRepository;
    
    public async Task<DebtDto?> Handle(GetDebtByIdQuery request, CancellationToken ct)
    {
        var debt = await _debtRepository.GetByIdAsync(request.Id, ct);
        return debt != null ? MapToDto(debt) : null;
    }
}
```

### Validation

**FluentValidation:**

```csharp
public class CreateDebtCommandValidator : AbstractValidator<CreateDebtCommand>
{
    public CreateDebtCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero");
            
        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(500);
            
        RuleFor(x => x.DebtorId)
            .NotEmpty();
    }
}
```

### Async/Await Best Practices

**Always:**
- Use `async`/`await` for I/O operations
- Pass `CancellationToken` to support request cancellation
- Suffix async methods with `Async`

**Never:**
- Use `.Result` or `.Wait()` (causes deadlocks)
- Use `async void` (except event handlers)
- Forget to configure await when needed

**Good:**

```csharp
public async Task<Debt> GetDebtAsync(Guid id, CancellationToken ct)
{
    return await _context.Debts
        .Where(d => d.Id == id)
        .FirstOrDefaultAsync(ct);
}
```

**Bad:**

```csharp
public Debt GetDebt(Guid id)
{
    return _context.Debts
        .Where(d => d.Id == id)
        .FirstOrDefaultAsync().Result; // Deadlock risk!
}
```

### Error Handling

**Use appropriate exceptions:**

```csharp
// Domain exceptions
public class DebtNotFoundException : Exception
{
    public DebtNotFoundException(Guid id) 
        : base($"Debt with ID {id} was not found")
    {
    }
}

// Validation exceptions
public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }
    
    public ValidationException(Dictionary<string, string[]> errors)
    {
        Errors = errors;
    }
}
```

**Global exception handling:**

```csharp
app.UseExceptionHandler("/Home/Error");
```

### Comments

**When to comment:**
- Complex business logic
- Non-obvious workarounds
- Public API documentation (XML comments)

**When NOT to comment:**
- Obvious code
- Commented-out code (delete it)
- Redundant information

**Good XML comments:**

```csharp
/// <summary>
/// Calculates the discount amount based on payment plan type.
/// </summary>
/// <param name="amount">The original debt amount.</param>
/// <param name="planType">The selected payment plan type.</param>
/// <returns>The discount amount in the same currency as the debt.</returns>
public decimal CalculateDiscount(decimal amount, PaymentPlanType planType)
{
    // Implementation
}
```

---

## Git Workflow

### Branch Strategy

**Main Branches:**
- `main` - Production-ready code
- `develop` - Integration branch (optional)

**Feature Branches:**
- `feature/debt-import` - New features
- `fix/payment-calculation` - Bug fixes
- `refactor/repository-pattern` - Code improvements
- `docs/api-documentation` - Documentation

### Commit Messages

**Format:**

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat` - New feature
- `fix` - Bug fix
- `refactor` - Code refactoring
- `docs` - Documentation
- `test` - Tests
- `chore` - Maintenance

**Examples:**

```
feat(payments): Add Stripe webhook handler

- Implement checkout.session.completed event
- Add payment confirmation logic
- Update debt status on successful payment

Closes #123
```

```
fix(auth): Resolve token expiration issue

The token cache was not refreshing properly,
causing users to be logged out prematurely.

Fixes #456
```

### Pull Request Process

1. **Create feature branch** from `main`
2. **Make changes** following coding standards
3. **Write tests** for new functionality
4. **Run tests locally** - ensure all pass
5. **Commit changes** with descriptive messages
6. **Push to remote** repository
7. **Create pull request** with description
8. **Request review** from team members
9. **Address feedback** and make changes
10. **Merge** after approval and passing CI

**PR Template:**

```markdown
## Description
Brief description of changes

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Testing
- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Manual testing completed

## Checklist
- [ ] Code follows style guidelines
- [ ] Self-review completed
- [ ] Documentation updated
- [ ] No new warnings
- [ ] Tests pass locally
```

---

## Testing Guidelines

### Unit Tests

**Framework:** NUnit

**Structure:**

```csharp
[TestFixture]
public class PaymentServiceTests
{
    private Mock<IPaymentRepository> _mockRepo;
    private PaymentService _service;
    
    [SetUp]
    public void Setup()
    {
        _mockRepo = new Mock<IPaymentRepository>();
        _service = new PaymentService(_mockRepo.Object);
    }
    
    [Test]
    public async Task ProcessPayment_ValidAmount_CreatesPayment()
    {
        // Arrange
        var amount = 100m;
        var debtId = Guid.NewGuid();
        
        // Act
        var result = await _service.ProcessPaymentAsync(debtId, amount);
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Amount, Is.EqualTo(amount));
        _mockRepo.Verify(r => r.AddAsync(It.IsAny<Payment>(), default), Times.Once);
    }
    
    [Test]
    public void ProcessPayment_NegativeAmount_ThrowsException()
    {
        // Arrange
        var amount = -100m;
        var debtId = Guid.NewGuid();
        
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            async () => await _service.ProcessPaymentAsync(debtId, amount)
        );
    }
}
```

### Integration Tests

**WebApplicationFactory:**

```csharp
public class DebtControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public DebtControllerTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }
    
    [Test]
    public async Task GetDebts_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/debts");
        
        // Assert
        response.EnsureSuccessStatusCode();
        Assert.That(response.Content.Headers.ContentType.MediaType, 
            Is.EqualTo("application/json"));
    }
}
```

### Test Coverage

**Minimum targets:**
- Domain logic: 90%+
- Application services: 80%+
- Controllers: 70%+
- Overall: 75%+

**Run coverage:**

```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

---

## Database Migrations

### Creating Migrations

```bash
# Add new migration
dotnet ef migrations add MigrationName \
    -p src/DebtManager.Infrastructure \
    -s src/DebtManager.Web

# View migration SQL
dotnet ef migrations script \
    -p src/DebtManager.Infrastructure \
    -s src/DebtManager.Web

# Apply migrations
dotnet ef database update \
    -p src/DebtManager.Infrastructure \
    -s src/DebtManager.Web
```

### Migration Best Practices

1. **Descriptive names** - `AddPaymentPlanTable`, `AddDebtorEmailIndex`
2. **Small migrations** - One logical change per migration
3. **Review SQL** - Always check generated SQL before applying
4. **Test rollback** - Ensure `Down` method works
5. **Data migrations** - Use separate migrations for data changes

**Example:**

```csharp
public partial class AddPaymentPlanTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "PaymentPlans",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                DebtId = table.Column<Guid>(nullable: false),
                PlanType = table.Column<int>(nullable: false),
                StartDate = table.Column<DateTime>(nullable: false),
                EndDate = table.Column<DateTime>(nullable: false),
                InstallmentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_PaymentPlans", x => x.Id);
                table.ForeignKey(
                    name: "FK_PaymentPlans_Debts_DebtId",
                    column: x => x.DebtId,
                    principalTable: "Debts",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }
    
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "PaymentPlans");
    }
}
```

---

## Debugging

### Visual Studio

**Breakpoints:**
- F9 - Toggle breakpoint
- F5 - Start debugging
- F10 - Step over
- F11 - Step into

**Useful Windows:**
- Locals - View local variables
- Watch - Monitor specific expressions
- Call Stack - View execution path
- Immediate - Execute code during debugging

### Logging

**Serilog configuration:**

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
```

**Structured logging:**

```csharp
_logger.LogInformation(
    "Payment processed for debt {DebtId} with amount {Amount}",
    debtId,
    amount
);
```

### Common Issues

**Database connection:**

```bash
# Test connection
dotnet ef database drop -f -p src/DebtManager.Infrastructure -s src/DebtManager.Web
dotnet ef database update -p src/DebtManager.Infrastructure -s src/DebtManager.Web
```

**Tailwind not updating:**

```bash
cd src/DebtManager.Web
npm run build
```

**Authentication errors:**

Check Azure AD B2C configuration and ensure redirect URIs match.

---

## Code Review Guidelines

### Reviewer Checklist

- [ ] Code follows style guidelines
- [ ] Tests are included and pass
- [ ] No unnecessary complexity
- [ ] Naming is clear and consistent
- [ ] Error handling is appropriate
- [ ] Performance considerations addressed
- [ ] Security implications reviewed
- [ ] Documentation updated

### Providing Feedback

**Be constructive:**

❌ "This code is bad"  
✅ "Consider extracting this into a separate method for better testability"

**Be specific:**

❌ "Improve performance"  
✅ "This query causes N+1 problem. Consider using .Include() to eager load related entities"

**Acknowledge good work:**

✅ "Great use of the repository pattern here!"

---

**See Also:**
- [Getting Started](Getting-Started.md) - Setup guide
- [Architecture](Architecture.md) - System design
- [Testing Guidelines](#testing-guidelines) - Detailed testing practices
