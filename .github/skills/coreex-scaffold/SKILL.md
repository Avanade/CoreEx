---
name: coreex-scaffold
description: "Guide a developer through CoreEx solution shaping after bootstrap, using a short plain-English interview that turns user answers into safe dotnet new template inputs. USE FOR: bootstrap-only repos, deciding API-only vs API plus relay vs API plus subscriber, choosing SQL Server vs Postgres vs no database, choosing refdata/outbox/DDD/ROP options, installing CoreEx.Template, checking current solution shape, and adding missing Api/Outbox.Relay/Subscribe hosts to an existing repo. DO NOT USE FOR: unrelated runtime debugging, bootstrap creation, or forcing root re-scaffolding over an existing solution. INVOKES: workspace inspection, ask-questions style interviews, dotnet new install/list, dry-run validation, and either template generation or manual retrofit work depending on repo shape."
argument-hint: "Optional: base solution name, whether this is new or retrofit, required hosts, database choice, and messaging needs."
tags: ["coreex", "scaffolding", "retrofit", "template", "hosts"]
---

# CoreEx Scaffold

Guides a repository through the right CoreEx setup path by interviewing the user in simple English and translating the answers into safe CoreEx template commands.

## When to Use

- Starting a new CoreEx domain or service from a repository that already contains only the bootstrap AI assets.
- Deciding whether the first cut should create just the API host or also include the outbox relay and/or subscriber hosts.
- Adding missing `Api`, `Outbox.Relay`, or `Subscribe` hosts to an existing scaffolded CoreEx solution.
- Converting plain-English project needs into either the matching `dotnet new coreex*` commands or the safest retrofit path.

## When Not to Use

- You are debugging local runtime, container, Aspire, or package restore issues unrelated to project shaping.
- You still need to create the initial bootstrap repository; run `coreex-bootstrap` before this skill is used.
- You want architectural guidance for an existing implementation beyond project setup; use `CoreEx Expert` instead.

## Workflow Overview

1. **Inspect the workspace shape first.**
2. **Interview the user in simple English.** Ask one short question at a time and turn the answers into template inputs.
3. **Validate the naming shape before any template command.**
4. **Choose the safest implementation path.** Prefer dry-runs and stop on layout mismatches.
5. **Apply the right shape.** Generate only what is missing or explicitly requested.
6. **Summarize the result.** Show the derived inputs, commands, and any deferred steps.

For step-by-step guidance, see [references/workflow.md](references/workflow.md).

## Interactive Interview Rules

- Ask short, plain-English questions. Do not ask the user to supply template flag names.
- Prefer either/or or small-choice questions before freeform questions.
- Ask one topic at a time: name, new vs retrofit, HTTP API, event publishing, event consumption, data storage, reference data, domain layer, and ROP.
- If the user gives a partial or non-canonical name, stop and help them reach `[Company].[Product].[Domain]` before any template command.
- If the workspace already proves a choice, confirm it instead of asking again.
- Before any real `dotnet new` command, restate the derived inputs in one compact summary and ask for confirmation when there is any ambiguity.

## Quick Reference

| User answer | Template impact |
|---|---|
| "I need HTTP endpoints." | Add `coreex-api`. |
| "I need to publish events reliably." | Add `coreex-relay`; use a messaging provider and keep outbox enabled where relevant. |
| "I need to consume events." | Add `coreex-subscriber`; use a messaging provider. |
| "This service stores its own data in SQL Server." | `--data-provider SqlServer` |
| "This service stores its own data in Postgres." | `--data-provider Postgres` |
| "This is a facade. No local database." | `--data-provider None` |
| "I need reference data." | `--refdata-enabled true` |
| "I want a Domain layer." | `--domain-driven-enabled true` |
| "I want Result/ROP style pipelines." | `--rop-enabled true` |

## Prerequisite

Assume the repository is already in one of these states before this skill runs:
- a bootstrap-only shell created earlier by `coreex-bootstrap`; or
- an existing CoreEx solution that is missing some runtime hosts.

## Naming Rules

- All of `coreex`, `coreex-api`, `coreex-relay`, `coreex-subscriber` require the canonical format: `[Company].[Product].[Domain]`.
- `coreex -n Company.Product.Domain` establishes the canonical base solution name.
- `coreex-api -n Company.Product.Domain` generates the API host and its tests from that same base name.
- `coreex-relay -n Company.Product.Domain` generates the outbox relay host and its tests from that same base name.
- `coreex-subscriber -n Company.Product.Domain` generates the subscriber host and its tests from that same base name.
- Do not substitute the host project name into `-n`; the templates already append or derive the host-specific suffixes.
- If the user gives only a two-part name for domain templates, ask for the missing segment before running the templates.
- If the repository already exists with a non-canonical root name (e.g., `Avanade.Books`), prefer a manual retrofit unless the user explicitly wants to rename the solution first.

## Command Patterns

### Bootstrap-Only Repository

```text
# Run the core domain template first, then add only the required hosts.
dotnet new coreex -n Avanade.Product.Books --data-provider SqlServer --messaging-provider ServiceBus --refdata-enabled true --outbox-enabled true --domain-driven-enabled false --rop-enabled false -o .

# Add host templates in sequence (only those needed)
dotnet new coreex-api -n Avanade.Product.Books -o . --data-provider SqlServer --refdata-enabled true --outbox-enabled true
dotnet new coreex-relay -n Avanade.Product.Books -o . --data-provider SqlServer --messaging-provider ServiceBus
dotnet new coreex-subscriber -n Avanade.Product.Books -o . --messaging-provider ServiceBus
```

### Existing Repository Retrofit (Skip Bootstrap)

```text
# If the repo already has a bootstrap or partial scaffold, run only the missing domain/host templates
# First confirm the canonical three-part base name, then run coreex and/or host templates
dotnet new coreex -n Company.Product.Domain ...
dotnet new coreex-api -n Company.Product.Domain ...
# etc.
```

## Existing Repository Guardrails

- **Bootstrap is a prereq:** This skill assumes any bootstrap creation has already happened before the workflow starts.
- **For retrofit work:** If the repo already contains a solution, `src/`, tooling, or tests, do not re-run the root scaffold unless the current shape is still only the bootstrap shell.
- **Pre-flight validation:** Always run a dry-run with the intended base solution name before any real domain/host template invocation.
- **Watch for nested roots:** If dry-run output shows paths like `src\\Company.Product.Domain.Api\\src\\Company.Product.Domain.Api\\...`, the output root is wrong; stop and change strategy.
- **Naming mismatch:** If dry-run output shows incorrect project names because the repo uses a non-canonical root name, stop and recommend either renaming the solution first or manually creating the missing hosts.
- **No force:** Do not use `--force` to push through a mismatched layout. The mismatch itself indicates an unsafe operation.

## Shape Selection Guide

| Need | Recommendation |
|---|---|
| Empty repo, own data, expose HTTP only | `coreex` + `coreex-api` |
| Empty repo, own data, expose HTTP, publish reliable events | `coreex` + `coreex-api` + `coreex-relay` |
| Empty repo, expose HTTP, publish events, consume events | `coreex` + `coreex-api` + `coreex-relay` + `coreex-subscriber` |
| Existing repo missing only runtime hosts | Infer current shape, then add only the missing hosts |
| Facade over another system with no local database | `coreex --data-provider None --messaging-provider None` and add `coreex-api --data-provider None` only if an API is needed |

## Key References

- `../../docs/coreex/application-scaffolding-guide.md`.
- `../../docs/coreex/layers.md`.
- `../../docs/coreex/patterns.md`.
- `https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Template/README.md`.
