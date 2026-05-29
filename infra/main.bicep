// ── LMS Infrastructure — Azure SQL Database (free serverless offer) ──────────
// Deploy:
//   New-AzResourceGroupDeployment `
//     -ResourceGroupName 'lms-rg' `
//     -TemplateFile './infra/main.bicep' `
//     -adminPassword (Read-Host -AsSecureString "SQL admin password")

@description('Azure region for all resources. Defaults to the resource group location.')
param location string = resourceGroup().location

@description('SQL Server administrator login name.')
param adminLogin string = 'lmsadmin'

@description('SQL Server administrator password.')
@secure()
param adminPassword string

@description('Your local IP address to allow through the SQL Server firewall. Leave empty to skip.')
param clientIpAddress string = ''

// ── Unique server name scoped to this resource group ─────────────────────────
var sqlServerName = 'lms-sql-${uniqueString(resourceGroup().id)}'

// ── SQL Server ────────────────────────────────────────────────────────────────
resource sqlServer 'Microsoft.Sql/servers@2023-05-01-preview' = {
  name: sqlServerName
  location: location
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
  }
}

// Allow Azure services (GitHub Actions CI, App Service) to reach the server
resource allowAzureServices 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = {
  name: 'AllowAllWindowsAzureIps'
  parent: sqlServer
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

// Allow your development machine — only added when clientIpAddress is provided
resource allowClientIp 'Microsoft.Sql/servers/firewallRules@2023-05-01-preview' = if (!empty(clientIpAddress)) {
  name: 'AllowDeveloperIp'
  parent: sqlServer
  properties: {
    startIpAddress: clientIpAddress
    endIpAddress: clientIpAddress
  }
}

// ── SQL Database — free serverless offer ──────────────────────────────────────
// GP_S_Gen5_1 = General Purpose Serverless, Gen5, 1 vCore
// useFreeLimit: true claims the free offer (once per subscription)
// autoPauseDelay: 60 = auto-pause after 60 min of inactivity (zero cost at rest)
resource sqlDatabase 'Microsoft.Sql/servers/databases@2023-05-01-preview' = {
  name: 'LmsDb'
  parent: sqlServer
  location: location
  sku: {
    name: 'GP_S_Gen5_1'
    tier: 'GeneralPurpose'
    family: 'Gen5'
    capacity: 1
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    maxSizeBytes: 34359738368 // 32 GB
    autoPauseDelay: 60
    minCapacity: json('0.5')
    useFreeLimit: true
    freeLimitExhaustionBehavior: 'AutoPause' // pause instead of billing when free quota runs out
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
@description('Fully qualified domain name of the SQL Server.')
output sqlServerFqdn string = sqlServer.properties.fullyQualifiedDomainName

@description('Database name.')
output databaseName string = sqlDatabase.name

@description('Connection string template — replace {password} with the admin password.')
output connectionStringTemplate string = 'Server=tcp:${sqlServer.properties.fullyQualifiedDomainName},1433;Initial Catalog=LmsDb;Persist Security Info=False;User ID=${adminLogin};Password={password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
