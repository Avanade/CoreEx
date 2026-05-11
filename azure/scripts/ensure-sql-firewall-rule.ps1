$ErrorActionPreference = 'Stop'

# Ensures the current runner public IP has an Azure SQL firewall rule.

if (-not (Get-Command azd -ErrorAction SilentlyContinue)) {
    throw "The 'azd' command is required to resolve environment values."
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "The 'az' command is required to manage Azure SQL firewall rules."
}

function Invoke-WithRetry {
    param(
        [Parameter(Mandatory = $true)]
        [int] $Attempts,
        [Parameter(Mandatory = $true)]
        [int] $DelaySeconds,
        [Parameter(Mandatory = $true)]
        [scriptblock] $ScriptBlock,
        [Parameter(Mandatory = $true)]
        [string] $Description
    )

    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            & $ScriptBlock
            return
        }
        catch {
            if ($attempt -ge $Attempts) {
                throw
            }

            Write-Host "Waiting on $Description ($attempt/$Attempts); retrying in $DelaySeconds seconds."
            Start-Sleep -Seconds $DelaySeconds
        }
    }
}

$sqlServer = (azd env get-value sqlServerName).Trim()
$azureResourceGroup = (azd env get-value AZURE_RESOURCE_GROUP).Trim()
$azureSubscriptionId = (azd env get-value AZURE_SUBSCRIPTION_ID).Trim()
$azureEnvName = (azd env get-value AZURE_ENV_NAME).Trim()

if ([string]::IsNullOrWhiteSpace($sqlServer) -or [string]::IsNullOrWhiteSpace($azureResourceGroup)) {
    throw 'Unable to resolve sqlServerName/AZURE_RESOURCE_GROUP from the active azd environment.'
}

$clientIp = ''
foreach ($ipLookup in @('https://api.ipify.org', 'https://ifconfig.me/ip')) {
    try {
        $clientIp = (Invoke-RestMethod -Uri $ipLookup -TimeoutSec 15).ToString().Trim()
    }
    catch {
        $clientIp = ''
    }

    if (-not [string]::IsNullOrWhiteSpace($clientIp)) {
        break
    }
}

if ([string]::IsNullOrWhiteSpace($clientIp)) {
    throw 'Unable to determine the public IP address for the current runner.'
}

if ($clientIp -notmatch '^([0-9]{1,3}\.){3}[0-9]{1,3}$') {
    throw "Resolved public IP '$clientIp' is not a valid IPv4 address."
}

$effectiveEnvName = if ([string]::IsNullOrWhiteSpace($azureEnvName)) { 'env' } else { $azureEnvName }
$firewallRuleName = "azd-$effectiveEnvName-$($clientIp -replace '\.', '-')"
$azServerArgs = @('--resource-group', $azureResourceGroup, '--name', $sqlServer)
$azFirewallArgs = @('--resource-group', $azureResourceGroup, '--server', $sqlServer)
if (-not [string]::IsNullOrWhiteSpace($azureSubscriptionId)) {
    $azServerArgs += @('--subscription', $azureSubscriptionId)
    $azFirewallArgs += @('--subscription', $azureSubscriptionId)
}

if ($env:AZD_SQL_FIREWALL_WAIT_FOR_SERVER -eq '1') {
    Write-Host "Waiting for Azure SQL server '$sqlServer' to become available."
    Invoke-WithRetry -Attempts 12 -DelaySeconds 10 -Description 'Azure SQL server readiness' -ScriptBlock {
        az sql server show @azServerArgs | Out-Null
    }
}
else {
    az sql server show @azServerArgs | Out-Null
}

az sql server firewall-rule show @azFirewallArgs --name $firewallRuleName *> $null
if ($LASTEXITCODE -eq 0) {
    Write-Host "Updating Azure SQL firewall rule '$firewallRuleName' for $clientIp."
    az sql server firewall-rule update @azFirewallArgs --name $firewallRuleName --start-ip-address $clientIp --end-ip-address $clientIp | Out-Null
}
else {
    Write-Host "Creating Azure SQL firewall rule '$firewallRuleName' for $clientIp."
    az sql server firewall-rule create @azFirewallArgs --name $firewallRuleName --start-ip-address $clientIp --end-ip-address $clientIp | Out-Null
}

Write-Host "Azure SQL firewall rule '$firewallRuleName' is ready."