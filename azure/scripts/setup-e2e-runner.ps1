#Requires -Version 7
[CmdletBinding()]
param (
    [Alias('g')]
    [Parameter(Mandatory)]
    [string] $ResourceGroup,

    [Alias('a')]
    [string] $AppsettingsPath,

    [Alias('k')]
    [string] $KeyVaultName,

    [Alias('p')]
    [string] $ProductsAppName,

    [Alias('s')]
    [string] $ShoppingAppName,

    [Alias('i')]
    [Alias('SkipCertificateValidation')]
    [switch] $Insecure,

    [switch] $SkipValidation
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot  = Resolve-Path (Join-Path $scriptDir '../..')

if (-not $AppsettingsPath) {
    $AppsettingsPath = Join-Path $repoRoot 'samples/tests/Contoso.E2E.Runner/appsettings.json'
}

# Verify prerequisites.
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI 'az' is not installed or not on PATH."
    exit 1
}

if (-not (Test-Path $AppsettingsPath)) {
    Write-Error "The E2E runner appsettings file was not found at '$AppsettingsPath'."
    exit 1
}

function Invoke-ValidateRequest {
    param (
        [string] $Label,
        [string] $Url,
        [string] $Method = 'GET',
        [switch] $Insecure
    )

    $code = $null
    try {
        $invokeWebRequestParams = @{
            Uri             = $Url
            Method          = $Method
            UseBasicParsing = $true
            ErrorAction     = 'Stop'
        }

        if ($Insecure) {
            $invokeWebRequestParams['SkipCertificateCheck'] = $true
        }

        $response = Invoke-WebRequest @invokeWebRequestParams
        $code = [int]$response.StatusCode
    }
    catch [Microsoft.PowerShell.Commands.HttpResponseException] {
        $code = [int]$_.Exception.Response.StatusCode
    }
    catch {
        Write-Error "Validation failed for ${Label}: ${Method} ${Url} — $($_.Exception.Message)"
        exit 1
    }

    if (-not $code -or $code -lt 200 -or $code -ge 400) {
        Write-Error "Validation failed for ${Label}: ${Method} ${Url} returned HTTP ${code}."
        exit 1
    }

    Write-Host "Validated ${Label}: ${Method} ${Url} (${code})"
}

# Auto-detect app names.
if (-not $ProductsAppName) {
    $ProductsAppName = (az webapp list --resource-group $ResourceGroup --query "[?contains(name, 'products-api')].name | [0]" -o tsv)
}

if (-not $ShoppingAppName) {
    $ShoppingAppName = (az webapp list --resource-group $ResourceGroup --query "[?contains(name, 'shopping-api')].name | [0]" -o tsv)
}

if (-not $ProductsAppName -or -not $ShoppingAppName) {
    Write-Error "Unable to auto-detect the Products or Shopping API app names in resource group '$ResourceGroup'. Re-run with -ProductsAppName and -ShoppingAppName."
    exit 1
}

$productsHost = (az webapp show --resource-group $ResourceGroup --name $ProductsAppName --query defaultHostName -o tsv)
$shoppingHost = (az webapp show --resource-group $ResourceGroup --name $ShoppingAppName --query defaultHostName -o tsv)

if (-not $productsHost -or -not $shoppingHost) {
    Write-Error "Unable to resolve one or more app host names."
    exit 1
}

# Auto-detect Key Vault.
if (-not $KeyVaultName) {
    $KeyVaultName = (az keyvault list --resource-group $ResourceGroup --query '[0].name' -o tsv)
}

if (-not $KeyVaultName) {
    Write-Error "Unable to auto-detect the Key Vault in resource group '$ResourceGroup'. Re-run with -KeyVaultName <name>."
    exit 1
}

$postgresConnectionString = (az keyvault secret show --vault-name $KeyVaultName --name postgres-connection-string --query value -o tsv)
$sqlConnectionString      = (az keyvault secret show --vault-name $KeyVaultName --name sql-connection-string      --query value -o tsv)

if (-not $postgresConnectionString -or -not $sqlConnectionString) {
    Write-Error "Unable to retrieve one or more connection strings from Key Vault '$KeyVaultName'."
    exit 1
}

# Validate endpoints.
if (-not $SkipValidation) {
    Invoke-ValidateRequest -Label 'Products API'     -Url "https://${productsHost}/api/products" -Insecure:$Insecure
    Invoke-ValidateRequest -Label 'Shopping API'     -Url "https://${shoppingHost}/api/customers/test/baskets" -Method POST -Insecure:$Insecure
    Invoke-ValidateRequest -Label 'Products health'  -Url "https://${productsHost}/health/ready/detailed" -Insecure:$Insecure
    Invoke-ValidateRequest -Label 'Products swagger' -Url "https://${productsHost}/swagger" -Insecure:$Insecure
    Invoke-ValidateRequest -Label 'Shopping health'  -Url "https://${shoppingHost}/health/ready/detailed" -Insecure:$Insecure
    Invoke-ValidateRequest -Label 'Shopping swagger' -Url "https://${shoppingHost}/swagger" -Insecure:$Insecure
}

# Update appsettings.json.
$backupPath = "${AppsettingsPath}.bak"
Copy-Item -Path $AppsettingsPath -Destination $backupPath -Force

$settings = Get-Content $AppsettingsPath -Raw | ConvertFrom-Json -AsHashtable

if (-not $settings.ContainsKey('E2E'))                          { $settings['E2E'] = @{} }
if (-not $settings['E2E'].ContainsKey('Products'))              { $settings['E2E']['Products'] = @{} }
if (-not $settings['E2E'].ContainsKey('Shopping'))              { $settings['E2E']['Shopping'] = @{} }

$settings['E2E']['Products']['BaseAddress']      = "https://${productsHost}"
$settings['E2E']['Products']['ConnectionString'] = $postgresConnectionString
$settings['E2E']['Shopping']['BaseAddress']      = "https://${shoppingHost}"
$settings['E2E']['Shopping']['ConnectionString'] = $sqlConnectionString

$settings | ConvertTo-Json -Depth 10 | Set-Content -Path $AppsettingsPath -Encoding utf8

Write-Host "Updated E2E runner configuration: $AppsettingsPath"
Write-Host "Backup created: $backupPath"
Write-Host "Products BaseAddress: https://$productsHost"
Write-Host "Shopping BaseAddress: https://$shoppingHost"
Write-Host "Key Vault: $KeyVaultName"
Write-Host ""
Write-Host "Next step:"
Write-Host "  cd $repoRoot/samples/tests/Contoso.E2E.Runner"
$targetFramework = $env:AZD_DOTNET_TARGET_FRAMEWORK ?? $env:DOTNET_TARGET_FRAMEWORK
Write-Host "  dotnet run --framework `"$targetFramework`""
