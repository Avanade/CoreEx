# Getting Started with CoreEx

This guide walks through creating a new CoreEx-based microservice from scratch using the `CoreEx.Template` `dotnet new` template pack.

## What we're building

Throughout this guide we'll build the **People** domain — part of a fictional HR solution for Avanade. The solution name is `Avanade.Hr.People`. We'll scaffold the solution with all defaults, stand up local infrastructure, and implement an **Employee** entity with full CRUD, validation, and tests. We'll then add an Outbox Relay host and a Subscribe host and verify both with the generated integration tests.

By the end you'll have a running, fully tested CoreEx microservice as a foundation to build on.

---

## Contents

**Setup**
- [Prerequisites](#prerequisites)
- [Walk-through](#walk-through)

**Core walkthrough**
- [1. Create and enter your solution folder](#1-create-and-enter-your-solution-folder)
- [2. Install the template pack](#2-install-the-template-pack)
- [3. Install AI workflow assets](#3-install-ai-workflow-assets)
- [4. Scaffold the solution](#4-scaffold-the-solution)
- [5. Start the infrastructure](#5-start-the-infrastructure)
- [6. Open your IDE and AI tooling](#6-open-your-ide-and-ai-tooling)
- [7–9. Bring your domain to life](#79-bring-your-domain-to-life)
- [7. Implement your first entity and API host](#7-implement-your-first-entity-and-api-host)
- [8. Add an Outbox Relay host](#8-add-an-outbox-relay-host)
- [9. Add a Subscribe host](#9-add-a-subscribe-host)
- [10. Build and test](#10-build-and-test)

**Going further (optional)**
- [11. Add an Employee query endpoint](#11-add-an-employee-query-endpoint)
- [12. Add GraphQL query support](#12-add-graphql-query-support)

**Reference**
- [What's next](#whats-next)
- [Alternative: AI-guided scaffold](#alternative-ai-guided-scaffold)

---

## Prerequisites

| Requirement | Notes |
|---|---|
| **.NET SDK 10+** | `dotnet --list-sdks` to confirm |
| **Podman** (preferred) or **Docker** | For running local infrastructure |

### Git configuration (Windows)

Run this once to prevent Git for Windows overriding LF line endings:

```bash
git config --global core.autocrlf input
```

---

## Walk-through

The following video provides an example walk-through and commentary of implementing the Employee entity and API host as discussed in the steps below.

[Walk-through video](https://github.com/user-attachments/assets/6f410262-0cd8-47c3-ab7d-8ff5164c5fc2)

_Note:_ The video was accurate at the time of recording and may not reflect the latest changes in CoreEx. If you notice any discrepancies, please refer to the written steps below. Also, given the probabilistic nature of LLMs, AI-generated results _will_ vary; however, the overall solution outcome should be similar.

---

## 1. Create and enter your solution folder

Name the folder using the three-part `Company.Product.Domain` convention — this becomes the solution and project namespace root:

```bash
mkdir Avanade.Hr.People
cd Avanade.Hr.People
git init
```

---

## 2. Install the template pack

```bash
dotnet new install CoreEx.Template
```

This installs the latest stable release. To target a specific version (e.g. to match an existing project's CoreEx NuGet reference), use `::<version>`:

```bash
dotnet new install CoreEx.Template::<version>
```

Verify installation — you should see the `coreex*` templates listed:

```bash
dotnet new list --tag CoreEx
```

---

## 3. Install AI workflow assets

Run `dotnet new coreex-ai` from the **repo root**. This installs CoreEx AI instructions, prompts, and agents into `.github/` so your AI tooling has full CoreEx context from the start:

```bash
dotnet new coreex-ai
```

For a **monorepo** where the CoreEx application lives in a subfolder, pass `--app-folder` to scope AI instructions to that path:

```bash
dotnet new coreex-ai --app-folder backend
```

---

## 4. Scaffold the solution

Run `dotnet new coreex` from inside your solution folder. The folder name is used as the solution name automatically:

```bash
dotnet new coreex
```

That's it — accept all defaults. This gives you PostgreSQL, Azure Service Bus, transactional outbox, and reference data code generation out of the box.

This emits the solution file, `src/`, `tests/`, `tools/`, `docker-compose.yml`, and config files.

> **Note:** The scaffold compiles cleanly straight away — `dotnet restore` and `dotnet build` will both succeed. It is, however, an intentional empty shell: the layer structure is in place but there are no entities, no API routes, no EF mappings, and no ref-data types yet. The AI agent in step 7 is what implements your first domain entity and makes the solution functional.

### Available options

If your project requires a different shape, the full option set is:

| Option | Values | Default | Notes |
|---|---|---|---|
| `--data-provider` | `SqlServer` · `Postgres` · `None` | `Postgres` | `None` for a facade over an external system |
| `--messaging-provider` | `ServiceBus` · `None` | `ServiceBus` | Azure Service Bus emulator is wired into `docker-compose.yml` when enabled |
| `--outbox-enabled` | `true` · `false` | `true` | Transactional outbox — commit events atomically with database writes |
| `--refdata-enabled` | `true` · `false` | `true` | Reference data code generation from `tools/[name].CodeGen/ref-data.yaml` |
| `--rop-enabled` | `true` · `false` | `false` | Railway-oriented programming — services return `Result`/`Result<T>` |

> **Need a DDD Domain layer?** `coreex` no longer scaffolds `Domain` directly — run `dotnet new coreex-domain` afterwards to add it as a separate project, wire it into the solution with `dotnet sln <name>.slnx add src/<name>.Domain`, and add a project reference from `Application` to the new `Domain` project: `dotnet add src/<name>.Application/<name>.Application.csproj reference src/<name>.Domain/<name>.Domain.csproj`. Without that last step, Application-layer code cannot use the aggregate types at all. Only add a Domain layer when the entity has real invariants to enforce (state machines, cross-property rules) — most CRUD-style entities don't need it.

---

## 5. Start the infrastructure

The generated `docker-compose.yml` contains all local dependencies:

```bash
# Podman (preferred)
podman compose up -d

# Docker
docker compose up -d
```

| Container | Port(s) | Purpose |
|---|---|---|
| `db-postgres` | 5432 | Domain database |
| `db-sql-server` | 1433 | Service Bus emulator backing store |
| `redis-cache` | 6379 | Distributed cache |
| `servicebus-emulator` | 5672, 5300 | Azure Service Bus emulator |
| `aspire-dashboard` | 18888 (UI), 4317 (OTLP) | OpenTelemetry dashboard — open at http://localhost:18888 |

> **Optional — verify the scaffold before handing off to the AI agent:**
> ```bash
> dotnet run --project tools/Avanade.Hr.People.Database -- all  # apply initial schema
> dotnet run --project tools/Avanade.Hr.People.CodeGen          # generate ref-data stubs
> ```
> The AI agent in step 5 will run both of these automatically as part of implementing the domain — you only need to run them manually if you want to confirm the scaffold builds cleanly first.

---

## 6. Open your IDE and AI tooling

Open the solution folder in your IDE. The `coreex-ai` step in step 3 has already installed all AI context — no manual setup needed.

**GitHub Copilot Chat:**
- Switch to **Agent** mode and select **CoreEx Expert** for architecture guidance
- Use `/coreex-scaffold` to add missing hosts or reshape the solution

**Claude Code:**
- `CLAUDE.md` is pre-configured — all CoreEx instructions load automatically on startup
- `/coreex-scaffold` — add missing hosts or reshape the solution
- `/coreex-expert` — architecture guidance and pattern decisions
- `/coreex-docs-sync` — refresh the local CoreEx doc cache after a version bump

---

## 7–9. Bring your domain to life

Steps 7, 8, and 9 walk through adding an API host, an Outbox Relay host, and a Subscribe host and implementing your first domain entity end-to-end.

The **recommended path** uses AI-assisted prompts built on the CoreEx skill catalog — the AI reads `AGENTS.md` and the CoreEx instructions, invokes the relevant `.github/skills/` skill (e.g. `coreex-api-e2e` for step 7), and runs `dotnet new coreex-*` for you. The **manual path** lists the raw commands for each host if you prefer direct control.

Use the prompts in sequence. For each, ask your AI agent to work through **Plan → Review → Implement** phases — plan the approach first, confirm with you, then generate code. This prevents a 2,000-line diff appearing before you have a chance to course-correct.

---

## 7. Implement your first entity and API host

### AI-assisted (recommended)

> Paste the following into GitHub Copilot (Agent mode) or Claude Code. It adds the API host, then uses the `coreex-api-e2e` skill to implement the entity end-to-end and run the API tests.

```
The local infrastructure containers (database, redis, service bus) are already running.

Work in phases: Plan your approach and present it for review before writing any code; once confirmed, implement.

Add an API host to the solution — it does not exist yet.

Use the coreex-api-e2e skill to add a new Employee entity end-to-end with the following properties:
  - FirstName (string)
  - NickName (string) - optional
  - LastName (string)
  - Gender (reference data)
  - Salary (decimal)
  - DateOfBirth (DateOnly) - must be at least 16 years of age.

Operations needed: Get, Create, Update, Delete. No business guard/policy is required.

Run all the tests and correct any failures.
```

This invokes the [`coreex-api-e2e`](https://github.com/Avanade/CoreEx/blob/main/.github/skills/coreex-api-e2e/SKILL.md) skill, which chains together the contract, migration, repository, validator, application-service, API endpoint, and integration-test L1 skills in one guided pass — instead of hand-specifying each piece.

### Manual alternative

```bash
dotnet new coreex-api -n Avanade.Hr.People.Api --data-provider Postgres --refdata-enabled true --outbox-enabled true
```

This adds `src/Avanade.Hr.People.Api/` and `tests/Avanade.Hr.People.Test.Api/` and wires both into the solution file. After scaffolding, implement the domain entity, validation, and tests by hand following the [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md) and [Contoso samples](https://github.com/Avanade/CoreEx/tree/main/samples).

---

## 8. Add an Outbox Relay host

### AI-assisted (recommended)

> Paste the following into GitHub Copilot (Agent mode) or Claude Code.

```
Work in phases: Plan your approach and present it for review before writing any code; once confirmed, implement.

Add the Outbox Relay host to the solution. Run the existing relay integration tests; do not attempt to correct any failures.
```

### Manual alternative

```bash
dotnet new coreex-relay -n Avanade.Hr.People.Relay --data-provider Postgres --messaging-provider ServiceBus
```

This adds `src/Avanade.Hr.People.Relay/` and `tests/Avanade.Hr.People.Test.Relay/`.

---

## 9. Add a Subscribe host

### AI-assisted (recommended)

> Paste the following into GitHub Copilot (Agent mode) or Claude Code.

```
Work in phases: Plan your approach and present it for review before writing any code; once confirmed, implement.

Add the Subscribe host to the solution. Run the existing subscriber integration tests; do not attempt to correct any failures.
```

### Manual alternative

```bash
dotnet new coreex-subscribe -n Avanade.Hr.People.Subscribe --data-provider Postgres --messaging-provider ServiceBus --refdata-enabled true
```

This adds `src/Avanade.Hr.People.Subscribe/` and `tests/Avanade.Hr.People.Test.Subscribe/`.

---

## 10. Build and test

```bash
dotnet build
dotnet test tests/Avanade.Hr.People.Test.Unit   # fast, no infrastructure required
dotnet test                                      # all tests — requires containers + database
```

At this point you have a complete, fully tested CoreEx microservice — CRUD API, Outbox Relay, and Subscribe host, all green. Everything from here is optional: two further extensions that build on the Employee entity you already have.

> **Note:** The [walk-through video](#walk-through) above covers steps 1–10 only; it predates steps 11 and 12 below.

---

## Going further (optional)

## 11. Add an Employee query endpoint

### AI-assisted (recommended)

> Paste the following into GitHub Copilot (Agent mode) or Claude Code. It invokes the `coreex-api` skill to add a paged, filterable, sortable query endpoint for the existing Employee entity.

```
/coreex-api

Create a query endpoint for the Employee.
Support paging.
Support the following filter properties:
- LastName - either EQ or StartsWith (case insensitive)
- Gender - standard
Support the following ordering properties (use all three as the default in sequence specified):
- LastName
- FirstName
- Id
```

This invokes the [`coreex-api`](https://github.com/Avanade/CoreEx/blob/main/.github/skills/coreex-api/SKILL.md) skill, which scaffolds the `{Entity}ReadService`/`{Entity}ReadController` CQRS read pair and the underlying `QueryArgsConfig<TSelf>` filter/order configuration on the repository — following the same interview-first pattern used in step 7.

### Manual alternative

Follow the query pattern in the [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md) by hand: add an `EmployeeQueryArgsConfig` under `Infrastructure/Repositories/`, a `QueryAsync` method on the repository and `IEmployeeReadService`, and a matching `QueryAsync` action on `EmployeeReadController`. See `ProductQueryArgsConfig`/`ProductRepository` in the [Contoso samples](https://github.com/Avanade/CoreEx/tree/main/samples) for a worked example.

---

## 12. Add GraphQL query support

### AI-assisted (recommended)

> Paste the following into GitHub Copilot (Agent mode) or Claude Code. The `coreex-graphql` skill is self-contained — no additional prompt detail is required.

```
/coreex-graphql
```

This invokes the [`coreex-graphql`](https://github.com/Avanade/CoreEx/blob/main/.github/skills/coreex-graphql/SKILL.md) skill, which wires up `AddCoreExGraphQLLite`/`MapCoreExGraphQLLite` on the API host (first-time only) and registers a GraphQL root for the Employee query endpoint added in step 11 — bridging its existing `QueryArgsConfig`, with no new filter/sort logic written.

### Manual alternative

Follow [`CoreEx.Data.GraphQL`'s AGENTS.md](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Data.GraphQL/AGENTS.md) to wire `AddCoreExGraphQLLite`/`MapCoreExGraphQLLite` by hand and register an `AddQuery<EmployeeLite>` (or `AddQuery<Employee>` if there's no separate "Lite" projection) root.

---

## What's next

| Resource | Description |
|---|---|
| `README.md` | Solution-local developer reference (infrastructure, database commands, conventions) |
| `AGENTS.md` | AI-oriented project brief — architecture, configuration, docs index |
| [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md) | Every CoreEx architectural and design pattern, with links to layer docs |
| [Layer Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/layers.md) | Business and host layer overview with full dependency diagram |
| [Contoso samples](https://github.com/Avanade/CoreEx/tree/main/samples) | Complete reference implementation — Products, Shopping, Orders domains |
| [Capabilities Guide](https://github.com/Avanade/CoreEx/blob/main/docs/capabilities.md) | Deep capability and pattern explanations |

---

## Alternative: AI-guided scaffold

If you prefer an interactive approach rather than choosing template parameters manually, use the `/coreex-scaffold` command (GitHub Copilot Chat Agent mode, or Claude Code) after completing steps 1–2:

```
/coreex-scaffold
```

The workflow interviews you in plain English — one question at a time — and derives and runs the correct `dotnet new coreex*` commands for the shape you describe. It also installs the template pack if not already present.
