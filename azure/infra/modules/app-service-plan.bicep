param location string
param name string
param skuName string
param skuTier string
param capacity int
param tags object = {}

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'linux'
  sku: {
    name: skuName
    tier: skuTier
    capacity: capacity
  }
  properties: {
    reserved: true
  }
}

output id string = plan.id
output name string = plan.name
