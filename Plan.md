# Debt Management Platform — Senior Developer Plan

## Mission Snapshot
- Build a **white-labelled debt management MVC web app** on **.NET 8**.
- Support three roles: Debtor (`User`), Creditor (`Client`), and Admin (`Admin`).
- Core integrations: Docker-hosted SQL Server Express, EF Core, MSAL auth, Stripe, Tailwind, Twilio, ABR API, Hangfire nightly jobs.
- Multi-tenant branding by subdomain or custom domain → fallback to default org theme.
- Non-functional goals: security, scalability, maintainability, testability.

---

## DO NOT
- Use client-side frameworks (React, Angular, Vue).
- Over-abstract or over-document without ROI.
- Add unapproved features outside milestones.

---

## High-Level Architecture

### Layers
- **Presentation**: ASP.NET Core MVC with Areas (`/Areas/User`, `/Areas/Client`, `/Areas/Admin`). Razor + Tailwind.
- **Application**: CQRS with MediatR. FluentValidation. AutoMapper.
- **Domain**: Aggregates (`Organization`, `Debtor`, `Debt`, `PaymentPlan`, `Transaction`, etc.).
- **Infrastructure**: EF Core repositories, UoW, external service adapters.
- **Cross-cutting**: Serilog, OpenTelemetry, Polly, global exception handling, health checks.

### Modularization (avoid monolith)

- Introduce a `Contracts` project for interfaces/DTOs consumed by multiple services (e.g., `IAbrValidator`).
- Each integration can be a small, self-contained service (e.g., `Services.AbrValidation`).
- The Web app depends only on `Contracts` and treats implementations via DI; environments can swap stub vs HTTP adapter.
- EF and external adapters live in Infrastructure, but can be extracted per service as needed.

### Services
- **Background jobs**: Hangfire + hosted services.
- **Integrations**: Stripe (payments), Twilio (SMS), SMTP/SendGrid (email), ABR API.
- **Security**: OIDC/MSAL auth, MFA, HTTPS enforced, AES + TDE encryption, centralized secret storage.
- **Branding Resolver**: Middleware resolves host → organization → theme injection.

### State & Caching
- Minimal session/TempData.
- Memory cache for branding metadata with invalidation.

### Testing
- NUnit + Moq.
- Integration tests with WebApplicationFactory.
- Worker tests for background jobs.
- Playwright for smoke routing tests.

---

## Workflows & User Journeys

### Debtor
1. Receive reference ID → register → validate via OTP (email/SMS).
2. Accept debt → offered 3 plan types:
   - **Full now with discount.**
   - **System-generated weekly plan with partial discount.**
   - **Custom schedule with admin fees, no discount.**
3. View debt summary → Stripe-hosted checkout → messages & reminders.

### Client
1. Onboard with ABR validation → pending approval by Admin.
2. Configure remittance schedule, fees, branding, bank details.
3. Upload debtor invoices → track statuses → review messages.
4. View remittance and payout reports.

### Admin
1. Approve/reject clients.
2. Manage templates, fees, payout approvals.
3. Monitor background jobs, dashboards, system health.

### Branding
- `client1.domain.com` → resolves to Client1 branding.
- `client2.domain.com` → resolves to Client2 branding.
- Unknown → default branding.

### Background Jobs
- Nightly reminders, payment reconciliation, failed retry queue.
- Remittance sheet generation.
- Invoice extraction orchestration.
- Observability: logs, retries, dead-letter queue.

### Configuration
- `appsettings.json` = bootstrap only.
- DB-backed config entity for API keys, SMTP creds, Twilio keys.
- Editable via secure Admin UI.

### Security
- Sensitive fields `[SensitiveData]` encrypted.
- MFA for critical actions.
- Centralized logging & audits.
- Automatic dependency updates & security checks.

---

## Environment & Tooling Setup
- Requirements: .NET 8 SDK, Node.js 20, Docker Desktop, Azure CLI, Stripe CLI, Twilio CLI.
- Repo structure:
  - `src/DebtManager.Contracts`
  - `src/DebtManager.Web`
  - `src/DebtManager.Application`
  - `src/DebtManager.Domain`
  - `src/DebtManager.Infrastructure`
  - `src/Services.AbrValidation`
  - `tests/DebtManager.Tests`
  - `deploy/`
- Dependencies: EF Core, Microsoft.Identity.Web, Stripe.net, Twilio, Hangfire, FluentValidation, Polly, Tailwind, PostCSS, Serilog.

---

## Immediate Milestones
1. Scaffold solution + Tailwind pipeline.
2. Entity models + EF migrations.
3. Authentication & authorization.
4. Branding resolver middleware.
5. Core dashboards (Debtor, Client, Admin).
6. Templates + messaging service.
7. Stripe integration + webhooks.
8. Invoice upload pipeline (stub OCR).
9. Nightly Hangfire jobs.
10. Unit, integration, Playwright tests.

---

## AI / ML Integration (Exploratory)
- Payment plan recommendations.
- Chatbot for debtor inquiries.
- Invoice OCR (Azure Form Recognizer).
- Risk prediction & fraud detection.
- AI-powered analytics for Admin dashboards.

---

## Dashboarding & Analytics
- Chart.js/D3.js dashboards.
- Admin metrics: debts, repayments, client KPIs.
- Drill-downs, CSV/Excel exports.
- SignalR for real-time metrics.
- Optional Power BI integration.

---

## Development Workflow Guidelines
- Feature branches → PR → main.
- Automated builds/tests/deploys.
- EF migrations tracked in repo.
- Background services idempotent (outbox pattern for messages).

---

## Deployment & Ops
- Target: Azure App Service or container platform.
- Production DB: Azure SQL Managed Instance.
- Health endpoints: `/health/live`, `/health/ready`.
- Monitoring: Application Insights.
- SSL automation, GDPR compliance, retention policies.

---

## Open Questions
- OCR vendor choice.
- Feature flag provider.
- Email provider decision.
- SLA/HA for background workers.
- Branding asset storage (Azure Blob + CDN).

---

## Next-Action Checklist
- [x] Scaffold solution & Docker Compose.
- [x] Branding resolver stub + tests.
- [x] EF Core entities + migrations. <!-- migrations pending actual dotnet ef -->
- [x] Tailwind pipeline + shared layout.
- [x] Admin onboarding flow + ABR validation placeholder.
- [x] Document config/secrets strategy.