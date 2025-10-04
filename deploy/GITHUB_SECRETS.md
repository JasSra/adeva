# GitHub Secrets Configuration Template

This file lists all the GitHub secrets required for the Azure deployment workflows.

## How to Configure Secrets

1. Go to your GitHub repository
2. Navigate to: **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret** for each secret below
4. Copy the name exactly as shown and paste your value

## Required Secrets

### Azure Authentication (Federated Identity/OIDC)

Configure these for authentication to Azure using OpenID Connect (recommended):

```
Name: AZURE_CLIENT_ID
Value: <Your Service Principal Application (client) ID>
Description: The client ID of the Azure AD service principal
```

```
Name: AZURE_TENANT_ID
Value: <Your Azure AD Tenant ID>
Description: The tenant ID of your Azure AD
```

```
Name: AZURE_SUBSCRIPTION_ID
Value: <Your Azure Subscription ID>
Description: The subscription ID where resources will be deployed
```

### Resource Configuration

```
Name: AZURE_RESOURCE_GROUP
Value: rg-debtmanager-dev
Description: The resource group name for dev environment
Notes: For production, create a separate environment variable in GitHub environments
```

```
Name: AZURE_WEBAPP_NAME
Value: <Will be set after infrastructure deployment>
Description: The name of the Azure Web App (output from infrastructure deployment)
Notes: Run infrastructure deployment first, then get this from Azure Portal or CLI
```

### Database Configuration

```
Name: SQL_ADMIN_LOGIN
Value: <Your SQL admin username>
Description: SQL Server administrator username
Notes: Choose a secure username, avoid 'admin', 'sa', etc.
```

```
Name: SQL_ADMIN_PASSWORD
Value: <Your strong password>
Description: SQL Server administrator password
Security: Must be at least 8 characters with uppercase, lowercase, numbers, and special chars
Example format: MyStr0ng!Pass#2024
```

### Azure AD B2C Configuration

These are pre-configured for your existing B2C tenant:

```
Name: AZURE_AD_B2C_CLIENT_ID
Value: c83c5908-2b64-4304-8c53-b964ace5a1ea
Description: Azure AD B2C application client ID
```

```
Name: AZURE_AD_B2C_AUTHORITY
Value: https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0
Description: Azure AD B2C authority URL
```

## Setting Up Service Principal with Federated Credentials

### Step 1: Create Service Principal

```bash
# Login to Azure
az login

# Create service principal
az ad sp create-for-rbac \
  --name "debtmanager-github-deploy" \
  --role contributor \
  --scopes /subscriptions/<YOUR_SUBSCRIPTION_ID> \
  --json-auth > sp-credentials.json

# Note the appId (AZURE_CLIENT_ID) and tenant (AZURE_TENANT_ID)
```

### Step 2: Configure Federated Credentials for GitHub

```bash
# Get the service principal object ID
APP_ID="<your-app-id-from-step-1>"
OBJECT_ID=$(az ad app show --id $APP_ID --query id -o tsv)

# Create federated credential for main branch
az ad app federated-credential create \
  --id $OBJECT_ID \
  --parameters '{
    "name": "github-deploy-main",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:JasSra/adeva:ref:refs/heads/main",
    "description": "GitHub Actions deployment from main branch",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for pull requests (optional)
az ad app federated-credential create \
  --id $OBJECT_ID \
  --parameters '{
    "name": "github-deploy-pr",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:JasSra/adeva:pull_request",
    "description": "GitHub Actions for pull requests",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for environment (dev)
az ad app federated-credential create \
  --id $OBJECT_ID \
  --parameters '{
    "name": "github-deploy-dev",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:JasSra/adeva:environment:dev",
    "description": "GitHub Actions for dev environment",
    "audiences": ["api://AzureADTokenExchange"]
  }'

# Create federated credential for environment (prod)
az ad app federated-credential create \
  --id $OBJECT_ID \
  --parameters '{
    "name": "github-deploy-prod",
    "issuer": "https://token.actions.githubusercontent.com",
    "subject": "repo:JasSra/adeva:environment:prod",
    "description": "GitHub Actions for prod environment",
    "audiences": ["api://AzureADTokenExchange"]
  }'
```

### Step 3: Set GitHub Secrets

After creating the service principal and federated credentials:

1. Set `AZURE_CLIENT_ID` to the `appId` from step 1
2. Set `AZURE_TENANT_ID` to the `tenant` from step 1
3. Set `AZURE_SUBSCRIPTION_ID` to your Azure subscription ID

## Alternative: Using Client Secret (Less Secure)

If you prefer using client secrets instead of federated credentials:

```bash
# Create service principal with secret
az ad sp create-for-rbac \
  --name "debtmanager-github-deploy" \
  --role contributor \
  --scopes /subscriptions/<YOUR_SUBSCRIPTION_ID> \
  --sdk-auth
```

This will output JSON with credentials. Then:
- Add `AZURE_CREDENTIALS` secret with the entire JSON output
- Modify workflows to use `azure/login@v2` with `creds: ${{ secrets.AZURE_CREDENTIALS }}`

**Note**: Federated credentials (OIDC) are recommended as they're more secure and don't require managing secrets.

## Environment-Specific Secrets

For environment-specific configurations (dev vs prod), use GitHub Environments:

1. Go to **Settings** → **Environments**
2. Create environments: `dev` and `prod`
3. Add environment-specific secrets:
   - `AZURE_RESOURCE_GROUP` (e.g., `rg-debtmanager-prod` for prod)
   - `AZURE_WEBAPP_NAME` (different names for each environment)

## Verifying Secrets

After setting all secrets, verify by:

1. Going to **Actions** → **Deploy Infrastructure**
2. Click **Run workflow**
3. Select environment and tier
4. Click **Run workflow**

If it fails, check the logs for which secret might be missing or incorrect.

## Security Best Practices

1. **Never commit secrets to Git**
2. **Use strong passwords** for SQL admin
3. **Rotate secrets regularly** (especially SQL passwords and client secrets)
4. **Use federated credentials** instead of client secrets when possible
5. **Limit service principal permissions** to only what's needed
6. **Enable branch protection** on main branch
7. **Require reviews** for production deployments
8. **Use GitHub Environments** with protection rules for production

## Troubleshooting

### "Login failed" error
- Verify `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, and `AZURE_SUBSCRIPTION_ID`
- Check federated credential configuration
- Ensure service principal has Contributor role

### "Resource group not found" error
- Verify `AZURE_RESOURCE_GROUP` is set correctly
- Resource group will be created if it doesn't exist

### "Web app not found" error
- Set `AZURE_WEBAPP_NAME` after running infrastructure deployment
- Get the name from Azure Portal or using: `az webapp list --resource-group <rg-name> --query "[].name" -o tsv`

### SQL authentication errors
- Verify `SQL_ADMIN_LOGIN` and `SQL_ADMIN_PASSWORD`
- Ensure password meets complexity requirements
- Check SQL Server firewall rules

## Additional Resources

- [Azure Service Principal Documentation](https://docs.microsoft.com/azure/active-directory/develop/app-objects-and-service-principals)
- [GitHub Actions Secrets](https://docs.github.com/en/actions/security-guides/encrypted-secrets)
- [Azure Login Action](https://github.com/Azure/login)
- [OpenID Connect with GitHub Actions](https://docs.github.com/en/actions/deployment/security-hardening-your-deployments/configuring-openid-connect-in-azure)
