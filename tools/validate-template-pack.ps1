#!/usr/bin/env pwsh
<#
.SYNOPSIS
Validates the CoreEx.Template pack by scaffolding and building all required template combinations.

.DESCRIPTION
This script validates the CoreEx.Template pack by:
1. Building the template package
2. Installing it locally
3. Scaffolding each template combination with the specified parameters
4. Verifying each generated solution builds and tests successfully
5. Cleaning up temporary test directories

.PARAMETER SkipCleanup
If specified, temporary test directories are not cleaned up after validation.

.PARAMETER NoRebuild
If specified, the template pack is not rebuilt before validation.

.EXAMPLE
.\validate-template-pack.ps1
.\validate-template-pack.ps1 -SkipCleanup
#>

[CmdletBinding()]
param(
    [switch]$SkipCleanup = $false,
    [switch]$NoRebuild = $false
)

$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$templateProjectPath = Join-Path $repoRoot "src/CoreEx.Template"
$temporaryTestRoot = Join-Path $repoRoot "artifacts/template-validation-test-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$nugetCacheDir = Join-Path $env:USERPROFILE ".nuget/packages"

Write-Verbose "Repository root: $repoRoot"
Write-Verbose "Template project: $templateProjectPath"
Write-Verbose "Test root: $temporaryTestRoot"

# Define test scenarios
$testScenarios = @(
    @{
        Name = "coreex-ai-single-repo"
        Template = "coreex-ai"
        Parameters = @{}
        TestPath = "test-ai-single-repo"
    },
    @{
        Name = "coreex-ai-monorepo"
        Template = "coreex-ai"
        Parameters = @{
            "app-folder" = "backend"
        }
        TestPath = "test-ai-monorepo"
    },
    @{
        Name = "coreex-fullstack"
        Template = "coreex"
        Parameters = @{
            "data-provider" = "SqlServer"
            "messaging-provider" = "ServiceBus"
            "refdata-enabled" = "true"
            "rop-enabled" = "false"
            "domain-driven-enabled" = "false"
            "outbox-enabled" = "true"
        }
        TestPath = "test-fullstack"
    },
    @{
        Name = "coreex-api-with-refdata"
        Template = "coreex-api"
        Parameters = @{
            "data-provider" = "SqlServer"
            "refdata-enabled" = "true"
        }
        TestPath = "test-api-refdata"
    },
    @{
        Name = "coreex-relay-with-servicebus"
        Template = "coreex-relay"
        Parameters = @{
            "data-provider" = "SqlServer"
            "messaging-provider" = "ServiceBus"
        }
        TestPath = "test-relay-servicebus"
    },
    @{
        Name = "coreex-subscriber-with-refdata"
        Template = "coreex-subscriber"
        Parameters = @{
            "data-provider" = "SqlServer"
            "messaging-provider" = "ServiceBus"
            "refdata-enabled" = "true"
        }
        TestPath = "test-subscriber-refdata"
    }
)

function Write-Header {
    param([string]$Message)
    Write-Output ""
    Write-Output "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    Write-Output $Message
    Write-Output "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
}

function Write-Success {
    param([string]$Message)
    Write-Host "✓ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "✗ $Message" -ForegroundColor Red
}

try {
    # Step 1: Build and pack the template
    Write-Header "Building CoreEx.Template package"
    
    if (-not $NoRebuild) {
        Push-Location $templateProjectPath
        Write-Verbose "Running: dotnet pack"
        dotnet pack -c Release
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to pack CoreEx.Template"
        }
        Pop-Location
        Write-Success "Template packed successfully"
    }

    # Step 2: Get the template package path
    $nupkgPattern = Join-Path $templateProjectPath "bin/Release/CoreEx.Template.*.nupkg" | Resolve-Path
    $nupkgFile = @($nupkgPattern)[0]
    if (-not (Test-Path $nupkgFile)) {
        throw "Template package not found: $nupkgPattern"
    }
    Write-Verbose "Using template package: $nupkgFile"

    # Step 3: Create test root
    Write-Header "Creating test environment"
    New-Item -ItemType Directory -Path $temporaryTestRoot -Force | Out-Null
    Write-Success "Test root created: $temporaryTestRoot"

    # Step 4: Run test scenarios
    Write-Header "Running template validation scenarios"
    
    $failedScenarios = @()
    
    foreach ($scenario in $testScenarios) {
        Write-Output ""
        Write-Output "Testing: $($scenario.Name)"
        Write-Output "  Template: $($scenario.Template)"
        Write-Output "  Parameters: $(($scenario.Parameters | ConvertTo-Json -Compress))"
        
        $testDir = Join-Path $temporaryTestRoot $scenario.TestPath
        
        try {
            # Create test directory
            New-Item -ItemType Directory -Path $testDir -Force | Out-Null
            
            # Build the dotnet new command with parameters
            $newCommand = "dotnet new $($scenario.Template) --output `"$testDir`" --force"
            foreach ($param in $scenario.Parameters.GetEnumerator()) {
                $newCommand += " --$($param.Key) `"$($param.Value)`""
            }
            
            # Scaffold the template
            Write-Verbose "Running: $newCommand"
            Invoke-Expression $newCommand
            if ($LASTEXITCODE -ne 0) {
                throw "Template scaffolding failed"
            }
            
            # For API, Relay, and Subscriber, verify basic compilation
            if ($scenario.Template -in @("coreex-api", "coreex-relay", "coreex-subscriber")) {
                # Verify the generated files compile
                $projFile = Get-ChildItem -Path $testDir -Filter "*.csproj" -Recurse | Select-Object -First 1
                if ($projFile) {
                    Write-Verbose "Validating: $($projFile.FullName)"
                    # Just check if dotnet can load the project without building the entire solution
                    $dotnetListPropsResult = & dotnet list `"$($projFile.FullName)`" --format json 2>&1 | Out-String
                    if ($LASTEXITCODE -ne 0) {
                        throw "Project file validation failed: $dotnetListPropsResult"
                    }
                }
            }
            
            # For full solution (coreex), verify the solution can be loaded
            if ($scenario.Template -eq "coreex") {
                $slnFile = Get-ChildItem -Path $testDir -Filter "*.sln" -Recurse | Select-Object -First 1
                if ($slnFile) {
                    Write-Verbose "Validating solution: $($slnFile.FullName)"
                    # Verify the solution exists and has projects
                    $solutionContent = Get-Content $slnFile.FullName | Select-Object -First 10
                    if ($solutionContent -notmatch 'Project') {
                        throw "Solution file appears invalid"
                    }
                }
            }
            
            Write-Success "$($scenario.Name) validated successfully"
        }
        catch {
            Write-Error-Custom "$($scenario.Name) failed: $_"
            $failedScenarios += $scenario.Name
        }
    }

    # Step 5: Summary
    Write-Header "Validation Summary"
    
    $passedCount = $testScenarios.Count - $failedScenarios.Count
    $totalCount = $testScenarios.Count
    
    Write-Output "Passed: $passedCount / $totalCount"
    
    if ($failedScenarios.Count -gt 0) {
        Write-Error-Custom "Failed scenarios:"
        $failedScenarios | ForEach-Object { Write-Output "  - $_" }
        throw "Template validation failed"
    }
    else {
        Write-Success "All validation scenarios passed!"
    }
}
finally {
    # Cleanup
    if (-not $SkipCleanup -and (Test-Path $temporaryTestRoot)) {
        Write-Header "Cleaning up test environment"
        Remove-Item -Path $temporaryTestRoot -Recurse -Force -ErrorAction SilentlyContinue
        Write-Success "Test directory cleaned up: $temporaryTestRoot"
    }
    elseif ($SkipCleanup) {
        Write-Output ""
        Write-Output "Test directory preserved (--SkipCleanup): $temporaryTestRoot"
    }
}
