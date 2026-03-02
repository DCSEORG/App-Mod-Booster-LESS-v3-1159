// prompt-001-create-managed-identity
// User-assigned managed identity for App Service to connect to Azure SQL

@description('Azure region for the managed identity')
param location string = resourceGroup().location

var uniqueSuffix = uniqueString(resourceGroup().id)
var managedIdentityName = 'mid-AppModAssist-${uniqueSuffix}'

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: managedIdentityName
  location: location
}

@description('Resource ID of the managed identity')
output managedIdentityId string = managedIdentity.id

@description('Client ID of the managed identity (for AZURE_CLIENT_ID env var)')
output managedIdentityClientId string = managedIdentity.properties.clientId

@description('Principal ID of the managed identity (for role assignments)')
output managedIdentityPrincipalId string = managedIdentity.properties.principalId

@description('Name of the managed identity')
output managedIdentityName string = managedIdentity.name
