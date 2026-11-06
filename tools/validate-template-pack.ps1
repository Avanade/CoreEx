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
5. Cleaning successful scenarios' build outputs immediately and temporary test directories at completion (unless -SkipCleanup).

Runs on Windows, Linux, and macOS via pwsh (cross-platform PowerShell).
Local runs build/package libraries for net10.0 in Debug to minimize disk usage.
CI retains all supported library frameworks in Release. Generated scenarios build/test net10.0 in Debug.

.PARAMETER SkipCleanup
If specified, temporary test directories are not cleaned up after validation.

.PARAMETER NoRebuild
If specified, the template pack is not rebuilt before validation (uses existing nupkg).

.PARAMETER ScenarioFilter
Optional scenario-name wildcard filters for focused validation. Omit to run every scenario.

.EXAMPLE
./validate-template-pack.ps1
./validate-template-pack.ps1 -SkipCleanup
./validate-template-pack.ps1 -NoRebuild
#>

[CmdletBinding()]
param(
    [switch]$SkipCleanup = $false,
    [switch]$NoRebuild = $false,
    [Alias("Scenario")]
    [string[]]$ScenarioFilter = @("*")
)

$ErrorActionPreference = "Stop"
$VerbosePreference = "Continue"

$repoRoot = Split-Path -Parent $PSScriptRoot
$templateProjectPath = Join-Path $repoRoot "src/CoreEx.Template"
# Prefer RUNNER_TEMP (set by GitHub Actions) to stay on a well-known short path in CI.
# Fall back to the system temp dir for local runs. Both avoid the deep repo worktree path
# which exceeds Windows MAX_PATH (260 chars) when build output is factored in.
$tempBase = if ($env:RUNNER_TEMP) { $env:RUNNER_TEMP } else { [System.IO.Path]::GetTempPath() }
$temporaryTestRoot = Join-Path $tempBase "cxval-$([Guid]::NewGuid().ToString('N').Substring(0, 12))"
$templateHive = Join-Path $temporaryTestRoot "template-hive"
$originalPackagesPath = $env:NUGET_PACKAGES

Write-Verbose "Repository root: $repoRoot"
Write-Verbose "Template project: $templateProjectPath"
Write-Verbose "Test root: $temporaryTestRoot"

$isCI = $env:CI -eq "true"
$packageConfiguration = if ($isCI) { "Release" } else { "Debug" }
# Override the shared policy, not TargetFramework(s): the generator must remain netstandard2.0.
# Pass this to both build and pack because pack intentionally ignores CoreExLocalTargetFramework.
$packageFrameworkArgs = @(if ($isCI) { "-p:CoreExLocalTargetFramework=" } else { "-p:CoreExTargetFrameworks=net10.0" })
Write-Verbose "Library packages: $packageConfiguration / $(if ($isCI) { 'all supported frameworks' } else { 'net10.0' })"

# ---------------------------------------------------------------------------
# Test scenarios
# ---------------------------------------------------------------------------
# Each scenario may declare:
#   FilesPresent  - paths relative to the output dir that MUST exist
#   FilesAbsent   - paths relative to the output dir that MUST NOT exist
#   FileContains  - hashtable of relative-path => required substring
#   Build         - if $true, run dotnet build on the output dir after scaffolding
#   Prepare       - optional scriptblock to compose a workload after scaffolding
#   VerifyBuild   - optional assertions against the compiled output
#   TestTargets   - optional project paths to run after a successful build
#   BeforeBuild   - optional generation after the scenario's NuGet source is configured
#   AfterBuild    - optional evaluated-property/compiled-output assertions
#   ExpectedBuildFailure - diagnostic required from an intentionally invalid consumer
# ---------------------------------------------------------------------------
$testScenarios = @(
    @{
        Name = "coreex-solution-default"
        Template = "coreex-solution"
        ProjectName = "Example.Integration"
        Parameters = @{}
        TestPath = "test-solution"
        Verify = @{
            FilesPresent = @(
                "Example.Integration.slnx", "Directory.Build.props", "Directory.Packages.props",
                "tests/Directory.Build.props", ".editorconfig", ".gitattributes", ".gitignore", ".filenesting.json",
                "README.md", "AGENTS.md", "CLAUDE.md",
                "tests/Example.Integration.Test.Common/Example.Integration.Test.Common.csproj",
                "tests/Example.Integration.Test.Common/GlobalUsing.cs", "tests/Example.Integration.Test.Common/TestData.cs"
            )
            FilesAbsent = @("src", "tools", "docker-compose.yml", "servicebus", "tests/Example.Integration.Test.Common/ServiceBus.cs", "tests/Example.Integration.Test.Common/Data")
            FileContains = @{
                "Example.Integration.slnx" = 'tests\Example.Integration.Test.Common\Example.Integration.Test.Common.csproj'
                "Directory.Packages.props" = 'Include="CoreEx.CodeGen"'
                ".gitignore" = "local.settings.json"
                "AGENTS.md" = "Baseline only"
            }
            FileNotContains = @{
                "Directory.Build.props" = "<!--#"
                "Directory.Packages.props" = "<!--#"
                "tests/Example.Integration.Test.Common/GlobalUsing.cs" = "Azure"
            }
        }
        Build = $true
        AfterBuild = {
            param($testDir)
            $project = Join-Path $testDir "tests/Example.Integration.Test.Common/Example.Integration.Test.Common.csproj"
            Assert-PackagePolicy $project $true
            $properties = & dotnet msbuild $project -nologo "-getProperty:IsTestProject,IsPackable" | ConvertFrom-Json
            if ($LASTEXITCODE -ne 0 -or $properties.Properties.IsTestProject -ne "false" -or $properties.Properties.IsPackable -ne "false") {
                throw "Test.Common must be non-packable support, not an executable test suite."
            }
        }
    },
    @{
        Name = "coreex-solution-no-common"
        Template = "coreex-solution"
        ProjectName = "Example.Integration"
        Parameters = @{ "test-common" = "false" }
        TestPath = "test-solution-no-common"
        Verify = @{
            FilesPresent = @("Example.Integration.slnx", "Directory.Packages.props", "tests/Directory.Build.props")
            FilesAbsent = @("src", "tools", "tests/Example.Integration.Test.Common")
            FileNotContains = @{ "Example.Integration.slnx" = "<Project" }
            FileContains = @{ "Directory.Packages.props" = 'Include="CoreEx.IO"' }
        }
        Build = $false
    },
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
                ".github/prompts/coreex-bootstrap.prompt.md"
                ".github/prompts/coreex-functions.prompt.md"
                ".github/prompts/coreex-file.prompt.md"
                ".github/instructions/coreex-files.instructions.md"
                ".github/skills/coreex-file/SKILL.md"
                ".github/skills/coreex-file/references/workflow.md"
                ".github/skills/coreex-file/references/patterns.md"
                ".claude/commands/coreex-file.md"
                ".github/docs/coreex/agents/CoreEx.IO.md"
                ".github/docs/coreex/files/README.md"
                ".github/docs/coreex/files/CodeGeneration.md"
                ".github/docs/coreex/schema/coreex-files.json"
                ".github/instructions/coreex-functions.instructions.md"
                ".github/skills/coreex-functions/SKILL.md"
                ".github/skills/coreex-functions/references/workflow.md"
                ".github/skills/coreex-functions/references/patterns.md"
                ".github/docs/coreex/agents/CoreEx.Azure.Functions.md"
                ".claude/commands/coreex-functions.md"
                ".github/agents/coreex-expert.agent.md"
                ".github/skills/coreex-bootstrap/SKILL.md"
                ".github/skills/coreex-docs-sync/SKILL.md"
                ".github/skills/coreex-scaffold/SKILL.md"
                ".github/skills/coreex-aspire/SKILL.md"
                ".github/skills/coreex-command-publish-e2e/SKILL.md"
                ".github/skills/coreex-command-subscribe-e2e/SKILL.md"
                ".github/instructions/coreex-aspire.instructions.md"
                ".github/docs/coreex/manifest.txt"
                ".github/coreex-ai-workflows.md"
                ".claude/commands/coreex-bootstrap.md"
                ".claude/commands/coreex-expert.md"
                ".claude/commands/coreex-docs-sync.md"
                ".claude/commands/coreex-scaffold.md"
                ".claude/commands/coreex-adapter.md"
                ".claude/commands/coreex-aspire.md"
                ".claude/commands/coreex-command-publish-e2e.md"
                ".claude/commands/coreex-command-subscribe-e2e.md"
                ".claude/commands/coreex-aggregate.md"
                ".claude/commands/coreex-api.md"
                ".claude/commands/coreex-api-e2e.md"
                ".claude/commands/coreex-app-service.md"
                ".claude/commands/coreex-contract.md"
                ".claude/commands/coreex-db-migration.md"
                ".claude/commands/coreex-graphql.md"
                ".claude/commands/coreex-policy.md"
                ".claude/commands/coreex-refdata.md"
                ".claude/commands/coreex-repository.md"
                ".claude/commands/coreex-subscriber.md"
                ".claude/commands/coreex-subscriber-e2e.md"
                ".claude/commands/coreex-test-api.md"
                ".claude/commands/coreex-test-relay.md"
                ".claude/commands/coreex-test-subscribe.md"
                ".claude/commands/coreex-validator.md"
            )
            FilesAbsent  = @(
                ".github/copilot-instructions.md"
                ".github/skills/coreex-solution-scaffolder"
            )
            FileContains = @{
                ".github/instructions/coreex.instructions.md"                   = 'applyTo: "**"'
                ".github/instructions/coreex-api-controllers.instructions.md"   = "Paging is tolerant"
                ".github/instructions/coreex-validators.instructions.md"        = "applyTo:"
                ".github/docs/coreex/manifest.txt"                             = "coreex-version:"
                ".github/docs/coreex/agents/CoreEx.Data.md"                    = "OrderQueryArgsConfig : QueryArgsConfig<OrderQueryArgsConfig>"
                ".github/prompts/coreex-repository.prompt.md"                  = "follow its authoritative workflow"
                ".github/instructions/coreex-tests.instructions.md"            = "Derive expectations from requirements"
                ".github/instructions/coreex-functions.instructions.md"        = 'applyTo: "**/*.Functions/**/*.cs,**/*.Test.Functions/**/*.cs"'
                ".github/skills/coreex-functions/references/patterns.md"        = "SnapshotHttpResponse"
                ".github/docs/coreex/agents/CoreEx.Azure.Functions.md"          = "AutoCompleteMessages = false"
                ".github/instructions/coreex-files.instructions.md"            = 'applyTo: "**/*-file.yaml;**/Files/**/*.cs;!**/Files/**/*.g.cs"'
                ".github/docs/coreex/agents/CoreEx.IO.md"                       = "CsvRoutedFileProcessor"
                ".github/docs/coreex/files/CodeGeneration.md"                  = "runtimeProjectPath"
                ".github/docs/coreex/schema/coreex-files.json"                 = '"discriminatorIndex"'
                ".github/skills/coreex-scaffold/SKILL.md"                      = "coreex-solution"
                ".github/skills/coreex-scaffold/references/workflow.md"        = "preserve it exactly"
                ".github/skills/coreex-file/references/workflow.md"            = "coreex-codegen"
                ".github/docs/coreex/agents/CoreEx.CodeGen.md"                 = "return await"
            }
            FileNotContains = @{
                ".github/instructions/coreex.instructions.md"                 = "Read each matching file in full"
                ".github/docs/coreex/agents/CoreEx.Data.md"                    = '.WithDefault("A")'
                ".github/skills/coreex-test-api/references/workflow.md"         = "resources get captured from the actual run"
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
                ".github/instructions/coreex-files.instructions.md"
                ".github/skills/coreex-file/SKILL.md"
                ".github/skills/coreex-file/references/workflow.md"
                ".github/skills/coreex-file/references/patterns.md"
                ".github/prompts/coreex-file.prompt.md"
                ".claude/commands/coreex-file.md"
                ".github/docs/coreex/agents/CoreEx.IO.md"
                ".github/docs/coreex/schema/coreex-files.json"
                ".github/instructions/coreex.instructions.md"
                ".github/instructions/coreex-validators.instructions.md"
                ".github/skills/coreex-docs-sync/SKILL.md"
                ".github/skills/coreex-scaffold/SKILL.md"
                ".github/skills/coreex-aspire/SKILL.md"
                ".github/skills/coreex-command-publish-e2e/SKILL.md"
                ".github/skills/coreex-command-subscribe-e2e/SKILL.md"
                ".github/instructions/coreex-aspire.instructions.md"
                ".github/instructions/coreex-functions.instructions.md"
                ".github/prompts/coreex-functions.prompt.md"
                ".github/skills/coreex-functions/SKILL.md"
                ".github/skills/coreex-functions/references/workflow.md"
                ".github/skills/coreex-functions/references/patterns.md"
                ".github/docs/coreex/agents/CoreEx.Azure.Functions.md"
                ".claude/commands/coreex-functions.md"
                ".github/docs/coreex/manifest.txt"
                ".github/coreex-ai-workflows.md"
                ".claude/commands/coreex-docs-sync.md"
            )
            FilesAbsent  = @(
                ".github/copilot-instructions.md"
                ".github/skills/coreex-solution-scaffolder"
            )
            FileContains = @{
                ".github/instructions/coreex.instructions.md"                 = 'applyTo: "backend/'
                ".github/instructions/coreex-validators.instructions.md"      = 'applyTo: "backend/'
                ".github/instructions/coreex-api-controllers.instructions.md" = 'applyTo: "backend/'
                ".github/docs/coreex/manifest.txt"                            = "coreex-version:"
                ".github/skills/coreex-api/SKILL.md"                           = "Resolve context before coding"
                ".github/instructions/coreex-functions.instructions.md"      = 'applyTo: "backend/**/*.Functions/**/*.cs,backend/**/*.Test.Functions/**/*.cs"'
                ".github/instructions/coreex-tooling.instructions.md"        = ';!backend/**/*.Database/Schema/**/*.g.*"'
                ".github/instructions/coreex-files.instructions.md"          = 'applyTo: "backend/**/*-file.yaml;backend/**/Files/**/*.cs;!backend/**/Files/**/*.g.cs"'
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
                "tests/Directory.Build.props"
            )
            FilesAbsent  = @(
                ".github"
                "src/App.Domain"
                "tests/_Directory.Build.props"
            )
            FileContains = @{
                "src/App.Infrastructure/App.Infrastructure.csproj" = "CoreEx.Database.Postgres"
                "tests/Directory.Build.props" = "IDE1006"
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
                "src/App.Domain"
            )
            FileContains = @{
                "src/App.Infrastructure/App.Infrastructure.csproj" = "CoreEx.Database.SqlServer"
            }
        }
        Build      = $true
    },
    @{
        Name        = "coreex-postgres-multiword"
        Template    = "coreex"
        ProjectName = "Contoso.ProductCatalog"
        Parameters  = @{
            "data-provider"      = "Postgres"
            "messaging-provider" = "ServiceBus"
            "refdata-enabled"    = "true"
            "outbox-enabled"     = "true"
            "rop-enabled"        = "false"
        }
        TestPath    = "test-coreex-postgres-multi"
        Verify      = @{
            # Migration filenames must be kebab-case; schema identifiers inside must be snake_case.
            FilesPresent     = @(
                "tools/Contoso.ProductCatalog.Database/Migrations/*-create-product-catalog-schema.pgsql"
                "tools/Contoso.ProductCatalog.Database/Migrations/*-create-product-catalog-outbox-tables.pgsql"
            )
            FileContains     = @{
                "tools/Contoso.ProductCatalog.Database/dbex.yaml"               = "schema: product_catalog"
                "tools/Contoso.ProductCatalog.Database/Program.cs"              = '"product_catalog"'
                "tools/Contoso.ProductCatalog.Database/Data/ref-data.seed.yaml" = "product_catalog:"
                # docker-compose project names reject dots (e.g. podman) - must be fully kebab-case, not just lower-cased.
                "docker-compose.yml"                                            = "name: contoso-product-catalog"
            }
            GlobFileContains = @{
                "tools/Contoso.ProductCatalog.Database/Migrations/*-create-product-catalog-schema.pgsql"        = '"product_catalog"'
                "tools/Contoso.ProductCatalog.Database/Migrations/*-create-product-catalog-outbox-tables.pgsql" = '"product_catalog"."outbox"'
            }
        }
        Build       = $false
    },
    @{
        Name        = "coreex-sqlserver-multiword"
        Template    = "coreex"
        ProjectName = "Contoso.ProductCatalog"
        Parameters  = @{
            "data-provider"      = "SqlServer"
            "messaging-provider" = "ServiceBus"
            "refdata-enabled"    = "true"
            "outbox-enabled"     = "true"
            "rop-enabled"        = "false"
        }
        TestPath    = "test-coreex-sqlserver-multi"
        Verify      = @{
            # Migration filenames must be kebab-case; schema identifiers inside must be PascalCase.
            FilesPresent     = @(
                "tools/Contoso.ProductCatalog.Database/Migrations/*-create-product-catalog-schema.sql"
                "tools/Contoso.ProductCatalog.Database/Migrations/*-create-product-catalog-outbox-tables.sql"
            )
            FileContains     = @{
                "tools/Contoso.ProductCatalog.Database/dbex.yaml"  = "schema: ProductCatalog"
                "tools/Contoso.ProductCatalog.Database/Program.cs" = '"ProductCatalog"'
                # docker-compose project names reject dots (e.g. podman) - must be fully kebab-case, not just lower-cased.
                "docker-compose.yml"                                = "name: contoso-product-catalog"
            }
            GlobFileContains = @{
                "tools/Contoso.ProductCatalog.Database/Migrations/*-create-product-catalog-schema.sql"        = "[ProductCatalog]"
                "tools/Contoso.ProductCatalog.Database/Migrations/*-create-product-catalog-outbox-tables.sql" = "[ProductCatalog].[Outbox]"
            }
        }
        Build       = $false
    },
    @{
        Name       = "coreex-no-data-provider"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "None"
            "messaging-provider"   = "None"
            "refdata-enabled"      = "false"
            "outbox-enabled"       = "false"
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
                "src/App.Domain"
                "tests/App.Test.Common/ServiceBus.cs"
                "tests/App.Test.Common/GlobalUsing.cs"
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
                "src/App.Application/ReferenceDataProvider.g.cs"
                "src/App.Domain"
            )
        }
        Build      = $true
    },
    @{
        Name       = "coreex-domain"
        Template   = "coreex-domain"
        # Must pass the name WITH the .Domain suffix - like every other add-on template
        # (coreex-api/relay/subscribe/aspire), sourceName is "app-name.Domain" so the suffix
        # is part of the substitutable token, not a fixed literal folder segment.
        ProjectName = "App.Domain"
        Parameters = @{}
        TestPath   = "test-coreex-domain"
        Verify     = @{
            FilesPresent = @(
                "src/App.Domain/App.Domain.csproj"
                "src/App.Domain/GlobalUsing.cs"
            )
        }
        Build      = $false  # add-on template; no standalone solution
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
        Name       = "coreex-api-postgres-refdata"
        Template   = "coreex-api"
        Parameters = @{
            "data-provider"   = "Postgres"
            "refdata-enabled" = "true"
            "outbox-enabled"  = "true"
        }
        TestPath   = "test-api-postgres"
        Verify     = @{
            FilesPresent = @(
                "src/App/App.csproj"
                "tests/App.Test.Api/App.Test.Api.csproj"
            )
            FileContains = @{
                # Regression guard: the Npgsql log-suppression entry must be gated on implement-postgres, not
                # implement-sqlserver - a copy-paste-then-diverged bug previously left it emitted only for SqlServer.
                "src/App/appsettings.Development.json" = '"Npgsql": "Warning"'
            }
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
        Name       = "coreex-subscribe-postgres-servicebus-refdata"
        Template   = "coreex-subscribe"
        Parameters = @{
            "data-provider"      = "Postgres"
            "messaging-provider" = "ServiceBus"
            "refdata-enabled"    = "true"
        }
        TestPath   = "test-subscribe-postgres"
        Verify     = @{
            FilesPresent = @(
                "src/App/App.csproj"
                "tests/App.Test.Subscribe/App.Test.Subscribe.csproj"
            )
            FileContains = @{
                # Regression guard: same Npgsql/implement-postgres guard bug found in coreex-api - copy-pasted
                # into Subscribe's appsettings too.
                "src/App/appsettings.Development.json" = '"Npgsql": "Warning"'
                "src/App/GlobalUsing.cs" = ".Infrastructure.Repositories;"
                "src/App/AGENTS.md" = "SubscribedBase<TValue>"
            }
            FileNotContains = @{
                "src/App/Program.cs" = "using CoreEx."
                "src/App/AGENTS.md" = "SubscriberBase"
            }
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
    },
    # ---------------------------------------------------------------------
    # Composite scenarios: scaffold `coreex` + one or more host templates into the
    # SAME directory (unlike the isolated add-on scenarios above, which have no
    # `coreex`-generated siblings to compile against and therefore can't set
    # Build = $true). These are what actually catch a symbol-conditional bug — an
    # unconditional `global using`/`ProjectReference` that should have been gated
    # behind a symbol like `has-data-provider` or `implement-servicebus` only fails
    # to compile once the host is built inside a real solution.
    # ---------------------------------------------------------------------
    @{
        Name       = "coreex-api-none-data-provider-regression"
        # Regression guard: --data-provider None + --refdata-enabled false used to leave
        # `global using CoreEx.Database;` and `using solution-name.Infrastructure.Repositories;`
        # unconditional in the Api host, even though neither the package nor the Repositories
        # folder exist in this combination (CS0234 at build time).
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "None"; "messaging-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false"; "rop-enabled" = "false" } }
            @{ Template = "coreex-api"; Name = "App.Api"; Parameters = @{ "data-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false" } }
        )
        TestPath   = "test-api-none-regression"
        Build      = $true
        BuildTarget = "src/App.Api/App.Api.csproj"
    },
    @{
        Name       = "coreex-subscribe-none-data-provider-regression"
        # Same regression guard as above, plus: --messaging-provider None used to leave
        # `global using CoreEx.Azure.Messaging.ServiceBus;` unconditional even though the
        # backing package is only referenced when implement-servicebus is true.
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "None"; "messaging-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false"; "rop-enabled" = "false" } }
            @{ Template = "coreex-subscribe"; Name = "App.Subscribe"; Parameters = @{ "data-provider" = "None"; "messaging-provider" = "None"; "refdata-enabled" = "false" } }
        )
        TestPath   = "test-subscribe-none-regression"
        Build      = $true
        BuildTarget = "src/App.Subscribe/App.Subscribe.csproj"
    },
    @{
        Name       = "coreex-aspire-full-stack"
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "Postgres"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "true"; "outbox-enabled" = "true"; "rop-enabled" = "false" } }
            @{ Template = "coreex-api"; Name = "App.Api"; Parameters = @{ "data-provider" = "Postgres"; "refdata-enabled" = "true"; "outbox-enabled" = "true" } }
            @{ Template = "coreex-relay"; Name = "App.Relay"; Parameters = @{ "data-provider" = "Postgres"; "messaging-provider" = "ServiceBus" } }
            @{ Template = "coreex-subscribe"; Name = "App.Subscribe"; Parameters = @{ "data-provider" = "Postgres"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "true" } }
            @{ Template = "coreex-aspire"; Name = "App.Aspire"; Parameters = @{ "has-api" = "true"; "has-relay" = "true"; "has-subscribe" = "true" } }
        )
        TestPath   = "test-aspire-full-stack"
        Verify     = @{
            FilesPresent = @(
                "aspire/App.Aspire/App.Aspire.csproj"
                "aspire/App.Aspire/AppHost.cs"
                "aspire/App.Aspire.MockHost/App.Aspire.MockHost.csproj"
                "aspire/App.Aspire.MockHost/Program.cs"
                "aspire/App.Test.Aspire/App.Test.Aspire.csproj"
                "aspire/App.Test.Aspire/GlobalUsing.cs"
                "aspire/App.Test.Aspire/HostTests.cs"
                "aspire/Directory.Build.props"
                "tests/App.Test.Common/ServiceBus.cs"
            )
            FileContains = @{
                "aspire/Directory.Build.props"    = "IDE1006"
                "aspire/App.Aspire/AppHost.cs"    = "builder.Environment.IsDevelopment()"
                "aspire/App.Aspire/App.Aspire.csproj" = "UserSecretsId"
                "aspire/App.Aspire/appsettings.Development.json" = "Port=5432"
                "src/App.Api/appsettings.Development.json" = "Port=5432"
                "src/App.Api/Program.cs"          = "AddNamedDestinationProvider"
                "src/App.Subscribe/Program.cs"    = "WithKeyedSubscribedSubscriber"
                "tests/App.Test.Common/ServiceBus.cs" = "CreateTopicOptions"
                "aspire/App.Test.Aspire/HostTests.cs" = "must report healthy"
            }
            FileNotContains = @{
                "aspire/App.Test.Aspire/HostTests.cs" = "BeOneOf"
                "aspire/App.Aspire/appsettings.Development.json" = "******"
                "src/App.Api/appsettings.Development.json" = "******"
                "src/App.Relay/appsettings.Development.json" = "******"
                "src/App.Subscribe/appsettings.Development.json" = "******"
            }
            FilesAbsent = @(
                # Extensions.cs was superseded by CoreEx.UnitTesting's Aspire extension methods (UnitTestExExtensions.Aspire.cs).
                "aspire/App.Aspire/Extensions.cs"
                "aspire/App.Aspire/InfrastructureResourceExtensions.cs"
                "aspire/App.Test.Aspire/InfrastructureResourceTests.cs"
                "aspire/_Directory.Build.props"
            )
        }
        Build        = $true
        BuildTargets = @(
            "aspire/App.Aspire/App.Aspire.csproj"           # transitively builds every host it references
            "aspire/App.Test.Aspire/App.Test.Aspire.csproj" # AppHost doesn't reference this, so build it explicitly
            "tests/App.Test.Subscribe/App.Test.Subscribe.csproj" # Compiles the keyed-subscriber tests and the Test.Common ServiceBus reset.
            "tests/App.Test.Relay/App.Test.Relay.csproj"
        )
    },
    @{
        Name       = "coreex-aspire-api-only"
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "SqlServer"; "messaging-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false"; "rop-enabled" = "false" } }
            @{ Template = "coreex-api"; Name = "App.Api"; Parameters = @{ "data-provider" = "SqlServer"; "refdata-enabled" = "false"; "outbox-enabled" = "false" } }
            @{ Template = "coreex-aspire"; Name = "App.Aspire"; Parameters = @{ "has-api" = "true"; "has-relay" = "false"; "has-subscribe" = "false"; "data-provider" = "SqlServer"; "messaging-provider" = "None" } }
        )
        TestPath   = "test-aspire-api-only"
        Verify     = @{
            FilesPresent    = @(
                "aspire/App.Aspire/App.Aspire.csproj"
                "aspire/App.Aspire.MockHost/App.Aspire.MockHost.csproj"
                "aspire/App.Test.Aspire/App.Test.Aspire.csproj"
            )
            FilesAbsent     = @(
                # Extensions.cs was superseded by CoreEx.UnitTesting's Aspire extension methods (UnitTestExExtensions.Aspire.cs).
                "aspire/App.Aspire/Extensions.cs"
            )
            FileContains    = @{
                "aspire/App.Aspire/AppHost.cs" = "Projects.App_Api"
            }
            FileNotContains = @{
                # has-relay/has-subscribe are false — confirms the #if stripping actually drops
                # the other hosts' AddProject calls and ProjectReferences, not just that has-api's survive.
                "aspire/App.Aspire/AppHost.cs"          = "Projects.App_Relay"
                "aspire/App.Aspire/App.Aspire.csproj"   = "App.Relay"
                "aspire/App.Aspire/appsettings.Development.json" = "******"
                "src/App.Api/appsettings.Development.json" = "******"
            }
        }
        Build        = $true
        BuildTargets = @(
            "aspire/App.Aspire/App.Aspire.csproj"
            "aspire/App.Test.Aspire/App.Test.Aspire.csproj"
        )
    },
    @{
        Name       = "coreex-aspire-no-data"
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "None"; "messaging-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false"; "rop-enabled" = "false" } }
            @{ Template = "coreex-api"; Name = "App.Api"; Parameters = @{ "data-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false" } }
            @{ Template = "coreex-aspire"; Name = "App.Aspire"; Parameters = @{ "has-api" = "true"; "has-relay" = "false"; "has-subscribe" = "false"; "data-provider" = "None"; "messaging-provider" = "None" } }
        )
        TestPath   = "test-aspire-no-data"
        Verify     = @{
            FilesAbsent = @(
                "aspire/App.Test.Aspire/InfrastructureResourceTests.cs"
                "aspire/App.Aspire/InfrastructureResourceExtensions.cs"
            )
            FileContains = @{
                "aspire/App.Aspire/AppHost.cs" = 'AddExternalConnectionString("redis")'
            }
            FileNotContains = @{
                "aspire/App.Aspire/AppHost.cs" = ".WithReference(db)"
            }
        }
        Build        = $true
        BuildTargets = @(
            "aspire/App.Aspire/App.Aspire.csproj"
            "aspire/App.Test.Aspire/App.Test.Aspire.csproj"
        )
    },
    @{
        Name       = "coreex-cosmos"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "Cosmos"
            "messaging-provider"   = "ServiceBus"
            "refdata-enabled"      = "true"
            "outbox-enabled"       = "true"
            "rop-enabled"          = "false"
        }
        TestPath   = "test-coreex-cosmos"
        Verify     = @{
            FilesPresent = @(
                "src/App.Infrastructure/App.Infrastructure.csproj"
                "src/App.Infrastructure/Repositories/AppCosmosDb.cs"
                "tools/App.Database/App.Database.csproj"
                "tools/App.Database/Data/ref-data.seed.yaml"
                "tools/App.CodeGen/App.CodeGen.csproj"
            )
            FilesAbsent  = @(
                ".github"
                "src/App.Domain"
                "tools/App.Database/dbex.yaml"
                "tools/App.Database/Migrations"
            )
            FileContains = @{
                "src/App.Infrastructure/App.Infrastructure.csproj" = "CoreEx.Cosmos"
                "docker-compose.yml"                               = "cosmos-emulator"
                "tools/App.CodeGen/ref-data.yaml"                  = "repository: Cosmos"
            }
            FileNotContains = @{
                "src/App.Infrastructure/App.Infrastructure.csproj" = "EntityFrameworkCore"
            }
        }
        Build      = $true
    },
    @{
        Name       = "coreex-cosmos-no-refdata"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "Cosmos"
            "messaging-provider"   = "ServiceBus"
            "refdata-enabled"      = "false"
            "outbox-enabled"       = "true"
            "rop-enabled"          = "false"
        }
        TestPath   = "test-coreex-cosmos-no-refdata"
        Verify     = @{
            FilesPresent = @(
                "src/App.Infrastructure/App.Infrastructure.csproj"
                "tools/App.Database/App.Database.csproj"
            )
            FilesAbsent  = @(
                ".github"
                "tools/App.CodeGen"
                "src/App.Domain"
            )
        }
        Build      = $true
    },
    @{
        Name       = "coreex-cosmos-no-outbox"
        Template   = "coreex"
        Parameters = @{
            "data-provider"        = "Cosmos"
            "messaging-provider"   = "ServiceBus"
            "refdata-enabled"      = "true"
            "outbox-enabled"       = "false"
            "rop-enabled"          = "false"
        }
        TestPath   = "test-coreex-cosmos-no-outbox"
        Verify     = @{
            FilesPresent = @(
                "src/App.Infrastructure/App.Infrastructure.csproj"
                "tools/App.Database/App.Database.csproj"
            )
        }
        Build      = $true
    },
    @{
        Name       = "coreex-cosmos-full-stack"
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "Cosmos"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "true"; "outbox-enabled" = "true"; "rop-enabled" = "false" } }
            @{ Template = "coreex-api"; Name = "App.Api"; Parameters = @{ "data-provider" = "Cosmos"; "refdata-enabled" = "true"; "outbox-enabled" = "true" } }
            @{ Template = "coreex-relay"; Name = "App.Relay"; Parameters = @{ "data-provider" = "Cosmos"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "true" } }
            @{ Template = "coreex-subscribe"; Name = "App.Subscribe"; Parameters = @{ "data-provider" = "Cosmos"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "true"; "outbox-enabled" = "true" } }
            @{ Template = "coreex-aspire"; Name = "App.Aspire"; Parameters = @{ "has-api" = "true"; "has-relay" = "true"; "has-subscribe" = "true"; "data-provider" = "Cosmos"; "messaging-provider" = "ServiceBus" } }
        )
        TestPath   = "test-cosmos-full-stack"
        Verify     = @{
            FilesPresent = @(
                "aspire/App.Aspire/AppHost.cs"
                "src/App.Api/Program.cs"
                "src/App.Relay/Program.cs"
                "tests/App.Test.Relay/HostTests.cs"
            )
            FilesAbsent  = @(
                # The Cosmos relay test is health-only: a fresh solution has no business mutation to enlist in the outbox transaction.
                "tests/App.Test.Relay/RelayTests.cs"
            )
            FileContains = @{
                "aspire/App.Aspire/AppHost.cs"  = 'AddExternalConnectionString("Cosmos", endpointKey: "AccountEndpoint")'
                "aspire/App.Aspire/appsettings.Development.json" = "AccountEndpoint=https://localhost:8081/"
                "src/App.Api/Program.cs"        = "AddCosmosDbEventPublisher"
                "src/App.Relay/Program.cs"      = "AddCosmosDbOutboxRelayHostedService"
                "src/App.Subscribe/Program.cs"  = "AddCosmosDbEventPublisher"
                "tools/App.Database/Program.cs" = "OutboxLeaseContainer"
            }
            FileNotContains = @{
                # The write-side publisher must never be registered on the Relay host.
                "src/App.Relay/Program.cs"      = "AddCosmosDbEventPublisher"
                "aspire/App.Aspire/appsettings.Development.json" = "******"
            }
        }
        Build        = $true
        BuildTargets = @(
            "aspire/App.Aspire/App.Aspire.csproj"
            "aspire/App.Test.Aspire/App.Test.Aspire.csproj"
            "tests/App.Test.Api/App.Test.Api.csproj"
            "tests/App.Test.Subscribe/App.Test.Subscribe.csproj"
            "tests/App.Test.Relay/App.Test.Relay.csproj"
        )
    },
    @{
        Name       = "coreex-cosmos-hosts-no-refdata-no-outbox"
        # Regression guard: with refdata disabled a fresh Cosmos solution has no containers, so the relay hosted service
        # and the relay tests that depend on it must be gated out; with the outbox disabled the Subscribe host must not
        # register a Cosmos event publisher and Service Bus becomes the default IEventPublisher.
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "Cosmos"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "false"; "outbox-enabled" = "false"; "rop-enabled" = "false" } }
            @{ Template = "coreex-api"; Name = "App.Api"; Parameters = @{ "data-provider" = "Cosmos"; "refdata-enabled" = "false"; "outbox-enabled" = "false" } }
            @{ Template = "coreex-relay"; Name = "App.Relay"; Parameters = @{ "data-provider" = "Cosmos"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "false" } }
            @{ Template = "coreex-subscribe"; Name = "App.Subscribe"; Parameters = @{ "data-provider" = "Cosmos"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "false"; "outbox-enabled" = "false" } }
        )
        TestPath   = "test-cosmos-hosts-minimal"
        Verify     = @{
            FileNotContains = @{
                "src/App.Subscribe/Program.cs"  = "AddCosmosDbEventPublisher"
                "src/App.Relay/Program.cs"      = "AddCosmosDbOutboxRelayHostedService"
            }
        }
        Build        = $true
        BuildTargets = @(
            "tests/App.Test.Api/App.Test.Api.csproj"
            "tests/App.Test.Subscribe/App.Test.Subscribe.csproj"
            "tests/App.Test.Relay/App.Test.Relay.csproj"
        )
    },
    @{
        Name       = "coreex-subscribe-no-outbox-regression"
        # Regression guard: the Subscribe host used to register the SQL Server/Postgres outbox publisher unconditionally,
        # and Service Bus as a non-default publisher, even when --outbox-enabled false.
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "SqlServer"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "false"; "outbox-enabled" = "false"; "rop-enabled" = "false" } }
            @{ Template = "coreex-subscribe"; Name = "App.Subscribe"; Parameters = @{ "data-provider" = "SqlServer"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "false"; "outbox-enabled" = "false" } }
        )
        TestPath   = "test-subscribe-no-outbox-sqlserver"
        Verify     = @{
            FileNotContains = @{
                "src/App.Subscribe/Program.cs" = "AddSqlServerOutboxPublisher"
            }
        }
        Build       = $true
        BuildTargets = @(
            "src/App.Subscribe/App.Subscribe.csproj"
            "tests/App.Test.Subscribe/App.Test.Subscribe.csproj"
        )
    },
    @{
        Name       = "coreex-functions-multiple-hosts"
        Steps      = @(
            @{ Template = "coreex"; Name = "Example.Integration.Orders"; Parameters = @{ "data-provider" = "None"; "messaging-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false" } }
            @{ Template = "coreex-functions"; Name = "Example.Integration.Orders.Functions"; Parameters = @{} }
            @{ Template = "coreex-functions"; Name = "Example.Imports.Functions"; Parameters = @{ "solution-name" = "Example.Integration.Orders" } }
        )
        TestPath   = "test-functions-multiple"
        Verify     = @{
            FilesPresent = @(
                "src/Example.Integration.Orders.Functions/Program.cs"
                "src/Example.Imports.Functions/Program.cs"
                "src/Example.Imports.Functions/.gitignore"
                "src/Example.Imports.Functions/local.settings.json"
                "tests/Example.Integration.Orders.Test.Functions/Example.Integration.Orders.Test.Functions.csproj"
                "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj"
                "tests/Example.Imports.Test.Functions/GlobalUsing.cs"
            )
            FilesAbsent = @(
                "tests/Example.Integration.Orders.Test.Functions/HostTests.cs"
                "tests/Example.Imports.Test.Functions/HostTests.cs"
                "src/Example.Imports.Functions/Health"
                "src/Example.Imports.Functions/Imports"
            )
            FileContains = @{
                "src/Example.Integration.Orders.Functions/Example.Integration.Orders.Functions.csproj" = "..\Example.Integration.Orders.Infrastructure\Example.Integration.Orders.Infrastructure.csproj"
                "src/Example.Imports.Functions/Example.Imports.Functions.csproj" = "..\Example.Integration.Orders.Application\Example.Integration.Orders.Application.csproj"
                "src/Example.Imports.Functions/appsettings.json" = '"SolutionName": "Example.Integration"'
                "src/Example.Integration.Orders.Functions/appsettings.json" = '"DomainName": "Orders"'
                "src/Example.Imports.Functions/.gitignore" = "local.settings.json"
                "src/Example.Imports.Functions/local.settings.json" = '"FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"'
                "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj" = "..\..\src\Example.Imports.Functions\Example.Imports.Functions.csproj"
                "tests/Example.Integration.Orders.Test.Functions/Example.Integration.Orders.Test.Functions.csproj" = "..\..\src\Example.Integration.Orders.Functions\Example.Integration.Orders.Functions.csproj"
            }
            FileNotContains = @{
                "src/Example.Imports.Functions/Program.cs" = "ConfigureFunctionsWebApplication"
                "src/Example.Imports.Functions/AGENTS.md" = "solution-name"
                "src/Example.Imports.Functions/Example.Imports.Functions.csproj" = "<ManagePackageVersionsCentrally>false"
                "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj" = "<ManagePackageVersionsCentrally>false"
            }
        }
        Build        = $true
        BuildTargets = @(
            "tests/Example.Integration.Orders.Test.Functions/Example.Integration.Orders.Test.Functions.csproj"
            "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj"
        )
        VerifyBuild = @{
            FileContains = @{
                "src/Example.Integration.Orders.Functions/bin/Debug/net10.0/functions.metadata" = "[]"
                "src/Example.Imports.Functions/bin/Debug/net10.0/functions.metadata" = "[]"
            }
            FilesPresent = @(
                "src/Example.Imports.Functions/bin/Debug/net10.0/host.json"
                "src/Example.Imports.Functions/bin/Debug/net10.0/appsettings.json"
            )
        }
    },
    @{
        Name       = "coreex-functions-http-composition"
        Steps      = @(
            @{ Template = "coreex"; Name = "Example.Integration.Catalog"; Parameters = @{ "data-provider" = "Postgres"; "messaging-provider" = "ServiceBus"; "refdata-enabled" = "true"; "outbox-enabled" = "true" } }
            @{ Template = "coreex-functions"; Name = "Example.Integration.Catalog.Functions"; Parameters = @{} }
        )
        TestPath   = "test-functions-http"
        # Compose only in the validation fixture: the user-facing template must remain empty.
        Prepare = {
            param($testDir)
            $hostDir = Join-Path $testDir "src/Example.Integration.Catalog.Functions"
            $programPath = Join-Path $hostDir "Program.cs"
            $program = (Get-Content -LiteralPath $programPath -Raw).Replace(
                "var builder = FunctionsApplication.CreateBuilder(args);",
                "var builder = FunctionsApplication.CreateBuilder(args);`n        builder.ConfigureFunctionsWebApplication();`n        builder.Services.AddMvc();`n        builder.Services.AddMvcWebApi((_, webApi) => webApi.ConvertUnhandledExceptionsToProblemDetails = true);")
            [System.IO.File]::WriteAllText($programPath, $program)
            $capabilityDir = Join-Path $hostDir "Products"
            New-Item -ItemType Directory -Path $capabilityDir -Force | Out-Null
            [System.IO.File]::WriteAllText((Join-Path $capabilityDir "ProductFunctions.cs"), @'
namespace Example.Integration.Catalog.Functions.Products;

public sealed class ProductFunctions(CoreEx.AspNetCore.Mvc.WebApi webApi)
{
    [Microsoft.Azure.Functions.Worker.Function("ReadProduct")]
    public Task<Microsoft.AspNetCore.Mvc.IActionResult> ReadAsync(
        [Microsoft.Azure.Functions.Worker.HttpTrigger(Microsoft.Azure.Functions.Worker.AuthorizationLevel.Function, "get", Route = "products/{id}")] Microsoft.AspNetCore.Http.HttpRequest request,
        string id, CancellationToken cancellationToken)
        => webApi.GetAsync(request, (_, _) => Task.FromResult(id), cancellationToken: cancellationToken);
}
'@)
            $testCapabilityDir = Join-Path $testDir "tests/Example.Integration.Catalog.Test.Functions/Products"
            New-Item -ItemType Directory -Path $testCapabilityDir -Force | Out-Null
            [System.IO.File]::WriteAllText((Join-Path $testCapabilityDir "ProductFunctionsTests.cs"), @'
namespace Example.Integration.Catalog.Test.Functions.Products;

public class ProductFunctionsTests
{
    [Test]
    public void Read_UsesWebApi()
    {
        using var test = FunctionTester.Create<Example.Integration.Catalog.Functions.Products.ProductFunctions>();
        var request = test.CreateHttpRequest(System.Net.Http.HttpMethod.Get, "products/example");
        test.HttpTrigger<Example.Integration.Catalog.Functions.Products.ProductFunctions>()
            .Run(f => f.ReadAsync(request, "example", CancellationToken.None))
            .ToHttpResponseMessageAssertor(request)
            .AssertOK()
            .AssertValue("example");
    }
}
'@)
        }
        Verify = @{
            FilesPresent = @("src/Example.Integration.Catalog.Functions/Products/ProductFunctions.cs")
            FileContains = @{
                "src/Example.Integration.Catalog.Functions/Program.cs" = "builder.ConfigureFunctionsWebApplication();"
            }
        }
        Build       = $true
        BuildTarget = "tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj"
        TestTargets = @("tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj")
        VerifyBuild = @{
            FileContains = @{
                "src/Example.Integration.Catalog.Functions/bin/Debug/net10.0/functions.metadata" = '"name": "ReadProduct"'
            }
        }
    },
    @{
        Name       = "coreex-functions-standalone"
        Template   = "coreex-functions"
        ProjectName = "Example.Integration.Catalog.Functions"
        Parameters = @{ "shared-core" = "false" }
        TestPath   = "test-functions-standalone"
        Verify = @{
            FilesPresent = @(
                "src/Example.Integration.Catalog.Functions/Program.cs"
                "tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj"
                "tests/Example.Integration.Catalog.Test.Functions/.gitignore"
            )
            FilesAbsent = @(
                "Directory.Build.props"
                "Directory.Packages.props"
                "src/Example.Integration.Catalog.Application"
                "src/Example.Integration.Catalog.Infrastructure"
                "src/Example.Integration.Catalog.Contracts"
            )
            FileContains = @{
                "src/Example.Integration.Catalog.Functions/Example.Integration.Catalog.Functions.csproj" = "ManagePackageVersionsCentrally"
                "tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj" = 'Include="UnitTestEx.Azure.Functions" Version="6.1.0"'
                "src/Example.Integration.Catalog.Functions/AGENTS.md" = "standalone Azure Functions"
            }
            FileNotContains = @{
                "src/Example.Integration.Catalog.Functions/Example.Integration.Catalog.Functions.csproj" = "ProjectReference"
                "src/Example.Integration.Catalog.Functions/AGENTS.md" = "AssemblyMarker"
                "tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj" = "coreex-version"
            }
        }
        AfterBuild = {
            param($testDir)
            Assert-PackagePolicy (Join-Path $testDir "src/Example.Integration.Catalog.Functions/Example.Integration.Catalog.Functions.csproj") $false
            Assert-PackagePolicy (Join-Path $testDir "tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj") $false
        }
        Build       = $true
        BuildTarget = "tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj"
        TestTargets = @("tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj")
        VerifyBuild = @{
            FileContains = @{
                "src/Example.Integration.Catalog.Functions/bin/Debug/net10.0/functions.metadata" = '"name": "ReadProduct"'
            }
        }
    },
    @{
        Name       = "coreex-functions-standalone-in-shared-solution"
        Steps      = @(
            @{ Template = "coreex"; Name = "Example.Integration.Orders"; Parameters = @{ "data-provider" = "None"; "messaging-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false" } }
            @{ Template = "coreex-functions"; Name = "Example.Imports.Functions"; Parameters = @{ "shared-core" = "false" } }
        )
        TestPath   = "test-functions-standalone-cpm"
        Verify = @{
            FileContains = @{
                "Directory.Packages.props" = "<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>"
                "src/Example.Imports.Functions/Example.Imports.Functions.csproj" = "ManagePackageVersionsCentrally"
            }
            FileNotContains = @{
                "src/Example.Imports.Functions/Example.Imports.Functions.csproj" = "ProjectReference"
                "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj" = "Orders"
            }
        }
        Build       = $true
        BuildTarget = "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj"
        AfterBuild = {
            param($testDir)
            Assert-PackagePolicy (Join-Path $testDir "src/Example.Imports.Functions/Example.Imports.Functions.csproj") $true
            Assert-PackagePolicy (Join-Path $testDir "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj") $true
        }
        VerifyBuild = @{
            FileContains = @{
                "src/Example.Imports.Functions/bin/Debug/net10.0/functions.metadata" = "[]"
            }
        }
    },
    @{
        Name = "coreex-functions-standalone-baseline"
        Steps = @(
            @{ Template = "coreex-solution"; Name = "Example.Integration.Catalog"; Parameters = @{} }
            @{ Template = "coreex-functions"; Name = "Example.Integration.Catalog.Functions"; Parameters = @{ "shared-core" = "false"; "test-common-project" = "..\Example.Integration.Catalog.Test.Common\Example.Integration.Catalog.Test.Common.csproj" } }
        )
        TestPath = "test-functions-baseline"
        Prepare = {
            param($testDir)
            & $httpCompositionScenario.Prepare $testDir
            Add-CommonResourceTest $testDir "Example.Integration.Catalog" "Example.Integration.Catalog"
            Add-SolutionProjects $testDir "Example.Integration.Catalog" @("Example.Integration.Catalog")
        }
        Verify = @{
            FilesAbsent = @("src/Example.Integration.Catalog.Application", "src/Example.Integration.Catalog.Contracts", "src/Example.Integration.Catalog.Infrastructure", "tools", "servicebus")
            FileContains = @{
                "tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj" = '..\Example.Integration.Catalog.Test.Common\Example.Integration.Catalog.Test.Common.csproj'
            }
        }
        Build = $true
        TestTargets = @("tests/Example.Integration.Catalog.Test.Functions/Example.Integration.Catalog.Test.Functions.csproj")
        AfterBuild = {
            param($testDir)
            Assert-PackagePolicy (Join-Path $testDir "src/Example.Integration.Catalog.Functions/Example.Integration.Catalog.Functions.csproj") $true
        }
        VerifyBuild = @{ FileContains = @{ "src/Example.Integration.Catalog.Functions/bin/Debug/net10.0/functions.metadata" = '"name": "ReadProduct"' } }
    },
    @{
        Name = "coreex-functions-independent-common"
        Steps = @(
            @{ Template = "coreex-solution"; Name = "Example.Integration"; Parameters = @{} }
            @{ Template = "coreex-functions"; Name = "Example.Input.Functions"; Parameters = @{ "shared-core" = "false"; "test-common-project" = "..\Example.Integration.Test.Common\Example.Integration.Test.Common.csproj" } }
            @{ Template = "coreex-functions"; Name = "Example.Output.Functions"; Parameters = @{ "shared-core" = "false"; "test-common-project" = "..\Example.Integration.Test.Common\Example.Integration.Test.Common.csproj" } }
        )
        TestPath = "test-functions-common"
        Prepare = {
            param($testDir)
            Add-CommonResourceTest $testDir "Example.Integration" "Example.Input"
            Add-CommonResourceTest $testDir "Example.Integration" "Example.Output"
            Add-SolutionProjects $testDir "Example.Integration" @("Example.Input", "Example.Output")
        }
        Verify = @{
            FilesAbsent = @("src/Example.Input.Application", "src/Example.Output.Infrastructure")
            FileContains = @{ "tests/Example.Output.Test.Functions/Example.Output.Test.Functions.csproj" = '..\Example.Integration.Test.Common\Example.Integration.Test.Common.csproj' }
        }
        Build = $true
        TestTargets = @("tests/Example.Input.Test.Functions/Example.Input.Test.Functions.csproj", "tests/Example.Output.Test.Functions/Example.Output.Test.Functions.csproj")
    },
    @{
        Name = "coreex-functions-common-opt-out"
        Steps = @(
            @{ Template = "coreex-solution"; Name = "Example.Integration"; Parameters = @{ "test-common" = "false" } }
            @{ Template = "coreex-functions"; Name = "Example.Imports.Functions"; Parameters = @{ "shared-core" = "false" } }
        )
        TestPath = "test-functions-no-common"
        Verify = @{
            FilesAbsent = @("tests/Example.Integration.Test.Common")
            FileNotContains = @{ "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj" = "Test.Common" }
        }
        Build = $true
        BuildTarget = "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj"
    },
    @{
        Name = "coreex-functions-missing-common"
        Template = "coreex-functions"
        ProjectName = "Example.Imports.Functions"
        Parameters = @{ "shared-core" = "false"; "test-common-project" = "..\Missing.Common\Missing.Common.csproj" }
        TestPath = "test-functions-missing-common"
        Build = $true
        BuildTarget = "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj"
        ExpectedBuildFailure = "COREEX001"
    },
    @{
        Name = "coreex-functions-missing-central-version"
        Steps = @(
            @{ Template = "coreex-solution"; Name = "Example.Integration"; Parameters = @{ "test-common" = "false" } }
            @{ Template = "coreex-functions"; Name = "Example.Imports.Functions"; Parameters = @{ "shared-core" = "false" } }
        )
        TestPath = "test-functions-missing-pin"
        Prepare = {
            param($testDir)
            $path = Join-Path $testDir "Directory.Packages.props"
            [xml]$central = Get-Content $path -Raw
            $entry = $central.SelectSingleNode("//PackageVersion[@Include='CoreEx.Azure.Functions']")
            $entry.ParentNode.RemoveChild($entry) | Out-Null
            $central.Save($path)
        }
        Build = $true
        BuildTarget = "src/Example.Imports.Functions/Example.Imports.Functions.csproj"
        ExpectedBuildFailure = "NU1010"
    },
    @{
        Name = "coreex-functions-explicit-cpm-opt-out"
        Steps = @(
            @{ Template = "coreex-solution"; Name = "Example.Integration"; Parameters = @{ "test-common" = "false" } }
            @{ Template = "coreex-functions"; Name = "Example.Imports.Functions"; Parameters = @{ "shared-core" = "false" } }
        )
        TestPath = "test-functions-cpm-opt-out"
        Prepare = {
            param($testDir)
            Set-ProjectCpmOptOut (Join-Path $testDir "src/Example.Imports.Functions/Example.Imports.Functions.csproj")
            Set-ProjectCpmOptOut (Join-Path $testDir "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj")
        }
        Build = $true
        BuildTarget = "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj"
        AfterBuild = {
            param($testDir)
            Assert-PackagePolicy (Join-Path $testDir "src/Example.Imports.Functions/Example.Imports.Functions.csproj") $false
            Assert-PackagePolicy (Join-Path $testDir "tests/Example.Imports.Test.Functions/Example.Imports.Test.Functions.csproj") $false
        }
    },
    @{
        Name = "coreex-codegen-inline-files"
        Steps = @(
            @{ Template = "coreex-functions"; Name = "Example.Files.Functions"; Parameters = @{ "shared-core" = "false" } }
            @{ Template = "coreex-codegen"; Name = "Example.Files.CodeGen"; Parameters = @{} }
        )
        TestPath = "test-codegen-inline"
        Prepare = { param($testDir); Add-FileConsumer $testDir }
        BeforeBuild = { param($testDir); Invoke-FileConsumerGeneration $testDir }
        Build = $true
        BuildTargets = @("tools/Example.Files.CodeGen/Example.Files.CodeGen.csproj", "tests/Example.Files.Test.Functions/Example.Files.Test.Functions.csproj")
        TestTargets = @("tests/Example.Files.Test.Functions/Example.Files.Test.Functions.csproj")
        Verify = @{
            FilesAbsent = @("Directory.Packages.props", "tools/Example.Files.Database", "tools/Example.Files.CodeGen/ref-data.yaml")
            FileNotContains = @{ "tools/Example.Files.CodeGen/Example.Files.CodeGen.csproj" = "ProjectReference" }
        }
        AfterBuild = { param($testDir); Assert-PackagePolicy (Join-Path $testDir "tools/Example.Files.CodeGen/Example.Files.CodeGen.csproj") $false }
        VerifyBuild = @{ FilesPresent = @("src/Example.Files.Functions/Files/Smoke/SmokeProcessor.g.cs", "src/Example.Files.Functions/Files/Smoke/SmokeHandler.cs") }
    },
    @{
        Name = "coreex-codegen-central-files"
        Steps = @(
            @{ Template = "coreex-solution"; Name = "Example.Files"; Parameters = @{} }
            @{ Template = "coreex-functions"; Name = "Example.Files.Functions"; Parameters = @{ "shared-core" = "false"; "test-common-project" = "..\Example.Files.Test.Common\Example.Files.Test.Common.csproj" } }
            @{ Template = "coreex-codegen"; Name = "Example.Files.CodeGen"; Parameters = @{} }
        )
        TestPath = "test-codegen-central"
        Prepare = {
            param($testDir)
            Add-FileConsumer $testDir
            Add-CommonResourceTest $testDir "Example.Files" "Example.Files"
            Add-SolutionProjects $testDir "Example.Files" @("Example.Files")
            & dotnet sln (Join-Path $testDir "Example.Files.slnx") add (Join-Path $testDir "tools/Example.Files.CodeGen") --solution-folder tools
            if ($LASTEXITCODE -ne 0) { throw "Failed to register independent CodeGen." }
        }
        BeforeBuild = { param($testDir); Invoke-FileConsumerGeneration $testDir }
        Build = $true
        TestTargets = @("tests/Example.Files.Test.Functions/Example.Files.Test.Functions.csproj")
        Verify = @{
            FilesAbsent = @("src/Example.Files.Contracts", "src/Example.Files.Application", "src/Example.Files.Infrastructure", "tools/Example.Files.Database", "tools/Example.Files.CodeGen/ref-data.yaml")
            FileNotContains = @{ "tools/Example.Files.CodeGen/Example.Files.CodeGen.csproj" = "ProjectReference" }
        }
        AfterBuild = { param($testDir); Assert-PackagePolicy (Join-Path $testDir "tools/Example.Files.CodeGen/Example.Files.CodeGen.csproj") $true }
    },
    @{
        Name = "coreex-codegen-explicit-cpm-opt-out"
        Steps = @(
            @{ Template = "coreex-solution"; Name = "Example.Files"; Parameters = @{ "test-common" = "false" } }
            @{ Template = "coreex-codegen"; Name = "Example.Files.CodeGen"; Parameters = @{} }
        )
        TestPath = "test-codegen-opt-out"
        Prepare = {
            param($testDir)
            Set-ProjectCpmOptOut (Join-Path $testDir "tools/Example.Files.CodeGen/Example.Files.CodeGen.csproj")
        }
        Build = $true
        BuildTarget = "tools/Example.Files.CodeGen/Example.Files.CodeGen.csproj"
        AfterBuild = { param($testDir); Assert-PackagePolicy (Join-Path $testDir "tools/Example.Files.CodeGen/Example.Files.CodeGen.csproj") $false }
    },
    @{
        Name = "coreex-solution-existing-functions-retrofit"
        Steps = @(
            @{ Template = "coreex-functions"; Name = "Example.Retrofit.Functions"; Parameters = @{ "shared-core" = "false" }; Prepare = { param($testDir); Initialize-InlineRetrofit $testDir } }
            @{ Template = "coreex-solution"; Name = "Example.Retrofit"; Parameters = @{} }
        )
        TestPath = "test-solution-retrofit"
        Prepare = {
            param($testDir)
            Complete-InlineRetrofit $testDir
            Add-CommonResourceTest $testDir "Example.Retrofit" "Example.Retrofit"
            Add-SolutionProjects $testDir "Example.Retrofit" @("Example.Retrofit")
        }
        Build = $true
        TestTargets = @("tests/Example.Retrofit.Test.Functions/Example.Retrofit.Test.Functions.csproj")
        AfterBuild = { param($testDir); Assert-InlineRetrofit $testDir }
        Verify = @{
            FilesAbsent = @("tools", "src/Example.Retrofit.Application", "src/Example.Retrofit.Infrastructure")
            FileContains = @{ "tests/Example.Retrofit.Test.Functions/Example.Retrofit.Test.Functions.csproj" = "Example.Retrofit.Test.Common.csproj" }
        }
    },
    @{
        Name       = "coreex-domain-regression"
        # Regression guard: coreex-domain's ProjectReference to Contracts previously resolved only
        # by accident, via substring overlap between the short sourceName "app-name" and the
        # "app-name" prefix embedded in "app-name.Contracts" - a standalone scaffold (Build = $false
        # above) can't catch this, since it never has a real Contracts sibling to compile against.
        Steps      = @(
            @{ Template = "coreex"; Name = "App"; Parameters = @{ "data-provider" = "Postgres"; "messaging-provider" = "None"; "refdata-enabled" = "false"; "outbox-enabled" = "false"; "rop-enabled" = "false" } }
            @{ Template = "coreex-domain"; Name = "App.Domain"; Parameters = @{} }
        )
        TestPath   = "test-domain-regression"
        Verify     = @{
            FilesPresent = @(
                "src/App.Domain/App.Domain.csproj"
            )
            FileContains = @{
                "src/App.Domain/App.Domain.csproj" = "App.Contracts\App.Contracts.csproj"
            }
        }
        Build       = $true
        BuildTarget = "src/App.Domain/App.Domain.csproj"
    }
)

$httpCompositionScenario = $testScenarios | Where-Object { $_.Name -eq "coreex-functions-http-composition" }
$testScenarios = @($testScenarios | Where-Object {
    $name = $_.Name
    @($ScenarioFilter | Where-Object { $name -like $_ }).Count -gt 0
})
if ($testScenarios.Count -eq 0) { throw "No validation scenarios match: $($ScenarioFilter -join ', ')." }

# Exercise the same HTTP capability and invocation test in shared-core and standalone layouts.
$standaloneScenario = $testScenarios | Where-Object { $_.Name -eq "coreex-functions-standalone" }
if ($standaloneScenario) { $standaloneScenario.Prepare = $httpCompositionScenario.Prepare }

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

    if ($Verify.FileContains) {
        foreach ($rel in $Verify.FileContains.Keys) {
            $full = Join-Path $TestDir $rel
            $needle = $Verify.FileContains[$rel]
            if (-not (Test-Path $full)) {
                Write-Fail "MISSING (content check): $rel"
                $Failures.Value += "File missing for content check: $rel"
            } elseif ((Get-Content $full -Raw).Contains($needle)) {
                Write-Pass "Content '$needle': $rel"
            } else {
                Write-Fail "CONTENT NOT FOUND '$needle' in $rel"
                $Failures.Value += "Expected '$needle' in $rel"
            }
        }
    }

    if ($Verify.FileNotContains) {
        foreach ($rel in $Verify.FileNotContains.Keys) {
            $full = Join-Path $TestDir $rel
            $needle = $Verify.FileNotContains[$rel]
            if (-not (Test-Path $full)) {
                Write-Fail "MISSING (negative content check): $rel"
                $Failures.Value += "File missing for negative content check: $rel"
            } elseif (-not (Get-Content $full -Raw).Contains($needle)) {
                Write-Pass "Absent content '$needle': $rel"
            } else {
                Write-Fail "CONTENT SHOULD NOT BE PRESENT '$needle' in $rel"
                $Failures.Value += "Expected '$needle' absent from $rel"
            }
        }
    }

    if ($Verify.GlobFileContains) {
        foreach ($pattern in $Verify.GlobFileContains.Keys) {
            $fullGlob = Join-Path $TestDir $pattern
            $matched = Get-ChildItem $fullGlob -ErrorAction SilentlyContinue | Select-Object -First 1
            $needle = $Verify.GlobFileContains[$pattern]
            if (-not $matched) {
                Write-Fail "NO FILE MATCHING: $pattern"
                $Failures.Value += "No file matching glob: $pattern"
            } elseif ((Get-Content $matched.FullName -Raw).Contains($needle)) {
                Write-Pass "Content '$needle': $($matched.Name)"
            } else {
                Write-Fail "CONTENT NOT FOUND '$needle' in $($matched.Name)"
                $Failures.Value += "Expected '$needle' in glob-matched file: $pattern"
            }
        }
    }
}

function Assert-PackagePolicy {
    param([string]$Project, [bool]$Central)
    $output = & dotnet msbuild $Project -nologo "-getProperty:ManagePackageVersionsCentrally" "-getItem:PackageReference"
    if ($LASTEXITCODE -ne 0) { throw "Cannot evaluate package policy for $Project." }
    $evaluation = $output | ConvertFrom-Json
    $expected = if ($Central) { "true" } else { "false" }
    $actual = $evaluation.Properties.ManagePackageVersionsCentrally
    if (($Central -and $actual -ne "true") -or (-not $Central -and $actual -eq "true")) {
        throw "Expected CPM=$expected for $Project, got '$actual'."
    }
    $references = @($evaluation.Items.PackageReference)
    if ($references.Count -eq 0) { throw "No effective package references in $Project." }
    foreach ($reference in $references) {
        if ($Central -and $reference.Version) { throw "Central reference $($reference.Identity) has an inline version in $Project." }
        if (-not $Central -and -not $reference.Version) { throw "Noncentral reference $($reference.Identity) lacks a version in $Project." }
    }
    Write-Pass "Effective CPM=$expected and correct package metadata: $Project"
}

function Add-SolutionProjects {
    param([string]$TestDir, [string]$SolutionName, [string[]]$HostPrefixes)
    $solution = Join-Path $TestDir "$SolutionName.slnx"
    foreach ($prefix in $HostPrefixes) {
        & dotnet sln $solution add (Join-Path $TestDir "src/$prefix.Functions") --solution-folder hosts
        if ($LASTEXITCODE -ne 0) { throw "Failed to register $prefix.Functions." }
        & dotnet sln $solution add (Join-Path $TestDir "tests/$prefix.Test.Functions") --solution-folder tests
        if ($LASTEXITCODE -ne 0) { throw "Failed to register $prefix.Test.Functions." }
    }
}

function Set-ProjectCpmOptOut {
    param([string]$Path)
    [xml]$project = Get-Content $Path -Raw
    $policy = $project.CreateElement("ManagePackageVersionsCentrally")
    $policy.InnerText = "false"
    $project.SelectSingleNode("//PropertyGroup").AppendChild($policy) | Out-Null
    $project.Save($Path)
}

function Add-CommonResourceTest {
    param([string]$TestDir, [string]$SolutionName, [string]$HostPrefix)
    $commonDir = Join-Path $TestDir "tests/$SolutionName.Test.Common"
    New-Item -ItemType Directory -Path (Join-Path $commonDir "Resources") -Force | Out-Null
    [System.IO.File]::WriteAllText((Join-Path $commonDir "FixtureHelper.cs"), @"
namespace $SolutionName.Test.Common;

/// <summary>Provides shared fixture normalization.</summary>
public static class FixtureHelper
{
    /// <summary>Normalizes the shared resource.</summary>
    public static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
"@ + "`n")
    [System.IO.File]::WriteAllText((Join-Path $commonDir "Resources/common.txt"), "shared fixture")
    [System.IO.File]::WriteAllText((Join-Path $TestDir "tests/$HostPrefix.Test.Functions/CommonTests.cs"), @"
namespace $HostPrefix.Test.Functions;

[TestFixture]
public sealed class CommonTests
{
    [Test]
    public void UsesSharedHelperAndEmbeddedResource()
    {
        var assembly = typeof($SolutionName.Test.Common.TestData).Assembly;
        using var resource = assembly.GetManifestResourceStream("$SolutionName.Test.Common.Resources.common.txt");
        Assert.That(resource, Is.Not.Null);
        using var reader = new StreamReader(resource ?? throw new InvalidOperationException("Missing shared resource."));
        Assert.That($SolutionName.Test.Common.FixtureHelper.Normalize(reader.ReadToEnd()), Is.EqualTo("SHARED FIXTURE"));
    }
}
"@ + "`n")
}

function Add-FileConsumer {
    param([string]$TestDir)
    $hostDir = Join-Path $TestDir "src/Example.Files.Functions"
    $hostProject = Join-Path $hostDir "Example.Files.Functions.csproj"
    [xml]$project = Get-Content $hostProject -Raw
    $coreExVersion = $project.SelectSingleNode("//PackageReference[@Include='CoreEx.Azure.Functions'][@Version]").Version
    $group = $project.CreateElement("ItemGroup")
    foreach ($name in @("CoreEx.IO", "CoreEx.Validation")) {
        $reference = $project.CreateElement("PackageReference")
        $reference.SetAttribute("Include", $name)
        if (-not (Test-Path (Join-Path $TestDir "Directory.Packages.props"))) { $reference.SetAttribute("Version", $coreExVersion) }
        $group.AppendChild($reference) | Out-Null
    }
    $project.Project.AppendChild($group) | Out-Null
    $project.Save($hostProject)
    Add-Content (Join-Path $hostDir "GlobalUsing.cs") @'
global using CoreEx;
global using CoreEx.DependencyInjection;
global using CoreEx.IO;
global using CoreEx.IO.Csv;
global using CoreEx.Validation;
global using System.Globalization;
global using System.Text;
global using System.Text.RegularExpressions;
'@
    [System.IO.File]::WriteAllText((Join-Path $TestDir "tools/Example.Files.CodeGen/smoke-file.yaml"), @'
name: Smoke
version: "fixture-v1"
runtimeProjectPath: ../../src/Example.Files.Functions
mode: ValidateOnly
encoding: Utf8
records:
  - name: Row
    properties:
      - name: Code
        type: String
        column: Code
        required: true
      - name: Count
        type: Integer
        column: Count
        min: 1
'@ + "`n")
    Add-Content (Join-Path $TestDir "tests/Example.Files.Test.Functions/GlobalUsing.cs") @'
global using CoreEx;
global using CoreEx.IO;
global using Example.Files.Functions.Files.Smoke;
global using Microsoft.Extensions.DependencyInjection;
global using System.Text;
'@
    [System.IO.File]::WriteAllText((Join-Path $TestDir "tests/Example.Files.Test.Functions/FileTests.cs"), @'
namespace Example.Files.Test.Functions;

[TestFixture]
public sealed class FileTests
{
    [TestCase("Code,Count\nok,1\n", FileProcessingOutcome.Validated)]
    [TestCase("Code,Count\nok,0\n", FileProcessingOutcome.Rejected)]
    public async Task ProcessesGeneratedDefinition(string csv, FileProcessingOutcome expected)
    {
        var services = new ServiceCollection();
        services.AddExecutionContext();
        services.AddScoped<SmokeHandler>();
        using var provider = services.BuildServiceProvider();
        var processor = new SmokeProcessor(provider);
        var result = await processor.ProcessAsync(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(csv))), SmokeProcessor.CreateContext("fixture", "attempt"));
        Assert.That(result.Outcome, Is.EqualTo(expected));
        Assert.That(result.Progress.UnitsAttempted, Is.Zero);
    }
}
'@ + "`n")
}

function Invoke-FileConsumerGeneration {
    param([string]$TestDir)
    $toolDir = Join-Path $TestDir "tools/Example.Files.CodeGen"
    Push-Location $toolDir
    try {
        & dotnet run -c Debug -- file --validate
        if ($LASTEXITCODE -ne 0) { throw "Independent File definition validation failed." }
        $outputDir = Join-Path $TestDir "src/Example.Files.Functions/Files/Smoke"
        if (Test-Path $outputDir) { throw "File --validate unexpectedly generated runtime code." }
        & dotnet run -c Debug --no-build -- file
        if ($LASTEXITCODE -ne 0) { throw "Independent File generation failed." }
        $handler = Join-Path $outputDir "SmokeHandler.cs"
        Add-Content $handler "// Handwritten fixture content must survive regeneration."
        $hash = (Get-FileHash $handler).Hash
        & dotnet run -c Debug --no-build -- file
        if ($LASTEXITCODE -ne 0 -or (Get-FileHash $handler).Hash -ne $hash) { throw "Regeneration changed the handwritten handler." }
        & dotnet run -c Debug --no-build -- file --expect-no-changes
        if ($LASTEXITCODE -ne 0) { throw "Regeneration is not deterministic." }
        $failureOutput = & dotnet run -c Debug --no-build -- file missing-file.yaml --validate 2>&1
        if ($LASTEXITCODE -eq 0) { throw "CodeGen swallowed a failed command's exit code: $failureOutput" }
        Write-Pass "File validation/generation, deterministic replay, handwritten preservation and failure exit code"
    }
    finally { Pop-Location }
}

function Initialize-InlineRetrofit {
    param([string]$TestDir)
    $pins = @()
    foreach ($relative in @("src/Example.Retrofit.Functions/Example.Retrofit.Functions.csproj", "tests/Example.Retrofit.Test.Functions/Example.Retrofit.Test.Functions.csproj")) {
        $path = Join-Path $TestDir $relative
        [xml]$project = Get-Content $path -Raw
        foreach ($group in @($project.Project.ItemGroup)) {
            if ($group.Condition -and $group.Condition.Contains(" == ")) { $project.Project.RemoveChild($group) | Out-Null }
            elseif ($group.Condition -and $group.Condition.Contains(" != ")) { $group.RemoveAttribute("Condition") }
        }
        $policy = $project.CreateElement("ManagePackageVersionsCentrally")
        $policy.InnerText = "false"
        $project.SelectSingleNode("//PropertyGroup").AppendChild($policy) | Out-Null
        if ($relative.StartsWith("src/")) {
            $identity = $project.CreateElement("PackageReference")
            $identity.SetAttribute("Include", "Azure.Identity")
            $identity.SetAttribute("Version", "1.16.0")
            $aliases = $project.CreateElement("Aliases")
            $aliases.InnerText = "global,AzureIdentity"
            $identity.AppendChild($aliases) | Out-Null
            $project.SelectSingleNode("//ItemGroup[PackageReference]").AppendChild($identity) | Out-Null
            $globalUsing = Join-Path $TestDir "src/Example.Retrofit.Functions/GlobalUsing.cs"
            [System.IO.File]::WriteAllText($globalUsing, "extern alias AzureIdentity;`nglobal using AliasCredential = AzureIdentity::Azure.Identity.DefaultAzureCredential;`n" + (Get-Content $globalUsing -Raw))
            [System.IO.File]::WriteAllText((Join-Path $TestDir "src/Example.Retrofit.Functions/AliasProof.cs"), @'
namespace Example.Retrofit.Functions;

/// <summary>Proves the existing Azure.Identity alias still compiles.</summary>
public static class AliasProof
{
    /// <summary>Gets the aliased credential type without contacting Azure.</summary>
    public static Type CredentialType => typeof(AliasCredential);
}
'@ + "`n")
        }
        foreach ($reference in $project.SelectNodes("//PackageReference[@Version]")) {
            $versionless = $reference.CloneNode($true)
            $versionless.RemoveAttribute("Version")
            $pins += @{ Project = $relative; Name = $reference.Include; Version = $reference.Version; Metadata = $versionless.OuterXml }
        }
        $project.Save($path)
    }
    $hostHashes = @{}
    foreach ($file in @("Program.cs", "host.json", "appsettings.json", "local.settings.json")) {
        $hostHashes[$file] = (Get-FileHash (Join-Path $TestDir "src/Example.Retrofit.Functions/$file")).Hash
    }
    @{ Pins = $pins; HostHashes = $hostHashes } | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $TestDir "retrofit-snapshot.json")
}

function Complete-InlineRetrofit {
    param([string]$TestDir)
    $snapshot = Get-Content (Join-Path $TestDir "retrofit-snapshot.json") -Raw | ConvertFrom-Json
    $centralPath = Join-Path $TestDir "Directory.Packages.props"
    [xml]$central = Get-Content $centralPath -Raw
    $versions = @{}
    foreach ($pin in $snapshot.Pins) {
        if ($versions.ContainsKey($pin.Name) -and $versions[$pin.Name] -ne $pin.Version) { throw "Conflicting existing version pins for $($pin.Name)." }
        $versions[$pin.Name] = $pin.Version
        $entry = $central.SelectSingleNode("//PackageVersion[@Include='$($pin.Name)']")
        if (-not $entry) {
            $entry = $central.CreateElement("PackageVersion")
            $entry.SetAttribute("Include", $pin.Name)
            $central.SelectSingleNode("//ItemGroup[PackageVersion]").AppendChild($entry) | Out-Null
        }
        $entry.SetAttribute("Version", $pin.Version)
    }
    $central.Save($centralPath)
    foreach ($relative in @($snapshot.Pins.Project | Select-Object -Unique)) {
        $path = Join-Path $TestDir $relative
        [xml]$project = Get-Content $path -Raw
        foreach ($policy in @($project.SelectNodes("//ManagePackageVersionsCentrally"))) { $policy.ParentNode.RemoveChild($policy) | Out-Null }
        foreach ($reference in $project.SelectNodes("//PackageReference[@Version]")) { $reference.RemoveAttribute("Version") }
        $project.Save($path)
    }
    & dotnet add (Join-Path $TestDir "tests/Example.Retrofit.Test.Functions/Example.Retrofit.Test.Functions.csproj") reference (Join-Path $TestDir "tests/Example.Retrofit.Test.Common/Example.Retrofit.Test.Common.csproj")
    if ($LASTEXITCODE -ne 0) { throw "Failed to wire existing tests to Test.Common." }
}

function Assert-InlineRetrofit {
    param([string]$TestDir)
    $snapshot = Get-Content (Join-Path $TestDir "retrofit-snapshot.json") -Raw | ConvertFrom-Json
    [xml]$central = Get-Content (Join-Path $TestDir "Directory.Packages.props") -Raw
    foreach ($pin in $snapshot.Pins) {
        $path = Join-Path $TestDir $pin.Project
        [xml]$project = Get-Content $path -Raw
        $reference = $project.SelectSingleNode("//PackageReference[@Include='$($pin.Name)']")
        $actualXml = [System.Xml.XmlDocument]::new()
        $actualXml.LoadXml($reference.OuterXml)
        $expectedXml = [System.Xml.XmlDocument]::new()
        $expectedXml.LoadXml($pin.Metadata)
        if ($actualXml.OuterXml -ne $expectedXml.OuterXml) { throw "Migration changed project metadata for $($pin.Name) in $($pin.Project)." }
        if ($central.SelectSingleNode("//PackageVersion[@Include='$($pin.Name)']").Version -ne $pin.Version) { throw "Migration changed the pin for $($pin.Name)." }
    }
    foreach ($relative in @($snapshot.Pins.Project | Select-Object -Unique)) { Assert-PackagePolicy (Join-Path $TestDir $relative) $true }
    foreach ($entry in $snapshot.HostHashes.PSObject.Properties) {
        if ((Get-FileHash (Join-Path $TestDir "src/Example.Retrofit.Functions/$($entry.Name)")).Hash -ne $entry.Value) { throw "Retrofit changed host file $($entry.Name)." }
    }
    Write-Pass "Existing exact pins, package metadata/Azure.Identity alias and host files preserved"
}

try {
    # Step 1: Build and pack CoreEx.Template
    Write-Header "Building CoreEx.Template package"
    if (-not $NoRebuild) {
        # Use dotnet build rather than dotnet pack: GeneratePackageOnBuild=true (from
        # Directory.Build.props) means the build target already produces the nupkg, and
        # dotnet pack alone skips compilation on a clean runner (no prior bin/ output).
        dotnet build $templateProjectPath -c $packageConfiguration @packageFrameworkArgs --nologo -m:1
        if ($LASTEXITCODE -ne 0) { throw "Failed to build CoreEx.Template" }
        Write-Pass "Template packed successfully"
    }

    # Step 2: Locate nupkg
    $packageOutputPath = Join-Path $templateProjectPath "bin/$packageConfiguration"
    $nupkgFiles = Get-ChildItem -Path $packageOutputPath -Filter "CoreEx.Template.*.nupkg" -ErrorAction SilentlyContinue
    if (-not $nupkgFiles) { throw "Template package not found in $packageOutputPath" }
    $nupkgFile = ($nupkgFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
    Write-Verbose "Using template package: $nupkgFile"

    # Step 3: Build and collect all CoreEx library packages into a local feed so that
    # scaffolded projects can restore without requiring the version to be published on NuGet.org.
    New-Item -ItemType Directory -Path $temporaryTestRoot -Force | Out-Null
    $localFeedPath = Join-Path $temporaryTestRoot "local-feed"
    New-Item -ItemType Directory -Path $localFeedPath -Force | Out-Null

    Write-Header "Building local NuGet feed"
    $srcRoot = Join-Path $repoRoot "src"
    # Only look one level deep — each package lives in its own direct subdirectory.
    # Recursing would pick up template content files inside CoreEx.Template/content/.
    $libraryProjects = Get-ChildItem -Path $srcRoot -Directory |
        Where-Object { $_.Name -ne "CoreEx.Template" } |
        ForEach-Object { Get-ChildItem -Path $_.FullName -Filter "*.csproj" | Select-Object -First 1 } |
        Where-Object { $_ -ne $null }

    # Build only the packable libraries (not samples/tests) so binaries and assets exist before packing individually.
    foreach ($proj in $libraryProjects) {
        dotnet build $proj.FullName -c $packageConfiguration @packageFrameworkArgs --nologo -m:1 -v q
        if ($LASTEXITCODE -ne 0) { throw "Failed to build $($proj.Directory.Name)" }
    }

    foreach ($proj in $libraryProjects) {
        Write-Verbose "Packing $($proj.Directory.Name)"
        $packOutput = dotnet pack $proj.FullName -c $packageConfiguration @packageFrameworkArgs --nologo --no-build --no-restore -o $localFeedPath 2>&1
        $packExit = $LASTEXITCODE
        $packOutput | Where-Object { $_ -match "error|successfully created" } | Write-Output
        if ($packExit -ne 0) { throw "Failed to pack $($proj.Directory.Name) (exit $packExit)" }
    }
    Write-Pass "Local feed populated: $localFeedPath"

    # Some CoreEx package versions depend on a paired prerelease of a third-party package (e.g. UnitTestEx.Aspire)
    # that may not be published on NuGet.org yet, only in a developer's own private local NuGet source. Adding
    # that whole source to the generated nuget.config is dangerous: if it also happens to hold an older, stale
    # copy of a CoreEx.* package at the SAME version this script just packed fresh (very possible — it's the
    # same developer machine, same unchanged version numbers), NuGet may resolve that stale copy instead of the
    # one in $localFeedPath, silently testing stale source at an unchanged package version.
    # So instead of adding the source, copy only the packages it holds that this script does NOT itself own
    # (i.e. not one of $libraryProjects) into $localFeedPath — this keeps a single trusted source of truth for
    # every CoreEx.* package while still filling in missing third-party prerelease dependencies.
    $libraryPackageIds = [System.Collections.Generic.HashSet[string]]::new([string[]]($libraryProjects | ForEach-Object { $_.BaseName.ToLowerInvariant() }))
    $localFileSources = @(dotnet nuget list source --format short) | ForEach-Object {
        if ($_ -match '^(?<flags>\S+)\s+(?<value>.+)$') {
            $flags = $Matches.flags
            $value = $Matches.value.Trim()
            if ($flags -notmatch 'D' -and $value -notmatch '^https?://' -and (Test-Path $value)) { $value }
        }
    }
    foreach ($source in $localFileSources) {
        Get-ChildItem -Path $source -Filter "*.nupkg" -ErrorAction SilentlyContinue | ForEach-Object {
            if ($_.Name -match '^(?<id>.+?)\.(?<version>\d+\.\d+\.\d+(\.\d+)?(-[0-9A-Za-z.]+)?)\.nupkg$') {
                $id = $Matches.id.ToLowerInvariant()
                $destination = Join-Path $localFeedPath $_.Name
                if (-not $libraryPackageIds.Contains($id) -and -not (Test-Path $destination)) {
                    Write-Verbose "Pulling in third-party package from private local source: $($_.Name)"
                    Copy-Item -Path $_.FullName -Destination $destination
                }
            }
        }
    }

    # Keep same-id/version source packages and template registrations isolated from other sessions.
    $env:NUGET_PACKAGES = Join-Path $temporaryTestRoot "packages"

    # Step 4: Install only in a private template hive; leave the user's installed pack untouched.
    Write-Header "Installing template pack"
    dotnet new install $nupkgFile --debug:custom-hive $templateHive
    if ($LASTEXITCODE -ne 0) { throw "Failed to install template pack" }
    Write-Pass "Template pack installed"

    Write-Verbose "Test root: $temporaryTestRoot"

    # Step 5: Run scenarios
    Write-Header "Running validation scenarios"
    $failedScenarios = @()

    foreach ($scenario in $testScenarios) {
        $isComposite = $null -ne $scenario.Steps
        Write-Output ""
        if ($isComposite) {
            Write-Output "▶ $($scenario.Name) ($(($scenario.Steps | ForEach-Object { $_.Template }) -join ' + '))"
        } else {
            Write-Output "▶ $($scenario.Name) ($($scenario.Template))"
            if ($scenario.Parameters.Count -gt 0) {
                Write-Output "  Params: $(($scenario.Parameters | ConvertTo-Json -Compress))"
            }
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
            if ($isComposite) {
                # Multiple templates into the SAME directory — e.g. `coreex` plus one or more hosts —
                # so the later steps have real siblings to compile against (see the composite scenarios above).
                foreach ($step in $scenario.Steps) {
                    $args = @("new", $step.Template, "--output", $testDir, "--name", $step.Name, "--no-update-check", "--debug:custom-hive", $templateHive)
                    foreach ($kv in $step.Parameters.GetEnumerator()) {
                        $args += "--$($kv.Key)"
                        if ($kv.Value -ne "") { $args += $kv.Value }
                    }
                    Write-Verbose "dotnet $($args -join ' ')"
                    & dotnet @args
                    if ($LASTEXITCODE -ne 0) { throw "dotnet new failed for step '$($step.Template)'" }
                    if ($step.Prepare) { & $step.Prepare $testDir }
                }
            } else {
                $projectName = if ($scenario.ProjectName) { $scenario.ProjectName } else { "App" }
                $args = @("new", $scenario.Template, "--output", $testDir, "--name", $projectName, "--no-update-check", "--debug:custom-hive", $templateHive)
                foreach ($kv in $scenario.Parameters.GetEnumerator()) {
                    $args += "--$($kv.Key)"
                    if ($kv.Value -ne "") { $args += $kv.Value }
                }
                Write-Verbose "dotnet $($args -join ' ')"
                & dotnet @args
                if ($LASTEXITCODE -ne 0) { throw "dotnet new failed" }
            }

            if ($scenario.Prepare) {
                & $scenario.Prepare $testDir
            }

            # Assertions
            if ($scenario.Verify) {
                Invoke-Assertion -TestDir $testDir -Verify $scenario.Verify -Failures ([ref]$scenarioFailures)
            }

            # Build
            if ($scenario.Build) {
                # Inject a nuget.config pointing at the local feed so restore succeeds
                # even when the current version hasn't been published to NuGet.org yet.
                $nugetConfigContent = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <!-- Clear inherited user/machine sources so this build only ever hits the local feed and
         nuget.org — avoids failures against unreachable corporate/internal feeds in CI. Any
         third-party prerelease dependency only available on a developer's private local source
         (e.g. UnitTestEx.Aspire) has already been copied into local-coreex above. -->
    <clear />
    <add key="local-coreex" value="$localFeedPath" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="local-coreex">
      <package pattern="CoreEx" />
      <package pattern="CoreEx.*" />
      <package pattern="*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@
                Set-Content -Path (Join-Path $testDir "nuget.config") -Value $nugetConfigContent -Encoding utf8

                if ($scenario.BeforeBuild) { & $scenario.BeforeBuild $testDir }

                Write-Output "  Building generated output..."
                # Explicit target(s) — required for composite scenarios: later steps (hosts) are never
                # `dotnet sln add`-ed to the first step's .slnx, so auto-detecting the .slnx would build
                # only the `coreex` solution's own projects and silently skip the host being tested.
                # BuildTargets (plural) lets a scenario build more than one entry point — e.g. the Aspire
                # scenarios build both the AppHost (which transitively builds every host it references) and
                # the Test.Aspire project (which the AppHost does NOT reference, so it would otherwise never compile).
                $buildTargets = if ($scenario.BuildTargets) { $scenario.BuildTargets } elseif ($scenario.BuildTarget) { @($scenario.BuildTarget) } else { $null }

                if ($buildTargets) {
                    $buildPaths = $buildTargets | ForEach-Object { Join-Path $testDir $_ }
                } else {
                    $buildTarget = (Get-ChildItem $testDir -Filter "*.slnx" -Recurse | Select-Object -First 1)
                    if (-not $buildTarget) { $buildTarget = Get-ChildItem $testDir -Filter "*.sln" -Recurse | Select-Object -First 1 }
                    if (-not $buildTarget) { $buildTarget = Get-ChildItem $testDir -Filter "*.csproj" -Recurse | Select-Object -First 1 }
                    $buildPaths = @(if ($buildTarget) { $buildTarget.FullName } else { $testDir })
                }

                foreach ($buildPath in $buildPaths) {
                    $buildOutput = & dotnet build $buildPath -c Debug --nologo --verbosity minimal -m:1 2>&1
                    $buildExit = $LASTEXITCODE
                    $buildOutput | Where-Object { $_ -match "error|warning|succeeded|failed" } | Write-Output
                    if ($scenario.ExpectedBuildFailure) {
                        if ($buildExit -eq 0 -or -not ($buildOutput -match $scenario.ExpectedBuildFailure)) {
                            $scenarioFailures += "Expected failed build with diagnostic '$($scenario.ExpectedBuildFailure)' for '$buildPath'."
                            Write-Fail "Expected diagnostic was not raised"
                        } else {
                            Write-Pass "Invalid consumer failed explicitly: $($scenario.ExpectedBuildFailure)"
                        }
                    } elseif ($buildExit -ne 0) {
                        $scenarioFailures += "dotnet build failed for '$buildPath'"
                        Write-Fail "Build FAILED ($buildPath)"
                    } else {
                        Write-Pass "Build succeeded ($buildPath)"
                    }
                }

                if ($scenario.VerifyBuild -and -not $scenario.ExpectedBuildFailure) {
                    Invoke-Assertion -TestDir $testDir -Verify $scenario.VerifyBuild -Failures ([ref]$scenarioFailures)
                }

                if ($scenarioFailures.Count -eq 0) {
                    if ($scenario.AfterBuild) { & $scenario.AfterBuild $testDir }
                    foreach ($testTarget in $scenario.TestTargets) {
                        $testPath = Join-Path $testDir $testTarget
                        dotnet test $testPath -c Debug -f net10.0 --no-build --nologo --verbosity minimal
                        if ($LASTEXITCODE -ne 0) {
                            $scenarioFailures += "dotnet test failed for '$testPath'"
                            Write-Fail "Tests FAILED ($testPath)"
                        } else {
                            Write-Pass "Tests succeeded ($testPath)"
                        }
                    }
                }
            }

            if ($scenarioFailures.Count -eq 0) {
                Write-Pass "$($scenario.Name) PASSED"
                if (-not $SkipCleanup) {
                    $buildOutputDirs = Get-ChildItem -LiteralPath $testDir -Directory -Recurse |
                        Where-Object { $_.Name -in @("bin", "obj") } |
                        Sort-Object { $_.FullName.Length } -Descending
                    foreach ($buildOutputDir in $buildOutputDirs) {
                        Remove-Item -LiteralPath $buildOutputDir.FullName -Recurse -Force
                    }
                }
            } else {
                $scenarioFailures | ForEach-Object { Write-Verbose "  Error: $_" }
                $failedScenarios += $scenario.Name
            }
        }
        catch {
            Write-Fail "$($scenario.Name) threw: $_"
            Write-Verbose $_.ScriptStackTrace
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
    $env:NUGET_PACKAGES = $originalPackagesPath
    if (-not $SkipCleanup -and (Test-Path $temporaryTestRoot)) {
        Write-Header "Cleaning up"
        Remove-Item -LiteralPath $temporaryTestRoot -Recurse -Force
        Write-Pass "Cleaned: $temporaryTestRoot"
    } elseif ($SkipCleanup) {
        Write-Output ""
        Write-Output "Test dirs preserved (-SkipCleanup): $temporaryTestRoot"
    }
}
