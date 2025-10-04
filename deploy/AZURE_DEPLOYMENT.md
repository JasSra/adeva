# Azure Deployment Guide

This document describes the Azure deployment infrastructure and workflows for the Debt Management Platform.

## Overview

The deployment infrastructure supports two configurations:
- **Free Tier**: Minimal cost setup for development and staging (Australian East region)
- **Beefy Tier**: Production-ready setup with enhanced performance and redundancy (Australian East region)

## Azure Resources

### Free Tier (Development/Staging)
- **App Service Plan**: Free (F1) tier
- **SQL Database**: Basic tier (5 DTUs, 2GB storage)
- **Application Insights**: Free tier with Log Analytics
- **Log Analytics Workspace**: Pay-as-you-go with 30-day retention
- **Region**: Australia East

### Beefy Tier (Production)
- **App Service Plan**: Premium V3 (P1V3) with 2 instances
- **SQL Database**: Standard S3 tier (100 DTUs, 250GB storage, zone-redundant)
- **Application Insights**: Connected to Log Analytics
- **Log Analytics Workspace**: Pay-as-you-go with 90-day retention
- **Optional**: Application Gateway or Front Door can be added for advanced routing
- **Region**: Australia East

## Prerequisites

### Azure Setup
1. Azure subscription with appropriate permissions
2. Service Principal with Contributor role on the subscription
3. Azure AD B2C tenant configured (already set up as per README.md)

### GitHub Secrets Required

Configure the following secrets in your GitHub repository settings:

#### Azure Authentication (Federated Identity)
- `AZURE_CLIENT_ID`: Service Principal Client ID
- `AZURE_TENANT_ID`: Azure AD Tenant ID
- `AZURE_SUBSCRIPTION_ID`: Azure Subscription ID

#### Resource Configuration
- `AZURE_RESOURCE_GROUP`: Resource group name (e.g., `rg-debtmanager-dev` or `rg-debtmanager-prod`)
- `AZURE_WEBAPP_NAME`: Web app name (output from infrastructure deployment)

#### Database Credentials
- `SQL_ADMIN_LOGIN`: SQL Server administrator username
- `SQL_ADMIN_PASSWORD`: SQL Server administrator password (strong password required)

#### Azure AD B2C Configuration
- `AZURE_AD_B2C_CLIENT_ID`: c83c5908-2b64-4304-8c53-b964ace5a1ea (from existing setup)
- `AZURE_AD_B2C_AUTHORITY`: https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0

### GitHub Environment Configuration

Create two environments in GitHub:
1. **dev**: For development/staging deployments
2. **prod**: For production deployments (with protection rules recommended)

## Deployment Workflows

### 1. Infrastructure Deployment

**Workflow**: `.github/workflows/deploy-infrastructure.yml`

Deploy or update Azure infrastructure:

```bash
# Manually trigger via GitHub UI:
# Actions → Deploy Infrastructure → Run workflow
# Select environment (dev/prod) and tier (free/beefy)
```

This workflow:
- Creates or updates the Azure resource group
- Deploys all Azure resources using Bicep templates
- Configures Application Insights, SQL Database, and App Service
- Sets up connection strings and app settings

### 2. Application Deployment

**Workflow**: `.github/workflows/deploy-app.yml`

Automatically deploys on push to `main` branch, or manually via workflow dispatch.

This workflow:
- Builds the .NET 8 application
- Runs unit tests
- Builds Tailwind CSS assets
- Publishes the application
- Deploys to Azure App Service
- Placeholder for EF Core migrations (needs configuration)

## Infrastructure as Code (Bicep)

### Main Template
**File**: `deploy/bicep/main.bicep`

This template provisions:
- Log Analytics Workspace
- Application Insights
- App Service Plan (Linux)
- Web App with system-assigned managed identity
- SQL Server
- SQL Database
- Firewall rules for Azure services

### Parameter Files
- `deploy/bicep/parameters.dev.json`: Development environment parameters
- `deploy/bicep/parameters.prod.json`: Production environment parameters

Note: Parameter files reference Azure Key Vault for secrets. Update the Key Vault IDs before use, or pass parameters directly in the workflow.

## Manual Deployment Steps

### First-Time Setup

1. **Create Service Principal**:
   ```bash
   az ad sp create-for-rbac --name "debtmanager-deploy" \
     --role contributor \
     --scopes /subscriptions/{subscription-id} \
     --sdk-auth
   ```

2. **Configure Federated Credentials** (for GitHub Actions):
   ```bash
   az ad app federated-credential create \
     --id {app-id} \
     --parameters credential.json
   ```

3. **Set GitHub Secrets**: Add all required secrets to your repository

4. **Deploy Infrastructure**:
   - Go to Actions → Deploy Infrastructure
   - Select environment and tier
   - Run workflow

5. **Get Web App Name**:
   ```bash
   az webapp list --resource-group {rg-name} --query "[].name" -o tsv
   ```
   Add this as `AZURE_WEBAPP_NAME` secret

6. **Deploy Application**:
   - Push to main branch, or
   - Manually trigger Deploy Application workflow

### Database Migrations

EF Core migrations need to be run after deployment. Options:

1. **Azure Cloud Shell**:
   ```bash
   # Set connection string
   export ConnectionStrings__Default="Server=tcp:..."
   
   # Run migrations
   dotnet ef database update --project src/DebtManager.Infrastructure
   ```

2. **Local with Azure SQL**:
   ```bash
   dotnet ef database update --project src/DebtManager.Infrastructure \
     --connection "Server=tcp:...; Database=...; ..."
   ```

3. **Automated in Pipeline**: Configure the migration step in `deploy-app.yml` with proper credentials

## Monitoring and Observability

### Application Insights
- Automatically configured with connection string
- Available at: Azure Portal → Application Insights → {app-name}
- Telemetry includes:
  - Request/response metrics
  - Dependency tracking
  - Exception logging
  - Custom events from application

### Log Analytics
- Centralized logging for all resources
- Query logs using KQL (Kusto Query Language)
- 30-day retention (free tier) or 90-day (beefy tier)

### Health Endpoints
- `/health/live`: Liveness probe
- `/health/ready`: Readiness probe

These are used by App Service health checks.

## Cost Optimization

### Free Tier (Approximate Monthly Cost)
- App Service F1: Free
- SQL Database Basic: ~$5-7 AUD
- Application Insights: Free (up to 5GB/month)
- Log Analytics: Free tier (500MB/day)
- **Total**: ~$5-10 AUD/month

### Beefy Tier (Approximate Monthly Cost)
- App Service P1V3 x 2: ~$300-400 AUD
- SQL Database S3: ~$150-200 AUD
- Application Insights: Usage-based (estimated $10-50 AUD)
- Log Analytics: Usage-based (estimated $10-50 AUD)
- **Total**: ~$470-700 AUD/month

Note: Costs are approximate and may vary based on usage and region.

## Security Considerations

1. **HTTPS Only**: All apps enforce HTTPS
2. **TLS 1.2**: Minimum TLS version enforced
3. **Managed Identity**: Web app uses system-assigned identity for Azure resource access
4. **SQL Firewall**: Only Azure services allowed by default (update for additional IPs)
5. **Secrets**: Store in Azure Key Vault or GitHub Secrets, never in code
6. **Authentication**: Azure AD B2C integration pre-configured

## Troubleshooting

### Deployment Fails
- Check GitHub Actions logs for specific errors
- Verify all secrets are correctly configured
- Ensure Service Principal has proper permissions
- Check Azure resource quotas and limits

### Application Not Starting
- Check Application Insights for exceptions
- Verify connection strings in App Service configuration
- Check App Service logs: Azure Portal → App Service → Log stream
- Verify .NET 8 runtime is available

### Database Connection Issues
- Verify SQL Server firewall rules
- Check connection string format
- Ensure SQL admin credentials are correct
- Test connectivity using Azure Cloud Shell

## Future Enhancements

- [ ] Add Application Gateway or Front Door for advanced routing and SSL termination
- [ ] Implement Azure Key Vault for secret management
- [ ] Add staging slots for zero-downtime deployments
- [ ] Configure auto-scaling rules for App Service
- [ ] Add Azure CDN for static assets
- [ ] Implement geo-replication for SQL Database (beefy tier)
- [ ] Add Azure Monitor alerts and dashboards
- [ ] Implement backup and disaster recovery procedures

## Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/azure/azure-sql/)
- [Application Insights Documentation](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [GitHub Actions for Azure](https://github.com/Azure/actions)
