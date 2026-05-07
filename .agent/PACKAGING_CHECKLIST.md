# Agentic Instruction Package Extraction Guide

## Overview

This guide helps you extract and customize the CoreEx agentic instruction package for a new repository.

**What you're extracting:**
- 9 instruction files (layer-specific conventions)
- 5 domain-specific skills (code generation, planning, etc.)
- Templates for code scaffolding
- Documentation and guides
- Customization templates and scripts

**What you're NOT extracting:**
- `src/` — CoreEx framework code (project-specific)
- `tests/` — CoreEx framework tests (project-specific)
- `gen/` — Roslyn source generators (CoreEx-specific)
- Sample implementations (optional; included by default)

---

## Quick Start: One-Command Extraction

From the CoreEx repository root, run:

```powershell
.\.agent\scripts\extract-agent-package.ps1 -DestinationPath "C:\path\to\new-repo"
```

**Options:**
```powershell
# Skip documentation files
.\.agent\scripts\extract-agent-package.ps1 -DestinationPath "..." -SkipDocs

# Skip sample implementations
.\.agent\scripts\extract-agent-package.ps1 -DestinationPath "..." -SkipSamples

# Skip both
.\.agent\scripts\extract-agent-package.ps1 -DestinationPath "..." -SkipDocs -SkipSamples
```

The script will:
1. ✅ Validate the source CoreEx repository
2. ✅ Create destination folder structure
3. ✅ Copy all agentic instruction files
4. ✅ Create `.agent/execplans/` folder
5. ✅ Generate `PLANS.md` index
6. ✅ Generate `PACKAGING_CHECKLIST.md` for customization

---

## Manual Extraction (If Not Using Script)

If you prefer to extract manually:

```
Destination Repository Root/
├── .github/
│   ├── instructions/                    # Copy from CoreEx
│   │   ├── api-controllers.instructions.md
│   │   ├── application-services.instructions.md
│   │   ├── contracts.instructions.md
│   │   ├── database-project.instructions.md
│   │   ├── event-subscribers.instructions.md
│   │   ├── host-setup.instructions.md
│   │   ├── repositories.instructions.md
│   │   ├── tests.instructions.md
│   │   └── validators.instructions.md
│   │
│   ├── skills/                          # Copy from CoreEx
│   │   ├── acquire-codebase-knowledge/
│   │   ├── add-capability/
│   │   ├── aspire/
│   │   ├── coreex-exec-plan/
│   │   └── generate-domain/
│   │
│   ├── templates/                       # Copy from CoreEx
│   │   └── domain/
│   │
│   ├── copilot-instructions.md          # Copy & customize
│   ├── INSTRUCTION_AUTHORING.md         # Copy (reference)
│   └── SKILL_AUTHORING.md               # Copy (reference)
│
├── .agent/
│   ├── execplans/                       # Create (empty folder)
│   ├── PLANS.md                         # Create (index file)
│   ├── PACKAGING_CHECKLIST.md           # Create (this file)
│   └── templates/
│       └── copilot-instructions-template.md  # Copy for reference
│
├── docs/
│   ├── capabilities.md                  # Copy & customize
│   ├── agent-interaction-guide.md       # Copy (keep as-is or customize)
│   └── application-scaffolding-guide.md # Copy & customize
│
├── samples/                             # Copy (optional)
│
└── README.md                            # Create/Update
```

---

## After Extraction: Customization Phases

### Phase 1: Customize `copilot-instructions.md`

**Location:** `.github/copilot-instructions.md`

This is the main guidance document. Update these sections:

1. **Header**
   ```markdown
   description: "Project-wide guidelines for {{YOUR_PROJECT}} development"
   ```

2. **Purpose**
   ```markdown
   {{YOUR_PROJECT}} is a {{DESCRIPTION}}.
   Favor {{YOUR_FRAMEWORK}}-native primitives...
   ```

3. **Repository Shape**
   - Replace folder structure with your actual layout
   - Update project names and layer names

4. **Build, Test, and Run**
   ```
   - Build: dotnet build {{YOUR_PROJECT}}.sln
   - Test:  dotnet test {{YOUR_PROJECT}}.sln
   - Run:   {{YOUR_RUN_COMMAND}}
   ```

5. **Architecture**
   - Replace CoreEx patterns with your patterns
   - Update service/repository conventions
   - Update exception types and Result<T> patterns

6. **Key Conventions**
   - Replace `CoreEx` with your framework name
   - Update service registration patterns (if different)
   - Update validation framework references
   - Update data layer patterns (EF, ADO.NET, etc.)

7. **Instruction Files Table**
   - Update file paths if you rename any `.instructions.md` files
   - Remove instruction files you don't use (e.g., if no events: remove `event-subscribers.instructions.md`)

8. **Skills Section**
   - Keep if using domain generation
   - Update skill names to match your project context
   - Remove skills that don't apply

---

### Phase 2: Customize Individual Instruction Files

**Location:** `.github/instructions/*.md`

Each file may need these updates:

| File | Find & Replace | Purpose |
|------|-----------------|---------|
| **All** | `CoreEx` → `{{YOUR_FRAMEWORK}}` | Framework references |
| **All** | `ScopedService<T>` → `YOUR_DI_PATTERN` | DI conventions |
| **application-services** | `IUnitOfWork` → `YOUR_TRANSACTION_PATTERN` | Transactional boundaries |
| **repositories** | `EFCore` sections | Data access layer specifics |
| **host-setup** | Service Bus, outbox references | Messaging patterns |
| **contracts** | `[Contract]`, `IETag` | DTO generation |
| **tests** | `WithApiTester<Program>` | Test base classes |

**Tip:** Use Find & Replace (Ctrl+H) in VS Code to batch-update across instruction files.

---

### Phase 3: Customize Docs

**Location:** `docs/`

- **`capabilities.md`** — Rename or rewrite to describe YOUR framework capabilities
- **`agent-interaction-guide.md`** — Keep as-is (generic guidance)
- **`application-scaffolding-guide.md`** — Update with your patterns
- **Add new files** for project-specific patterns

---

### Phase 4: Update README.md

**Location:** `README.md`

Include:
- Project description
- Quick start commands
- Link to `.github/copilot-instructions.md`
- Link to `docs/capabilities.md`
- Instructions on how to invoke Copilot skills (`/generate-domain`, etc.)

**Example:**
```markdown
# {{PROJECT_NAME}}

{{DESCRIPTION}}

## Quick Start

```bash
dotnet build
dotnet test
```

## Using Copilot Skills

This repository includes agent-powered skills for rapid development:

- `/generate-domain` — Scaffold a new domain with contracts, services, and repositories
- `/add-capability` — Add messaging or integration features
- `/coreex-exec-plan` — Create execution plans before coding

See [Copilot Instructions](.github/copilot-instructions.md) for details.

## Architecture

See [Capabilities Documentation](docs/capabilities.md).
```

---

### Phase 5: Customize Skills (If Needed)

**Location:** `.github/skills/`

Only customize if needed for your specific project:

- **`generate-domain/`** — Update domain templates and naming conventions
- **`add-capability/`** — Update capability scaffolding
- **`coreex-exec-plan/`** — Keep as-is (generic planning structure)
- **`aspire/`** — Remove if not using Aspire orchestration
- **`acquire-codebase-knowledge/`** — Keep as-is (generic exploration)

---

### Phase 6: Customize Templates

**Location:** `.github/templates/domain/`

Update code generation templates for:
- Namespace conventions (e.g., `{{ProjectName}}.{{DomainName}}.{{Layer}}`)
- Layer names (Contracts, Application, Infrastructure, etc.)
- Property attributes and patterns
- Service registration patterns

---

## Validation Checklist

After customization, verify:

- [ ] `.github/copilot-instructions.md` describes YOUR project (not CoreEx)
- [ ] All instruction files reference YOUR framework/patterns (not CoreEx patterns)
- [ ] `README.md` includes quick start and Copilot skill references
- [ ] Docs in `docs/` describe YOUR capabilities
- [ ] `.agent/PLANS.md` exists
- [ ] Sample domains (if included) use YOUR naming and patterns
- [ ] `dotnet build` succeeds (if a .NET project)
- [ ] Copilot can see your instructions (open a file, invoke `/` to see skills)

---

## Testing Your Package

1. **Clone the new repository**
2. **Open in VS Code**
3. **Type `/` in a chat message** — Should see your skills listed
4. **Open a `.cs` file** in a layer folder (e.g., `Application/Services/`) — Should see instruction reference in Copilot UI
5. **Invoke a skill** (e.g., `/generate-domain`) — Should scaffold code matching your conventions

---

## Sharing with Your Team

Once customized:

1. **Commit to version control**
   ```bash
   git add .github/ .agent/ docs/ README.md
   git commit -m "Add agentic instruction package"
   ```

2. **Share with team**
   - Point to `README.md` for quick start
   - Direct to `.github/copilot-instructions.md` for conventions
   - Encourage use of skills for consistent scaffolding

3. **Maintain over time**
   - Update instruction files as conventions evolve
   - Add new skills for recurring patterns
   - Keep `docs/` in sync with codebase changes

---

## FAQ

**Q: Should I include `samples/`?**  
A: Yes, if you have reference implementations. No, if your patterns are simple or well-documented elsewhere.

**Q: Can I remove some instruction files?**  
A: Yes. Remove instruction files that don't apply to your project (e.g., if no events: remove `event-subscribers.instructions.md`).

**Q: How often should I update instruction files?**  
A: Whenever your coding standards or patterns change. Treat them as "living documentation."

**Q: Can I customize the skills?**  
A: Yes. See `.github/SKILL_AUTHORING.md` for the structure and rules.

**Q: What if my project doesn't use .NET?**  
A: You can still use this package; just update instruction files to your language/framework and remove .NET-specific patterns.

**Q: How do I add new skills?**  
A: Create a new folder in `.github/skills/\<name\>/` following the structure in existing skills. Reference `.github/SKILL_AUTHORING.md`.

---

## Need Help?

- **Instruction file syntax:** See `.github/INSTRUCTION_AUTHORING.md`
- **Skill structure:** See `.github/SKILL_AUTHORING.md`
- **CoreEx patterns:** See original [CoreEx README](https://github.com/Avanade/CoreEx)
- **Copilot customization:** See VS Code [Chat Customizations](https://code.visualstudio.com/docs/copilot/chat-reference)

