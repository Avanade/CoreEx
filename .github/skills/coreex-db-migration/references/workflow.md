# DB Migration Workflow

All commands run from the `*.Database` project directory for the target domain.

---

## Phase 1: Establish Baseline

1. **Bring the database up to date** (non-destructive — Create → Migrate → Schema → Data):
   ```
   dotnet run -- database
   ```
   If this fails, **stop** — surface the verbatim error. Do not proceed. Do not edit files to work around a broken baseline.

2. **Identify every table involved.** For an entity with `[ReferenceData<T>]` properties, this includes the entity's own table plus any reference-data tables implied by those properties.

3. **Inspect the target table(s)**:
   ```
   dotnet run -- inspect <schema> <table> [<table2> ...]
   ```
   Read the `## SCHEMA.TABLE — Exists: Yes|No` header for each table. This is a **hard gate** — do not author any script before the inspect result is in hand.

---

## Phase 2: Choose Script Type

Branch on the combination of what the developer needs and the inspect result.

### Path A — New transactional table (`Exists: No`)

```
dotnet run -- script create <schema> <table>
```

Open the generated `Migrations/yyyyMMdd-HHmmss-create-<schema>-<table>.{sql|pgsql}` and fill in domain columns following the templates in Phase 3. Then proceed to Phase 4 to register the table in `dbex.yaml`.

### Path B — New reference-data table (`Exists: No`, ref-data entity)

```
dotnet run -- script refdata <schema> <table>
```

Open the generated script and fill in any extra columns beyond the standard `IReferenceData` set. Then proceed to Phase 4.

### Path C — Alter existing table (`Exists: Yes`, schema differs from contract)

```
dotnet run -- script alter <schema> <table>
```

Open the generated `Migrations/yyyyMMdd-HHmmss-alter-<schema>-<table>.{sql|pgsql}` and author the `ALTER TABLE` statements for the **delta only**. The original create script is immutable — never touch it. `dbex.yaml` does not need updating (the table entry already exists). Identify any hand-written `.cs` code that references changed or removed columns and flag those for update in Phase 6.

> This path applies to both transactional and reference-data tables. For a reference-data table whose shape has changed, use `script alter` for the DDL delta; the `ref-data.yaml` + CodeGen side is a separate step (see `coreex-refdata` skill).

### Path D — Non-entity schema change (index, constraint, function, etc.)

```
dotnet run -- script
```

DbEx creates an empty script with a placeholder suffix. **Immediately rename** the suffix portion of the filename to a 3–5 word kebab summary of the intent:

- Good: `add-unique-sku-index`, `drop-legacy-status-column`, `add-fk-order-customer`
- Bad: `migration`, `update`, `change1`

**255-character filename limit:** The complete filename (including timestamp prefix, descriptor, and extension) must not exceed 255 characters. Internally DbEx derives the embedded-resource name from the project assembly name + `.Migrations.` + the filename (without extension), e.g. `Contoso.Products.Database.Migrations.20260629-143000-add-unique-sku-index`. Keep descriptors short — 3–5 words is both the intent and the safety margin. A descriptor that needs more than ~200 characters is a design signal, not a naming requirement.

`dbex.yaml` does not need updating for non-entity changes. Proceed directly to Phase 5.

### Path E — Table already matches (`Exists: Yes`, schema already correct)

No script is needed. Say so and stop. Do not scaffold a no-op migration.

---

## Phase 3: Fill In The Script

### PK replacement (Paths A and B only)

The `script create` and `script refdata` scaffolds generate a placeholder primary key. **Always overwrite it** to match the contract's identifier type:

| Contract identifier type | SQL Server column | PostgreSQL column |
|---|---|---|
| `string` (default) | `[XxxId] NVARCHAR(50) NOT NULL PRIMARY KEY` | `"xxx_id" VARCHAR(50) NOT NULL PRIMARY KEY` |
| `Guid` | `[XxxId] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY` | `"xxx_id" UUID NOT NULL PRIMARY KEY` |
| `int` | `[XxxId] INT NOT NULL PRIMARY KEY` | `"xxx_id" INTEGER NOT NULL PRIMARY KEY` |

Remove any `DEFAULT (NEWSEQUENTIALID())`, `IDENTITY`, or `SERIAL` unless the user explicitly requests a DB-assigned key. The application layer assigns identifiers.

### Column-to-type mapping

| .NET type | SQL Server | PostgreSQL |
|---|---|---|
| `string` / `string?` | `NVARCHAR(n)` | `VARCHAR(n)` |
| `Guid` | `UNIQUEIDENTIFIER` | `UUID` |
| `int` | `INT` | `INTEGER` |
| `long` | `BIGINT` | `BIGINT` |
| `decimal` | `DECIMAL(p,s)` | `DECIMAL(p,s)` |
| `bool` | `BIT` | `BOOLEAN` |
| `DateTimeOffset` | `DATETIMEOFFSET` | `TIMESTAMPTZ` |
| `DateTime` | `DATETIME2` | `TIMESTAMP` |

### Reference-data relationships

For each `[ReferenceData<T>]` property on the entity (contract property is `{Name}Code`): create a `{Name}Code` column typed to match the ref-data `Code` column. **No foreign key — this is the default.** Never create a `{Name}Id` column unless the user explicitly asks.

### Logical delete (`is_deleted` / `IsDeleted`)

For root/aggregate tables, ask whether logical (soft) delete support is needed — **default yes**. If yes, add:
- SQL Server: `[IsDeleted] BIT NOT NULL DEFAULT (0)`
- PostgreSQL: `"is_deleted" BOOLEAN NOT NULL DEFAULT FALSE`

Must be `NOT NULL` and default to the DB's false. No corresponding contract/entity property.

### Audit columns (IChangeLog)

Use the `On` suffix — **never** `CreatedDate` / `UpdatedDate`:
- SQL Server: `DATETIMEOFFSET NULL` for date columns, `NVARCHAR(250) NULL` for `*By` columns
- PostgreSQL: `TIMESTAMPTZ NULL` for date columns, `VARCHAR(250) NULL` for `*By` columns

### Canonical column templates

**Aggregate / transactional — PostgreSQL**
```sql
BEGIN TRANSACTION;
CREATE TABLE "schema"."entity" (
  "entity_id"   VARCHAR(50)   NOT NULL PRIMARY KEY,
  -- domain columns here
  "is_deleted"  BOOLEAN       NOT NULL DEFAULT FALSE,
  "created_by"  VARCHAR(250)  NULL,
  "created_on"  TIMESTAMPTZ   NULL,
  "updated_by"  VARCHAR(250)  NULL,
  "updated_on"  TIMESTAMPTZ   NULL
);
COMMIT TRANSACTION;
```

**Aggregate / transactional — SQL Server**
```sql
BEGIN TRANSACTION;
CREATE TABLE [Schema].[Entity] (
  [EntityId]   NVARCHAR(50)   NOT NULL PRIMARY KEY,
  -- domain columns here
  [IsDeleted]  BIT            NOT NULL DEFAULT (0),
  [CreatedBy]  NVARCHAR(250)  NULL,
  [CreatedOn]  DATETIMEOFFSET NULL,
  [UpdatedBy]  NVARCHAR(250)  NULL,
  [UpdatedOn]  DATETIMEOFFSET NULL,
  [RowVersion] TIMESTAMP      NOT NULL   -- ETag / optimistic concurrency
);
COMMIT TRANSACTION;
```

**Reference-data — PostgreSQL**
```sql
BEGIN TRANSACTION;
CREATE TABLE "schema"."entity" (
  "entity_id"  VARCHAR(50)   NOT NULL PRIMARY KEY,
  "code"       VARCHAR(50)   NOT NULL UNIQUE,
  "text"       VARCHAR(250)  NULL,
  "is_active"  BOOLEAN       NULL,
  "sort_order" INTEGER       NULL,
  "created_by" VARCHAR(250)  NULL,
  "created_on" TIMESTAMPTZ   NULL,
  "updated_by" VARCHAR(250)  NULL,
  "updated_on" TIMESTAMPTZ   NULL
);
COMMIT TRANSACTION;
```

**Reference-data — SQL Server**
```sql
BEGIN TRANSACTION;
CREATE TABLE [Schema].[Entity] (
  [EntityId]   NVARCHAR(50)   NOT NULL PRIMARY KEY,
  [Code]       NVARCHAR(50)   NOT NULL UNIQUE,
  [Text]       NVARCHAR(250)  NULL,
  [IsActive]   BIT            NULL,
  [SortOrder]  INT            NULL,
  [RowVersion] TIMESTAMP      NOT NULL,
  [CreatedBy]  NVARCHAR(250)  NULL,
  [CreatedOn]  DATETIMEOFFSET NULL,
  [UpdatedBy]  NVARCHAR(250)  NULL,
  [UpdatedOn]  DATETIMEOFFSET NULL
);
COMMIT TRANSACTION;
```

---

## Phase 4: Register in dbex.yaml (Paths A and B only)

Add one line under `tables:`:
```yaml
- name: <table>   # snake_case for PostgreSQL, PascalCase for SQL Server
```

Keep entries minimal — write `- name: Xxx` and nothing else unless there is a specific known need. Do not add `efModel`, `schema`, or other properties speculatively.

Paths C, D, and E: no `dbex.yaml` change needed.

---

## Phase 5: Apply and Regenerate

```
dotnet run -- All
```

This runs: Create → Migrate → CodeGen → Schema → Data.

- On success: confirm `Infrastructure/Persistence/<Entity>.g.cs` and `Infrastructure/Repositories/*DbContext.g.cs` have been updated (Paths A–C). Path D produces no `.g.cs` changes.
- On failure: **stop**, surface the verbatim error. Do not edit `*.g.cs` files to work around a failure — a broken migration means the script or `dbex.yaml` needs fixing, not the generated output.

---

## Phase 6: Validate

1. Re-inspect to confirm the table state matches expectations:
   ```
   dotnet run -- inspect <schema> <table>
   ```
2. For Paths A–C: confirm `*.g.cs` files reflect the new/changed columns.
3. **Path C (alter) only:** update any hand-written repository, mapper, or application code that references changed or removed columns.
4. Run `dotnet build` on the solution to confirm no compile errors.

---

## Guardrails

- **Scripts are immutable once applied.** Never modify a migration script that has already been run. Author a new script for any subsequent delta.
- **Never edit `*.g.cs` files.** They are owned by DbEx CodeGen — regenerate by fixing the script or `dbex.yaml` and re-running `All`.
- **Never add `IF NOT EXISTS` or `IF OBJECT_ID(...)` guards.** DbEx tracks applied scripts in its journal; each script runs exactly once. Conditional guards mask state mismatches — they don't fix them.
- **Never touch the DbEx journal table.** If the live database is out of sync with the scripts, stop and ask the user. The standard clean fix for a disposable dev database is `dotnet run -- dropanddatabase` (destructive — confirm first).
- **Path D filename limit:** full filename ≤255 characters including the timestamp prefix and extension. Keep descriptors to 3–5 words.
- **Outbox provisioning is a separate concern** — use `dotnet run -- script outbox <schema> <name>` as described in `coreex-tooling.instructions.md`. Do not conflate it with this workflow.
- **No schema-create script.** The `coreex` template ships the schema-create migration; never emit another `create-<schema>-schema` script unless an additional schema is explicitly requested.
