# Debt Management Platform

Minimal scaffold per plan to bootstrap development.

## Deployment

This application is ready for deployment to Azure using Infrastructure as Code (Bicep templates) and GitHub Actions.

**Quick Deploy**: See [`deploy/QUICKSTART.md`](deploy/QUICKSTART.md) for step-by-step deployment instructions.

**Detailed Guide**: See [`deploy/AZURE_DEPLOYMENT.md`](deploy/AZURE_DEPLOYMENT.md) for comprehensive documentation.

- **Free Tier**: ~$5-10 AUD/month (dev/staging)
- **Beefy Tier**: ~$470-700 AUD/month (production)
- **Region**: Australia East

## Solution layout (small projects, modular)

- src/DebtManager.Contracts — interfaces/DTOs shared across services (e.g., IAbrValidator)
- src/DebtManager.Domain — domain entities and value objects
- src/DebtManager.Application — application layer (CQRS-ready)
- src/DebtManager.Infrastructure — implementations (EF, external adapters)
- src/DebtManager.Web — MVC web front-end (areas for User/Client/Admin)
- src/Services.AbrValidation — standalone ABR validation microservice (optional)
- tests/DebtManager.Tests — unit/integration tests

Contracts decouple interfaces from implementations so we can split into independent deployable services without monolith coupling. For example, ABR validation can be hosted separately and consumed via HTTP, while the web app uses a stub locally.

## Prereqs

- .NET 8 SDK
- Node.js 20+
- Docker Desktop (for SQL Server)

## Quickstart

1. Start SQL Server (optional for now):
   - docker compose -f deploy/docker-compose.yml up -d
2. Restore/build:
   - dotnet restore
   - dotnet build
3. Tailwind CSS:
   - cd src/DebtManager.Web && npm install && npm run build
4. Run web:
   - dotnet run --project src/DebtManager.Web

Open <http://localhost:5000> or <https://localhost:5001>.

### Admin Setup

**First Admin Creation:**
- Visit `/Backoffice` to see the one-time admin signup link
- Link is only visible when NO admin users exist
- After first admin is created, only existing admins can assign admin roles

**Admin Portal:**
- Access at `/Admin` with comprehensive sidebar navigation
- Sections: Applications, Debts, Debtors, Organizations, Transactions, Payments, Communications, Audit, Accounts, Configuration
- All sections include search, pagination, and filtering

### Auth configuration (Azure AD B2C)

Configure via environment variables (override `appsettings.json` as needed):

- AzureAdB2C__ClientId=c83c5908-2b64-4304-8c53-b964ace5a1ea
- AzureAdB2C__Authority=<https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0>
- AzureAdB2C__KnownAuthorities__0=jsraauth.b2clogin.com
- AzureAdB2C__Instance=<https://jsraauth.b2clogin.com/>
- AzureAdB2C__Domain=jsraauth.onmicrosoft.com
- AzureB2CScopes__TenantDomain=jsraauth.onmicrosoft.com
- AzureB2CScopes__AppId=c83c5908-2b64-4304-8c53-b964ace5a1ea
- AzureB2CScopes__Admin=<https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.Administrator>
- AzureB2CScopes__Client=<https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.Client>
- AzureB2CScopes__User=<https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.User>

Sign in: /Account/SignIn  •  Sign out: /Account/SignOutUser

### ABR Service configuration

- AbrApi__BaseUrl=\<your ABR API base url\>
- AbrApi__ApiKey=\<your secret key\>
- AbrApi__DefinitionUrl=<https://abr.business.gov.au/ApiDocumentation>

If BaseUrl is not set, the app uses a local stub that validates 11-digit ABNs.

## Tests

Run unit tests:

- dotnet test

