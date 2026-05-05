---
agent: agent
tools: ['create', 'read', 'search', 'todo']
description: "Fast-path domain scaffolding: clone and materialize the canonical templates in .github/templates/domain/ with placeholder substitution. Use when you want exact template output with no creative generation — entity fields match the template shape exactly. For custom entity fields or reasoning about your domain model, use /generate-domain instead."
---

Scaffold a new CoreEx domain by cloning and materializing files from `/.github/templates/domain/**`.

## Purpose

Use this prompt when:
- **Speed is the priority** and the entity fields match the template shape (Id, ETag, ChangeLog, one status ref-data, optional child entity).
- You want **exact, deterministic output** — every file is copied verbatim from the templates with placeholder substitution, no reasoning or generation.
- You do not need the agent to inspect existing sample source code.

Use the `/generate-domain` skill instead when:
- Your entity has **custom fields, types, or business rules** that go beyond what the templates express.
- You want the agent to **reason about your domain model** and apply conventions (validation rules, event naming, query config) appropriately.
- You are unsure which operations or patterns to include and want guided scaffolding.

## Inputs Required

If not supplied, ask for:

1. `Solution` (e.g. `Contoso`).
2. `Domain` (e.g. `Orders`).
3. `Entity` (e.g. `Order`).
4. `ChildEntity` (e.g. `OrderItem`).
5. `targetRoot` (default: `samples/src`).

## Naming Helper (Auto-Derive)

Derive naming values from `Entity` unless the user explicitly overrides them:

- `EntityPlural` = English plural form of `Entity`.
	- Default rule: append `s`.
	- If ends with `y` preceded by a consonant: replace `y` with `ies`.
	- If ends with `s`, `x`, `z`, `ch`, `sh`: append `es`.
	- Preserve casing (e.g. `Order` -> `Orders`, `Category` -> `Categories`).
- `entityKebab` = kebab-case of `Entity`.
- `entityPluralKebab` = kebab-case of `EntityPlural`.
- `EntityPluralVar` = `EntityPlural` unless overridden.

Example:

- `Entity = Order` -> `EntityPlural = Orders`, `entityKebab = order`, `entityPluralKebab = orders`.
- `Entity = Category` -> `EntityPlural = Categories`, `entityKebab = category`, `entityPluralKebab = categories`.

## Placeholders to Replace

For every template file, replace all placeholders:

- `{Solution}`
- `{Domain}`
- `{Entity}`
- `{ChildEntity}`
- `{EntityPlural}`
- `{EntityPluralKebab}` where present
- `{entityKebab}`
- `{entityPluralKebab}`
- `{EntityPlural}` in class/type names
- `{EntityPlural}` / `{EntityPluralVar}` in repository/EfDb property names

If `EntityPluralVar` is not supplied, default to `{EntityPlural}`.

## Output Projects

Create these projects under `{targetRoot}`:

- `{Solution}.{Domain}.Contracts`
- `{Solution}.{Domain}.Application`
- `{Solution}.{Domain}.Infrastructure`
- `{Solution}.{Domain}.Api`
- `{Solution}.{Domain}.Database`

## Materialization Rules

1. Copy each `.template` file into the corresponding project location.
2. Remove `.template` suffix from output files.
3. Rename `Domain.*.csproj.template` to `{Solution}.{Domain}.*.csproj`.
4. Rename `Entity*` files to use concrete entity names.
5. Keep folder structure identical to template tree.
6. Preserve line endings and indentation.

## Required Post-Generation Adjustments

After template materialization:

1. In API controllers:
- Ensure routes use concrete kebab-case paths.
- Verify OpenApi tags use `{EntityPlural}`.

2. In Database seed data:
- If status model is used, ensure `Pending`, `Confirmed`, `Cancelled` values are present unless the caller supplied alternatives.

3. In Infrastructure repository:
- Ensure EfDb mapped model property uses concrete plural entity name.

4. In Program files:
- Ensure namespaces match generated project names.

## Validation

Run `dotnet build` for all generated projects to check for compilation errors:

- `{targetRoot}/{Solution}.{Domain}.Contracts`
- `{targetRoot}/{Solution}.{Domain}.Application`
- `{targetRoot}/{Solution}.{Domain}.Infrastructure`
- `{targetRoot}/{Solution}.{Domain}.Api`
- `{targetRoot}/{Solution}.{Domain}.Database`

If errors are found, fix them before completing.

## Completion Gate

Use `/.github/templates/domain/DomainScaffold.checklist.md` as the final acceptance checklist. Do not finish until all applicable items are satisfied.
