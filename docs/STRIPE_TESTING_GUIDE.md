# Stripe Integration Testing Guide

## Overview

This guide explains how to test payment functionality with Stripe in development mode without processing real payments.

## Prerequisites

- Stripe Account (free test account)
- Stripe CLI installed (for webhook testing)
- .NET 8.0 SDK
- Running application instance

## Setup Stripe Test Mode

### 1. Get Test API Keys

1. Sign in to [Stripe Dashboard](https://dashboard.stripe.com)
2. **Toggle to Test Mode** (switch in top right corner)
3. Navigate to **Developers > API Keys**
4. Copy your test keys:
   - **Publishable key** (starts with `pk_test_`)
   - **Secret key** (starts with `sk_test_`)

### 2. Configure Application

Update `appsettings.Development.json` or use User Secrets:

```json
{
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_TEST_KEY_HERE",
    "SecretKey": "sk_test_YOUR_TEST_KEY_HERE",
    "WebhookSecret": "whsec_YOUR_WEBHOOK_SECRET_HERE"
  }
}
```

**Using User Secrets (recommended for development):**

```bash
cd src/DebtManager.Web

# Set Stripe test keys
dotnet user-secrets set "Stripe:PublishableKey" "pk_test_YOUR_KEY"
dotnet user-secrets set "Stripe:SecretKey" "sk_test_YOUR_KEY"
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_YOUR_SECRET"
```

## Install and Configure Stripe CLI

### Installation

**Windows (via Scoop):**
```bash
scoop install stripe
```

**macOS (via Homebrew):**
```bash
brew install stripe/stripe-cli/stripe
```

**Linux:**
```bash
# Download latest release
wget https://github.com/stripe/stripe-cli/releases/download/v1.19.0/stripe_1.19.0_linux_x86_64.tar.gz
tar -xvf stripe_1.19.0_linux_x86_64.tar.gz
sudo mv stripe /usr/local/bin/
```

### Login to Stripe CLI

```bash
stripe login
```

This will open a browser to authenticate with your Stripe account.

## Testing Payment Flow

### Method 1: Local Webhook Testing with Stripe CLI

1. **Start your application:**
```bash
cd src/DebtManager.Web
dotnet run
```

2. **In a new terminal, start Stripe webhook forwarding:**
```bash
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe
```

The CLI will output a webhook signing secret (starts with `whsec_`). Copy this and update your configuration:

```bash
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_YOUR_CLI_SECRET"
```

3. **Make a test payment:**
   - Navigate to `https://localhost:5001/User/Payments/MakePayment`
   - Enter a test debt reference
   - Use Stripe test cards (see below)
   - Complete the payment

4. **Monitor webhook events:**
   - The Stripe CLI will show webhook events in real-time
   - Check Hangfire dashboard at `/hangfire` for background jobs
   - Verify email notifications in logs

### Method 2: Stripe Dashboard Webhook Testing

1. **Set up webhook endpoint in Stripe Dashboard:**
   - Go to [Stripe Dashboard > Developers > Webhooks](https://dashboard.stripe.com/test/webhooks)
   - Click **Add endpoint**
   - URL: `https://your-dev-url.com/api/webhooks/stripe`
   - Select events:
     - `payment_intent.succeeded`
     - `payment_intent.payment_failed`
     - `payment_intent.canceled`
   - Click **Add endpoint**
   - Copy the **Signing secret**

2. **Update configuration with webhook secret**

3. **Test using ngrok or similar tool** for local development:
```bash
ngrok http https://localhost:5001
```

Use the ngrok URL as your webhook endpoint.

## Stripe Test Cards

### Successful Payments

| Card Number | Description |
|-------------|-------------|
| 4242 4242 4242 4242 | Visa - Always succeeds |
| 5555 5555 5555 4444 | Mastercard - Always succeeds |
| 3782 822463 10005 | American Express - Always succeeds |

### Failed Payments

| Card Number | Description |
|-------------|-------------|
| 4000 0000 0000 0002 | Card declined |
| 4000 0000 0000 9995 | Insufficient funds |
| 4000 0000 0000 0069 | Card expired |
| 4000 0000 0000 0127 | Incorrect CVC |
| 4000 0000 0000 0119 | Processing error |

### 3D Secure Authentication

| Card Number | Description |
|-------------|-------------|
| 4000 0027 6000 3184 | 3D Secure required - authenticate |
| 4000 0082 6000 3178 | 3D Secure required - fails |

### Other Test Scenarios

| Card Number | Description |
|-------------|-------------|
| 4000 0000 0000 3220 | Dispute after successful payment |
| 4000 0000 0000 5126 | Refund after successful payment |

**For all test cards:**
- Use any future expiry date (e.g., 12/25)
- Use any 3-digit CVC (4-digit for Amex)
- Use any postal code

## Testing Payment Notifications

### Test Receipt Email

1. Make a successful payment using test card `4242 4242 4242 4242`
2. Check logs for email sending:
   ```
   Receipt email sent to {email} for transaction {transactionId}
   ```
3. Verify Hangfire job completed successfully
4. Check application logs for email content (if using console email sender in dev)

### Test Failure Notification

1. Make a failed payment using test card `4000 0000 0000 0002`
2. Check logs for failure notification:
   ```
   Payment failure email sent to {email}
   ```
3. Verify appropriate error handling in UI

## Monitoring and Debugging

### Hangfire Dashboard

Access at: `https://localhost:5001/hangfire`

**Monitor:**
- Succeeded jobs (receipt generation, notifications)
- Failed jobs (retry information)
- Scheduled jobs (failed payment retries)
- Processing jobs (real-time execution)

**Features:**
- Retry failed jobs manually
- View job arguments and stack traces
- Monitor queue lengths
- Check recurring jobs

### Stripe Dashboard

**Test Mode Features:**
- View all test payments in **Payments** section
- Monitor webhook deliveries in **Developers > Webhooks**
- Replay webhook events for testing
- View detailed logs for each payment

**Useful Views:**
- Payment Intents: See all payment attempts
- Logs: Real-time API request/response logs
- Events: Complete event history

### Application Logs

Enable detailed logging for payment operations:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "DebtManager.Infrastructure.Payments": "Debug",
      "DebtManager.Web.Controllers.PaymentApiController": "Debug"
    }
  }
}
```

## Common Test Scenarios

### 1. Successful Payment Flow

```bash
# Steps:
1. Navigate to /User/Payments/MakePayment
2. Enter debt reference: D-5001
3. Enter amount: $100.00
4. Use test card: 4242 4242 4242 4242
5. Complete payment

# Expected Results:
- Payment succeeds
- Receipt generated (Hangfire job)
- Success email sent (Hangfire job)
- Debt balance updated
- Transaction recorded with status: Succeeded
```

### 2. Failed Payment Flow

```bash
# Steps:
1. Navigate to /User/Payments/MakePayment
2. Enter debt reference: D-5001
3. Enter amount: $100.00
4. Use test card: 4000 0000 0000 0002
5. Attempt payment

# Expected Results:
- Payment fails
- Redirect to /User/Payments/Failed
- Failure email sent (Hangfire job)
- Transaction recorded with status: Failed
- Failure reason logged
```

### 3. Webhook Processing

```bash
# Using Stripe CLI:
stripe trigger payment_intent.succeeded

# Expected Results:
- Webhook received at /api/webhooks/stripe
- Signature validated
- Hangfire job queued
- Transaction created/updated
- Receipt and notifications sent
```

### 4. Admin Adhoc Payment

```bash
# Steps:
1. Navigate to /Admin/Payments/CreateAdhoc
2. Enter debt reference
3. Select payment method: Cash
4. Enter amount
5. Add notes
6. Submit

# Expected Results:
- Transaction created with Manual provider
- Debt balance updated
- Audit log entry created
- Success notification sent
```

## Troubleshooting

### Webhook Not Received

**Check:**
1. Stripe CLI is running and forwarding to correct port
2. Application is running on expected port (5001)
3. Webhook secret matches CLI output
4. Check firewall/antivirus settings

**Solution:**
```bash
# Restart Stripe CLI
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe

# Copy new webhook secret
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_NEW_SECRET"
```

### Hangfire Jobs Failing

**Check:**
1. SQL Server connection is working
2. Hangfire tables exist (run migrations)
3. Background server is running
4. Check job logs in Hangfire dashboard

**Solution:**
```bash
# Check Hangfire dashboard
https://localhost:5001/hangfire

# View failed job details
# Retry manually or check stack trace
```

### Email Not Sending

**Check:**
1. IEmailSender implementation is registered
2. Email configuration is correct
3. For development, email sender might just log

**Solution:**
```csharp
// In development, emails are logged to console
// Check application logs for email content
// For actual sending, configure SMTP or email service
```

### Invalid Signature Error

**Check:**
1. Webhook secret matches Stripe CLI or Dashboard
2. Using test mode keys (pk_test_, sk_test_)
3. Webhook payload is not modified

**Solution:**
```bash
# Get current webhook secret from Stripe CLI
stripe listen --print-secret

# Update configuration
dotnet user-secrets set "Stripe:WebhookSecret" "whsec_CORRECT_SECRET"
```

## Testing Checklist

- [ ] Configure Stripe test mode keys
- [ ] Install and authenticate Stripe CLI
- [ ] Start webhook forwarding
- [ ] Test successful payment with test card 4242...
- [ ] Verify receipt generation in Hangfire
- [ ] Verify success email in logs
- [ ] Test failed payment with test card 4000...0002
- [ ] Verify failure notification
- [ ] Test admin adhoc payment creation
- [ ] Monitor Hangfire jobs completion
- [ ] Check audit logs for all operations
- [ ] Test webhook replay from Stripe Dashboard
- [ ] Verify payment status updates
- [ ] Test 3D Secure flow

## Production Deployment

Before deploying to production:

1. **Switch to Live Mode:**
   - Use live API keys (pk_live_, sk_live_)
   - Configure production webhook endpoint
   - Update webhook signing secret

2. **Security:**
   - Store keys in Azure Key Vault or secure secrets manager
   - Never commit keys to source control
   - Use environment variables in production

3. **Monitoring:**
   - Set up application insights
   - Configure alerts for failed payments
   - Monitor Hangfire job failures
   - Track email delivery rates

4. **Testing:**
   - Use Stripe test mode for staging environment
   - Test webhook failover and retry logic
   - Verify email delivery in production email service

## Resources

- [Stripe Testing Guide](https://stripe.com/docs/testing)
- [Stripe CLI Documentation](https://stripe.com/docs/stripe-cli)
- [Webhook Testing](https://stripe.com/docs/webhooks/test)
- [Test Card Numbers](https://stripe.com/docs/testing#cards)
- [Hangfire Documentation](https://docs.hangfire.io/)
