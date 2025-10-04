# Azure Deployment Quick Reference

## Quick Start

### 1. Initial Setup (One-time)

Set up GitHub secrets in your repository settings:

```
Settings → Secrets and variables → Actions → New repository secret
```

**Required Secrets:**
- `AZURE_CLIENT_ID` - Your service principal client ID
- `AZURE_TENANT_ID` - Your Azure AD tenant ID  
- `AZURE_SUBSCRIPTION_ID` - Your Azure subscription ID
- `AZURE_RESOURCE_GROUP` - Resource group name (e.g., `rg-debtmanager-dev`)
- `SQL_ADMIN_LOGIN` - SQL Server admin username
- `SQL_ADMIN_PASSWORD` - SQL Server admin password (strong password)
- `AZURE_AD_B2C_CLIENT_ID` - `c83c5908-2b64-4304-8c53-b964ace5a1ea`
- `AZURE_AD_B2C_AUTHORITY` - `https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0`

### 2. Deploy Infrastructure (First Time)

Go to GitHub Actions:
```
Actions → Deploy Infrastructure → Run workflow
```

Select:
- Environment: `dev` or `prod`
- Tier: `free` (for dev) or `beefy` (for prod)
- Click "Run workflow"

Wait for completion (~5-10 minutes)

### 3. Get Web App Name

After infrastructure deployment completes:

```bash
az login
az webapp list --resource-group rg-debtmanager-dev --query "[].name" -o tsv
```

Add the result as `AZURE_WEBAPP_NAME` secret in GitHub.

### 4. Deploy Application

**Automatic:** Push to `main` branch

**Manual:**
```
Actions → Deploy Application → Run workflow
```

Select environment and click "Run workflow"

### 5. Run Database Migrations

After first deployment:

```bash
# Get connection string from Azure Portal
# Navigate to: SQL Server → Connection strings

# Run migrations locally
dotnet ef database update \
  --project src/DebtManager.Infrastructure \
  --startup-project src/DebtManager.Web \
  --connection "Server=tcp:..."
```

## Daily Operations

### Deploy Code Changes

Just push to `main` branch:
```bash
git add .
git commit -m "Your changes"
git push origin main
```

The app will automatically build and deploy to dev environment.

### Manual Deployment

Use the "Deploy Application" workflow from GitHub Actions UI.

### View Application Logs

```bash
# Stream live logs
az webapp log tail --name {webapp-name} --resource-group {rg-name}

# Or via Azure Portal:
# App Service → Log stream
```

### Check Application Status

```bash
# Check health endpoint
curl https://{webapp-name}.azurewebsites.net/health/live

# Check via Azure Portal:
# App Service → Overview (shows status and metrics)
```

## Troubleshooting

### Deployment Failed

1. Check workflow logs in GitHub Actions
2. Verify all secrets are set correctly
3. Check Azure Portal for error details

### App Not Starting

```bash
# View detailed logs
az webapp log tail --name {webapp-name} --resource-group {rg-name}

# Check Application Insights
# Azure Portal → Application Insights → Failures
```

### Database Connection Issues

1. Check SQL Server firewall rules
2. Verify connection string in App Service Configuration
3. Test connection:
   ```bash
   az sql db show-connection-string \
     --server {server-name} \
     --name {db-name} \
     --client ado.net
   ```

### Can't Access Application

1. Verify HTTPS URL: `https://{webapp-name}.azurewebsites.net`
2. Check App Service status in Azure Portal
3. Check Application Insights for exceptions

## Monitoring

### Application Insights

```
Azure Portal → Application Insights → {app-name}
```

View:
- Live metrics
- Failures and exceptions
- Performance
- User analytics

### Log Analytics

```
Azure Portal → Log Analytics Workspace → Logs
```

Sample query:
```kql
AppServiceHTTPLogs
| where TimeGenerated > ago(1h)
| project TimeGenerated, CsHost, CsMethod, CsUriStem, ScStatus
| order by TimeGenerated desc
```

## Resource Management

### Scale Up (More Power)

```bash
# Change App Service Plan SKU
az appservice plan update \
  --name {plan-name} \
  --resource-group {rg-name} \
  --sku B2  # Basic 2, or P1V3 for Premium
```

### Scale Out (More Instances)

```bash
# Add more instances
az appservice plan update \
  --name {plan-name} \
  --resource-group {rg-name} \
  --number-of-workers 3
```

### View Costs

```
Azure Portal → Cost Management → Cost analysis
```

Filter by resource group to see costs for your environment.

## Useful Commands

### Azure CLI

```bash
# Login
az login

# List resource groups
az group list --output table

# List all resources in a group
az resource list --resource-group {rg-name} --output table

# Get Web App URL
az webapp show --name {webapp-name} --resource-group {rg-name} --query defaultHostName -o tsv

# Restart Web App
az webapp restart --name {webapp-name} --resource-group {rg-name}

# Get SQL connection string
az sql db show-connection-string \
  --server {server-name} \
  --name {db-name} \
  --client ado.net
```

### .NET CLI

```bash
# Create migration
dotnet ef migrations add MigrationName \
  --project src/DebtManager.Infrastructure \
  --startup-project src/DebtManager.Web

# Update database
dotnet ef database update \
  --project src/DebtManager.Infrastructure \
  --startup-project src/DebtManager.Web \
  --connection "Server=..."

# List migrations
dotnet ef migrations list \
  --project src/DebtManager.Infrastructure \
  --startup-project src/DebtManager.Web
```

## Cost Estimates

### Free Tier (~$5-10 AUD/month)
- Development/testing environments
- Low traffic applications
- Limited compute resources

### Beefy Tier (~$470-700 AUD/month)
- Production workloads
- High availability
- Better performance
- Zone redundancy

## Support Links

- **Azure Status**: https://status.azure.com/
- **Azure Support**: https://portal.azure.com/#blade/Microsoft_Azure_Support/HelpAndSupportBlade
- **Documentation**: See `deploy/AZURE_DEPLOYMENT.md` for detailed guide
- **Bicep Templates**: See `deploy/bicep/README.md`

## Emergency Procedures

### Complete Rollback

Redeploy a previous working version:
```
Actions → Deploy Application → Run workflow
# Select the commit SHA from a working deployment
```

### Database Restore

```bash
# List available restore points
az sql db list-restore-points \
  --resource-group {rg-name} \
  --server {server-name} \
  --database {db-name}

# Restore to point-in-time
az sql db restore \
  --resource-group {rg-name} \
  --server {server-name} \
  --name {restored-db-name} \
  --source-database {db-name} \
  --time "2024-01-15T12:00:00Z"
```

### Infrastructure Rebuild

If infrastructure is broken:
1. Delete resource group (or resources)
2. Re-run "Deploy Infrastructure" workflow
3. Re-run "Deploy Application" workflow
4. Restore database if needed
