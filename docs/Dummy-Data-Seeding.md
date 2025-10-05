# Dummy Data Seeding for Development and Testing

This document describes the dummy data seeding functionality added to the Adeva Debt Management Platform for development, testing, and demonstration purposes.

## Overview

The dummy data seeder creates realistic test data representing various business scenarios, enabling developers, testers, and stakeholders to:

- Test features with realistic data
- Demonstrate the application with complete scenarios
- Validate workflows across different entity states
- Train users with familiar test scenarios

All dummy data is tagged with "dummy" to easily identify and filter test data from production data.

## Features

### Entity Tagging

All main entities (Organization, Debtor, Debt, PaymentPlan) now support tagging via a `TagsCsv` property:

```csharp
// Add tags to any entity
organization.SetTags(new[] { "dummy", "scenario:pending-approval" });
debtor.SetTags(new[] { "dummy", "scenario:new-customer" });
debt.SetTags(new[] { "dummy", "scenario:active-on-plan" });
paymentPlan.SetTags(new[] { "dummy", "scenario:draft-plan" });
```

### Automatic Seeding

In non-production environments (Development, Staging), dummy data is automatically seeded during application startup through `DbInitializer`.

The seeder is **idempotent** - it checks for existing dummy data and will not duplicate if run multiple times.

## Scenarios Included

### Organizations

1. **Pending Approval** (`scenario:pending-approval`)
   - ABC Collections Ltd
   - New organization awaiting admin approval
   - Not yet approved or onboarded

2. **Rejected** (`scenario:rejected`)
   - XYZ Debt Recovery
   - Organization that was reviewed and rejected
   - Represents failed onboarding

3. **Active Established** (`scenario:active-established`)
   - Premier Collections
   - Approved 30 days ago, onboarded
   - Fully operational organization

4. **Recently Approved** (`scenario:recently-approved`)
   - Swift Debt Solutions
   - Approved 3 days ago
   - Not yet onboarded

### Debtors (Customers)

1. **New Customer** (`scenario:new-customer`)
   - John Smith
   - Status: New
   - Just added to the system

2. **Invited Customer** (`scenario:invited-customer`)
   - Sarah Jones
   - Status: Invited
   - Invitation sent, awaiting verification

3. **Active Paying** (`scenario:active-paying`)
   - Michael Brown
   - Status: Active
   - Portal access enabled
   - Recent login activity
   - Making regular payments

4. **Delinquent Non-Responsive** (`scenario:delinquent-non-responsive`)
   - Linda White
   - Status: Delinquent
   - Multiple contact attempts
   - No response

5. **Settled** (`scenario:settled`)
   - David Green
   - Status: Settled
   - All debts paid off

### Debts

1. **New Debt** (`scenario:new-debt`)
   - For new customers
   - Status: PendingAssignment
   - $2,500 utility bill
   - Due in 14 days

2. **Pending Verification** (`scenario:pending-verification`)
   - For invited customers
   - Status: Active
   - $1,800 telecommunications debt

3. **Active on Plan** (`scenario:active-on-plan`)
   - For active customers
   - Status: Active
   - $5,000 credit card debt
   - Has payment plan
   - 2 payments already made

4. **In Arrears High Risk** (`scenario:in-arrears-high-risk`)
   - For delinquent customers
   - Status: InArrears
   - $3,200 personal loan
   - 4 months overdue
   - Interest and fees accrued

5. **Disputed** (`scenario:disputed`)
   - Status: Disputed
   - $800 retail debt
   - Customer claims service not provided

6. **Settled** (`scenario:settled`)
   - Status: Settled
   - $1,500 medical debt
   - Fully paid

### Payment Plans

1. **Draft Plan** (`scenario:draft-plan`)
   - Status: Draft
   - System generated
   - Requires manual review
   - Weekly payments

2. **Active Plan** (`scenario:active-plan`)
   - Status: Active
   - Fortnightly payments
   - $500 per installment
   - 2 of 8 installments paid

3. **Completed Plan** (`scenario:completed-plan`)
   - Status: Completed
   - One-off full settlement
   - Successfully completed

4. **Defaulted Plan** (`scenario:defaulted-plan`)
   - Status: Defaulted
   - Monthly payments
   - Missed 2 consecutive payments

### Transactions

- Settlement payments for settled debts
- Installment payments for active payment plans
- Multiple payment methods (Bank Transfer, Credit Card)
- All transactions marked as succeeded

## Usage

### In Development

The dummy data is automatically seeded when you start the application in development mode:

```bash
dotnet run --project src/DebtManager.Web
```

### Filtering Dummy Data

Query for dummy data using the tags:

```csharp
// Get all dummy organizations
var dummyOrgs = await context.Organizations
    .Where(o => o.TagsCsv.Contains("dummy"))
    .ToListAsync();

// Get specific scenario
var pendingOrgs = await context.Organizations
    .Where(o => o.TagsCsv.Contains("scenario:pending-approval"))
    .ToListAsync();

// Exclude dummy data
var realOrgs = await context.Organizations
    .Where(o => !o.TagsCsv.Contains("dummy"))
    .ToListAsync();
```

### Manual Seeding

You can manually trigger the seeder:

```csharp
using var scope = serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await DummyDataSeeder.SeedDummyDataAsync(context);
```

## Testing

The seeder includes comprehensive unit tests covering:

- Organization creation with different states
- Debtor creation in various statuses
- Debt scenarios with different states
- Payment plan lifecycle states
- Transaction creation
- Idempotent behavior (no duplicate seeding)
- Scenario-specific relationships

Run the tests:

```bash
dotnet test --filter "FullyQualifiedName~DummyDataSeederTests"
```

All 11 tests should pass.

## Data Volumes

The seeder creates:
- 4 Organizations (different approval states)
- ~20 Debtors (5 per organization)
- ~15-20 Debts (various states)
- ~10 Payment Plans (various states)
- ~5 Transactions (successful payments)

## Implementation Details

### Files

- `src/DebtManager.Web/Data/DummyDataSeeder.cs` - Main seeder implementation
- `src/DebtManager.Web/Data/DbInitializer.cs` - Integration point
- `src/DebtManager.Domain/Organizations/Organization.cs` - Added TagsCsv property
- `src/DebtManager.Domain/Debts/Debt.cs` - Added TagsCsv property
- `src/DebtManager.Domain/Payments/PaymentPlan.cs` - Added TagsCsv property
- `tests/DebtManager.Tests/DummyDataSeederTests.cs` - Test suite

### Database Schema

The `TagsCsv` property on entities stores comma-separated tags. This allows:
- Multiple tags per entity
- Easy filtering with LINQ `.Contains()`
- Simple string storage (no additional tables)

## Best Practices

1. **Always tag dummy data**: Use at minimum the "dummy" tag
2. **Use scenario tags**: Add descriptive scenario tags for clarity
3. **Clean data between tests**: Reset database when switching test scenarios
4. **Don't modify seeded data**: Treat it as read-only test fixtures
5. **Production safety**: Seeding only runs in non-production environments

## Future Enhancements

Potential additions:
- Admin UI to trigger/reset dummy data
- Configurable scenario selection
- More complex scenarios (payment failures, refunds, etc.)
- User-specific customizations
- Export/import scenario definitions
- Time-based scenario progression

## Security Considerations

- Dummy data only seeds in non-production environments
- All dummy entities are clearly tagged
- No sensitive real-world data used
- Idempotent to prevent accidental data multiplication
- Easily identifiable and filterable for cleanup

## Support

For issues or questions about dummy data seeding:
- Check test coverage in `DummyDataSeederTests.cs`
- Review scenario definitions in `DummyDataSeeder.cs`
- Verify environment settings in `DbInitializer.cs`
