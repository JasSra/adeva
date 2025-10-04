# Getting Started

This guide will help you set up the Debt Management Platform for local development.

## Prerequisites

Before you begin, ensure you have the following installed:

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Node.js 20+** - [Download](https://nodejs.org/)
- **Docker Desktop** - [Download](https://www.docker.com/products/docker-desktop) (for SQL Server)
- **Git** - For version control

### Optional Tools

- **Azure CLI** - For Azure AD B2C management
- **Stripe CLI** - For local webhook testing
- **Twilio CLI** - For SMS testing

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/JasSra/adeva.git
cd adeva
```

### 2. Start SQL Server (Optional)

For local development with Docker:

```bash
docker compose -f deploy/docker-compose.yml up -d
```

Alternatively, use SQL Server LocalDB (default configuration).

### 3. Restore Dependencies

```bash
dotnet restore
dotnet build
```

### 4. Setup Tailwind CSS

```bash
cd src/DebtManager.Web
npm install
npm run build
cd ../..
```

### 5. Run the Application

```bash
dotnet run --project src/DebtManager.Web
```

The application will be available at:
- HTTP: http://localhost:5000
- HTTPS: https://localhost:5001

## Initial Setup

### First Admin User

1. Navigate to `/Backoffice` in your browser
2. You'll see a one-time admin signup link (only visible when no admins exist)
3. Click the link to create your first admin account
4. After the first admin is created, only existing admins can assign admin roles

### Admin Portal Access

Once you have an admin account:
- Access the admin portal at `/Admin`
- Navigate through comprehensive sections:
  - Applications
  - Debts
  - Debtors
  - Organizations
  - Transactions
  - Payments
  - Communications
  - Audit
  - Accounts
  - Configuration

## Configuration

### Azure AD B2C Authentication

The platform uses Azure AD B2C for authentication. Configure via environment variables or `appsettings.json`:

```json
{
  "AzureAdB2C": {
    "ClientId": "c83c5908-2b64-4304-8c53-b964ace5a1ea",
    "Instance": "https://jsraauth.b2clogin.com/",
    "Domain": "jsraauth.onmicrosoft.com",
    "Authority": "https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "AzureB2CScopes": {
    "TenantDomain": "jsraauth.onmicrosoft.com",
    "AppId": "c83c5908-2b64-4304-8c53-b964ace5a1ea",
    "Admin": "https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.Administrator",
    "Client": "https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.Client",
    "User": "https://jsraauth.onmicrosoft.com/c83c5908-2b64-4304-8c53-b964ace5a1ea/Consolidated.User"
  }
}
```

### ABR Validation Service

Configure Australian Business Register API integration:

```json
{
  "AbrApi": {
    "BaseUrl": "<your ABR API base url>",
    "ApiKey": "<your secret key>",
    "DefinitionUrl": "https://abr.business.gov.au/ApiDocumentation"
  }
}
```

**Note:** If `BaseUrl` is not set, the app uses a local stub that validates 11-digit ABNs.

### Database Connection

Default connection string (LocalDB):

```
Server=(localdb)\\MSSQLLocalDB;Database=DebtManager;Trusted_Connection=True;
```

Override in `appsettings.Development.json` or environment variables for Docker SQL Server.

### Secrets Management

For local development:
- Use .NET User Secrets for sensitive values
- Never commit API keys or connection strings

```bash
dotnet user-secrets init --project src/DebtManager.Web
dotnet user-secrets set "Stripe:SecretKey" "your-secret-key" --project src/DebtManager.Web
```

## Running Migrations

If you need to create or update database migrations:

```bash
# Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Add a new migration
dotnet ef migrations add MigrationName -p src/DebtManager.Infrastructure -s src/DebtManager.Web

# Update the database
dotnet ef database update -p src/DebtManager.Infrastructure -s src/DebtManager.Web
```

## Running Tests

Execute the test suite:

```bash
dotnet test
```

For specific test projects:

```bash
dotnet test tests/DebtManager.Tests
```

## Common Endpoints

- **Home:** `/`
- **Backoffice:** `/Backoffice`
- **Admin Portal:** `/Admin`
- **User Sign In:** `/Account/SignInUser`
- **Client Sign In:** `/Account/SignInClient`
- **Health Check (Live):** `/health/live`
- **Health Check (Ready):** `/health/ready`
- **Hangfire Dashboard:** `/hangfire`

## Troubleshooting

### Build Errors

If you encounter build errors:

```bash
dotnet clean
dotnet restore
dotnet build
```

### Tailwind CSS Not Updating

Rebuild Tailwind assets:

```bash
cd src/DebtManager.Web
npm run build
```

For watch mode during development:

```bash
npm run watch
```

### Database Connection Issues

- Ensure SQL Server is running
- Verify connection string in `appsettings.json`
- Check firewall settings for Docker

### Authentication Issues

- Verify Azure AD B2C configuration
- Check that redirect URIs are configured in Azure portal
- Ensure the application is registered correctly

## Next Steps

- Review the [Architecture](Architecture.md) documentation
- Explore [User Guides](User-Guides.md) for role-specific workflows
- Check the [Development Guide](Development-Guide.md) for coding standards

## Support

For issues or questions:
- Check the [FAQ](FAQ.md)
- Review existing documentation
- Contact the development team
