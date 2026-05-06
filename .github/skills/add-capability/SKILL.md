---
name: add-capability
description: "Retrofit an existing CoreEx domain or service with additional capabilities. Use when: adding Outbox.Relay, Subscribe, Azure Service Bus integration, subscriber scaffolding, or aligning messaging and integration wiring for an existing domain."
argument-hint: "Optional: solution, domain, and requested capability — e.g. 'Contoso Products add relay and subscribers'"
tags: ["retrofit", "messaging", "service-bus", "outbox-relay", "subscribers", "integration"]
---

# Add Capability

Retrofitting an existing domain is different from greenfield scaffolding. The goal is to inspect what is already present, determine the safest incremental change, and add only the missing files, packages, host wiring, and tests.

This MVP focuses on **messaging and integration retrofits** for existing CoreEx-style domains.

## When to Use

- Add an `Outbox.Relay` host to an existing API write domain.
- Add a `Subscribe` host to an existing domain.
- Add or align Azure Service Bus publisher and subscriber wiring.
- Add initial subscriber classes, registration, and error handling.
- Add or align outbox publisher and event formatter wiring in an API host that should publish integration events.
- Bring an existing CoreEx domain closer to the Product or Shopping sample messaging architecture.

## When Not to Use

- Creating a new domain from scratch — use `/generate-domain` or `/scaffold-domain-from-templates`.
- Bootstrapping an entirely new solution — use the starter bootstrap workflow.
- Non-CoreEx brownfield migrations.
- Database-provider or broker diversification beyond the repo defaults unless the user explicitly wants to design that divergence.

## MVP Support Boundary

This skill currently assumes:

- an existing CoreEx-style domain shape is already present, typically with `Contracts`, `Application`, `Infrastructure`, `Api`, and often `Database`.
- SQL Server is the current or intended initial persistence implementation for outbox support.
- Azure Service Bus is the current or intended initial broker implementation for publish/subscribe wiring.

Treat SQL Server and Azure Service Bus as the **default initial retrofit targets** because they have the strongest repo support. If the user wants a different backend or broker, ask before making changes.

## Pre-flight: Load Context

Before making changes, load and use all of the following:

1. Relevant instruction files in `/.github/instructions/`, especially:
   - `host-setup.instructions.md`
   - `event-subscribers.instructions.md`
   - `application-services.instructions.md`
   - `database-project.instructions.md` when relay or outbox database work is required
2. Sample host wiring from:
   - `samples/src/Contoso.Products.Api/Program.cs`
   - `samples/src/Contoso.Products.Subscribe/Program.cs`
   - `samples/src/Contoso.Products.Outbox.Relay/Program.cs`
   - equivalent Shopping or Orders hosts when helpful
3. Domain templates under `/.github/templates/domain/**` when you need canonical outbox/database/project patterns.
4. The reference documents:
   - `references/messaging-retrofit-checkpoints.md`
   - `references/messaging-retrofit-checklist.md`

## Step 1 — Inspect the Current Domain State

Determine the actual current shape before proposing or applying a retrofit.

Inspect for:

1. Domain boundary and project names.
2. Existing hosts:
   - `*.Api`
   - `*.Outbox.Relay`
   - `*.Subscribe`
3. Existing database support:
   - `*.Database` project
   - outbox tables and stored procedures
   - SQL Server/outbox package references
4. Existing messaging support:
   - `CoreEx.Events`
   - `CoreEx.Azure.Messaging.ServiceBus`
   - `AddEventFormatter`
   - `AddSqlServerOutboxPublisher`
   - `AddSubscribedManager`
   - `AzureServiceBusReceiving`
5. Existing telemetry and health wiring.
6. Existing integration-event semantics:
   - event subjects
   - subscriber classes
   - related application service methods

Use conservative detection. If the solution is ambiguous, ask instead of guessing.

## Step 2 — Clarify Only What Cannot Be Inferred

Ask only when required to avoid an incorrect retrofit.

Typical clarification questions:

- Which domain should be retrofitted?
- Which capability should be added: relay, subscribe, subscriber classes, or a combined messaging retrofit?
- Should the skill reuse SQL Server and Azure Service Bus as the initial implementation defaults?
- If adding subscribers, what subjects and payload contracts should be used?
- Should the retrofit only add infrastructure/host wiring, or also add initial application-facing subscriber handlers?

## Step 3 — Choose the Retrofit Mode

Choose one of these modes based on detected state plus user intent.

### Mode A — Add `Outbox.Relay`

Use when:

- the domain already writes business data and should publish integration events reliably.
- the API host either already has or should gain outbox publisher/event formatter wiring.

Expected work:

- create `*.Outbox.Relay` project if missing.
- add required packages and project references.
- add relay `Program.cs` wiring using host setup conventions.
- ensure the database project contains outbox tables and required stored procedures.
- ensure the API host uses event formatter and outbox publisher wiring if the domain is expected to publish.

### Mode B — Add `Subscribe`

Use when:

- the domain must consume integration events or commands from other services.

Expected work:

- create `*.Subscribe` project if missing.
- add Service Bus client and receiver wiring.
- add hosted service manager and hosted service mapping.
- add subscriber classes and registration.
- reuse reference data, cache, infrastructure, and telemetry patterns where appropriate.

### Mode C — Add Both Relay and Subscribe

Use when:

- the service should both publish its own integration events and consume events from other services.

### Mode D — Add Subscribers to an Existing Subscribe Host

Use when:

- the host exists but subscriber classes, registration, or error handling are missing or incomplete.

## Step 4 — Apply Incremental Changes

Prefer targeted edits over regeneration.

Rules:

1. Reuse existing project naming and layering.
2. Do not duplicate wiring that already exists.
3. Keep subscriber logic thin; delegate to Application services.
4. Preserve host middleware order and telemetry conventions.
5. Reuse domain templates only for the missing pieces being introduced.
6. If the domain shape is too inconsistent for safe retrofit, stop and explain what blocks automation.

## Step 5 — Validate Before Finishing

Run the `references/messaging-retrofit-checklist.md` completion gate.

Minimum completion criteria:

- affected `Program.cs` files follow the applicable host setup conventions.
- required package and project references are present.
- relay-specific outbox database assets exist when relay support is added.
- subscribers are registered and use the expected `SubscribedBase` patterns.
- new or changed files fit existing naming and layering conventions.
- diagnostics/build for affected projects are clean.

## Supported MVP Outcomes

- API domain retrofitted to support reliable integration-event publishing with relay.
- Existing domain retrofitted with a subscribe host and initial subscriber scaffolding.
- Existing messaging host aligned to the repo's current conventions.

## Completion Gate

Do not finish until:

1. The requested messaging capability is either fully applied or clearly blocked.
2. The changed hosts and projects reflect the repo conventions.
3. Any user decisions still required are listed explicitly.
