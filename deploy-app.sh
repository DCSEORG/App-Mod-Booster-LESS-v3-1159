#!/usr/bin/env bash
# deploy-app.sh – Deploy database objects and application code.
# prompt-009-create-deployment-scripts
#
# Usage: bash deploy-app.sh
# Must be run AFTER deploy-infra.sh (or after populating AgentVariables.sh).

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Load shared variables ────────────────────────────────────────────────────
source "${SCRIPT_DIR}/AgentVariables.sh"

# Validate required variables
if [[ -z "${SQL_SERVER_FQDN}" || "${SQL_SERVER_FQDN}" == '""' ]]; then
    echo "ERROR: SQL_SERVER_FQDN is not set. Run deploy-infra.sh first."
    exit 1
fi
if [[ -z "${APP_SERVICE_NAME}" || "${APP_SERVICE_NAME}" == '""' ]]; then
    echo "ERROR: APP_SERVICE_NAME is not set. Run deploy-infra.sh first."
    exit 1
fi

echo "========================================================"
echo " Expense Management System – Application Deployment"
echo "========================================================"
echo "SQL Server FQDN   : ${SQL_SERVER_FQDN}"
echo "Database          : ${SQL_DATABASE}"
echo "Managed Identity  : ${MANAGED_IDENTITY_NAME}"
echo "App Service       : ${APP_SERVICE_NAME}"
echo ""

# Export for Python scripts
export SQL_SERVER_FQDN
export SQL_DATABASE
export MANAGED_IDENTITY_NAME

# ── 1. Install Python dependencies ───────────────────────────────────────────
echo "[1/6] Installing Python dependencies ..."
pip3 install --quiet pyodbc azure-identity
echo "      ✓ Dependencies installed."

# ── 2. Import database schema ─────────────────────────────────────────────────
echo ""
echo "[2/6] Importing database schema (run-sql.py) ..."
sleep 5   # brief pause to ensure SQL is fully responsive
python3 "${SCRIPT_DIR}/run-sql.py"
echo "      ✓ Schema imported."

# ── 3. Configure database roles ──────────────────────────────────────────────
echo ""
echo "[3/6] Configuring database roles (run-sql-dbrole.py) ..."
python3 "${SCRIPT_DIR}/run-sql-dbrole.py"
echo "      ✓ Database roles configured."

# ── 4. Deploy stored procedures ──────────────────────────────────────────────
echo ""
echo "[4/6] Deploying stored procedures (run-sql-stored-procs.py) ..."
python3 "${SCRIPT_DIR}/run-sql-stored-procs.py"
echo "      ✓ Stored procedures deployed."

# ── 5. Build the application ──────────────────────────────────────────────────
echo ""
echo "[5/6] Building .NET application ..."
cd "${SCRIPT_DIR}/app"
dotnet publish ExpenseManagement.csproj \
    --configuration Release \
    --output        "${SCRIPT_DIR}/app/publish" \
    --runtime       linux-x64 \
    --self-contained false \
    --nologo \
    --verbosity quiet
echo "      ✓ Application built."

# Create app.zip with files at the ZIP root (required by Azure App Service)
cd "${SCRIPT_DIR}/app/publish"
zip -r "${SCRIPT_DIR}/app.zip" . -x "*.pdb"
cd "${SCRIPT_DIR}"
echo "      ✓ app.zip created."

# ── 6. Deploy to App Service ──────────────────────────────────────────────────
echo ""
echo "[6/6] Deploying app.zip to App Service (${APP_SERVICE_NAME}) ..."
echo "      Waiting 30 seconds before deploy ..."
sleep 30

az webapp deploy \
    --resource-group "${RESOURCE_GROUP}" \
    --name           "${APP_SERVICE_NAME}" \
    --src-path       "${SCRIPT_DIR}/app.zip" \
    --type           zip \
    --output         none

echo "      ✓ Application deployed."

echo ""
echo "========================================================"
echo " Deployment complete!"
echo "========================================================"
echo ""
echo " Application URL: ${APP_SERVICE_URL}/Index"
echo ""
echo " NOTE: The app URL is /Index — not the root path."
echo ""
