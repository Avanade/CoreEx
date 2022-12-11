@description('Location for all resources.')
param location string = resourceGroup().location

@description('Describes plan\'s pricing tier and instance size. Check details at https://azure.microsoft.com/en-us/pricing/details/app-service/')
@allowed([
  'F1'
  'D1'
  'B1'
  'B2'
  'B3'
  'S1'
  'S2'
  'S3'
  'P1'
  'P2'
  'P3'
  'P4'
])
param skuName string = 'S1'

@description('Describes plan\'s instance count')
@minValue(1)
@maxValue(3)
param skuCapacity int = 1

@description('The admin user of the SQL Server')
param sqlAdministratorLogin string

@description('The password of the admin user of the SQL Server')
@secure()
param sqlAdministratorLoginPassword string

@description('The fully qualified domain name for Sql Server')
param sqlServerFullyQualifiedDomainName string

@description('The database name for the app')
param sqlServerDatabaseName string

var hostingPlanName = 'myHrPlan-${uniqueString(resourceGroup().id)}'
var websiteName = 'myHrApp${uniqueString(resourceGroup().id)}'

resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {
  name: 'plan-myapplication'
  location: location
  sku: {
    name: 'S1'
  }
  properties:{
    reserved: true
  }
  kind: 'Linux'
  // tags:tags
}

resource hostingPlan 'Microsoft.Web/serverfarms@2020-12-01' = {
  name: hostingPlanName
  location: location
  tags: {
    displayName: 'HostingPlan'
  }
  sku: {
    name: skuName
    capacity: skuCapacity
  }
  kind: 'Linux'
  properties: {
    reserved: true
  }  
}

resource website 'Microsoft.Web/sites@2020-12-01' = {
  name: websiteName
  location: location
  tags: {
    'hidden-related:${hostingPlan.id}': 'empty'
    displayName: 'Website'
  }
  properties: {
    serverFarmId: hostingPlan.id
    reserved: true
    siteConfig:{
      alwaysOn: true
      ftpsState: 'Disabled'
      // appSettings: appSettings
      linuxFxVersion: 'DOTNETCORE|6.0'
      http20Enabled: true
    }
    httpsOnly: true  
  }  
}

resource webSiteConnectionStrings 'Microsoft.Web/sites/config@2020-12-01' = {
  parent: website
  name: 'connectionstrings'
  properties: {
    DefaultConnection: {
      value: 'Data Source=tcp:${sqlServerFullyQualifiedDomainName},1433;Initial Catalog=${sqlServerDatabaseName};User Id=${sqlAdministratorLogin}@${sqlServerFullyQualifiedDomainName};Password=${sqlAdministratorLoginPassword};'
      type: 'SQLAzure'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'AppInsights${website.name}'
  location: location
  tags: {
    'hidden-link:${website.id}': 'Resource'
    displayName: 'AppInsightsComponent'
  }
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2020-08-01' = {
  name: 'LogAnalytics${website.name}'  
  location: location
  tags: {
    displayName: 'Log Analytics'
    ProjectName: websiteName
  }
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 120
    features: {
      searchVersion: 1
      legacy: 0
      enableLogAccessUsingOnlyResourcePermissions: true
    }
  }
}
