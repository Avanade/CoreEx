param location string
param serverName string
param databaseName string
param adminLogin string
@secure()
param adminPassword string
param clientIp string = ''
param skuName string
param skuTier string
param version string
param storageSizeGb int
param tags object = {}

resource server 'Microsoft.DBforPostgreSQL/flexibleServers@2023-12-01-preview' = {
  name: serverName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    version: version
    publicNetworkAccess: 'Enabled'
    storage: {
      storageSizeGB: storageSizeGb
    }
    highAvailability: {
      mode: 'Disabled'
    }
    backup: {
      backupRetentionDays: 7
      geoRedundantBackup: 'Disabled'
    }
  }
}

resource azureFirewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01-preview' = {
  parent: server
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource clientFirewallRule 'Microsoft.DBforPostgreSQL/flexibleServers/firewallRules@2023-12-01-preview' = if (!empty(clientIp)) {
  parent: server
  name: 'AllowCurrentRunner-${replace(clientIp, '.', '-')}'
  properties: {
    startIpAddress: clientIp
    endIpAddress: clientIp
  }
}

resource db 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2023-12-01-preview' = {
  parent: server
  name: databaseName
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

output serverName string = server.name
output databaseName string = db.name
output fullyQualifiedDomainName string = '${server.name}.postgres.database.azure.com'
output connectionString string = 'Server=${server.name}.postgres.database.azure.com;Port=5432;Database=${databaseName};User Id=${adminLogin};Password=${adminPassword};Ssl Mode=Require;Trust Server Certificate=true;'
