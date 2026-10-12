<#
.SYNOPSIS
    Recovers the local Service Bus emulator after a crash-loop.

.DESCRIPTION
    The 'servicebus-emulator' container occasionally crash-loops (exit code 139 / SIGSEGV) because it
    depends on 'db-sql-server' for its backing message-container databases (e.g. SbMessageContainerDatabase00001)
    and neither container has a persistent volume configured. If 'db-sql-server' is recreated (or its data
    directory otherwise resets) while stale/mismatched database files remain, or the emulator's expected
    databases get out of sync with what's actually in SQL Server, the emulator's startup 'ALTER DATABASE'
    step fails and it crashes hard on every subsequent start.

    The fix is to recreate BOTH containers together - never just the emulator alone - so they reinitialize
    from a consistent, empty state.

.NOTES
    Run this from anywhere; it locates the repo root (parent of this script's folder) to find docker-compose.yml.
    Prefers podman compose; falls back to docker compose if podman is not available.
#>

[CmdletBinding()]
param(
    [switch]$UseDocker
)

$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repoRoot 'docker-compose.yml'

if (-not (Test-Path $composeFile)) {
    throw "Could not find docker-compose.yml at '$composeFile'."
}

$composeCmd = if ($UseDocker) { 'docker' } else { 'podman' }

if (-not (Get-Command $composeCmd -ErrorAction SilentlyContinue)) {
    Write-Warning "'$composeCmd' not found on PATH; falling back to 'docker'."
    $composeCmd = 'docker'
}

function Invoke-Compose {
    param([string[]]$Arguments)
    Write-Host "> $composeCmd compose -f `"$composeFile`" $($Arguments -join ' ')" -ForegroundColor Cyan
    & $composeCmd compose -f $composeFile @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE"
    }
}

Write-Host "== Stopping db-sql-server and servicebus-emulator ==" -ForegroundColor Yellow
Invoke-Compose @('stop', 'db-sql-server', 'servicebus-emulator')

Write-Host "== Removing db-sql-server and servicebus-emulator containers ==" -ForegroundColor Yellow
Invoke-Compose @('rm', '-f', 'db-sql-server', 'servicebus-emulator')

Write-Host "== Recreating both containers together ==" -ForegroundColor Yellow
Invoke-Compose @('up', '-d', 'db-sql-server', 'servicebus-emulator')

Write-Host "== Waiting for SQL Server + Service Bus emulator init (up to ~60s) ==" -ForegroundColor Yellow
$healthy = $false
$status = $null
for ($i = 0; $i -lt 12; $i++) {
    Start-Sleep -Seconds 5
    # Scope to this compose project only (name=servicebus-emulator alone would also match other unrelated repos' containers on the same host).
    $status = & $composeCmd compose -f $composeFile ps servicebus-emulator --format '{{.Status}}' 2>$null
    if ($status -match '^Up') {
        $healthy = $true
        break
    }
    if ($status -match 'Exited') {
        Write-Host "  Still initializing/crash-looping ($status), waiting..." -ForegroundColor DarkYellow
    }
}

Write-Host ""
if ($healthy) {
    Write-Host "servicebus-emulator is up: $status" -ForegroundColor Green
} else {
    Write-Warning "servicebus-emulator did not report 'Up' within the wait window. Check logs with:"
    Write-Warning "  $composeCmd logs coreex-servicebus-emulator-1 --tail 60"
}

Write-Host ""
Write-Host "== Current container status ==" -ForegroundColor Yellow
& $composeCmd compose -f $composeFile ps
