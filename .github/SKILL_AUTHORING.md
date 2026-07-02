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
- **assets/examples/** — concrete working examples (links only, no duplicates)

**Important**: Never maintain duplicate copies of sample code.

## Cross-Referencing (consumer-agnostic)

> **These skills ship to consumer repositories** via `dotnet new coreex-ai` (the `CoreEx.Template` pack).
> A consumer repo does **not** contain this repository's `samples/` or `src/CoreEx.*` source. A skill must
> therefore be **domain-agnostic** and must not link to local paths that only exist inside the CoreEx repo.
> A link that resolves here but is dead in a consumer repo is a defect.

Reference targets, in priority order:

1. **Installed instructions** — always present in a consumer repo. Absolute workspace path, markdown link:
   `[Application Services](/.github/instructions/coreex-application-services.instructions.md)`.
2. **Sibling skills** — relative markdown link to the other skill: `[coreex-repository](../coreex-repository/SKILL.md)`.
   For "invoke this skill instead" mentions, a bare backtick name (`` `coreex-repository` ``) is fine — that is how a
   user invokes it.
3. **Labeled illustrative examples** — concrete `samples/` or `src/CoreEx.*` code linked as a **full GitHub URL**,
   clearly marked as an external example, e.g.
   `[ProductController (CoreEx sample — illustrative, not in your project)](https://github.com/Avanade/CoreEx/tree/main/samples/src/Contoso.Products.Api/Controllers)`.
4. **docs-sync cache** (`/.github/docs/coreex/*.md`, `/.github/docs/coreex/agents/*.md`) — optional, secondary.
   Present after `dotnet new coreex-ai` (it now ships the cache) and refreshable via `/coreex-docs-sync`. Never the
   *only* pointer, so a skill still works if the cache is absent.

Rules:
- **Never** use a bare local `samples/…` or `src/CoreEx…` path in a skill — use a GitHub URL (target 3) instead.
- Keep skill bodies domain-agnostic: placeholders (`{solution}`, `{domain}`, `{Entity}`) in copyable code;
  concrete sample names only in prose, clearly framed as examples ("e.g. …").
- Verify every link before committing; prefer the always-present targets (1, 2) as the backbone.

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
name: coreex-design-review
description: "Review a proposed CoreEx design against repository conventions and samples"
argument-hint: "Feature, domain, or host shape to review"
tags: ["design-review", "architecture", "coreex"]
---

# CoreEx Design Review

Reviews a proposed feature or service shape against current CoreEx guidance. Focuses on layering, host responsibilities, validation, persistence, and messaging choices.

## When to Use

- Reviewing a proposed API, subscriber, relay, or orchestration shape
- Comparing multiple implementation options before coding
- Checking whether a design aligns to existing samples and instructions
- Creating a small evidence-backed plan for a change

## When Not to Use

- Deterministic project scaffolding — use the `CoreEx.Template` `dotnet new` templates instead
- Repository onboarding — use `/acquire-codebase-knowledge`
- Running the local distributed app — use `/aspire`

## Workflow Overview

1. **Inspect Context** — examine the current domain, hosts, and nearby sample
2. **Identify Shape** — decide API-only, API plus relay, API plus subscriber, or orchestration
3. **Check Conventions** — validate layering, instructions, and naming expectations
4. **Assess Risks** — call out migration, compatibility, and operational tradeoffs
5. **Recommend Next Steps** — provide the smallest safe implementation path

For detailed step-by-step guidance, see [`references/workflow.md`](references/workflow.md).

## Key References

- [Application Services Instructions](/.github/instructions/coreex-application-services.instructions.md)
- [Contracts Instructions](/.github/instructions/coreex-contracts.instructions.md)
- [Host Setup Instructions](/.github/instructions/coreex-host-setup.instructions.md)
- [coreex-app-service](../coreex-app-service/SKILL.md) — related skill
- [Application layer deep-dive](/.github/docs/coreex/application-layer.md) — optional (after `/coreex-docs-sync`)
- [ProductService (CoreEx sample — illustrative)](https://github.com/Avanade/CoreEx/tree/main/samples/src/Contoso.Products.Application)
```

## Quality Gates

Before completing a skill:

- [ ] SKILL.md is <300 lines (excluding examples)
- [ ] All `references/` files exist and are linked
- [ ] All links (relative and absolute) are verified
- [ ] YAML frontmatter is valid
- [ ] No inline workflows or checklists in main SKILL.md
- [ ] Cross-references to instructions are correct
- [ ] **No bare local `samples/…` or `src/CoreEx…` paths** — illustrative examples use full GitHub URLs, labeled
- [ ] Body is domain-agnostic — placeholders in code; sample names only as framed prose examples
- [ ] The reference backbone (instructions + sibling skills) resolves in a consumer repo without the docs cache
