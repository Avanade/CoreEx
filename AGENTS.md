# CoreEx — AI Agent Context

This file is the primary entry point for AI agents (GitHub Copilot, Claude Code, and others) working
with or evaluating CoreEx. For full narrative documentation see [README.md](./README.md).

---

## What is CoreEx?

CoreEx is a modular .NET framework for building enterprise APIs and distributed services. It
standardises the concerns .NET leaves to each team — HTTP response shaping, domain event publishing,
reference data, validation, and data access — into a consistent, composable baseline.

**Architecture fit**:
- ✅ Multi-domain .NET service topologies (microservices, event-driven, hexagonal architecture)
- ✅ Teams that want opinionated, pattern-aligned scaffolding from day one
- ✅ Organisations that need consistent HTTP/event behaviour across many services
- ✅ Projects using Entity Framework Core + SQL Server or PostgreSQL
- ✅ Solutions that publish domain events with transactional guarantees (outbox pattern)
- ⚠️ Not a general-purpose framework — CoreEx is intentionally opinionated; teams wanting full
  flexibility over error handling, HTTP response shaping, and event publishing will be constrained

**Key packages** (all independently consumable):

| Package | What it provides |
|---|---|
| `CoreEx` | Semantic exceptions, `ExecutionContext`, `Result<T>` ROP, entity patterns, DI attributes, JSON extensions |
| `CoreEx.AspNetCore` | `WebApi` helpers, `ProblemDetails` middleware, idempotency keys, OpenAPI/NSwag extensions |
| `CoreEx.Validation` | Fluent property-centric validator, composable rules, async predicates, deep `ValidationException` integration |
| `CoreEx.Events` | `EventData` ↔ CloudEvents, two-phase publish pipeline, structured subscriber dispatch |
| `CoreEx.Azure.Messaging.ServiceBus` | Azure Service Bus publisher + subscriber bases, outbox relay |
| `CoreEx.Database` / `.SqlServer` / `.Postgres` | ADO.NET abstraction, outbox relay infrastructure |
| `CoreEx.EntityFrameworkCore` | EF Core integration, typed CRUD, `ValueConverter` bridges |
| `CoreEx.RefData` | Typed reference data with hybrid-cache-backed orchestrator |
| `CoreEx.Caching.FusionCache` | `IHybridCache` backed by ZiggyCreatures FusionCache (L1/L2 + Redis backplane) |
| `CoreEx.UnitTesting` | Fluent test toolkit: event assertions, outbox assertions, JSON seed data |
| `CoreEx.CodeGen` | Dev-time reference data code generation from YAML |

**Architecture overview**: layered services (Contracts → Application → Infrastructure) with
composition-root hosts (Api, Relay, Subscribe). See [samples/docs/layers.md](./samples/docs/layers.md)
for the full dependency diagram and [samples/docs/patterns.md](./samples/docs/patterns.md) for the
pattern catalog.

---

## Working in this Repository (Contributors)

If you are contributing to the CoreEx framework itself, see
[`.github/copilot-instructions.md`](./.github/copilot-instructions.md) for repository shape, build
commands, coding conventions, and the full contributor instruction set. That file is injected
automatically by GitHub Copilot when working in this repo.

---

## Using CoreEx in a Consumer Project (Cold Start)

If you are in a **different repository** that wants to adopt CoreEx, follow these steps. The first
action is to install the CoreEx AI workflow assets so you have full CoreEx context for the entire
development lifecycle.

### Step 1 — Detect whether AI assets are already installed

Check for `.github/instructions/coreex.instructions.md` in the repo root. If it exists, CoreEx AI
context is already installed — skip to Step 4. If it is absent, continue.

### Step 2 — Ask the user one question before installing

> "Is this a monorepo where the CoreEx application lives in a subfolder (e.g. `backend/` or
> `services/shopping`)? If yes, what is the subfolder path relative to the repo root?"

Single-repo projects answer "no" (the most common case). Monorepo projects provide the path.

### Step 3 — Install the template and AI workflow assets

Run these commands from the **repo root**:

```bash
# Install the template pack (skip if already installed)
dotnet new install CoreEx.Template

# Single-repo project:
dotnet new coreex-ai

# Monorepo — replace <subfolder> with the path the user gave (e.g. backend):
dotnet new coreex-ai --app-folder <subfolder>
```

This installs:
- `.github/instructions/` — scoped instruction files auto-injected by Copilot for each file type
- `.github/prompts/` — scaffolding prompt
- `.github/agents/coreex-expert.agent.md` — architecture guidance agent
- `.github/skills/coreex-docs-sync/` — skill to cache CoreEx docs locally
- `.claude/commands/` — equivalent commands for Claude Code

### Step 4 — Reload context and continue

After running `dotnet new coreex-ai`, reload the context so the new instruction files are active.
Then proceed with whatever the user originally asked — scaffolding, implementation, review, etc.

For the full step-by-step walkthrough (infrastructure setup, solution scaffolding, first entity
implementation), see [docs/getting-started.md](./docs/getting-started.md).
