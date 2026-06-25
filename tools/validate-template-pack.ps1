#!/usr/bin/env pwsh
<#
.SYNOPSIS
Validates the CoreEx.Template pack by scaffolding parameter combinations and asserting expected outputs.

.DESCRIPTION
This script validates the CoreEx.Template pack by:
1. Building and installing the template package from source.
2. Scaffolding each template+parameter combination into a temp directory.
3. Asserting expected files are present/absent and file content matches.
4. Running dotnet build on code-emitting templates to confirm compilation.
5. Cleaning up temporary test directories (unless -SkipCleanup).

Runs on Windows, Linux, and macOS via pwsh (cross-platform PowerShell).

.PARAMETER SkipCleanup
If specified, temporary test directories are not cleaned up after validation.

.PARAMETER NoRebuild
If specified, the template pack is not rebuilt before validation (uses existing nupkg).

.EXAMPLE
./validate-template-pack.ps1
./validate-template-pack.ps1 -SkipCleanup
./validate-template-pack.ps1 -NoRebuild
#>

[CmdletBinding()]
param(
    [switch]$SkipCleanup = $false,
    [switch]$NoRebuild = $false
)

$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

$repoRoot = Split-Path -Parent $PSScriptRoot
$templateProjectPath = Join-Path $repoRoot "src/CoreEx.Template"
# Prefer RUNNER_TEMP (set by GitHub Actions) to stay on a well-known short path in CI.
# Fall back to the system temp dir for local runs. Both avoid the deep repo worktree path
# which exceeds Windows MAX_PATH (260 chars) when build output is factored in.
$tempBase = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [System.IO.Path]::GetTempPath() }
$temporaryTestRoot = Join-Path $tempBase "cxval-$(Get-Date -Format 'yyyyMMdd-HHmmss')"

Write-Verbose "Repository root: $repoRoot"
Write-Verbose "Template project: $templateProjectPath"
Write-Verbose "Test root: $temporaryTestRoot"

# ---------------------------------------------------------------------------
# Test scenarios
# ---------------------------------------------------------------------------
# Each scenario may declare:
#   FilesPresent  - paths relative to the output dir that MUST exist
#   FilesAbsent   - paths relative to the output dir that MUST NOT exist
#   FileContains  - hashtable of relative-path => required substring
#   Build         - if $true, run dotnet build on the output dir after scaffolding
# ---------------------------------------------------------------------------
$testScenarios = @(
    @{
        Name       = "coreex-ai-single-repo"
        Template   = "coreex-ai"
        Parameters = @{}
        TestPath   = "test-ai-single"
        Verify     = @{
            FilesPresent = @(
                ".github/instructions/coreex.instructions.md"
                ".github/instructions/coreex-api-controllers.instructions.md"
                ".github/instructions/coreex-conventions.instructions.md"
                ".github/instructions/coreex-validators.instructions.md"
                ".github/prompts/coreex-scaffold.prompt.md"
                ".github/agents/coreex-expert.agent.md"
                ".github/skills/coreex-docs-sync/SKILL.md"
                ".claude/commands/coreex-expert.md"
                ".claude/commands/coreex-docs-sync.md"
            )
            FilesAbsent  = @(
                ".github/copilot-instructions.md"
                ".github/docs"
                ".github/skills/solution-scaffolder"
            )
            FileContains = @{
                ".github/instructions/coreex.instructions.md"                   = 'applyTo: "**"'
                ".github/instructions/coreex-api-controllers.instructions.md"   = "applyTo:"
                ".github/instructions/coreex-validators.instructions.md"        = "applyTo:"
            }
        }
        Build      = $false
    },
    @{
        Name       = "coreex-ai-monorepo"
        Template   = "coreex-ai"
        Parameters = @{ "app-folder" = "backend" }
        TestPath   = "test-ai-monorepo"
        Verify     = @{
            FilesPresent = @(
                ".github/instructions/coreex.instructions.md"
                ".github/instructions/coreex-validators.instructions.md"
                ".github/skills/coreex-docs-sync/SKILL.md"
                ".claude/commands/coreex-docs-sync.md"
            )
            FilesAbsent  = @(
                ".github/copilot-instructions.md"
                ".github/docs"
            )
            FileContains = @{
                ".github/instructions/coreex.instructions.md"                 = 'applyTo: "backend/'
                ".github/instructions/coreex-validators.instructions.md"      = 'applyTo: "backend/'
                ".github/instructions/coreex-api-controllers.instructions.md" = 'applyTo: "backend/'
            }
        }
        Build      = $false
    },
    @{
        Name       = "coreex-postgres-defaults"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "Postgres"
            "messaging-provider"   = "ServiceBus"
            "refdata-enabled"      = "true"
            "outbox-enabled"       = "true"
            "domain-driven-enabled"= "false"
            "rop-enabled"          = "false"
        }
        TestPath   = "test-coreex-postgres"
        Verify     = @{
            FilesPresent = @(
                "src/App.Contracts/App.Contracts.csproj"
                "src/App.Application/App.Application.csproj"
                "src/App.Infrastructure/App.Infrastructure.csproj"
                "tools/App.Database/App.Database.csproj"
                "tools/App.CodeGen/App.CodeGen.csproj"
                "tests/App.Test.Common/App.Test.Common.csproj"
                "tests/App.Test.Unit/App.Test.Unit.csproj"
            )
            FilesAbsent  = @(
                ".github"
                "src/App.Domain"
            )
            FileContains = @{
                "src/App.Infrastructure/App.Infrastructure.csproj" = "CoreEx.Database.Postgres"
            }
        }
        Build      = $true
    },
    @{
        Name       = "coreex-sqlserver"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "SqlServer"
            "messaging-provider"   = "ServiceBus"
            "refdata-enabled"      = "true"
            "outbox-enabled"       = "true"
            "domain-driven-enabled"= "false"
            "rop-enabled"          = "false"
        }
        TestPath   = "test-coreex-sqlserver"
        Verify     = @{
            FilesPresent = @(
                "src/App.Infrastructure/App.Infrastructure.csproj"
                "tools/App.Database/App.Database.csproj"
            )
            FilesAbsent  = @(
                ".github"
            )
            FileContains = @{
                "src/App.Infrastructure/App.Infrastructure.csproj" = "CoreEx.Database.SqlServer"
            }
        }
        Build      = $true
    },
    @{
        Name       = "coreex-no-data-provider"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "None"
            "messaging-provider"   = "None"
            "refdata-enabled"      = "false"
            "outbox-enabled"       = "false"
            "domain-driven-enabled"= "false"
            "rop-enabled"          = "false"
        }
        TestPath   = "test-coreex-none"
        Verify     = @{
            FilesPresent = @(
                "src/App.Contracts/App.Contracts.csproj"
                "src/App.Application/App.Application.csproj"
                "src/App.Infrastructure/App.Infrastructure.csproj"
            )
            FilesAbsent  = @(
                ".github"
                "tools/App.Database"
                "tools/App.CodeGen"
            )
        }
        Build      = $true
    },
    @{
        Name       = "coreex-no-refdata"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "Postgres"
            "messaging-provider"   = "ServiceBus"
            "refdata-enabled"      = "false"
            "outbox-enabled"       = "true"
            "domain-driven-enabled"= "false"
            "rop-enabled"          = "false"
        }
        TestPath   = "test-coreex-no-refdata"
        Verify     = @{
            FilesPresent = @(
                "src/App.Application/App.Application.csproj"
                "tools/App.Database/App.Database.csproj"
            )
            FilesAbsent  = @(
                ".github"
                "tools/App.CodeGen"
                "src/App.Application/ReferenceDataService.cs"
            )
        }
        Build      = $true
    },
    @{
        Name       = "coreex-domain-driven"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "Postgres"
            "messaging-provider"   = "ServiceBus"
            "refdata-enabled"      = "true"
            "outbox-enabled"       = "true"
            "domain-driven-enabled"= "true"
            "rop-enabled"          = "false"
        }
        TestPath   = "test-coreex-domain"
        Verify     = @{
            FilesPresent = @(
                "src/App.Domain/App.Domain.csproj"
                "src/App.Application/App.Application.csproj"
            )
            FilesAbsent  = @(
                ".github"
            )
        }
        Build      = $true
    },
    @{
        Name       = "coreex-api-sqlserver-refdata"
        Template   = "coreex-api"
        Parameters = @{
            "data-provider"   = "SqlServer"
            "refdata-enabled" = "true"
            "outbox-enabled"  = "true"
        }
        TestPath   = "test-api-sqlserver"
        Verify     = @{
            FilesPresent = @(
                "src/App/App.csproj"
                "tests/App.Test.Api/App.Test.Api.csproj"
            )
        }
        Build      = $false  # add-on template; no standalone solution
    },
    @{
        Name       = "coreex-relay-sqlserver-servicebus"
        Template   = "coreex-relay"
        Parameters = @{
            "data-provider"      = "SqlServer"
            "messaging-provider" = "ServiceBus"
        }
        TestPath   = "test-relay-sqlserver"
        Verify     = @{
            FilesPresent = @(
                "src/App/App.csproj"
                "tests/App.Test.Relay/App.Test.Relay.csproj"
            )
        }
        Build      = $false  # add-on template; no standalone solution
    },
    @{
        Name       = "coreex-subscribe-sqlserver-servicebus-refdata"
        Template   = "coreex-subscribe"
        Parameters = @{
            "data-provider"      = "SqlServer"
            "messaging-provider" = "ServiceBus"
            "refdata-enabled"    = "true"
        }
        TestPath   = "test-subscribe-sqlserver"
        Verify     = @{
            FilesPresent = @(
                "src/App/App.csproj"
                "tests/App.Test.Subscribe/App.Test.Subscribe.csproj"
            )
        }
        Build      = $false  # add-on template; no standalone solution
    }
)

function Write-Header {
    param([string]$Message)
    Write-Output ""
    Write-Output "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    Write-Output $Message
    Write-Output "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
}

function Write-Pass { param([string]$m); Write-Host "  ✓ $m" -ForegroundColor Green }
function Write-Fail { param([string]$m); Write-Host "  ✗ $m" -ForegroundColor Red }

function Invoke-Assertion {
    param([string]$TestDir, [hashtable]$Verify, [ref]$Failures)

    foreach ($rel in $Verify.FilesPresent) {
        $full = Join-Path $TestDir $rel
        if (Test-Path $full) {
            Write-Pass "Present: $rel"
        } else {
            Write-Fail "MISSING: $rel"
            $Failures.Value += "Expected present: $rel"
        }
    }

    foreach ($rel in $Verify.FilesAbsent) {
        $full = Join-Path $TestDir $rel
        if (-not (Test-Path $full)) {
            Write-Pass "Absent:  $rel"
        } else {
            Write-Fail "SHOULD NOT EXIST: $rel"
            $Failures.Value += "Expected absent: $rel"
        }
    }

    foreach ($rel in $Verify.FileContains.Keys) {
        $full = Join-Path $TestDir $rel
        $needle = $Verify.FileContains[$rel]
        if (-not (Test-Path $full)) {
            Write-Fail "MISSING (content check): $rel"
            $Failures.Value += "File missing for content check: $rel"
        } elseif ((Get-Content $full -Raw) -like "*$needle*") {
            Write-Pass "Content '$needle': $rel"
        } else {
            Write-Fail "CONTENT NOT FOUND '$needle' in $rel"
            $Failures.Value += "Expected '$needle' in $rel"
        }
    }
}

try {
    # Step 1: Build and pack
    Write-Header "Building CoreEx.Template package"
    if (-not $NoRebuild) {
        Push-Location $templateProjectPath
        dotnet pack -c Release --nologo -v:normal
        if ($LASTEXITCODE -ne 0) { throw "Failed to pack CoreEx.Template" }
        Pop-Location
        Write-Pass "Template packed successfully"
    }

    # Step 2: Locate nupkg
    $nupkgFiles = Get-ChildItem -Path (Join-Path $templateProjectPath "bin/Release") -Filter "CoreEx.Template.*.nupkg" -ErrorAction SilentlyContinue
    if (-not $nupkgFiles) { throw "Template package not found in $templateProjectPath/bin/Release" }
    $nupkgFile = ($nupkgFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
    Write-Verbose "Using template package: $nupkgFile"

    # Step 3: Install template pack (uninstall any existing version first to avoid duplicate registrations)
    Write-Header "Installing template pack"
    dotnet new uninstall CoreEx.Template 2>&1 | Out-Null
    Write-Verbose "Uninstalled any prior CoreEx.Template (exit: $LASTEXITCODE — ignored)"
    dotnet new install $nupkgFile
    if ($LASTEXITCODE -ne 0) { throw "Failed to install template pack" }
    Write-Pass "Template pack installed"

    # Step 4: Create test root
    New-Item -ItemType Directory -Path $temporaryTestRoot -Force | Out-Null
    Write-Verbose "Test root: $temporaryTestRoot"

    # Step 5: Run scenarios
    Write-Header "Running validation scenarios"
    $failedScenarios = @()

    foreach ($scenario in $testScenarios) {
        Write-Output ""
        Write-Output "▶ $($scenario.Name) ($($scenario.Template))"
        if ($scenario.Parameters.Count -gt 0) {
            Write-Output "  Params: $(($scenario.Parameters | ConvertTo-Json -Compress))"
        }

        $testDir = Join-Path $temporaryTestRoot $scenario.TestPath
        $scenarioFailures = @()

        try {
            # Always scaffold into a fresh directory — stale build artefacts cause MSB3030 copy errors.
            if (Test-Path $testDir) {
                Remove-Item -Path $testDir -Recurse -Force | Out-Null
            }
            New-Item -ItemType Directory -Path $testDir -Force | Out-Null

            # Scaffold
            $args = @("new", $scenario.Template, "--output", $testDir, "--name", "App", "--no-update-check")
            foreach ($kv in $scenario.Parameters.GetEnumerator()) {
                $args += "--$($kv.Key)"
                if ($kv.Value -ne "") { $args += $kv.Value }
            }
            Write-Verbose "dotnet $($args -join ' ')"
            & dotnet @args
            if ($LASTEXITCODE -ne 0) { throw "dotnet new failed" }

            # Assertions
            if ($scenario.Verify) {
                Invoke-Assertion -TestDir $testDir -Verify $scenario.Verify -Failures ([ref]$scenarioFailures)
            }

            # Build
            if ($scenario.Build) {
                Write-Output "  Building generated output..."
                $buildTarget = (Get-ChildItem $testDir -Filter "*.slnx" -Recurse | Select-Object -First 1)
                if (-not $buildTarget) { $buildTarget = Get-ChildItem $testDir -Filter "*.sln" -Recurse | Select-Object -First 1 }
                if (-not $buildTarget) { $buildTarget = Get-ChildItem $testDir -Filter "*.csproj" -Recurse | Select-Object -First 1 }
                $buildPath = if ($buildTarget) { $buildTarget.FullName } else { $testDir }
                dotnet build $buildPath --nologo --verbosity minimal 2>&1 | Where-Object { $_ -match "error|warning|succeeded|failed" }
                if ($LASTEXITCODE -ne 0) {
                    $scenarioFailures += "dotnet build failed"
                    Write-Fail "Build FAILED"
                } else {
                    Write-Pass "Build succeeded"
                }
            }

            if ($scenarioFailures.Count -eq 0) {
                Write-Pass "$($scenario.Name) PASSED"
            } else {
                $scenarioFailures | ForEach-Object { Write-Verbose "  Error: $_" }
                $failedScenarios += $scenario.Name
            }
        }
        catch {
            Write-Fail "$($scenario.Name) threw: $_"
            $failedScenarios += $scenario.Name
        }
    }

    # Step 6: Summary
    Write-Header "Validation Summary"
    $passed = $testScenarios.Count - $failedScenarios.Count
    Write-Output "Passed: $passed / $($testScenarios.Count)"

    if ($failedScenarios.Count -gt 0) {
        Write-Fail "Failed scenarios:"
        $failedScenarios | ForEach-Object { Write-Output "  - $_" }
        throw "Template validation failed: $($failedScenarios.Count) scenario(s) failed"
    } else {
        Write-Pass "All $($testScenarios.Count) scenarios passed"
    }
}
finally {
    if (-not $SkipCleanup -and (Test-Path $temporaryTestRoot)) {
        Write-Header "Cleaning up"
        Remove-Item -Path $temporaryTestRoot -Recurse -Force -ErrorAction SilentlyContinue
        Write-Pass "Cleaned: $temporaryTestRoot"
    } elseif ($SkipCleanup) {
        Write-Output ""
        Write-Output "Test dirs preserved (-SkipCleanup): $temporaryTestRoot"
    }
}
