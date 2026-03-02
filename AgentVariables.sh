#!/usr/bin/env bash
# AgentVariables.sh – Shared variables for all deployment scripts.
# prompt-009-create-deployment-scripts
#
# Edit these values before running deploy-infra.sh or deploy-app.sh.
# Do NOT commit secrets to source control.

# ── Azure Resource Configuration ────────────────────────────────────────────
export RESOURCE_GROUP="rg-appmodassist-dev"
export LOCATION="uksouth"

# ── Entra ID / AAD (deployer identity for SQL admin) ────────────────────────
# Run: az ad signed-in-user show --query id -o tsv
export ADMIN_OBJECT_ID="<your-entra-id-object-id>"
# Run: az account show --query user.name -o tsv
export ADMIN_LOGIN="<your-entra-id-upn>"

# ── These are populated automatically by deploy-infra.sh ────────────────────
# You can override them here if deploying app separately after infra.
export SQL_SERVER_FQDN=""
export SQL_DATABASE="Northwind"
export MANAGED_IDENTITY_NAME=""
export AZURE_CLIENT_ID=""
export APP_SERVICE_NAME=""
export APP_SERVICE_URL=""
