$ErrorActionPreference = 'Stop'

# Ensures the current runner public IP has Azure SQL and PostgreSQL firewall rules.

if (-not (Get-Command azd -ErrorAction SilentlyContinue)) {
    throw "The 'azd' command is required to resolve environment values."
}

if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    throw "The 'az' command is required to manage Azure firewall rules."
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

function Ensure-FirewallRule {
    param(
        [Parameter(Mandatory = $true)]
        [string] $DbType,
        [Parameter(Mandatory = $true)]
        [string] $ServerName,
        [Parameter(Mandatory = $true)]
        [string] $ClientIp,
        [Parameter(Mandatory = $true)]
        [string] $FirewallRuleName,
        [Parameter(Mandatory = $true)]
        [string[]] $AzArgs
    )

    if ($DbType -eq 'sql') {
        az sql server firewall-rule show @AzArgs --name $FirewallRuleName *> $null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Updating Azure SQL firewall rule '$FirewallRuleName' for $ClientIp."
            az sql server firewall-rule update @AzArgs --name $FirewallRuleName --start-ip-address $ClientIp --end-ip-address $ClientIp | Out-Null
        }
        else {
            Write-Host "Creating Azure SQL firewall rule '$FirewallRuleName' for $ClientIp."
            az sql server firewall-rule create @AzArgs --name $FirewallRuleName --start-ip-address $ClientIp --end-ip-address $ClientIp | Out-Null
        }
        Write-Host "Azure SQL firewall rule '$FirewallRuleName' is ready."
    }
    elseif ($DbType -eq 'postgres') {
        az postgres flexible-server firewall-rule show @AzArgs --rule-name $FirewallRuleName *> $null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Updating Azure PostgreSQL firewall rule '$FirewallRuleName' for $ClientIp."
            az postgres flexible-server firewall-rule update @AzArgs --rule-name $FirewallRuleName --start-ip-address $ClientIp --end-ip-address $ClientIp | Out-Null
        }
        else {
            Write-Host "Creating Azure PostgreSQL firewall rule '$FirewallRuleName' for $ClientIp."
            az postgres flexible-server firewall-rule create @AzArgs --rule-name $FirewallRuleName --start-ip-address $ClientIp --end-ip-address $ClientIp | Out-Null
        }
        Write-Host "Azure PostgreSQL firewall rule '$FirewallRuleName' is ready."
    }
}

function Get-AzdEnvValue {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Key
    )

    $value = (azd env get-value $Key 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $value.StartsWith('ERROR:')) {
        return ''
    }

    return $value
}

$sqlServer = Get-AzdEnvValue -Key 'sqlServerName'
$postgresServer = Get-AzdEnvValue -Key 'postgresServerName'
$azureResourceGroup = Get-AzdEnvValue -Key 'AZURE_RESOURCE_GROUP'
$azureSubscriptionId = Get-AzdEnvValue -Key 'AZURE_SUBSCRIPTION_ID'
$azureEnvName = Get-AzdEnvValue -Key 'AZURE_ENV_NAME'

if (([string]::IsNullOrWhiteSpace($sqlServer) -and [string]::IsNullOrWhiteSpace($postgresServer)) -or [string]::IsNullOrWhiteSpace($azureResourceGroup)) {
    throw 'Unable to resolve sqlServerName and/or postgresServerName and AZURE_RESOURCE_GROUP from the active azd environment.'
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

if (-not [string]::IsNullOrWhiteSpace($sqlServer)) {
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

    Ensure-FirewallRule -DbType 'sql' -ServerName $sqlServer -ClientIp $clientIp -FirewallRuleName $firewallRuleName -AzArgs $azFirewallArgs
}

if (-not [string]::IsNullOrWhiteSpace($postgresServer)) {
    $azPostgresServerArgs = @('--resource-group', $azureResourceGroup, '--name', $postgresServer)
    $azPostgresFirewallArgs = @('--resource-group', $azureResourceGroup, '--name', $postgresServer)
    if (-not [string]::IsNullOrWhiteSpace($azureSubscriptionId)) {
        $azPostgresServerArgs += @('--subscription', $azureSubscriptionId)
        $azPostgresFirewallArgs += @('--subscription', $azureSubscriptionId)
    }

    if ($env:AZD_POSTGRES_FIREWALL_WAIT_FOR_SERVER -eq '1') {
        Write-Host "Waiting for Azure PostgreSQL server '$postgresServer' to become available."
        Invoke-WithRetry -Attempts 12 -DelaySeconds 10 -Description 'Azure PostgreSQL server readiness' -ScriptBlock {
            az postgres flexible-server show @azPostgresServerArgs | Out-Null
        }
    }
    else {
        az postgres flexible-server show @azPostgresServerArgs | Out-Null
    }

    Ensure-FirewallRule -DbType 'postgres' -ServerName $postgresServer -ClientIp $clientIp -FirewallRuleName $firewallRuleName -AzArgs $azPostgresFirewallArgs
}