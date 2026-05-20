$ErrorActionPreference = 'Stop'

# Runs Contoso *.Database DbEx migrations against their provisioned database providers.
# Products runs on Postgres, and all other domains continue to run on Azure SQL.

$scriptDir = Split-Path -Parent $PSCommandPath
$azureDir = Resolve-Path (Join-Path $scriptDir '..')
$repoRoot = Resolve-Path (Join-Path $azureDir '..')

if (-not (Get-Command azd -ErrorAction SilentlyContinue)) {
    throw "The 'azd' command is required to resolve environment values."
}

function Get-AzdEnvValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Key
    )

    try {
        $value = (azd env get-value $Key 2>$null).Trim()
    }
    catch {
        return $null
    }

    if ([string]::IsNullOrWhiteSpace($value) -or $value.StartsWith('ERROR:')) {
        return $null
    }

    return $value
}

$targetFramework = if ($env:AZD_DOTNET_TARGET_FRAMEWORK) {
    $env:AZD_DOTNET_TARGET_FRAMEWORK
}
elseif ($env:DOTNET_TARGET_FRAMEWORK) {
    $env:DOTNET_TARGET_FRAMEWORK
}
elseif ((dotnet --list-runtimes) -match 'Microsoft\.NETCore\.App 10\.') {
    'net10.0'
}
elseif ((dotnet --list-runtimes) -match 'Microsoft\.NETCore\.App 9\.') {
    'net9.0'
}
else {
    'net8.0'
}
$sqlServer = Get-AzdEnvValue -Key 'sqlServerName'
$sqlDatabase = Get-AzdEnvValue -Key 'sqlDatabaseName'
$postgresServer = Get-AzdEnvValue -Key 'postgresServerName'
$postgresDatabase = Get-AzdEnvValue -Key 'postgresDatabaseName'
$sqlAdminLogin = if ($env:AZURE_SQL_ADMIN_LOGIN) { $env:AZURE_SQL_ADMIN_LOGIN } else { 'coreexadmin' }
$sqlPassword = $env:AZURE_SQL_ADMIN_PASSWORD
$postgresAdminLogin = if ($env:AZURE_POSTGRES_ADMIN_LOGIN) { $env:AZURE_POSTGRES_ADMIN_LOGIN } else { 'coreexpgadmin' }
$postgresPassword = $env:AZURE_POSTGRES_ADMIN_PASSWORD

if ([string]::IsNullOrWhiteSpace($sqlServer) -or [string]::IsNullOrWhiteSpace($sqlDatabase) -or [string]::IsNullOrWhiteSpace($postgresServer) -or [string]::IsNullOrWhiteSpace($postgresDatabase)) {
    throw "Missing required azd environment outputs (sqlServerName/sqlDatabaseName/postgresServerName/postgresDatabaseName). Run 'azd provision --no-prompt' (or 'azd up --no-prompt') to refresh environment outputs before deploy."
}

if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
    $sqlPassword = Get-AzdEnvValue -Key 'AZURE_SQL_ADMIN_PASSWORD'
}

if ([string]::IsNullOrWhiteSpace($postgresPassword)) {
    $postgresPassword = Get-AzdEnvValue -Key 'AZURE_POSTGRES_ADMIN_PASSWORD'
}

if ([string]::IsNullOrWhiteSpace($postgresPassword)) {
    $postgresPassword = $sqlPassword
}

if ([string]::IsNullOrWhiteSpace($sqlPassword) -or [string]::IsNullOrWhiteSpace($postgresPassword)) {
    throw 'Database admin passwords are required to run DbEx migrations.'
}
$sqlConnectionString = "Server=tcp:$sqlServer.database.windows.net,1433;Initial Catalog=$sqlDatabase;User ID=$sqlAdminLogin;Password=$sqlPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$postgresConnectionString = "Server=$postgresServer.postgres.database.azure.com;Port=5432;Database=$postgresDatabase;User Id=$postgresAdminLogin;Password=$postgresPassword;Ssl Mode=Require;"
$projects = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'samples/src') -Recurse -File -Filter 'Contoso.*.Database.csproj' |
    Sort-Object FullName

if ($projects.Count -eq 0) {
    throw 'No Contoso database projects were found under samples/src.'
}

Write-Host "Running DbEx migrations for $($projects.Count) database project(s) using framework '$targetFramework' with domain-specific database providers."

$requiresSqlFirewall = $false
foreach ($project in $projects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project.Name)
    if ($projectName -ne 'Contoso.Products.Database') {
        $requiresSqlFirewall = $true
        break
    }
}

if ($requiresSqlFirewall) {
    & (Join-Path $scriptDir 'ensure-sql-firewall-rule.ps1')
}

foreach ($project in $projects) {
    $projectDir = $project.Directory.FullName
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project.Name)
    $domainName = $projectName -replace '\\.Database$', ''
    $testCommonProject = Join-Path $repoRoot "samples/tests/$domainName.Test.Common/$domainName.Test.Common.csproj"
    $migrationCommand = 'Migrate'
    $extraArgs = @()
    $promptArgs = @()

    # Remove Windows zone marker sidecar files to avoid embedding/executing them as migration resources on Linux.
    $zoneFiles = Get-ChildItem -LiteralPath $projectDir -Recurse -File | Where-Object { $_.Name -like '*:Zone.Identifier' }
    if ($zoneFiles.Count -gt 0) {
        Write-Host "Removing $($zoneFiles.Count) Zone.Identifier sidecar file(s) from $projectName."
        $zoneFiles | Remove-Item -Force
    }

    if (Test-Path -LiteralPath $testCommonProject) {
        $testCommonDir = Split-Path -Parent $testCommonProject
        $testCommonName = [System.IO.Path]::GetFileNameWithoutExtension($testCommonProject)

        $zoneFiles = Get-ChildItem -LiteralPath $testCommonDir -Recurse -File | Where-Object { $_.Name -like '*:Zone.Identifier' }
        if ($zoneFiles.Count -gt 0) {
            Write-Host "Removing $($zoneFiles.Count) Zone.Identifier sidecar file(s) from $testCommonName."
            $zoneFiles | Remove-Item -Force
        }

        dotnet build $testCommonProject -c Release -f $targetFramework | Out-Null
        $testCommonAssembly = Join-Path $testCommonDir "bin/Release/$targetFramework/$testCommonName.dll"
        if (Test-Path -LiteralPath $testCommonAssembly) {
            $migrationCommand = 'ResetAndAll'
            $extraArgs = @('--assembly', $testCommonAssembly)
            $promptArgs = @('--accept-prompts')
        }
    }

    $connectionString = $sqlConnectionString
    $connectionTarget = "Azure SQL database '$sqlDatabase'"
    if ($projectName -eq 'Contoso.Products.Database') {
        $connectionString = $postgresConnectionString
        $connectionTarget = "Azure Postgres database '$postgresDatabase'"
    }

    if ($projectName -eq 'Contoso.Products.Database' -and $migrationCommand -eq 'ResetAndAll') {
        Write-Host "Running $projectName migrations (Migrate + Schema + ResetAndData) against $connectionTarget."
        dotnet run --project $project.FullName -c Release -f $targetFramework -- --connection-string $connectionString Migrate
        dotnet run --project $project.FullName -c Release -f $targetFramework -- --connection-string $connectionString Schema
        dotnet run --project $project.FullName -c Release -f $targetFramework -- --connection-string $connectionString @promptArgs @extraArgs ResetAndData
        continue
    }

    Write-Host "Running $projectName migrations ($migrationCommand) against $connectionTarget."
    dotnet run --project $project.FullName -c Release -f $targetFramework -- --connection-string $connectionString @promptArgs @extraArgs $migrationCommand
}
