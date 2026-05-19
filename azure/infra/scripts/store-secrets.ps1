# Populates Key Vault with connection secrets needed for E2E testing.
# Runs automatically as a postprovision hook via azure.yaml.
$ErrorActionPreference = 'Stop'

$rg = $env:AZURE_RESOURCE_GROUP
if ([string]::IsNullOrWhiteSpace($rg)) {
    throw 'AZURE_RESOURCE_GROUP is not set.'
}

$sqlPassword = $env:AZURE_SQL_ADMIN_PASSWORD
if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
    throw 'AZURE_SQL_ADMIN_PASSWORD is not set.'
}

$postgresPassword = if ([string]::IsNullOrWhiteSpace($env:AZURE_POSTGRES_ADMIN_PASSWORD)) { $sqlPassword } else { $env:AZURE_POSTGRES_ADMIN_PASSWORD }

Write-Host "Locating Key Vault in resource group '$rg'..."
$kvName = (az keyvault list --resource-group $rg --query '[0].name' -o tsv)
$kvId   = (az keyvault show --name $kvName --resource-group $rg --query id -o tsv)

# Grant the current signed-in user Key Vault Administrator so secrets can be managed without manual IAM steps.
# Silently skipped when running as a service principal that does not support az ad signed-in-user.
try {
    $currentOid = (az ad signed-in-user show --query id -o tsv 2>$null)
    if (-not [string]::IsNullOrWhiteSpace($currentOid)) {
        Write-Host "Assigning Key Vault Administrator role to current user..."
        az role assignment create --role 'Key Vault Administrator' --assignee $currentOid --scope $kvId --only-show-errors 2>$null
        # Allow RBAC propagation before writing secrets.
        Start-Sleep -Seconds 15
    }
}
catch {
    # Non-fatal: user may already have the role assigned.
}

Write-Host 'Storing sql-admin-password...'
az keyvault secret set --vault-name $kvName --name 'sql-admin-password' --value $sqlPassword --output none

Write-Host 'Storing postgres-admin-password...'
az keyvault secret set --vault-name $kvName --name 'postgres-admin-password' --value $postgresPassword --output none

Write-Host 'Locating SQL Server...'
$sqlServer = (az sql server list --resource-group $rg --query '[0].name' -o tsv)
$sqlLogin  = (az sql server show --resource-group $rg --name $sqlServer --query administratorLogin -o tsv)
$sqlDb     = (az sql db list --resource-group $rg --server $sqlServer --query "[?name!='master'].name | [0]" -o tsv)
$sqlConn   = "Server=tcp:${sqlServer}.database.windows.net,1433;Database=${sqlDb};User Id=${sqlLogin};Password=${sqlPassword};Encrypt=true;TrustServerCertificate=false;"

Write-Host 'Storing sql-connection-string...'
az keyvault secret set --vault-name $kvName --name 'sql-connection-string' --value $sqlConn --output none

Write-Host 'Locating Postgres server...'
$postgresServer = (az postgres flexible-server list --resource-group $rg --query '[0].name' -o tsv)
$postgresLogin  = (az postgres flexible-server show --resource-group $rg --name $postgresServer --query administratorLogin -o tsv)
$postgresDb     = (az postgres flexible-server db list --resource-group $rg --server-name $postgresServer --query '[0].name' -o tsv)
$postgresConn   = "Server=$postgresServer.postgres.database.azure.com;Port=5432;Database=$postgresDb;User Id=$postgresLogin;Password=$postgresPassword;Ssl Mode=Require;Trust Server Certificate=true;"

Write-Host 'Storing postgres-connection-string...'
az keyvault secret set --vault-name $kvName --name 'postgres-connection-string' --value $postgresConn --output none

Write-Host 'Locating Service Bus namespace...'
$sbName = (az servicebus namespace list --resource-group $rg --query '[0].name' -o tsv)
$sbConn = (az servicebus namespace authorization-rule keys list `
    --resource-group $rg `
    --namespace-name $sbName `
    --name 'app' `
    --query primaryConnectionString -o tsv)

Write-Host 'Storing service-bus-connection-string...'
az keyvault secret set --vault-name $kvName --name 'service-bus-connection-string' --value $sbConn --output none

Write-Host "Secrets stored successfully in Key Vault '$kvName':"
Write-Host "  - sql-admin-password"
Write-Host "  - postgres-admin-password"
Write-Host "  - sql-connection-string"
Write-Host "  - postgres-connection-string"
Write-Host "  - service-bus-connection-string"
