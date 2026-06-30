---
name: coreex-db-migration
description: "Add or change a database table for a CoreEx domain. USE FOR: new transactional table, new reference-data table, altering an existing table (columns, indexes, constraints), or any other schema change (indexes, functions, stored procs). Scaffolds the correct migration script, updates dbex.yaml, applies the migration, and regenerates Infrastructure persistence models. DO NOT USE FOR: outbox provisioning (use dotnet run -- script outbox directly), seed data only changes (dotnet run -- Data), or CoreEx contract/service generation (that is *.CodeGen, not *.Database)."
argument-hint: "Optional: entity/table name, schema, SQL Server vs PostgreSQL, nature of change"
tags: ["database", "migration", "dbex", "schema", "efcore", "coreex"]
---

# CoreEx: DB Migration

Guides you through any database schema change for a CoreEx domain — from choosing the right migration script through to regenerated EF persistence models.

## When to Use

- Adding a new table for a new entity (transactional or reference-data)
- Altering an existing table — adding/modifying/removing columns, indexes, or constraints
- Any other schema change that needs to flow through to regenerated `*.g.cs` Infrastructure files
- Non-entity schema changes (adding an index, a unique constraint, a function)

## When Not to Use

- Provisioning the transactional outbox — run `dotnet run -- script outbox <schema> <name>` directly; see `coreex-tooling.instructions.md`
- Changing reference-data seed rows only — edit `Data/ref-data.seed.yaml` and run `dotnet run -- Data`
- Generating CoreEx contracts/services/repositories — that is `*.CodeGen`, not `*.Database`
- Runtime or deployment issues

## Quick Reference

All commands run from the `*.Database` project directory.

| Task | Command |
|---|---|
| Bring DB up to date | `dotnet run -- database` |
| Inspect current table state | `dotnet run -- inspect <schema> <table>` |
| New transactional table | `dotnet run -- script create <schema> <table>` |
| New reference-data table | `dotnet run -- script refdata <schema> <table>` |
| Alter existing table | `dotnet run -- script alter <schema> <table>` |
| Non-entity schema change | `dotnet run -- script` |
| Apply everything + regenerate | `dotnet run -- All` |
| Drop + full rebuild (destructive, confirm first) | `dotnet run -- dropandall --accept-prompts` |

## Naming

- Script commands produce a file named `yyyyMMdd-HHmmss-<descriptor>.{sql|pgsql}` using the current UTC date and time.
- For `create`, `refdata`, and `alter` the descriptor is auto-derived from the table name.
- For bare `script` (non-entity changes), DbEx names the file with a placeholder suffix — **rename it immediately** to a 3–5 word kebab summary of intent (e.g. `add-unique-code-index`, `drop-legacy-status-column`).
- The complete filename (timestamp + descriptor + extension) **must not exceed 255 characters**. The project name and Migrations folder (e.g. `Contoso.Products.Database.Migrations`) are used as an embedded-resource name prefix internally, so keep descriptors concise.

## Polyglot Note

| Provider | Script extension | Casing |
|---|---|---|
| PostgreSQL | `.pgsql` | `snake_case` |
| SQL Server | `.sql` | `PascalCase` |

Check the project's `*.Database/Program.cs` or `appsettings.json` to confirm the provider in use.

For the full step-by-step decision tree, SQL column templates, and guardrails see [`references/workflow.md`](references/workflow.md).

## Key References

- [`/.github/instructions/coreex-tooling.instructions.md`](/.github/instructions/coreex-tooling.instructions.md) — DbEx command reference, `dbex.yaml` structure, SQL conventions, outbox provisioning
- [`/.github/instructions/coreex-repositories.instructions.md`](/.github/instructions/coreex-repositories.instructions.md) — what the generated `*.g.cs` feeds into
- [`/samples/src/Contoso.Products.Database/`](/samples/src/Contoso.Products.Database/) — canonical PostgreSQL domain example
- [`/samples/src/Contoso.Shopping.Database/`](/samples/src/Contoso.Shopping.Database/) — canonical SQL Server domain example
