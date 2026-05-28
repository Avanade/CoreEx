---
description: "Standards for creating and maintaining SKILL.md files"
applyTo: "**/.github/skills/**/SKILL.md"
tags: ["authoring", "standards", "skills", "documentation"]
---

# Skill File Authoring Standards

When creating or updating any skill (SKILL.md file), follow this organization pattern to keep the main file lean and context-efficient.

## Purpose

Skill files guide AI agents through complex, multi-step tasks. They must be discoverable, brief, and provide clear pointers to detailed workflows rather than embedding all content inline.

## Skill Directory Structure

```
skills/{skill-name}/
  SKILL.md                    # Main entry point (lean, <300 lines)
  references/
    workflow.md               # Detailed step-by-step workflows
    checklists.md            # Completion gates, validation criteria
    patterns.md              # Code patterns, templates, conventions
    troubleshooting.md       # Known issues and solutions
  assets/
    templates/               # Code templates, boilerplate files
    examples/                # Real working examples from the repo
```

Each file serves one purpose. Keep files focused and scannable.

## SKILL.md Content Rules

The main SKILL.md must include:

1. **YAML frontmatter** with `name`, `description`, `argument-hint`, `tags`
2. **One-sentence purpose statement** — what the skill does and when to use it
3. **"When to Use" section** — bullet points, not prose; concrete triggers
4. **"When Not to Use" section** — prevents misuse and clarifies boundaries
5. **Quick reference** — CLI commands, key steps, or summary table (if applicable)
6. **Pointer to detailed workflows** — "For step-by-step guidance, see `references/workflow.md`"
7. **Key References** — links to relevant instructions, samples, or external docs

**Maximum: 300 lines** including frontmatter. If you exceed this, move content to `references/`.

## references/ Subdirectory

Detailed, procedural content lives in `references/`:

- **workflow.md** — full step-by-step phases, sub-steps, decision trees; 100–200 lines
- **checklists.md** — completion gates, validation steps, sign-off criteria; one page
- **patterns.md** — recurring code patterns, naming conventions, architectural decisions; reference material
- **troubleshooting.md** — known issues, debugging strategies, error messages and fixes; searchable format

Each file stays focused on one concern. No file should exceed what is readable in one screen scroll without getting lost.

## assets/ Subdirectory

Reusable templates and examples:

- **assets/templates/** — boilerplate code, project structures (copy-and-fill files)
- **assets/examples/** — concrete working examples from the repo (links only, no duplicates)

**Important**: Never maintain duplicate copies of sample code. Always link to the canonical source in `samples/` or other repository locations.

## Cross-Referencing

When skills reference each other, instructions, or samples:

- **Relative paths**: `../other-skill/references/...` (for other skills)
- **Absolute workspace paths**: `/.github/instructions/coreex-host-setup.instructions.md`, `/samples/src/Contoso.Products.Api/Program.cs`
- Always verify links work before committing
- Prefer workspace-relative links for durability

## Frontmatter Requirements

All SKILL.md files must include:

```yaml
---
name: skill-id
description: "Concise description of when and why to use this skill"
argument-hint: "What user should provide, e.g. 'Optional: domain name and entities'"
tags: ["tag1", "tag2", "tag3"]
---
```

**Tag guidance**: Reflect the skill's domain and primary use cases. Examples:
- `["scaffolding", "microservice", "code-generation"]`
- `["orchestration", "cli", "distributed-apps"]`
- `["retrofit", "integration", "messaging"]`

## Lean SKILL.md Example

```yaml
---
name: generate-domain
description: "Scaffold a new CoreEx domain across all layers following framework conventions"
argument-hint: "Domain name, entity fields, business rules (optional)"
tags: ["scaffolding", "domain", "code-generation"]
---

# Generate Domain

Guides you through creating a new CoreEx domain from scratch. Asks about entity shape, validation, messaging needs, and generates code tailored to your domain model.

## When to Use

- Creating a new bounded context or microservice
- Entity has custom fields, business rules, or complex validation
- You want the agent to reason about conventions and event naming
- Scaffolding Products, Orders, Shopping, or similar sample domains

## When Not to Use

- Entity fits a standard template shape — use `/scaffold-domain-from-templates` instead
- Domain already exists — use `/add-capability` to retrofit messaging/integration
- You need just one file (a contract or service) — manually create it

## Workflow Overview

1. **Load Context** — examine existing domains and conventions
2. **Gather Inputs** — domain name, entity fields, validation rules, events
3. **Contracts** — define DTOs with source-generation markers
4. **Application Services** — validation, unit-of-work patterns, event publishing
5. **Infrastructure** — repositories, mappers, database access
6. **API Host** — controllers, registration, middleware
7. **Database** — migrations, schema, outbox tables
8. **Tests** — integration and API test scaffolding

For detailed step-by-step guidance, see [`references/workflow.md`](references/workflow.md).

## Key References

- [Application Services Instructions](/.github/instructions/coreex-application-services.instructions.md)
- [Contracts Instructions](/.github/instructions/coreex-contracts.instructions.md)
- [Host Setup Instructions](/.github/instructions/coreex-host-setup.instructions.md)
- [Sample Domains](./samples/src/Contoso.Products/)
- [Roslyn Source Generation](./docs/capabilities.md)
```

## Quality Gates

Before completing a skill:

- [ ] SKILL.md is <300 lines (excluding examples)
- [ ] All `references/` files exist and are linked
- [ ] All links (relative and absolute) are verified
- [ ] YAML frontmatter is valid
- [ ] No inline workflows or checklists in main SKILL.md
- [ ] Cross-references to instructions are correct
- [ ] Example links point to real, canonical code locations
