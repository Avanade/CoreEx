---
name: coreex-subscriber
description: "Add or modify an event/command subscriber in a CoreEx Subscribe host. USE FOR: command subscriber (owns the contract, delegates to app service), event-data-sync subscriber (delegates to IXxxSyncAdapter), event-business-process subscriber (choreography step, delegates to app service). Covers SubscribedBase, SubscribedBase<T>, ValueValidator, ErrorHandler, and subject naming. DO NOT USE FOR: API controllers (use coreex-api), application services (use coreex-app-service), replication adapter implementations (use coreex-adapter), Subscribe host Program.cs setup (see coreex-host-setup.instructions.md), Subscribe-test integration tests (use coreex-test-subscribe)."
argument-hint: "Optional: subscriber scenario (command / event-sync / event-process), subject string, payload type, whether ErrorHandler is needed"
tags: ["subscriber", "messaging", "service-bus", "event-handling", "choreography", "saga", "coreex"]
---

# CoreEx: Subscriber

Guides you through adding or modifying a subscriber in a CoreEx Subscribe host. A subscriber is the
messaging equivalent of a controller — it receives a command or event from the broker and immediately
delegates to an Application-layer service or adapter. No business logic lives here.

## When to Use

There are three distinct subscriber scenarios — determine which applies before writing any code:

| Scenario | Trigger | Delegates to | Example |
|---|---|---|---|
| **Command** | A command addressed to _this_ domain arrives on the broker | Application service | `ReservationConfirmSubscriber` → `IMovementService` |
| **Event — Data Sync** | An event from _another_ domain/system arrives; maintain a local cached copy | Replication adapter (`IXxxSyncAdapter`) | `ProductModifySubscriber` → `IProductSyncAdapter` |
| **Event — Business Process** | An event from another domain arrives; trigger a choreography step in _this_ domain | Application service | e.g., `OrderPlacedSubscriber` → `IFulfillmentService` |

> **Rule:** Never subscribe to a **command addressed to another domain** — only to commands addressed
> to this domain, or to events from any domain. Subscribing to another domain's commands means you
> are not the intended recipient. Use events for cross-domain coordination.

## When Not to Use

- HTTP API controllers — use `coreex-api`
- Application services that the subscriber calls — use `coreex-app-service`
- Replication adapter implementations (`IXxxSyncAdapter`) — use `coreex-adapter`
- Subscribe host `Program.cs` setup or Service Bus receiver wiring — see `.github/instructions/coreex-host-setup.instructions.md`

## Quick Reference

- **Untyped subscriber** (`SubscribedBase`) — message carries data in the key only; extract with `@event.Key.Required()`
- **Typed subscriber** (`SubscribedBase<TValue>`) — message carries a serialised payload; set `ValueValidator` to validate before `OnReceiveAsync`
- `[ScopedService]` + one or more `[Subscribe("subject")]` attributes on every subscriber class
- No `Program.cs` edit needed — `AddSubscribersUsing<T>()` discovers all `[Subscribe]`-decorated classes automatically
- Subject format: `{solution}.{domain}.{entity}.{action}[.v{n}]` — include `.v{n}` only when the message carries a payload
- `EventData.CreateCommand(...)` for commands; `EventData.CreateEvent(...)` / `new EventData().WithTitle(...)` for events
- `ErrorHandler` for graceful not-found and retry/dead-letter control — share the same static instance across related subscribers
- Integration tests use `WithApiTester<Program>` (Subscribe host); simulate receipt via `ServiceBusSubscribedSubscriber.ReceiveAsync(sbm)` — see `coreex-test-subscribe` for the full test workflow

For full workflow and code examples see [`references/workflow.md`](references/workflow.md).

## Key References

- `samples/src/Contoso.Products.Subscribe/Subscribers/ReservationConfirmSubscriber.cs` — command subscriber with `ErrorHandler`
- `samples/src/Contoso.Products.Subscribe/Subscribers/ReservationCancelSubscriber.cs` — command subscriber sharing an `ErrorHandler`
- `samples/src/Contoso.Shopping.Subscribe/Subscribers/ProductModifySubscriber.cs` — typed event-sync subscriber with `ValueValidator`
- `samples/src/Contoso.Shopping.Subscribe/Subscribers/ProductDeleteSubscriber.cs` — untyped event-sync subscriber (key-only delete)
- `coreex-test-subscribe` — full Subscribe-test integration test workflow (test class shape, simulating message receipt, command/event-sync/event-business-process test patterns, unsubscribed-subject test)
- `.github/instructions/coreex-event-subscribers.instructions.md` — full subscriber conventions reference
- `.github/instructions/coreex-host-setup.instructions.md` — Subscribe host `Program.cs` shape
