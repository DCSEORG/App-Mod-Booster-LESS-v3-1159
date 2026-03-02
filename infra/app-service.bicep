// prompt-002-create-app-service
// App Service Plan + Web App in UK South with user-assigned managed identity

@description('Azure region')
param location string = 'uksouth'

@description('Resource ID of the user-assigned managed identity')
param managedIdentityId string

@description('Client ID of the user-assigned managed identity')
param managedIdentityClientId string

@description('Principal ID of the user-assigned managed identity')
param managedIdentityPrincipalId string

@description('SQL Server FQDN for the connection string')
param sqlServerFqdn string = ''

@description('Database name')
param databaseName string = 'Northwind'

var uniqueSuffix = uniqueString(resourceGroup().id)
var appServicePlanName = 'asp-appmodassist-${uniqueSuffix}'
var appServiceName = 'app-appmodassist-${uniqueSuffix}'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: 'S1'
    tier: 'Standard'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      http20Enabled: true
      minTlsVersion: '1.2'
      ftpsState: 'Disabled'
      appSettings: [
        {
          name: 'AZURE_CLIENT_ID'
          value: managedIdentityClientId
        }
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
      ]
      connectionStrings: sqlServerFqdn != '' ? [
        {
          name: 'DefaultConnection'
          connectionString: 'Server=tcp:${sqlServerFqdn};Database=${databaseName};Authentication=Active Directory Managed Identity;User Id=${managedIdentityClientId};'
          type: 'SQLAzure'
        }
      ] : []
    }
  }
}

@description('Name of the App Service')
output appServiceName string = appService.name

@description('Default hostname of the App Service')
output appServiceUrl string = 'https://${appService.properties.defaultHostName}'

@description('Principal ID of the assigned managed identity')
output managedIdentityPrincipalId string = managedIdentityPrincipalId

@description('Client ID of the assigned managed identity')
output managedIdentityClientId string = managedIdentityClientId
