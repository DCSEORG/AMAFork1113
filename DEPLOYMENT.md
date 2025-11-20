# Deployment Guide - Expense Management Application

This guide provides step-by-step instructions for deploying the modernized expense management application to Azure.

## Prerequisites

Before you begin, ensure you have:

- ✅ Azure subscription with permissions to create resources
- ✅ Azure CLI installed (version 2.50.0 or later)
- ✅ Git installed
- ✅ Python 3.x installed (for database setup)
- ✅ Bash shell (Linux, macOS, or WSL on Windows)

## Quick Deployment (Default - Without Chat UI)

The default deployment is optimized for cost and deploys only the core functionality:

```bash
# 1. Clone the repository
git clone <your-repo-url>
cd AMAFork1113

# 2. Login to Azure
az login
az account set --subscription <your-subscription-id>

# 3. Deploy (Chat UI disabled by default)
chmod +x deploy.sh
./deploy.sh
```

**Expected Time**: 10-15 minutes

**What Gets Deployed**:
- App Service (B1 SKU) - ~£40/month
- Azure SQL Database (Serverless, pay-per-use)
- User-Assigned Managed Identity (Free)

## Full Deployment (With AI Chat UI)

To deploy with AI chat capabilities:

```bash
# 1. Clone and login (same as above)
git clone <your-repo-url>
cd AMAFork1113
az login

# 2. Edit deploy.sh
nano deploy.sh  # or your preferred editor

# Change this line:
INCLUDE_CHAT_UI=false
# to:
INCLUDE_CHAT_UI=true

# Save and exit

# 3. Deploy
./deploy.sh
```

**Expected Time**: 15-20 minutes

**Additional Resources Deployed**:
- Azure OpenAI (S0 SKU, Sweden) - ~£40/month
- Azure AI Search (Basic) - ~£10/month
- Total additional cost: ~£50/month

## Post-Deployment

### Access Your Application

After successful deployment, you'll see output like:

```
====================================================================
DEPLOYMENT COMPLETED SUCCESSFULLY!
====================================================================

Application URL: https://app-expense-mgmt-1234567890.azurewebsites.net/Index
Resource Group: rg-expense-mgmt-app
Managed Identity: mid-AppModAssist-20-11-30

⚠️  IMPORTANT: Navigate to https://app-expense-mgmt-1234567890.azurewebsites.net/Index
====================================================================
```

### Explore the Application

1. **Dashboard**: `https://<your-app>.azurewebsites.net/Index`
   - View expense statistics
   - See recent expenses
   - Quick access to all features

2. **Expenses List**: Click "Expenses" in navigation
   - Filter by employee or status
   - View all expenses in a table

3. **Create Expense**: Click "New Expense"
   - Fill in expense details
   - Submit for approval

4. **Approvals**: Click "Approvals"
   - View pending submissions
   - Approve or reject expenses

5. **AI Assistant** (if enabled): Click "AI Assistant"
   - Ask questions about expenses
   - Natural language queries
   - Context-aware responses

6. **API Documentation**: Navigate to `/api-docs`
   - Interactive API testing
   - Full endpoint documentation
   - Try out API calls

## Configuration

### Database Connection

The application automatically configures the database connection using Managed Identity.
Connection string format:
```
Server=tcp:sql-expense-mgmt-xyz.database.windows.net,1433;
Initial Catalog=ExpenseManagementDB;
Encrypt=True;
TrustServerCertificate=False;
Connection Timeout=30;
Authentication=Active Directory Managed Identity;
User Id=<managed-identity-client-id>;
```

### App Service Settings

Key environment variables (automatically configured):
- `ConnectionStrings__DefaultConnection`: SQL connection string
- `ManagedIdentityClientId`: Client ID for SQL authentication
- `EnableChatUI`: true/false for chat features
- `OpenAI__Endpoint`: Azure OpenAI endpoint (if enabled)
- `OpenAI__DeploymentName`: GPT-4o deployment name (if enabled)

### Managed Identity Permissions

The Python script (`run-sql.py`) automatically sets up database permissions:
```sql
CREATE USER [mid-AppModAssist-XX-XX-XX] FROM EXTERNAL PROVIDER;
ALTER ROLE db_datareader ADD MEMBER [mid-AppModAssist-XX-XX-XX];
ALTER ROLE db_datawriter ADD MEMBER [mid-AppModAssist-XX-XX-XX];
```

## Troubleshooting

### Issue: "Database connection failed"

**Symptom**: Yellow warning banner appears, showing dummy data

**Solutions**:
1. Wait 2-3 minutes for SQL Database to fully start
2. Check managed identity permissions:
   ```bash
   cd AMAFork1113
   python3 run-sql.py
   ```
3. Verify SQL server firewall allows Azure services
4. Check App Service configuration has correct connection string

### Issue: "Chat UI not enabled"

**Symptom**: Chat page shows "not enabled" message

**Solutions**:
1. Verify `INCLUDE_CHAT_UI=true` in deploy.sh
2. Redeploy: `./deploy.sh`
3. Check OpenAI resources were created in Azure Portal
4. Verify App Service has OpenAI endpoint configuration

### Issue: Deployment fails

**Common causes**:
1. **Not logged in**: Run `az login`
2. **Wrong subscription**: Run `az account set --subscription <id>`
3. **Name conflicts**: Resource names must be globally unique
4. **Permissions**: Ensure you have contributor access

### Issue: APIs return 500 errors

**Solutions**:
1. Check App Service logs in Azure Portal
2. Verify managed identity is assigned to App Service
3. Restart App Service: `az webapp restart --name <app-name> --resource-group rg-expense-mgmt-app`

## Testing the Application

### Test Expense Workflow

1. **Create a Draft Expense**:
   - Navigate to "New Expense"
   - Fill in details (use dummy user "Alice Example")
   - Click "Create Expense"

2. **View Dashboard**:
   - Go to Dashboard
   - Verify expense appears in "Recent Expenses"
   - Check stats are updated

3. **Submit for Approval**:
   - Go to "Expenses" list
   - (Manual API call or update to add submit button on UI)

4. **Approve Expense**:
   - Go to "Approvals"
   - Click "Approve" on a submitted expense
   - Verify status changes to "Approved"

### Test API Endpoints

1. Navigate to `/api-docs`
2. Try these endpoints:
   - `GET /api/expenses` - List all expenses
   - `GET /api/users` - List users
   - `GET /api/categories` - List categories
   - `POST /api/expenses` - Create new expense

### Test Chat UI (if enabled)

1. Navigate to "AI Assistant"
2. Try these queries:
   - "How many expenses do I have?"
   - "What are my pending expenses?"
   - "Show me expenses over £20"
   - "What categories can I use?"

## Updating the Application

To update the application code:

```bash
# 1. Make code changes in /app folder
cd app
# ... make your changes ...

# 2. Rebuild and redeploy
cd ..
dotnet publish -c Release -o publish
cd publish
zip -r ../app.zip .
cd ..

# 3. Deploy updated package
az webapp deploy \
  --resource-group rg-expense-mgmt-app \
  --name <your-app-name> \
  --src-path ./app.zip
```

## Cleanup

To remove all deployed resources:

```bash
az group delete --name rg-expense-mgmt-app --yes --no-wait
```

This deletes:
- App Service and Plan
- Managed Identity
- Azure OpenAI (if deployed)
- Azure AI Search (if deployed)
- All other resources in the resource group

**Note**: Azure SQL Database may be in a different resource group if you're using an existing one.

## Cost Management

### Daily Cost Estimates

**Default Deployment (without Chat UI)**:
- App Service B1: ~£1.30/day
- Azure SQL Serverless: ~£0.50/day (varies with usage)
- Total: ~£1.80/day (~£54/month)

**With Chat UI**:
- Above costs: ~£1.80/day
- Azure OpenAI S0: ~£1.30/day
- Azure AI Search Basic: ~£0.30/day
- Total: ~£3.40/day (~£102/month)

### Cost Optimization Tips

1. **Use Serverless SQL**: Automatically pauses when not in use
2. **Stop App Service**: When not needed for development
3. **Disable Chat UI**: Keep `INCLUDE_CHAT_UI=false` for cost savings
4. **Delete when done**: Remove resources when testing is complete

## Security Considerations

### For Development/Testing (Current Configuration)

✅ HTTPS enforced
✅ Managed Identity (no passwords in connection strings)
✅ SQL Database encryption at rest
✅ Azure AD authentication

### Additional Recommendations for Production

- Enable Application Insights for monitoring
- Configure custom domain with SSL certificate
- Set up Azure Key Vault for secrets management
- Implement authentication and authorization
- Configure network security groups
- Enable diagnostic logging
- Set up backup and disaster recovery
- Implement rate limiting on APIs

## Support and Documentation

- **Architecture**: See [ARCHITECTURE.md](ARCHITECTURE.md)
- **GenAI Settings**: See [GenAISettings.md](GenAISettings.md)
- **Main README**: See [README.md](README.md)
- **Database Schema**: See `Database-Schema/database_schema.sql`
- **Azure Docs**: [https://docs.microsoft.com/azure/](https://docs.microsoft.com/azure/)

## Next Steps

1. ✅ Deploy the application
2. ✅ Test core functionality
3. ✅ Customize for your needs
4. ✅ Add authentication/authorization
5. ✅ Configure monitoring
6. ✅ Plan production migration

---

**Questions or Issues?**

Check the troubleshooting section above or consult the Azure documentation for specific service configurations.
