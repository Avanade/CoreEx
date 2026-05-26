# Developer Tooling

The samples include two developer-time tooling projects per domain that are not part of the runtime stack. They run locally during development to generate code and manage the database schema. Neither is deployed or referenced at runtime.

---

## Code Generation (`*.CodeGen`)

Each domain has a `*.CodeGen` console project (e.g. `Contoso.Products.CodeGen`, `Contoso.Shopping.CodeGen`) that drives the [CoreEx.CodeGen](../../src/CoreEx.CodeGen) package. Its sole purpose is to eliminate the boilerplate associated with reference data: every reference-data entity follows the same deterministic pattern — contract class, API controller route, service method, repository interface, repository implementation, and EF Core mapper — so all of it is generated rather than hand-authored.

### How it works

1. A developer authors `ref-data.yaml` alongside `Program.cs`, declaring each reference-data entity and any per-entity or per-property overrides.
2. The file is validated against the [`coreex-refdata.json`](../../schema/coreex-refdata.json) JSON Schema (IDE YAML language-server support provides validation and auto-complete).
3. Running `dotnet run` invokes `CodeGenConsole`, which hands off to [OnRamp](https://github.com/Avanade/OnRamp). OnRamp loads `ref-data-script.yaml` (embedded in the package), resolves the ordered generation steps, evaluates the embedded Handlebars templates against the typed configuration model, and writes `.g.cs` files into the correct target projects.
4. Target project directories — Contracts, Api, Application, Infrastructure — are resolved automatically by convention from the CodeGen project's location; no path configuration is required.

### What is generated

| Artefact | Target layer | Description |
|---|---|---|
| `*.g.cs` contract class | Contracts | Typed reference-data entity contract. |
| `*.g.cs` controller route | API host | HTTP endpoint exposing the entity collection. |
| `*.g.cs` service method | Application | Service-layer method delegating to the repository. |
| `*.g.cs` repository interface | Application | Interface declaration for the entity repository. |
| `*.g.cs` repository | Infrastructure | EF Core repository implementation. |
| `*.g.cs` mapper | Infrastructure | Bidirectional EF Core mapper for the entity. |

All generated files carry the `.g.cs` suffix, clearly distinguishing them from hand-authored code and excluding them from manual maintenance.

### `ref-data.yaml` structure

```yaml
collectionSortOrder: Code        # sort order applied to all entity collections
repository: EntityFramework      # repository implementation strategy
entities:
- name: Brand                    # simplest form — all defaults apply
- name: SubCategory
  properties:
  - name: CategoryCode
    type: ^Category              # ^ prefix = reference-data typed property
- name: UnitOfMeasure
  plural: UnitsOfMeasure         # override pluralization
  properties:
  - name: Scale
    type: int
```

The full schema reference is available in [`docs/CodeGeneration.md`](../../src/CoreEx.CodeGen/docs/CodeGeneration.md), [`docs/Entity.md`](../../src/CoreEx.CodeGen/docs/Entity.md), and [`docs/Property.md`](../../src/CoreEx.CodeGen/docs/Property.md).

### Code-count reporting

Running `dotnet run -- count` invokes the `Count` command, which walks all solution output directories, classifies `.g.cs` vs hand-authored `.cs` files, and renders a formatted table showing total and generated file and line counts per directory. This gives an at-a-glance view of how much of the codebase is generated.

---

## Database Management (`*.Database`)

Each domain has a `*.Database` console project (e.g. `Contoso.Products.Database`, `Contoso.Shopping.Database`) that drives the [DbEx](https://github.com/Avanade/DbEx) database management framework. It is the **primary tool for deploying and managing the domain database across all environments** — local development, CI/CD pipelines, and production — providing a consistent, repeatable, command-driven lifecycle.

Products uses `DbEx.Postgres` (PostgreSQL); Shopping uses `DbEx.SqlServer` (SQL Server), demonstrating that the same tooling approach is database-agnostic. A MySQL variant (`DbEx.MySql`) is also available.

### DbEx commands

DbEx exposes a rich command set. A single run can execute one or more commands in order:

| Command | Description |
|---|---|
| `Drop` | Drops the database where it already exists. |
| `Create` | Creates the database where it does not already exist. |
| `Migrate` | Applies outstanding [DbUp](https://dbup.readthedocs.io/en/latest/philosophy-behind-dbup/)-style ordered migration scripts; tracks which have already run and applies only new ones. Scripts are immutable once applied. |
| `CodeGen` | Executes an optional code-generation step — used here to generate the Infrastructure-layer `.g.cs` files (persistence models and `DbContext` partial). |
| `Schema` | Drops and re-applies idempotent schema objects (stored procedures, functions, types) from the `Schema/` folder on every run. |
| `Data` | Applies YAML/JSON-defined seed data — useful for reference data, master data, and test fixtures. Supports both `INSERT` and `MERGE` semantics. |
| `Reset` | Deletes all data from the database (exclusions configurable); designed for test environment resets. |
| `Execute` | Executes arbitrary SQL statements or script files directly. |
| `Script` | Scaffolds a new timestamped migration script file using the naming convention. |

Composite commands combine the above for common scenarios:

| Composite | Runs |
|---|---|
| `All` | `Create` → `Migrate` → `CodeGen` → `Schema` → `Data` |
| `Deploy` | `Migrate` → `Schema` |
| `DeployWithData` | `Migrate` → `Schema` → `Data` |
| `ResetAndAll` | `Reset` → `All` (test environments) |

In the samples, `dotnet run` without arguments defaults to `All` for local development, and `Deploy` (or `DeployWithData`) is the typical command for environment promotion pipelines.

### Migrate — schema evolution

Migration scripts live under `Migrations/` as embedded resources and follow a timestamp-ordered naming convention:

```
20260101-000201-create-products-product.pgsql
20260101-000202-create-products-inventory.pgsql
```

Scripts are **immutable once applied** — subsequent schema changes require new scripts (e.g. `ALTER TABLE`). DbEx tracks applied scripts in a journal table, so only new scripts are run. Scripts can include moustache-style parameter substitution (`{{Company}}`) resolved from `MigrationArgs.Parameters`.

### Schema — idempotent objects

Objects under `Schema/` (stored procedures, functions, user-defined table types) are dropped and re-created on every `Schema` run. This makes them safely idempotent and avoids the immutability constraint of migration scripts. The transactional outbox stored procedures / functions (e.g. `spOutboxEnqueue`, `fn_outbox_batch_claim`) are generated here and carry a `.g.sql` / `.g.pgsql` suffix.

### Data — seeding

Seed data is declared in `Data/ref-data.yaml` using a concise schema → table → rows YAML structure. DbEx infers column types from the live schema and supports:

- **Insert** (default) — straightforward row insertion.
- **Merge** (`$` prefix on table name) — upsert semantics; idempotent on repeated runs.
- **Reference data resolution** — foreign-key `Id` columns can be specified by code; DbEx resolves the identifier at runtime via sub-query.
- **Identifier generation** (`^` prefix) — auto-generates GUIDs for primary keys.

```yaml
products:
  - $brand:           # merge (upsert) — safe to re-run
    - AVA: Avanade
    - CON: Contoso
  - $unit_of_measure:
    - EA: Each
    - KG: Kilogram
```

### CodeGen — C# code generation

The `CodeGen` command generates `.g.cs` files into the Infrastructure project based on the tables declared in `dbex.yaml`:

| Generated artefact | Location | Description |
|---|---|---|
| `<Entity>.g.cs` (one per table) | `Infrastructure/Persistence/` | Schema-aligned persistence model extending `ModelBase<TId>`, with optional marker interfaces such as `ILogicallyDeleted`. |
| `*DbContext.g.cs` | `Infrastructure/Repositories/` | Partial `DbContext` class exposing `AddGeneratedModels(ModelBuilder)` to register all persistence models with EF Core. |

These are the only `.g.cs` files produced by `*.Database`; all other generated C# across the solution comes from `*.CodeGen`.

### `dbex.yaml` structure

`dbex.yaml` is the configuration entry point, declaring the schema, which tables to include in code generation, and whether to provision outbox infrastructure:

```yaml
schema: products        # schema (namespace) for all tables
outbox: true            # generate transactional outbox tables + stored procs/functions
outboxName: outbox      # prefix for outbox table and procedure names
tables:
- name: brand           # reference-data tables (also seeded via Data/ref-data.yaml)
- name: category
- name: product         # transactional tables
- name: inventory
```

When `outbox: true` is set, DbEx generates the full outbox infrastructure — tables (via a `Migrate` script) and stored procedures / functions (via `Schema`) — with no hand-authored DDL required.

---

## Relationship to the layer stack

The tooling projects feed the layer stack at design time but are invisible at runtime:

- **CodeGen** → generates `.g.cs` files that become part of the **Contracts**, **API**, **Application**, and **Infrastructure** layers.
- **Database** → deploys the schema that the **Infrastructure** layer reads and writes, provisions the outbox tables consumed by the **Outbox Relay** host, and generates the Infrastructure-layer persistence models (`Persistence/*.g.cs`) and EF Core `DbContext` partial (`Repositories/*DbContext.g.cs`).

See [contracts-layer.md](contracts-layer.md#reference-data) for how generated reference-data types are used, [infrastructure-layer.md](infrastructure-layer.md#persistence-models) for the schema-aligned models, and [hosts-layer.md](hosts-layer.md#outbox-relay-host) for how the outbox relay consumes the deployed outbox infrastructure.
