# Quick Payment Setup Guide

## Prerequisites

1. .NET 8.0 SDK
2. SQL Server (LocalDB or full instance)
3. Stripe Account (https://stripe.com)
4. Azure AD B2C tenant (for authentication)

## Step 1: Configure Stripe

### Get Your Stripe Keys

1. Sign in to [Stripe Dashboard](https://dashboard.stripe.com)
2. Navigate to **Developers > API Keys**
3. Copy your **Publishable key** (starts with `pk_test_`)
4. Copy your **Secret key** (starts with `sk_test_`)

### Set Up Webhook

1. Navigate to **Developers > Webhooks**
2. Click **Add endpoint**
3. Enter your webhook URL: `https://yourdomain.com/api/webhooks/stripe`
4. Select events:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
   - `payment_intent.canceled`
5. Click **Add endpoint**
6. Copy the **Signing secret** (starts with `whsec_`)

## Step 2: Configure Application

### Update appsettings.json

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\MSSQLLocalDB;Database=DebtManager;Trusted_Connection=True;",
    "Hangfire": "Server=(localdb)\\MSSQLLocalDB;Database=DebtManager;Trusted_Connection=True;"
  },
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_KEY_HERE",
    "SecretKey": "sk_test_YOUR_KEY_HERE",
    "WebhookSecret": "whsec_YOUR_SECRET_HERE"
  }
}
```

### For Production (use User Secrets or Key Vault)

```bash
# Set Stripe keys using user secrets
dotnet user-secrets set "Stripe:SecretKey" "sk_live_YOUR_LIVE_KEY"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_YOUR_LIVE_SECRET"
```

## Step 3: Database Setup

### Run Migrations

```bash
cd src/DebtManager.Web
dotnet ef database update
```

This will create:
- Application tables (Debts, Transactions, etc.)
- Hangfire tables (for job persistence)

## Step 4: Run the Application

```bash
cd src/DebtManager.Web
dotnet run
```

The application will start on:
- HTTPS: https://localhost:5001
- HTTP: http://localhost:5000

## Step 5: Test Payment Flow

### Using Browser

1. Navigate to `/User/Payments/MakePayment`
2. Enter a debt reference or ID
3. Enter payment amount
4. Complete payment using test card: `4242 4242 4242 4242`
   - Any future expiry date
   - Any 3-digit CVC
   - Any postal code

### Test Cards

Stripe provides test cards for different scenarios:

| Card Number | Scenario |
|-------------|----------|
| 4242 4242 4242 4242 | Success |
| 4000 0000 0000 0002 | Card declined |
| 4000 0025 0000 3155 | 3D Secure authentication |
| 4000 0000 0000 9995 | Insufficient funds |

Full list: https://stripe.com/docs/testing

## Step 6: Test Webhooks Locally

### Install Stripe CLI

```bash
# macOS
brew install stripe/stripe-cli/stripe

# Windows
scoop bucket add stripe https://github.com/stripe/scoop-stripe-cli.git
scoop install stripe

# Linux
# Download from https://github.com/stripe/stripe-cli/releases
```

### Forward Webhooks

```bash
stripe login
stripe listen --forward-to localhost:5000/api/webhooks/stripe
```

The CLI will output a webhook signing secret - use this in your `Stripe:WebhookSecret` configuration.

### Trigger Test Events

```bash
stripe trigger payment_intent.succeeded
stripe trigger payment_intent.payment_failed
```

## Step 7: Access Hangfire Dashboard

1. Sign in as an admin user
2. Navigate to `/hangfire`
3. View job processing status

If you can't access:
- Ensure you're authenticated
- Verify your user has the Admin scope claim
- Check logs for authorization errors

## API Endpoints

### Find Debt by Reference
```http
GET /api/payment/find-by-reference?reference=D-5001
```

### Get Payment Methods
```http
GET /api/payment/methods
```

### Create Payment Intent
```http
POST /api/payment/create-intent
Content-Type: application/json

{
  "debtId": "guid-here",
  "amount": 100.50,
  "currency": "AUD"
}
```

### Payment Status
```http
GET /api/payment/status/{paymentIntentId}
```

## Payment Flow

### Anonymous/Quick Payment Flow

1. **User receives reference** - Via email/SMS from creditor
2. **Navigate to payment page** - `/Payment/Anonymous` or `/User/Payments/MakePayment`
3. **Enter reference** - System finds debt details
4. **View debt summary** - Amount, reference, organization
5. **Enter payment amount** - Full or partial payment
6. **Choose payment method** - Card, Apple Pay, Google Pay, etc.
7. **Complete payment** - Stripe handles secure processing
8. **Receive confirmation** - Redirected to success page
9. **Webhook processing** - Background job updates database
10. **Email receipt** - (Future: automatic receipt generation)

### Authenticated Payment Flow

1. **User logs in** - Via Azure AD B2C
2. **View dashboard** - See all debts
3. **Select debt** - Choose which debt to pay
4. **Make payment** - Same flow as steps 5-10 above

## Security Best Practices

### Production Deployment

1. **Never commit secrets** - Use environment variables or Key Vault
2. **Use HTTPS only** - Enforce SSL/TLS
3. **Rotate keys regularly** - Update Stripe keys periodically
4. **Monitor webhook events** - Set up alerts for failures
5. **Validate amounts** - Ensure payment amounts match debt amounts
6. **Rate limiting** - Implement rate limiting on API endpoints
7. **Audit logging** - Log all payment operations

### Webhook Security

The application automatically validates webhook signatures using:

```csharp
var stripeEvent = EventUtility.ConstructEvent(
    payload,
    signature,
    webhookSecret
);
```

Never disable this validation in production!

## Troubleshooting

### "Debt not found" error
- Verify the reference number is correct
- Check database has test debt data
- Review logs for database errors

### "Invalid webhook signature"
- Ensure `Stripe:WebhookSecret` matches Stripe Dashboard
- For local testing, use Stripe CLI webhook secret
- Check webhook endpoint URL is correct

### Payment succeeds but database not updated
- Check Hangfire dashboard for failed jobs
- Verify SQL Server connection string
- Review job logs for exceptions
- Ensure background job server is running

### Hangfire dashboard shows 403
- Sign in with admin credentials
- Verify admin scope claim is present
- Check `HangfireAuthorizationFilter` configuration

### Apple Pay/Google Pay not showing
- HTTPS is required for wallet payments
- Ensure domain is properly configured in Stripe
- Test on actual iOS/Android device (not simulators)

## Development Tips

### Sample Debt Creation

Create test debts using SQL:

```sql
INSERT INTO Debts (Id, OrganizationId, DebtorId, ClientReferenceNumber, 
    ExternalAccountId, OriginalPrincipal, OutstandingPrincipal, 
    Currency, Status, CreatedAtUtc, UpdatedAtUtc)
VALUES 
    (NEWID(), /* org id */, /* debtor id */, 'D-5001', 
    'ACC123456', 1000.00, 1000.00, 'AUD', 1, GETUTCDATE(), GETUTCDATE());
```

### Test Without Stripe

For development without Stripe setup:
1. Comment out Stripe API calls
2. Return mock data from payment service
3. Test UI and flow logic
4. Re-enable for integration testing

### Debugging Webhooks

Enable detailed logging:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "DebtManager.Infrastructure.Payments": "Debug"
      }
    }
  }
}
```

## Additional Resources

- [Full Documentation](./PAYMENT_FLOW.md)
- [Stripe Testing Guide](https://stripe.com/docs/testing)
- [Hangfire Documentation](https://docs.hangfire.io)
- [Stripe Payment Element](https://stripe.com/docs/payments/payment-element)
- [Stripe Webhooks](https://stripe.com/docs/webhooks)

## Support

For issues or questions:
1. Check application logs
2. Review Hangfire dashboard
3. Check Stripe Dashboard > Events
4. Review this documentation
5. Contact development team
