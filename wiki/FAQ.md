# Frequently Asked Questions (FAQ)

This document answers common questions about the Debt Management Platform.

## Table of Contents

- [General Questions](#general-questions)
- [Getting Started](#getting-started)
- [Authentication & Security](#authentication--security)
- [Payment Processing](#payment-processing)
- [Multi-Tenancy & Branding](#multi-tenancy--branding)
- [Development](#development)
- [Deployment & Operations](#deployment--operations)
- [Troubleshooting](#troubleshooting)

---

## General Questions

### What is the Debt Management Platform?

The Debt Management Platform is a white-labelled, multi-tenant web application built on .NET 8 that helps businesses manage debt collection for their customers. It supports three user roles:

- **Debtors (Users)** - Individuals managing their debts
- **Clients (Creditors)** - Businesses collecting debts
- **Admins** - Platform administrators

### What technologies does it use?

The platform is built with:
- **.NET 8** - Application framework
- **ASP.NET Core MVC** - Web framework
- **Entity Framework Core** - Data access
- **SQL Server** - Database
- **Azure AD B2C** - Authentication
- **Stripe** - Payment processing
- **Twilio** - SMS notifications
- **Hangfire** - Background jobs
- **Tailwind CSS** - UI styling

### What are the key features?

- Multi-tenant white-labeling with subdomain support
- Flexible payment plans (full, weekly, custom)
- Secure payment processing via Stripe
- Automated email and SMS notifications
- Background job processing
- Australian Business Register (ABR) validation
- Comprehensive admin portal
- Health monitoring and logging

### Is it open source?

No, this is a proprietary platform. All rights reserved.

---

## Getting Started

### How do I set up the development environment?

See the [Getting Started](Getting-Started.md) guide for detailed setup instructions. The basic steps are:

1. Install .NET 8 SDK, Node.js 20+, and Docker Desktop
2. Clone the repository
3. Run `dotnet restore` and `dotnet build`
4. Setup Tailwind CSS with `npm install` and `npm run build`
5. Run the application with `dotnet run --project src/DebtManager.Web`

### Do I need SQL Server installed?

No, you have two options:
- Use SQL Server LocalDB (included with Visual Studio/SQL Server Express)
- Run SQL Server in Docker with the provided `docker-compose.yml`

### How do I create the first admin user?

1. Navigate to `/Backoffice` in your browser
2. You'll see a one-time admin signup link (only visible when no admins exist)
3. Click the link and complete registration via Azure AD B2C
4. Your account is now an admin

After the first admin is created, only existing admins can assign admin roles to other users.

### Where do I find the admin portal?

The admin portal is accessible at `/Admin` once you have an admin account.

---

## Authentication & Security

### What authentication system does it use?

The platform uses **Azure AD B2C** (Business-to-Consumer) for authentication via OpenID Connect. This provides:
- Secure, cloud-based authentication
- Password reset flows
- Multi-factor authentication (MFA)
- Social login support (optional)

### How do I configure Azure AD B2C?

You need:
1. An Azure subscription
2. An Azure AD B2C tenant
3. A registered application in the tenant
4. Configuration values in `appsettings.json` or environment variables

See the [Getting Started](Getting-Started.md) guide for detailed configuration.

### How are passwords stored?

Passwords are not stored in the application. Azure AD B2C handles all password storage and management using industry-standard security practices.

### Is sensitive data encrypted?

Yes, sensitive data is encrypted in multiple ways:
- **In transit**: All connections use HTTPS/TLS
- **At rest**: Sensitive database fields are encrypted with AES
- **Production**: Transparent Data Encryption (TDE) on SQL Server

### How do I enable MFA?

MFA can be configured in Azure AD B2C user flows. Once enabled:
1. Users are prompted to set up MFA on first login
2. Subsequent logins require the second factor
3. Admins can enforce MFA for critical operations

### How are API keys and secrets managed?

- **Development**: Use .NET User Secrets (`dotnet user-secrets`)
- **Production**: Use Azure Key Vault
- **Never** commit secrets to source control

---

## Payment Processing

### What payment provider is used?

The platform uses **Stripe** for payment processing. Stripe handles:
- Hosted checkout pages
- Payment method storage
- PCI compliance
- Refunds and disputes

### What payment methods are supported?

Through Stripe, the platform supports:
- Credit and debit cards
- Bank transfers (ACH)
- Digital wallets (Apple Pay, Google Pay)

### How do payment webhooks work?

Stripe sends webhook events to `/api/webhooks/stripe` to notify the application of:
- Successful payments
- Failed payments
- Refunds
- Other payment events

The webhook endpoint verifies the signature and processes the event.

### Can I test payments without real money?

Yes! Use Stripe's test mode:
1. Use test API keys (starts with `sk_test_`)
2. Use test card numbers like `4242 4242 4242 4242`
3. Use the Stripe CLI to test webhooks locally

### How are payment plans structured?

Three payment plan types are available:

1. **Full Payment** - Pay entire debt immediately with maximum discount
2. **Weekly Plan** - System-generated weekly installments with partial discount
3. **Custom Plan** - Debtor-proposed schedule with admin fees, no discount

The discount percentages are configurable per client organization.

---

## Multi-Tenancy & Branding

### How does white-labeling work?

The platform supports white-labeling through the **BrandingResolverMiddleware**:

1. Request comes in for `client1.domain.com`
2. Middleware extracts subdomain (`client1`)
3. Looks up organization by subdomain
4. Loads organization's branding theme
5. Injects theme into request context
6. Views use the theme for colors, logo, etc.

### Can I use custom domains?

Yes, custom domain mapping is supported. For example:
- `payments.clientbusiness.com` can map to a specific client organization
- Configure custom domain in DNS
- Configure mapping in the platform

### What branding elements can be customized?

Currently supported:
- Organization name
- Primary brand color (hex value)
- Logo URL

Planned:
- Custom CSS
- Footer text
- Email templates
- SMS sender name

### What happens if a subdomain doesn't exist?

The platform falls back to the default branding with:
- Default organization name
- Default primary color (#0ea5e9 - blue)
- Default logo

---

## Development

### What IDE should I use?

Either works well:
- **Visual Studio 2022** - Full-featured IDE with built-in debugging and database tools
- **Visual Studio Code** - Lightweight editor with C# extensions

### Do I need to know Azure?

For basic development, no. The platform can run entirely locally with:
- LocalDB for database
- Stub implementations for external services

For production deployment, Azure knowledge is helpful.

### How do I run tests?

```bash
dotnet test
```

Tests use:
- **NUnit** - Test framework
- **Moq** - Mocking library
- **WebApplicationFactory** - Integration testing

### How do I create a database migration?

```bash
dotnet ef migrations add MigrationName \
  -p src/DebtManager.Infrastructure \
  -s src/DebtManager.Web
```

Then apply it:

```bash
dotnet ef database update \
  -p src/DebtManager.Infrastructure \
  -s src/DebtManager.Web
```

### How does the background job system work?

The platform uses **Hangfire** for background jobs:
- Jobs are defined as C# methods
- Scheduled using cron expressions
- Dashboard available at `/hangfire`
- Jobs persist in SQL Server (production) or memory (development)

Example job types:
- Nightly payment reminders
- Payment reconciliation
- Remittance generation
- Failed payment retries

### Can I add new features?

Yes, but follow the architecture:
1. Domain entities in `DebtManager.Domain`
2. Commands/queries in `DebtManager.Application`
3. Controllers/views in `DebtManager.Web`
4. Infrastructure in `DebtManager.Infrastructure`

See the [Development Guide](Development-Guide.md) for coding standards.

---

## Deployment & Operations

### What are the deployment options?

1. **Azure App Service** - Recommended for production
2. **Container** - Docker/Kubernetes
3. **IIS** - On-premises Windows Server

See the [Deployment](Deployment.md) guide for details.

### What are the infrastructure requirements?

**Minimum (Development):**
- 2 CPU cores
- 4 GB RAM
- SQL Server Express or LocalDB

**Recommended (Production):**
- Azure App Service Premium P1V2 or higher
- Azure SQL Managed Instance
- Azure Storage for blob storage
- Application Insights for monitoring

### How do I monitor the application?

Use Application Insights for:
- Request tracking
- Error logging
- Performance metrics
- Custom telemetry

Health checks available at:
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe

### How are database backups handled?

**Azure SQL Managed Instance:**
- Automatic daily backups
- Point-in-time restore (7-35 days)
- Long-term retention (configurable)

**Manual backups:**
Use Azure CLI or SQL Server Management Studio.

### How do I scale the application?

**Vertical scaling:**
- Increase App Service tier (P1V2 → P2V2 → P3V2)
- Increase database capacity

**Horizontal scaling:**
- Add more App Service instances
- Enable auto-scale rules
- Use read replicas for database

See [Deployment Guide - Scaling Strategies](Deployment.md#scaling-strategies).

---

## Troubleshooting

### The application won't start

**Check:**
1. .NET 8 SDK is installed
2. Database connection string is correct
3. Required environment variables are set
4. Check logs for specific errors

**View logs:**
```bash
dotnet run --project src/DebtManager.Web
```

### Database connection fails

**Solutions:**
1. Verify SQL Server is running
2. Check connection string format
3. Ensure database exists (run migrations)
4. Check firewall rules (Azure SQL)

### Tailwind CSS styles not updating

**Solution:**
```bash
cd src/DebtManager.Web
npm run build
```

For watch mode during development:
```bash
npm run watch
```

### Authentication redirects to wrong URL

**Check:**
1. Azure AD B2C redirect URIs include your URL
2. `CallbackPath` matches B2C configuration (`/signin-oidc`)
3. HTTPS is used (required for OIDC)

### Hangfire dashboard shows 404

**Check:**
1. You're logged in with admin scope
2. Route `/hangfire` is correctly mapped
3. Hangfire middleware is registered in `Program.cs`

### Stripe webhooks fail locally

**Solution:**

Use Stripe CLI to forward webhooks to localhost:

```bash
stripe listen --forward-to https://localhost:5001/api/webhooks/stripe
```

Copy the webhook signing secret and update configuration.

### Health checks return 503

**Check:**
1. Database is accessible
2. External API connections (if configured)
3. Review health check logs

**Test manually:**
```bash
curl https://localhost:5001/health/ready
```

### Background jobs aren't running

**Check:**
1. Hangfire server is started (check logs)
2. Jobs are properly scheduled
3. Check `/hangfire` dashboard for errors
4. Ensure database connection for job storage

### Can't create first admin

**Solution:**

The one-time admin signup link only appears at `/Backoffice` when:
- You're NOT logged in
- Zero admin users exist in the database

If you already have an admin, ask them to assign you the admin role.

### Where can I get more help?

1. Check this FAQ
2. Review the [User Guides](User-Guides.md) for workflow help
3. Check the [Development Guide](Development-Guide.md) for technical issues
4. Review logs and error messages
5. Contact the development team

---

## Additional Resources

- [Getting Started](Getting-Started.md) - Setup instructions
- [Architecture](Architecture.md) - Technical architecture
- [User Guides](User-Guides.md) - Role-specific workflows
- [API Reference](API-Reference.md) - Integration documentation
- [Development Guide](Development-Guide.md) - Coding standards
- [Deployment](Deployment.md) - Deployment procedures

---

**Can't find your question?** Contact the development team or submit a documentation request.
