# Payment Plan Selection Feature

## Overview

This feature implements a smart payment plan selection workflow for debtors, presenting three optimized payment options with clear benefits and allowing custom schedules with intelligent validation.

## Three Payment Plan Options

### Option A: Full Payment with Maximum Discount
- **Discount**: 10% of total debt (configurable per organization)
- **Benefits**: 
  - Immediate debt settlement
  - Best value - maximum savings
  - No ongoing payments or fees
- **Use Case**: For debtors who can pay the full amount immediately

### Option B: System-Generated Weekly Plan
- **Discount**: 5% of total debt (configurable per organization)
- **Frequency**: Weekly installments
- **Features**:
  - Smart installment calculation using AI + rules-based logic
  - Prevents silly scenarios (e.g., no $10 payments for $5000 debt)
  - Rounds to clean amounts ($5/$10 increments)
  - Maximum 52 installments, minimum $50 per installment (configurable)
  - Automatic payment reminders
- **Use Case**: For debtors who prefer manageable regular payments

### Option C: Custom Payment Schedule
- **Discount**: None
- **Fees**: Admin fees applied to each installment
- **Features**:
  - Debtor proposes their own payment dates and amounts
  - Smart validation prevents unreasonable schedules
  - Admin approval required
  - Flexible payment according to cash flow
- **Use Case**: For debtors with irregular income or specific payment preferences

## Smart Rules-Based Logic

The system implements intelligent rules to ensure payment plans are practical and fair:

1. **Minimum Installment Amount**: No installments below $50 (configurable)
2. **Clean Rounding**: Amounts rounded to $1, $5, or $10 for clarity
3. **Maximum Duration**: Plans capped at 52 installments (1 year weekly)
4. **Smart Admin Fees**: Distributed evenly, not silly small amounts
5. **Total Coverage**: Custom schedules must cover full debt amount

## AI Service Integration

The feature is designed to integrate with AI services for enhanced recommendations:

- **Interface**: `IPaymentPlanAIService` ready for Azure OpenAI or custom ML models
- **Current**: Stub implementation with fallback to rules-based logic
- **Future**: 
  - AI-optimized installment schedules based on debtor history
  - Risk assessment for custom plans
  - Payment success prediction
  - Personalized recommendations

## API Endpoints

### Get Payment Plan Options
```
GET /api/payment-plans/options/{debtId}
```
Returns three payment plan options tailored for the specific debt.

**Response Example**:
```json
{
  "options": [
    {
      "type": "FullSettlement",
      "title": "Pay in Full with Discount",
      "description": "Pay the entire debt now and receive maximum discount",
      "originalAmount": 5000.00,
      "totalAmount": 4500.00,
      "discountAmount": 500.00,
      "discountPercentage": 10.0,
      "isRecommended": true,
      "benefits": [
        "Save AUD 500.00 (10% discount)",
        "Debt settled immediately",
        "No ongoing payments or fees",
        "Best value option"
      ]
    },
    // ... other options
  ]
}
```

### Accept Payment Plan
```
POST /api/payment-plans/accept
```
Creates a payment plan from the selected option.

**Request Body**:
```json
{
  "debtId": "guid",
  "selectedOption": { /* payment plan option object */ },
  "customSchedule": [ /* optional: for custom plans */ ],
  "userId": "string"
}
```

### Validate Custom Schedule
```
POST /api/payment-plans/validate-custom
```
Validates a custom payment schedule before acceptance.

## User Interface

### Payment Plan Selection View (`/Payment/SelectPlan`)
- Clean card-based layout showing all three options side-by-side
- Clear display of savings, total amount, and benefits
- Badges for recommended and approval-required options
- Click to select and proceed

### Custom Plan Modal
- Dynamic form for entering custom installment schedule
- Adjustable number of installments
- Date and amount inputs for each installment
- Real-time validation
- Submit for admin approval

### Confirmation Page (`/Payment/PlanConfirmation`)
- Success message with payment plan ID
- Next steps clearly outlined
- Links to dashboard and plan details

## Configuration

Payment plan settings are stored in `OrganizationFeeConfiguration`:

```csharp
public class OrganizationFeeConfiguration
{
    // Discounts
    public decimal FullPaymentDiscountPercentage { get; set; }  // Default: 10%
    public decimal SystemPlanDiscountPercentage { get; set; }   // Default: 5%
    
    // Admin Fees
    public decimal CustomPlanAdminFeeFlat { get; set; }         // Default: $25
    public decimal CustomPlanAdminFeePercentage { get; set; }   // Default: 2%
    
    // Installment Rules
    public decimal MinimumInstallmentAmount { get; set; }       // Default: $50
    public int MaximumInstallmentCount { get; set; }            // Default: 52
    public int DefaultInstallmentPeriodWeeks { get; set; }      // Default: 12
}
```

## Testing

Tests cover:
- Generation of all three payment plan types
- Discount calculations
- Smart installment logic
- Custom schedule validation
- Admin fee application
- Edge cases (minimum amounts, maximum counts, etc.)

Run tests:
```bash
dotnet test --filter "FullyQualifiedName~PaymentPlanGenerationServiceTests"
```

## Future Enhancements

1. **AI Integration**: Connect to Azure OpenAI for truly personalized recommendations
2. **Payment Analytics**: Track which plan types are most successful
3. **A/B Testing**: Test different discount percentages and fees
4. **Debtor Scoring**: Use payment history to adjust available options
5. **Mobile Optimization**: Enhanced mobile experience for plan selection
6. **Multi-Currency**: Support for international debtors
7. **Recurring Payments**: Automatic debit setup for system-generated plans

## Notes

- All monetary amounts use `decimal` for precision
- Dates use UTC timezone (`DateTime.UtcNow`)
- Custom plans always require manual review
- Pre-existing test failures in `PaymentServiceTests` are unrelated to this feature
