#!/usr/bin/env bash
# deploy-infra.sh – Deploy all Azure infrastructure for the Expense Management System.
# prompt-009-create-deployment-scripts
#
# Usage: bash deploy-infra.sh
# Requires: az CLI logged in with sufficient permissions.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Load shared variables ────────────────────────────────────────────────────
source "${SCRIPT_DIR}/AgentVariables.sh"

echo "========================================================"
echo " Expense Management System – Infrastructure Deployment"
echo "========================================================"
echo "Resource Group : ${RESOURCE_GROUP}"
echo "Location       : ${LOCATION}"
echo "Admin Login    : ${ADMIN_LOGIN}"
echo ""

# Validate placeholder values have been replaced
if [[ "${ADMIN_OBJECT_ID}" == "<your-entra-id-object-id>" ]]; then
    echo "ERROR: ADMIN_OBJECT_ID has not been configured in AgentVariables.sh."
    echo "       Run: az ad signed-in-user show --query id -o tsv"
    exit 1
fi
if [[ "${ADMIN_LOGIN}" == "<your-entra-id-upn>" ]]; then
    echo "ERROR: ADMIN_LOGIN has not been configured in AgentVariables.sh."
    echo "       Run: az account show --query user.name -o tsv"
    exit 1
fi

# ── 1. Create resource group ─────────────────────────────────────────────────
echo "[1/5] Creating resource group: ${RESOURCE_GROUP} ..."
az group create \
    --name     "${RESOURCE_GROUP}" \
    --location "${LOCATION}" \
    --output   none
echo "      ✓ Resource group ready."

# ── 2. Deploy Bicep (managed identity + SQL + App Service) ───────────────────
echo ""
echo "[2/5] Deploying Bicep templates (this may take 5-10 minutes) ..."
DEPLOY_OUTPUT=$(az deployment group create \
    --resource-group  "${RESOURCE_GROUP}" \
    --template-file   "${SCRIPT_DIR}/infra/main.bicep" \
    --parameters      adminObjectId="${ADMIN_OBJECT_ID}" \
                      adminLogin="${ADMIN_LOGIN}" \
    --query           "properties.outputs" \
    --output          json)

echo "      ✓ Bicep deployment complete."

# Extract outputs
SQL_SERVER_FQDN=$(echo "${DEPLOY_OUTPUT}"      | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['sqlServerFqdn']['value'])")
SQL_DATABASE=$(echo "${DEPLOY_OUTPUT}"         | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['databaseName']['value'])")
MANAGED_IDENTITY_NAME=$(echo "${DEPLOY_OUTPUT}"| python3 -c "import sys,json; d=json.load(sys.stdin); print(d['managedIdentityName']['value'])")
AZURE_CLIENT_ID=$(echo "${DEPLOY_OUTPUT}"      | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['managedIdentityClientId']['value'])")
APP_SERVICE_NAME=$(echo "${DEPLOY_OUTPUT}"     | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['appServiceName']['value'])")
APP_SERVICE_URL=$(echo "${DEPLOY_OUTPUT}"      | python3 -c "import sys,json; d=json.load(sys.stdin); print(d['appServiceUrl']['value'])")
SQL_SERVER_NAME=$(echo "${SQL_SERVER_FQDN}"    | cut -d'.' -f1)

echo ""
echo "      SQL Server FQDN   : ${SQL_SERVER_FQDN}"
echo "      Managed Identity  : ${MANAGED_IDENTITY_NAME}"
echo "      App Service       : ${APP_SERVICE_NAME}"

# ── 3. Configure App Service environment variables ───────────────────────────
echo ""
echo "[3/5] Configuring App Service settings ..."
az webapp config appsettings set \
    --resource-group "${RESOURCE_GROUP}" \
    --name           "${APP_SERVICE_NAME}" \
    --settings \
        AZURE_CLIENT_ID="${AZURE_CLIENT_ID}" \
        SQL_SERVER_FQDN="${SQL_SERVER_FQDN}" \
        SQL_DATABASE="${SQL_DATABASE}" \
        ASPNETCORE_ENVIRONMENT="Production" \
    --output none
echo "      ✓ App settings configured."

# ── 4. Wait for SQL Server to be ready ───────────────────────────────────────
echo ""
echo "[4/5] Waiting 30 seconds for SQL Server to be fully ready ..."
sleep 30
echo "      ✓ Wait complete."

# ── 5. Configure SQL firewall rules ──────────────────────────────────────────
echo ""
echo "[5/5] Configuring SQL firewall rules ..."

# Allow Azure services (0.0.0.0 → 0.0.0.0)
az sql server firewall-rule create \
    --resource-group  "${RESOURCE_GROUP}" \
    --server          "${SQL_SERVER_NAME}" \
    --name            "AllowAllAzureIPs" \
    --start-ip-address "0.0.0.0" \
    --end-ip-address   "0.0.0.0" \
    --output none

# Allow current deployment machine IP
MY_IP=$(curl -s https://api.ipify.org)
echo "      Allowing deployment IP: ${MY_IP}"
az sql server firewall-rule create \
    --resource-group  "${RESOURCE_GROUP}" \
    --server          "${SQL_SERVER_NAME}" \
    --name            "AllowDeploymentIP" \
    --start-ip-address "${MY_IP}" \
    --end-ip-address   "${MY_IP}" \
    --output none

echo "      ✓ Firewall rules configured."

# ── Write outputs to AgentVariables.sh for use by deploy-app.sh ──────────────
sed -i.bak "s|^export SQL_SERVER_FQDN=.*|export SQL_SERVER_FQDN=\"${SQL_SERVER_FQDN}\"|"     "${SCRIPT_DIR}/AgentVariables.sh" && rm -f "${SCRIPT_DIR}/AgentVariables.sh.bak"
sed -i.bak "s|^export SQL_DATABASE=.*|export SQL_DATABASE=\"${SQL_DATABASE}\"|"               "${SCRIPT_DIR}/AgentVariables.sh" && rm -f "${SCRIPT_DIR}/AgentVariables.sh.bak"
sed -i.bak "s|^export MANAGED_IDENTITY_NAME=.*|export MANAGED_IDENTITY_NAME=\"${MANAGED_IDENTITY_NAME}\"|" "${SCRIPT_DIR}/AgentVariables.sh" && rm -f "${SCRIPT_DIR}/AgentVariables.sh.bak"
sed -i.bak "s|^export AZURE_CLIENT_ID=.*|export AZURE_CLIENT_ID=\"${AZURE_CLIENT_ID}\"|"     "${SCRIPT_DIR}/AgentVariables.sh" && rm -f "${SCRIPT_DIR}/AgentVariables.sh.bak"
sed -i.bak "s|^export APP_SERVICE_NAME=.*|export APP_SERVICE_NAME=\"${APP_SERVICE_NAME}\"|"   "${SCRIPT_DIR}/AgentVariables.sh" && rm -f "${SCRIPT_DIR}/AgentVariables.sh.bak"
sed -i.bak "s|^export APP_SERVICE_URL=.*|export APP_SERVICE_URL=\"${APP_SERVICE_URL}\"|"     "${SCRIPT_DIR}/AgentVariables.sh" && rm -f "${SCRIPT_DIR}/AgentVariables.sh.bak"

echo ""
echo "========================================================"
echo " Infrastructure deployment complete!"
echo "========================================================"
echo ""
echo " Next step: run   bash deploy-app.sh"
echo ""
