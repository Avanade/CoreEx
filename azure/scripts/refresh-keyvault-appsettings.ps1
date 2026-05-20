#Requires -Version 7
[CmdletBinding()]
param (
    [Alias('g')]
    [Parameter(Mandatory)]
    [string] $ResourceGroup,

    [Alias('n')]
    [string[]] $AppName,

    [Alias('w')]
    [ValidateRange(0, 600)]
    [int] $WaitAfterRestartSeconds = 20
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "Azure CLI 'az' is not installed or not on PATH."
}

$appNames = @()
if ($AppName -and $AppName.Count -gt 0) {
    $appNames = $AppName
}
else {
    $appNames = @(az webapp list --resource-group $ResourceGroup --query '[].name' -o tsv)
}

$appNames = @($appNames | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })

if ($appNames.Count -eq 0) {
    throw "No web apps found in resource group '$ResourceGroup'."
}

foreach ($app in $appNames) {
    Write-Host "Refreshing Key Vault app settings references for '$app'."

    $appId = (az webapp show --resource-group $ResourceGroup --name $app --query id -o tsv)
    if ([string]::IsNullOrWhiteSpace($appId)) {
        throw "Unable to resolve app id for '$app'."
    }

    az rest --method post --url "https://management.azure.com${appId}/config/configreferences/appsettings/refresh?api-version=2023-12-01" --output none

    Write-Host "Restarting '$app'."
    az webapp restart --resource-group $ResourceGroup --name $app --output none

    if ($WaitAfterRestartSeconds -gt 0) {
        Write-Host "Waiting $WaitAfterRestartSeconds seconds for '$app' startup."
        Start-Sleep -Seconds $WaitAfterRestartSeconds
    }
}

Write-Host "Completed Key Vault app settings refresh for $($appNames.Count) app(s)."
