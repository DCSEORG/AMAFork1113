# Azure GenAI Configuration

This file contains the configuration for Azure OpenAI and related GenAI resources.

## Azure OpenAI Configuration

**Endpoint**: Retrieved from deployment output
**Model**: GPT-4o
**Deployment Name**: gpt-4o
**Location**: Sweden Central (swedencentral)
**SKU**: S0 (Standard)

## Azure AI Search Configuration

**Purpose**: Retrieval-Augmented Generation (RAG) for expense data
**SKU**: Basic
**Location**: Same as resource group (UK South)

## Configuration in Application

The GenAI settings are automatically configured during deployment through the Bicep templates.
Settings are passed to the App Service as environment variables:

- `EnableChatUI`: true/false - Controls whether chat UI is accessible
- `OpenAI__Endpoint`: Azure OpenAI endpoint URL
- `OpenAI__DeploymentName`: Name of the deployed GPT-4o model
- `OpenAI__ApiKey`: Azure OpenAI API key (retrieved from Key Vault or App Settings)

## Deployment

To deploy with GenAI resources enabled:
1. Edit `deploy.sh`
2. Set `INCLUDE_CHAT_UI=true`
3. Run `./deploy.sh`

To deploy without GenAI resources (saves cost):
1. Keep `INCLUDE_CHAT_UI=false` (default)
2. Run `./deploy.sh`

The chat UI will still be visible in the app but will show a message that it's not enabled.

## Cost Optimization

- **S0 SKU**: Low-cost tier suitable for development and testing
- **GPT-4o Model**: Latest model with excellent capabilities
- **Conditional Deployment**: Only deployed when needed
- **Basic Search**: Minimal cost for RAG functionality

## RAG (Retrieval-Augmented Generation) Pattern

The chat UI uses RAG to:
1. Query the expense database for relevant context
2. Include expense data in the prompt to Azure OpenAI
3. Generate contextually aware responses about expenses
4. Enable natural language queries about expense data

## Security

- Uses Managed Identity where possible for authentication
- API keys stored securely in App Service configuration
- HTTPS enforced for all communications
- Following Azure best practices for security

## Resources Created (when INCLUDE_CHAT_UI=true)

1. **Azure OpenAI Account**: Cognitive Services account with GPT-4o deployment
2. **Azure AI Search**: Search service for indexing and retrieving expense data
3. **App Service Configuration**: Updated with GenAI endpoint and keys
