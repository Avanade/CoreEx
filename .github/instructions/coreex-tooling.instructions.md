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
| `*.g.cs` contract class | Contracts | Typed reference-data entity contract extending `ReferenceData<TSelf>` |
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

Composite commands for common scenarios:

| Composite | Runs |
|---|---|
| `All` | `Create` → `Migrate` → `CodeGen` → `Schema` → `Data` |
| `Deploy` | `Migrate` → `Schema` |
| `DeployWithData` | `Migrate` → `Schema` → `Data` |
| `ResetAndAll` | `Reset` → `All` |

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
| `<Entity>.g.cs` | `Infrastructure/Persistence/` | Schema-aligned persistence model extending `ModelBase<TId>`, with optional marker interfaces (`ILogicallyDeleted`) |
| `*DbContext.g.cs` | `Infrastructure/Repositories/` | Partial `DbContext` class exposing `AddGeneratedModels(ModelBuilder)` to register all persistence models with EF Core |

These files are the only `.g.cs` outputs of `*.Database`; all other generated C# comes from `*.CodeGen`. Never edit them directly.

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
- Include `CreatedBy`, `CreatedOn`, `UpdatedBy`, `UpdatedOn` audit columns on aggregate tables.
- **SQL Server**: add a `ROWVERSION` / `TIMESTAMP` column for optimistic-concurrency mapped to `ETag`.
- **PostgreSQL**: use the built-in hidden `xmin` system column for optimistic-concurrency — no explicit column is required in the schema.

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
