# Enhanced 2-Click Payment Flow - Implementation Guide

## Overview

This implementation enhances the payment flow to enable seamless 2-click payments with support for modern payment methods including Apple Pay, Google Pay, and traditional card payments. The system uses Stripe for payment processing, Hangfire for background job processing, and implements a mobile-first UI design.

## Architecture

### Components

1. **Payment Service (`IPaymentService`)** - Handles Stripe payment intent creation and management
2. **Webhook Processor (`IWebhookProcessor`)** - Processes incoming Stripe webhooks
3. **Payment Webhook Job (`PaymentWebhookJob`)** - Background job for processing payment events
4. **Payment API Controller** - RESTful endpoints for payment operations
5. **Enhanced UI** - Mobile-first payment view with Stripe Elements integration

### Data Flow

```
User -> Find Debt (by reference) -> Create Payment Intent -> Stripe Checkout
     -> Payment Confirmation -> Stripe Webhook -> Hangfire Job -> Update Database
```

## Key Features

### 1. Quick Debt Lookup

Users can find their debt using a reference number without needing to log in:

```
GET /api/payment/find-by-reference?reference=D-5001
```

**Response:**
```json
{
  "debtId": "guid",
  "reference": "D-5001",
  "amount": 1250.50,
  "currency": "AUD",
  "organizationId": "guid"
}
```

### 2. Payment Methods

The system supports multiple payment methods configured through Stripe:

- **Credit/Debit Cards** - Visa, Mastercard, American Express
- **Apple Pay** - For iOS devices
- **Google Pay** - For Android devices
- **Bank Transfer** - Direct debit/ACH

Retrieve available methods:
```
GET /api/payment/methods
```

### 3. Create Payment Intent

The payment flow uses Stripe Payment Intents for secure, modern payment processing:

```
POST /api/payment/create-intent
{
  "debtId": "guid",
  "amount": 1250.50,
  "currency": "AUD"
}
```

**Response:**
```json
{
  "intentId": "pi_xxx",
  "clientSecret": "pi_xxx_secret_yyy",
  "amount": 1250.50,
  "currency": "AUD",
  "supportedMethods": ["Card", "BankTransfer"]
}
```

### 4. Webhook Processing

Stripe webhooks are processed asynchronously using Hangfire:

#### Webhook Endpoint
```
POST /api/webhooks/stripe
Headers:
  Stripe-Signature: t=xxx,v1=yyy
Body: {Stripe Event JSON}
```

#### Supported Events

1. **payment_intent.succeeded** - Payment completed successfully
   - Creates Transaction record
   - Updates Debt outstanding balance
   - Marks debt as settled if fully paid

2. **payment_intent.payment_failed** - Payment failed
   - Creates failed Transaction record
   - Logs failure reason

3. **payment_intent.canceled** - Payment canceled
   - Updates or creates canceled Transaction record

### 5. Hangfire Dashboard

The Hangfire dashboard is secured with admin authorization and accessible at:

```
/hangfire
```

**Security:**
- Requires authentication
- Requires Admin scope claim
- Dashboard shows:
  - Queued jobs
  - Processing jobs
  - Succeeded jobs
  - Failed jobs with retry capability
  - Job history for audit trail

### 6. Database Persistence

Hangfire is configured to use SQL Server for job persistence:

```csharp
builder.Services.AddHangfire(cfg => cfg
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(hangfireCs));
```

**Benefits:**
- Jobs survive application restarts
- Full audit trail
- Job retry capability
- Distributed processing support

## Configuration

### Required Settings (appsettings.json)

```json
{
  "ConnectionStrings": {
    "Default": "Server=...;Database=DebtManager;...",
    "Hangfire": "Server=...;Database=DebtManager;..."
  },
  "Stripe": {
    "PublishableKey": "pk_test_...",
    "SecretKey": "sk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

### Stripe Setup

1. **Create Stripe Account** - https://stripe.com
2. **Get API Keys** - Dashboard > Developers > API Keys
3. **Configure Webhook** - Dashboard > Developers > Webhooks
   - URL: `https://yourdomain.com/api/webhooks/stripe`
   - Events to select:
     - `payment_intent.succeeded`
     - `payment_intent.payment_failed`
     - `payment_intent.canceled`
4. **Copy Webhook Secret** - Use in `Stripe:WebhookSecret` config

## Mobile-First UI

The payment view is optimized for mobile devices:

### Features

1. **Responsive Design** - Works on all screen sizes
2. **Touch-Friendly** - Large buttons, easy-to-tap elements
3. **Quick Amount Selection** - 25%, 50%, 100% buttons
4. **Real-time Validation** - Immediate feedback
5. **Secure Payment Element** - Stripe-hosted, PCI-compliant
6. **Loading States** - Clear feedback during processing
7. **Error Handling** - User-friendly error messages

### Stripe Elements Integration

The UI uses Stripe's Payment Element which:
- Automatically detects available payment methods
- Shows Apple Pay/Google Pay when available
- Handles card validation
- Manages 3D Secure authentication
- Provides a consistent, localized experience

## Security Considerations

### 1. Webhook Signature Verification

All webhook requests are verified using the Stripe signature:

```csharp
var stripeEvent = EventUtility.ConstructEvent(
    payload,
    signature,
    webhookSecret
);
```

### 2. Hangfire Dashboard Authorization

Custom authorization filter ensures only admins can access:

```csharp
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        return httpContext.User.HasClaim("scp", adminScope);
    }
}
```

### 3. HTTPS Only

All payment endpoints should only be accessible via HTTPS in production.

### 4. PCI Compliance

- No card data is stored in the application
- All card processing handled by Stripe
- Stripe Elements used for secure card input
- PCI DSS Level 1 compliance through Stripe

## Testing

### Unit Tests

Tests are included for:
- Payment service operations
- Webhook processing
- Payment method retrieval
- Error scenarios

Run tests:
```bash
dotnet test
```

### Integration Testing

To test the complete flow:

1. **Configure Stripe Test Keys**
   - Use `pk_test_...` and `sk_test_...` keys
   - Use Stripe CLI for webhook testing

2. **Test Cards** - Use Stripe test cards:
   - Success: `4242 4242 4242 4242`
   - Decline: `4000 0000 0000 0002`
   - 3D Secure: `4000 0025 0000 3155`

3. **Webhook Testing**
   ```bash
   stripe listen --forward-to localhost:5000/api/webhooks/stripe
   ```

## Deployment Checklist

- [ ] Set production Stripe keys in secure configuration (Azure Key Vault, AWS Secrets Manager, etc.)
- [ ] Configure Stripe webhook endpoint in Stripe Dashboard
- [ ] Ensure HTTPS is enforced
- [ ] Configure Hangfire SQL Server connection string
- [ ] Test webhook delivery with Stripe CLI
- [ ] Verify Hangfire dashboard is accessible only to admins
- [ ] Monitor Hangfire failed jobs queue
- [ ] Set up alerts for payment failures
- [ ] Configure logging for payment events
- [ ] Test mobile payment flow on iOS and Android devices

## Monitoring and Observability

### Logs

Payment events are logged at various levels:

- **Information**: Payment intent created, payment succeeded
- **Warning**: Payment failed, webhook signature invalid
- **Error**: Unexpected exceptions during processing

### Metrics to Monitor

1. **Payment Success Rate** - Track successful vs failed payments
2. **Webhook Processing Time** - Time to process webhook events
3. **Hangfire Job Failures** - Monitor failed job count
4. **API Response Times** - Payment intent creation time

### Hangfire Dashboard

Access `/hangfire` to monitor:
- Current job queue length
- Processing time statistics
- Failed jobs with stack traces
- Retry statistics
- Server health

## Troubleshooting

### Webhook Not Receiving Events

1. Check webhook URL is correct in Stripe Dashboard
2. Verify HTTPS is accessible
3. Check `Stripe:WebhookSecret` configuration
4. Review application logs for signature validation errors

### Payment Not Updating Debt

1. Check Hangfire dashboard for failed jobs
2. Verify debt ID in payment intent metadata
3. Check transaction repository for duplicate prevention
4. Review logs for database errors

### Hangfire Dashboard Not Accessible

1. Verify user is authenticated
2. Check user has Admin scope claim
3. Review `HangfireAuthorizationFilter` logs

## Future Enhancements

Potential improvements for the payment flow:

1. **Payment Plans** - Support for installment payments
2. **Partial Payments** - Allow payments less than full amount
3. **Payment Receipts** - Automatic PDF receipt generation
4. **Refund Processing** - Handle refund webhooks
5. **Multiple Payment Methods** - Save and reuse payment methods
6. **Scheduled Payments** - Recurring payment support
7. **Payment Analytics** - Dashboard with payment statistics
8. **Mobile Apps** - Native iOS/Android integration

## References

- [Stripe Documentation](https://stripe.com/docs)
- [Stripe Payment Intents](https://stripe.com/docs/payments/payment-intents)
- [Stripe Elements](https://stripe.com/docs/payments/payment-element)
- [Hangfire Documentation](https://docs.hangfire.io)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
