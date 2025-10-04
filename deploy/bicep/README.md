# Bicep Infrastructure Templates

This directory contains Infrastructure as Code (IaC) templates using Azure Bicep for deploying the Debt Management Platform.

## Files

### Core Templates

- **main.bicep**: Main template that deploys all core Azure resources
  - App Service Plan (Linux)
  - Web App (.NET 8)
  - SQL Server and Database
  - Application Insights
  - Log Analytics Workspace
  
- **appgateway.bicep**: Optional Application Gateway template for advanced scenarios
  - Virtual Network with subnets
  - Public IP address
  - Application Gateway with WAF v2
  - Health probes and routing rules

### Parameter Files

- **parameters.dev.json**: Development environment parameters (free tier)
- **parameters.prod.json**: Production environment parameters (beefy tier)

## Usage

### Deploy via GitHub Actions

The recommended deployment method is using the GitHub Actions workflow:

```
Actions → Deploy Infrastructure → Run workflow
```

Select:
- Environment: `dev` or `prod`
- Tier: `free` or `beefy`

### Deploy via Azure CLI

For manual deployment:

```bash
# Login to Azure
az login

# Create resource group
az group create --name rg-debtmanager-dev --location australiaeast

# Deploy infrastructure
az deployment group create \
  --resource-group rg-debtmanager-dev \
  --template-file main.bicep \
  --parameters \
    environmentName=dev \
    tierType=free \
    sqlAdminLogin=sqladmin \
    sqlAdminPassword='YourStrongPassword123!' \
    azureAdB2CClientId='c83c5908-2b64-4304-8c53-b964ace5a1ea' \
    azureAdB2CAuthority='https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0'
```

### Deploy Application Gateway (Optional)

For the beefy tier, you can optionally deploy Application Gateway:

```bash
# First get the web app hostname from the main deployment
WEBAPP_HOSTNAME=$(az webapp show \
  --resource-group rg-debtmanager-prod \
  --name debtmanager-web-prod-xxxxx \
  --query defaultHostName -o tsv)

# Deploy Application Gateway
az deployment group create \
  --resource-group rg-debtmanager-prod \
  --template-file appgateway.bicep \
  --parameters \
    environmentName=prod \
    webAppHostname=$WEBAPP_HOSTNAME
```

## Tier Configurations

### Free Tier
- **App Service**: F1 (Free tier, 1GB RAM, 60 min/day)
- **SQL Database**: Basic (5 DTUs, 2GB max size)
- **Log Retention**: 30 days
- **Estimated Cost**: ~$5-10 AUD/month

### Beefy Tier
- **App Service**: P1V3 (Premium, 8GB RAM, 2 instances)
- **SQL Database**: S3 Standard (100 DTUs, 250GB, zone-redundant)
- **Log Retention**: 90 days
- **Estimated Cost**: ~$470-700 AUD/month

## Resource Naming Convention

Resources are named using the pattern:
```
{appNamePrefix}-{resourceType}-{environment}-{uniqueSuffix}
```

Examples:
- `debtmanager-web-dev-abc123` (Web App)
- `debtmanager-sql-prod-xyz789` (SQL Server)
- `debtmanager-insights-dev-abc123` (Application Insights)

The unique suffix ensures globally unique names for resources that require them.

## Security Features

1. **HTTPS Only**: All web apps enforce HTTPS
2. **TLS 1.2+**: Minimum TLS version enforced
3. **Managed Identity**: System-assigned managed identity for secure resource access
4. **SQL Firewall**: Restricts access to Azure services only
5. **Connection Strings**: Stored in App Service configuration, not in code
6. **Secrets**: Passed as secure parameters, never committed to source control

## Customization

### Modify Resource SKUs

Edit the SKU maps in `main.bicep`:

```bicep
var appServiceSkuMap = {
  free: {
    name: 'F1'  // Change to 'B1' for Basic tier
    tier: 'Free'
    capacity: 1
  }
  beefy: {
    name: 'P1V3'  // Change to 'P2V3' for more power
    tier: 'PremiumV3'
    capacity: 2  // Increase for more instances
  }
}
```

### Add Additional Resources

Create new Bicep modules or add resources to `main.bicep`:

```bicep
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}
```

## Validation

Validate templates before deployment:

```bash
az deployment group validate \
  --resource-group rg-debtmanager-dev \
  --template-file main.bicep \
  --parameters @parameters.dev.json
```

## What-If Analysis

Preview changes before deployment:

```bash
az deployment group what-if \
  --resource-group rg-debtmanager-dev \
  --template-file main.bicep \
  --parameters @parameters.dev.json
```

## Outputs

After deployment, the following outputs are available:

- `webAppName`: Name of the deployed web app
- `webAppUrl`: URL of the web app
- `sqlServerFqdn`: Fully qualified domain name of SQL Server
- `appInsightsInstrumentationKey`: Application Insights instrumentation key
- `appInsightsConnectionString`: Application Insights connection string

Access outputs:

```bash
az deployment group show \
  --resource-group rg-debtmanager-dev \
  --name main \
  --query properties.outputs
```

## Troubleshooting

### Deployment Fails

1. Check the error message in Azure Portal or CLI output
2. Verify resource name uniqueness (SQL Server, Web App)
3. Check Azure subscription quotas and limits
4. Ensure you have proper permissions (Contributor role)

### Parameter Issues

If using parameter files with Key Vault references, ensure:
- Key Vault exists and is accessible
- Secrets exist in the vault
- You have proper permissions to read secrets

### SQL Connectivity

If SQL Server is unreachable:
- Check firewall rules in Azure Portal
- Add your IP address to the firewall rules
- Verify the connection string is correct

## Best Practices

1. **Use Parameter Files**: Store environment-specific values in parameter files
2. **Secure Secrets**: Use Azure Key Vault or GitHub Secrets for sensitive data
3. **Version Control**: Track all Bicep files in Git
4. **Test in Dev**: Always deploy to dev environment first
5. **Tag Resources**: Add tags for cost tracking and organization
6. **Monitor Costs**: Set up Azure Cost Management alerts
7. **Use Managed Identities**: Avoid storing credentials in application code
8. **Enable Diagnostics**: Configure diagnostic settings for all resources

## Additional Resources

- [Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)
- [Azure App Service Best Practices](https://docs.microsoft.com/azure/app-service/app-service-best-practices)
- [Azure SQL Database Best Practices](https://docs.microsoft.com/azure/azure-sql/database/best-practices)
- [Application Gateway Documentation](https://docs.microsoft.com/azure/application-gateway/)
