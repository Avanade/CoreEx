$ErrorActionPreference = 'Stop'

# Runs all Contoso *.Database DbEx migrations against the provisioned Azure SQL database.

$scriptDir = Split-Path -Parent $PSCommandPath
$azureDir = Resolve-Path (Join-Path $scriptDir '..')
$repoRoot = Resolve-Path (Join-Path $azureDir '..')

if (-not (Get-Command azd -ErrorAction SilentlyContinue)) {
    throw "The 'azd' command is required to resolve environment values."
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
$sqlServer = (azd env get-value sqlServerName).Trim()
$sqlDatabase = (azd env get-value sqlDatabaseName).Trim()
$sqlAdminLogin = if ($env:AZURE_SQL_ADMIN_LOGIN) { $env:AZURE_SQL_ADMIN_LOGIN } else { 'coreexadmin' }
$sqlPassword = $env:AZURE_SQL_ADMIN_PASSWORD

if ([string]::IsNullOrWhiteSpace($sqlServer) -or [string]::IsNullOrWhiteSpace($sqlDatabase)) {
    throw 'Unable to resolve sqlServerName/sqlDatabaseName from the active azd environment.'
}

if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
    $sqlPassword = (azd env get-value AZURE_SQL_ADMIN_PASSWORD).Trim()
}

if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
    throw 'AZURE_SQL_ADMIN_PASSWORD is required to run DbEx migrations.'
}

& (Join-Path $scriptDir 'ensure-sql-firewall-rule.ps1')

$connectionString = "Server=tcp:$sqlServer.database.windows.net,1433;Initial Catalog=$sqlDatabase;User ID=$sqlAdminLogin;Password=$sqlPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$projects = Get-ChildItem -LiteralPath (Join-Path $repoRoot 'samples/src') -Recurse -File -Filter 'Contoso.*.Database.csproj' |
    Sort-Object FullName

if ($projects.Count -eq 0) {
    throw 'No Contoso database projects were found under samples/src.'
}

Write-Host "Running DbEx migrations for $($projects.Count) database project(s) using framework '$targetFramework' against database '$sqlDatabase'."
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

    Write-Host "Running $projectName migrations ($migrationCommand)."
    dotnet run --project $project.FullName -c Release -f $targetFramework -- --connection-string $connectionString @promptArgs @extraArgs $migrationCommand
}
