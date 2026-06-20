using './main.bicep'

param environmentType = 'dev'
param location = readEnvironmentVariable('AZURE_LOCATION')
param nameSuffix = 'dev01'

param tags = {
  workload: 'coreex'
  environment: 'dev'
  costProfile: 'minimum-practical'
}

// Basic B2 plan for multi-service web apps.
param appServicePlanSkuName = 'B2'
param appServicePlanTier = 'Basic'
param appServicePlanCapacity = 1
param appServiceLinuxFxVersion = 'DOTNETCORE|${replace(readEnvironmentVariable('AZD_DOTNET_TARGET_FRAMEWORK', readEnvironmentVariable('DOTNET_TARGET_FRAMEWORK', 'net8.0')), 'net', '')}'

// Requested by user: keep Service Bus on Standard.
param serviceBusSkuName = 'Standard'

param sqlAdminLogin = 'coreexadmin'
param sqlAdminPassword = readEnvironmentVariable('AZURE_SQL_ADMIN_PASSWORD')
param sqlDatabaseName = 'coreexdev'

// Requested by user: SQL serverless with 60-minute auto-pause.
param sqlSkuName = 'GP_S_Gen5_1'
param sqlTier = 'GeneralPurpose'
param sqlMinCapacity = '0.5'
param sqlAutoPauseDelay = 60

param postgresAdminLogin = 'coreexpgadmin'
param postgresAdminPassword = readEnvironmentVariable('AZURE_POSTGRES_ADMIN_PASSWORD', readEnvironmentVariable('AZURE_SQL_ADMIN_PASSWORD'))
param postgresDatabaseName = 'coreexdev'
param postgresSkuName = 'Standard_B1ms'
param postgresSkuTier = 'Burstable'
param postgresVersion = '16'
param postgresStorageSizeGb = 32

// Entry Azure Managed Redis tier.
param redisSkuName = 'Balanced_B0'
param redisHighAvailability = 'Disabled'
