#!/bin/bash
#
# deploy.sh - Master deployment script for Expense Management Application
# This script orchestrates the deployment of all Azure resources and the application
#

set -e  # Exit on error

# ============================================================================
# CONFIGURATION VARIABLES
# ============================================================================

# Resource Group Configuration
RESOURCE_GROUP="rg-expense-mgmt-app"
LOCATION="uksouth"

# Application Configuration
APP_NAME="app-expense-mgmt-$(date +%s)"
SQL_SERVER="sql-expense-mgmt-xyz"
SQL_DATABASE="ExpenseManagementDB"

# Managed Identity (using timestamp format: Day-Hour-Minute)
MANAGED_IDENTITY_NAME="mid-AppModAssist-$(date +%d-%H-%M)"

# GenAI Configuration
INCLUDE_CHAT_UI=false  # Set to true to deploy Azure OpenAI and chat UI resources
OPENAI_LOCATION="swedencentral"  # GPT-4o is available in Sweden

# ============================================================================
# FUNCTIONS
# ============================================================================

log_info() {
    echo "ℹ️  $1"
}

log_success() {
    echo "✅ $1"
}

log_error() {
    echo "❌ $1"
    exit 1
}

# ============================================================================
# MAIN DEPLOYMENT
# ============================================================================

log_info "Starting deployment of Expense Management Application..."
log_info "Resource Group: $RESOURCE_GROUP"
log_info "Location: $LOCATION"
log_info "Chat UI Enabled: $INCLUDE_CHAT_UI"

# Check if user is logged in to Azure
log_info "Checking Azure login status..."
if ! az account show &> /dev/null; then
    log_error "Not logged in to Azure. Please run 'az login' first."
fi

SUBSCRIPTION_ID=$(az account show --query id -o tsv)
log_success "Logged in to Azure (Subscription: $SUBSCRIPTION_ID)"

# Create resource group
log_info "Creating resource group $RESOURCE_GROUP..."
az group create \
    --name "$RESOURCE_GROUP" \
    --location "$LOCATION" \
    --output none

log_success "Resource group created"

# Deploy infrastructure using Bicep
log_info "Deploying Azure infrastructure..."
az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file ./infrastructure/main.bicep \
    --parameters appName="$APP_NAME" \
    --parameters location="$LOCATION" \
    --parameters sqlServerName="$SQL_SERVER" \
    --parameters sqlDatabaseName="$SQL_DATABASE" \
    --parameters managedIdentityName="$MANAGED_IDENTITY_NAME" \
    --parameters includeChatUI="$INCLUDE_CHAT_UI" \
    --parameters openAILocation="$OPENAI_LOCATION" \
    --output none

log_success "Infrastructure deployed successfully"

# Get the managed identity client ID
log_info "Retrieving managed identity details..."
MANAGED_IDENTITY_CLIENT_ID=$(az identity show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$MANAGED_IDENTITY_NAME" \
    --query clientId -o tsv)

log_success "Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"

# Install required Python packages and run SQL setup
log_info "Setting up database permissions..."
pip3 install --quiet pyodbc azure-identity

# Create script.sql with the managed identity
cat > script.sql << EOF
-- Drop and recreate the managed identity user with correct permissions
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = '$MANAGED_IDENTITY_NAME')
BEGIN
    DROP USER [$MANAGED_IDENTITY_NAME];
END
GO

CREATE USER [$MANAGED_IDENTITY_NAME] FROM EXTERNAL PROVIDER;
GO

ALTER ROLE db_datareader ADD MEMBER [$MANAGED_IDENTITY_NAME];
ALTER ROLE db_datawriter ADD MEMBER [$MANAGED_IDENTITY_NAME];
GO
EOF

# Run the Python script to set up database
python3 run-sql.py

log_success "Database permissions configured"

# Build and deploy the application
log_info "Building application..."
cd app
dotnet restore
dotnet publish -c Release -o ../publish

log_info "Creating deployment package..."
cd ../publish
zip -r ../app.zip . > /dev/null

log_success "Application built and packaged"

# Deploy the application to App Service
log_info "Deploying application to Azure App Service..."
az webapp deploy \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_NAME" \
    --src-path ../app.zip \
    --type zip \
    --output none

log_success "Application deployed successfully"

# Get the App Service URL
APP_URL=$(az webapp show \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_NAME" \
    --query defaultHostName -o tsv)

log_success "======================================================================"
log_success "DEPLOYMENT COMPLETED SUCCESSFULLY!"
log_success "======================================================================"
log_success ""
log_success "Application URL: https://${APP_URL}/Index"
log_success "Resource Group: $RESOURCE_GROUP"
log_success "Managed Identity: $MANAGED_IDENTITY_NAME"
log_success ""
log_success "⚠️  IMPORTANT: Navigate to https://${APP_URL}/Index (not just the root URL)"
log_success ""

if [ "$INCLUDE_CHAT_UI" = true ]; then
    log_success "Chat UI is enabled. Access it at: https://${APP_URL}/Chat"
else
    log_success "Chat UI is disabled. To enable it, set INCLUDE_CHAT_UI=true in deploy.sh"
fi

log_success "======================================================================"
