---
applyTo: "**/*.CodeGen/Program.cs;**/*.CodeGen/ref-data.yaml;**/*.Database/Program.cs;**/*.Database/dbex.yaml;**/*.Database/Migrations/**;**/*.Database/Data/**;**/*.Database/Schema/**;!**/*.Database/Schema/**/*.g.*"
description: "Developer tooling conventions: *.CodeGen reference-data C# code generation and *.Database schema migration, DbEx commands, seed data, and outbox provisioning"
tags: ["tooling", "codegen", "database", "migrations", "dbex", "reference-data", "outbox"]
---

# Developer Tooling Conventions

Each domain has two developer-time tooling projects that have **no runtime presence**. They run locally during development and in CI/CD pipelines to generate code and manage the database schema.

| Project | Purpose |
|---|---|
| `*.CodeGen` | Generates reference-data C# artefacts across all layers from `ref-data.yaml` |
| `*.Database` | Manages the full database lifecycle — schema, seed data, outbox provisioning, and Infrastructure C# code generation |

---

## `*.CodeGen` — Reference-Data C# Code Generation

### How it works

`Program.cs` is minimal — it delegates entirely to `CodeGenConsole`:

```csharp
await CoreEx.CodeGen.CodeGenConsole.Create().RunAsync(args);
```

Running `dotnet run` reads `ref-data.yaml`, validates it against the CoreEx JSON Schema, evaluates the embedded Handlebars templates via [OnRamp](https://github.com/Avanade/OnRamp), and writes `.g.cs` files into the correct target project directories (resolved automatically by convention from the CodeGen project location).

### What is generated

| Artefact | Target layer | Description |
|---|---|---|
| `*.g.cs` contract class | Contracts | Typed reference-data entity contract extending `ReferenceData<TSelf>`, decorated with `[ReferenceData]` which triggers the Roslyn source generator to emit additional members at compile time |
| `*.g.cs` controller route | API host | HTTP GET endpoint exposing the entity collection |
| `*.g.cs` service method | Application | Service method delegating to the repository |
| `*.g.cs` repository interface | Application | `IXxxRepository` interface declaration |
| `*.g.cs` repository | Infrastructure | EF Core repository implementation |
| `*.g.cs` mapper | Infrastructure | `BiDirectionMapper` for the entity |

All outputs carry the `.g.cs` suffix and must never be edited directly — regenerate by re-running `dotnet run`.

### `ref-data.yaml` structure

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Avanade/CoreEx/refs/heads/main/schema/coreex-refdata.json
collectionSortOrder: Code       # sort order applied to all collection types
repository: EntityFramework     # repository implementation strategy
entities:
- name: Brand                   # simplest form — all defaults apply
- name: Category
- name: SubCategory
  properties:
  - name: CategoryCode
    type: ^Category             # ^ prefix = typed reference-data property (generates navigation accessor)
- name: UnitOfMeasure
  plural: UnitsOfMeasure        # override pluralization where irregular
  idType: Guid                  # override identifier type; defaults to string
  properties:
  - name: Scale
    type: int                   # additional stored column beyond the standard ReferenceData fields
- name: DiscountCoupon
  properties:
  - name: DiscountPercentage
    type: decimal
    excludeContract: true       # exclude from generated contract (present in persistence model only)
```

Add the `$schema` annotation to the file for IDE YAML validation and auto-complete.

The standard `IReferenceData` properties (`Id`, `Code`, `Text`, `Description`, `SortOrder`, `IsActive`, etc.) are automatically included in every generated type — do not declare them under `properties:`. Only additional domain-specific columns need to be listed; most reference data entities require no `properties:` entry at all.

Key `entities:` options:

| Key | Required | Default | Purpose |
|---|---|---|---|
| `name` | Yes | -- | Entity name (PascalCase) |
| `plural` | No | Auto-pluralized | Override when pluralization is irregular |
| `idType` | No | `string` | Identifier type override (e.g. `Guid`, `int`) |
| `properties[].name` | Yes (if any) | -- | Additional stored property name |
| `properties[].type` | Yes (if any) | -- | CLR type; prefix `^` for a ref-data navigation accessor |
| `properties[].excludeContract` | No | `false` | Exclude from the generated contract (persistence only) |

> **Agent instruction:** When asked to create or modify a reference data type:
> 1. Add or update the entry under `entities:` in `ref-data.yaml`.
> 2. Offer to run `dotnet run` from the `*.CodeGen` directory on the user's behalf.
> 3. If confirmed, execute it and summarise the generated artefacts on success; on failure relay the **complete output verbatim** — it provides the diagnostic needed to fix the entry.
> 4. On failure, fix the issue in `ref-data.yaml` and offer to re-run -- do not create or edit `.g.cs` files to work around a generation error.
> 5. If the user declines, remind them to run `dotnet run` from the `*.CodeGen` directory before the new types are available.
>
> **Do not pre-create output directories.** A configured target path (e.g. `apiProjectPath`) that points to a not-yet-existing project directory does **not** cause CodeGen to fail — it simply emits a *warning* and skips that output. Never create an empty directory, stub project, or placeholder file to "unblock" code generation. If a target project is genuinely missing and its artefacts are needed, raise it with the user rather than fabricating the folder.

---

## `*.Database` — Database Lifecycle Management

### NuGet / Project References

| Package | Use case |
|---|---|
| `DbEx.SqlServer` + `DbEx.SqlServer.Console` | SQL Server domains |
| `DbEx.Postgres` + `DbEx.Postgres.Console` | PostgreSQL domains |
| `CoreEx.Database` | `SqlStatement` type — add its assembly to the migration runner for extended schema scripts |

> **Polyglot**: Use `PostgresMigrationConsole` with `.pgsql` scripts and PostgreSQL functions for PostgreSQL domains. Use `SqlServerMigrationConsole` with `.sql` scripts and stored procedures for SQL Server domains. Choose the correct package per domain.

### `Program.cs` pattern

```csharp
// PostgreSQL domain example
public static Task<int> Main(string[] args) => PostgresMigrationConsole
    .Create<Program>("Server=127.0.0.1;Database=mydb;Username=postgres;Password=...")
    .Configure(c => ConfigureMigrationArgs(c.Args))
    .RunAsync(args);

// SQL Server domain example
public static Task<int> Main(string[] args) => SqlServerMigrationConsole
    .Create<Program>("Data Source=127.0.0.1,1433;Initial Catalog=MyDb;User id=sa;Password=...")
    .Configure(c => ConfigureMigrationArgs(c.Args))
    .RunAsync(args);

public static MigrationArgs ConfigureMigrationArgs(MigrationArgs args)
{
    args.AddAssembly<SqlStatement>().AddAssembly<Program>()
        .IncludeExtendedSchemaScripts()
        .DataParserArgs
            .RefDataColumnDefault("SortOrder", _ => 0)        // standard — always include; defaults every ref-data row's SortOrder to zero
            .RefDataColumnDefault("Scale", _ => 0);           // domain-specific example — only needed when a ref-data entity has a Scale column (e.g. UnitOfMeasure); omit if not applicable

    // Scope data reset to this domain's schema only.
    args.DataResetFilterPredicate = ts => ts.Schema == "{domain-schema}";
    return args;
}
```

### DbEx commands

Run with `dotnet run -- <command>`. Default (no arguments) runs `All`.

| Command | Description |
|---|---|
| `Create` | Creates the database if it does not exist |
| `Migrate` | Applies outstanding ordered migration scripts; tracks applied scripts — only new ones run |
| `CodeGen` | Generates Infrastructure `.g.cs` persistence models and `DbContext` partial from `dbex.yaml` |
| `Schema` | Drops and re-creates idempotent schema objects from `Schema/` on every run (stored procs, functions) |
| `Data` | Applies YAML/JSON seed data with INSERT or MERGE semantics |
| `Reset` | Deletes all data from the database (scoped by `DataResetFilterPredicate`) |
| `Script` | Scaffolds a new timestamped migration script file |
| `Drop` | Drops the database |
| `Inspect` | Read-only; reports existence and current column schema (type, nullability, default, PK, identity, computed, unique, and whether it is reference data) for one or more tables in a schema, as markdown. Safe to run freely |

Composite commands for common scenarios:

| Composite | Runs |
|---|---|
| `All` | `Create` → `Migrate` → `CodeGen` → `Schema` → `Data` |
| `Deploy` | `Migrate` → `Schema` |
| `DeployWithData` | `Migrate` → `Schema` → `Data` |
| `CreateMigrateAndCodeGen` | `Create` → `Migrate` → `CodeGen` |
| `ResetAndAll` | `Reset` → `All` |

### Provisioning the Transactional Outbox

The outbox table(s) are created via a DbEx-generated migration script — never hand-authored. This applies only to domains that have a `*.Database` project (i.e. a `data-provider` other than `None`).

When a solution is scaffolded with outbox enabled, the `coreex` template **already ships the outbox create migration script** (`*-create-<schema>-outbox-tables.{sql,pgsql}`) under `Migrations/`. In that case nothing needs scaffolding — the script just needs to be applied via `Migrate`. Only scaffold a new create script when one does **not** already exist (e.g. outbox was enabled after the solution was generated).

> **Migration scripts are immutable once applied.** A create script runs exactly once and is never re-run, regenerated, or edited. If the outbox schema later needs to change (e.g. a DbEx version bump alters its shape), author a **new** timestamped `ALTER` migration script for the delta — do not modify or re-scaffold the original create script.

> **Agent instruction:** When asked to create the database outbox table(s):
> 1. **Validate `dbex.yaml` first.** All three conditions must hold:
>    - Root-level `outbox: true` is set.
>    - Root-level `schema:` has a value (call it `xxx`).
>    - Root-level `outboxName:` has a value (call it `yyy`).
>    If any condition fails, **stop and error** — state which is missing. Do not attempt to scaffold the script.
> 2. **Check for an existing create script.** Look under `Migrations/` for a `*-create-*-outbox-tables.{sql,pgsql}` script (the template ships one when outbox is enabled). If present, **do not scaffold another** — skip to step 4 to apply it. Only when none exists, proceed to step 3.
> 3. **Scaffold the migration script** using the extracted `schema` (`xxx`) and `outboxName` (`yyy`) values:
>    ```
>    dotnet run -- script outbox xxx yyy
>    ```
> 4. **Ask** whether the (new or existing) script should be deployed/migrated to the database.
> 5. **If confirmed**, run:
>    ```
>    dotnet run -- CreateMigrateAndCodeGen
>    ```
>    Summarise the output on success; on failure relay the **complete output verbatim** — it provides the diagnostic needed to resolve the issue.
> 6. **If declined**, remind the user to run `dotnet run -- CreateMigrateAndCodeGen` before the outbox table(s) exist in the database.

### `dbex.yaml` structure

Schema, table, and column names follow the casing convention of the target database:
- **PostgreSQL** — `snake_case` throughout
- **SQL Server** — `PascalCase` throughout

```yaml
# PostgreSQL example
# yaml-language-server: $schema=https://raw.githubusercontent.com/Avanade/DbEx/refs/heads/main/schema/dbex.json
schema: products        # snake_case schema name
outbox: true            # generate full transactional outbox infrastructure
outboxName: outbox      # prefix for outbox tables and functions
tables:
# Reference-data tables
- name: brand
- name: category
- name: unit_of_measure
# Transactional tables
- name: product
- name: inventory
```

```yaml
# SQL Server example
# yaml-language-server: $schema=https://raw.githubusercontent.com/Avanade/DbEx/refs/heads/main/schema/dbex.json
schema: Products        # PascalCase schema name
outbox: true
outboxName: Outbox      # prefix for outbox tables and stored procedures
tables:
# Reference-data tables
- name: Brand
- name: Category
- name: UnitOfMeasure
# Transactional tables
- name: Product
- name: Inventory
```

Add the `$schema` annotation to each file for IDE YAML validation and auto-complete.

### `CodeGen` phase — generated Infrastructure C#

The `CodeGen` command generates `.g.cs` files into the Infrastructure project:

| Generated artefact | Location | Description |
|---|---|---|
| `<Entity>.g.cs` | `Infrastructure/Persistence/` | Schema-aligned persistence model extending `ModelBase<TId>` (or `ReferenceDataModelBase<TId>` for reference data), both from `CoreEx.Data.Models`; the base supplies `Id`, `CreatedBy`/`CreatedOn`/`UpdatedBy`/`UpdatedOn`, and `ETag` (the DB `RowVersion` column is mapped onto `ETag`), with optional marker interfaces (`ILogicallyDeleted`) |
| `*DbContext.g.cs` | `Infrastructure/Repositories/` | Partial `DbContext` class exposing `AddGeneratedModels(ModelBuilder)` to register all persistence models with EF Core |

These files are the only `.g.cs` outputs of `*.Database`; all other generated C# comes from `*.CodeGen`. Never edit them directly.

### Inspecting current database state

The `Inspect` command queries the **live database** and reports, per table, whether it exists and — when it does — its current column schema as markdown. It is read-only and has no side effects, so run it freely without confirmation.

```
dotnet run -- inspect <schema> <table> [<table> ...]
```

The schema is the **first** argument; every argument after it is a table name within that schema. Example:

```
dotnet run -- inspect public contact gender
```

The output is the authoritative source of truth for what the database currently contains. **Do not infer table existence or configuration from the file system** — the presence of a script under `Migrations/` or an entry under `tables:` in `dbex.yaml` only means it was *authored*, not that it has been *applied* to the target database. Migration scripts may or may not have been run. Only `Inspect` reflects reality.

Reading the output:
- Branch on the `## SCHEMA.TABLE - Exists: Yes|No` header first. `No` means the table is absent and must be created.
- Use the **Qualified Name** bullet (e.g. `"public"."contact"` or `[Test].[Contact]`) for DDL casing and quoting — not the uppercased header text.
- Honour the **Reference Data: Yes|No** flag for routing decisions (a reference data table is maintained via `ref-data.yaml` + CodeGen, not by hand).
- PostgreSQL reports canonical type names (`CHARACTER VARYING(50)`, `TIMESTAMP WITH TIME ZONE`); treat these as equivalent to the `VARCHAR(50)` / `TIMESTAMPTZ` forms you would author in a script.
- Per the disclaimer in the output, the live database remains the ultimate truth; the report is derived from system catalogs and may not capture every nuance.

### `Migrate` — schema evolution

Migration scripts are embedded resources under `Migrations/`. Use timestamp-ordered names:

```
# PostgreSQL (.pgsql)
20260101-000001-create-products-schema.pgsql
20260101-000101-create-products-category.pgsql
20260101-000201-create-products-product.pgsql
20260101-000301-create-products-outbox.pgsql   # if outbox: true in dbex.yaml

# SQL Server (.sql)
20260101-000001-create-shopping-schema.sql
20260101-000101-create-shopping-basket-status.sql
20260101-000201-create-shopping-basket.sql
```

Scripts are **immutable once applied**. Subsequent changes require new scripts (e.g. `ALTER TABLE`). Use moustache-style `{{Parameter}}` for environment-specific values resolved from `MigrationArgs.Parameters`.

SQL conventions:
- Wrap each script in `BEGIN TRANSACTION ... COMMIT TRANSACTION` (SQL Server) or equivalent.
- Use explicit schema-qualified names.
- Include the `IChangeLog` audit columns on aggregate tables, named **exactly** `CreatedBy`, `CreatedOn`, `UpdatedBy`, `UpdatedOn` (SQL Server) / `created_by`, `created_on`, `updated_by`, `updated_on` (PostgreSQL). The date/time columns use the `On` suffix — **never** `CreatedDate`/`UpdatedDate` or `created_date`/`updated_date`. Type them `DATETIMEOFFSET` (SQL Server) / `TIMESTAMPTZ` (PostgreSQL) and the `*By` columns as the contract's user type (typically `NVARCHAR(n)` / `VARCHAR(n)`).
- **SQL Server**: add a `ROWVERSION` / `TIMESTAMP` column for optimistic-concurrency mapped to `ETag`.
- **PostgreSQL**: use the built-in hidden `xmin` system column for optimistic-concurrency — no explicit column is required in the schema.
- Logical (soft) delete on root/aggregate tables is an infrastructure-only column — `IsDeleted` (SQL Server) / `is_deleted` (PostgreSQL) — with **no** corresponding contract/entity property; default to including it (confirm) when creating such a table.

### Standard table templates

**Mirror these canonical shapes** when authoring create scripts — copy and adapt rather than inventing column names. They encode the standard names, types, and lengths. Note the audit columns use the `On` suffix (`CreatedOn`/`UpdatedOn`) — **never** `CreatedDate`/`UpdatedDate`.

**Reference-data table** — the standard `IReferenceData` columns. The primary key is the contract's identifier type (`NVARCHAR(50)` / `VARCHAR(50)` for the default `string`):

```sql
-- SQL Server
CREATE TABLE [Schema].[Xxx] (
  [XxxId] NVARCHAR(50) NOT NULL PRIMARY KEY,
  [Code] NVARCHAR(50) NOT NULL UNIQUE,
  [Text] NVARCHAR(250) NULL,
  [IsActive] BIT NULL,
  [SortOrder] INT NULL,
  [RowVersion] TIMESTAMP NOT NULL,        -- ETag (optimistic concurrency)
  [CreatedBy] NVARCHAR(250) NULL,
  [CreatedOn] DATETIMEOFFSET NULL,
  [UpdatedBy] NVARCHAR(250) NULL,
  [UpdatedOn] DATETIMEOFFSET NULL
);
```

```sql
-- PostgreSQL (no RowVersion column — the hidden xmin provides concurrency)
CREATE TABLE "schema"."xxx" (
  "xxx_id" VARCHAR(50) NOT NULL PRIMARY KEY,
  "code" VARCHAR(50) NOT NULL UNIQUE,
  "text" VARCHAR(250) NULL,
  "is_active" BOOLEAN NULL,
  "sort_order" INTEGER NULL,
  "created_by" VARCHAR(250) NULL,
  "created_on" TIMESTAMPTZ NULL,
  "updated_by" VARCHAR(250) NULL,
  "updated_on" TIMESTAMPTZ NULL
);
```

**Aggregate / transactional table** — domain columns first, then the identical audit + concurrency columns. Reference-data relationships are stored by `Code` by default (e.g. `[StatusCode] NVARCHAR(50) NOT NULL`) with no foreign key — see [Creating or altering a table for an entity](#creating-or-altering-a-table-for-an-entity):

```sql
-- SQL Server
CREATE TABLE [Schema].[Order] (
  [OrderId] NVARCHAR(50) NOT NULL PRIMARY KEY,
  [CustomerId] NVARCHAR(100) NOT NULL,
  [StatusCode] NVARCHAR(50) NOT NULL,     -- references [OrderStatus].[Code]; no FK by convention
  [CreatedBy] NVARCHAR(250) NULL,
  [CreatedOn] DATETIMEOFFSET NULL,
  [UpdatedBy] NVARCHAR(250) NULL,
  [UpdatedOn] DATETIMEOFFSET NULL,
  [RowVersion] TIMESTAMP NOT NULL
);
```

### Mapping contract types to columns

When authoring a migration script for an entity that has a corresponding .NET contract, the column types must mirror the contract's property types. Do not substitute a different type (most importantly for the primary key) unless the user explicitly asks. Before authoring, establish whether the table already exists and its current shape — see [Inspecting current database state](#inspecting-current-database-state) and the [Creating or altering a table for an entity](#creating-or-altering-a-table-for-an-entity) workflow.

| Contract (.NET) type | SQL Server | PostgreSQL |
|---|---|---|
| `string` / `string?` | `NVARCHAR(n)` | `VARCHAR(n)` / `TEXT` |
| `Guid` | `UNIQUEIDENTIFIER` | `UUID` |
| `int` | `INT` | `INTEGER` |
| `long` | `BIGINT` | `BIGINT` |
| `short` | `SMALLINT` | `SMALLINT` |
| `decimal` | `DECIMAL(p,s)` | `DECIMAL(p,s)` |
| `double` | `FLOAT` | `DOUBLE PRECISION` |
| `bool` | `BIT` | `BOOLEAN` |
| `DateTime` | `DATETIME2` | `TIMESTAMP` |
| `DateTimeOffset` | `DATETIMEOFFSET` | `TIMESTAMPTZ` |

> **Agent instruction:** When generating a migration script for an entity that has a .NET contract:
> 1. **Map the primary key column to the contract's identifier type.** A contract with `IIdentifier<string?>` (the default) maps to `NVARCHAR(50) NOT NULL PRIMARY KEY` (SQL Server) / `VARCHAR(50) NOT NULL PRIMARY KEY` (PostgreSQL). A `Guid` identifier maps to `UNIQUEIDENTIFIER` / `UUID`; an `int` to `INT` / `INTEGER`; and so on. Never assume `UNIQUEIDENTIFIER`/`UUID` for a `string` identifier.
> 2. **Do not add value-generation defaults** (e.g. `DEFAULT (NEWSEQUENTIALID())`, `IDENTITY`, `gen_random_uuid()`) unless explicitly requested. By convention the application services layer assigns identifier and other values — the database should not default them.
> 3. **Map every other column to its contract property type** per the table above, preserving nullability (`?` → nullable column).
> 4. **If in doubt about a type, nullability, or precision/length, ask** rather than guessing.

### Creating or altering a table for an entity

When asked to create or change a database table for a .NET entity (e.g. *"create a table for the Employee entity"*), do **not** blindly scaffold a `CREATE TABLE` script. The table — or a related reference data table — may already exist with a different shape. Use `Inspect` to establish the current state first.

> **Inspect first — this is a hard gate.** Inspection is the **first action**, ahead of authoring anything. Do **not** write (or plan to write) a `CREATE`/`ALTER` script before the `Inspect` result is in hand — the result determines *whether* a script is even needed and *which kind*. A plan that scaffolds scripts before inspecting is wrong; fix the plan, don't proceed.

> **Agent instruction:**
> 1. **Identify every table involved.** This includes the entity's own table plus any reference data tables implied by its `[ReferenceData<T>]` properties (each typed reference-data property maps to a lookup table that may need to exist).
> 2. **Inspect the current state — before authoring any script.** Run `dotnet run -- inspect <schema> <table> [<table> ...]` (read-only — no confirmation needed). If unapplied migration scripts may exist, offer to run `dotnet run -- Migrate` first (a mutation — confirm before running) so the inspection reflects the fully migrated schema. Only proceed to authoring once you know each table's actual state.
> 3. **Branch per table on the `Inspect` result:**
>    - **Not found** → author a new timestamped `CREATE TABLE` migration script under `Migrations/`, and register the table under `tables:` in `dbex.yaml`. Map column types per [Mapping contract types to columns](#mapping-contract-types-to-columns).
>    - **Found, Reference Data: Yes** → do **not** recreate or alter it directly. Reference it from the entity table per step 4 below. Any change to the reference data table's own shape flows through `ref-data.yaml` + CodeGen, not a hand-authored script.
>    - **Found, schema differs from the contract** → author a new timestamped `ALTER TABLE` migration script for the **delta only** (applied scripts are immutable — never edit the original create script).
>    - **Found, schema already matches** → no script is needed; say so.
> 4. **Confirm how each reference-data relationship is represented.** For every `[ReferenceData<T>]` property on the entity, ask whether the column should reference the lookup by its **Code** (the default) or by its **Id**:
>    - **By Code (default)** → name the column with a `Code` suffix (e.g. `Employee.GenderCode` / `gender_code`), typed to match the reference data `Code` column (typically `NVARCHAR(50)` / `VARCHAR(50)`). **Do not create a foreign key constraint.**
>    - **By Id** → name the column with an `Id` suffix (e.g. `Employee.GenderId` / `gender_id`), typed to match the reference data identifier type. A foreign key is **not** created automatically — **ask whether one is required** and add it only if confirmed.
> 5. **Confirm logical-delete support** (for root/aggregate tables). Ask whether the table should support logical (soft) deletes — **default yes**. If yes, add an infrastructure-only column: `IsDeleted` (SQL Server) / `is_deleted` (PostgreSQL). This is a persistence concern only — the .NET contract/entity must **not** declare an equivalent property.
> 6. **Offer to apply.** Offer to run `dotnet run -- CreateMigrateAndCodeGen`. Summarise the output on success; on failure relay the **complete output verbatim**.
>
> **On failure, do not add defensive existence-guards.** Migration scripts are plain DDL — DbEx tracks which have been applied and runs each exactly once, so a `CREATE TABLE` does **not** need `IF NOT EXISTS` / `IF OBJECT_ID(...) IS NULL` wrappers. If a script fails because an object already exists (or differs), that means the `Inspect` step was skipped or stale: **re-inspect** to learn the real state, then either remove the redundant create script (object already correct), or author a separate `ALTER` migration for the delta. Wrapping the DDL in conditional guards to make it "pass" masks the underlying state mismatch and is not the convention — never do it.

### `Schema` — idempotent objects

Objects under `Schema/` are dropped and re-created on every `Schema` run, making them safely idempotent. When `outbox: true` is set in `dbex.yaml`, DbEx generates the full outbox schema objects here:

| SQL Server | PostgreSQL |
|---|---|
| `Schema/Stored Procedures/spOutboxEnqueue.g.sql` | `Schema/Functions/fn_outbox_enqueue.g.pgsql` |
| `spOutboxLeaseAcquire.g.sql` | `fn_outbox_lease_acquire.g.pgsql` |
| `spOutboxLeaseRelease.g.sql` | `fn_outbox_lease_release.g.pgsql` |
| `spOutboxBatchClaim.g.sql` | `fn_outbox_batch_claim.g.pgsql` |
| `spOutboxBatchComplete.g.sql` | `fn_outbox_batch_complete.g.pgsql` |
| `spOutboxBatchCancel.g.sql` | `fn_outbox_batch_cancel.g.pgsql` |

These `.g.sql` / `.g.pgsql` files are generated by DbEx — never edit them directly.

### `Data` — seeding

Seed data in `Data/ref-data.yaml` is **cross-environment** — it is applied in every environment including production. It should therefore contain only shared **reference data** (lookup tables, code lists) that must exist everywhere. Do not seed master or transactional data here unless it is genuinely required in all environments; test-specific data belongs in the test project's own `data.yaml`, applied only during test setup.

The root node is the schema/domain name. DbEx infers column types from the live schema.

Prefixes control merge behaviour and identifier generation:

| Prefix | Meaning |
|---|---|
| `$` | MERGE (upsert) — safe to re-run; use for reference data |
| `^` | Auto-generate GUID for the primary key |
| `$^` | Both — upsert with auto-generated GUID (typical for reference data) |

```yaml
products:
  - $^brand:            # merge + auto-GUID primary key
    - YETI: Yeti Cycles
    - CANYON: Canyon Bicycles
  - $^unit_of_measure:
    - EA: Each
    - { code: HR, text: Hour, scale: 2 }   # inline object for additional columns
  - $^sub_category:
    - { code: XC, text: Cross country, category_code: B }  # FK column by code; DbEx resolves id at runtime
```

## Do Not

- Do not edit `*.g.cs`, `*.g.sql`, or `*.g.pgsql` files directly — they are owned by `*.CodeGen` or `*.Database` tooling.
- Do not use SQL Server packages (`DbEx.SqlServer`) in PostgreSQL domains or vice versa.
- Do not alter applied migration scripts — subsequent schema changes require new scripts.
- Do not hand-author the outbox stored procedures or functions — set `outbox: true` in `dbex.yaml` and let DbEx generate them.
- Do not write persistence models or `DbContext` partials by hand — run `dotnet run -- CodeGen` (or `dotnet run -- All`) to regenerate.

## Further Reading

- [Tooling Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/tooling.md) — full `*.CodeGen` and `*.Database` walkthrough with command reference.
- [CodeGen Schema Docs](https://github.com/Avanade/CoreEx/tree/main/src/CoreEx.CodeGen/docs) — `ref-data.yaml` schema: `CodeGeneration.md`, `Entity.md`, `Property.md`.
- [DbEx on GitHub](https://github.com/Avanade/DbEx) — DbEx command reference, YAML schema, and migration script conventions.
- [OnRamp on GitHub](https://github.com/Avanade/OnRamp) — Handlebars-based code generation engine used by `*.CodeGen`.
