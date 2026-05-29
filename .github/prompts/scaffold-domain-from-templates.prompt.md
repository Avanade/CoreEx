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

---

## Scaffolding Questions

Ask all unanswered questions before materializing any files:

| # | Question | Options | Default |
|---|----------|---------|---------|
| 1 | **Solution** (e.g. `Contoso`) | free text | — |
| 2 | **Domain** (e.g. `Orders`) | free text | — |
| 3 | **Entity** (e.g. `Order`) | free text | — |
| 4 | **Database engine** | `SQL Server` / `PostgreSQL` | `SQL Server` |
| 5 | **Reference Data** — generate a CodeGen project for reference data (e.g. `{Entity}Status`)? | `Yes` / `No` | `No` |
| 6 | **Child Entity** — does `{Entity}` own a child entity (e.g. `{Entity}Item`)? If Yes, provide the child entity name. | `Yes <name>` / `No` | `No` |
| 7 | **Domain project** — include a DDD domain project (`{Solution}.{Domain}.Domain`) with aggregate roots and value objects? | `Yes` / `No` | `No` |
| 8 | **ROP** — use Railway Oriented Programming (`Result<T>`) in service and repository layers? | `Yes` / `No` | `No` |
| 9 | **Outbox Relay** — include an `{Solution}.{Domain}.Outbox.Relay` hosted-service project? | `Yes` / `No` | `Yes` |
| 10 | **Subscribe** — include an `{Solution}.{Domain}.Subscribe` event-subscriber hosted-service project? | `Yes` / `No` | `Yes` |
| 11 | `targetRoot` (root folder for domain projects) | path | `samples/src` |
| 12 | `testsRoot` (root folder for test projects) | path | `samples/tests` |

---

## Naming Derivations (Auto-Derive)

Derive all naming variants from the supplied values unless explicitly overridden:

| Placeholder | Rule | Example |
|-------------|------|---------|
| `{EntityPlural}` | English plural of `{Entity}`. Append `s`; `y`→`ies` after consonant; `s/x/z/ch/sh`→`es`. | `Order` → `Orders`, `Category` → `Categories` |
| `{entityKebab}` | kebab-case of `{Entity}` | `Order` → `order`, `SalesOrder` → `sales-order` |
| `{entityPluralKebab}` | kebab-case of `{EntityPlural}` | `Orders` → `orders` |
| `{domainKebab}` | kebab-case of `{Domain}` | `Orders` → `orders` |
| `{solution-kebab}` | kebab-case of `{Solution}` | `Contoso` → `contoso` |
| `{entity_kebab}` | snake_case of `{entityKebab}` (replace `-` with `_`) | `sales-order` → `sales_order` |
| `{entity_status_kebab}` | snake_case of `{Entity}Status` kebab | `OrderStatus` → `order_status` |
| `{child_entity_kebab}` | snake_case of `{ChildEntity}` kebab | `OrderItem` → `order_item` |
| `{MigrationTimestamp}` | UTC timestamp at scaffold time, format `yyyymmdd-hhmmss` | `20260529-143000` |

---

## Placeholders to Replace

Replace every occurrence in every materialized file:

- `{Solution}`, `{Domain}`, `{Entity}`, `{ChildEntity}`, `{EntityPlural}`
- `{entityKebab}`, `{entityPluralKebab}`, `{domainKebab}`, `{solution-kebab}`
- `{entity_kebab}`, `{entity_status_kebab}`, `{child_entity_kebab}`
- `{MigrationTimestamp}`

> **Never create or edit `*.g.cs`, `*.g.sql`, or `*.g.pgsql` files.** These are produced exclusively by `CoreEx.CodeGen` (`dotnet run` in the CodeGen project) or DbEx migrations; hand-authoring them defeats the generator.

---

## Conditional File-Inclusion Rules

### Database Engine

| Condition | Include | Exclude |
|-----------|---------|---------|
| SQL Server | `*/sqlserver/**` | `*/postgres/**` |
| PostgreSQL | `*/postgres/**` | `*/sqlserver/**` |

`*/_shared/**` is always included regardless of engine.

### Reference Data (Q5)

| Answer | Action |
|--------|--------|
| **Yes** | Include `CodeGen/` project. Include ref-data patterns in Application and Infrastructure (service registration, `ReferenceDataService`, `AddReferenceDataOrchestrator` calls). |
| **No** | Omit `CodeGen/` project entirely. Remove all ref-data patterns (`ReferenceDataService`, status table migration, status mapper, `{Entity}Status` contract usages). |

### Child Entity (Q6)

| Answer | Action |
|--------|--------|
| **Yes `<name>`** | Include `Database/*/Migrations/*-childentity*` migration. Add child entity to `dbex.yaml`. Include child entity contract, mapper, persistence model, and EfDb relationship. |
| **No** | Skip all child entity files. |

### Domain Project (Q7)

| Answer | Action |
|--------|--------|
| **Yes** | Include `Domain/` project (`{Solution}.{Domain}.Domain`). Add `ProjectReference` to Domain from Application. |
| **No** | Skip `Domain/` project entirely. |

### ROP — Railway Oriented Programming (Q8)

| Answer | Action |
|--------|--------|
| **Yes** | Use `Application/rop/` templates for service and interfaces; use `Infrastructure/_shared/rop/` for repository. Skip the non-ROP equivalents. |
| **No** | Use default `Application/` service and interfaces; use `Infrastructure/_shared/Repositories/EntityRepository.cs.template`. Skip `*/rop/` folders. |

### Outbox Relay (Q9)

| Answer | Action |
|--------|--------|
| **Yes** | Include `Outbox.Relay/<engine>/` project (`csproj`, `Program.cs`, `appsettings.json`). |
| **No** | Skip `Outbox.Relay/` entirely. |

### Subscribe (Q10)

| Answer | Action |
|--------|--------|
| **Yes** | Include `Subscribe/<engine>/` (`csproj`, `Program.cs`, `appsettings.json`) and `Subscribe/_shared/` (`GlobalUsing.cs`, `Subscribers/{Entity}EventSubscriber.cs`). |
| **No** | Skip `Subscribe/` entirely. |

---

## Output Projects

Create the following projects under `{targetRoot}`:

**Always:**
- `{Solution}.{Domain}.Contracts`
- `{Solution}.{Domain}.Application`
- `{Solution}.{Domain}.Infrastructure`
- `{Solution}.{Domain}.Api`
- `{Solution}.{Domain}.Database`

**Conditional:**
- `{Solution}.{Domain}.CodeGen` — Reference Data = Yes
- `{Solution}.{Domain}.Domain` — Domain = Yes
- `{Solution}.{Domain}.Outbox.Relay` — Outbox Relay = Yes
- `{Solution}.{Domain}.Subscribe` — Subscribe = Yes

**Test projects** under `{testsRoot}`:
- `{Solution}.{Domain}.Test.Unit`
- `{Solution}.{Domain}.Test.Api`

---

## Materialization Rules

1. Copy each `.template` file into the corresponding project location.
2. Remove the `.template` suffix from output files.
3. Rename `Domain.*.csproj.template` → `{Solution}.{Domain}.*.csproj`.
4. Rename `Entity*` files to use the concrete entity name (e.g. `{Entity}Service.cs`).
5. Keep folder structure identical to the template tree (after stripping `_shared/`, `sqlserver/`, `postgres/`, `rop/` routing segments).
6. Preserve line endings and indentation exactly.
7. For `Subscribe/_shared/` files: output into the root of the Subscribe project (not a `_shared/` subfolder).
8. For `Subscribers/` subfolder: output into `Subscribers/` within the Subscribe project.

---

## Required Post-Generation Adjustments

After template materialization:

1. **API controllers**: Confirm routes use concrete kebab-case paths; OpenApi tags use `{EntityPlural}`.
2. **Database seed data**: If Reference Data = Yes, ensure `{Entity}Status` seed values (e.g. `Pending`, `Confirmed`, `Cancelled`) are present unless alternatives were supplied.
3. **Infrastructure EfDb**: Confirm the mapped model property uses the concrete plural entity name.
4. **Subscribe Program.cs**: If Reference Data = No, remove the `AddReferenceDataOrchestrator<ReferenceDataService>()` line.
5. **Program files**: Confirm all namespaces match the generated project names.
6. **Test projects**: Confirm namespaces match `{Solution}.{Domain}.Test.Unit` / `{Solution}.{Domain}.Test.Api`; unit tests follow `WithGenericTester<EntryPoint>`; API tests follow `WithApiTester<{Solution}.{Domain}.Api.Program>`; assertions use AwesomeAssertions (not FluentAssertions).
7. **Solution structure**: Add all generated domain and test projects to the Visual Studio solution, grouped under a solution folder named `{Domain}`.

---

## Validation

Run `dotnet build` for all generated projects:

```
dotnet build {targetRoot}/{Solution}.{Domain}.Contracts
dotnet build {targetRoot}/{Solution}.{Domain}.Application
dotnet build {targetRoot}/{Solution}.{Domain}.Infrastructure
dotnet build {targetRoot}/{Solution}.{Domain}.Api
dotnet build {targetRoot}/{Solution}.{Domain}.Database
dotnet build {testsRoot}/{Solution}.{Domain}.Test.Unit
dotnet build {testsRoot}/{Solution}.{Domain}.Test.Api
```

Also build any conditional projects that were included.

Run tests:

```
dotnet test {testsRoot}/{Solution}.{Domain}.Test.Unit
dotnet test {testsRoot}/{Solution}.{Domain}.Test.Api
```

Fix all compilation errors and test failures before reporting completion.

---

## Completion Gate

Use `/.github/templates/domain/DomainScaffold.checklist.md` as the final acceptance checklist. Do not finish until all applicable items are satisfied.
