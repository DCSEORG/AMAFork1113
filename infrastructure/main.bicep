// main.bicep - Main orchestration file for Expense Management Application
// This file orchestrates the deployment of all Azure resources

targetScope = 'resourceGroup'

// Parameters
@description('The name of the App Service')
param appName string

@description('The location for all resources')
param location string = resourceGroup().location

@description('SQL Server name')
param sqlServerName string

@description('SQL Database name')
param sqlDatabaseName string

@description('Managed Identity name')
param managedIdentityName string

@description('Whether to include Chat UI and GenAI resources')
param includeChatUI bool = false

@description('Location for OpenAI resources (Sweden for GPT-4o)')
param openAILocation string = 'swedencentral'

// Deploy App Service and Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    appName: appName
    location: location
    managedIdentityName: managedIdentityName
    sqlServerName: sqlServerName
    sqlDatabaseName: sqlDatabaseName
    includeChatUI: includeChatUI
  }
}

// Deploy GenAI resources conditionally
module genAI 'genai.bicep' = if (includeChatUI) {
  name: 'genAIDeployment'
  params: {
    location: openAILocation
    appName: appName
    managedIdentityId: appService.outputs.managedIdentityId
  }
}

// Outputs
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output openAIEndpoint string = includeChatUI ? genAI.outputs.openAIEndpoint : ''
output openAIModelName string = includeChatUI ? genAI.outputs.openAIModelName : ''
