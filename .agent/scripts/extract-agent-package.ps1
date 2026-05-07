#!/usr/bin/env pwsh
<#
.SYNOPSIS
Extracts the agentic instruction package from CoreEx to a destination repository.

.DESCRIPTION
This script copies all agent customization files (.instructions.md, skills, templates, docs)
and creates the necessary folder structure for a new repository.

.PARAMETER DestinationPath
The root path of the destination repository where the agentic package will be copied.

.PARAMETER SourcePath
(Optional) The path to the CoreEx repository. Defaults to current directory if it contains
a .github folder with instructions.

.PARAMETER SkipDocs
(Optional) Skip copying documentation files from docs/ folder.

.PARAMETER SkipSamples
(Optional) Skip copying the samples folder.

.EXAMPLE
PS> .\extract-agent-package.ps1 -DestinationPath "C:\MyNewRepo"

.EXAMPLE
PS> .\extract-agent-package.ps1 -DestinationPath "C:\MyNewRepo" -SkipSamples

#>

param(
    [Parameter(Mandatory = $true, HelpMessage = "Destination repository path")]
    [string]$DestinationPath,

    [Parameter(Mandatory = $false, HelpMessage = "Source repository path (defaults to current)")]
    [string]$SourcePath = (Get-Location),

    [Parameter(Mandatory = $false, HelpMessage = "Skip copying docs folder")]
    [switch]$SkipDocs,

    [Parameter(Mandatory = $false, HelpMessage = "Skip copying samples folder")]
    [switch]$SkipSamples
)

function Write-Header {
    param([string]$Message)
    Write-Host ""
    Write-Host "=" * 70 -ForegroundColor Cyan
    Write-Host $Message -ForegroundColor Cyan
    Write-Host "=" * 70 -ForegroundColor Cyan
}

function Write-Step {
    param([string]$Message)
    Write-Host "▶ $Message" -ForegroundColor Yellow
}

function Write-Success {
    param([string]$Message)
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Validate source path
Write-Header "Validating Source Repository"
if (-not (Test-Path $SourcePath)) {
    Write-Error-Custom "Source path does not exist: $SourcePath"
    exit 1
}

$sourceGithub = Join-Path $SourcePath ".github"
$sourceInstructions = Join-Path $sourceGithub "instructions"

if (-not (Test-Path $sourceInstructions)) {
    Write-Error-Custom "Source repository does not contain .github/instructions folder"
    Write-Host "Expected at: $sourceInstructions"
    exit 1
}

Write-Success "Source repository found: $SourcePath"

# Validate destination path
if (-not (Test-Path $DestinationPath)) {
    Write-Step "Creating destination directory: $DestinationPath"
    New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
}

Write-Success "Destination repository path: $DestinationPath"

# Define items to copy
$copyItems = @(
    @{ Source = ".github/instructions"; Dest = ".github/instructions"; Label = "Instruction files" },
    @{ Source = ".github/skills"; Dest = ".github/skills"; Label = "Skill definitions" },
    @{ Source = ".github/templates"; Dest = ".github/templates"; Label = "Templates" },
    @{ Source = ".github/copilot-instructions.md"; Dest = ".github/copilot-instructions.md"; Label = "Copilot instructions" },
    @{ Source = ".github/INSTRUCTION_AUTHORING.md"; Dest = ".github/INSTRUCTION_AUTHORING.md"; Label = "Instruction authoring guide" },
    @{ Source = ".github/SKILL_AUTHORING.md"; Dest = ".github/SKILL_AUTHORING.md"; Label = "Skill authoring guide" }
)

if (-not $SkipDocs) {
    $copyItems += @(
        @{ Source = "docs/capabilities.md"; Dest = "docs/capabilities.md"; Label = "Capabilities documentation" },
        @{ Source = "docs/agent-interaction-guide.md"; Dest = "docs/agent-interaction-guide.md"; Label = "Agent interaction guide" },
        @{ Source = "docs/application-scaffolding-guide.md"; Dest = "docs/application-scaffolding-guide.md"; Label = "Scaffolding guide" }
    )
}

if (-not $SkipSamples) {
    $copyItems += @(
        @{ Source = "samples"; Dest = "samples"; Label = "Sample implementations" }
    )
}

# Copy all items
Write-Header "Copying Agentic Instruction Package"

foreach ($item in $copyItems) {
    $sourceFull = Join-Path $SourcePath $item.Source
    $destFull = Join-Path $DestinationPath $item.Dest

    if (-not (Test-Path $sourceFull)) {
        Write-Host "⚠️  Skipping (not found): $($item.Label)" -ForegroundColor Yellow
        continue
    }

    Write-Step "Copying $($item.Label)..."
    
    $destParent = Split-Path $destFull
    if (-not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    }

    Copy-Item -Path $sourceFull -Destination $destFull -Recurse -Force
    Write-Success "Copied: $($item.Label)"
}

# Create .agent folder structure
Write-Header "Creating .agent Folder Structure"

$agentPath = Join-Path $DestinationPath ".agent"
$execplansPath = Join-Path $agentPath "execplans"

Write-Step "Creating .agent directory..."
New-Item -ItemType Directory -Path $execplansPath -Force | Out-Null
Write-Success "Created .agent folder"

# Create PLANS.md if it doesn't exist
$plansFile = Join-Path $agentPath "PLANS.md"
if (-not (Test-Path $plansFile)) {
    Write-Step "Creating PLANS.md..."
    @"
# Execution Plans

Plans are stored in `.agent/execplans/` and tracked here.

| Plan ID | Description | Status |
|---------|-------------|--------|
| (Plans will be indexed here) | | |

See `.github/skills/coreex-exec-plan/` for the plan structure and template.
"@ | Set-Content $plansFile
    Write-Success "Created PLANS.md"
}

# Create PACKAGING_CHECKLIST.md
Write-Header "Creating Customization Checklist"

$checklistFile = Join-Path $DestinationPath ".agent/PACKAGING_CHECKLIST.md"
@"
# Agentic Instruction Package Customization Checklist

This checklist guides you through customizing the extracted agentic instruction package for your new repository.

## Phase 1: Review Package Contents

- [ ] Verify all `.github/instructions/*.md` files are present
- [ ] Verify all `.github/skills/*/` folders are present
- [ ] Verify `.github/templates/domain/` folder exists
- [ ] Review `docs/capabilities.md` for your project fit
- [ ] Review `samples/` folder content

## Phase 2: Customize Core Files

### `.github/copilot-instructions.md`
This is the main guidance document for your repository. Customize:

- [ ] Update project name and description at the top
- [ ] Replace **Repository Shape** section with your actual folder structure
- [ ] Update **Build, Test, and Run** section with your actual commands
- [ ] Update **Architecture** section with your domain patterns and layers
- [ ] Update **Key Conventions That Matter** sections with your coding standards
- [ ] Verify all instruction file references in the table are correct
- [ ] Update **Available Skills** to reference your specific domain
- [ ] Update **Available Agents** if you've created project-specific agents

### `README.md`
- [ ] Update project overview and purpose
- [ ] Update quick start instructions
- [ ] Add reference to agent capabilities and how to invoke them
- [ ] Update build and test commands
- [ ] Add links to key documentation in `docs/`

## Phase 3: Customize Instruction Files

Review and customize individual instruction files in `.github/instructions/`:

- [ ] `api-controllers.instructions.md` — Update project-specific routing/conventions
- [ ] `application-services.instructions.md` — Update service patterns for your architecture
- [ ] `contracts.instructions.md` — Update DTO/contract conventions
- [ ] `repositories.instructions.md` — Update data access patterns (EF, ADO.NET, etc.)
- [ ] `validators.instructions.md` — Update validation framework references
- [ ] `tests.instructions.md` — Update test framework and patterns
- [ ] `host-setup.instructions.md` — Update Program.cs conventions
- [ ] `event-subscribers.instructions.md` — Skip or customize if using event patterns
- [ ] `database-project.instructions.md` — Skip or customize if using database projects

**Tip**: Look for these markers in each file:
- `CoreEx` → Replace with your framework/project name
- `ScopedService<T>` → Replace with your DI patterns
- `IUnitOfWork` → Replace with your transaction patterns
- Service Bus, Outbox → Replace with your messaging approach

## Phase 4: Customize Skills

Each skill in `.github/skills/` may need project-specific updates:

- [ ] `generate-domain/` — Update domain generation templates for your architecture
- [ ] `add-capability/` — Update capability scaffolding patterns
- [ ] `aspire/` — Customize if not using Aspire
- [ ] `coreex-exec-plan/` — Customize plan structure for your organization
- [ ] Other skills — Verify they apply to your project

## Phase 5: Customize Templates

Templates in `.github/templates/domain/` are used by skill-driven code generation:

- [ ] Review template structure in `domain/`
- [ ] Customize layer names (e.g., `Contracts`, `Application`, `Infrastructure`)
- [ ] Update namespace conventions (e.g., `{{ProjectName}}.{{DomainName}}.{{Layer}}`)
- [ ] Update property and method templates for your conventions

## Phase 6: Create/Customize Docs

Documentation in `docs/`:

- [ ] Review `capabilities.md` — Update to your framework capabilities
- [ ] Review `agent-interaction-guide.md` — Generic; keep as-is or customize
- [ ] Review `application-scaffolding-guide.md` — Update with your patterns
- [ ] Add new docs for project-specific patterns not covered by CoreEx docs

## Phase 7: Finalize and Validate

- [ ] Run `dotnet build` to ensure projects compile
- [ ] Open a fresh ChatGPT/Copilot session and invoke `/` to see available skills
- [ ] Verify instruction files appear in the Copilot instructions panel
- [ ] Create a test file in `src/Application/` and verify `application-services.instructions.md` is referenced
- [ ] Create a test domain using `/generate-domain` skill to validate scaffolding
- [ ] Update this checklist and commit the `.agent/` folder to source control

## Optional: Advanced Customization

### Add Project-Specific Skills
Create new skills in `.github/skills/\<skill-name\>/`:
- Copy structure from existing skills
- Follow `.github/SKILL_AUTHORING.md`
- Reference in `.github/copilot-instructions.md`

### Add Agent-Specific Prompts
Create custom prompts in `.github/prompts/`:
- Follow `.github/INSTRUCTION_AUTHORING.md` for YAML frontmatter
- Reference specific agents in `.github/agents/` (if you have them)

### Create Execution Plans
Use `/coreex-exec-plan` skill to create structured execution plans:
- Plans are stored in `.agent/execplans/`
- Index them in `.agent/PLANS.md`

## Cleanup

- [ ] Remove `.agent/scripts/extract-agent-package.ps1` if not needed
- [ ] Remove `.agent/templates/copilot-instructions-template.md` (kept for reference)
- [ ] Remove this checklist (optional; keep as a guide for future onboarding)

## Done! 🎉

Your repository now has a full agentic instruction package tailored to your project.
Commit `.github/`, `.agent/`, and updated `docs/` to version control.

Next steps:
1. Share the repo with your team
2. Instruct team members to review the `README.md` and `.github/copilot-instructions.md`
3. Encourage use of skills like `/generate-domain` for consistent scaffolding
4. Periodically review and update instructions based on team feedback
"@ | Set-Content $checklistFile
Write-Success "Created PACKAGING_CHECKLIST.md"

# Create template version of copilot-instructions.md
Write-Header "Creating Template for Future Use"

$templatePath = Join-Path $agentPath "templates/copilot-instructions-template.md"
if (-not (Test-Path (Split-Path $templatePath))) {
    New-Item -ItemType Directory -Path (Split-Path $templatePath) -Force | Out-Null
}

Copy-Item -Path (Join-Path $sourceGithub "instructions/../copilot-instructions.md") `
          -Destination (Join-Path $sourceGithub "../../.agent/templates/copilot-instructions-template.md") `
          -Force -ErrorAction SilentlyContinue

Write-Success "Template location: `.agent/templates/copilot-instructions-template.md`"

# Final summary
Write-Header "✅ Extraction Complete!"

Write-Host ""
Write-Host "Package Contents:" -ForegroundColor Cyan
Write-Host "  .github/instructions/     - Layer-specific conventions"
Write-Host "  .github/skills/           - Domain generation and capability skills"
Write-Host "  .github/templates/        - Code generation templates"
Write-Host "  .github/*.md              - Authoring guides"
Write-Host "  docs/                     - Conceptual documentation"
if (-not $SkipSamples) {
    Write-Host "  samples/                  - Reference implementations"
}
Write-Host "  .agent/                   - Plans and customization workspace"
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "  1. Review: $DestinationPath\.agent\PACKAGING_CHECKLIST.md"
Write-Host "  2. Edit:   $DestinationPath\.github\copilot-instructions.md"
Write-Host "  3. Edit:   $DestinationPath\README.md"
Write-Host "  4. Verify: dotnet build (if applicable)"
Write-Host "  5. Test:   Open a file and invoke Copilot with '/'"
Write-Host ""
Write-Host "Documentation:" -ForegroundColor Cyan
Write-Host "  - Instruction files: $DestinationPath\.github\instructions\"
Write-Host "  - Authoring guides:  $DestinationPath\.github\INSTRUCTION_AUTHORING.md"
Write-Host "  - Skill authoring:   $DestinationPath\.github\SKILL_AUTHORING.md"
Write-Host ""
