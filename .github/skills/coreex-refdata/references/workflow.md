# coreex-refdata: Workflow

Full step-by-step workflow for adding or modifying a reference data type. Follow phases in order — the database must be up to date before CodeGen runs.

---

## Phase 1 — Establish Baseline

Before any change, bring the database to a known good state and identify the scope.

1. Identify which domain is being changed (Products = PostgreSQL, Shopping/Orders = SQL Server).
2. From the `*.Database` project directory:
   ```
   dotnet run -- database
   ```
   This is non-destructive (`Create` → `Migrate` → `Schema` → `Data`). **If it fails, stop and surface the verbatim error — do not continue.**
3. Identify all tables involved: the ref-data entity's own table, plus any ref-data tables referenced via `^Type` properties.

---

## Phase 2 — Choose Your Path

Inspect the table(s) to determine which path applies:

```
dotnet run -- inspect <schema> <table>
```

| Result | Path |
|---|---|
| Table does not exist → new entity | **Path A** — new table + seed + CodeGen |
| Table exists, `Reference Data: Yes` → adding/changing property | **Path B** — alter table + CodeGen update |
| Table exists but no CodeGen entry yet (table was created externally) | **Path C** — seed + dbex.yaml + CodeGen only |
| Seed rows only, no schema change | **Path D** — seed rows only |
| Wire existing type into a contract | **Path E** — contract wiring only |

---

## Path A — New Ref-Data Type (new table + seed rows + CodeGen)

### A1 — Scaffold migration script

From `*.Database`:
```
dotnet run -- script refdata <schema> <table>
```

- **SQL Server**: table = `PascalCase` (e.g. `Brand`), schema = `PascalCase` (e.g. `Products`)
- **PostgreSQL**: table = `snake_case` (e.g. `brand`), schema = `snake_case` (e.g. `products`)

The scaffold generates a template. **Replace the primary key placeholder to match the contract's identifier type:**
- Default (`string` id) → `[{Name}Id] NVARCHAR(50) NOT NULL PRIMARY KEY` (SQL Server) / `"{name}_id" VARCHAR(50) NOT NULL PRIMARY KEY` (PostgreSQL)
- `Guid` id → `UNIQUEIDENTIFIER` / `UUID`; `int` id → `INT` / `INTEGER`
- Remove `DEFAULT (NEWSEQUENTIALID())` (SQL Server) and replace `SERIAL` with the explicit type (PostgreSQL) — the app assigns identifiers, not the database.

Standard ref-data table shape (the scaffold produces this, verify and adapt):

```sql
-- SQL Server
CREATE TABLE [Schema].[Xxx] (
  [XxxId] NVARCHAR(50) NOT NULL PRIMARY KEY,
  [Code] NVARCHAR(50) NOT NULL UNIQUE,
  [Text] NVARCHAR(250) NULL,
  [IsActive] BIT NULL,
  [SortOrder] INT NULL,
  [RowVersion] TIMESTAMP NOT NULL,
  [CreatedBy] NVARCHAR(250) NULL,
  [CreatedOn] DATETIMEOFFSET NULL,
  [UpdatedBy] NVARCHAR(250) NULL,
  [UpdatedOn] DATETIMEOFFSET NULL
);
```

```sql
-- PostgreSQL (no RowVersion — xmin provides concurrency)
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

For types with extra properties (e.g. `Scale: int`), add the extra column(s) after `SortOrder` and before the audit columns.

For types with a `^TypeCode` navigation property (e.g. `SubCategory.CategoryCode`), add a `{Name}Code` column (not `{Name}Id`, no FK):

```sql
-- SQL Server
[CategoryCode] NVARCHAR(50) NULL   -- no REFERENCES; resolved by Code at runtime
```
```sql
-- PostgreSQL
"category_code" VARCHAR(50) NULL
```

### A2 — Add seed rows

Edit `*.Database/Data/ref-data.seed.yaml`. Append the new table under the existing schema key (create the schema key if the file is new).

```yaml
# SQL Server (PascalCase)
Shopping:
  - $^Brand:
    - ACME: Acme Corp
    - BETA: Beta Co
```

```yaml
# PostgreSQL (snake_case)
products:
  - $^brand:
    - ACME: Acme Corp
    - BETA: Beta Co
```

Rules:
- **Always use `$^` prefix** on the table entry (merge + auto-generate id).
- **Never include the `{Name}Id` column** in a row — `$^` generates it.
- **Never include `SortOrder`** — DbEx assigns it positionally. Order rows by `Code` so display order is sensible.
- **Never include `IsActive`** — defaults to active.
- Use the `Code: Text` shorthand (sets `Code` and `Text`). Use an inline object only for extra columns: `{ code: HR, text: Hour, scale: 2 }`.
- For a type with a navigation property (`^CategoryCode`), the inline object references the related code: `{ code: XC, text: Cross country, category_code: B }`.

### A3 — Register in dbex.yaml

Edit `*.Database/dbex.yaml`. Add the table under `tables:` — minimal form only:

```yaml
tables:
  - name: brand          # PostgreSQL
  - name: Brand          # SQL Server
```

Do not add `efModel`, `schema`, or other properties unless there is a specific, known reason.

### A4 — Apply DB changes

From `*.Database`:
```
dotnet run -- All
```

This runs: `Create` → `Migrate` → `CodeGen` → `Schema` → `Data`. **If it fails, stop and surface verbatim error — do not proceed to CoreEx CodeGen.**

Then proceed to → **Phase 3 (CoreEx CodeGen)**.

---

## Path B — Alter Existing Ref-Data Table

When a property needs to be added to (or changed on) an existing ref-data table.

### B1 — Scaffold alter script

```
dotnet run -- script alter <schema> <table>
```

Add only the delta (new columns or constraints). Applied scripts are immutable — never modify the original create script.

```sql
-- SQL Server example: adding a Scale column
ALTER TABLE [Products].[UnitOfMeasure]
  ADD [Scale] INT NULL;
```

```sql
-- PostgreSQL
ALTER TABLE "products"."unit_of_measure"
  ADD COLUMN "scale" INTEGER NULL;
```

### B2 — Update seed rows if needed

If the new column needs values in existing seed rows, update them in `Data/ref-data.seed.yaml` with inline objects.

### B3 — Apply and continue to CodeGen

```
dotnet run -- All
```

Then proceed to → **Phase 3 (CoreEx CodeGen)** to update the entity definition.

---

## Path C — Table Exists, No CodeGen Entry Yet

Table was created externally (e.g. by `coreex-db-migration`). Only seed and CodeGen steps needed.

1. Verify table is in `dbex.yaml` `tables:` — add `- name: Xxx` if missing.
2. Add seed rows to `Data/ref-data.seed.yaml` if required.
3. Run `dotnet run -- All` to apply seed data and regenerate EF models.
4. Proceed to → **Phase 3 (CoreEx CodeGen)**.

---

## Path D — Seed Rows Only

No schema change. Just add/amend rows in `*.Database/Data/ref-data.seed.yaml`, then:

```
dotnet run -- Data
```

No CodeGen re-run needed unless `ref-data.yaml` entity definitions changed.

---

## Path E — Wire Existing Type Into a Contract

No database or CodeGen change needed. Existing types only.

On the contract class:
1. Class must be `[Contract]` and `partial`.
2. Add the property as `partial` (only `[ReferenceData<T>]` properties are partial):

```csharp
[ReferenceData<Brand>]
[Localization("Brand")]   // only if the auto-derived label would be wrong
public partial string? BrandCode { get; set; }
```

3. Rebuild — the Roslyn generator emits the typed `Brand` navigation property automatically. Do not hand-author it.

---

## Phase 3 — CoreEx CodeGen (edit ref-data.yaml + generate)

This step runs in the **`*.CodeGen`** project — completely separate from `*.Database`.

### Edit `*.CodeGen/ref-data.yaml`

Add or update the entry under `entities:`. The standard `IReferenceData` properties (`Id`, `Code`, `Text`, `IsActive`, `SortOrder`, `Description`, `StartsOn`, `EndsOn`) are **automatic** — do not declare them.

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Avanade/CoreEx/refs/heads/main/schema/coreex-refdata.json
collectionSortOrder: Code
repository: EntityFramework
entities:
- name: Brand                   # minimal — no extra properties
- name: SubCategory
  properties:
  - name: CategoryCode
    type: ^Category             # ^ = typed navigation accessor generated
- name: UnitOfMeasure
  plural: UnitsOfMeasure        # override irregular pluralization
  properties:
  - name: Scale
    type: int                   # extra stored column
- name: DiscountCoupon
  properties:
  - name: DiscountPercentage
    type: decimal
    excludeContract: true       # persistence model only — not in the contract
```

Key `entities:` options:

| Key | Default | Purpose |
|---|---|---|
| `name` | — | Entity name (PascalCase) |
| `plural` | Auto-pluralized | Override for irregular plurals |
| `idType` | `string` | Identifier type: `string`, `Guid`, `int` |
| `properties[].name` | — | Additional property name |
| `properties[].type` | — | CLR type; prefix `^` for navigation accessor |
| `properties[].excludeContract` | `false` | Persistence model only |

### Run CodeGen

From the `*.CodeGen` project directory:

```
dotnet run
```

On success, CodeGen emits `.g.cs` files across all layers:

| Artefact | Layer |
|---|---|
| `<Entity>.g.cs` | Contracts |
| `<Entity>Controller.g.cs` | API host |
| `<Entity>Service.g.cs` | Application |
| `I<Entity>Repository.g.cs` | Application |
| `<Entity>Repository.g.cs` | Infrastructure |
| `<Entity>Mapper.g.cs` | Infrastructure |

**On failure, relay verbatim error output — do not create or edit `.g.cs` files to work around it. Fix `ref-data.yaml` and re-run.**

---

## Phase 4 — Global Usings (first entity in a fresh solution only)

When this is the first CodeGen-generated type in a solution, the generated code references namespaces that are not yet in `GlobalUsing.cs`. Add them as each layer gets its first generated artefact:

| When | Add to `GlobalUsing.cs` of |
|---|---|
| First contract generated | `Application`, `Infrastructure`, `Api` — add `global using {Solution}.Contracts;` |
| First repo interface generated | `Application`, `Infrastructure` — add `global using {Solution}.Application.Repositories;` |
| First controller referencing a service | `Api` — add `global using {Solution}.Application;` |

Do **not** pre-add empty-namespace usings — wait until the generated code that needs them exists.

---

## Phase 5 — Validate

1. Run `dotnet build` from the solution root — no warnings or errors.
2. Confirm all expected `.g.cs` files are present in the correct layer directories.
3. If this is the first type: confirm the controller is reachable (GET endpoint registered by the generated controller).
4. Optionally: add a test for the new ref-data GET endpoint.

---

## Guardrails

- **Never edit `.g.cs` files** — they are owned by `*.CodeGen` (contract, controller, service, repository, mapper) or `*.Database` (persistence model). Regenerate instead.
- **Two separate YAML files** — `*.CodeGen/ref-data.yaml` (entity definitions) vs `*.Database/Data/ref-data.seed.yaml` (seed rows). Wrong file = runtime failure.
- **Never include `{Name}Id` in seed rows** — `$^` auto-generates the id. Including it is always a bug.
- **Always use `$^` on ref-data table entries** — regardless of identifier type (`string`, `Guid`, `int`).
- **Order seed rows by Code** — `SortOrder` is positional; unordered rows produce arbitrary display order.
- **Do not add `SortOrder` or `IsActive` to seed rows** — both are automatic.
- **Do not add standard `IReferenceData` properties** (`Id`, `Code`, `Text`, etc.) to `ref-data.yaml` `properties:` — they are inherited automatically.
- **PK default is `string` (`NVARCHAR(50)` / `VARCHAR(50)`)** — do not substitute `UNIQUEIDENTIFIER`/`UUID` unless `idType: Guid` is explicitly declared in `ref-data.yaml`.
- **No FK on ref-data relationships** — `{Name}Code` columns reference by Code value; no `REFERENCES` constraint.
- **Do not author a schema-create script** — the template ships one; the schema already exists.
- **PostgreSQL = snake_case throughout** (schema, table, columns, seed keys). SQL Server = PascalCase throughout.
- **Two distinct CodeGen steps** — `*.Database` (`dotnet run -- All`) generates EF persistence models from the live schema; `*.CodeGen` (`dotnet run`) generates contracts, controllers, services, repositories, and mappers from `ref-data.yaml`. Run them in that order.
