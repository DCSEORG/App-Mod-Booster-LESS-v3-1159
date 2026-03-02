// main.bicep - orchestrates all infrastructure modules
// Deploys: managed identity, app service, azure sql

@description('Azure region for all resources')
param location string = 'uksouth'

@description('Object ID of the Entra ID administrator (deployer) for SQL')
param adminObjectId string

@description('UPN of the Entra ID administrator (deployer) for SQL')
param adminLogin string

// ── Module: Managed Identity ─────────────────────────────────────────────────
module managedIdentity 'managed-identity.bicep' = {
  name: 'deploy-managed-identity'
  params: {
    location: location
  }
}

// ── Module: Azure SQL ────────────────────────────────────────────────────────
module azureSql 'azure-sql.bicep' = {
  name: 'deploy-azure-sql'
  params: {
    location: location
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
  }
}

// ── Module: App Service ──────────────────────────────────────────────────────
module appService 'app-service.bicep' = {
  name: 'deploy-app-service'
  params: {
    location: location
    managedIdentityId: managedIdentity.outputs.managedIdentityId
    managedIdentityClientId: managedIdentity.outputs.managedIdentityClientId
    managedIdentityPrincipalId: managedIdentity.outputs.managedIdentityPrincipalId
    sqlServerFqdn: azureSql.outputs.sqlServerFqdn
    databaseName: azureSql.outputs.databaseName
  }
}

// ── Outputs ──────────────────────────────────────────────────────────────────
output managedIdentityId string = managedIdentity.outputs.managedIdentityId
output managedIdentityClientId string = managedIdentity.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = managedIdentity.outputs.managedIdentityPrincipalId
output managedIdentityName string = managedIdentity.outputs.managedIdentityName

output sqlServerName string = azureSql.outputs.sqlServerName
output sqlServerFqdn string = azureSql.outputs.sqlServerFqdn
output databaseName string = azureSql.outputs.databaseName

output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = appService.outputs.appServiceUrl
