targetScope = 'resourceGroup'

param location string = 'eastus'
param nameSuffix string = 'test01'

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: 'asp-dev-${nameSuffix}'
  location: location
  kind: 'linux'
  sku: {
    name: 'B1'
    tier: 'Basic'
    capacity: 1
  }
  properties: {
    reserved: true
  }
}

output appServicePlanId string = appServicePlan.id
