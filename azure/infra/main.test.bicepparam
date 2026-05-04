using './main.bicep'

// Placeholder only. Fill concrete test values when test environment is approved.
param environmentType = 'test'
param location = 'eastus'
param nameSuffix = 'test01'

param tags = {
  workload: 'coreex'
  environment: 'test'
}

// TODO: Confirm test sizing.
param appServicePlanSkuName = 'B1'
param appServicePlanTier = 'Basic'
param appServicePlanCapacity = 1
param appServiceLinuxFxVersion = 'DOTNETCORE|${replace(readEnvironmentVariable('AZD_DOTNET_TARGET_FRAMEWORK', readEnvironmentVariable('DOTNET_TARGET_FRAMEWORK', 'net8.0')), 'net', '')}'

// TODO: Confirm messaging tier for test.
param serviceBusSkuName = 'Standard'

param sqlAdminLogin = 'coreexadmin'
param sqlAdminPassword = readEnvironmentVariable('AZURE_SQL_ADMIN_PASSWORD')
param sqlDatabaseName = 'coreextest'

// TODO: Confirm SQL model for test.
param sqlSkuName = 'GP_S_Gen5_1'
param sqlTier = 'GeneralPurpose'
param sqlMinCapacity = '0.5'
param sqlAutoPauseDelay = 60

// TODO: Confirm cache tier for test.
param redisSkuName = 'Balanced_B0'
param redisHighAvailability = 'Enabled'
