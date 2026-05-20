param location string
param appServicePlanId string
param appServiceLinuxFxVersion string
param environmentType string
param suffix string
param tags object = {}
param appInsightsConnectionString string
param appInsightsResourceId string
param appInsightsInstrumentationKey string
param keyVaultName string
param keyVaultUri string
param redisConnectionString string
param otlpHttpEndpoint string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

var keyVaultSecretsUserRoleDefinitionId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6')
var sqlConnectionStringKeyVaultReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/sql-connection-string/)'
var postgresConnectionStringKeyVaultReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/postgres-connection-string/)'
var serviceBusConnectionStringKeyVaultReference = '@Microsoft.KeyVault(SecretUri=${keyVaultUri}secrets/service-bus-connection-string/)'

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
    name: 'Aspire__StackExchange__Redis__ConnectionString'
    value: redisConnectionString
  }
  {
    name: 'Aspire__Azure__Messaging__ServiceBus__ConnectionString'
    value: serviceBusConnectionStringKeyVaultReference
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

var sqlDbAppSettings = [
  {
    name: 'Aspire__Microsoft__Data__SqlClient__ConnectionString'
    value: sqlConnectionStringKeyVaultReference
  }
]

var postgresDbAppSettings = [
  {
    name: 'Aspire__Npgsql__ConnectionString'
    value: postgresConnectionStringKeyVaultReference
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
  identity: {
    type: 'SystemAssigned'
  }
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
      appSettings: concat(sharedAppSettings, postgresDbAppSettings)
    }
  }
}

resource productsApiKeyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, productsApi.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    principalId: productsApi.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
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
  identity: {
    type: 'SystemAssigned'
  }
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
      appSettings: concat(sharedAppSettings, sqlDbAppSettings, [
        {
          name: 'ProductsApi__BaseAddress'
          value: 'https://${productsApi.properties.defaultHostName}'
        }
      ])
    }
  }
}

resource shoppingApiKeyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, shoppingApi.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    principalId: shoppingApi.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
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
  identity: {
    type: 'SystemAssigned'
  }
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
      appSettings: concat(sharedAppSettings, postgresDbAppSettings)
    }
  }
}

resource productsOutboxRelayKeyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, productsOutboxRelay.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    principalId: productsOutboxRelay.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
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
  identity: {
    type: 'SystemAssigned'
  }
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
      appSettings: concat(sharedAppSettings, sqlDbAppSettings)
    }
  }
}

resource shoppingOutboxRelayKeyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, shoppingOutboxRelay.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    principalId: shoppingOutboxRelay.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
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
  identity: {
    type: 'SystemAssigned'
  }
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
      appSettings: concat(sharedAppSettings, postgresDbAppSettings)
    }
  }
}

resource productsSubscribeKeyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, productsSubscribe.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    principalId: productsSubscribe.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
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
  identity: {
    type: 'SystemAssigned'
  }
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
      appSettings: concat(sharedAppSettings, sqlDbAppSettings)
    }
  }
}

resource shoppingSubscribeKeyVaultSecretsUserRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, shoppingSubscribe.id, 'KeyVaultSecretsUser')
  scope: keyVault
  properties: {
    principalId: shoppingSubscribe.identity.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: keyVaultSecretsUserRoleDefinitionId
  }
}

output productsApiName string = productsApi.name
output shoppingApiName string = shoppingApi.name
output productsOutboxRelayName string = productsOutboxRelay.name
output shoppingOutboxRelayName string = shoppingOutboxRelay.name
output productsSubscribeName string = productsSubscribe.name
output shoppingSubscribeName string = shoppingSubscribe.name

