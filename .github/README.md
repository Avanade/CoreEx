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
| Domain templates | `templates/domain/` | 77 ready-made source-file templates covering all domain layers and both database engines. See the [templates README](./templates/domain/README.md). |
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

| Command | Type | What it does |
|---------|------|-------------|
| [`CoreEx.Template`](../src/CoreEx.Template/README.md) | Template pack | Deterministic `dotnet new` scaffolding for a CoreEx solution plus API, relay, and subscriber hosts. Use `dotnet new install CoreEx.Template` and then the `coreex*` templates in a terminal. |
| [`/acquire-codebase-knowledge`](./skills/acquire-codebase-knowledge/README.md) | Skill | Maps an unfamiliar codebase and produces seven structured onboarding documents. |
| [`/coreex-scaffold`](./skills/coreex-scaffold/README.md) | Skill-backed prompt | Guides greenfield solution scaffolding, chooses the smallest safe CoreEx.Template shape, and runs the matching `dotnet new coreex*` commands. |
| [`/coreex-docs-sync`](./skills/coreex-docs-sync/README.md) | Skill | Fetches and caches CoreEx architecture docs and all per-package AI guides locally under `.github/docs/coreex/`. |
| [`/aspire`](./skills/aspire/README.md) | Skill | Orchestrates Aspire distributed apps locally: start, stop, logs, debug. |
| `/init` | Prompt | Initializes a new CoreEx solution or workspace. |
| `/setup` | Prompt | Configures an existing CoreEx solution with standard tooling and settings. |

## Domain templates

See [templates/domain/README.md](./templates/domain/README.md) for the full option set, directory layout, and invocation instructions for both Claude Code and GitHub Copilot.
