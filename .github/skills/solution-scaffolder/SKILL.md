---
name: solution-scaffolder
description: "Guide a developer through CoreEx solution shaping after bootstrap, using a short plain-English interview that turns user answers into safe dotnet new template inputs. USE FOR: bootstrap-only repos, deciding API-only vs API plus relay vs API plus subscriber, choosing SQL Server vs Postgres vs no database, choosing refdata/outbox/DDD/ROP options, installing CoreEx.Template, checking current solution shape, and adding missing Api/Outbox.Relay/Subscribe hosts to an existing repo. DO NOT USE FOR: unrelated runtime debugging, bootstrap creation, or forcing root re-scaffolding over an existing solution. INVOKES: workspace inspection, ask-questions style interviews, dotnet new install/list, dry-run validation, solution wiring, focused build/test validation, and either template generation or manual retrofit work depending on repo shape."
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
6. **Validate the scaffold.** Wire projects into the solution, build, and run the narrowest safe tests.
7. **Summarize the result.** Show the derived inputs, commands, validations, and any deferred steps.

For step-by-step guidance, see [references/workflow.md](references/workflow.md).

## Interactive Interview Rules

- When `mcp_microsoft_git_confirm_options` is available, use it for every interview step.
- Each interview turn must contain exactly one editable field plus optional readonly context fields.
- Ask short, plain-English questions. Do not ask the user to supply template flag names.
- The canonical interview order is: base solution name, new vs retrofit, HTTP API, reliable event publishing, event consumption, data storage, messaging provider when needed, reference data, domain layer, and ROP.
- Use a single `text` field only for the base solution name.
- Use a single `select` field for every other interview step so the workflow stays multiple-choice and deterministic.
- For yes or no questions, use a `select` with `Yes` and `No` so the default remains visibly preselected.
- Prefer either/or or small-choice questions before any extra freeform follow-up.
- Do not batch multiple scaffold questions into a single assistant message.
- If the user gives a partial or non-canonical name, stop and help them reach `[Company].[Product].[Domain]` before any template command.
- If the workspace already proves a choice, confirm it instead of asking again.
- Before any real `dotnet new` command, restate the derived inputs in one compact summary and ask for confirmation when there is any ambiguity.

## Default Selection Policy

When the workspace does not already prove a value, preselect the safest default that still keeps the workflow moving:

| Question | Default |
|---|---|
| Base solution name | The workspace root folder name if it is already in `[Company].[Product].[Domain]` form; otherwise the best canonical guess from workspace hints. If only two parts exist, use `Product` as a temporary middle segment. The user can override during the interview. |
| New domain or retrofit | `New domain` in a bootstrap-only repo |
| HTTP API | `Yes` |
| Reliable event publishing | `No` |
| Event consumption | `No` |
| Data storage | `No local database` |
| Messaging provider | `Yes` for Azure Service Bus when messaging is needed |
| Reference data | `No` |
| Domain layer | `No` |
| Result/ROP style | `No` |

Set outbox to `true` only when the user chose owned persistence and reliable publishing. Otherwise keep it `false`.

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

- The base solution name must be in `[Company].[Product].[Domain]` form, e.g. `Avanade.Bookstore.Books`.
- The `coreex` (solution) template takes the **3-part base name**: `-n Company.Product.Domain`. If the current directory is already named `Company.Product.Domain`, `-n` can be omitted — the template uses the folder name automatically.
- Each **host template** takes a **4-part name** with the host suffix appended:
  - `coreex-api -n Company.Product.Domain.Api`
  - `coreex-relay -n Company.Product.Domain.Relay`
  - `coreex-subscriber -n Company.Product.Domain.Subscriber`
- Host templates **always require `-n`** — the folder name is the 3-part base and cannot supply the host suffix automatically.
- Do not pass the 3-part base name to host templates; doing so causes all three hosts to emit into the same `src/Company.Product.Domain/` directory, overwriting each other.
- If the user gives only a two-part name, ask for the missing segment before running any template.
- If the repository already exists with a non-canonical root name (e.g., `Avanade.Books`), prefer a manual retrofit unless the user explicitly wants to rename the solution first.

## Command Patterns

### Bootstrap-Only Repository

```text
# Step 1: Run the solution template. If the folder is named Avanade.Product.Books, -n can be omitted.
dotnet new coreex -n Avanade.Product.Books --data-provider SqlServer --messaging-provider ServiceBus --refdata-enabled true --outbox-enabled true --domain-driven-enabled false --rop-enabled false

# Step 2: Add each host template using the 4-part name (base + host suffix).
dotnet new coreex-api        -n Avanade.Product.Books.Api        --data-provider SqlServer --refdata-enabled true --outbox-enabled true
dotnet new coreex-relay      -n Avanade.Product.Books.Relay      --data-provider SqlServer --messaging-provider ServiceBus
dotnet new coreex-subscriber -n Avanade.Product.Books.Subscriber --data-provider SqlServer --messaging-provider ServiceBus --refdata-enabled true

# Step 3: Add host and test projects to the solution file.
dotnet sln Avanade.Product.Books.slnx add src/Avanade.Product.Books.Api
dotnet sln Avanade.Product.Books.slnx add tests/Avanade.Product.Books.Test.Api
dotnet sln Avanade.Product.Books.slnx add src/Avanade.Product.Books.Relay
dotnet sln Avanade.Product.Books.slnx add tests/Avanade.Product.Books.Test.Outbox.Relay
dotnet sln Avanade.Product.Books.slnx add src/Avanade.Product.Books.Subscriber
dotnet sln Avanade.Product.Books.slnx add tests/Avanade.Product.Books.Test.Subscribe

# Step 4: Validate the generated solution.
dotnet build Avanade.Product.Books.slnx
dotnet test tests/Avanade.Product.Books.Test.Unit
# Run broader test projects only when their local infrastructure dependencies are available.
```

### Existing Repository Retrofit (Skip Bootstrap)

```text
# If the repo already has a bootstrap or partial scaffold, run only the missing domain/host templates.
# Confirm the canonical three-part base name, then run coreex and/or host templates with the correct suffixes.
dotnet new coreex            -n Company.Product.Domain ...
dotnet new coreex-api        -n Company.Product.Domain.Api ...
dotnet new coreex-relay      -n Company.Product.Domain.Relay ...
dotnet new coreex-subscriber -n Company.Product.Domain.Subscriber ...
# Then add new projects to the solution file as in Step 3 above.
```

## Existing Repository Guardrails

- **Bootstrap is a prereq:** This skill assumes any bootstrap creation has already happened before the workflow starts.
- **For retrofit work:** If the repo already contains a solution, `src/`, tooling, or tests, do not re-run the root scaffold unless the current shape is still only the bootstrap shell.
- **Template identity conflicts:** If `dotnet new list` or template execution reports duplicate CoreEx template identities, warn the user which template source is being selected before continuing.
- **Pre-flight validation:** Always run a dry-run with the intended base solution name before any real domain/host template invocation.
- **Watch for nested roots:** If dry-run output shows paths like `src\\Company.Product.Domain.Api\\src\\Company.Product.Domain.Api\\...`, the output root is wrong; stop and change strategy.
- **Naming mismatch:** If dry-run output shows incorrect project names because the repo uses a non-canonical root name, stop and recommend either renaming the solution first or manually creating the missing hosts.
- **Force only for bootstrap replacement:** In a confirmed bootstrap-only repo, `--force` is acceptable only after dry-run validation shows the expected canonical layout.
- **No force for mismatches:** Do not use `--force` to push through a naming or layout mismatch. The mismatch itself indicates an unsafe operation.

## Validation Rules

- After generation, verify that every expected host and test project exists on disk and has been added to the `.slnx`.
- Always run `dotnet build` on the solution as the minimum end-to-end validation step.
- Always run the unit test project when present.
- Run API, relay, or subscriber test projects only when their required local dependencies are available; otherwise skip them and report the reason explicitly.
- Treat missing local infrastructure such as SQL Server, Postgres, Redis, or Service Bus as a validation skip, not as a scaffolding failure, unless the user explicitly asked for full local environment setup.
- If `refdata-enabled` is `true`, call out the `*.CodeGen` tool as the next step or run it only when the user wants a fully generated local state.
- If `data-provider != None`, call out the `*.Database` tool as the next step or run it only when the user wants local schema setup as part of scaffolding.

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


