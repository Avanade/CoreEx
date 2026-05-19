$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$infraDir = Resolve-Path (Join-Path $scriptDir '..')

if ([string]::IsNullOrWhiteSpace($env:AZURE_SQL_ADMIN_PASSWORD)) {
	throw 'AZURE_SQL_ADMIN_PASSWORD is not set. Set it before running azd provision.'
}

if ([string]::IsNullOrWhiteSpace($env:AZURE_POSTGRES_ADMIN_PASSWORD)) {
    $env:AZURE_POSTGRES_ADMIN_PASSWORD = $env:AZURE_SQL_ADMIN_PASSWORD
}

if ([string]::IsNullOrWhiteSpace($env:AZURE_LOCATION)) {
    throw "AZURE_LOCATION is not set. Set it via 'azd env set AZURE_LOCATION <region>' before running azd provision."
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
    throw 'Unable to determine the current public IP address for the Azure SQL firewall rule.'
}

if ($clientIp -notmatch '^([0-9]{1,3}\.){3}[0-9]{1,3}$') {
    throw "Resolved public IP '$clientIp' is not a valid IPv4 address."
}

$targetFramework = if ($env:AZD_DOTNET_TARGET_FRAMEWORK) {
    $env:AZD_DOTNET_TARGET_FRAMEWORK
}
elseif ($env:DOTNET_TARGET_FRAMEWORK) {
    $env:DOTNET_TARGET_FRAMEWORK
}
else {
    'net8.0'
}

$appServiceLinuxFxVersion = switch ($targetFramework) {
    'net8.0' { 'DOTNETCORE|8.0' }
    'net9.0' { 'DOTNETCORE|9.0' }
    'net10.0' { 'DOTNETCORE|10.0' }
    default { throw "Unsupported target framework '$targetFramework'. Expected net8.0, net9.0, or net10.0." }
}

$templatePath = Join-Path $infraDir 'main.dev.parameters.json'
$outputPath = Join-Path $infraDir 'main.parameters.json'

# Try using ConvertFrom-Json / ConvertTo-Json for safe JSON processing
try {
    $json = Get-Content -Raw -Path $templatePath | ConvertFrom-Json
    $json.parameters.location.value = $env:AZURE_LOCATION
    $json.parameters.sqlAdminPassword.value = $env:AZURE_SQL_ADMIN_PASSWORD
    $json.parameters.postgresAdminPassword.value = $env:AZURE_POSTGRES_ADMIN_PASSWORD
    $json.parameters.appServiceLinuxFxVersion.value = $appServiceLinuxFxVersion
    $json.parameters.sqlFirewallClientIp.value = $clientIp
    $json.parameters.postgresFirewallClientIp.value = $clientIp
    $json | ConvertTo-Json -Depth 100 | Set-Content -Path $outputPath -NoNewline
} catch {
    # Fallback: direct string replacement
    $content = Get-Content -Raw -Path $templatePath
    $content = $content.Replace('__AZURE_LOCATION__', $env:AZURE_LOCATION)
    $content = $content.Replace('__AZURE_SQL_ADMIN_PASSWORD__', $env:AZURE_SQL_ADMIN_PASSWORD)
    $content = $content.Replace('__AZURE_POSTGRES_ADMIN_PASSWORD__', $env:AZURE_POSTGRES_ADMIN_PASSWORD)
    $content = $content.Replace('__APP_SERVICE_LINUX_FX_VERSION__', $appServiceLinuxFxVersion)
    $content = $content.Replace('__AZURE_CLIENT_IP__', $clientIp)
    Set-Content -Path $outputPath -Value $content -NoNewline
}
