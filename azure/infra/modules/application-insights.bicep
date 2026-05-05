param location string
param name string
param tags object = {}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    Flow_Type: 'Bluefield'
    Request_Source: 'rest'
  }
}

output name string = appInsights.name
output connectionString string = appInsights.properties.ConnectionString
output id string = appInsights.id
output instrumentationKey string = appInsights.properties.InstrumentationKey
