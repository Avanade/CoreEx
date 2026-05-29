---
name: add-capability
description: "Retrofit an existing CoreEx domain or service with additional capabilities. Use when: adding Outbox.Relay, Subscribe, Azure Service Bus integration, subscriber scaffolding, or aligning messaging and integration wiring for an existing domain."
argument-hint: "Optional: solution, domain, and requested capability — e.g. 'Contoso Products add relay and subscribers'"
tags: ["retrofit", "messaging", "service-bus", "outbox-relay", "subscribers", "integration"]
---

# Add Capability

Retrofitting an existing domain with messaging and integration support. Choose only the missing pieces.

## When to Use

- Add `Outbox.Relay` to publish integration events reliably via the transactional outbox pattern.
- Add `Subscribe` to consume integration events from other services.
- Add or align Azure Service Bus wiring.
- Add initial subscriber classes and registration.

## When Not to Use

- Creating a new domain from scratch — use `/generate-domain` or `/scaffold-domain-from-templates`.
- Bootstrapping a new solution — use the starter bootstrap workflow.
- Non-CoreEx brownfield migrations.

## Assumptions

- Existing CoreEx-style domain shape (Contracts, Application, Infrastructure, Api, Database).
- **Database engine**: SQL Server (default) or PostgreSQL — ask if not determinable from the existing codebase.
- Azure Service Bus for publish/subscribe.

If different backends are needed, ask before making changes.

## Workflow

1. **Load context**: Read host-setup, event-subscribers, application-services, tooling instructions + sample hosts for both SQL Server (Shopping) and PostgreSQL (Products).
2. **Inspect domain state**: Detect existing hosts, database engine, outbox wiring, messaging packages, event subjects.
3. **Clarify**: Ask only what cannot be inferred — which domain, which capability, DB engine if ambiguous, topics/payloads if needed, reference data presence.
4. **Choose mode**: A (relay), B (subscribe), C (both), or D (subscribers only).
5. **Apply changes**: Targeted edits only — reuse patterns, don't regenerate.
6. **Validate**: Run checklist, confirm clean build.

For detailed step-by-step workflow, see [`references/workflow.md`](references/workflow.md).

## Key References

- [Host Setup Conventions](/.github/instructions/coreex-host-setup.instructions.md)
- [Event Subscriber Conventions](/.github/instructions/coreex-event-subscribers.instructions.md)
- [Application Service Conventions](/.github/instructions/coreex-application-services.instructions.md)
- [Developer Tooling Conventions](/.github/instructions/coreex-tooling.instructions.md)
- SQL Server sample hosts: `samples/src/Contoso.Shopping.Api/Program.cs`, `samples/src/Contoso.Shopping.Subscribe/Program.cs`, `samples/src/Contoso.Shopping.Outbox.Relay/Program.cs`
- PostgreSQL sample hosts: `samples/src/Contoso.Products.Api/Program.cs`, `samples/src/Contoso.Products.Subscribe/Program.cs`, `samples/src/Contoso.Products.Outbox.Relay/Program.cs`
- Domain templates: `/.github/templates/domain/`
