#Requires -Version 7
[CmdletBinding()]
param (
    [Alias('g')]
    [Parameter(Mandatory)]
    [string] $ResourceGroup,

    [Alias('n')]
    [string] $DashboardAppName,

    [Alias('t')]
    [int] $TokenTimeoutSeconds = 20,

    [int] $LogLookbackMinutes = 60
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-TokenFromRuntimeLog {
    param (
        [string] $PublishUser,
        [string] $PublishPassword,
        [string] $AppName,
        [long]   $CutoffEpoch
    )

    $payload  = '{"command":"grep \"Login to the dashboard\" /appsvctmp/volatile/logs/runtime/container.log","dir":"/home"}'
    $base64   = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${PublishUser}:${PublishPassword}"))
    $headers  = @{ Authorization = "Basic $base64"; 'Content-Type' = 'application/json' }
    $scmUrl   = "https://${AppName}.scm.azurewebsites.net/api/command"

    try {
        $response = Invoke-RestMethod -Uri $scmUrl -Method Post -Headers $headers -Body $payload -ErrorAction Stop
    }
    catch {
        return $null
    }

    $output = $response.Output
    if (-not $output) { return $null }

    # Match timestamp + token on the same line.
    if ($output -match '(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.\d+)?Z[^"]*login\?t=([A-Za-z0-9]+)') {
        $tsString  = $Matches[1] + 'Z'
        $entryTime = [DateTimeOffset]::Parse($tsString, $null, [Globalization.DateTimeStyles]::AssumeUniversal)
        $entryEpoch = $entryTime.ToUnixTimeSeconds()
        if ($entryEpoch -ge $CutoffEpoch) {
            return $Matches[2]
        }
    }

    return $null
}

function Get-TokenFromLogArchive {
    param (
        [string] $PublishUser,
        [string] $PublishPassword,
        [string] $AppName,
        [long]   $CutoffEpoch
    )

    $base64  = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("${PublishUser}:${PublishPassword}"))
    $headers = @{ Authorization = "Basic $base64" }
    $scmUrl  = "https://${AppName}.scm.azurewebsites.net/api/zip/LogFiles/"
    $tmpZip  = [IO.Path]::GetTempFileName() + '.zip'
    $tmpDir  = [IO.Path]::Combine([IO.Path]::GetTempPath(), [IO.Path]::GetRandomFileName())

    try {
        try {
            Invoke-WebRequest -Uri $scmUrl -Headers $headers -OutFile $tmpZip -ErrorAction Stop
        }
        catch {
            return $null
        }

        $null = New-Item -ItemType Directory -Path $tmpDir -Force
        Expand-Archive -Path $tmpZip -DestinationPath $tmpDir -Force

        $files = Get-ChildItem -Path $tmpDir -Recurse -File

        foreach ($file in $files) {
            $lines = Get-Content -Path $file.FullName -ErrorAction SilentlyContinue
            foreach ($line in $lines) {
                if ($line -match '^(\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2})(?:\.\d+)?Z.*login\?t=([A-Za-z0-9]+)') {
                    $tsString  = $Matches[1] + 'Z'
                    $entryTime = [DateTimeOffset]::Parse($tsString, $null, [Globalization.DateTimeStyles]::AssumeUniversal)
                    if ($entryTime.ToUnixTimeSeconds() -ge $CutoffEpoch) {
                        return $Matches[2]
                    }
                }
            }
        }
    }
    finally {
        if (Test-Path $tmpZip)  { Remove-Item $tmpZip  -Force -ErrorAction SilentlyContinue }
        if (Test-Path $tmpDir)  { Remove-Item $tmpDir  -Recurse -Force -ErrorAction SilentlyContinue }
    }

    return $null
}

# Verify az CLI.
if (-not (Get-Command az -ErrorAction SilentlyContinue)) {
    Write-Error "Azure CLI 'az' is not installed or not on PATH."
    exit 1
}

# Auto-detect dashboard app if not supplied.
if (-not $DashboardAppName) {
    $DashboardAppName = (az webapp list --resource-group $ResourceGroup --query "[?contains(name, 'aspire-dashboard')].name | [0]" -o tsv)
}

if (-not $DashboardAppName) {
    Write-Error "Unable to auto-detect the dashboard app name in resource group '$ResourceGroup'. Re-run with -DashboardAppName <name>."
    exit 1
}

$hostName = (az webapp show --resource-group $ResourceGroup --name $DashboardAppName --query defaultHostName -o tsv)

if (-not $hostName) {
    Write-Error "Unable to resolve dashboard host name for app '$DashboardAppName'."
    exit 1
}

$cutoffEpoch = [DateTimeOffset]::UtcNow.AddMinutes(-$LogLookbackMinutes).ToUnixTimeSeconds()

# Attempt 1: query runtime container log via SCM Kudu.
$token = $null
try {
    $creds        = (az webapp deployment list-publishing-credentials --resource-group $ResourceGroup --name $DashboardAppName --output json 2>$null | ConvertFrom-Json)
    $publishUser  = $creds.publishingUserName
    $publishPass  = $creds.publishingPassword

    if ($publishUser -and $publishPass) {
        $token = Get-TokenFromRuntimeLog -PublishUser $publishUser -PublishPassword $publishPass -AppName $DashboardAppName -CutoffEpoch $cutoffEpoch

        # Attempt 2: fall back to archived log files.
        if (-not $token) {
            $token = Get-TokenFromLogArchive -PublishUser $publishUser -PublishPassword $publishPass -AppName $DashboardAppName -CutoffEpoch $cutoffEpoch
        }
    }
}
catch {
    # Non-fatal; will fall through to live tail.
}

# Attempt 3: live log tail.
if (-not $token) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TokenTimeoutSeconds)
    $job = Start-Job -ScriptBlock {
        param($rg, $app)
        az webapp log tail --resource-group $rg --name $app 2>&1
    } -ArgumentList $ResourceGroup, $DashboardAppName

    while ([DateTime]::UtcNow -lt $deadline -and $job.State -eq 'Running') {
        $partial = Receive-Job -Job $job -Keep 2>$null
        $partialText = ($partial | Out-String)
        if ($partialText -match 'login\?t=([A-Za-z0-9]+)') {
            $token = $Matches[1]
            break
        }
        Start-Sleep -Milliseconds 500
    }
    Stop-Job  -Job $job -ErrorAction SilentlyContinue
    Remove-Job -Job $job -Force -ErrorAction SilentlyContinue
}

Write-Host "Dashboard app: $DashboardAppName"
Write-Host "Dashboard URL: https://$hostName"

if ($token) {
    Write-Host "Login URL: https://${hostName}/login?t=${token}"
}
else {
    Write-Host "Token not found in the last $LogLookbackMinutes minutes of logs or within ${TokenTimeoutSeconds}s of live tailing."
    Write-Host "Open the dashboard URL and, if prompted, run this script again with a larger -TokenTimeoutSeconds value."
}
