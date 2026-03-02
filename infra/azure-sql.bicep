// prompt-003-create-azure-sql
// Azure SQL Server + Database with Entra ID-only authentication

@description('Azure region')
param location string = 'uksouth'

@description('Object ID of the Entra ID administrator (deployer)')
param adminObjectId string

@description('UPN / login of the Entra ID administrator (deployer)')
param adminLogin string

@description('Principal ID of the managed identity that needs DB access')
param managedIdentityPrincipalId string

var uniqueSuffix = uniqueString(resourceGroup().id)
var sqlServerName = 'sql-appmodassist-${uniqueSuffix}'
var databaseName = 'Northwind'

// Azure SQL Server — Entra ID-only, no SQL auth
resource sqlServer 'Microsoft.Sql/servers@2021-11-01' = {
  name: sqlServerName
  location: location
  properties: {
    // SQL authentication disabled — Entra ID only (MCAPS policy)
    administrators: {
      administratorType: 'ActiveDirectory'
      azureADOnlyAuthentication: true
      login: adminLogin
      sid: adminObjectId
      tenantId: tenant().tenantId
      principalType: 'User'
    }
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Northwind database (Basic tier for development)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2021-11-01' = {
  parent: sqlServer
  name: databaseName
  location: location
  sku: {
    name: 'Basic'
    tier: 'Basic'
    capacity: 5
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 2147483648 // 2 GB
    requestedBackupStorageRedundancy: 'Local'
  }
}

// Allow Azure services to access the SQL Server
resource firewallAllowAzure 'Microsoft.Sql/servers/firewallRules@2021-11-01' = {
  parent: sqlServer
  name: 'AllowAllAzureIPs'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

@description('SQL Server name')
output sqlServerName string = sqlServer.name

@description('SQL Server fully-qualified domain name')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Database name')
output databaseName string = databaseName
