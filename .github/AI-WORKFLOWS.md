# AI Workflow Set

This folder contains the AI artefacts that give GitHub Copilot and Claude Code authoritative knowledge of CoreEx patterns, conventions, and architecture. They can be used directly in the CoreEx repository or copied into a consuming project.

## What's here

| Artefact | Path | Purpose |
|----------|------|---------|
| Global instructions | `copilot-instructions.md` | Auto-injected project-wide context: repo shape, conventions, house rules, generated-file ownership. Applied to every chat interaction automatically. |
| Area instructions | `instructions/*.instructions.md` | Scoped context injected automatically when editing a matching file type (contracts, services, repositories, controllers, tests, etc.). |
| Agent | `agents/coreex-expert.agent.md` | Dedicated expert for CoreEx architecture and pattern guidance — explains conventions, reviews designs, and routes to the right command. |
| Prompts | `prompts/*.prompt.md` | Deterministic, file-driven commands invoked with `/` in chat. |
| Skills | `skills/*/SKILL.md` | Reasoning-based commands for open-ended tasks. Invoked with `/` in Claude Code; attach the `SKILL.md` via `#file:` in Copilot. |
| Authoring guides | `INSTRUCTION_AUTHORING.md`, `SKILL_AUTHORING.md` | Standards for writing new instruction files and skills. |

## Agent

**`coreex-expert`** — invoke when you need to explain a CoreEx concept, choose between patterns, review a design, or get architecture guidance aligned to the sample implementations.

- Claude Code: `@coreex-expert`
- Copilot Chat: switch to **Agent** mode and select **CoreEx Expert**

The agent uses a local doc cache (populated by `/coreex-docs-sync`) to avoid live GitHub fetches on every question. It covers all 16 CoreEx packages, distinguishing those already in the project from ones the project could adopt. See the [agent README](./agents/README.md) for the resolution flowchart, cache structure, and adoption guide.

## Instructions

Instructions are passive — no action is needed to activate them. The global file applies to every session; area files are injected automatically based on what you are editing.

| File | Injected when editing |
|------|-----------------------|
| `coreex-conventions.instructions.md` | All `.cs` files — naming, nullability, expression bodies, `ConfigureAwait`, house rules |
| `coreex-contracts.instructions.md` | Contract files — `[Contract]`, `[ReferenceData]`, source generation |
| `coreex-application-services.instructions.md` | Application services — `TransactionAsync`, validation, event enqueuing |
| `coreex-validators.instructions.md` | Validator files — `Validator<T,TSelf>`, rule chains |
| `coreex-repositories.instructions.md` | Repository files — `EfDbModel`, mappers, `QueryArgsConfig`, paging |
| `coreex-api-controllers.instructions.md` | Controller files — `WebApi` helpers, `[IdempotencyKey]`, PATCH |
| `coreex-event-subscribers.instructions.md` | Subscriber files — `[Subscribe]`, `SubscribedManager`, error handling |
| `coreex-host-setup.instructions.md` | `Program.cs` files — middleware order, service registration, outbox relay |
| `coreex-tooling.instructions.md` | CodeGen and Database projects — `ref-data.yaml`, DbEx, generated-file ownership |
| `coreex-tests.instructions.md` | Test files — UnitTestEx, NUnit, AwesomeAssertions, outbox/event assertions |
| `coreex-domain.instructions.md` | Domain files — aggregates, mutation guards, `Result<T>` pipelines |

## Prompts, Skills, and Templates

### Skills vs Prompts vs Instructions

Three artefact types cooperate, each with a distinct job:

- **Instruction** (`instructions/*.instructions.md`) — invariant rules that are *auto-injected* whenever an edited file matches the instruction's `applyTo` glob. You never invoke them; they passively shape every edit to a matching file.
- **Skill** (`skills/coreex-*/`) — a reasoning *workflow* you invoke explicitly (`/coreex-<x>` in Claude Code) to create or modify something. Each skill owns a `SKILL.md` and a `references/workflow.md` that drive the interaction.
- **Prompt** (`prompts/coreex-*.prompt.md`) — the GitHub Copilot entry point that *delegates to the matching skill's workflow*. Skills and prompts map 1:1 by name, so `/coreex-contract` (Claude Code skill) and `coreex-contract.prompt.md` (Copilot prompt) run the same workflow.

**Feature Configuration.** A CoreEx solution's project-wide choices — `data-provider`, `refdata-enabled`, `domain-driven-enabled`, `rop-enabled`, `outbox-enabled`, `messaging-provider` — are persisted in the solution-root `AGENTS.md` **Feature Configuration** block. Skills read that block before asking anything, so recorded decisions are not re-prompted from one skill to the next.

### Command catalog

| Command | Type | What it does |
|---------|------|-------------|
| [`CoreEx.Template`](../src/CoreEx.Template/README.md) | Template pack | Deterministic `dotnet new` scaffolding for a CoreEx solution plus API, relay, and subscriber hosts. Use `dotnet new install CoreEx.Template` and then the `coreex*` templates in a terminal. `dotnet new coreex-ai` installs this full AI workflow set (instructions, prompts, skills, the `coreex-expert` agent, `.claude/commands/`, and the `.github/docs/coreex/` docs cache) into a consuming project. |
| [`/acquire-codebase-knowledge`](./skills/acquire-codebase-knowledge/README.md) | Skill | Maps an unfamiliar codebase and produces seven structured onboarding documents. |
| [`/coreex-scaffold`](./skills/solution-scaffolder/README.md) · [prompt](./prompts/coreex-scaffold.prompt.md) | Skill + prompt | Guides greenfield solution scaffolding, chooses the smallest safe CoreEx.Template shape, and runs the matching `dotnet new coreex*` commands. |
| [`/coreex-docs-sync`](./skills/coreex-docs-sync/README.md) | Skill | Fetches and caches CoreEx architecture docs and all per-package AI guides locally under `.github/docs/coreex/`. |
| [`/aspire`](./skills/aspire/README.md) | Skill | Orchestrates Aspire distributed apps locally: start, stop, logs, debug. |

#### Per-capability skills (L1)

Fourteen skills add or modify a single CoreEx capability on an existing solution. Each is invoked as `/coreex-<name>` in Claude Code, or via the matching [`prompts/coreex-<name>.prompt.md`](./prompts/) in Copilot (1:1 by name). Every skill reads the solution-root `AGENTS.md` **Feature Configuration** first to avoid redundant questioning.

| Skill / prompt | Capability |
|----------------|-----------|
| [`coreex-contract`](./skills/coreex-contract/) | Hand-authored contract (DTO/entity) — root, subordinate, request/response, base class |
| [`coreex-refdata`](./skills/coreex-refdata/) | Reference data type + `ref-data.yaml` entry |
| [`coreex-db-migration`](./skills/coreex-db-migration/) | Database table / DbEx migration |
| [`coreex-repository`](./skills/coreex-repository/) | EF Core repository, mapper, and query configuration |
| [`coreex-adapter`](./skills/coreex-adapter/) | External-integration adapter |
| [`coreex-app-service`](./skills/coreex-app-service/) | Application service orchestration |
| [`coreex-validator`](./skills/coreex-validator/) | Fluent `Validator<T,TSelf>` |
| [`coreex-policy`](./skills/coreex-policy/) | Authorization / business policy |
| [`coreex-aggregate`](./skills/coreex-aggregate/) | DDD aggregate / domain entity |
| [`coreex-api`](./skills/coreex-api/) | API controller / endpoint |
| [`coreex-subscriber`](./skills/coreex-subscriber/) | Event subscriber |
| [`coreex-test-api`](./skills/coreex-test-api/) | API tests |
| [`coreex-test-subscribe`](./skills/coreex-test-subscribe/) | Subscriber tests |
| [`coreex-test-relay`](./skills/coreex-test-relay/) | Outbox relay tests |
