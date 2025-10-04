# API Reference

This document provides technical reference for integrating with the Debt Management Platform.

## Table of Contents

- [Authentication](#authentication)
- [Stripe Integration](#stripe-integration)
- [Twilio Integration](#twilio-integration)
- [ABR Validation Service](#abr-validation-service)
- [Webhook Endpoints](#webhook-endpoints)
- [Health Check Endpoints](#health-check-endpoints)

---

## Authentication

The platform uses **Azure AD B2C** for authentication via OpenID Connect (OIDC).

### Configuration

**Required Settings:**

```json
{
  "AzureAdB2C": {
    "ClientId": "your-client-id",
    "Instance": "https://your-tenant.b2clogin.com/",
    "Domain": "your-tenant.onmicrosoft.com",
    "Authority": "https://your-tenant.b2clogin.com/your-tenant.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

### User Flows

**Available Flows:**
- `B2C_1_SIGNUP_SIGNIN` - Combined sign-up and sign-in
- `B2C_1_PASSWORD_RESET` - Password reset
- `B2C_1_PROFILE_EDIT` - Profile editing

### Scopes

**Role-Based Scopes:**

```json
{
  "AzureB2CScopes": {
    "Admin": "https://tenant.onmicrosoft.com/app-id/Consolidated.Administrator",
    "Client": "https://tenant.onmicrosoft.com/app-id/Consolidated.Client",
    "User": "https://tenant.onmicrosoft.com/app-id/Consolidated.User"
  }
}
```

### Authorization Policies

**Policy Names:**
- `RequireUserScope` - Debtor access
- `RequireClientScope` - Client access
- `RequireAdminScope` - Admin access

**Usage in Controllers:**

```csharp
[Authorize(Policy = "RequireAdminScope")]
public class AdminController : Controller
{
    // Admin-only actions
}
```

### Sign-In Endpoints

**User Sign-In:**
- URL: `/Account/SignInUser`
- Redirects to Azure AD B2C user flow
- Returns to app with authentication token

**Client Sign-In:**
- URL: `/Account/SignInClient`
- Redirects to Azure AD B2C client flow
- Returns to app with authentication token

**Admin Sign-In:**
- URL: `/Account/SignInAdmin` (via backoffice)
- Requires admin scope assignment
- First admin created via special one-time flow

**Sign-Out:**
- URL: `/Account/SignOutUser`
- Clears authentication cookies
- Redirects to Azure AD B2C sign-out

---

## Stripe Integration

The platform uses **Stripe** for payment processing.

### Configuration

**Required Settings:**

```json
{
  "Stripe": {
    "SecretKey": "sk_test_...",
    "PublishableKey": "pk_test_...",
    "WebhookSecret": "whsec_..."
  }
}
```

**Environment Variables (Recommended):**

```bash
Stripe__SecretKey=sk_test_your_secret_key
Stripe__PublishableKey=pk_test_your_publishable_key
Stripe__WebhookSecret=whsec_your_webhook_secret
```

### Payment Flow

**Checkout Session Creation:**

1. User selects payment plan
2. Backend creates Stripe Checkout Session
3. User redirected to Stripe-hosted checkout
4. User completes payment
5. Stripe redirects back to success/cancel URL
6. Webhook confirms payment

**Checkout Session Parameters:**

```csharp
var options = new SessionCreateOptions
{
    PaymentMethodTypes = new List<string> { "card" },
    LineItems = new List<SessionLineItemOptions>
    {
        new SessionLineItemOptions
        {
            PriceData = new SessionLineItemPriceDataOptions
            {
                Currency = "aud",
                ProductData = new SessionLineItemPriceDataProductDataOptions
                {
                    Name = "Debt Payment",
                    Description = $"Payment for debt #{debtId}"
                },
                UnitAmount = amount * 100 // Amount in cents
            },
            Quantity = 1
        }
    },
    Mode = "payment",
    SuccessUrl = $"{baseUrl}/payment/success?session_id={{CHECKOUT_SESSION_ID}}",
    CancelUrl = $"{baseUrl}/payment/cancel"
};
```

### Webhook Events

**Handled Events:**

- `checkout.session.completed` - Payment successful
- `payment_intent.succeeded` - Payment confirmed
- `payment_intent.payment_failed` - Payment failed
- `charge.refunded` - Refund processed

**Webhook Endpoint:**

```
POST /api/webhooks/stripe
```

**Signature Verification:**

```csharp
var json = await new StreamReader(Request.Body).ReadToEndAsync();
var stripeEvent = EventUtility.ConstructEvent(
    json,
    Request.Headers["Stripe-Signature"],
    webhookSecret
);
```

### Testing

**Stripe CLI for Local Development:**

```bash
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe
```

**Test Card Numbers:**

- Success: `4242 4242 4242 4242`
- Decline: `4000 0000 0000 0002`
- Auth Required: `4000 0025 0000 3155`

---

## Twilio Integration

The platform uses **Twilio** for SMS communications.

### Configuration

**Required Settings:**

```json
{
  "Twilio": {
    "AccountSid": "AC...",
    "AuthToken": "...",
    "FromPhoneNumber": "+61..."
  }
}
```

### SMS Types

**Supported Messages:**

1. **OTP Verification** - One-time passwords for account verification
2. **Payment Reminders** - Upcoming payment notifications
3. **Receipt Confirmations** - Payment confirmation
4. **Status Updates** - Debt status changes

### SMS Service Interface

```csharp
public interface ISmsService
{
    Task SendAsync(string to, string message, CancellationToken ct = default);
    Task SendOtpAsync(string to, string code, CancellationToken ct = default);
}
```

### Usage Example

```csharp
await _smsService.SendAsync(
    to: debtor.PhoneNumber,
    message: $"Payment reminder: ${amount} due on {dueDate:d}",
    ct: cancellationToken
);
```

### Rate Limiting

- Max 1 SMS per phone number per minute
- Max 10 SMS per phone number per day
- Respects opt-out requests

---

## ABR Validation Service

The **Australian Business Register (ABR) API** validates business numbers.

### Configuration

**Required Settings:**

```json
{
  "AbrApi": {
    "BaseUrl": "https://abr.business.gov.au/abrxmlsearch/AbrXmlSearch.asmx",
    "ApiKey": "your-api-key",
    "DefinitionUrl": "https://abr.business.gov.au/ApiDocumentation"
  }
}
```

**Stub Mode:**

If `BaseUrl` is not configured, a local stub validator is used that:
- Validates 11-digit format
- Applies basic checksum
- Useful for development/testing

### Service Interface

```csharp
public interface IAbrValidator
{
    Task<AbrValidationResult> ValidateAbnAsync(string abn, CancellationToken ct = default);
}
```

### Validation Result

```csharp
public class AbrValidationResult
{
    public bool IsValid { get; set; }
    public string? BusinessName { get; set; }
    public string? Abn { get; set; }
    public string? EntityType { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Usage Example

```csharp
var result = await _abrValidator.ValidateAbnAsync("51 824 753 556", ct);
if (result.IsValid)
{
    // Proceed with onboarding
    organization.BusinessName = result.BusinessName;
}
else
{
    // Show error to user
    ModelState.AddModelError("Abn", result.ErrorMessage);
}
```

### ABN Format

- 11 digits
- Can be formatted with spaces: `51 824 753 556`
- Can be unformatted: `51824753556`
- Validation removes spaces automatically

---

## Webhook Endpoints

### Stripe Webhooks

**Endpoint:** `POST /api/webhooks/stripe`

**Headers:**
- `Stripe-Signature` - Required for verification

**Events Processed:**
- `checkout.session.completed`
- `payment_intent.succeeded`
- `payment_intent.payment_failed`
- `charge.refunded`

**Response:**
- `200 OK` - Event processed successfully
- `400 Bad Request` - Invalid signature or payload
- `500 Internal Server Error` - Processing error

### Custom Webhooks (Future)

**Planned Integrations:**
- SendGrid email events
- Twilio delivery status
- Third-party payment providers

---

## Health Check Endpoints

### Liveness Probe

**Endpoint:** `GET /health/live`

**Purpose:** Check if application is running

**Response:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0123456"
}
```

**Status Codes:**
- `200` - Application is running
- `503` - Application is unhealthy

### Readiness Probe

**Endpoint:** `GET /health/ready`

**Purpose:** Check if application can handle requests

**Checks:**
- Database connectivity
- External API availability (if configured)

**Response:**

```json
{
  "status": "Healthy",
  "totalDuration": "00:00:00.0456789",
  "entries": {
    "database": {
      "status": "Healthy",
      "description": "Database connection successful"
    }
  }
}
```

**Status Codes:**
- `200` - Ready to serve traffic
- `503` - Not ready (dependencies unavailable)

### Usage in Kubernetes

```yaml
livenessProbe:
  httpGet:
    path: /health/live
    port: 8080
  initialDelaySeconds: 30
  periodSeconds: 10

readinessProbe:
  httpGet:
    path: /health/ready
    port: 8080
  initialDelaySeconds: 5
  periodSeconds: 5
```

---

## Background Job API

### Hangfire Dashboard

**Endpoint:** `/hangfire`

**Authentication:** Admin scope required

**Features:**
- View job status
- Trigger jobs manually
- View job history
- Configure recurring schedules

### Job Types

**Recurring Jobs:**

1. **Nightly Reminders** - Sends payment reminders
   - Cron: `0 9 * * *` (9 AM daily)

2. **Payment Reconciliation** - Matches payments to debts
   - Cron: `0 1 * * *` (1 AM daily)

3. **Remittance Generation** - Creates payout reports
   - Cron: `0 0 * * 1` (Monday midnight)

4. **Failed Payment Retry** - Retries failed transactions
   - Cron: `0 */4 * * *` (Every 4 hours)

### Programmatic Job Creation

```csharp
// Enqueue immediate job
BackgroundJob.Enqueue<PaymentReminderJob>(job => job.SendReminders());

// Schedule delayed job
BackgroundJob.Schedule<ReportJob>(
    job => job.GenerateReport(reportId),
    TimeSpan.FromHours(1)
);

// Recurring job
RecurringJob.AddOrUpdate<NightlyJob>(
    "nightly-reconciliation",
    job => job.ReconcilePayments(),
    Cron.Daily(1) // 1 AM
);
```

---

## Email Service API

### Email Service Interface

```csharp
public interface IEmailService
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
    Task SendTemplateAsync(string templateId, object data, string to, CancellationToken ct = default);
}
```

### Email Message Model

```csharp
public class EmailMessage
{
    public string To { get; set; }
    public string Subject { get; set; }
    public string HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public List<Attachment>? Attachments { get; set; }
}
```

### Template Variables

**Available Merge Fields:**

- `{DebtorName}` - Debtor's full name
- `{Amount}` - Amount owed/paid
- `{DueDate}` - Payment due date
- `{ReferenceId}` - Debt reference ID
- `{ClientName}` - Creditor organization name
- `{PaymentUrl}` - Link to make payment

### Usage Example

```csharp
await _emailService.SendTemplateAsync(
    templateId: "payment-reminder",
    data: new
    {
        DebtorName = debtor.FullName,
        Amount = payment.Amount.ToString("C"),
        DueDate = payment.DueDate.ToString("d"),
        PaymentUrl = $"https://app.domain.com/pay/{payment.Id}"
    },
    to: debtor.Email
);
```

---

## Error Handling

### Standard Error Response

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Amount": ["Amount must be greater than 0"],
    "Email": ["Email address is invalid"]
  }
}
```

### Status Codes

- `200 OK` - Success
- `201 Created` - Resource created
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `409 Conflict` - Resource conflict
- `500 Internal Server Error` - Server error

---

## Rate Limiting

**Global Limits:**
- 1000 requests per minute per IP
- 10,000 requests per hour per authenticated user

**Specific Limits:**
- SMS: 10 per day per phone number
- Email: 50 per day per email address
- API keys: 100 requests per minute

**Headers:**

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 950
X-RateLimit-Reset: 1635724800
```

---

**See Also:**
- [Architecture](Architecture.md) - System architecture
- [Development Guide](Development-Guide.md) - Development practices
- [Deployment](Deployment.md) - Deployment procedures
