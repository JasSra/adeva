# Configuration & Secrets Strategy

- Local development: use `appsettings.json` and .NET User Secrets for sensitive values.
- Staging/Prod: use Azure App Configuration and Azure Key Vault (planned). Environment variables override defaults.
- Database connection strings and API keys are not committed.

Next steps:

- Wire Microsoft.Extensions.Configuration.AzureAppConfiguration and KeyVault providers.
- Create Admin UI for editing DB-backed config entries.
