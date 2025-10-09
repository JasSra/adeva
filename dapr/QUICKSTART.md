# Quick Start Guide - Dapr Saga Orchestration POC

This guide walks you through setting up and running the Dapr Saga Orchestration POC locally.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop)
- [Dapr CLI](https://docs.dapr.io/getting-started/install-dapr-cli/)

## Step 1: Install Dapr

### Windows (PowerShell as Administrator)
```powershell
powershell -Command "iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex"
```

### macOS
```bash
brew install dapr/tap/dapr-cli
```

### Linux
```bash
wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | /bin/bash
```

### Verify Installation
```bash
dapr --version
```

Expected output: `CLI version: 1.13.0` (or later)

## Step 2: Initialize Dapr

This installs the Dapr runtime, Redis, and Zipkin locally:

```bash
dapr init
```

Expected output:
```
Making the jump to hyperspace...
✅  Success! Dapr is up and running. To get started, check out https://aka.ms/dapr-getting-started
```

Verify components are running:
```bash
docker ps
```

You should see:
- `dapr_redis`
- `dapr_placement`
- `dapr_zipkin`

## Step 3: Start Infrastructure Services

From the repository root:

```bash
cd dapr
docker-compose up -d
```

This starts:
- Redis (state store and pub/sub)
- Zipkin (distributed tracing)
- Dapr Placement Service (for workflows)

Verify services:
```bash
docker-compose ps
```

## Step 4: Run the Dapr Dashboard

In a new terminal:

```bash
dapr dashboard -p 8080
```

Access the dashboard at: http://localhost:8080

The dashboard shows:
- Running Dapr applications
- Components (pub/sub, state stores, etc.)
- Logs and traces

## Step 5: Test Components

### Test State Store

Save state:
```bash
curl -X POST http://localhost:3500/v1.0/state/saga-statestore \
  -H "Content-Type: application/json" \
  -d '[
    {
      "key": "test-saga-1",
      "value": {
        "sagaId": "test-saga-1",
        "status": "running",
        "startedAt": "2025-01-01T00:00:00Z"
      }
    }
  ]'
```

Retrieve state:
```bash
curl http://localhost:3500/v1.0/state/saga-statestore/test-saga-1
```

### Test Pub/Sub

Publish a message:
```bash
curl -X POST http://localhost:3500/v1.0/publish/saga-pubsub/organization.created \
  -H "Content-Type: application/json" \
  -d '{
    "organizationId": "123e4567-e89b-12d3-a456-426614174000",
    "name": "Test Organization",
    "createdAt": "2025-01-01T00:00:00Z"
  }'
```

## Step 6: Example - Simple Saga Workflow

### Create a Simple Console App

This example demonstrates the workflow pattern without requiring a full microservices setup.

```bash
# Create a new console app
dotnet new console -n DaprSagaExample -o /tmp/DaprSagaExample
cd /tmp/DaprSagaExample

# Add Dapr Workflow SDK
dotnet add package Dapr.Workflow
dotnet add package Dapr.Client
```

### Implement a Simple Workflow

Create `OnboardingWorkflow.cs`:

```csharp
using Dapr.Workflow;
using Microsoft.Extensions.Logging;

namespace DaprSagaExample;

public class OnboardingWorkflow : Workflow<OnboardingInput, OnboardingOutput>
{
    public override async Task<OnboardingOutput> RunAsync(
        WorkflowContext context,
        OnboardingInput input)
    {
        var logger = context.CreateReplaySafeLogger<OnboardingWorkflow>();
        var output = new OnboardingOutput { SagaId = context.InstanceId };

        try
        {
            // Step 1: Validate organization
            logger.LogInformation("Validating organization: {Name}", input.OrganizationName);
            var isValid = await context.CallActivityAsync<bool>(
                nameof(ValidationActivity),
                input.OrganizationName);

            if (!isValid)
            {
                output.Success = false;
                output.Message = "Validation failed";
                return output;
            }

            // Step 2: Create organization
            logger.LogInformation("Creating organization: {Name}", input.OrganizationName);
            var orgId = await context.CallActivityAsync<string>(
                nameof(CreateOrganizationActivity),
                input.OrganizationName);
            output.OrganizationId = orgId;

            // Step 3: Send notification
            logger.LogInformation("Sending notification for organization: {OrgId}", orgId);
            await context.CallActivityAsync(
                nameof(SendNotificationActivity),
                orgId);

            output.Success = true;
            output.Message = "Onboarding completed successfully";
            return output;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Workflow failed");
            output.Success = false;
            output.Message = ex.Message;
            return output;
        }
    }
}

// Activity methods
public class ValidationActivity
{
    private readonly ILogger<ValidationActivity> _logger;

    public ValidationActivity(ILogger<ValidationActivity> logger)
    {
        _logger = logger;
    }

    public Task<bool> RunAsync(string organizationName)
    {
        _logger.LogInformation("Validating: {Name}", organizationName);
        // Simulate validation
        return Task.FromResult(!string.IsNullOrWhiteSpace(organizationName));
    }
}

public class CreateOrganizationActivity
{
    private readonly ILogger<CreateOrganizationActivity> _logger;

    public CreateOrganizationActivity(ILogger<CreateOrganizationActivity> logger)
    {
        _logger = logger;
    }

    public Task<string> RunAsync(string organizationName)
    {
        _logger.LogInformation("Creating organization: {Name}", organizationName);
        // Simulate organization creation
        var orgId = Guid.NewGuid().ToString();
        return Task.FromResult(orgId);
    }
}

public class SendNotificationActivity
{
    private readonly ILogger<SendNotificationActivity> _logger;

    public SendNotificationActivity(ILogger<SendNotificationActivity> logger)
    {
        _logger = logger;
    }

    public Task RunAsync(string organizationId)
    {
        _logger.LogInformation("Sending notification for org: {OrgId}", organizationId);
        // Simulate notification
        return Task.CompletedTask;
    }
}

public record OnboardingInput(string OrganizationName);

public record OnboardingOutput
{
    public string SagaId { get; init; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

### Update Program.cs

```csharp
using Dapr.Workflow;
using DaprSagaExample;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddDaprWorkflow(options =>
        {
            // Register workflow
            options.RegisterWorkflow<OnboardingWorkflow>();

            // Register activities
            options.RegisterActivity<ValidationActivity>();
            options.RegisterActivity<CreateOrganizationActivity>();
            options.RegisterActivity<SendNotificationActivity>();
        });

        services.AddDaprClient();
        
        // Register activity classes
        services.AddSingleton<ValidationActivity>();
        services.AddSingleton<CreateOrganizationActivity>();
        services.AddSingleton<SendNotificationActivity>();

        services.AddHostedService<WorkflowTriggerService>();
    });

var host = builder.Build();
await host.RunAsync();

// Service to trigger the workflow
public class WorkflowTriggerService : BackgroundService
{
    private readonly DaprWorkflowClient _workflowClient;
    private readonly ILogger<WorkflowTriggerService> _logger;

    public WorkflowTriggerService(
        DaprWorkflowClient workflowClient,
        ILogger<WorkflowTriggerService> logger)
    {
        _workflowClient = workflowClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait for Dapr to be ready
        await Task.Delay(5000, stoppingToken);

        _logger.LogInformation("Starting onboarding workflow...");

        var input = new OnboardingInput("Acme Corporation");
        var instanceId = $"onboarding-{Guid.NewGuid()}";

        await _workflowClient.ScheduleNewWorkflowAsync(
            nameof(OnboardingWorkflow),
            instanceId,
            input);

        _logger.LogInformation("Workflow started with ID: {InstanceId}", instanceId);

        // Wait for workflow to complete
        await Task.Delay(2000, stoppingToken);

        var state = await _workflowClient.GetWorkflowStateAsync(instanceId);
        var result = state?.ReadOutputAs<OnboardingOutput>();

        _logger.LogInformation("Workflow completed: Success={Success}, Message={Message}, OrgId={OrgId}",
            result?.Success, result?.Message, result?.OrganizationId);

        // Keep running to maintain Dapr connection
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}
```

### Run with Dapr

```bash
dapr run \
  --app-id onboarding-saga \
  --dapr-http-port 3500 \
  --dapr-grpc-port 50001 \
  --components-path /path/to/adeva/dapr/components \
  -- dotnet run
```

Expected output:
```
Starting onboarding workflow...
Workflow started with ID: onboarding-xxxxx
Validating organization: Acme Corporation
Creating organization: Acme Corporation
Sending notification for org: xxxxx
Workflow completed: Success=True, Message=Onboarding completed successfully, OrgId=xxxxx
```

## Step 7: View Traces in Zipkin

1. Open Zipkin: http://localhost:9411
2. Click "Run Query"
3. You'll see traces for:
   - Workflow execution
   - Activity calls
   - State operations

## Step 8: Inspect State in Redis

```bash
# Connect to Redis
docker exec -it dapr_redis redis-cli

# List all keys
KEYS *

# Get workflow state
GET "saga||onboarding-xxxxx"
```

## Step 9: Monitor in Dapr Dashboard

1. Open Dapr Dashboard: http://localhost:8080
2. Go to "Applications" → Click on "onboarding-saga"
3. View:
   - Configuration
   - Logs
   - Metrics

## Next Steps

### 1. Implement Compensation

Add rollback logic to handle failures:

```csharp
catch (Exception ex)
{
    logger.LogError(ex, "Workflow failed, starting compensation");
    
    if (!string.IsNullOrEmpty(output.OrganizationId))
    {
        // Rollback: Delete organization
        await context.CallActivityAsync(
            nameof(DeleteOrganizationActivity),
            output.OrganizationId);
    }
    
    output.Success = false;
    output.Message = ex.Message;
    return output;
}
```

### 2. Add HTTP API

Create a REST API to trigger and monitor workflows:

```bash
dotnet new webapi -n DaprSagaApi
cd DaprSagaApi
dotnet add package Dapr.Workflow
dotnet add package Dapr.AspNetCore
```

### 3. Implement Real Services

Extract services from the monolith:
- Organization Service
- Debt Service
- Payment Service
- Notification Service

### 4. Deploy to Azure

Use Azure Container Apps with Dapr integration (see `deploy/bicep/` examples)

## Troubleshooting

### Issue: "Dapr placement service not found"

**Solution**: Ensure placement service is running:
```bash
docker ps | grep placement
```

Restart if needed:
```bash
dapr init --reset
dapr init
```

### Issue: "Component not found"

**Solution**: Verify components path:
```bash
ls -la /path/to/adeva/dapr/components
```

Ensure the path is correct in `dapr run` command.

### Issue: "Redis connection refused"

**Solution**: Start Redis:
```bash
cd dapr
docker-compose up -d redis
```

### Issue: "Workflow not starting"

**Solution**: Check Dapr logs:
```bash
dapr logs --app-id onboarding-saga
```

## Cleanup

Stop all Dapr services:

```bash
# Stop Docker Compose services
cd dapr
docker-compose down

# Uninstall Dapr
dapr uninstall
```

## Additional Resources

- [Dapr Workflows Documentation](https://docs.dapr.io/developing-applications/building-blocks/workflow/)
- [Full POC Documentation](../docs/DAPR_SAGA_POC.md)
- [Component Reference](https://docs.dapr.io/reference/components-reference/)
- [Dapr SDK for .NET](https://docs.dapr.io/developing-applications/sdks/dotnet/)

## Support

For questions or issues:
1. Check the [Dapr Discord](https://discord.gg/ptHhX6jc34)
2. Review [Dapr GitHub Discussions](https://github.com/dapr/dapr/discussions)
3. Consult the team Slack channel
