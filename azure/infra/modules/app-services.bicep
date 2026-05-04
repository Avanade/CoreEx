param location string
param appServicePlanId string
param appServiceLinuxFxVersion string
param environmentType string
param suffix string
param tags object = {}
param appInsightsConnectionString string
param appInsightsResourceId string
param appInsightsInstrumentationKey string
param sqlConnectionString string
param redisConnectionString string
param serviceBusConnectionString string
param otlpHttpEndpoint string

var sharedAppSettings = [
  {
    name: 'ASPNETCORE_ENVIRONMENT'
    value: 'Development'
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: appInsightsConnectionString
  }
  {
    name: 'APPINSIGHTS_INSTRUMENTATIONKEY'
    value: appInsightsInstrumentationKey
  }
  {
    name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
    value: '~3'
  }
  {
    name: 'XDT_MicrosoftApplicationInsights_Mode'
    value: 'recommended'
  }
  {
    name: 'XDT_MicrosoftApplicationInsights_PreemptSdk'
    value: 'disabled'
  }
  {
    name: 'DiagnosticServices_EXTENSION_VERSION'
    value: '~3'
  }
  {
    name: 'APPINSIGHTS_PROFILERFEATURE_VERSION'
    value: '1.0.0'
  }
  {
    name: 'APPINSIGHTS_SNAPSHOTFEATURE_VERSION'
    value: '1.0.0'
  }
  {
    name: 'Aspire__Microsoft__Data__SqlClient__ConnectionString'
    value: sqlConnectionString
  }
  {
    name: 'Aspire__StackExchange__Redis__ConnectionString'
    value: redisConnectionString
  }
  {
    name: 'Aspire__Azure__Messaging__ServiceBus__ConnectionString'
    value: serviceBusConnectionString
  }
  {
    name: 'OTEL_EXPORTER_OTLP_PROTOCOL'
    value: 'http/protobuf'
  }
  {
    name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
    value: otlpHttpEndpoint
  }
]

resource productsApi 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-products-api-${environmentType}-${suffix}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'products-api'
    'hidden-link: /app-insights-resource-id': appInsightsResourceId
  })
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    sshEnabled: false
    endToEndEncryptionEnabled: true
    siteConfig: {
      linuxFxVersion: appServiceLinuxFxVersion
      minTlsVersion: '1.3'
      minTlsCipherSuite: 'TLS_AES_256_GCM_SHA384'
      scmMinTlsVersion: '1.3'
      netFrameworkVersion: ''
      ftpsState: 'Disabled'
      http20Enabled: true
      alwaysOn: true
      appSettings: sharedAppSettings
    }
  }
}

resource shoppingApi 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-shopping-api-${environmentType}-${suffix}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'shopping-api'
    'hidden-link: /app-insights-resource-id': appInsightsResourceId
  })
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    sshEnabled: false
    endToEndEncryptionEnabled: true
    siteConfig: {
      linuxFxVersion: appServiceLinuxFxVersion
      minTlsVersion: '1.3'
      minTlsCipherSuite: 'TLS_AES_256_GCM_SHA384'
      scmMinTlsVersion: '1.3'
      netFrameworkVersion: ''
      ftpsState: 'Disabled'
      http20Enabled: true
      alwaysOn: true
      appSettings: concat(sharedAppSettings, [
        {
          name: 'ProductsApi__BaseAddress'
          value: 'https://${productsApi.properties.defaultHostName}'
        }
      ])
    }
  }
}

resource productsOutboxRelay 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-products-outbox-relay-${environmentType}-${suffix}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'products-outbox-relay'
    'hidden-link: /app-insights-resource-id': appInsightsResourceId
  })
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    sshEnabled: false
    endToEndEncryptionEnabled: true
    siteConfig: {
      linuxFxVersion: appServiceLinuxFxVersion
      minTlsVersion: '1.3'
      minTlsCipherSuite: 'TLS_AES_256_GCM_SHA384'
      scmMinTlsVersion: '1.3'
      netFrameworkVersion: ''
      ftpsState: 'Disabled'
      http20Enabled: true
      alwaysOn: true
      appSettings: sharedAppSettings
    }
  }
}

resource shoppingOutboxRelay 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-shopping-outbox-relay-${environmentType}-${suffix}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'shopping-outbox-relay'
    'hidden-link: /app-insights-resource-id': appInsightsResourceId
  })
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    sshEnabled: false
    endToEndEncryptionEnabled: true
    siteConfig: {
      linuxFxVersion: appServiceLinuxFxVersion
      minTlsVersion: '1.3'
      minTlsCipherSuite: 'TLS_AES_256_GCM_SHA384'
      scmMinTlsVersion: '1.3'
      netFrameworkVersion: ''
      ftpsState: 'Disabled'
      http20Enabled: true
      alwaysOn: true
      appSettings: sharedAppSettings
    }
  }
}

resource productsSubscribe 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-products-subscribe-${environmentType}-${suffix}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'products-subscribe'
    'hidden-link: /app-insights-resource-id': appInsightsResourceId
  })
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    sshEnabled: false
    endToEndEncryptionEnabled: true
    siteConfig: {
      linuxFxVersion: appServiceLinuxFxVersion
      minTlsVersion: '1.3'
      minTlsCipherSuite: 'TLS_AES_256_GCM_SHA384'
      scmMinTlsVersion: '1.3'
      netFrameworkVersion: ''
      ftpsState: 'Disabled'
      http20Enabled: true
      alwaysOn: true
      appSettings: sharedAppSettings
    }
  }
}

resource shoppingSubscribe 'Microsoft.Web/sites@2023-12-01' = {
  name: 'app-shopping-subscribe-${environmentType}-${suffix}'
  location: location
  tags: union(tags, {
    'azd-service-name': 'shopping-subscribe'
    'hidden-link: /app-insights-resource-id': appInsightsResourceId
  })
  kind: 'app,linux'
  properties: {
    serverFarmId: appServicePlanId
    httpsOnly: true
    sshEnabled: false
    endToEndEncryptionEnabled: true
    siteConfig: {
      linuxFxVersion: appServiceLinuxFxVersion
      minTlsVersion: '1.3'
      minTlsCipherSuite: 'TLS_AES_256_GCM_SHA384'
      scmMinTlsVersion: '1.3'
      netFrameworkVersion: ''
      ftpsState: 'Disabled'
      http20Enabled: true
      alwaysOn: true
      appSettings: sharedAppSettings
    }
  }
}

output productsApiName string = productsApi.name
output shoppingApiName string = shoppingApi.name
output productsOutboxRelayName string = productsOutboxRelay.name
output shoppingOutboxRelayName string = shoppingOutboxRelay.name
output productsSubscribeName string = productsSubscribe.name
output shoppingSubscribeName string = shoppingSubscribe.name
