param location string
param appServicePlanId string
param environmentType string
param suffix string
param tags object = {}

var dashboardName = 'app-aspire-dashboard-${environmentType}-${suffix}'

resource aspireDashboard 'Microsoft.Web/sites@2023-12-01' = {
  name: dashboardName
  location: location
  tags: union(tags, {
    role: 'aspire-dashboard'
  })
  kind: 'app,linux,container'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOCKER|mcr.microsoft.com/dotnet/aspire-dashboard:latest'
      ftpsState: 'Disabled'
      alwaysOn: true
      http20Enabled: true
      appSettings: [
        {
          name: 'WEBSITES_PORT'
          value: '18888'
        }
        {
          name: 'ASPNETCORE_URLS'
          value: 'http://0.0.0.0:18888'
        }
        {
          name: 'ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL'
          value: 'http://0.0.0.0:18889'
        }
        {
          name: 'ASPIRE_DASHBOARD_OTLP_HTTP_ENDPOINT_URL'
          value: 'http://0.0.0.0:18888'
        }
        {
          name: 'DASHBOARD__UI__DISABLERESOURCEGRAPH'
          value: 'true'
        }
      ]
    }
  }
}

output id string = aspireDashboard.id
output appName string = aspireDashboard.name
output dashboardUri string = 'https://${aspireDashboard.properties.defaultHostName}'
output otlpGrpcEndpoint string = 'https://${aspireDashboard.properties.defaultHostName}'
output otlpHttpEndpoint string = 'https://${aspireDashboard.properties.defaultHostName}'
