# Deployment Guide

This guide covers deployment strategies, infrastructure setup, and operational procedures for the Debt Management Platform.

## Table of Contents

- [Deployment Overview](#deployment-overview)
- [Azure App Service Deployment](#azure-app-service-deployment)
- [Database Setup](#database-setup)
- [Configuration Management](#configuration-management)
- [Continuous Integration/Deployment](#continuous-integrationdeployment)
- [Monitoring & Observability](#monitoring--observability)
- [Backup & Recovery](#backup--recovery)
- [Scaling Strategies](#scaling-strategies)

---

## Deployment Overview

### Environments

1. **Development** - Local developer machines
2. **Staging** - Pre-production testing environment
3. **Production** - Live customer-facing environment

### Target Infrastructure

**Recommended Azure Services:**
- **Azure App Service** - Web application hosting (Premium tier)
- **Azure SQL Managed Instance** - Production database
- **Azure Storage** - Blob storage for files and assets
- **Azure Key Vault** - Secrets and certificate management
- **Application Insights** - Monitoring and telemetry
- **Azure CDN** - Content delivery network

### Architecture Diagram

```
┌─────────────────────┐
│   Azure Front Door  │ (Optional: Global distribution)
└──────────┬──────────┘
           │
┌──────────▼──────────┐
│   Azure CDN         │ (Static assets)
└──────────┬──────────┘
           │
┌──────────▼──────────────────┐
│   Azure App Service         │
│   (Premium P1V2 or higher)  │
└──────────┬──────────────────┘
           │
    ┌──────┴───────┬────────────────┐
    │              │                │
┌───▼────┐   ┌────▼────┐   ┌──────▼──────┐
│Azure   │   │ Azure   │   │Application  │
│SQL     │   │ Storage │   │  Insights   │
│Managed │   │ Account │   └─────────────┘
│Instance│   └─────────┘
└────────┘
```

---

## Azure App Service Deployment

### Prerequisites

- Azure subscription
- Azure CLI installed
- .NET 8 SDK
- Access to Azure portal

### Initial Setup

**1. Create Resource Group:**

```bash
az login
az group create \
  --name rg-debtmanager-prod \
  --location australiaeast
```

**2. Create App Service Plan:**

```bash
az appservice plan create \
  --name asp-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --sku P1V2 \
  --is-linux
```

**3. Create Web App:**

```bash
az webapp create \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --plan asp-debtmanager-prod \
  --runtime "DOTNETCORE:8.0"
```

**4. Configure App Settings:**

```bash
az webapp config appsettings set \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    WEBSITE_RUN_FROM_PACKAGE=1
```

### Deployment Methods

#### Method 1: Azure CLI

```bash
# Build and publish
dotnet publish src/DebtManager.Web \
  -c Release \
  -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --src deploy.zip
```

#### Method 2: GitHub Actions

See [CI/CD section](#continuous-integrationdeployment) for automated deployment.

#### Method 3: Visual Studio Publish

1. Right-click `DebtManager.Web` project
2. Select **Publish**
3. Choose **Azure** → **Azure App Service (Linux)**
4. Sign in and select subscription
5. Select or create App Service
6. Click **Publish**

### Configuration

**Enable Always On:**

```bash
az webapp config set \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --always-on true
```

**Configure Health Check:**

```bash
az webapp config set \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --health-check-path "/health/ready"
```

**Set Connection Strings:**

```bash
az webapp config connection-string set \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --connection-string-type SQLAzure \
  --settings Default="Server=tcp:...;Database=...;User ID=...;Password=...;Encrypt=True;"
```

---

## Database Setup

### Azure SQL Managed Instance

**1. Create SQL Managed Instance:**

```bash
az sql mi create \
  --name sqlmi-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --location australiaeast \
  --admin-user sqladmin \
  --admin-password 'YourSecurePassword!' \
  --subnet /subscriptions/.../subnets/default \
  --capacity 4 \
  --storage 32GB \
  --edition GeneralPurpose \
  --family Gen5
```

**2. Create Database:**

```bash
az sql midb create \
  --managed-instance sqlmi-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --name DebtManager
```

**3. Configure Firewall:**

```bash
# Allow Azure services
az sql mi update \
  --name sqlmi-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --public-data-endpoint-enabled true
```

### Database Migrations

**Apply migrations on deployment:**

Option 1 - **Startup Migration** (not recommended for production):

```csharp
// Program.cs
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}
```

Option 2 - **Pre-deployment Script** (recommended):

```bash
# In CI/CD pipeline before deployment
dotnet ef database update \
  --project src/DebtManager.Infrastructure \
  --startup-project src/DebtManager.Web \
  --connection "Server=...;Database=...;User ID=...;Password=...;"
```

### Backup Configuration

**Automated Backups:**

```bash
az sql midb ltr-policy set \
  --resource-group rg-debtmanager-prod \
  --managed-instance sqlmi-debtmanager-prod \
  --database DebtManager \
  --weekly-retention P4W \
  --monthly-retention P12M \
  --yearly-retention P5Y
```

---

## Configuration Management

### Azure Key Vault

**1. Create Key Vault:**

```bash
az keyvault create \
  --name kv-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --location australiaeast
```

**2. Add Secrets:**

```bash
az keyvault secret set \
  --vault-name kv-debtmanager-prod \
  --name "Stripe--SecretKey" \
  --value "sk_live_..."

az keyvault secret set \
  --vault-name kv-debtmanager-prod \
  --name "Twilio--AuthToken" \
  --value "your_auth_token"
```

**3. Grant App Service Access:**

```bash
# Enable managed identity on App Service
az webapp identity assign \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod

# Get identity principal ID
PRINCIPAL_ID=$(az webapp identity show \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --query principalId -o tsv)

# Grant Key Vault access
az keyvault set-policy \
  --name kv-debtmanager-prod \
  --object-id $PRINCIPAL_ID \
  --secret-permissions get list
```

**4. Configure App to Use Key Vault:**

Add to `Program.cs`:

```csharp
if (builder.Environment.IsProduction())
{
    var keyVaultUri = new Uri(builder.Configuration["KeyVault:Uri"]);
    builder.Configuration.AddAzureKeyVault(
        keyVaultUri,
        new DefaultAzureCredential()
    );
}
```

### App Settings

**Required Production Settings:**

```bash
az webapp config appsettings set \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --settings \
    ASPNETCORE_ENVIRONMENT=Production \
    KeyVault__Uri=https://kv-debtmanager-prod.vault.azure.net/ \
    AzureAdB2C__ClientId=... \
    AzureAdB2C__Authority=... \
    ApplicationInsights__ConnectionString=...
```

---

## Continuous Integration/Deployment

### GitHub Actions Workflow

Create `.github/workflows/deploy-production.yml`:

```yaml
name: Deploy to Production

on:
  push:
    branches:
      - main

env:
  AZURE_WEBAPP_NAME: app-debtmanager-prod
  DOTNET_VERSION: '8.0.x'

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: ${{ env.DOTNET_VERSION }}
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish src/DebtManager.Web -c Release -o ./publish
    
    - name: Deploy to Azure Web App
      uses: azure/webapps-deploy@v2
      with:
        app-name: ${{ env.AZURE_WEBAPP_NAME }}
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### Setup GitHub Secrets

1. Download publish profile from Azure portal
2. Go to GitHub repository → Settings → Secrets
3. Add secret: `AZURE_WEBAPP_PUBLISH_PROFILE` with content of publish profile

### Pre-deployment Checks

```yaml
    - name: Run Migrations
      run: |
        dotnet tool install --global dotnet-ef
        dotnet ef database update \
          --project src/DebtManager.Infrastructure \
          --startup-project src/DebtManager.Web \
          --connection "${{ secrets.DB_CONNECTION_STRING }}"
    
    - name: Health Check
      run: |
        curl -f https://app-debtmanager-prod.azurewebsites.net/health/live || exit 1
```

---

## Monitoring & Observability

### Application Insights

**1. Create Application Insights:**

```bash
az monitor app-insights component create \
  --app ai-debtmanager-prod \
  --location australiaeast \
  --resource-group rg-debtmanager-prod \
  --application-type web
```

**2. Get Connection String:**

```bash
az monitor app-insights component show \
  --app ai-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --query connectionString -o tsv
```

**3. Configure in Application:**

```json
{
  "ApplicationInsights": {
    "ConnectionString": "InstrumentationKey=...;IngestionEndpoint=..."
  }
}
```

### Alerts

**Create availability test:**

```bash
az monitor app-insights web-test create \
  --resource-group rg-debtmanager-prod \
  --name "Homepage Availability" \
  --location australiaeast \
  --kind ping \
  --web-test-name "homepage-check" \
  --enabled true \
  --frequency 300 \
  --timeout 30 \
  --locations "Australia East" "Australia Southeast" \
  --web-test '{"request":{"url":"https://app-debtmanager-prod.azurewebsites.net/"}}'
```

**Create alert rule:**

```bash
az monitor metrics alert create \
  --name "High Response Time" \
  --resource-group rg-debtmanager-prod \
  --scopes /subscriptions/.../app-debtmanager-prod \
  --condition "avg requests/duration > 2000" \
  --description "Alert when response time exceeds 2 seconds"
```

### Logging

**View logs:**

```bash
# Stream logs
az webapp log tail \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod

# Download logs
az webapp log download \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --log-file logs.zip
```

### Metrics to Monitor

- **Request rate** - Requests per second
- **Response time** - Average response duration
- **Error rate** - Failed requests percentage
- **CPU usage** - App Service CPU percentage
- **Memory usage** - App Service memory consumption
- **Database DTU** - Database resource utilization
- **Exceptions** - Unhandled exception count

---

## Backup & Recovery

### Database Backups

**Automated backups** (enabled by default):
- Point-in-time restore for last 7-35 days
- Long-term retention configured separately

**Manual backup:**

```bash
az sql midb create \
  --managed-instance sqlmi-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --name DebtManager-Backup-$(date +%Y%m%d) \
  --source-database DebtManager
```

**Restore from backup:**

```bash
az sql midb restore \
  --resource-group rg-debtmanager-prod \
  --managed-instance sqlmi-debtmanager-prod \
  --name DebtManager-Restored \
  --dest-name DebtManager-Restored \
  --time "2024-01-01T00:00:00Z"
```

### Application Backups

**Blob Storage:**

```bash
az storage account create \
  --name stdebtmanagerprod \
  --resource-group rg-debtmanager-prod \
  --location australiaeast \
  --sku Standard_LRS

az webapp config backup create \
  --resource-group rg-debtmanager-prod \
  --webapp-name app-debtmanager-prod \
  --container-url "https://stdebtmanagerprod.blob.core.windows.net/backups" \
  --backup-name backup-$(date +%Y%m%d)
```

### Disaster Recovery Plan

1. **Backup verification** - Weekly restore tests
2. **RTO (Recovery Time Objective)** - 4 hours
3. **RPO (Recovery Point Objective)** - 1 hour
4. **Geo-redundancy** - Multi-region setup for critical systems

---

## Scaling Strategies

### Vertical Scaling

**Scale up App Service:**

```bash
az appservice plan update \
  --name asp-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --sku P2V2
```

### Horizontal Scaling

**Auto-scale configuration:**

```bash
az monitor autoscale create \
  --resource-group rg-debtmanager-prod \
  --resource app-debtmanager-prod \
  --resource-type Microsoft.Web/serverfarms \
  --name autoscale-debtmanager \
  --min-count 2 \
  --max-count 10 \
  --count 2

az monitor autoscale rule create \
  --resource-group rg-debtmanager-prod \
  --autoscale-name autoscale-debtmanager \
  --condition "Percentage CPU > 70 avg 5m" \
  --scale out 2
```

### Database Scaling

**Scale database tier:**

```bash
az sql midb update \
  --managed-instance sqlmi-debtmanager-prod \
  --resource-group rg-debtmanager-prod \
  --name DebtManager \
  --capacity 8
```

**Read replicas:**

Consider Azure SQL Database with read replicas for reporting queries.

---

## Security Considerations

### SSL/TLS

- HTTPS enforced on App Service
- TLS 1.2 minimum version
- HSTS headers enabled

### Network Security

- VNet integration for App Service
- Private endpoints for SQL and Storage
- NSG rules restricting access

### Compliance

- **GDPR** - Data retention policies
- **PCI DSS** - Payment data security (Stripe handles)
- **SOC 2** - Audit logging and monitoring

---

## Troubleshooting

### Common Issues

**App won't start:**

```bash
# Check logs
az webapp log tail \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod

# Restart app
az webapp restart \
  --name app-debtmanager-prod \
  --resource-group rg-debtmanager-prod
```

**Database connection errors:**

- Verify firewall rules
- Check connection string
- Ensure managed identity has permissions

**High latency:**

- Check Application Insights for slow queries
- Review database DTU usage
- Consider caching strategy

---

**See Also:**
- [Getting Started](Getting-Started.md) - Local development setup
- [Architecture](Architecture.md) - System architecture
- [API Reference](API-Reference.md) - Integration details
