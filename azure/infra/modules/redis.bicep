param location string
param cacheName string
param skuName string
@allowed([
  'Enabled'
  'Disabled'
])
param highAvailability string
param tags object = {}

resource redis 'Microsoft.Cache/redisEnterprise@2025-07-01' = {
  name: cacheName
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    highAvailability: highAvailability
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    encryption: {}
  }
}

resource defaultDatabase 'Microsoft.Cache/redisEnterprise/databases@2025-04-01' = {
  parent: redis
  name: 'default'
  properties: {
    clientProtocol: 'Encrypted'
    clusteringPolicy: 'OSSCluster'
    evictionPolicy: 'VolatileLRU'
    modules: []
    port: 10000
  }
}

output id string = redis.id
output name string = redis.name
output hostName string = redis.properties.hostName
output connectionString string = '${redis.properties.hostName}:10000,password=${listKeys(defaultDatabase.id, defaultDatabase.apiVersion).primaryKey},ssl=True,abortConnect=False'
