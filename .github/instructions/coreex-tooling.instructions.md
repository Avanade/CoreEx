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

## Order of operations (database-first)

A change that touches the database, code generation, and hand-written code must be done **in dependency order** — the database is the baseline everything else generates from or builds on. **Plan the full sequence first, execute it in order, and do not begin a step until the previous one has succeeded.** Doing things out of order (CodeGen first, or adding seed rows / `dbex.yaml` tables before the create migrations exist) is the main cause of confused, self-inflicted "fixes".

1. **Establish & inspect.** Bring the DB up to date (`dotnet run -- database`), then `dotnet run -- inspect <schema> <table>…` to confirm current state and decide create-vs-alter — see [Creating or altering a table for an entity](#creating-or-altering-a-table-for-an-entity). **If the bring-up fails, STOP and surface the verbatim error — do not continue or start editing unrelated files.**
2. **Author migration script(s)** for the absent/changed table(s): `dotnet run -- script refdata|create <schema> <table>`, then fill in the columns.
3. **Add reference-data rows** to `Data/ref-data.seed.yaml`.
4. **Register the tables** in `dbex.yaml` `tables:`.
5. **Apply & generate (DB side):** `dotnet run -- All` (Create → Migrate → CodeGen → Schema → Data) — Migrate applies the new scripts (tables now exist), CodeGen generates the EF persistence models from the live schema, Data seeds. **If it fails, STOP and surface the verbatim error** — a broken baseline invalidates everything downstream.
6. **CoreEx CodeGen — contracts:** edit `*.CodeGen/ref-data.yaml` and run `dotnet run` (in `*.CodeGen`) to generate the reference-data contracts/services/repositories/mappers.
7. **Hand-written .NET code:** application services, validators, controllers, tests — last, on top of the generated baseline.

Steps 3–4 come **after** the migration scripts (step 2) and are only applied/introspected by step 5 — where, within `All`, Migrate creates the tables **before** Data seeds and **before** CodeGen introspects `dbex.yaml`. Never add seed rows or `dbex.yaml` tables (or run CodeGen) before the create migrations exist; that is exactly what makes the bring-up fail (seeding into, or generating models from, tables that do not yet exist).

> Two distinct `dotnet run` code-gen steps in different projects: step 5's `All` invokes the **DbEx** CodeGen in `*.Database` (EF persistence models from `dbex.yaml` + live schema); step 6 is the **CoreEx** CodeGen in `*.CodeGen` (contracts etc. from `*.CodeGen/ref-data.yaml`). Do not conflate them.

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

> **Application project must globally import the Contracts namespace.** The generated Application artefacts (e.g. `IXxxRepository.g.cs`, the service `.g.cs`) reference contract types **unqualified** — `GenderCollection`, `Gender`, etc. For these to resolve, the Application project's `GlobalUsing.cs` must contain `global using {Solution}.Contracts;`. The `coreex` template ships this already; if a generated solution hits a build error like *"`GenderCollection` could not be found"* in the Application project after CodeGen, add that global using (do **not** edit the `.g.cs` to fully-qualify it).

### `ref-data.yaml` structure

> ⚠️ **Two related but distinct reference-data files — do not confuse them:**
> - **`*.CodeGen/ref-data.yaml`** (this section) — *defines* the reference-data **entities/contracts** (`entities:` with `name`/`idType`/`properties`); consumed by CodeGen to generate C#.
> - **`*.Database/Data/ref-data.seed.yaml`** (see [`Data` — seeding](#data--seeding)) — *seeds* reference-data **rows** into the database (`Schema:` → `- $^Table:` → rows); a completely different format. The `.seed.` in the filename marks it as the seed file.
>
> When you mean to add seed rows, edit the **Database/Data** file in the seed format — never put `entities:`-style definitions there, and never put `Schema:`/row data here.

```yaml
# *.CodeGen/ref-data.yaml — entity/contract DEFINITIONS (not seed data)
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

This is the generated baseline (the `Main` is provider-specific; `ConfigureMigrationArgs` is a plain block-bodied method — note **no** `=>` before the `{`):

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
    args.AddAssembly<SqlStatement>().AddAssembly<Program>();   // SqlStatement = CoreEx EF code-gen templates; Program = this project's embedded Migrations/Schema/Data. Both REQUIRED — do not remove (see below).
    args.DataResetFilterPredicate = ts => ts.Schema == "{domain-schema}";   // Only reset data for this domain's schema.
    return args;
}
```

**Both `AddAssembly` calls are required — never drop `AddAssembly<Program>()`.** `AddAssembly<SqlStatement>()` brings in the CoreEx EF code-generation templates used by the `CodeGen` command; `AddAssembly<Program>()` registers **this** Database project's assembly, which carries the embedded `Migrations/`, `Schema/`, and `Data/` resources. It is tempting to omit `AddAssembly<Program>()` because `Create<Program>(...)` registers that assembly automatically — but that only applies when migration runs via **`Main`** (the console path). The **API integration tests call `ConfigureMigrationArgs` directly** (e.g. `MigrateXxxDataAsync<TestData>(…, DbMigration.ConfigureMigrationArgs)`) **without** the `Create<Program>` console setup, so the embedded migrations/data are found **only** because `AddAssembly<Program>()` is inside `ConfigureMigrationArgs`. Removing it leaves the tests unable to locate any migration scripts or seed data. All sample domains (SQL Server and PostgreSQL) include both calls.

**Optional additions** (only when the domain needs them — not in the baseline):
- `.DataParserArgs.RefDataColumnDefault("<Column>", _ => <value>)` — default a **domain-specific** ref-data seed column when the YAML omits it (e.g. `Scale` for a `UnitOfMeasure`). Add one per such column; omit entirely if not needed. (Do **not** do this for `SortOrder` — it is auto-assigned, see below.)
- `.IncludeExtendedSchemaScripts()` — enable the extended `SqlStatement`-based schema scripting when the domain uses it.

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
| `Script` | Scaffolds a new migration script, correctly named `yyyyMMdd-HHmmss-<kebab-name>` (current UTC date+time, lower-cased). Subcommands: `schema`, `create`, `alter`, `refdata`, `outbox`. Prefer this over hand-creating script files |
| `Drop` | Drops the database |
| `Inspect` | Read-only; reports existence and current column schema (type, nullability, default, PK, identity, computed, unique, and whether it is reference data) for one or more tables in a schema, as markdown. Safe to run freely |

Composite commands for common scenarios:

| Composite | Runs |
|---|---|
| `All` | `Create` → `Migrate` → `CodeGen` → `Schema` → `Data` |
| `Database` | `Create` → `Migrate` → `Schema` → `Data` (database-only; no `CodeGen`) — the standard non-destructive bring-up to make the live database reflect all authored scripts |
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

**Keep table entries minimal — write `- name: Xxx` and nothing else.** A table entry needs only its `name`; everything else is by-convention. Use the simple `- name: Xxx` form (not the inline-object `- { name: Xxx, ... }` form) and do **not** add properties speculatively:
- **`efModel`** is a **choice string** — `Yes` (default), `No`, `ModelOnly`, or `ModelBuilderOnly` — **not** a boolean. **Omit it entirely** — the default is already `Yes` (generate the EF model), so `efModel: Yes` is redundant noise. Never write `efModel: true`, and don't add `efModel: Yes`.
- **The `IsDeleted` logical-delete column is recognised by convention** (the table-level `columnNameIsDeleted`, default `IsDeleted`). Do **not** declare it under `columns:` — and there is no per-column `isDeleted` flag (a column entry only supports `name`, `property`, `type`, `valueConverter`, `default`). DbEx detects the `IsDeleted` column from the live schema automatically. Only set the table-level `columnNameIsDeleted: "X"` if the column is non-standardly named.
- **`schema:`** on a table is a valid **override**, but only use it when the table genuinely lives in a **different, existing** schema. Do not invent a separate schema (e.g. `Ref`) for reference data — by default every table (reference and transactional) lives in the domain's root `schema:`, consistent with the migration scripts and the seed `Data/ref-data.seed.yaml`.

So a typical Gender + Employee domain is simply:

```yaml
tables:
# Reference-data
- name: Gender
# Transactional-data
- name: Employee
```

(Add `columns:`, `efModel`, `efModelName`, `includeColumns`/`excludeColumns`, or `columnName*` overrides only when a specific need arises.)

### `CodeGen` phase — generated Infrastructure C#

The `CodeGen` command generates `.g.cs` files into the Infrastructure project:

| Generated artefact | Location | Description |
|---|---|---|
| `<Entity>.g.cs` | `Infrastructure/Persistence/` | Schema-aligned persistence model extending `ModelBase<TId>` (or `ReferenceDataModelBase<TId>` for reference data), both from `CoreEx.Data.Models`; the base supplies `Id`, `CreatedBy`/`CreatedOn`/`UpdatedBy`/`UpdatedOn`, and `ETag` (the DB `RowVersion` column is mapped onto `ETag`), with optional marker interfaces (`ILogicallyDeleted`) |
| `*DbContext.g.cs` | `Infrastructure/Repositories/` | Partial `DbContext` class exposing `AddGeneratedModels(ModelBuilder)` to register all persistence models with EF Core |

These files are the only `.g.cs` outputs of `*.Database`; all other generated C# comes from `*.CodeGen`. Never edit them directly.

> **⚠️ Hand-written `*DbContext.cs` must not declare an `AddGeneratedModels` stub.** The generated `*DbContext.g.cs` defines `AddGeneratedModels(ModelBuilder)` as a regular **`public void`** method on the partial class. The hand-written `*DbContext.cs` must be a **`partial class`** that simply **calls** `AddGeneratedModels(modelBuilder)` from `OnModelCreating` — it must **not** also declare a `partial void AddGeneratedModels(ModelBuilder);` stub. A `partial void` declaration alongside the generated `public void` is two members with the same signature → **CS0111** ("type already defines a member …"). Remove the stub; the generated method is the sole definition. (Because there is no stub, the project only compiles once CodeGen has emitted the `.g.cs` — run CodeGen before building, per the order of operations.)

### Inspecting current database state

The `Inspect` command queries the **live database** and reports, per table, whether it exists and — when it does — its current column schema as markdown. It is read-only and has no side effects, so run it freely without confirmation.

> **Bring the database up to date first.** `Inspect` only reflects what is actually in the database, so before inspecting (especially before any create/alter decision) run `dotnet run -- database` to ensure the database exists and all authored migrations/schema/data are applied. `Database` is non-destructive (`Create` → `Migrate` → `Schema` → `Data`; no `Drop`/`Reset`), so run it as the standard precursor, then inspect:
>
> ```
> dotnet run -- database        # create-if-missing + apply migrations/schema/data
> dotnet run -- inspect <schema> <table> [<table> ...]
> ```

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

Migration scripts are embedded resources under `Migrations/`.

**Scaffold new scripts with the DbEx `Script` command — do not hand-create the file.** It generates a correctly named, correctly cased, correctly templated script every time:

```
dotnet run -- script schema <schema>            # new schema (rarely needed — see below)
dotnet run -- script create <schema> <table>    # new table
dotnet run -- script alter  <schema> <table>    # alter an existing table
dotnet run -- script refdata <schema> <table>   # new reference-data table
dotnet run -- script outbox <schema> <name>     # transactional outbox table(s)
```

**Naming convention** (what `Script` produces, and what any hand-named file must match): `yyyyMMdd-HHmmss-<kebab-description>.{sql|pgsql}`.
- The leading segment is the **current UTC date *and* time** (`yyyyMMdd-HHmmss`) at the moment of creation — **not** a placeholder date (e.g. `20250101`) and **not** a per-day incrementing index (e.g. `000001`). The time component provides natural ordering and uniqueness without tracking indices.
- The entire filename is **kebab-lower-case** — all lowercase, words separated by hyphens (e.g. `20260603-142530-create-bar-employee.sql`, never `...-create-Bar-Employee.sql`).

> **Do not author a schema-create script.** The `coreex` template already ships the default schema-create migration, so the schema exists from the first `Migrate`. Never emit a `create-<schema>-schema` script unless the user **explicitly** asks for an additional schema.

Scripts are **immutable once applied**. Subsequent changes require new scripts (e.g. `ALTER TABLE`). Use moustache-style `{{Parameter}}` for environment-specific values resolved from `MigrationArgs.Parameters`.

> **Never modify the database — or DbEx's journal — directly to unblock a migration.** DbEx tracks applied scripts in an internal **journal** table that it owns exclusively; do **not** insert/pre-seed/back-fill journal rows, and do **not** hand-run `CREATE`/`ALTER`/`DROP`/`INSERT` to reconcile state. If `migrate`/`database` fails because the live database is out of step with the scripts (e.g. "the journal is empty but the objects already exist", or scripts re-running over existing objects), **stop and ask the user** — reconciling environment state is their decision. The usual clean fix for a disposable local/dev database is to drop and rebuild via `dotnet run -- dropanddatabase` (destructive — confirm first); production/shared environments are reconciled by the user. Never edit the database or journal yourself.

SQL conventions:
- Wrap each script in `BEGIN TRANSACTION ... COMMIT TRANSACTION` (SQL Server) or equivalent.
- Use explicit schema-qualified names.
- Include the `IChangeLog` audit columns on aggregate tables, named **exactly** `CreatedBy`, `CreatedOn`, `UpdatedBy`, `UpdatedOn` (SQL Server) / `created_by`, `created_on`, `updated_by`, `updated_on` (PostgreSQL). The date/time columns use the `On` suffix — **never** `CreatedDate`/`UpdatedDate` or `created_date`/`updated_date`. Type them `DATETIMEOFFSET` (SQL Server) / `TIMESTAMPTZ` (PostgreSQL) and the `*By` columns as the contract's user type (typically `NVARCHAR(n)` / `VARCHAR(n)`).
- **SQL Server**: add a `ROWVERSION` / `TIMESTAMP` column for optimistic-concurrency mapped to `ETag`.
- **PostgreSQL**: use the built-in hidden `xmin` system column for optimistic-concurrency — no explicit column is required in the schema.
- Logical (soft) delete on root/aggregate tables is an infrastructure-only column — `[IsDeleted] BIT NOT NULL DEFAULT (0)` (SQL Server) / `is_deleted BOOLEAN NOT NULL DEFAULT FALSE` (PostgreSQL) — with **no** corresponding contract/entity property; default to including it (confirm) when creating such a table. It must be **NOT NULL** and **default to the DB's `false`** (`0` / `FALSE`), never nullable.

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
  [IsDeleted] BIT NOT NULL DEFAULT (0),   -- logical delete (default yes — confirm); NOT NULL, defaults false; no contract property
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
> 1. **The `script` scaffold's PK is a placeholder — replace it.** `dotnet run -- script create|refdata` seeds the primary key with a generated-key placeholder: `[{Name}Id] UNIQUEIDENTIFIER NOT NULL DEFAULT (NEWSEQUENTIALID()) PRIMARY KEY` (SQL Server) or `"{name}_id" SERIAL PRIMARY KEY` (PostgreSQL). **Neither is the default to keep** — overwrite it to match the contract's identifier type.
> 2. **Map the primary key column to the contract's identifier type.** The identifier is `string` **by default** → `[{Name}Id] NVARCHAR(50) NOT NULL PRIMARY KEY` (SQL Server) / `"{name}_id" VARCHAR(50) NOT NULL PRIMARY KEY` (PostgreSQL). Use `UNIQUEIDENTIFIER`/`UUID` **only** when the contract identifier is explicitly a `Guid`; `INT`/`INTEGER` for `int`; etc. Never leave the scaffold's `UNIQUEIDENTIFIER` (SQL Server) or `SERIAL` (PostgreSQL) for a `string` identifier.
> 3. **Drop the scaffold's value-generation default.** Remove `DEFAULT (NEWSEQUENTIALID())` (SQL Server) and **replace `SERIAL` with the plain column type** (PostgreSQL — `SERIAL` is an auto-increment sequence, i.e. a DB-assigned value); never add `IDENTITY` / `gen_random_uuid()` either — unless explicitly requested. The application services layer assigns identifier and other values; the database should not default them.
> 4. **Map every other column to its contract property type** per the table above, preserving nullability (`?` → nullable column). For a `[ReferenceData<T>]` property, the contract property is `{Name}Code` (a string) — so the column is `{Name}Code` (e.g. `[GenderCode] NVARCHAR(50) NULL`), **not** `{Name}Id`, and **no foreign key** (see step 4 of the create/alter workflow). Mirror the contract property name exactly.
> 5. **Lock the agreed identifier type — never deviate, especially in fixing loops.** Once the type is agreed (the `string` default, or whatever the user explicitly specified), it is fixed for the whole task. Do **not** silently change it, and do **not** revert to the scaffold's `UNIQUEIDENTIFIER` (or flip it again) while troubleshooting a build/migration failure — a failure is never resolved by changing the PK type. If you believe the agreed type is wrong, **stop and ask** rather than changing it.
> 6. **If in doubt about a type, nullability, or precision/length, ask** rather than guessing.

### Creating or altering a table for an entity

When asked to create or change a database table for a .NET entity (e.g. *"create a table for the Employee entity"*), do **not** blindly scaffold a `CREATE TABLE` script. The table — or a related reference data table — may already exist with a different shape. Use `Inspect` to establish the current state first.

> **Inspect first — this is a hard gate.** Inspection is the **first action**, ahead of authoring anything. Do **not** write (or plan to write) a `CREATE`/`ALTER` script before the `Inspect` result is in hand — the result determines *whether* a script is even needed and *which kind*. A plan that scaffolds scripts before inspecting is wrong; fix the plan, don't proceed.

> **Agent instruction:**
> 1. **Identify every table involved.** This includes the entity's own table plus any reference data tables implied by its `[ReferenceData<T>]` properties (each typed reference-data property maps to a lookup table that may need to exist).
> 2. **Bring the database up to date, then inspect — before authoring any script.** First run `dotnet run -- database` (non-destructive: `Create` → `Migrate` → `Schema` → `Data`) so the live database reflects all authored scripts, then run `dotnet run -- inspect <schema> <table> [<table> ...]` (read-only). Only proceed to authoring once you know each table's actual state.
> 3. **Branch per table on the `Inspect` result:**
>    - **Not found** → scaffold the script with `dotnet run -- script create <schema> <table>` (or `script refdata <schema> <table>` for a reference-data table) so it is correctly named/timestamped/cased, fill in the columns, and register the table under `tables:` in `dbex.yaml`. Map column types per [Mapping contract types to columns](#mapping-contract-types-to-columns). **Do not create a schema-create script** — the schema already exists (template-provided).
>    - **Found, Reference Data: Yes** → do **not** recreate or alter it directly. Reference it from the entity table per step 4 below. Any change to the reference data table's own shape flows through `ref-data.yaml` + CodeGen, not a hand-authored script.
>    - **Found, schema differs from the contract** → scaffold with `dotnet run -- script alter <schema> <table>` and include the **delta only** (applied scripts are immutable — never edit the original create script).
>    - **Found, schema already matches** → no script is needed; say so.
> 4. **Reference-data relationships are stored by `Code`, with no foreign key — this is the default; do not deviate silently.** For each `[ReferenceData<T>]` property on the entity (the contract has a `{Name}Code` string property, e.g. `GenderCode`):
>    - **Default — by Code:** create a column that **mirrors the contract property** — same name `{Name}Code` (e.g. `[GenderCode] NVARCHAR(50) NULL` / `gender_code`), typed to match the reference data's `Code` column. **Do not create a foreign key.** Do **not** invent a `{Name}Id` column.
>    - **Only if the user explicitly asks for an Id reference** → name it `{Name}Id`, typed to match the reference data identifier; and even then a foreign key is **not** automatic — ask whether one is required and add it only if confirmed.
>
>    **Never** default to `{Name}Id` + a `REFERENCES` foreign key — that contradicts both the contract (whose property is `{Name}Code`) and the CoreEx convention (reference data is resolved by code, not FK-joined).
> 5. **Confirm logical-delete support** (for root/aggregate tables). Ask whether the table should support logical (soft) deletes — **default yes**. If yes, add an infrastructure-only column: `[IsDeleted] BIT NOT NULL DEFAULT (0)` (SQL Server) / `is_deleted BOOLEAN NOT NULL DEFAULT FALSE` (PostgreSQL) — it must be **NOT NULL** and default to the DB's `false` (`0` / `FALSE`), **never nullable**. This is a persistence concern only — the .NET contract/entity must **not** declare an equivalent property.
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

> 🚫 **NEVER include the identifier (`{Name}Id`, e.g. `GenderId`) in a seed row.** The `$^` prefix **auto-generates** the id — providing it is a bug (it conflicts with the generated key). A row carries **only** the business columns (`Code`, `Text`, and any extra/`scale` columns). This is the single most common seeding mistake — it applies in **both** the shorthand and the inline-object form:
> ```yaml
> - M: Male                                   # ✅ shorthand (preferred)
> - { code: HR, text: Hour, scale: 2 }        # ✅ inline object (extra columns) — still no id
> - { GenderId: M, Code: M, Text: Male, ... } # 🚫 NEVER — has the GenderId identifier
> ```

> ⚠️ **This is `*.Database/Data/ref-data.seed.yaml` — the seed-data file** (the `.seed.` distinguishes it), *not* the `*.CodeGen/ref-data.yaml` entity-definition file (see [`ref-data.yaml` structure](#ref-datayaml-structure)). It uses the seed format below (`Schema:` → `- $^Table:` → rows) — **never** the `entities:` definition format.

Seed data in `Data/ref-data.seed.yaml` is **cross-environment** — it is applied in every environment including production. It should therefore contain only shared **reference data** (lookup tables, code lists) that must exist everywhere. Do not seed master or transactional data here unless it is genuinely required in all environments; test-specific data belongs in the test project's own `data.yaml`, applied only during test setup.

**Structure** — there is exactly one valid shape, three levels deep:

```
<Schema>:              # root mapping key = schema name (no prefix, no dots)
  - $^<Table>:         # YAML LIST ITEM (note the leading "- ") = table, with a $ / $^ prefix
    - <row>            # rows
```

- The **schema** is the root mapping key — **never** a dotted `Schema.Table:` key (e.g. `Bar.Gender:` is **wrong**), and **never** prefixed.
- Each **table** is a YAML **list item** — it **must** begin with `- ` (e.g. `- $^Gender:`). A bare mapping key without the dash (`$^Gender:`) is **wrong** — that makes it an object property, not a list entry, and DbEx will not process it. Also never mash schema and table (`- $Bar.$^Gender:` is wrong).
- The **prefix is required** on reference-data table entries. **Reference data always uses `$^`** (merge + auto-generate the identifier) — this is the default **regardless of the identifier's type**. `^` auto-generates the id for *any* id type, not just `Guid` (DbEx handles, and can be extended per type) — so a `string`/`NVARCHAR(50)` PK still uses `$^`. Use a different prefix **only when explicitly asked**: plain `$` (merge, no auto-id) when ids are supplied/assigned externally. An **unprefixed** entry is a plain INSERT — not re-runnable; never use it for reference data.

DbEx infers column types from the live schema.

**Names follow the provider's casing** (same as the schema and migration scripts):
- **SQL Server** — PascalCase: schema `Bar`, table `Gender`, columns `Code`, `Text`, `IsActive`, `SortOrder`.
- **PostgreSQL** — snake_case: schema `bar`, table `gender`, columns `code`, `text`, `is_active`, `sort_order`.

Prefixes control merge behaviour and identifier generation (on the table entry):

| Prefix | Meaning |
|---|---|
| `$` | MERGE (upsert) — safe to re-run |
| `^` | Auto-generate the primary-key identifier (**any** id type — not GUID-only) |
| `$^` | Both — merge + auto-generated id; **the default for reference data**, whatever the id type |

Prefer the `Code: Text` **shorthand** (it sets `Code` and `Text`); use an inline object only for extra columns. **Never set the identifier column** (`{Name}Id`, e.g. `GenderId`) — `$^` auto-generates it, so supplying it is wrong unless you were **explicitly** asked to provide ids. Likewise omit `IsActive` (defaults active) and `SortOrder` (auto-assigned by row order — see below). The ideal row is just `M: Male`.

```yaml
# SQL Server (PascalCase) — schema "Bar", reference table "Gender"
Bar:
  - $^Gender:           # merge + auto-generated id (the ref-data default, any id type)
    - F: Female
    - M: Male
    - X: Other
```

```yaml
# PostgreSQL (snake_case) — schema "products"
products:
  - $^brand:            # merge + auto-generated id (the ref-data default, any id type)
    - CANYON: Canyon Bicycles
    - YETI: Yeti Cycles
  - $^unit_of_measure:
    - EA: Each
    - { code: HR, text: Hour, scale: 2 }   # inline object for additional columns
  - $^sub_category:
    - { code: XC, text: Cross country, category_code: B }  # FK column by code; DbEx resolves id at runtime
```

**`SortOrder` is auto-assigned from row order.** When a ref-data row does not specify `SortOrder`, DbEx assigns it based on the row's position in the YAML. So **order the rows the way they should sort** — normally **by `Code`** — otherwise the resulting `SortOrder` (and thus default display order) will look arbitrary. Only specify an explicit `SortOrder` value when it must differ from positional order.

## Do Not

- Do not edit `*.g.cs`, `*.g.sql`, or `*.g.pgsql` files directly — they are owned by `*.CodeGen` or `*.Database` tooling.
- Do not use SQL Server packages (`DbEx.SqlServer`) in PostgreSQL domains or vice versa.
- Do not alter applied migration scripts — subsequent schema changes require new scripts.
- Do not hand-author the outbox stored procedures or functions — set `outbox: true` in `dbex.yaml` and let DbEx generate them.
- Do not write persistence models or `DbContext` partials by hand — run `dotnet run -- CodeGen` (or `dotnet run -- All`) to regenerate.
- Do not hand-create or hand-name migration script files — scaffold via `dotnet run -- script <type> ...`; names must be `yyyyMMdd-HHmmss-<kebab-name>` using the current date+time (never a placeholder date or a per-day index) and be fully kebab-lower-case.
- Do not author a schema-create script — the template already provides the default schema; only create one if the user explicitly asks for an additional schema.
- Do not modify the database directly to unblock anything — no ad-hoc `CREATE`/`ALTER`/`DROP`/`INSERT`/`UPDATE`/`DELETE`, and never touch DbEx's journal/tracking table (no pre-seeding rows). Structural change = migration script; data = `Data/*.yaml`. If state is inconsistent, stop and ask the user.
- Do not leave ref-data seed rows unordered, and do not default `SortOrder` via `RefDataColumnDefault` — order the YAML rows by `Code` so DbEx's positional `SortOrder` assignment is sensible.
- Do not use a dotted `Schema.Table:` seed key (e.g. `Bar.Gender:`) or mash schema and table into one prefixed key (`- $Schema.$^Table:`) — the schema is the **root mapping key**, and the prefixed table is a **list entry** beneath it (`Schema:` → `- $^Table:` → rows).
- Do not omit the leading `- ` on a table entry — it is a YAML **list item** (`- $^Gender:`), not a bare mapping key (`$^Gender:`); without the dash DbEx will not process it.
- **Do not put the identifier (`{Name}Id`, e.g. `GenderId`) in a seed row — ever, under `$^`.** The `$^` prefix auto-generates the id; a row has only `Code`/`Text`/extra columns. (An id is supplied only in the rare explicit case of plain `$` with externally-assigned keys — not the reference-data default.)
- Do not write an **unprefixed** ref-data table entry — it is a plain INSERT (not re-runnable). Default reference data to **`$^`** (merge + auto-generate the id, **any** id type — `^` is not GUID-only); use plain `$` only when explicitly asked (ids supplied externally). Do not downgrade `$^` to `$` just because the PK is a `string`/`NVARCHAR(50)`.
- Do not work out of order — follow the [database-first order of operations](#order-of-operations-database-first) (inspect → author migrations → seed + `dbex.yaml` → `All` → CoreEx CodeGen → .NET code). Add seed rows and `dbex.yaml` tables only **after** their create migrations exist, and apply them together via `All` (Migrate creates the tables before Data seeds / CodeGen introspects). Never run a bring-up that would seed, or CodeGen, against tables whose create migration does not yet exist.
- Do not continue past a failed `dotnet run -- database` bring-up — stop and surface the error; a broken baseline invalidates everything downstream, and pressing on causes churn-y, misdirected fixes.
- Do not keep the `script` scaffold's generated-key PK — `[{Name}Id] UNIQUEIDENTIFIER ... DEFAULT (NEWSEQUENTIALID())` (SQL Server) or `"{name}_id" SERIAL` (PostgreSQL) — replace it with the agreed identifier type's column (`string` default → `NVARCHAR(50)`/`VARCHAR(50)`), dropping the value-generation default **unless the user explicitly asks to keep/include it**.
- Do not change the agreed identifier type to make a failure go away — it is locked for the task; never revert to `UNIQUEIDENTIFIER` (or flip the type) in a fixing loop. If you think it is wrong, stop and ask.
- Do not default a reference-data relationship to a `{Name}Id` column or a `REFERENCES` foreign key — mirror the contract's `{Name}Code` string property (e.g. `[GenderCode] NVARCHAR(50) NULL`, **no FK**). Use `{Name}Id`/FK only when the user explicitly asks.
- Do not make the logical-delete column nullable or default-less — it is `[IsDeleted] BIT NOT NULL DEFAULT (0)` (SQL Server) / `is_deleted BOOLEAN NOT NULL DEFAULT FALSE` (PostgreSQL): **NOT NULL**, defaulting to the DB's `false`.
- Do not add `efModel` to a `dbex.yaml` table entry when it would be the default — write the bare `- name: Xxx` (not `- { name: Xxx, efModel: Yes }`). `efModel: Yes` is redundant (it's the default); `efModel: true` is invalid (it's a `Yes`/`No`/`ModelOnly`/`ModelBuilderOnly` choice). Only set `efModel` for a non-default (`No`/`ModelOnly`/`ModelBuilderOnly`).
- Do not declare the `IsDeleted` column under a table's `columns:` (and there is no `isDeleted` column flag) — it is recognised by convention from the live schema; keep table entries to `- name: Xxx` unless an override is genuinely needed.
- Do not add a per-table `schema:` override for reference data (e.g. a `Ref` schema) — reference and transactional tables both live in the domain's root `schema:` unless a different schema actually exists.
- Do not use the wrong casing in seed data — match the provider (SQL Server PascalCase `Code`/`Text`/`IsActive`/`SortOrder`; PostgreSQL snake_case `code`/`text`/`is_active`/`sort_order`), and do not hand-write `id`/`IsActive`/`SortOrder` rows — prefer the `Code: Text` shorthand.

## Further Reading

- [Tooling Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/tooling.md) — full `*.CodeGen` and `*.Database` walkthrough with command reference.
- [CodeGen Schema Docs](https://github.com/Avanade/CoreEx/tree/main/src/CoreEx.CodeGen/docs) — `ref-data.yaml` schema: `CodeGeneration.md`, `Entity.md`, `Property.md`.
- [DbEx on GitHub](https://github.com/Avanade/DbEx) — DbEx command reference, YAML schema, and migration script conventions.
- [OnRamp on GitHub](https://github.com/Avanade/OnRamp) — Handlebars-based code generation engine used by `*.CodeGen`.
