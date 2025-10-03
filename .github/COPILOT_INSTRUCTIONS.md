# Copilot Instructions for Debt Management Platform

These instructions ensure AI coding assistants (e.g., GitHub Copilot) generate **consistent, compliant, and useful code**.

---

## General Guidelines
- **Framework**: .NET 8 (ASP.NET Core MVC, Razor pages, Tailwind).
- **Language**: C# for backend, minimal JS for Razor interop only.
- **Frontend**: Tailwind + PostCSS only. No React/Angular/Vue.
- **Testing**: NUnit + Moq. Integration tests with WebApplicationFactory.
- **Security**: Always enforce HTTPS, OIDC auth, MFA for critical actions.
- **Patterns**: CQRS, DI, Repository + UoW, MediatR, FluentValidation.
- **Error handling**: Centralized middleware, Serilog logging.
- **Secrets**: Never hardcode. Use env vars or DB-backed configuration.

---

## Coding Conventions
- Use **async/await** for all I/O.
- Use **CancellationToken** in services and handlers.
- Validate all DTOs with **FluentValidation**.
- Keep **entities clean** (no EF attributes leaking into domain).
- Use **AutoMapper** for DTO ↔ entity conversions.
- Add **unit tests** for each handler/service.
- Log structured events via Serilog (`Log.ForContext<>()`).

---

## Razor & Tailwind
- Use `_Layout.cshtml` for shared header/footer/nav.
- Responsive design first. Apply Tailwind utility classes.
- Extract UI into **partial views** for reuse.
- Support dark mode (`dark:` classes).
- For forms, include validation summaries + anti-forgery tokens.

---

## Integrations
- **Stripe**: Implement webhook verification.
- **Twilio**: Wrap in `IMessagingService` interface.
- **ABR**: Call via HttpClient with Polly retry policies.
- **Hangfire**: Idempotent jobs only. Store job state in DB.

---

## Branding & Multi-Tenancy
- Middleware resolves `Organization` from subdomain → theme injected into `ViewData`.
- Cache branding in MemoryCache, invalidate on updates.
- Fallback to default theme.

---

## AI & Analytics
- When generating AI features:  
  - Respect **privacy and compliance** (GDPR).  
  - Keep models external (Azure Cognitive, OpenAI).  
  - Do not train on sensitive debtor data.  
- Use **SignalR** for real-time dashboards.
- Chart.js/D3.js for visualizations.

---

## File Structure
- Follow this structure:
```
    src/
    DebtManager.Web
    DebtManager.Application
    DebtManager.Domain
    DebtManager.Infrastructure
    tests/
    DebtManager.Tests
    deploy/
    (IaC & CI/CD scripts)
    docs/
    (architecture, setup guides)
```
---

## Do/Don’t
**DO**
- Follow Clean Architecture separation.
- Add tests with each feature.
- Use dependency injection everywhere.

**DON’T**
- Hardcode secrets.
- Add client-side frameworks.
- Write direct SQL (use EF Core).
- Skip validation.

---

## Prompts for Copilot
Use these when requesting code from Copilot:

- "Generate a MediatR command and handler for creating a new Debtor with FluentValidation."  
- "Write EF Core entity configurations for PaymentPlan with required properties and relationships."  
- "Generate NUnit test cases for the BrandingResolver middleware."  
- "Implement Razor view with Tailwind for Admin dashboard table (sortable, paginated)."  
- "Add Hangfire recurring job for nightly payment reminders with retry policies."  

---

## Quality Gate
- Run: `dotnet test` and `npm run build`.
- All PRs require green tests.
- EF migrations must be included in commits.
- Document new config keys in `/docs/configuration.md`.

---