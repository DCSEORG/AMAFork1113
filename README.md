# App-Mod-Assist

A modern Azure-native expense management application demonstrating how GitHub Copilot can transform legacy application screenshots and database schemas into cloud-ready solutions.

## 🚀 Quick Start

### Prerequisites

- Azure subscription with appropriate permissions
- Azure CLI installed and configured
- .NET 8.0 SDK (for local development)
- Git

### Deployment Steps

1. **Clone the Repository**
   ```bash
   git clone <your-fork-url>
   cd AMAFork1113
   ```

2. **Login to Azure**
   ```bash
   az login
   az account set --subscription <your-subscription-id>
   ```

3. **Deploy the Application**
   ```bash
   chmod +x deploy.sh
   ./deploy.sh
   ```

4. **Access the Application**
   
   After deployment completes, the script will display your application URL:
   ```
   Application URL: https://<your-app>.azurewebsites.net/Index
   ```
   
   ⚠️ **Important**: Navigate to `/Index` (not just the root URL) to access the application.

## 📋 Features

- **Modern UI**: Clean, responsive interface built with ASP.NET Core Razor Pages
- **Expense Management**: Create, submit, and track expenses
- **Approval Workflow**: Managers can approve or reject submitted expenses
- **REST APIs**: Full API support with Swagger documentation at `/api-docs`
- **AI Assistant** (Optional): Natural language interface for expense queries
- **Database Fallback**: Displays dummy data if database connection fails
- **Managed Identity**: Secure, passwordless authentication to Azure SQL

## 🎨 Application Screenshots

Modern UI screenshots are available in the `/Modern-Screenshots` folder (generated during development).

## 🤖 AI Chat UI (Optional Feature)

The application includes an optional AI-powered chat interface using Azure OpenAI.

### Enable Chat UI

By default, Chat UI is **disabled** to reduce deployment costs. To enable it:

1. Edit `deploy.sh`:
   ```bash
   INCLUDE_CHAT_UI=true  # Change from false to true
   ```

2. Run deployment:
   ```bash
   ./deploy.sh
   ```

This will deploy:
- Azure OpenAI with GPT-4o model (in Sweden)
- Azure AI Search for RAG (Retrieval-Augmented Generation)
- Chat interface at `/Chat`

### Deployment Without Chat UI (Default)

The default deployment is optimized for cost:
```bash
./deploy.sh  # INCLUDE_CHAT_UI defaults to false
```

This deploys:
- App Service with web application
- Azure SQL Database
- User-Assigned Managed Identity

The chat UI pages remain in the app but display a message that the feature is not enabled.

## 🏗️ Architecture

See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed architecture diagrams and component descriptions.

### Key Components

- **App Service**: Hosts the ASP.NET Core web application (B1 SKU)
- **Azure SQL Database**: Stores expense data
- **Managed Identity**: Provides secure, passwordless authentication
- **Azure OpenAI** (Optional): Powers the AI chat assistant
- **Azure AI Search** (Optional): Enables RAG for contextual AI responses

## 📚 API Documentation

Once deployed, access the interactive API documentation at:
```
https://<your-app>.azurewebsites.net/api-docs
```

### Available Endpoints

- `GET /api/expenses` - List all expenses
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `DELETE /api/expenses/{id}` - Delete expense
- `POST /api/expenses/{id}/submit` - Submit for approval
- `POST /api/expenses/{id}/approve` - Approve/reject expense
- `GET /api/users` - List users
- `GET /api/categories` - List categories
- `POST /api/chat` - Chat with AI assistant (if enabled)

## 🗄️ Database Schema

The application uses the schema defined in `/Database-Schema/database_schema.sql`:

- **Users**: Employee and manager information
- **Roles**: Employee and Manager roles
- **Expenses**: Expense records with amounts in minor units (pence)
- **ExpenseCategories**: Travel, Meals, Supplies, Accommodation, Other
- **ExpenseStatus**: Draft, Submitted, Approved, Rejected

## 🔧 Local Development

### Build and Run Locally

```bash
cd app
dotnet restore
dotnet build
dotnet run
```

Access at: `https://localhost:5001/Index`

### Environment Variables

Configure in `appsettings.json` or App Service Configuration:

- `ConnectionStrings__DefaultConnection`: Azure SQL connection string
- `ManagedIdentityClientId`: Client ID of the managed identity
- `EnableChatUI`: true/false - Enable chat features
- `OpenAI__Endpoint`: Azure OpenAI endpoint (if chat enabled)
- `OpenAI__DeploymentName`: Model deployment name (e.g., gpt-4o)

## 📝 Configuration Files

- `deploy.sh` - Main deployment script
- `infrastructure/main.bicep` - Main Bicep orchestration file
- `infrastructure/app-service.bicep` - App Service and Managed Identity
- `infrastructure/genai.bicep` - Azure OpenAI and AI Search (optional)
- `run-sql.py` - Python script for database setup
- `GenAISettings.md` - GenAI configuration documentation

## 🔐 Security

- HTTPS enforced on all endpoints
- Managed Identity for Azure SQL authentication
- No passwords stored in connection strings
- SQL injection protection through parameterized queries
- Azure security best practices implemented

See [GenAISettings.md](GenAISettings.md) for GenAI-specific security configuration.

## 💰 Cost Optimization

Default deployment uses low-cost tiers suitable for development and testing:

- **App Service**: B1 (Basic) - ~£40/month
- **Azure SQL**: Serverless - Pay per use
- **Managed Identity**: Free
- **With Chat UI**: +£40-50/month for OpenAI S0 and AI Search Basic

## 🛠️ Troubleshooting

### Database Connection Issues

If you see "Database connection failed" messages:
- The app will automatically display dummy data
- Check that the managed identity has proper SQL permissions
- Verify the connection string in App Service configuration
- Run `python3 run-sql.py` to set up database permissions

### Chat UI Not Working

If chat UI shows "not enabled":
- Verify `INCLUDE_CHAT_UI=true` in deploy.sh
- Redeploy with `./deploy.sh`
- Check OpenAI endpoint configuration in App Service settings

### Deployment Failures

- Ensure you're logged into Azure CLI: `az login`
- Verify subscription permissions
- Check resource name availability (must be globally unique)

## 📖 Additional Resources

- [Azure App Service Documentation](https://docs.microsoft.com/azure/app-service/)
- [Azure OpenAI Documentation](https://docs.microsoft.com/azure/ai-services/openai/)
- [Managed Identity Documentation](https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/)

## 🤝 Contributing

This repo is meant to demonstrate app modernization capabilities. When testing:

1. **Fork the repo** (don't work directly on main)
2. **Rename your fork** to avoid confusion (e.g., "AMA-Test-Nov20")
3. Test your changes in the fork
4. Submit PRs with improvements

## ⚠️ Important Notes

- This is a **proof-of-concept** for workshops and demonstrations
- Not production-ready without additional security hardening
- See PRODUCTION_CONSIDERATIONS (if exists) for production deployment guidance
- Always review and test infrastructure changes before deploying

## 📄 License

See [LICENSE](LICENSE) file for details.

---

**Note**: Navigate to the application at `https://<your-app>.azurewebsites.net/Index` (include `/Index` in the URL)
