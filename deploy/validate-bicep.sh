#!/bin/bash
# Local Bicep Template Validation Script
# This script validates Bicep templates without deploying them

set -e

echo "================================"
echo "Bicep Template Validation Script"
echo "================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Check if Azure CLI is installed
if ! command -v az &> /dev/null; then
    echo -e "${RED}Error: Azure CLI is not installed${NC}"
    echo "Install from: https://docs.microsoft.com/cli/azure/install-azure-cli"
    exit 1
fi

echo -e "${GREEN}✓ Azure CLI is installed${NC}"

# Check if logged in to Azure
if ! az account show &> /dev/null; then
    echo -e "${YELLOW}Warning: Not logged in to Azure${NC}"
    echo "Run: az login"
    exit 1
fi

echo -e "${GREEN}✓ Logged in to Azure${NC}"
echo ""

# Navigate to script directory
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
BICEP_DIR="$SCRIPT_DIR/bicep"

cd "$BICEP_DIR"

echo "Validating Bicep templates..."
echo ""

# Validate main.bicep
echo "1. Validating main.bicep..."
if az bicep build --file main.bicep &> /dev/null; then
    echo -e "${GREEN}   ✓ main.bicep is valid${NC}"
else
    echo -e "${RED}   ✗ main.bicep has errors${NC}"
    az bicep build --file main.bicep
    exit 1
fi

# Validate appgateway.bicep
echo "2. Validating appgateway.bicep..."
if az bicep build --file appgateway.bicep &> /dev/null; then
    echo -e "${GREEN}   ✓ appgateway.bicep is valid${NC}"
else
    echo -e "${RED}   ✗ appgateway.bicep has errors${NC}"
    az bicep build --file appgateway.bicep
    exit 1
fi

echo ""
echo "3. Testing what-if deployment (dev environment)..."
echo -e "${YELLOW}   This shows what would be deployed without actually deploying${NC}"
echo ""

# Create temporary parameter file with dummy values for validation
cat > /tmp/test-params.json <<EOF
{
  "\$schema": "https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#",
  "contentVersion": "1.0.0.0",
  "parameters": {
    "environmentName": {
      "value": "dev"
    },
    "tierType": {
      "value": "free"
    },
    "sqlAdminLogin": {
      "value": "sqladmin"
    },
    "sqlAdminPassword": {
      "value": "TestPassword123!"
    },
    "azureAdB2CClientId": {
      "value": "c83c5908-2b64-4304-8c53-b964ace5a1ea"
    },
    "azureAdB2CAuthority": {
      "value": "https://jsraauth.b2clogin.com/jsraauth.onmicrosoft.com/B2C_1_SIGNUP_SIGNIN/v2.0"
    }
  }
}
EOF

# Get default resource group or create a test one
RESOURCE_GROUP="rg-debtmanager-validation"
LOCATION="australiaeast"

echo "   Creating test resource group: $RESOURCE_GROUP"
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" > /dev/null 2>&1 || true

echo "   Running what-if analysis..."
echo ""

if az deployment group what-if \
    --resource-group "$RESOURCE_GROUP" \
    --template-file main.bicep \
    --parameters /tmp/test-params.json \
    --no-pretty-print; then
    echo ""
    echo -e "${GREEN}   ✓ What-if analysis completed successfully${NC}"
else
    echo ""
    echo -e "${RED}   ✗ What-if analysis failed${NC}"
    exit 1
fi

# Clean up temp files
rm /tmp/test-params.json

echo ""
echo "================================"
echo -e "${GREEN}All validations passed!${NC}"
echo "================================"
echo ""
echo "The templates are ready to deploy."
echo ""
echo "To deploy:"
echo "  - Use GitHub Actions workflows, or"
echo "  - Run: az deployment group create --resource-group <rg-name> --template-file main.bicep --parameters <params>"
echo ""
echo "Note: The validation resource group '$RESOURCE_GROUP' was created but is empty."
echo "      You can delete it with: az group delete --name $RESOURCE_GROUP --yes"
echo ""
