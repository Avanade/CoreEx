---
name: add-capability
description: "Retrofit an existing CoreEx domain or service with additional capabilities. Use when: adding Outbox.Relay, Subscribe, Azure Service Bus integration, subscriber scaffolding, or aligning messaging and integration wiring for an existing domain."
argument-hint: "Optional: solution, domain, and requested capability — e.g. 'Contoso Products add relay and subscribers'"
tags: ["retrofit", "messaging", "service-bus", "outbox-relay", "subscribers", "integration", "exec-plan"]
---

# Add Capability

Retrofitting an existing domain with messaging and integration support. Work begins with an approval checkpoint after a capability-addition plan is created.

## When to Use

- Add `Outbox.Relay` to publish integration events reliably.
- Add `Subscribe` to consume integration events from other services.
- Add or align Azure Service Bus wiring.
- Add initial subscriber classes and registration.

## When Not to Use

- Creating a new domain from scratch — use `/generate-domain`.
- Bootstrapping a new solution — use the starter bootstrap workflow.
- Non-CoreEx brownfield migrations.

## MVP Assumptions

- Existing CoreEx-style domain shape (Contracts, Application, Infrastructure, Api, Database).
- SQL Server for outbox support.
- Azure Service Bus for publish/subscribe.

If different backends are needed, ask before making changes.

## Workflow Overview

This skill integrates an exec-plan checkpoint before applying changes:

1. **Step 0: Create Plan** — Inspect domain state, clarify intent, and scaffold a capability-addition plan in `.agent/execplans/`.
2. **Approval Checkpoint** — Present the plan to the user for review and approval.
3. **Steps 1–6: Execute** — Apply targeted changes following the approved plan.
4. **Step 7: Validate & Document** — Run validation checklist, update the plan with results.

For detailed step-by-step workflow, see [`references/workflow.md`](references/workflow.md).

## Key References

- Exec-plan template: `/.github/skills/coreex-exec-plan/assets/templates/PLAN.template.md`
- [Host Setup Conventions](/.github/instructions/host-setup.instructions.md)
- [Event Subscriber Conventions](/.github/instructions/event-subscribers.instructions.md)
- [Application Service Conventions](/.github/instructions/application-services.instructions.md)
- [Database Project Conventions](/.github/instructions/database-project.instructions.md)
- Sample hosts: `samples/src/Contoso.Products.Api/Program.cs`, `samples/src/Contoso.Products.Subscribe/Program.cs`, `samples/src/Contoso.Products.Outbox.Relay/Program.cs`
- Active plans index: `.agent/PLANS.md`
