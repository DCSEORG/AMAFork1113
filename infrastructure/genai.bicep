// genai.bicep - Azure OpenAI and related GenAI resources
// Creates Azure OpenAI with GPT-4o model in Sweden

@description('Location for OpenAI resources (Sweden Central for GPT-4o)')
param location string = 'swedencentral'

@description('App name for resource naming')
param appName string

@description('Managed Identity ID for access')
param managedIdentityId string

// Azure OpenAI Account
resource openAI 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: 'openai-${appName}'
  location: location
  sku: {
    name: 'S0'  // Standard tier for development
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: 'openai-${appName}'
    publicNetworkAccess: 'Enabled'
  }
}

// Deploy GPT-4o model
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAI
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 10  // Token per minute capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-05-13'
    }
  }
}

// Azure AI Search for RAG (Retrieval-Augmented Generation)
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: 'search-${appName}'
  location: location
  sku: {
    name: 'basic'  // Basic tier for development
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
  }
}

// Outputs
output openAIEndpoint string = openAI.properties.endpoint
output openAIName string = openAI.name
output openAIModelName string = gpt4oDeployment.name
output searchServiceEndpoint string = 'https://${searchService.name}.search.windows.net'
output searchServiceName string = searchService.name
