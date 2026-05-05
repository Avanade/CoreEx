param location string
param serverName string
param databaseName string
param adminLogin string
@secure()
param adminPassword string
param clientIp string = ''
param skuName string
param skuTier string
param minCapacity string
param autoPauseDelay int
param tags object = {}

resource server 'Microsoft.Sql/servers@2023-08-01-preview' = {
  name: serverName
  location: location
  tags: tags
  properties: {
    administratorLogin: adminLogin
    administratorLoginPassword: adminPassword
    version: '12.0'
    publicNetworkAccess: 'Enabled'
    minimalTlsVersion: '1.2'
  }
}

resource azureFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = {
  parent: server
  name: 'AllowAzureServices'
  properties: {
    startIpAddress: '0.0.0.0'
    endIpAddress: '0.0.0.0'
  }
}

resource clientFirewallRule 'Microsoft.Sql/servers/firewallRules@2023-08-01-preview' = if (!empty(clientIp)) {
  parent: server
  name: 'AllowCurrentRunner-${replace(clientIp, '.', '-')}'
  properties: {
    startIpAddress: clientIp
    endIpAddress: clientIp
  }
}

resource db 'Microsoft.Sql/servers/databases@2023-08-01-preview' = {
  parent: server
  name: databaseName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuTier
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
    minCapacity: json(minCapacity)
    autoPauseDelay: autoPauseDelay
  }
}

output serverName string = server.name
output databaseName string = db.name
output fullyQualifiedDomainName string = server.properties.fullyQualifiedDomainName
output connectionString string = 'Data Source=tcp:${server.properties.fullyQualifiedDomainName},1433;Initial Catalog=${databaseName};User id=${adminLogin};Password=${adminPassword};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;'
