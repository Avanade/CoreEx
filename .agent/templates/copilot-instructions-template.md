---
description: "Project-wide guidelines and conventions for {{PROJECT_NAME}} development"
tags: ["guidelines", "conventions", "comments"]
---

# Copilot Instructions

## Purpose
{{PROJECT_DESCRIPTION}}

Favor {{PROJECT_DOMAIN}}-native primitives, patterns, and extensions over ad-hoc implementations.

## Repository Shape
- `{{PROJECT_NAME}}.sln`: main solution for framework + samples (or primary project structure).
- `src\`: reusable libraries and core functionality.
- `samples\`: reference implementations and example domains.
- `tests\`: project-level tests.
- `docs\`: documentation and guides.

**Example structure:**
```
{{PROJECT_NAME}}.sln
├── src/
│   ├── {{PROJECT_NAME}}.Core/
│   ├── {{PROJECT_NAME}}.Application/
│   └── {{PROJECT_NAME}}.Infrastructure/
├── samples/
│   ├── {{SAMPLE_DOMAIN_1}}/
│   └── {{SAMPLE_DOMAIN_2}}/
├── tests/
├── docs/
└── .github/
```

## Build, Test, and Run

- **Build**: `dotnet build {{PROJECT_NAME}}.sln`
- **Test**: `dotnet test {{PROJECT_NAME}}.sln`
- **Single test**: `dotnet test <proj> --filter "FullyQualifiedName~<name>"`
- **Run samples**: {{SAMPLE_RUN_INSTRUCTIONS}}
- **Linting**: {{LINTING_APPROACH}}
- **Formatting**: 4 spaces for `*.cs`, 2 spaces for `*.json|*.xml|*.yaml|*.props|*.csproj|*.sln|*.sql` per `.editorconfig`.

## Architecture

### Primary Patterns
- {{ARCHITECTURE_PATTERN_1}}
- {{ARCHITECTURE_PATTERN_2}}
- {{ARCHITECTURE_PATTERN_3}}

### Layering and Separation of Concerns
- **Contracts/DTOs**: {{CONTRACT_CONVENTIONS}}
- **Application Layer**: {{APPLICATION_LAYER_CONVENTIONS}}
- **Infrastructure**: {{INFRASTRUCTURE_CONVENTIONS}}
- **API/Hosts**: {{API_CONVENTIONS}}

### Example Domain Flow
{{DOMAIN_FLOW_DESCRIPTION}}

## Key Conventions That Matter in This Repo

### {{PROJECT_DOMAIN}}-First Patterns
- Prefer {{PROJECT_DOMAIN}} primitives before introducing external libraries that overlap with framework capabilities.
- Prefer {{PROJECT_DOMAIN}} exception types and Result<T> flows over custom error wrappers.
- {{PROJECT_SPECIFIC_PATTERN_1}}

### Contracts and Data Transfer
- {{CONTRACT_CONVENTION_1}}
- {{CONTRACT_CONVENTION_2}}
- {{DATA_MAPPING_STRATEGY}}

### Dependency Injection and Layering
- {{DI_PATTERN_1}}
- {{DI_PATTERN_2}}
- Keep interface/implementation layering intact.

### Application-Service Shape
- {{SERVICE_CONVENTION_1}}
- {{SERVICE_CONVENTION_2}}
- {{SERVICE_CONVENTION_3}}

### Data and Messaging
- {{DATA_PATTERN_1}}
- {{DATA_PATTERN_2}}

### Testing
- Test framework: {{TEST_FRAMEWORK}}
- Test structure: {{TEST_STRUCTURE}}
- Integration patterns: {{INTEGRATION_TEST_PATTERNS}}

### House Rules
- Code comments end with a period/full stop.
- Use `GlobalUsing.cs` per project; do not scatter `using` directives.
- Always use `.ConfigureAwait(false)` in service/repository code.
- {{PROJECT_SPECIFIC_RULE_1}}
- {{PROJECT_SPECIFIC_RULE_2}}

## Key Docs to Read Before Large Changes

- [README.md](../../README.md) for repo-level positioning and top-level commands.
- [capabilities.md](../../docs/capabilities.md) for the deeper {{PROJECT_DOMAIN}} capability/pattern explanations.
- `.github/instructions/` for area-specific rules when editing contracts, services, repositories, validators, hosts, or tests.

## Instruction Files

This repository includes scoped instruction files that define conventions for specific code areas.
Follow these files when creating or modifying code in their respective domains:

| File | Purpose | Applies To |
|------|---------|-----------|
| `api-controllers.instructions.md` | API controller conventions | `**/Controllers/**/*.cs` |
| `application-services.instructions.md` | Application service patterns | `**/Application/**/*.cs` |
| `contracts.instructions.md` | DTO/Contract conventions | `**/Contracts/**/*.cs` |
| `repositories.instructions.md` | Data access layer patterns | `**/Infrastructure/**/*.cs` |
| `validators.instructions.md` | Validation framework usage | `**/*Validator*.cs` |
| `tests.instructions.md` | Test conventions | `**/*.Test*/**/*.cs` |
| `host-setup.instructions.md` | Program.cs and host configuration | `**/Program.cs` |
| `event-subscribers.instructions.md` | Event/message subscriber patterns | `**/Subscribe/**/*.cs` |
| `database-project.instructions.md` | Database project structure | `**/*.Database/**` |

## Skills and Custom Agents

The following skills and agents are available to enhance development workflows:

### Available Skills
- `/generate-domain` — Scaffold a new domain across all layers with custom fields and business rules.
- `/add-capability` — Retrofit an existing domain with messaging, integration, or subscriber capabilities.
- `/aspire` — Orchestrate distributed applications using Aspire CLI.
- `/coreex-exec-plan` — Create structured execution plans before implementation.

### Available Agents
- **{{PROJECT_DOMAIN}} Expert** — Explain patterns, architecture decisions, and coding guidance.
- **Explore** — Fast codebase exploration and Q&A.

## Guidance for Authoring Instructions and Skills

When creating or maintaining Copilot instruction files and skills:

- **Instruction files** (`.instructions.md`) — See `.github/INSTRUCTION_AUTHORING.md` for standards on YAML frontmatter, section order, and content rules.
- **Skill files** (`SKILL.md`) — See `.github/SKILL_AUTHORING.md` for directory structure pattern (`references/`, `assets/`), lean main file rules, and cross-referencing guidelines.

Both documents define durable patterns for creating guidance that is discoverable, maintainable, and context-efficient.
