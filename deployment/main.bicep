@description('Location for all resources.')
param location string = resourceGroup().location

@description('The admin user of the SQL Server')
param sqlAdministratorLogin string

@description('The password of the admin user of the SQL Server')
@secure()
param sqlAdministratorLoginPassword string

module sql  'modules/sqlServer.bicep' = {
  name: 'sql'
  params: {
    sqlAdministratorLogin: sqlAdministratorLogin
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
    location: location
  }
}

module bus  'modules/serviceBus.bicep' = {
  name: 'serviceBus'
  params: {
    serviceBusQueueName: 'myHr'
    location: location

  }
}

module webApp 'modules/appService.bicep' = {
  name: 'webApp'
  params: {
    sqlAdministratorLogin: sqlAdministratorLogin
    sqlAdministratorLoginPassword: sqlAdministratorLoginPassword
    sqlServerDatabaseName: sql.outputs.sqlServerDatabaseName
    sqlServerFullyQualifiedDomainName: sql.outputs.sqlServerFullyQualifiedName
    location: location
    servicebusName: bus.name
  }
}
