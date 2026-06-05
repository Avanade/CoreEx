# Workflow

Use this workflow to guide the user through CoreEx solution shape decisions in plain English, then turn the answers into safe `dotnet new` inputs.

## Principles

- Ask one short question at a time.
- Use simple English, not template parameter names.
- Prefer small fixed choices before freeform text.
- Reuse what the workspace already proves.
- If the user seems unsure, give a recommended default and explain it in one sentence.
- Before running any real command, restate the derived inputs in a compact summary.

## Mandatory Interview Mechanics

- When `mcp_microsoft_git_confirm_options` is available, use it for each interview step.
- Ask exactly one scaffold question per turn.
- Each confirmation card must contain exactly one editable option plus optional readonly context.
- Preselect or prefill a default for the current question.
- Never batch multiple scaffold questions into one assistant message.
- Wait for the user to confirm the current card before moving to the next question.

## Default Selection Policy

Use these defaults when the workspace does not already prove the answer:

| Question | Default |
|---|---|
| Base solution name | Best canonical guess from workspace hints; if only two parts exist, insert `Product` as the temporary middle segment |
| New domain or retrofit | `New domain` for bootstrap-only repos |
| HTTP API | `Yes` |
| Reliable event publishing | `No` |
| Event consumption | `No` |
| Data storage | `No local database` |
| Messaging provider | `Yes` for Azure Service Bus when messaging is required |
| Reference data | `No` |
| Domain layer | `No` |
| Result/ROP style | `No` |

Derive `outbox-enabled` after the interview. Default it to `false` unless the user chose owned persistence and reliable publishing.

## Phase 1: Inspect The Workspace

Decide which path applies.

### Bootstrap-only shell

Treat the repo as bootstrap-only when the workspace mostly contains AI assets, docs, or starter shell content and does not yet contain a populated CoreEx solution for the target domain.

### Existing scaffolded solution

Treat the repo as scaffolded when there is already a real solution shape, such as a populated `src/`, `tests/`, `*.Database`, `*.CodeGen`, or one or more existing hosts.

If the workspace already shows the current provider, naming shape, or enabled capabilities, confirm those choices instead of asking open-ended questions again.

## Phase 2: Run The Interview

Ask the questions in this order. Skip questions that the workspace has already answered with high confidence.

Implementation notes:
- Use one `confirm_options` call per question when the tool is available.
- Include only one editable field in the card for the current question.
- Use readonly fields to show prior confirmed answers when that context helps the user answer quickly.
- If `confirm_options` is unavailable, ask the same question as plain chat text and still ask only one question per message.

### 1. Base solution name

Ask:

> What should the base solution name be? Use `Company.Product.Domain`, for example `Avanade.Product.Books`.

Default:

> Prefill the best canonical guess from workspace hints. If only a two-part name exists, use `Product` as the temporary middle segment and ask the user to correct it if needed.

If the user gives only two parts, ask:

> I still need the domain part. What is the business domain name, for example `Books` or `Orders`?

Rules:

- Do not continue to template commands until the name is in `[Company].[Product].[Domain]` form.
- Do not ask for host-specific names such as `.Api` or `.Subscribe`.

### 2. New domain or retrofit

Ask:

> Are we creating a new CoreEx domain here, or adding missing hosts to an existing one?

Recommended options:

- `New domain`
- `Add missing hosts`

Interpretation:

- `New domain` means the skill may need `coreex` plus one or more host templates.
- `Add missing hosts` means preserve the existing provider and capability choices unless the user explicitly wants to change them.

Default: `New domain` for a bootstrap-only repo.

### 3. HTTP API need

Ask:

> Do you need an HTTP API for this solution?

Recommended options:

- `Yes`
- `No`

Interpretation:

- `Yes` means add `coreex-api` if it does not already exist.

Default: `Yes`.

### 4. Reliable event publishing

Ask:

> Does this solution need to publish events reliably to other systems?

Recommended options:

- `Yes`
- `No`
- `Not sure`

Interpretation:

- `Yes` usually means use a messaging provider and add `coreex-relay`.
- `Not sure` should trigger one brief explanation: if events must be stored with the database change and sent later, the answer is usually `Yes`.

Default: `No`.

### 5. Event consumption

Ask:

> Does this solution need to receive events from other systems?

Recommended options:

- `Yes`
- `No`

Interpretation:

- `Yes` means add `coreex-subscriber`.

Default: `No`.

### 6. Data storage

Ask:

> Will this solution store its own data? If yes, which database do you want to use?

Recommended options:

- `SQL Server`
- `Postgres`
- `No local database`

Interpretation:

- `SQL Server` maps to `--data-provider SqlServer`.
- `Postgres` maps to `--data-provider Postgres`.
- `No local database` maps to `--data-provider None`.

Default: `No local database`.

### 7. Messaging provider

Ask this only if the user needs to publish or consume events.

> For messaging, should I use Azure Service Bus?

Recommended options:

- `Yes`
- `No, I need something else`

Interpretation:

- `Yes` maps to `--messaging-provider ServiceBus`.
- Any other answer should pause command generation until the supported provider choice is clear.

Default: `Yes` for Azure Service Bus.

### 8. Reference data

Ask:

> Do you need reference data code generation, such as managed lists of codes and descriptions?

Recommended options:

- `Yes`
- `No`
- `Not sure`

Interpretation:

- `Yes` maps to `--refdata-enabled true`.
- `No` maps to `--refdata-enabled false`.
- `Not sure` should get one short explanation and then a confirmation question.

Default: `No`.

### 9. Domain layer

Ask:

> Do you want a separate Domain layer for richer business logic?

Recommended options:

- `Yes`
- `No`
- `Not sure`

Interpretation:

- `Yes` maps to `--domain-driven-enabled true`.
- `No` maps to `--domain-driven-enabled false`.

Default: `No`.

### 10. Result/ROP style

Ask:

> Do you want Result or ROP style service pipelines?

Recommended options:

- `Yes`
- `No`
- `Not sure`

Interpretation:

- `Yes` maps to `--rop-enabled true`.
- `No` maps to `--rop-enabled false`.

Default: `No`.

## Phase 3: Translate Answers Into Inputs

Build a structured decision summary before any command is run.

| Interview outcome | Derived input |
|---|---|
| Base solution name confirmed | `-n Company.Product.Domain` |
| New domain | Consider `coreex` first |
| Add missing hosts | Preserve current provider and capability choices |
| HTTP API needed | Add `coreex-api` |
| Publish events reliably | Add `coreex-relay` and a messaging provider |
| Consume events | Add `coreex-subscriber` and a messaging provider |
| SQL Server | `--data-provider SqlServer` |
| Postgres | `--data-provider Postgres` |
| No local database | `--data-provider None` |
| Azure Service Bus | `--messaging-provider ServiceBus` |
| Reference data needed | `--refdata-enabled true` |
| Domain layer needed | `--domain-driven-enabled true` |
| Result/ROP style needed | `--rop-enabled true` |

If the user is retrofitting an existing solution, only include flags that are required for missing projects or that the user explicitly asked to change.

## Phase 4: Confirm Before Generation

Before any real template command, summarize the inputs in plain English. Use a compact confirmation like this:

> Here is the shape I derived: base name `Avanade.Product.Books`, new domain, API yes, relay yes, subscriber no, SQL Server, Service Bus, reference data yes, Domain layer no, ROP no. I will dry-run the matching `dotnet new` commands next.

If anything in that summary is uncertain, ask for confirmation before proceeding.

## Phase 5: Safe Command Path

1. Check the template pack with `dotnet new list --tag CoreEx`.
2. Install `CoreEx.Template` if it is missing.
3. Always run `--dry-run` before the first real template invocation unless the workspace is empty and the command shape is already obvious.
4. Stop if the dry-run shows nested root folders, incorrect host names, or a layout that conflicts with the existing repo.
5. For bootstrap-only repos, run `coreex` first and then add only the needed host templates.
6. For retrofit work, add only the missing hosts unless the user explicitly asked for broader reshaping.

## Phase 6: Final Output

After generation or dry-run review, report:

- what was created or proposed
- which answers were translated into template inputs
- what was intentionally omitted
- any next prerequisite, such as package feeds, CodeGen, or database tooling

## Command Examples
