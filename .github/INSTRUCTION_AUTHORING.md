---
description: "Standards for creating and maintaining .instructions.md files"
applyTo: "**/*.instructions.md"
tags: ["authoring", "standards", "instructions", "documentation"]
---

# Instruction File Authoring Standards

When creating or updating any Copilot instruction Markdown file (`.instructions.md`), follow these rules to keep guidance durable, easy to review, and maintainable.

## Purpose

Instruction files define scoped AI guidance for specific file types or code areas. They must be predictable and machine-readable.

## General Authoring Rules

- Prefer precise, testable directives over vague guidance.
- Avoid overlapping or conflicting instructions across files.
- Keep content reusable and not tied to one temporary task.
- Use imperative language ("Use", "Prefer", "Do not", "Validate").
- If a rule is scoped to a subset of files, use a path-specific `.instructions.md` file rather than adding it to the global file.
- Do not restate general rules in multiple files unless required for clarity.
- When unsure, produce fewer, clearer rules.

## Required Format

All `.instructions.md` files must begin with YAML frontmatter and follow this section order:

```yaml
---
description: "Short, concrete summary of what the file governs"
applyTo: "src/**/*.cs"
tags: ["tag1", "tag2"]
---

# Purpose

[One paragraph explaining why this guidance exists]

## Scope

[What files and scenarios this applies to]

## Required Rules

[MUST / MUST NOT directives, phrased imperatively]

## Preferred Patterns

[SHOULD directives, repeated patterns, best practices]

## Validation

[Concrete checks: build, test, lint, docs validation]

## Examples

[Optional: "Preferred" and "Avoid" patterns]
```

## Frontmatter Rules

- `description` must be one sentence and concrete.
- `applyTo` must use explicit glob patterns with the narrowest safe scope. Examples:
  - `src/**/*.cs` — all C# files in source
  - `tests/**/*.cs` — all test files
  - `**/Program.cs` — program entry points
- `applyTo` must **not** be `**` unless the file is intentionally repository-wide.
- If multiple globs are needed, keep them explicit and readable, separated by semicolons or as separate lines.
- `tags` should reflect the instruction's domain (e.g., `["validation", "dependency-injection", "testing"]`).

## Content Rules

- **Required rules** must be phrased as MUST / MUST NOT / SHOULD where possible.
- **Validation section** must include concrete checks (build, test, lint, docs validation) when applicable.
- **Examples** must show "preferred" and "avoid" patterns when useful.
- Do not include secrets, tokens, or environment-specific sensitive values.
- Keep sections short and scannable; each section should fit on one screen without scrolling.

## Conflict Resolution

When multiple instruction files might apply to the same file:

- Repository-wide instructions define defaults.
- Path-specific instruction files define narrower, stronger rules for matching files.
- If a new file would conflict with an existing instruction file, revise the narrow file instead of creating duplicate policy.
- Always document the relationship between overlapping instruction files in cross-references.

## Cross-Referencing

Link between instruction files using relative paths or workspace absolute paths:

- Relative: `../other-instruction.md`
- Absolute: `/.github/instructions/host-setup.instructions.md`
- Always verify links work before committing.

## Example Structure

A well-formed instruction file:

```yaml
---
description: "Controller conventions for CoreEx API hosts"
applyTo: "**/Controllers/**/*.cs"
tags: ["controllers", "api", "dependency-injection"]
---

# Purpose

Controllers define HTTP endpoints for CoreEx API hosts. This guidance ensures consistent routing, dependency injection, and use of CoreEx WebApi helpers.

## Scope

Applies to all `Controllers/` directories in API projects (`.Api` projects).

## Required Rules

- MUST inherit from `WebApiControllerBase<TRequest, TResponse>` or `WebApiControllerBase`.
- MUST use `[Route("api/v1/...")]` and follow REST conventions.
- MUST NOT inject `IUnitOfWork` directly; receive it only through application service dependency.

## Preferred Patterns

- Prefer `PostAsync()`, `PutAsync()`, `PatchAsync()` from `WebApi` helpers over manual response building.
- Prefer explicit dependency injection over service locator patterns.
- Prefer PATCH with `application/merge-patch+json` for partial updates.

## Validation

- Build the project: `dotnet build`
- Run tests: `dotnet test`
- Check inheritance with: `grep "class.*Controller" Controllers/*.cs`
```
