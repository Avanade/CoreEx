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
                ".github/prompts/coreex-bootstrap.prompt.md"
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
                ".github/instructions/coreex-api-controllers.instructions.md"   = "applyTo:"
                ".github/instructions/coreex-validators.instructions.md"        = "applyTo:"
                ".github/docs/coreex/manifest.txt"                             = "coreex-version:"
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
                ".github/skills/coreex-scaffold/SKILL.md"
                ".github/skills/coreex-aspire/SKILL.md"
                ".github/skills/coreex-command-publish-e2e/SKILL.md"
                ".github/skills/coreex-command-subscribe-e2e/SKILL.md"
                ".github/instructions/coreex-aspire.instructions.md"
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
                "aspire/App.Aspire/AppHost.cs"    = "Projects.App_Api"
                "aspire/App.Aspire/App.Aspire.csproj" = "CoreEx.UnitTesting"
                "src/App.Api/Program.cs"          = "AddNamedDestinationProvider"
                "src/App.Subscribe/Program.cs"    = "WithKeyedSubscribedSubscriber"
                "tests/App.Test.Common/ServiceBus.cs" = "CreateTopicOptions"
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
                "src/App.Api/Program.cs"        = "AddCosmosDbEventPublisher"
                "src/App.Relay/Program.cs"      = "AddCosmosDbOutboxRelayHostedService"
                "src/App.Subscribe/Program.cs"  = "AddCosmosDbEventPublisher"
                "tools/App.Database/Program.cs" = "OutboxLeaseContainer"
            }
            FileNotContains = @{
                # The write-side publisher must never be registered on the Relay host.
                "src/App.Relay/Program.cs"      = "AddCosmosDbEventPublisher"
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

try {
    # Step 1: Build and pack CoreEx.Template
    Write-Header "Building CoreEx.Template package"
    if (-not $NoRebuild) {
        Push-Location $templateProjectPath
        # Use dotnet build rather than dotnet pack: GeneratePackageOnBuild=true (from
        # Directory.Build.props) means the build target already produces the nupkg, and
        # dotnet pack alone skips compilation on a clean runner (no prior bin/ output).
        dotnet build -c Release --nologo -m:1
        if ($LASTEXITCODE -ne 0) { throw "Failed to build CoreEx.Template" }
        Pop-Location
        Write-Pass "Template packed successfully"
    }

    # Step 2: Locate nupkg
    $nupkgFiles = Get-ChildItem -Path (Join-Path $templateProjectPath "bin/Release") -Filter "CoreEx.Template.*.nupkg" -ErrorAction SilentlyContinue
    if (-not $nupkgFiles) { throw "Template package not found in $templateProjectPath/bin/Release" }
    $nupkgFile = ($nupkgFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName
    Write-Verbose "Using template package: $nupkgFile"

    # Step 3: Build and collect all CoreEx library packages into a local feed so that
    # scaffolded projects can restore without requiring the version to be published on NuGet.org.
    New-Item -ItemType Directory -Path $temporaryTestRoot -Force | Out-Null
    $localFeedPath = Join-Path $temporaryTestRoot "local-feed"
    New-Item -ItemType Directory -Path $localFeedPath -Force | Out-Null

    Write-Header "Building local NuGet feed"
    # Build the solution so binaries and assets exist before packing individually.
    dotnet build (Join-Path $repoRoot "CoreEx.sln") -c Release --nologo -m:1
    if ($LASTEXITCODE -ne 0) { throw "Failed to build solution" }

    $srcRoot = Join-Path $repoRoot "src"
    # Only look one level deep — each package lives in its own direct subdirectory.
    # Recursing would pick up template content files inside CoreEx.Template/content/.
    $libraryProjects = Get-ChildItem -Path $srcRoot -Directory |
        Where-Object { $_.Name -ne "CoreEx.Template" } |
        ForEach-Object { Get-ChildItem -Path $_.FullName -Filter "*.csproj" | Select-Object -First 1 } |
        Where-Object { $_ -ne $null }

    foreach ($proj in $libraryProjects) {
        Write-Verbose "Packing $($proj.Directory.Name)"
        $packOutput = dotnet pack $proj.FullName -c Release --nologo --no-build --no-restore -o $localFeedPath 2>&1
        $packExit = $LASTEXITCODE
        $packOutput | Where-Object { $_ -match "error|successfully created" } | Write-Output
        if ($packExit -ne 0) { throw "Failed to pack $($proj.Directory.Name) (exit $packExit)" }
    }
    Write-Pass "Local feed populated: $localFeedPath"

    # NuGet's global-packages cache is keyed purely by package id + version. Every package here is
    # packed at whatever version is currently in Directory.Build.props (typically unchanged between
    # local runs), so once a version has been restored once, NuGet will happily keep serving that
    # STALE cached copy to every future scaffold — silently ignoring the freshly-packed content above
    # and making local source changes invisible to validation. Evict exactly these package ids from
    # the global cache before anything restores from the local feed, so each run reflects current source.
    $globalPackagesFolder = (dotnet nuget locals global-packages --list) -replace '^global-packages:\s*', ''
    $globalPackagesFolder = $globalPackagesFolder.Trim()
    if ($globalPackagesFolder -and (Test-Path $globalPackagesFolder)) {
        foreach ($proj in $libraryProjects) {
            $packageId = $proj.BaseName.ToLowerInvariant()
            $cachedPackagePath = Join-Path $globalPackagesFolder $packageId
            if (Test-Path $cachedPackagePath) {
                Write-Verbose "Evicting stale cached package: $cachedPackagePath"
                Remove-Item -Path $cachedPackagePath -Recurse -Force
            }
        }
        Write-Pass "Evicted stale CoreEx.* packages from the global NuGet cache"
    }

    # Some CoreEx package versions depend on a paired prerelease of a third-party package (e.g. UnitTestEx.Aspire)
    # that may not be published on NuGet.org yet, only in a developer's own private local NuGet source. Adding
    # that whole source to the generated nuget.config is dangerous: if it also happens to hold an older, stale
    # copy of a CoreEx.* package at the SAME version this script just packed fresh (very possible — it's the
    # same developer machine, same unchanged version numbers), NuGet may resolve that stale copy instead of the
    # one in $localFeedPath, silently re-introducing the exact staleness bug evicted from the global cache above.
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

    # Step 4: Install template pack (uninstall any existing version first to avoid duplicate registrations)
    Write-Header "Installing template pack"
    dotnet new uninstall CoreEx.Template 2>&1 | Out-Null
    Write-Verbose "Uninstalled any prior CoreEx.Template (exit: $LASTEXITCODE — ignored)"
    dotnet new install $nupkgFile
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
                    $args = @("new", $step.Template, "--output", $testDir, "--name", $step.Name, "--no-update-check")
                    foreach ($kv in $step.Parameters.GetEnumerator()) {
                        $args += "--$($kv.Key)"
                        if ($kv.Value -ne "") { $args += $kv.Value }
                    }
                    Write-Verbose "dotnet $($args -join ' ')"
                    & dotnet @args
                    if ($LASTEXITCODE -ne 0) { throw "dotnet new failed for step '$($step.Template)'" }
                }
            } else {
                $projectName = if ($scenario.ProjectName) { $scenario.ProjectName } else { "App" }
                $args = @("new", $scenario.Template, "--output", $testDir, "--name", $projectName, "--no-update-check")
                foreach ($kv in $scenario.Parameters.GetEnumerator()) {
                    $args += "--$($kv.Key)"
                    if ($kv.Value -ne "") { $args += $kv.Value }
                }
                Write-Verbose "dotnet $($args -join ' ')"
                & dotnet @args
                if ($LASTEXITCODE -ne 0) { throw "dotnet new failed" }
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
</configuration>
"@
                Set-Content -Path (Join-Path $testDir "nuget.config") -Value $nugetConfigContent -Encoding utf8

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
                    dotnet build $buildPath --nologo --verbosity minimal -m:1 2>&1 | Where-Object { $_ -match "error|warning|succeeded|failed" }
                    if ($LASTEXITCODE -ne 0) {
                        $scenarioFailures += "dotnet build failed for '$buildPath'"
                        Write-Fail "Build FAILED ($buildPath)"
                    } else {
                        Write-Pass "Build succeeded ($buildPath)"
                    }
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
