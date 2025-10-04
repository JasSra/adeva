# Architecture

This document describes the technical architecture of the Debt Management Platform.

## Overview

The platform follows **Clean Architecture** principles with clear separation of concerns across multiple layers and projects.

## Architectural Principles

- **Domain-Driven Design (DDD)** - Rich domain models with business logic
- **CQRS Pattern** - Command Query Responsibility Segregation
- **Dependency Inversion** - Abstractions defined in contracts, implementations in infrastructure
- **Single Responsibility** - Each project has a focused purpose
- **Testability** - All layers are independently testable

## Project Structure

### Layer Diagram

```
┌─────────────────────────────────────────┐
│      DebtManager.Web (Presentation)     │
│         ASP.NET Core MVC + Areas        │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│   DebtManager.Application (Use Cases)   │
│     MediatR, FluentValidation, DTOs    │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│     DebtManager.Domain (Core Logic)     │
│      Entities, Value Objects, Rules     │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│  DebtManager.Infrastructure (Data/IO)   │
│   EF Core, External APIs, Repositories  │
└─────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────┐
│   DebtManager.Contracts (Interfaces)    │
│        Shared DTOs and Contracts        │
└─────────────────────────────────────────┘
```

### Project Responsibilities

#### 1. DebtManager.Web

**Responsibility:** Presentation layer and user interface

- ASP.NET Core MVC application
- Three areas: `/Areas/User`, `/Areas/Client`, `/Areas/Admin`
- Razor views with Tailwind CSS
- Middleware pipeline (authentication, branding, etc.)
- API controllers for external integrations

**Key Components:**
- Controllers
- Views (Razor)
- Middleware (BrandingResolverMiddleware)
- Services (AdminService)
- ViewModels

#### 2. DebtManager.Application

**Responsibility:** Application logic and use cases

- MediatR command/query handlers
- FluentValidation validators
- AutoMapper profiles
- DTOs for data transfer
- Business workflows

**Key Patterns:**
- Command handlers
- Query handlers
- Validators
- Mapping configurations

#### 3. DebtManager.Domain

**Responsibility:** Core business logic and rules

- Domain entities
- Value objects
- Domain events
- Business rules and invariants
- No external dependencies

**Key Entities:**
- `Organization` - Client organizations
- `Debtor` - End users with debts
- `Debt` - Individual debt records
- `PaymentPlan` - Payment schedules
- `Transaction` - Payment transactions
- `PaymentInstallment` - Individual payments
- `AdminUser` - System administrators

#### 4. DebtManager.Infrastructure

**Responsibility:** External concerns and data access

- Entity Framework Core DbContext
- Repository implementations
- External API adapters
- Email/SMS services
- File storage

**Key Components:**
- `AppDbContext` - EF Core context
- Repositories
- Migrations
- External service clients

#### 5. DebtManager.Contracts

**Responsibility:** Shared contracts and interfaces

- Repository interfaces
- Service interfaces
- DTOs for cross-layer communication
- Enables loose coupling

#### 6. Services.AbrValidation

**Responsibility:** ABR (Australian Business Register) validation

- Standalone microservice (optional deployment)
- Can run independently or embedded
- Validates ABN (Australian Business Number)

## Core Services

### Authentication & Authorization

**Provider:** Azure AD B2C (MSAL)

- **User Flows:** Sign-up, sign-in, password reset
- **Scopes:** User, Client, Admin
- **Token Management:** In-memory token cache
- **MFA:** Multi-factor authentication support

**Authorization Policies:**
- `RequireUserScope` - For debtor access
- `RequireClientScope` - For creditor access
- `RequireAdminScope` - For admin access

### Multi-Tenant Branding

**BrandingResolverMiddleware**

Resolves tenant-specific branding based on:
1. Subdomain (e.g., `client1.adeva.local`)
2. Custom domain mapping
3. Fallback to default branding

**Flow:**
```
Request → Extract Host → Resolve Tenant → Load Theme → Inject into HttpContext
```

**Theme Properties:**
- Organization name
- Primary color (hex)
- Logo URL
- Custom CSS (future)

### Background Jobs

**Provider:** Hangfire

**Scheduled Jobs:**
- Nightly payment reminders
- Payment reconciliation
- Remittance sheet generation
- Invoice extraction orchestration
- Failed payment retries

**Features:**
- Dashboard at `/hangfire`
- Job persistence (in-memory for dev, SQL for prod)
- Automatic retries
- Dead-letter queue for failed jobs

### Payment Processing

**Provider:** Stripe

**Capabilities:**
- Hosted checkout
- Webhook handling
- Payment method management
- Subscription support
- Refunds and disputes

### Communications

**Providers:**
- Email: SMTP/SendGrid
- SMS: Twilio

**Features:**
- Template-based messaging
- Automated reminders
- OTP (One-Time Password) delivery
- Audit trail

### Data Access

**Provider:** Entity Framework Core

**Patterns:**
- Repository pattern
- Unit of Work
- Optimistic concurrency
- Soft deletes
- Audit fields (CreatedAt, UpdatedAt)

**Features:**
- Migrations for schema changes
- Query optimization
- Connection pooling
- Lazy loading disabled

## Security Architecture

### Encryption

**Data at Rest:**
- Sensitive fields marked with `[SensitiveData]` attribute
- AES encryption for sensitive database fields
- TDE (Transparent Data Encryption) for production SQL Server

**Data in Transit:**
- HTTPS enforced
- TLS 1.2+ required
- HSTS headers

### Authentication Flow

```
User → Azure AD B2C → Token → Application → Validate → HttpContext.User
```

### Authorization

- Claims-based authorization
- Scope validation per role
- Resource-based authorization for multi-tenancy

### Auditing

- Centralized logging via Serilog
- Audit trail for critical operations
- User action tracking
- Security event monitoring

## Caching Strategy

### Memory Cache

- Branding metadata
- Configuration values
- ABR validation results (short TTL)

**Invalidation:**
- Time-based expiration
- Manual invalidation on updates
- Cache dependencies

### Session State

- Minimal session usage
- TempData for flash messages
- Cookie-based authentication

## Health Checks

**Endpoints:**
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe

**Checks:**
- Database connectivity
- External API availability
- File storage access
- Background job status

## Testing Strategy

### Unit Tests

- Domain logic
- Command/query handlers
- Validators
- Pure business rules

**Framework:** NUnit + Moq

### Integration Tests

- API endpoints
- Database operations
- Repository implementations

**Framework:** WebApplicationFactory

### Worker Tests

- Background job logic
- Scheduled task execution
- Idempotency verification

### End-to-End Tests

- Critical user flows
- Multi-step processes
- UI smoke tests

**Framework:** Playwright

## Deployment Architecture

### Development

- LocalDB or Docker SQL Server
- In-memory Hangfire
- Local file storage
- Stub external services

### Staging

- Azure App Service
- Azure SQL Database
- Azure Storage (Blob)
- Real external services (test mode)

### Production

- Azure App Service (Premium tier)
- Azure SQL Managed Instance
- Azure Storage with CDN
- Application Insights monitoring
- Azure Key Vault for secrets

## Scalability Considerations

### Horizontal Scaling

- Stateless web application
- Sticky sessions not required
- Distributed caching ready

### Database Optimization

- Indexed queries
- Read replicas for reporting
- Connection pooling
- Query result caching

### Background Jobs

- Distributed job execution
- Queue-based processing
- Idempotent operations

## Performance Monitoring

**Tools:**
- Application Insights
- Serilog structured logging
- Hangfire dashboard
- Health check endpoints

**Metrics:**
- Request duration
- Database query performance
- External API latency
- Background job execution time

## Future Enhancements

### Planned

- OpenTelemetry integration
- Feature flags (LaunchDarkly or similar)
- Power BI embedded analytics
- Advanced AI/ML features

### Under Consideration

- SignalR for real-time updates
- GraphQL API
- Mobile app support
- Multi-region deployment

---

**See Also:**
- [Development Guide](Development-Guide.md) - Coding standards and practices
- [Deployment](Deployment.md) - Deployment procedures
- [API Reference](API-Reference.md) - Integration details
