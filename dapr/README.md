# Dapr Components Configuration

This directory contains Dapr component definitions for the Saga Orchestration POC.

## Overview

Dapr (Distributed Application Runtime) provides building blocks for distributed applications. These components configure:

- **Pub/Sub**: Message bus for inter-service communication
- **State Store**: Persistent storage for saga state
- **Secret Store**: Secure secret management
- **Configuration**: Global Dapr runtime settings

## Components

### 1. Pub/Sub (`pubsub.yaml`)

Handles asynchronous messaging between services.

**Production**: Azure Service Bus Topics
- Reliable, scalable messaging
- Dead-letter queue for failed messages
- Sessions for ordered processing

**Local Development**: Redis Pub/Sub
- Lightweight, easy to run locally
- No cloud dependencies

### 2. State Store (`statestore.yaml`)

Stores saga execution state and workflow data.

**Options**:
- **Redis**: Fast, in-memory state storage (recommended for development and production)
- **Azure Cosmos DB**: Multi-region, globally distributed (for high availability)
- **Azure Table Storage**: Cost-effective (for simpler scenarios)

### 3. Secret Store (`secretstore.yaml`)

Manages sensitive configuration like connection strings and API keys.

**Local Development**: File-based secrets (`secrets.json`)
**Production**: Azure Key Vault (recommended)

### 4. Configuration (`configuration.yaml`)

Global Dapr runtime configuration including tracing, metrics, and feature flags.

## Local Development Setup

### Prerequisites

1. Install Dapr CLI:
   ```bash
   # Windows (PowerShell)
   powershell -Command "iwr -useb https://raw.githubusercontent.com/dapr/cli/master/install/install.ps1 | iex"
   
   # macOS
   brew install dapr/tap/dapr-cli
   
   # Linux
   wget -q https://raw.githubusercontent.com/dapr/cli/master/install/install.sh -O - | /bin/bash
   ```

2. Initialize Dapr:
   ```bash
   dapr init
   ```
   
   This installs:
   - Dapr runtime
   - Redis (for state store and pub/sub)
   - Zipkin (for distributed tracing)

3. Verify installation:
   ```bash
   dapr --version
   ```

### Running with Dapr

Start a service with Dapr sidecar:

```bash
# From repository root
dapr run \
  --app-id saga-orchestrator \
  --app-port 5000 \
  --dapr-http-port 3500 \
  --dapr-grpc-port 50001 \
  --components-path ./dapr/components \
  -- dotnet run --project src/DebtManager.Sagas
```

Access the application:
- App: http://localhost:5000
- Dapr HTTP: http://localhost:3500
- Dapr gRPC: localhost:50001

### Dapr Dashboard

Monitor running services:

```bash
dapr dashboard -p 8080
```

Access at: http://localhost:8080

## Production Deployment

### Azure Service Bus

1. Create Azure Service Bus namespace:
   ```bash
   az servicebus namespace create \
     --name debtmanager-servicebus \
     --resource-group debt-management-rg \
     --location australiaeast \
     --sku Standard
   ```

2. Get connection string:
   ```bash
   az servicebus namespace authorization-rule keys list \
     --resource-group debt-management-rg \
     --namespace-name debtmanager-servicebus \
     --name RootManageSharedAccessKey \
     --query primaryConnectionString -o tsv
   ```

3. Update `secrets.json` or Azure Key Vault with the connection string

### Azure Redis Cache

1. Create Azure Cache for Redis:
   ```bash
   az redis create \
     --name debtmanager-redis \
     --resource-group debt-management-rg \
     --location australiaeast \
     --sku Basic \
     --vm-size c0
   ```

2. Get access key:
   ```bash
   az redis list-keys \
     --name debtmanager-redis \
     --resource-group debt-management-rg \
     --query primaryKey -o tsv
   ```

### Azure Key Vault (Recommended for Production)

1. Create Key Vault:
   ```bash
   az keyvault create \
     --name debtmanager-kv \
     --resource-group debt-management-rg \
     --location australiaeast
   ```

2. Store secrets:
   ```bash
   az keyvault secret set \
     --vault-name debtmanager-kv \
     --name servicebus-connection-string \
     --value "<your-connection-string>"
   
   az keyvault secret set \
     --vault-name debtmanager-kv \
     --name redis-password \
     --value "<your-redis-key>"
   ```

3. Update `secretstore.yaml` to use Azure Key Vault (see commented section)

### Kubernetes Deployment

For Kubernetes, Dapr components are deployed as CRDs:

```bash
# Apply components to Kubernetes
kubectl apply -f dapr/components/

# Verify components
kubectl get components
```

### Azure Container Apps

Azure Container Apps has built-in Dapr support. Components are configured via Bicep/ARM templates (see `deploy/bicep/` for examples).

## Configuration Updates

### Switching Message Brokers

To switch from Azure Service Bus to Kafka:

1. Edit `pubsub.yaml`
2. Change `type` to `pubsub.kafka`
3. Update metadata for Kafka brokers:
   ```yaml
   metadata:
   - name: brokers
     value: "localhost:9092"
   - name: consumerGroup
     value: "debt-management-saga"
   ```

### Switching State Stores

To switch to PostgreSQL state store:

1. Edit `statestore.yaml`
2. Change `type` to `state.postgresql`
3. Update metadata:
   ```yaml
   metadata:
   - name: connectionString
     secretKeyRef:
       name: postgres-connection
       key: value
   ```

## Troubleshooting

### Component Not Loading

Check Dapr logs:
```bash
dapr logs --app-id saga-orchestrator
```

### Connection Issues

Test component connectivity:
```bash
# Test pub/sub
dapr publish --publish-app-id saga-orchestrator --pubsub saga-pubsub --topic test --data '{"test":"data"}'

# Test state store
dapr invoke --app-id saga-orchestrator --method state/saga-statestore --data '{"key":"test","value":"data"}'
```

### Secret Not Found

Verify secrets file:
```bash
cat dapr/secrets.json
```

Ensure the file path in `secretstore.yaml` is correct relative to where Dapr is running.

## Security Notes

⚠️ **IMPORTANT**: 
- **Never commit real secrets** to version control
- `secrets.json` contains placeholder values only
- In production, use Azure Key Vault or Kubernetes Secrets
- Rotate secrets regularly
- Use managed identities when possible

## References

- [Dapr Documentation](https://docs.dapr.io/)
- [Component Specifications](https://docs.dapr.io/reference/components-reference/)
- [Azure Service Bus Component](https://docs.dapr.io/reference/components-reference/supported-pubsub/setup-azure-servicebus/)
- [Redis State Store](https://docs.dapr.io/reference/components-reference/supported-state-stores/setup-redis/)
- [Azure Key Vault Secret Store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/azure-keyvault/)

## Next Steps

1. Review the [Dapr Saga POC documentation](../docs/DAPR_SAGA_POC.md)
2. Set up local development environment
3. Run the example saga orchestrator
4. Deploy to Azure Container Apps (see deployment guide)
