#!/usr/bin/env pwsh
<#
.SYNOPSIS
Removes generated Debug and Release build outputs across the repository to recover disk space.

.DESCRIPTION
Deletes the per-configuration directories beneath each project's bin and obj directories (for example
bin\Debug, bin\Release, obj\Debug and obj\Release). NuGet restore assets directly within obj (such as
project.assets.json) are retained, so a subsequent build does not require a restore.

Source, NuGet caches, containers and database volumes are never touched. All outputs are regenerated on
the next build; the trade-off is losing incremental-build state, so that build is slower.

Use -WhatIf to preview what would be removed.

.PARAMETER Configuration
The build configurations to remove. Defaults to Debug and Release.

.PARAMETER ShutdownBuildServers
Shuts down MSBuild/compiler build servers first to release any file locks.

.EXAMPLE
./clean-build-outputs.ps1
./clean-build-outputs.ps1 -WhatIf
./clean-build-outputs.ps1 -Configuration Release
./clean-build-outputs.ps1 -ShutdownBuildServers
#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [string[]]$Configuration = @("Debug", "Release"),
    [switch]$ShutdownBuildServers = $false
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$searchRoots = @("src", "tests", "samples", "gen", "tools") |
    ForEach-Object { Join-Path $repoRoot $_ } |
    Where-Object { Test-Path $_ }

if ($ShutdownBuildServers -and $PSCmdlet.ShouldProcess("dotnet build servers", "Shut down")) {
    dotnet build-server shutdown | Out-Null
}

# Template content is raw dotnet new input and never contains build output; skip it explicitly.
$templateContent = Join-Path $repoRoot (Join-Path "src" (Join-Path "CoreEx.Template" "content"))

$targets = foreach ($root in $searchRoots) {
    Get-ChildItem -LiteralPath $root -Directory -Recurse -ErrorAction SilentlyContinue |
        Where-Object { $_.Name -in @("bin", "obj") -and -not $_.FullName.StartsWith($templateContent, [StringComparison]::OrdinalIgnoreCase) } |
        ForEach-Object {
            foreach ($config in $Configuration) {
                $path = Join-Path $_.FullName $config
                if (Test-Path -LiteralPath $path) { Get-Item -LiteralPath $path }
            }
        }
}

if (-not $targets) {
    Write-Output "Nothing to clean for configuration(s): $($Configuration -join ', ')."
    return
}

$totalBytes = 0L
$removed = 0
$failed = @()

foreach ($target in $targets) {
    $bytes = (Get-ChildItem -LiteralPath $target.FullName -Recurse -File -Force -ErrorAction SilentlyContinue | Measure-Object Length -Sum).Sum
    $relative = $target.FullName.Substring($repoRoot.Length).TrimStart('\', '/')

    if ($PSCmdlet.ShouldProcess($relative, "Remove ($([math]::Round($bytes / 1MB, 1)) MB)")) {
        try {
            Remove-Item -LiteralPath $target.FullName -Recurse -Force
            $totalBytes += $bytes
            $removed++
        }
        catch {
            $failed += $relative
            Write-Warning "Could not remove '$relative' (likely locked by a running process): $($_.Exception.Message)"
        }
    }
}

if ($WhatIfPreference) { return }

Write-Output "Removed $removed director$(if ($removed -eq 1) { 'y' } else { 'ies' }); recovered $([math]::Round($totalBytes / 1GB, 2)) GB."
if ($failed) {
    Write-Warning "$($failed.Count) director$(if ($failed.Count -eq 1) { 'y' } else { 'ies' }) could not be removed. Close Visual Studio/running hosts, or rerun with -ShutdownBuildServers."
    exit 1
}
