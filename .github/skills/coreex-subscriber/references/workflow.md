# coreex-subscriber: Workflow

Full workflow for adding or modifying a subscriber in a CoreEx Subscribe host.

---

## Phase 1 — Clarify Before Writing

| Question | Notes |
|---|---|
| Which scenario? | Command / Event-DataSync / Event-BusinessProcess — drives base class, subject, and delegation target |
| Does the message carry a payload? | Yes → `SubscribedBase<TValue>` + `ValueValidator`. No → `SubscribedBase`, extract key with `@event.Key.Required()` |
| What does the subscriber delegate to? | Command or Business-Process → application service. Data-Sync → replication adapter (`IXxxSyncAdapter`). Never both in the same subscriber. |
| Is an `ErrorHandler` needed? | Yes for command/process subscribers where a not-found outcome is semantically valid (e.g., idempotent replay). Data-sync subscribers typically handle not-found silently via delete semantics. |
| Are multiple subjects handled identically? | Yes → stack `[Subscribe]` attributes on a single class. No → separate classes per subject. |
| Is an integration test needed? | Yes — always provide a test class. Use existing `*.Test.Subscribe` project for the domain. |

---

## Step 1 — Subscriber Class

### Scenario A — Command

A command is addressed to _this_ domain. The contract (payload or key) is owned by this domain.
The command was published by another service specifically to trigger logic here. Delegate to an
application service — the subscriber is the entry point only.

```csharp
// Subscribe/Subscribers/{Entity}{Action}Subscriber.cs
namespace {Domain}.Subscribe.Subscribers;

[ScopedService, Subscribe("{solution}.{domain}.{entity}.{action}")]
public class {Entity}{Action}Subscriber : SubscribedBase
{
    /// <summary>
    /// Handles the case where the target entity is not found — treat as completed-informational
    /// rather than dead-lettering, to allow for idempotent replay and expiry scenarios.
    /// </summary>
    internal static readonly ErrorHandler DefaultErrorHandler = new ErrorHandler()
        .Add<NotFoundException>(ex => ex.ErrorCode == "{entity}-not-found"
            ? ErrorHandling.CompleteAsInformation   // consume silently; log as informational
            : null);                                // null = fall through to default handling (retry / dead-letter)

    private readonly I{Entity}Service _service;

    public {Entity}{Action}Subscriber(I{Entity}Service service)
    {
        _service = service.ThrowIfNull();
        ErrorHandler = DefaultErrorHandler;         // set in constructor body (primary constructor cannot set base properties)
    }

    protected override async Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
    {
        var id = @event.Key.Required();
        await _service.{Action}Async(id, cancellationToken).ConfigureAwait(false);
        return Result.Success;
    }
}
```

> **Note on `return Result.Success`:** Use the async body pattern when the application service returns
> `Task` (not `Task<Result>`). If the service returns `Task<Result>`, prefer the expression body:
> `=> _service.{Action}Async(@event.Key.Required(), cancellationToken);`

**Rules:**
- Commands follow the same version-suffix rule as events: include `.v{n}` when the message carries a
  payload; omit it for key-only commands. See [Step 2 — Subject Naming](#step-2--subject-naming).
- `ErrorHandler` must be assigned in the constructor body — it is a property on the base class and
  cannot be set with a field initialiser in a primary constructor class.
- Share the same static `ErrorHandler` instance across related command subscribers (e.g., confirm and cancel).

---

### Scenario B — Event, Data Synchronization

Another domain or external system publishes events for data that this domain needs a local cached
copy of. The subscriber delegates exclusively to a replication adapter (`IXxxSyncAdapter`).
Keep only the fields the domain needs internally — do not replicate the full external contract.

**Typed subscriber (message carries a payload):**

```csharp
// Subscribe/Subscribers/{Entity}ModifySubscriber.cs
namespace {Domain}.Subscribe.Subscribers;

[ScopedService]
[Subscribe("{solution}.{external}.{entity}.created.v1")]
[Subscribe("{solution}.{external}.{entity}.updated.v1")]
public class {Entity}ModifySubscriber(I{Entity}SyncAdapter adapter) : SubscribedBase<{Entity}>
{
    private readonly I{Entity}SyncAdapter _adapter = adapter.ThrowIfNull();

    public override IValidator<{Entity}>? ValueValidator => {Entity}Validator.Default;

    protected override Task<Result> OnReceiveAsync({Entity} value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.ModifyAsync(value, cancellationToken);
}
```

**Untyped subscriber (key-only — delete by ID):**

```csharp
// Subscribe/Subscribers/{Entity}DeleteSubscriber.cs
namespace {Domain}.Subscribe.Subscribers;

[ScopedService, Subscribe("{solution}.{external}.{entity}.deleted")]
public class {Entity}DeleteSubscriber(I{Entity}SyncAdapter adapter) : SubscribedBase
{
    private readonly I{Entity}SyncAdapter _adapter = adapter.ThrowIfNull();

    protected override Task<Result> OnReceiveAsync(EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _adapter.DeleteAsync(@event.Key.Required(), cancellationToken);
}
```

**Rules:**
- Validate incoming payloads with `ValueValidator` — only accept well-formed data into the local store.
- The adapter handles upsert semantics — a create and an update event route to the same `ModifyAsync`.
- Delete is key-only; no payload, no version suffix on the subject.
- Do not store the full external contract — map to the minimal local persistence model in the adapter.

---

### Scenario C — Event, Business Process (Choreography)

Another domain published an event that triggers a step in _this_ domain's workflow. This is pure
choreography — no central orchestrator. The subscriber delegates to an application service; the
service executes the business logic and publishes any resulting events to continue the chain.

```csharp
// Subscribe/Subscribers/{TriggerEntity}{TriggerAction}Subscriber.cs
namespace {Domain}.Subscribe.Subscribers;

[ScopedService, Subscribe("{solution}.{external}.{trigger-entity}.{trigger-action}.v1")]
public class {TriggerEntity}{TriggerAction}Subscriber(I{DomainAction}Service service) : SubscribedBase<{TriggerPayload}>
{
    private readonly I{DomainAction}Service _service = service.ThrowIfNull();

    public override IValidator<{TriggerPayload}>? ValueValidator => {TriggerPayload}Validator.Default;

    protected override Task<Result> OnReceiveAsync({TriggerPayload} value, EventData @event, EventSubscriberArgs args, CancellationToken cancellationToken = default)
        => _service.{DomainAction}Async(value, cancellationToken);
}
```

**Rules:**
- The trigger event is from an external domain — never subscribe to commands addressed to others.
- The application service decides what to do in response and publishes any outbound events.
- An `ErrorHandler` is appropriate when the not-found case is semantically valid (e.g., the referenced
  entity was deleted before this event was processed).

---

## Step 2 — Subject Naming

Use dot-separated lowercase strings: `{solution}.{domain}.{entity}.{action}[.v{n}]`

| Rule | Example |
|---|---|
| With payload → include `.v{n}` suffix | `contoso.products.product.created.v1` |
| Key-only (no payload) → no version suffix | `contoso.products.product.deleted` |
| Commands with payload → include `.v{n}` suffix | `contoso.products.reservation.create.v1` |
| Commands key-only (no payload) → no version suffix | `contoso.products.reservation.confirm` |
| `EventData.CreateCommand(...)` for commands | `EventData.CreateCommand("{domain}", "{entity}", "{action}")` |
| `EventData.CreateEvent(...)` or `new EventData().WithTitle(...)` for events | `new EventData().WithTitle("{solution}.{domain}.{entity}.updated.v1")` |

The version reflects the payload schema — not whether the message is an event or command. A command
with a payload would also carry a version; a key-only event would not.

---

## Step 3 — Error Handling

Define a static `ErrorHandler` when specific exceptions should be treated differently from the
default retry/dead-letter behaviour. The most common case: a `NotFoundException` that is semantically
expected (e.g., an entity was deleted before a queued command was processed).

```csharp
internal static readonly ErrorHandler DefaultErrorHandler = new ErrorHandler()
    .Add<NotFoundException>(ex => ex.ErrorCode == "{entity}-not-found"
        ? ErrorHandling.CompleteAsInformation   // consume silently; log as informational
        : null);                                // null = fall through to default dead-letter logic
```

**Assign in the constructor:** `ErrorHandler = DefaultErrorHandler;`

**Share across related subscribers:**

> Example uses an illustrative Reservation domain — substitute your own entity/service/subscriber
> names and `{solution}`/`{domain}` literals.

```csharp
// ReservationCancelSubscriber.cs — reuses the handler defined in ReservationConfirmSubscriber
public ReservationCancelSubscriber(IMovementService service)
{
    _service = service.ThrowIfNull();
    ErrorHandler = ReservationConfirmSubscriber.DefaultErrorHandler;
}
```

**`ErrorHandling` values:**

| Value | Behaviour |
|---|---|
| `CompleteAsInformation` | Message consumed; logged as information (not an error) |
| `CompleteAsWarning` | Message consumed; logged as warning |
| `CompleteAsSilent` | Message consumed; no log entry |
| `Retry` | Message requeued for retry |
| `DeadLetter` | Message sent to dead-letter queue |
| `null` (from handler) | Fall through to next handler / default behaviour |

---

## Step 4 — DI Discovery

No `Program.cs` edit is required when adding a new subscriber. `AddSubscribersUsing<T>()` in
`Program.cs` scans the assembly containing `T` and auto-registers every class decorated with both
`[ScopedService]` and one or more `[Subscribe]` attributes:

```csharp
// Program.cs — already present; adding a new subscriber requires only creating the class
builder.Services
    .AddSubscribedManager((_, c) => c.AddSubscribersUsing<AnySubscriberInAssembly>());
```

> Only confirm `Program.cs` is wired correctly when bootstrapping a new Subscribe host from scratch.
> For existing hosts (all sample Subscribe projects), no change is needed.

---

## Step 5 — Integration Tests

Always provide a test for a new or modified subscriber. Full workflow, test-class shape, message-receipt
simulation, and per-scenario test patterns (command / event-data-sync / event-business-process, plus the
required "unsubscribed subject" test) now live in the dedicated **`coreex-test-subscribe`** skill — see
[`../coreex-test-subscribe/references/workflow.md`](../coreex-test-subscribe/references/workflow.md).
That skill also links back to `coreex-test-api` for the shared DB/cache/outbox `OneTimeSetUp` mechanics
(seed authoring, provider-specific outbox helpers) common to both API and Subscribe host tests.

---

## Saga and Choreography Concepts

Subscribers are the building blocks of distributed coordination. Understanding where they fit in the
saga pattern helps choose the right scenario type.

### What is a Saga?

A saga is a sequence of local transactions across multiple services where each transaction publishes
a message to trigger the next step. If a step fails, compensating transactions undo prior work. This
provides eventual consistency without distributed two-phase commits.

There are two coordination styles:

| Style | Description | CoreEx fit |
|---|---|---|
| **Choreography** | Each service reacts to events and publishes its own events. No central coordinator. | Event-BusinessProcess subscribers + Outbox relay |
| **Orchestration** | A central coordinator (e.g., Durable Task, Azure Logic Apps) tells each service what to do. | Orchestrator calls service APIs; services remain passive |

### Choreography with CoreEx Subscribers

> Example uses an illustrative Shopping/Products reservation flow — substitute your own domains,
> entities, subjects, and `{solution}` literals.

The Shopping/Products reservation flow is a choreography saga:

1. **Shopping** places an order → publishes `reservation.confirm` command via Outbox relay
2. **Products** `ReservationConfirmSubscriber` receives it → `IMovementService.ConfirmReservationAsync()` → publishes `movement.confirmed` events via Outbox
3. **Shopping** (or another service) may subscribe to those events to continue the workflow

Compensation follows the same pattern: Shopping publishes `reservation.cancel`, Products cancels and
restores inventory.

**Design rules for choreography:**
- Each service only knows about its own events — never about the internal state of other services.
- Compensating commands/events follow the same subscriber pattern as forward commands.
- Keep sagas as short as possible — each additional step is another failure point.
- Idempotency is essential — messages can be replayed; use `ErrorHandler` to handle "already done" gracefully.
- Limit cross-domain coordination where possible — every saga hop adds latency and failure surface area.

> **Orchestration note:** When coordination complexity exceeds what choreography can handle cleanly,
> consider a Durable Task-based orchestrator (the CoreEx samples' Aspire setup includes the
> Durable Task emulator). Orchestration is out of scope for the subscriber skill — it lives in a
> dedicated orchestrator service, not in subscriber classes.

---

## Guardrails

- **Never embed business logic** in a subscriber — immediate delegation only; the subscriber is a thin
  routing layer
- **Never subscribe to a command addressed to another domain** — a command is addressed to a specific
  recipient; subscribing to another domain's commands means you are not the intended recipient and
  creates tight coupling; use events for cross-domain coordination
- **Do not use MediatR or in-process dispatchers** — subscribers react to integration events from
  the broker only; in-process dispatching bypasses the error handling and dead-letter semantics
- **Do not manually register subscriber classes** — `AddSubscribersUsing<T>()` discovers them;
  never add `AddScoped<IXxxSubscriber, XxxSubscriber>()` manually
- **`[ScopedService]` is required** — without it, `AddSubscribersUsing` will not discover the class
- **Do not omit `AddEventFormatter()`** from `Program.cs` — required for message parsing
- **`ErrorHandler = DefaultErrorHandler`** must be set in the **constructor body** — it is a base
  class property and cannot be set via a field initialiser in a primary constructor class
- **Subject version suffix (`.v{n}`) is payload-driven**, not event-type-driven — add it when the
  message carries a data schema; omit it for key-only messages
- **Outbox assertion helper is database-specific** — use `UseExpectedSqlServerOutboxPublisher()` for
  SQL Server domains; `UseExpectedPostgresOutboxPublisher()` for PostgreSQL; do not mix them
- **Always specify seed files explicitly** — `read-data.seed.yaml` for read tests, `mutate-data.seed.yaml`
  for mutate/subscribe tests, `no-data.seed.yaml` for schema-only tests (health, relay); never rely on
  auto-discovery of YAML files in the test common assembly
