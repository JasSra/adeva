# Quick Start Guide - 2-Click Payment Flow

## For End Users (Debtors)

### Scenario 1: Anonymous Payment (No Login Required)

1. **Receive Reference**
   ```
   You receive an email/SMS:
   "Your debt reference: D-5001
   Pay now at: https://debt.example.com/Payment/Anonymous"
   ```

2. **Visit Payment Page**
   - Navigate to the link
   - Enter reference: `D-5001`
   - Click "Find Debt"

3. **Review & Pay**
   - See debt amount: $1,250.50
   - Choose payment amount (full or partial)
   - Click quick buttons: 25%, 50%, or 100%
   - Choose payment method:
     * 💳 Card (Visa, Mastercard, Amex)
     * 🍎 Apple Pay
     * 🤖 Google Pay
     * 🏦 Bank Transfer

4. **Complete Payment**
   - Click "Pay Now"
   - Complete payment on Stripe (2FA if required)
   - Receive confirmation

### Scenario 2: Authenticated Payment (Logged In)

1. **Sign In**
   - Go to `/User`
   - Sign in with your credentials

2. **Navigate to Payments**
   - Click "Make Payment" from dashboard
   - System auto-loads your debt

3. **Complete Payment**
   - Same as steps 3-4 above

## For Developers

### Setup (5 minutes)

```bash
# 1. Clone and build
git clone https://github.com/JasSra/adeva
cd adeva
dotnet restore
dotnet build

# 2. Configure Stripe
# Edit src/DebtManager.Web/appsettings.json
{
  "Stripe": {
    "PublishableKey": "pk_test_YOUR_KEY",
    "SecretKey": "sk_test_YOUR_KEY",
    "WebhookSecret": "whsec_YOUR_SECRET"
  }
}

# 3. Run app
dotnet run --project src/DebtManager.Web

# 4. Test payment
# Visit: https://localhost:5001/User/Payments/MakePayment
# Use test card: 4242 4242 4242 4242
```

### Testing Webhooks Locally

```bash
# Install Stripe CLI
brew install stripe/stripe-cli/stripe

# Forward webhooks to local dev
stripe login
stripe listen --forward-to localhost:5000/api/webhooks/stripe

# In another terminal, trigger test events
stripe trigger payment_intent.succeeded
```

## For Administrators

### Access Hangfire Dashboard

1. **Sign In as Admin**
   - Ensure you have Admin scope

2. **Navigate to Dashboard**
   - URL: `/hangfire`
   - View job queue, processing, and history

3. **Monitor Payment Jobs**
   - See webhook processing jobs
   - View success/failure rates
   - Retry failed jobs manually

### Check Payment Status

**Via Database:**
```sql
SELECT TOP 10 
    t.Id,
    t.Amount,
    t.Status,
    t.Method,
    t.ProcessedAtUtc,
    d.ClientReferenceNumber
FROM Transactions t
JOIN Debts d ON t.DebtId = d.Id
ORDER BY t.ProcessedAtUtc DESC
```

**Via Hangfire:**
1. Go to `/hangfire`
2. Click "Jobs" > "Succeeded"
3. Filter by job type: `PaymentWebhookJob`

## API Usage Examples

### Find Debt by Reference

```bash
curl -X GET "https://localhost:5001/api/payment/find-by-reference?reference=D-5001"
```

Response:
```json
{
  "debtId": "123e4567-e89b-12d3-a456-426614174000",
  "reference": "D-5001",
  "amount": 1250.50,
  "currency": "AUD",
  "organizationId": "223e4567-e89b-12d3-a456-426614174000"
}
```

### Create Payment Intent

```bash
curl -X POST "https://localhost:5001/api/payment/create-intent" \
  -H "Content-Type: application/json" \
  -d '{
    "debtId": "123e4567-e89b-12d3-a456-426614174000",
    "amount": 1250.50,
    "currency": "AUD"
  }'
```

Response:
```json
{
  "intentId": "pi_3AbC123xyz",
  "clientSecret": "pi_3AbC123xyz_secret_456def",
  "amount": 1250.50,
  "currency": "AUD",
  "supportedMethods": ["Card", "BankTransfer"]
}
```

### Get Payment Methods

```bash
curl -X GET "https://localhost:5001/api/payment/methods"
```

Response:
```json
[
  {
    "method": "Card",
    "displayName": "Credit/Debit Card",
    "isEnabled": true,
    "supportsWallets": true
  },
  {
    "method": "BankTransfer",
    "displayName": "Bank Transfer",
    "isEnabled": true,
    "supportsWallets": false
  }
]
```

## Common Scenarios

### Test Successful Payment

```javascript
// Use in browser console on payment page
const testPayment = async () => {
  // 1. Find debt
  const debt = await fetch('/api/payment/find-by-reference?reference=D-5001')
    .then(r => r.json());
  
  // 2. Create intent
  const intent = await fetch('/api/payment/create-intent', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      debtId: debt.debtId,
      amount: debt.amount,
      currency: 'AUD'
    })
  }).then(r => r.json());
  
  console.log('Payment intent created:', intent.intentId);
  
  // 3. Complete payment using Stripe Elements
  // (handled by UI - use test card 4242 4242 4242 4242)
};
```

### Monitor Webhook Processing

```bash
# Watch logs in real-time
dotnet run --project src/DebtManager.Web | grep "Payment"

# Expected output:
# [12:34:56 INF] Created Stripe payment intent pi_xxx for debt abc-123
# [12:35:01 INF] Received Stripe webhook: payment_intent.succeeded - evt_xxx
# [12:35:02 INF] Processing payment webhook event evt_xxx
# [12:35:03 INF] Payment succeeded for debt abc-123, amount 1250.50 AUD
```

## UI Flow Diagram

```
┌─────────────────────────────────────────────────────────────┐
│  1. User enters debt reference                              │
│     [D-5001        ] [Find Debt]                           │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│  2. System displays debt summary                            │
│     Reference: D-5001                                       │
│     Amount: $1,250.50                                       │
│                                                             │
│     Payment Amount: [$ 1250.50   ]                         │
│     Quick: [25%] [50%] [Full Amount]                       │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│  3. Choose payment method (Stripe Elements)                 │
│     ┌─────────────────────────────────────────────┐        │
│     │  💳 Card                                    │        │
│     │  🍎 Apple Pay                               │        │
│     │  🤖 Google Pay                              │        │
│     │  🏦 Bank Transfer                           │        │
│     └─────────────────────────────────────────────┘        │
│                                                             │
│     [Pay Now]  [Cancel]                                    │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│  4. Stripe processes payment (with 3DS if required)         │
│     → Webhook sent to /api/webhooks/stripe                 │
│     → Hangfire queues background job                       │
│     → Job processes payment event                          │
│     → Database updated                                      │
└───────────────────┬─────────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────────────────────────────┐
│  5. Success page shown                                      │
│     ✓ Payment Successful!                                  │
│                                                             │
│     Payment ID: pi_3AbC123xyz                              │
│     Amount: $1,250.50                                      │
│     Status: Completed                                       │
│                                                             │
│     [Go to Dashboard]  [View Payment History]              │
└─────────────────────────────────────────────────────────────┘
```

## Troubleshooting Quick Reference

| Issue | Solution |
|-------|----------|
| "Debt not found" | Verify reference exists in database |
| Webhook not processing | Check Stripe Dashboard > Webhooks > Event history |
| Payment succeeded but DB not updated | Check Hangfire dashboard for failed jobs |
| Apple Pay not showing | Must use HTTPS and real iOS device |
| Dashboard 403 error | Sign in with admin credentials |

## Next Steps

1. ✅ Test with Stripe test cards
2. ✅ Monitor Hangfire dashboard
3. ✅ Review webhook event logs
4. ✅ Test mobile responsiveness
5. ✅ Deploy to staging environment
6. ✅ Configure production Stripe keys
7. ✅ Set up monitoring and alerts

---

📖 **Full Documentation**: See `docs/PAYMENT_FLOW.md` and `docs/PAYMENT_SETUP.md`

🔧 **Support**: Check logs, Hangfire dashboard, and Stripe Dashboard > Events
