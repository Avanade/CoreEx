# domain-name Subscriber Host -- AI Agent Guide

This is the **Message Subscriber host** for the `domain-name` domain, part of the `solution-name` microservice. It receives messages from the configured message broker and dispatches them to subscriber implementations.

> **Before answering any CoreEx question:** check whether `.github/docs/coreex/` is populated at the solution root. If empty, run `/coreex-docs-sync` first. The package guide at `.github/docs/coreex/agents/CoreEx.Azure.Messaging.ServiceBus.md` is especially relevant to this project.

---

## Host Responsibilities

<!-- #if implement-servicebus -->
This is a **background worker host** -- it runs a Service Bus session receiver as a hosted service and delegates message handling to typed subscriber classes. It should contain:

- `Program.cs` -- startup, DI registration, subscriber wiring
- `Subscribers/` -- `ISubscriber<TEvent>` implementations (one per message type)
- No business logic; business processing belongs in `solution-name.Application`
<!-- #else -->
This is a **background worker host** with no messaging provider configured. It should contain:

- `Program.cs` -- startup and DI registration
- No business logic; business processing belongs in `solution-name.Application`
- Add a messaging provider (`CoreEx.Azure.Messaging.ServiceBus`) to enable message receiving
<!-- #endif -->

Read `.github/docs/coreex/hosts-layer.md` for the full host-layer guide.

---

<!-- #if implement-servicebus -->
## Adding a New Subscriber

1. Create a class in `Subscribers/`, e.g. `ProductCreatedSubscriber.cs`
2. Implement `SubscriberBase<TEvent>` or `ISubscriber<TEvent>` from `CoreEx.Events`
3. Register it via `AddSubscribedManager()` in `Program.cs` (the template wires this up via `AddSubscribersUsing<T>()`)
4. Decorate with `[EventSubscriber("EventType", "EventSource")]` to declare which messages it handles
5. Call the Application service from inside `ReceiveAsync()` -- keep subscriber classes thin

Consult `.github/docs/coreex/agents/CoreEx.Azure.Messaging.ServiceBus.md` for subscriber patterns and session receiver configuration.

---

## Session Receiver Configuration

The subscriber uses Azure Service Bus **session-enabled topics** for ordered, partitioned message delivery:

```json
// appsettings.Development.json
"Aspire": {
  "Azure": {
    "Messaging": {
      "ServiceBus": {
        "QueueOrTopicName": "domain-parent-lower",
        "SubscriptionName": "domain-name-lower"
      }
    }
  }
}
```

`MaxConcurrentSessions` (default: 4) controls parallelism -- each session processes messages sequentially, preventing ordering violations.

<!-- #endif -->

---

## Caching

The subscriber wires FusionCache with both in-memory (L1) and Redis distributed (L2) backing -- consistent with the API host -- so reference data and idempotency checks are shared across instances.

---

## This Host's Feature Configuration

<!-- #if implement-servicebus -->
- **Messaging:** Azure Service Bus -- session receiver with `SubscribedManager` dispatch
<!-- #else -->
- **Messaging:** None configured -- add a messaging provider to enable message receiving
<!-- #endif -->
<!-- #if implement-sqlserver -->
- **Database:** SQL Server -- used for outbox publishing when subscribers need to emit their own events
<!-- #elif implement-postgres -->
- **Database:** PostgreSQL -- used for outbox publishing when subscribers need to emit their own events
<!-- #else -->
- **Database:** None -- no database configured; subscribers do not persist data directly
<!-- #endif -->
<!-- #if refdata-enabled -->
- **Reference data:** Enabled -- `ReferenceDataOrchestrator<ReferenceDataService>` is registered; reference data is available in subscriber logic
<!-- #else -->
- **Reference data:** Disabled
<!-- #endif -->

---

## Key Packages

| Package | Purpose |
|---|---|
<!-- #if implement-servicebus -->
| `CoreEx.Azure.Messaging.ServiceBus` | Service Bus session receiver, `SubscribedManager` |
<!-- #endif -->
| `CoreEx.Events` | `ISubscriber<T>`, `SubscriberBase<T>`, `EventSubscriberAttribute` |
| `CoreEx.Caching.FusionCache` | FusionCache `IHybridCache` integration |
<!-- #if implement-sqlserver -->
| `CoreEx.Database.SqlServer` | SQL Server outbox for outbound events |
<!-- #endif -->
<!-- #if implement-postgres -->
| `CoreEx.Database.Postgres` | PostgreSQL outbox for outbound events |
<!-- #endif -->
<!-- #if !implement-none-data -->
| `CoreEx.EntityFrameworkCore` | EF Core integration (`EfDb`, `IEfDbContext`) |
<!-- #endif -->
<!-- #if refdata-enabled -->
| `CoreEx.RefData` | Reference data orchestration |
<!-- #endif -->

---

## Relevant Docs

- `.github/docs/coreex/hosts-layer.md` -- subscriber host startup patterns
- `.github/docs/coreex/patterns.md` -- event and messaging patterns
- `.github/docs/coreex/local-dev.md` -- running locally with .NET Aspire
<!-- #if implement-servicebus -->
- `.github/docs/coreex/agents/CoreEx.Azure.Messaging.ServiceBus.md` -- Service Bus receiver, session config, subscriber patterns
<!-- #endif -->
- `.github/docs/coreex/agents/CoreEx.Events.md` -- event type system and subscriber contracts
- `.github/docs/coreex/agents/CoreEx.Caching.FusionCache.md` -- caching
<!-- #if refdata-enabled -->
- `.github/docs/coreex/agents/CoreEx.RefData.md` -- reference data patterns
<!-- #endif -->
