# Debt Management Platform Wiki

Welcome to the **Debt Management Platform** wiki! This is a comprehensive white-labelled debt management system built on .NET 8.

## Quick Links

- [Getting Started](Getting-Started.md) - Setup and installation guide
- [Architecture](Architecture.md) - Technical architecture and design
- [User Guides](User-Guides.md) - Role-based workflow documentation
- [Development Guide](Development-Guide.md) - Development workflows and best practices
- [API Reference](API-Reference.md) - Integration and API documentation
- [Deployment](Deployment.md) - Deployment and operations guide
- [FAQ](FAQ.md) - Frequently asked questions

## Project Overview

The Debt Management Platform is a **white-labelled, multi-tenant MVC web application** designed to streamline debt management for businesses and individuals. It supports three primary roles:

- **Debtor (User)** - End users managing their debts
- **Client (Creditor)** - Business clients managing their debtors
- **Admin** - System administrators managing the platform

### Key Features

- **Multi-tenant Branding** - Subdomain/custom domain-based white-labeling
- **Payment Plans** - Flexible payment options with automated scheduling
- **Secure Authentication** - Azure AD B2C integration with MFA support
- **Payment Processing** - Stripe integration for secure transactions
- **Automated Communications** - Email and SMS notifications via Twilio
- **Background Jobs** - Hangfire-powered nightly processing
- **ABR Validation** - Australian Business Register integration
- **Comprehensive Admin Portal** - Full system management and monitoring

### Technology Stack

- **.NET 8** - Core framework
- **ASP.NET Core MVC** - Web presentation layer
- **Entity Framework Core** - Data access
- **SQL Server** - Primary database
- **Azure AD B2C** - Authentication (MSAL)
- **Stripe** - Payment processing
- **Twilio** - SMS communications
- **Hangfire** - Background job processing
- **Tailwind CSS** - UI styling
- **Serilog** - Structured logging

### Architecture Principles

- **Clean Architecture** - Separation of concerns with clear layer boundaries
- **CQRS Ready** - Command Query Responsibility Segregation pattern
- **Modular Design** - Independent, deployable services
- **Security First** - Encrypted sensitive data, MFA, comprehensive auditing
- **Testability** - Unit, integration, and end-to-end test support

## Project Structure

```
adeva/
├── src/
│   ├── DebtManager.Contracts/      # Shared interfaces and DTOs
│   ├── DebtManager.Domain/         # Domain entities and value objects
│   ├── DebtManager.Application/    # Application layer (CQRS)
│   ├── DebtManager.Infrastructure/ # EF Core, external adapters
│   ├── DebtManager.Web/           # MVC web application
│   └── Services.AbrValidation/    # ABR validation service
├── tests/
│   └── DebtManager.Tests/         # Unit and integration tests
├── deploy/                        # Deployment scripts and configs
└── wiki/                          # This documentation
```

## Getting Help

- Check the [FAQ](FAQ.md) for common questions
- Review the [User Guides](User-Guides.md) for workflow-specific help
- Consult the [Development Guide](Development-Guide.md) for technical issues

## Contributing

This project follows a feature-branch workflow:
1. Create a feature branch from `main`
2. Make your changes
3. Submit a pull request
4. Automated builds and tests run
5. Merge after review

See the [Development Guide](Development-Guide.md) for detailed guidelines.

---

**Version:** 1.0  
**Last Updated:** 2024  
**License:** All rights reserved
