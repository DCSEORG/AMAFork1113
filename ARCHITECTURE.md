# Azure Services Architecture Diagram

## Expense Management System - Azure Architecture

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Azure Resource Group                            │
│                         (rg-expense-mgmt-app)                            │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────┐    │
│  │                    Azure App Service Plan                       │    │
│  │                       (B1 - Basic Tier)                         │    │
│  │  ┌──────────────────────────────────────────────────────┐      │    │
│  │  │          Azure App Service (Linux)                    │      │    │
│  │  │         ASP.NET Core 8.0 Web App                      │      │    │
│  │  │                                                        │      │    │
│  │  │  ┌─────────────────────────────────────────────┐     │      │    │
│  │  │  │  Razor Pages UI                             │     │      │    │
│  │  │  │  - Dashboard                                │     │      │    │
│  │  │  │  - Expense Management                       │     │      │    │
│  │  │  │  - Approvals                                │     │      │    │
│  │  │  │  - Chat UI (if enabled)                     │     │      │    │
│  │  │  └─────────────────────────────────────────────┘     │      │    │
│  │  │                                                        │      │    │
│  │  │  ┌─────────────────────────────────────────────┐     │      │    │
│  │  │  │  REST APIs + Swagger                        │     │      │    │
│  │  │  │  - /api/expenses                            │     │      │    │
│  │  │  │  - /api/users                               │     │      │    │
│  │  │  │  - /api/categories                          │     │      │    │
│  │  │  │  - /api/chat                                │     │      │    │
│  │  │  └─────────────────────────────────────────────┘     │      │    │
│  │  │                                                        │      │    │
│  │  │  Assigned Identity:                                   │      │    │
│  │  │  [User-Assigned Managed Identity]  ◄─────────────────┼──┐   │    │
│  │  └────────────────────────────┬───────────────────────────┘  │   │    │
│  └───────────────────────────────┼──────────────────────────────┘   │    │
│                                   │                                  │    │
│                                   │ Authenticates with              │    │
│                                   │ Managed Identity                │    │
│                                   ▼                                  │    │
│  ┌─────────────────────────────────────────────────────────────┐   │    │
│  │              Azure SQL Database                              │   │    │
│  │         (sql-expense-mgmt-xyz.database.windows.net)         │   │    │
│  │                                                              │   │    │
│  │  Database: ExpenseManagementDB                              │   │    │
│  │  Authentication: Active Directory Managed Identity          │   │    │
│  │                                                              │   │    │
│  │  Tables:                                                     │   │    │
│  │  - Users                                                     │   │    │
│  │  - Roles                                                     │   │    │
│  │  - Expenses                                                  │   │    │
│  │  - ExpenseCategories                                         │   │    │
│  │  - ExpenseStatus                                             │   │    │
│  └─────────────────────────────────────────────────────────────┘   │    │
│                                                                      │    │
│  ┌─────────────────────────────────────────────────────────────┐   │    │
│  │          User-Assigned Managed Identity                      │   │    │
│  │          (mid-AppModAssist-{timestamp})                      │ ◄─┘    │
│  │                                                              │        │
│  │  Purpose: Authenticate App Service to Azure SQL             │        │
│  │  Permissions: db_datareader, db_datawriter                  │        │
│  └─────────────────────────────────────────────────────────────┘        │
│                                                                          │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │          GenAI Resources (Optional - when INCLUDE_CHAT_UI=true) │   │
│  │                                                                  │   │
│  │  ┌────────────────────────────────────────────────────────┐    │   │
│  │  │        Azure OpenAI (Sweden Central)                    │    │   │
│  │  │        (S0 SKU)                                         │    │   │
│  │  │                                                          │    │   │
│  │  │  Deployment:                                            │    │   │
│  │  │  - Model: GPT-4o                                        │    │   │
│  │  │  - Version: 2024-05-13                                  │    │   │
│  │  │  - Capacity: 10 TPM                                     │    │   │
│  │  └────────────────────────────────────────────────────────┘    │   │
│  │                              ▲                                  │   │
│  │                              │ API Calls                        │   │
│  │                              │                                  │   │
│  │  ┌────────────────────────────────────────────────────────┐    │   │
│  │  │        Azure AI Search (UK South)                       │    │   │
│  │  │        (Basic SKU)                                      │    │   │
│  │  │                                                          │    │   │
│  │  │  Purpose: RAG (Retrieval-Augmented Generation)         │    │   │
│  │  │  - Index expense data                                   │    │   │
│  │  │  - Provide context for AI responses                    │    │   │
│  │  └────────────────────────────────────────────────────────┘    │   │
│  └─────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────┘

                                    ▲
                                    │
                                    │ HTTPS
                                    │
                          ┌─────────┴─────────┐
                          │                   │
                          │   End Users       │
                          │   (Employees &    │
                          │    Managers)      │
                          │                   │
                          └───────────────────┘
```

## Connection Flow

1. **User Access**: Users access the application via HTTPS through the App Service URL
2. **Authentication**: App Service uses User-Assigned Managed Identity for Azure SQL authentication
3. **Data Operations**: App reads/writes expense data from Azure SQL Database
4. **Chat AI (Optional)**: When enabled, chat requests flow to Azure OpenAI with RAG context from AI Search
5. **API Access**: RESTful APIs available at /api/* endpoints with Swagger documentation at /api-docs

## Security Features

- ✅ HTTPS enforced on App Service
- ✅ Managed Identity for SQL authentication (no connection strings with passwords)
- ✅ SQL Database encryption at rest
- ✅ Network security with Azure defaults
- ✅ API keys stored in App Service configuration

## Deployment

Infrastructure deployed using:
- **Bicep**: Infrastructure as Code templates in `/infrastructure` folder
- **Azure CLI**: Deployment orchestrated by `deploy.sh` script
- **Modular**: GenAI resources conditionally deployed based on settings

## Cost Optimization

- **Basic App Service Plan**: Low-cost tier for development
- **S0 Cognitive Services**: Standard tier for OpenAI
- **Basic AI Search**: Minimal cost for RAG functionality
- **Conditional Deployment**: GenAI resources only deployed when needed
