// Main Bicep template for Debt Management Platform
// Supports both free tier (dev/staging) and beefy tier (production)
targetScope = 'resourceGroup'

@description('The environment name (e.g., dev, staging, prod)')
param environmentName string

@description('The tier type: "free" for minimal setup or "beefy" for production')
@allowed([
  'free'
  'beefy'
])
param tierType string = 'free'

@description('Location for all resources')
param location string = 'australiaeast'

@description('Application name prefix')
param appNamePrefix string = 'debtmanager'

@description('SQL Server administrator login')
@secure()
param sqlAdminLogin string

@description('SQL Server administrator password')
@secure()
param sqlAdminPassword string

@description('Azure AD B2C Client ID')
@secure()
param azureAdB2CClientId string = ''

@description('Azure AD B2C Authority')
@secure()
param azureAdB2CAuthority string = ''

// Variables for resource naming
var uniqueSuffix = uniqueString(resourceGroup().id)
var appServicePlanName = '${appNamePrefix}-plan-${environmentName}-${uniqueSuffix}'
var webAppName = '${appNamePrefix}-web-${environmentName}-${uniqueSuffix}'
var sqlServerName = '${appNamePrefix}-sql-${environmentName}-${uniqueSuffix}'
var sqlDatabaseName = '${appNamePrefix}-db-${environmentName}'
var appInsightsName = '${appNamePrefix}-insights-${environmentName}-${uniqueSuffix}'
var logAnalyticsName = '${appNamePrefix}-logs-${environmentName}-${uniqueSuffix}'
var appGatewayName = '${appNamePrefix}-appgw-${environmentName}-${uniqueSuffix}'

// Tier-specific configurations
var appServiceSkuMap = {
  free: {
    name: 'F1'
    tier: 'Free'
    capacity: 1
  }
  beefy: {
    name: 'P1V3'
    tier: 'PremiumV3'
    capacity: 2
  }
}

var sqlSkuMap = {
  free: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  beefy: {
    name: 'S3'
    tier: 'Standard'
    capacity: 100
  }
}

// Log Analytics Workspace
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: logAnalyticsName
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: tierType == 'free' ? 30 : 90
  }
}

// Application Insights
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    IngestionMode: 'LogAnalytics'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
  }
}

// App Service Plan
resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: appServiceSkuMap[tierType]
  kind: 'linux'
  properties: {
    reserved: true
  }
}

// Web App
resource webApp 'Microsoft.Web/sites@2022-09-01' = {
  name: webAppName
  location: location
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: tierType == 'beefy'
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      healthCheckPath: '/health/live'
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: environmentName == 'prod' ? 'Production' : 'Staging'
        }
        {
          name: 'AzureAdB2C__ClientId'
          value: azureAdB2CClientId
        }
        {
          name: 'AzureAdB2C__Authority'
          value: azureAdB2CAuthority
        }
        {
          name: 'AzureAdB2C__Instance'
          value: 'https://jsraauth.b2clogin.com/'
        }
        {
          name: 'AzureAdB2C__Domain'
          value: 'jsraauth.onmicrosoft.com'
        }
      ]
      connectionStrings: [
        {
          name: 'Default'
          connectionString: 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=${sqlDatabaseName};Persist Security Info=False;User ID=${sqlAdminLogin};Password=${sqlAdminPassword};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
          type: 'SQLAzure'
        }
      ]
    }
  }
}

// SQL Server
resource sqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: sqlAdminLogin
    administratorLoginPassword: sqlAdminPassword
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// SQL Firewall rule to allow Azure services
resource sqlFirewallRule 'Microsoft.Sql/servers/firewallRules@2022-05-01-preview' = {
  parent: sqlServer
  name: 'AllowAllWindowsAzureIps'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// SQL Database
resource sqlDatabase 'Microsoft.Sql/servers/databases@2022-05-01-preview' = {
  parent: sqlServer
  name: sqlDatabaseName
  location: location
  sku: sqlSkuMap[tierType]
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: tierType == 'free' ? 2147483648 : 268435456000 // 2GB for free, 250GB for beefy
    catalogCollation: 'SQL_Latin1_General_CP1_CI_AS'
    zoneRedundant: tierType == 'beefy'
  }
}

// Outputs
output webAppName string = webApp.name
output webAppUrl string = 'https://${webApp.properties.defaultHostName}'
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName
output appInsightsInstrumentationKey string = appInsights.properties.InstrumentationKey
output appInsightsConnectionString string = appInsights.properties.ConnectionString
output resourceGroupName string = resourceGroup().name
