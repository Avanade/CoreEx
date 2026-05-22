using './main.bicep'

// Placeholder only. Fill concrete production values during hardening.
param environmentType = 'prod'
param location = 'eastus'
param nameSuffix = 'prod01'

param tags = {
  workload: 'coreex'
  environment: 'prod'
}

// TODO: Replace with production-ready sizing.
param appServicePlanSkuName = 'B1'
param appServicePlanTier = 'Basic'
param appServicePlanCapacity = 1
param appServiceLinuxFxVersion = 'DOTNETCORE|${replace(readEnvironmentVariable('AZD_DOTNET_TARGET_FRAMEWORK', readEnvironmentVariable('DOTNET_TARGET_FRAMEWORK', 'net8.0')), 'net', '')}'

// TODO: Confirm production messaging tier.
param serviceBusSkuName = 'Standard'

param sqlAdminLogin = 'coreexadmin'
param sqlAdminPassword = readEnvironmentVariable('AZURE_SQL_ADMIN_PASSWORD')
param sqlDatabaseName = 'coreexprod'

// TODO: Replace with production-grade SQL settings.
param sqlSkuName = 'GP_S_Gen5_1'
param sqlTier = 'GeneralPurpose'
param sqlMinCapacity = '0.5'
param sqlAutoPauseDelay = 60

param postgresAdminLogin = 'coreexpgadmin'
param postgresAdminPassword = readEnvironmentVariable('AZURE_POSTGRES_ADMIN_PASSWORD', readEnvironmentVariable('AZURE_SQL_ADMIN_PASSWORD'))
param postgresDatabaseName = 'coreexprod'
param postgresSkuName = 'Standard_B1ms'
param postgresSkuTier = 'Burstable'
param postgresVersion = '16'
param postgresStorageSizeGb = 32

// TODO: Replace with production-grade cache tier.
param redisSkuName = 'Balanced_B0'
param redisHighAvailability = 'Enabled'
