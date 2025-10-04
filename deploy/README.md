# Deployment Documentation Index

This directory contains all deployment-related documentation, infrastructure as code, and configuration for the Debt Management Platform.

## 📋 Quick Navigation

### Getting Started
1. **[QUICKSTART.md](QUICKSTART.md)** - Fast-track deployment guide
   - Step-by-step deployment instructions
   - Common operations reference
   - Troubleshooting tips

2. **[GITHUB_SECRETS.md](GITHUB_SECRETS.md)** - GitHub secrets configuration
   - Complete list of required secrets
   - Service principal setup guide
   - Federated credential configuration

### Detailed Documentation
3. **[AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md)** - Comprehensive deployment guide
   - Architecture overview
   - Detailed Azure resources description
   - Cost estimates for both tiers
   - Monitoring and observability
   - Security considerations

### Infrastructure as Code
4. **[bicep/](bicep/)** - Bicep templates directory
   - `main.bicep` - Core infrastructure template
   - `appgateway.bicep` - Optional Application Gateway
   - `parameters.dev.json` - Free tier parameters
   - `parameters.prod.json` - Beefy tier parameters
   - `README.md` - Bicep usage guide

### Configuration
5. **[CONFIG_SECRETS.md](CONFIG_SECRETS.md)** - Application configuration strategy
   - Local development setup
   - Staging/production configuration
   - Azure Key Vault integration (planned)

### Utilities
6. **[validate-bicep.sh](validate-bicep.sh)** - Bicep validation script
   - Validates templates locally
   - Runs what-if analysis
   - Requires Azure CLI

7. **[BADGES.md](BADGES.md)** - GitHub Actions status badges
   - Ready-to-use badge markdown
   - For README.md enhancement

## 🚀 Deployment Workflows

Located in `.github/workflows/`:

1. **build-test.yml** - Continuous Integration
   - Runs on all PRs and pushes
   - Builds and tests the application
   - Validates code quality

2. **deploy-app.yml** - Application Deployment
   - Deploys to Azure App Service
   - Runs automatically on main branch
   - Manual trigger available

3. **deploy-infrastructure.yml** - Infrastructure Deployment
   - Deploys Azure resources using Bicep
   - Manual trigger only
   - Supports both free and beefy tiers

## 🏗️ Infrastructure Tiers

### Free Tier (~$5-10 AUD/month)
Perfect for development and staging environments:
- App Service: F1 (Free)
- SQL Database: Basic (5 DTUs, 2GB)
- Application Insights: Free tier
- Log Analytics: 30-day retention
- **Use case**: Development, testing, demos

### Beefy Tier (~$470-700 AUD/month)
Production-ready with high availability:
- App Service: P1V3 Premium (2 instances)
- SQL Database: S3 Standard (100 DTUs, 250GB, zone-redundant)
- Application Insights: Full features
- Log Analytics: 90-day retention
- Optional: Application Gateway with WAF
- **Use case**: Production workloads

## 📍 Azure Resources Deployed

All resources are deployed in **Australia East** region:

### Core Resources (Both Tiers)
- **App Service Plan** - Hosts the web application
- **Web App** - .NET 8 web application
- **SQL Server** - Managed SQL Server instance
- **SQL Database** - Application database
- **Application Insights** - Application monitoring and telemetry
- **Log Analytics Workspace** - Centralized logging

### Optional Resources (Beefy Tier)
- **Application Gateway** - Advanced routing and WAF
- **Virtual Network** - Network isolation
- **Public IP** - Static IP for Application Gateway

### Planned Resources
- **Azure Key Vault** - Secret management
- **Azure Blob Storage** - File storage
- **Azure CDN** - Content delivery
- **Azure Front Door** - Global load balancing

## 🔐 Security Features

1. **HTTPS Only** - All communication encrypted
2. **TLS 1.2+** - Modern TLS version enforcement
3. **Managed Identity** - No credentials in code
4. **SQL Firewall** - Restricted network access
5. **Azure AD B2C** - Enterprise authentication
6. **Application Insights** - Security monitoring
7. **WAF** - Web application firewall (beefy tier)

## 📊 Monitoring and Observability

### Application Insights
- Real-time application metrics
- Exception tracking
- Dependency monitoring
- User analytics
- Custom telemetry

### Log Analytics
- Centralized logging
- KQL query support
- Custom dashboards
- Alerting rules

### Health Endpoints
- `/health/live` - Liveness probe
- `/health/ready` - Readiness probe

## 🛠️ Deployment Process

### First-Time Setup
1. Configure GitHub secrets (see GITHUB_SECRETS.md)
2. Run "Deploy Infrastructure" workflow
3. Configure Web App name in secrets
4. Run "Deploy Application" workflow
5. Run database migrations

### Regular Deployments
1. Push code to main branch
2. GitHub Actions automatically builds and tests
3. Application deploys to dev environment
4. Manual promotion to production (optional)

## 📝 Configuration Files

### GitHub Actions Workflows
```
.github/workflows/
├── build-test.yml           # CI pipeline
├── deploy-app.yml           # Application deployment
└── deploy-infrastructure.yml # Infrastructure deployment
```

### Bicep Templates
```
deploy/bicep/
├── main.bicep              # Core infrastructure
├── appgateway.bicep        # Optional Application Gateway
├── parameters.dev.json     # Dev environment params
├── parameters.prod.json    # Prod environment params
└── README.md              # Bicep documentation
```

### Documentation
```
deploy/
├── AZURE_DEPLOYMENT.md     # Comprehensive guide
├── QUICKSTART.md          # Quick reference
├── GITHUB_SECRETS.md      # Secrets configuration
├── CONFIG_SECRETS.md      # App configuration
├── BADGES.md              # Status badges
├── validate-bicep.sh      # Validation script
└── README.md              # This file
```

## 🔄 Continuous Integration/Deployment

### CI Pipeline (build-test.yml)
Triggers on:
- Push to main or develop
- Pull requests to main or develop

Steps:
1. Checkout code
2. Setup .NET 8 and Node.js 20
3. Restore dependencies
4. Build application
5. Run tests
6. Build Tailwind CSS
7. Upload test results

### CD Pipeline (deploy-app.yml)
Triggers on:
- Push to main (automatic to dev)
- Manual workflow dispatch

Steps:
1. Build application
2. Run tests
3. Publish application
4. Build frontend assets
5. Deploy to Azure Web App
6. Run migrations (placeholder)

## 🎯 Next Steps

After deploying the infrastructure:

1. **Configure Custom Domain** (optional)
   - Add custom domain in Azure Portal
   - Configure SSL certificate
   - Update DNS records

2. **Set Up Staging Slots** (beefy tier)
   - Create staging slot
   - Configure slot-specific settings
   - Enable deployment slots in workflow

3. **Enable Auto-scaling** (beefy tier)
   - Configure scaling rules
   - Set min/max instances
   - Add metrics-based scaling

4. **Configure Alerts**
   - Set up Azure Monitor alerts
   - Configure notification channels
   - Define SLA thresholds

5. **Implement Backup Strategy**
   - Configure SQL Database backups
   - Set up geo-replication (optional)
   - Test restore procedures

## 📚 Additional Resources

### Azure Documentation
- [App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [SQL Database Documentation](https://docs.microsoft.com/azure/azure-sql/)
- [Application Insights Documentation](https://docs.microsoft.com/azure/azure-monitor/app/app-insights-overview)
- [Bicep Documentation](https://docs.microsoft.com/azure/azure-resource-manager/bicep/)

### GitHub Actions
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Actions](https://github.com/Azure/actions)
- [Deployment Environments](https://docs.github.com/en/actions/deployment/targeting-different-environments/using-environments-for-deployment)

### .NET 8
- [ASP.NET Core Documentation](https://docs.microsoft.com/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/ef/core/)
- [Deployment Documentation](https://docs.microsoft.com/aspnet/core/host-and-deploy/)

## 💡 Tips and Best Practices

1. **Use Environments** - Separate dev, staging, and prod with GitHub Environments
2. **Enable Branch Protection** - Require reviews for main branch
3. **Monitor Costs** - Set up budget alerts in Azure
4. **Test in Dev First** - Always deploy to dev before production
5. **Use Managed Identities** - Avoid storing credentials
6. **Enable Diagnostics** - Configure diagnostic settings for all resources
7. **Tag Resources** - Use tags for cost tracking and organization
8. **Review Logs Regularly** - Check Application Insights and Log Analytics
9. **Update Dependencies** - Keep packages and tools up to date
10. **Document Changes** - Update documentation when infrastructure changes

## 🆘 Support

For issues or questions:
1. Check [QUICKSTART.md](QUICKSTART.md) for common operations
2. Review [AZURE_DEPLOYMENT.md](AZURE_DEPLOYMENT.md) for detailed information
3. Check GitHub Actions workflow logs
4. Review Application Insights for runtime issues
5. Create an issue in the repository

---

**Last Updated**: January 2024  
**Maintained By**: Development Team  
**License**: As per repository license
