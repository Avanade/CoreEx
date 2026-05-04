targetScope = 'resourceGroup'

@allowed([
  'dev'
  'test'
  'prod'
])
@description('Deployment environment.')
param environmentType string

@description('Location for all resources.')
param location string = resourceGroup().location

@description('Unique suffix for globally unique resource names. Use a short lowercase token, e.g. a1b2c3.')
param nameSuffix string

@description('Tags applied to all resources.')
param tags object = {}

@description('App Service Plan SKU name.')
param appServicePlanSkuName string

@description('App Service Plan tier.')
param appServicePlanTier string

@description('App Service Plan instance count.')
param appServicePlanCapacity int

@description('App Service Linux runtime stack. Example: DOTNETCORE|10.0.')
param appServiceLinuxFxVersion string

@description('Service Bus namespace SKU. Basic, Standard, or Premium.')
param serviceBusSkuName string

@description('Azure SQL administrator login name.')
param sqlAdminLogin string

@secure()
@description('Azure SQL administrator password.')
param sqlAdminPassword string

@description('Azure SQL database name.')
param sqlDatabaseName string

@description('Current runner public IPv4 address to allow through the Azure SQL firewall.')
param sqlFirewallClientIp string = ''

@description('Azure SQL database SKU name. Example: GP_S_Gen5_1.')
param sqlSkuName string

@description('Azure SQL database tier. Example: GeneralPurpose.')
param sqlTier string

@description('Azure SQL minimum vCores for serverless, represented as a JSON number string (for example 0.5).')
param sqlMinCapacity string

@description('Azure SQL auto-pause delay in minutes. Set to -1 to disable.')
param sqlAutoPauseDelay int

@description('Azure Managed Redis SKU name. Example: Balanced_B0.')
param redisSkuName string

@allowed([
  'Enabled'
  'Disabled'
])
@description('Azure Managed Redis high availability mode.')
param redisHighAvailability string

var suffix = toLower(nameSuffix)
var keyVaultName = take('kv${environmentType}${suffix}${uniqueString(deployment().name, resourceGroup().id)}', 24)
var mergedTags = union(tags, {
  environment: environmentType
  managedBy: 'azd'
  'azd-env-name': environmentType
})

module appInsights './modules/application-insights.bicep' = {
  name: 'appInsightsDeploy'
  params: {
    location: location
    name: 'appi-${environmentType}-${suffix}'
    tags: mergedTags
  }
}

module keyVault './modules/key-vault.bicep' = {
  name: 'keyVaultDeploy'
  params: {
    location: location
    name: keyVaultName
    tags: mergedTags
  }
}

module appServicePlan './modules/app-service-plan.bicep' = {
  name: 'appServicePlanDeploy'
  params: {
    location: location
    name: 'asp-${environmentType}-${suffix}'
    skuName: appServicePlanSkuName
    skuTier: appServicePlanTier
    capacity: appServicePlanCapacity
    tags: mergedTags
  }
}

module serviceBus './modules/service-bus.bicep' = {
  name: 'serviceBusDeploy'
  params: {
    location: location
    namespaceName: 'sb-${environmentType}-${suffix}'
    skuName: serviceBusSkuName
    tags: mergedTags
  }
}

module redis './modules/redis.bicep' = {
  name: 'redisDeploy'
  params: {
    location: location
    cacheName: 'redis-${environmentType}-${suffix}'
    skuName: redisSkuName
    highAvailability: redisHighAvailability
    tags: mergedTags
  }
}

module sql './modules/database.bicep' = {
  name: 'sqlDeploy'
  params: {
    location: location
    serverName: 'sql-${environmentType}-${suffix}'
    databaseName: sqlDatabaseName
    adminLogin: sqlAdminLogin
    adminPassword: sqlAdminPassword
    clientIp: sqlFirewallClientIp
    skuName: sqlSkuName
    skuTier: sqlTier
    minCapacity: sqlMinCapacity
    autoPauseDelay: sqlAutoPauseDelay
    tags: mergedTags
  }
}

module appServices './modules/app-services.bicep' = {
  name: 'appServicesDeploy'
  params: {
    location: location
    appServicePlanId: appServicePlan.outputs.id
    appServiceLinuxFxVersion: appServiceLinuxFxVersion
    environmentType: environmentType
    suffix: suffix
    tags: mergedTags
    appInsightsConnectionString: appInsights.outputs.connectionString
    appInsightsResourceId: appInsights.outputs.id
    appInsightsInstrumentationKey: appInsights.outputs.instrumentationKey
    sqlConnectionString: sql.outputs.connectionString
    redisConnectionString: redis.outputs.connectionString
    serviceBusConnectionString: serviceBus.outputs.connectionString
    otlpHttpEndpoint: aspireDashboard.outputs.otlpHttpEndpoint
  }
}

module aspireDashboard './modules/aspire-dashboard.bicep' = {
  name: 'aspireDashboardDeploy'
  params: {
    location: location
    appServicePlanId: appServicePlan.outputs.id
    environmentType: environmentType
    suffix: suffix
    tags: mergedTags
  }
}

output appServicePlanName string = appServicePlan.outputs.name
output appInsightsConnectionString string = appInsights.outputs.connectionString
output keyVaultName string = keyVault.outputs.name
output serviceBusNamespaceName string = serviceBus.outputs.namespaceName
output redisHostName string = redis.outputs.hostName
output sqlServerName string = sql.outputs.serverName
output sqlDatabaseName string = sql.outputs.databaseName
output productsApiAppName string = appServices.outputs.productsApiName
output shoppingApiAppName string = appServices.outputs.shoppingApiName
output productsOutboxRelayAppName string = appServices.outputs.productsOutboxRelayName
output shoppingOutboxRelayAppName string = appServices.outputs.shoppingOutboxRelayName
output productsSubscribeAppName string = appServices.outputs.productsSubscribeName
output shoppingSubscribeAppName string = appServices.outputs.shoppingSubscribeName
output aspireDashboardAppName string = aspireDashboard.outputs.appName
output aspireDashboardUri string = aspireDashboard.outputs.dashboardUri
output aspireDashboardOtlpGrpcEndpoint string = aspireDashboard.outputs.otlpGrpcEndpoint
