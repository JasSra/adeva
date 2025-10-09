# Dapr Saga Orchestration - Gap Analysis & Detailed Implementation Plan

## Executive Summary

This document provides a comprehensive gap analysis of the current Debt Management Platform against the proposed Dapr Saga orchestration architecture. It includes detailed, minute-level implementation steps for each migration phase and incorporates HashiCorp Vault for enhanced secret management.

---

## Table of Contents

1. [Current State Analysis](#current-state-analysis)
2. [Gap Analysis](#gap-analysis)
3. [Detailed Phase Implementation](#detailed-phase-implementation)
4. [HashiCorp Vault Integration](#hashicorp-vault-integration)
5. [Workflow-Specific Analysis](#workflow-specific-analysis)
6. [Risk Mitigation Matrix](#risk-mitigation-matrix)

---

## Current State Analysis

### Architecture Overview

**Current Architecture: Monolithic MVC Application**

```
┌────────────────────────────────────────────────────┐
│         DebtManager.Web (ASP.NET Core MVC)         │
│                                                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐        │
│  │  Areas/  │  │ Services │  │  Jobs/   │        │
│  │  Admin   │  │          │  │ Hangfire │        │
│  │  Client  │  │          │  │          │        │
│  │  User    │  │          │  │          │        │
│  └──────────┘  └──────────┘  └──────────┘        │
│                                                    │
│  ┌──────────────────────────────────────────┐     │
│  │   DebtManager.Application (CQRS/MediatR) │     │
│  └──────────────────────────────────────────┘     │
│                                                    │
│  ┌──────────────────────────────────────────┐     │
│  │   DebtManager.Infrastructure             │     │
│  │   - EF Core Repositories                 │     │
│  │   - External Service Adapters            │     │
│  └──────────────────────────────────────────┘     │
│                                                    │
│  ┌──────────────────────────────────────────┐     │
│  │   DebtManager.Domain (Aggregates)        │     │
│  └──────────────────────────────────────────┘     │
└────────────────────────────────────────────────────┘
              │
              ▼
    ┌──────────────────┐
    │  SQL Server      │
    │  - DebtManager   │
    │  - Hangfire      │
    └──────────────────┘
```

### Current Components Inventory

#### 1. **Projects (6 total)**
- `DebtManager.Web` - MVC frontend with 3 areas (Admin, Client, User)
- `DebtManager.Application` - CQRS layer with MediatR
- `DebtManager.Domain` - Domain entities and aggregates
- `DebtManager.Infrastructure` - Data access and external integrations
- `DebtManager.Contracts` - Shared interfaces and DTOs
- `Services.AbrValidation` - ABR validation microservice (optional/standalone)

#### 2. **Controllers (45 total)**

**Admin Area Controllers:**
- AccountsController
- AnalyticsController
- ArticlesController
- AuditController
- BusinessLookupController
- CommsController
- DebtsController
- DebtorsController
- DocumentsController
- InvoiceProcessingController
- JobsController
- MessagesController
- OrganizationsController
- PaymentPlansController
- ReceiptsController
- TransactionsController

**Client Area Controllers:**
- AuditController
- BrandingController
- CommunicationsController
- DebtsController
- DelegatedAccessController
- HomeController
- MessagesController
- OnboardingController
- OrganizationController
- ReceiptsController

**User Area Controllers:**
- AcceptController
- AuthController
- DebtController
- HomeController
- MessagesController
- PaymentController
- ReceiptController

**Root Controllers:**
- AccountController
- ArticleController
- BackofficeController
- DevController
- HomeController
- PaymentController
- PaymentApiController
- PaymentPlanApiController
- PublicAcceptController
- SecurityController
- WebhooksController

#### 3. **Services**

**Current Services (DebtManager.Web/Services):**
- `OnboardingNotificationService` - Handles organization onboarding notifications
- `MessageQueueService` - Queues internal messages, emails, and SMS
- `AdminService` - Admin-specific operations
- `BrandingResolverMiddleware` - Multi-tenant branding resolution
- `MaintenanceState` - Application maintenance mode management

**Infrastructure Services (DebtManager.Infrastructure):**
- `AppConfigService` - Configuration management
- `BusinessLookupService` - ABR API integration
- `AzureFormRecognizerInvoiceService` - OCR for invoices
- `DocumentGenerationService` - PDF generation
- `PaymentPlanAIService` - AI-powered payment plan recommendations
- `EmailSender` - SMTP/SendGrid email delivery
- `SmsSender` - Twilio SMS delivery
- `MetricService` - Analytics and metrics
- `StripePaymentService` - Stripe payment processing
- `StripeWebhookProcessor` - Stripe webhook handling

#### 4. **Background Jobs (Hangfire)**

**Current Jobs:**
- `MessageDispatchJob` - Processes queued messages (email/SMS)
- `NightlyJobs` - Scheduled maintenance tasks
- `PaymentWebhookJob` - Processes Stripe webhook events

**Job Storage:** SQL Server (shared with main database)

#### 5. **External Integrations**
- **Azure AD B2C** - Authentication and authorization
- **Stripe** - Payment processing
- **Twilio** - SMS notifications (configured but implementation may vary)
- **SMTP/SendGrid** - Email delivery
- **ABR API** - Australian Business Number validation

#### 6. **Database**
- **SQL Server** (single database)
- **Entity Framework Core** for data access
- **Repository Pattern** with Unit of Work
- **Migrations** managed in Infrastructure project

#### 7. **Configuration & Secrets**
- **appsettings.json** - Base configuration
- **Connection strings** - Plaintext in appsettings
- **API keys** - Plaintext in appsettings
- **ClientSecret** - Plaintext in appsettings (Azure AD B2C)
- **No centralized secret management**

---

## Gap Analysis

### 1. **Service Decomposition Gaps**

| Component Needed | Current State | Gap | Priority |
|------------------|---------------|-----|----------|
| **Saga Orchestrator Service** | ❌ Does not exist | Need new .NET service with Dapr Workflow SDK | **Critical** |
| **Organization Service** | ⚠️ Embedded in monolith | Extract from Areas/Client & Areas/Admin | **High** |
| **Debt Service** | ⚠️ Embedded in monolith | Extract debt management logic | **High** |
| **Payment Service** | ⚠️ Partially separated | Extract payment processing logic | **High** |
| **Notification Service** | ⚠️ Embedded | Extract email/SMS logic | **Medium** |
| **Customer/Debtor Service** | ⚠️ Embedded | Extract debtor management | **Medium** |
| **Validation Service** | ⚠️ Partially exists (ABR) | Enhance and expose via Dapr | **Low** |

### 2. **Infrastructure Gaps**

| Component | Current State | Gap | Priority |
|-----------|---------------|-----|----------|
| **Message Bus** | ❌ None | Need Azure Service Bus or Kafka | **Critical** |
| **State Store** | ❌ None (SQL only) | Need Redis/Cosmos DB for saga state | **Critical** |
| **Dapr Runtime** | ❌ Not installed | Install Dapr CLI and runtime | **Critical** |
| **Dapr Sidecars** | ❌ Not configured | Configure sidecar for each service | **Critical** |
| **HashiCorp Vault** | ❌ Not configured | Setup Vault for secrets | **High** |
| **Distributed Tracing** | ❌ No Zipkin/Jaeger | Setup OpenTelemetry + Zipkin | **Medium** |
| **Service Mesh** | ❌ None | Optional: Consider Istio/Linkerd | **Low** |

### 3. **Configuration & Secret Management Gaps**

| Item | Current State | Gap | Priority |
|------|---------------|-----|----------|
| **Secret Storage** | ⚠️ Plaintext in appsettings | Migrate to HashiCorp Vault | **Critical** |
| **Connection Strings** | ⚠️ Plaintext | Store in Vault, inject via Dapr | **Critical** |
| **API Keys (Stripe, Twilio, ABR)** | ⚠️ Plaintext | Store in Vault | **Critical** |
| **Certificate Management** | ❌ Manual | Automate with Vault PKI | **High** |
| **Secret Rotation** | ❌ Manual process | Implement automatic rotation | **High** |
| **Audit Logging for Secrets** | ❌ None | Enable Vault audit logs | **Medium** |

### 4. **Workflow State Management Gaps**

| Workflow | Current State | Gap | Priority |
|----------|---------------|-----|----------|
| **Organization Onboarding** | ⚠️ Synchronous controller action | Convert to saga with compensation | **High** |
| **Debt Raising** | ⚠️ Multiple DB transactions | Implement saga pattern | **High** |
| **Payment Processing** | ⚠️ Webhook-driven (async) | Add saga coordination | **High** |
| **Customer Identification** | ❌ No formal workflow | Create new saga workflow | **Medium** |
| **State Persistence** | ⚠️ SQL-only | Add Redis for workflow state | **High** |
| **Compensation Logic** | ❌ Manual cleanup | Implement automated compensation | **High** |

### 5. **Observability Gaps**

| Capability | Current State | Gap | Priority |
|------------|---------------|-----|----------|
| **Distributed Tracing** | ❌ None | Implement OpenTelemetry | **High** |
| **Saga Execution Tracking** | ❌ None | Dapr workflow telemetry | **High** |
| **Cross-Service Correlation** | ❌ None | Correlation IDs via Dapr | **Medium** |
| **Metrics Dashboard** | ⚠️ Basic (Hangfire only) | Prometheus + Grafana | **Medium** |
| **Log Aggregation** | ⚠️ Serilog to console/file | Centralize with ELK/Loki | **Medium** |
| **Alerting** | ❌ None | Setup alerts for saga failures | **Low** |

### 6. **Testing Gaps**

| Test Type | Current State | Gap | Priority |
|-----------|---------------|-----|----------|
| **Saga Integration Tests** | ❌ None | Create saga-specific tests | **High** |
| **Chaos Engineering** | ❌ None | Test compensation logic | **Medium** |
| **Load Testing** | ❌ None | Test saga throughput | **Medium** |
| **Contract Testing** | ❌ None | Service boundary contracts | **Low** |

### 7. **Deployment Gaps**

| Component | Current State | Gap | Priority |
|-----------|---------------|-----|----------|
| **Container Images** | ❌ Not containerized | Dockerize all services | **High** |
| **Kubernetes Manifests** | ❌ None | Create K8s deployments | **High** |
| **Helm Charts** | ❌ None | Package services as Helm charts | **Medium** |
| **CI/CD Pipeline** | ⚠️ Basic (GitHub Actions) | Extend for multi-service deployment | **High** |
| **Feature Flags** | ❌ None | Implement feature flag system | **High** |
| **Blue-Green Deployment** | ❌ None | Setup deployment strategies | **Low** |

---

## Detailed Phase Implementation

### Phase 1: Foundation & Side-by-Side Deployment (Weeks 1-4)

**Objective:** Install Dapr infrastructure, create saga orchestrator service, and run alongside monolith without breaking changes.

#### Week 1: Environment Setup & Infrastructure Provisioning

##### Day 1-2: Development Environment Setup

**Tasks:**
1. Install Dapr CLI on all developer machines
2. Initialize Dapr runtime locally
3. Verify Dapr dashboard access
4. Setup local Redis for state store
5. Setup local Service Bus emulator or Redis pub/sub

**Detailed Steps:**

```bash
# Windows (PowerShell as Administrator)
powershell -Command "iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex"

# macOS
brew install dapr/tap/dapr-cli

# Linux
wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | /bin/bash

# Initialize Dapr (installs Redis, Zipkin, Placement service)
dapr init

# Verify installation
dapr --version
docker ps  # Should see dapr_redis, dapr_zipkin, dapr_placement

# Start Dapr dashboard
dapr dashboard -p 8080
# Access at http://localhost:8080
```

**Validation:**
- [ ] Dapr CLI installed on all machines
- [ ] Dapr runtime initialized successfully
- [ ] Dashboard accessible at http://localhost:8080
- [ ] Redis container running
- [ ] Zipkin container running
- [ ] Placement service running

##### Day 3-4: HashiCorp Vault Setup

**Tasks:**
1. Install Vault server (dev mode for local, production config for staging/prod)
2. Initialize and unseal Vault
3. Enable audit logging
4. Create policies for each service
5. Setup AppRole authentication
6. Configure Vault Agent for automatic secret injection

**Detailed Steps:**

**Install Vault:**
```bash
# macOS
brew install hashicorp/tap/vault

# Linux
wget -O- https://apt.releases.hashicorp.com/gpg | sudo gpg --dearmor -o /usr/share/keyrings/hashicorp-archive-keyring.gpg
echo "deb [signed-by=/usr/share/keyrings/hashicorp-archive-keyring.gpg] https://apt.releases.hashicorp.com $(lsb_release -cs) main" | sudo tee /etc/apt/sources.list.d/hashicorp.list
sudo apt update && sudo apt install vault

# Windows (Chocolatey)
choco install vault
```

**Start Vault Dev Server (Local Development):**
```bash
# Start Vault in dev mode (DO NOT USE IN PRODUCTION)
vault server -dev -dev-root-token-id="root"

# In new terminal, set environment
export VAULT_ADDR='http://127.0.0.1:8200'
export VAULT_TOKEN='root'

# Verify
vault status
```

**Production Vault Setup:**
```bash
# Create Vault config
cat > vault-config.hcl <<EOF
storage "file" {
  path = "/opt/vault/data"
}

listener "tcp" {
  address     = "0.0.0.0:8200"
  tls_disable = 0
  tls_cert_file = "/opt/vault/tls/vault.crt"
  tls_key_file  = "/opt/vault/tls/vault.key"
}

api_addr = "https://vault.example.com:8200"
cluster_addr = "https://vault.example.com:8201"
ui = true
EOF

# Start Vault
vault server -config=vault-config.hcl

# Initialize Vault (first time only)
vault operator init -key-shares=5 -key-threshold=3

# Unseal Vault (requires 3 of 5 keys)
vault operator unseal <key-1>
vault operator unseal <key-2>
vault operator unseal <key-3>

# Login with root token
vault login <root-token>
```

**Enable Audit Logging:**
```bash
vault audit enable file file_path=/var/log/vault/audit.log
```

**Create Policies:**
```bash
# Saga Orchestrator Policy
vault policy write saga-orchestrator - <<EOF
path "secret/data/saga-orchestrator/*" {
  capabilities = ["read", "list"]
}
path "database/creds/saga-orchestrator" {
  capabilities = ["read"]
}
EOF

# Organization Service Policy
vault policy write organization-service - <<EOF
path "secret/data/organization-service/*" {
  capabilities = ["read", "list"]
}
path "database/creds/organization-service" {
  capabilities = ["read"]
}
EOF

# Debt Service Policy
vault policy write debt-service - <<EOF
path "secret/data/debt-service/*" {
  capabilities = ["read", "list"]
}
path "database/creds/debt-service" {
  capabilities = ["read"]
}
EOF

# Payment Service Policy
vault policy write payment-service - <<EOF
path "secret/data/payment-service/*" {
  capabilities = ["read", "list"]
}
path "secret/data/stripe/*" {
  capabilities = ["read"]
}
EOF

# Notification Service Policy
vault policy write notification-service - <<EOF
path "secret/data/notification-service/*" {
  capabilities = ["read", "list"]
}
path "secret/data/twilio/*" {
  capabilities = ["read"]
}
path "secret/data/sendgrid/*" {
  capabilities = ["read"]
}
EOF
```

**Enable AppRole Authentication:**
```bash
# Enable AppRole
vault auth enable approle

# Create AppRole for Saga Orchestrator
vault write auth/approle/role/saga-orchestrator \
    token_policies="saga-orchestrator" \
    token_ttl=1h \
    token_max_ttl=4h

# Get Role ID
vault read auth/approle/role/saga-orchestrator/role-id

# Generate Secret ID
vault write -f auth/approle/role/saga-orchestrator/secret-id

# Repeat for each service...
```

**Store Initial Secrets:**
```bash
# Enable KV v2 secrets engine
vault secrets enable -path=secret kv-v2

# Store database connection strings
vault kv put secret/database/connection-strings \
    default="Server=localhost,1433;Database=DebtManager;User Id=sa;Password=Your_strong_password123;" \
    hangfire="Server=localhost,1433;Database=DebtManager;User Id=sa;Password=Your_strong_password123;"

# Store Stripe keys
vault kv put secret/stripe \
    publishable_key="pk_test_..." \
    secret_key="sk_test_..." \
    webhook_secret="whsec_..."

# Store Azure AD B2C secrets
vault kv put secret/azure-ad-b2c \
    client_id="c83c5908-2b64-4304-8c53-b964ace5a1ea" \
    client_secret="<your-secret>" \
    authority="https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0"

# Store Twilio credentials
vault kv put secret/twilio \
    account_sid="<your-sid>" \
    auth_token="<your-token>" \
    phone_number="+1234567890"

# Store SendGrid API key
vault kv put secret/sendgrid \
    api_key="SG...."

# Store ABR API credentials
vault kv put secret/abr-api \
    base_url="https://api.abr.example/" \
    api_key="<your-key>"
```

**Configure Vault Agent:**

Create `vault-agent-config.hcl`:
```hcl
pid_file = "./vault-agent.pid"

vault {
  address = "http://127.0.0.1:8200"
}

auto_auth {
  method {
    type = "approle"
    config = {
      role_id_file_path = "/etc/vault/role-id"
      secret_id_file_path = "/etc/vault/secret-id"
      remove_secret_id_file_after_reading = false
    }
  }

  sink {
    type = "file"
    config = {
      path = "/tmp/vault-token"
    }
  }
}

template {
  source      = "/etc/vault/templates/appsettings.json.tmpl"
  destination = "/app/appsettings.json"
  command     = "systemctl reload myapp"
}
```

Create template `/etc/vault/templates/appsettings.json.tmpl`:
```json
{
  "ConnectionStrings": {
{{ with secret "secret/database/connection-strings" }}
    "Default": "{{ .Data.data.default }}",
    "Hangfire": "{{ .Data.data.hangfire }}"
{{ end }}
  },
  "Stripe": {
{{ with secret "secret/stripe" }}
    "PublishableKey": "{{ .Data.data.publishable_key }}",
    "SecretKey": "{{ .Data.data.secret_key }}",
    "WebhookSecret": "{{ .Data.data.webhook_secret }}"
{{ end }}
  }
}
```

Run Vault Agent:
```bash
vault agent -config=vault-agent-config.hcl
```

**Validation:**
- [ ] Vault server running and accessible
- [ ] Vault initialized and unsealed
- [ ] Audit logging enabled
- [ ] Policies created for all services
- [ ] AppRoles configured
- [ ] Initial secrets stored
- [ ] Vault Agent configured and tested

##### Day 5: Azure Infrastructure Provisioning (Production)

**Tasks:**
1. Provision Azure Service Bus namespace
2. Provision Azure Redis Cache
3. Provision Azure Key Vault (alternative to self-hosted Vault)
4. Provision Application Insights for monitoring
5. Configure network security groups and virtual networks

**Detailed Steps:**

**1. Create Resource Group:**
```bash
az group create \
  --name debt-management-dapr-rg \
  --location australiaeast
```

**2. Provision Azure Service Bus:**
```bash
# Create Service Bus namespace (Standard tier for topics)
az servicebus namespace create \
  --name debtmanager-servicebus \
  --resource-group debt-management-dapr-rg \
  --location australiaeast \
  --sku Standard

# Get connection string
az servicebus namespace authorization-rule keys list \
  --resource-group debt-management-dapr-rg \
  --namespace-name debtmanager-servicebus \
  --name RootManageSharedAccessKey \
  --query primaryConnectionString -o tsv

# Create topics
az servicebus topic create \
  --namespace-name debtmanager-servicebus \
  --resource-group debt-management-dapr-rg \
  --name organization.created

az servicebus topic create \
  --namespace-name debtmanager-servicebus \
  --resource-group debt-management-dapr-rg \
  --name debt.created

az servicebus topic create \
  --namespace-name debtmanager-servicebus \
  --resource-group debt-management-dapr-rg \
  --name payment.processed
```

**3. Provision Azure Redis Cache:**
```bash
# Create Redis Cache (Basic C0 for dev, Standard C1+ for prod)
az redis create \
  --name debtmanager-redis \
  --resource-group debt-management-dapr-rg \
  --location australiaeast \
  --sku Basic \
  --vm-size c0

# Get access keys
az redis list-keys \
  --name debtmanager-redis \
  --resource-group debt-management-dapr-rg \
  --query primaryKey -o tsv

# Get host name
az redis show \
  --name debtmanager-redis \
  --resource-group debt-management-dapr-rg \
  --query hostName -o tsv
```

**4. Provision Azure Key Vault (Alternative to HashiCorp Vault):**
```bash
# Create Key Vault
az keyvault create \
  --name debtmanager-kv \
  --resource-group debt-management-dapr-rg \
  --location australiaeast \
  --enable-rbac-authorization false \
  --enabled-for-deployment true \
  --enabled-for-template-deployment true

# Store secrets
az keyvault secret set \
  --vault-name debtmanager-kv \
  --name servicebus-connection-string \
  --value "<connection-string>"

az keyvault secret set \
  --vault-name debtmanager-kv \
  --name redis-password \
  --value "<redis-key>"

az keyvault secret set \
  --vault-name debtmanager-kv \
  --name stripe-secret-key \
  --value "sk_test_..."

# Create access policy for Saga Orchestrator
az keyvault set-policy \
  --name debtmanager-kv \
  --object-id <service-principal-object-id> \
  --secret-permissions get list
```

**5. Provision Application Insights:**
```bash
# Create Application Insights
az monitor app-insights component create \
  --app debtmanager-insights \
  --location australiaeast \
  --resource-group debt-management-dapr-rg \
  --kind web

# Get instrumentation key
az monitor app-insights component show \
  --app debtmanager-insights \
  --resource-group debt-management-dapr-rg \
  --query instrumentationKey -o tsv
```

**Validation:**
- [ ] Service Bus namespace created with topics
- [ ] Redis Cache provisioned and accessible
- [ ] Key Vault created with secrets
- [ ] Application Insights configured
- [ ] Connection strings retrieved and stored in Vault

#### Week 2: Saga Orchestrator Service Creation

##### Day 1-2: Project Scaffolding

**Tasks:**
1. Create new .NET 8 Web API project for Saga Orchestrator
2. Add Dapr Workflow SDK NuGet packages
3. Setup project structure
4. Configure Dependency Injection
5. Add logging and configuration

**Detailed Steps:**

```bash
# Navigate to src directory
cd /home/runner/work/adeva/adeva/src

# Create new Web API project
dotnet new webapi -n DebtManager.Sagas -f net8.0

cd DebtManager.Sagas

# Add Dapr packages
dotnet add package Dapr.Workflow --version 1.13.0
dotnet add package Dapr.Client --version 1.13.0
dotnet add package Dapr.AspNetCore --version 1.13.0

# Add logging
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.File

# Add OpenTelemetry
dotnet add package OpenTelemetry.Exporter.Zipkin
dotnet add package OpenTelemetry.Extensions.Hosting
dotnet add package OpenTelemetry.Instrumentation.AspNetCore
dotnet add package OpenTelemetry.Instrumentation.Http

# Add reference to existing contracts
dotnet add reference ../DebtManager.Contracts/DebtManager.Contracts.csproj

# Add to solution
cd ../..
dotnet sln add src/DebtManager.Sagas/DebtManager.Sagas.csproj
```

**Create Directory Structure:**
```bash
cd src/DebtManager.Sagas

mkdir -p Orchestrators
mkdir -p Activities
mkdir -p Models
mkdir -p Models/Events
mkdir -p Models/Requests
mkdir -p Models/Responses
mkdir -p Compensation
mkdir -p Controllers
mkdir -p Services
mkdir -p Configuration
```

**Create Program.cs:**
```csharp
using Dapr.Workflow;
using DebtManager.Sagas.Orchestrators;
using DebtManager.Sagas.Activities;
using Serilog;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/saga-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// OpenTelemetry
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("saga-orchestrator"))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("Dapr.Workflow")
            .AddZipkinExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:9411/api/v2/spans");
            });
    });

// Dapr Workflow
builder.Services.AddDaprWorkflow(options =>
{
    // Register workflows
    options.RegisterWorkflow<OrganizationOnboardingSaga>();
    options.RegisterWorkflow<DebtRaisingSaga>();
    options.RegisterWorkflow<CustomerIdentificationSaga>();
    options.RegisterWorkflow<PaymentProcessingSaga>();
    
    // Register activities
    options.RegisterActivity<OrganizationActivities>();
    options.RegisterActivity<DebtActivities>();
    options.RegisterActivity<PaymentActivities>();
    options.RegisterActivity<NotificationActivities>();
    options.RegisterActivity<ValidationActivities>();
    options.RegisterActivity<AuditActivities>();
});

// Dapr Client
builder.Services.AddDaprClient();

// Activity classes
builder.Services.AddScoped<OrganizationActivities>();
builder.Services.AddScoped<DebtActivities>();
builder.Services.AddScoped<PaymentActivities>();
builder.Services.AddScoped<NotificationActivities>();
builder.Services.AddScoped<ValidationActivities>();
builder.Services.AddScoped<AuditActivities>();

// Controllers
builder.Services.AddControllers().AddDapr();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseRouting();
app.UseCloudEvents();

app.MapControllers();
app.MapSubscribeHandler();
app.MapHealthChecks("/health");

app.Run();
```

**Create appsettings.json:**
```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "Dapr": "Information"
      }
    }
  },
  "AllowedHosts": "*",
  "Dapr": {
    "HttpPort": 3500,
    "GrpcPort": 50001
  }
}
```

**Validation:**
- [ ] Project created and compiles
- [ ] Dapr packages installed
- [ ] Directory structure created
- [ ] Program.cs configured with Dapr Workflow
- [ ] Serilog and OpenTelemetry configured

##### Day 3-5: Implement Organization Onboarding Saga

**Create Models:**

`Models/Requests/OnboardingRequest.cs`:
```csharp
namespace DebtManager.Sagas.Models.Requests;

public record OnboardingRequest
{
    public string Name { get; init; } = string.Empty;
    public string LegalName { get; init; } = string.Empty;
    public string Abn { get; init; } = string.Empty;
    public string ContactFirstName { get; init; } = string.Empty;
    public string ContactLastName { get; init; } = string.Empty;
    public string ContactEmail { get; init; } = string.Empty;
    public string ContactPhone { get; init; } = string.Empty;
    public string Subdomain { get; init; } = string.Empty;
    public string PrimaryColor { get; init; } = "#1E40AF";
    public string SecondaryColor { get; init; } = "#3B82F6";
    public string SupportEmail { get; init; } = string.Empty;
    public string SupportPhone { get; init; } = string.Empty;
    public string Timezone { get; init; } = "Australia/Melbourne";
    public Guid InitiatedBy { get; init; }
    public DateTime InitiatedAt { get; init; } = DateTime.UtcNow;
}
```

`Models/Responses/OnboardingResult.cs`:
```csharp
namespace DebtManager.Sagas.Models.Responses;

public record OnboardingResult
{
    public string SagaId { get; init; } = string.Empty;
    public Guid OrganizationId { get; set; }
    public bool Success { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CompletedAt { get; set; }
    public List<string> StepsCompleted { get; set; } = new();
}
```

**Create Orchestrator:**

`Orchestrators/OrganizationOnboardingSaga.cs`:
```csharp
using Dapr.Workflow;
using DebtManager.Sagas.Activities;
using DebtManager.Sagas.Models.Requests;
using DebtManager.Sagas.Models.Responses;

namespace DebtManager.Sagas.Orchestrators;

public class OrganizationOnboardingSaga : Workflow<OnboardingRequest, OnboardingResult>
{
    public override async Task<OnboardingResult> RunAsync(
        WorkflowContext context,
        OnboardingRequest input)
    {
        var logger = context.CreateReplaySafeLogger<OrganizationOnboardingSaga>();
        var result = new OnboardingResult { SagaId = context.InstanceId };
        
        try
        {
            // Step 1: Create Organization (Pending status)
            logger.LogInformation("Creating organization for {Name}", input.Name);
            var orgId = await context.CallActivityAsync<Guid>(
                nameof(OrganizationActivities.CreateOrganizationAsync),
                input);
            result.OrganizationId = orgId;
            result.StepsCompleted.Add("Organization Created");
            
            // Step 2: Validate ABN
            logger.LogInformation("Validating ABN {ABN}", input.Abn);
            var validationResult = await context.CallActivityAsync<ValidationResult>(
                nameof(ValidationActivities.ValidateAbnAsync),
                input.Abn);
            
            if (!validationResult.IsValid)
            {
                logger.LogWarning("ABN validation failed for {ABN}: {Reason}",
                    input.Abn, validationResult.FailureReason);
                result.Success = false;
                result.FailureReason = validationResult.FailureReason;
                
                // Compensation: Delete organization
                await context.CallActivityAsync(
                    nameof(OrganizationActivities.DeleteOrganizationAsync),
                    orgId);
                result.StepsCompleted.Add("Organization Deleted (Compensation)");
                
                result.CompletedAt = DateTime.UtcNow;
                return result;
            }
            result.StepsCompleted.Add("ABN Validated");
            
            // Step 3: Send Welcome Email
            logger.LogInformation("Sending welcome email to {Email}", input.ContactEmail);
            await context.CallActivityAsync(
                nameof(NotificationActivities.SendWelcomeEmailAsync),
                new WelcomeEmailData
                {
                    OrganizationId = orgId,
                    ContactEmail = input.ContactEmail,
                    ContactFirstName = input.ContactFirstName,
                    ContactLastName = input.ContactLastName,
                    OrganizationName = input.Name
                });
            result.StepsCompleted.Add("Welcome Email Sent");
            
            // Step 4: Notify Admins
            logger.LogInformation("Notifying admins about new organization");
            await context.CallActivityAsync(
                nameof(NotificationActivities.NotifyAdminsNewOrganizationAsync),
                new AdminNotificationData
                {
                    OrganizationId = orgId,
                    OrganizationName = input.Name,
                    ContactEmail = input.ContactEmail,
                    ContactFirstName = input.ContactFirstName,
                    ContactLastName = input.ContactLastName
                });
            result.StepsCompleted.Add("Admin Notified");
            
            // Step 5: Log Audit Event
            await context.CallActivityAsync(
                nameof(AuditActivities.LogOnboardingEventAsync),
                new AuditEventData
                {
                    OrganizationId = orgId,
                    EventType = "OrganizationOnboarded",
                    InitiatedBy = input.InitiatedBy,
                    Data = input
                });
            result.StepsCompleted.Add("Audit Logged");
            
            result.Success = true;
            result.CompletedAt = DateTime.UtcNow;
            logger.LogInformation("Organization onboarding completed successfully");
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Organization onboarding saga failed");
            result.Success = false;
            result.FailureReason = ex.Message;
            result.CompletedAt = DateTime.UtcNow;
            
            // Trigger compensation workflow
            if (result.OrganizationId != Guid.Empty)
            {
                await context.CallActivityAsync(
                    nameof(OrganizationActivities.DeleteOrganizationAsync),
                    result.OrganizationId);
                result.StepsCompleted.Add("Organization Deleted (Exception Compensation)");
            }
            
            return result;
        }
    }
}
```

**Create Activities:**

`Activities/OrganizationActivities.cs`:
```csharp
using Dapr.Client;
using DebtManager.Sagas.Models.Requests;

namespace DebtManager.Sagas.Activities;

public class OrganizationActivities
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<OrganizationActivities> _logger;
    
    public OrganizationActivities(DaprClient daprClient, ILogger<OrganizationActivities> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }
    
    public async Task<Guid> CreateOrganizationAsync(OnboardingRequest request)
    {
        _logger.LogInformation("Creating organization: {Name}", request.Name);
        
        // Call existing monolith API or new Organization Service via Dapr
        var response = await _daprClient.InvokeMethodAsync<OnboardingRequest, CreateOrganizationResponse>(
            HttpMethod.Post,
            "debtmanager-web",  // App ID of existing monolith
            "api/internal/organizations/create",
            request);
        
        _logger.LogInformation("Organization created with ID: {OrgId}", response.OrganizationId);
        return response.OrganizationId;
    }
    
    public async Task DeleteOrganizationAsync(Guid organizationId)
    {
        _logger.LogWarning("Compensating: Deleting organization {OrgId}", organizationId);
        
        await _daprClient.InvokeMethodAsync(
            HttpMethod.Delete,
            "debtmanager-web",
            $"api/internal/organizations/{organizationId}");
        
        _logger.LogInformation("Organization {OrgId} deleted", organizationId);
    }
}

public record CreateOrganizationResponse
{
    public Guid OrganizationId { get; init; }
}
```

`Activities/ValidationActivities.cs`:
```csharp
using Dapr.Client;

namespace DebtManager.Sagas.Activities;

public class ValidationActivities
{
    private readonly DaprClient _daprClient;
    private readonly ILogger<ValidationActivities> _logger;
    
    public ValidationActivities(DaprClient daprClient, ILogger<ValidationActivities> logger)
    {
        _daprClient = daprClient;
        _logger = logger;
    }
    
    public async Task<ValidationResult> ValidateAbnAsync(string abn)
    {
        _logger.LogInformation("Validating ABN: {ABN}", abn);
        
        // Call ABR validation service
        var response = await _daprClient.InvokeMethodAsync<string, ValidationResult>(
            HttpMethod.Post,
            "abr-validation-service",
            $"api/validate/abn/{abn}",
            abn);
        
        _logger.LogInformation("ABN validation result: {IsValid}", response.IsValid);
        return response;
    }
}

public record ValidationResult
{
    public bool IsValid { get; init; }
    public string? FailureReason { get; init; }
    public Dictionary<string, string> Details { get; init; } = new();
}
```

**Continue in next document...**
