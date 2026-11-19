# CoreEx v4 — Web API Publishing and Async Operations Design

Status: draft for review (rev 4). Successor to v3 `WebApiPublisher` + `WorkStateOrchestrator`. The proposed public entry point is `WebApi.PublishAsync`; section 11 records decisions and section 10 lists remaining questions.

## 1. Principles

1. **Publishing is the default; tracking is opt-in.** The ordinary path validates (if configured) and publishes the request as an event/message without creating operation state or exposing status/result/cancel endpoints. Async request-reply explicitly opts into tracking. The command/event carries the work; tracking never gets a shared transaction with business data.
2. **`IWorkProvider` is independent of the business store.** No coupling to `IUnitOfWork`/outbox. `IEventPublisher` may be outbox-backed or direct; the operations layer does not care.
3. **Ordering plus a fence replaces atomicity.** State is created *before* dispatch. A publish error is treated as **unknown delivery**, never as "not sent". The outcome is decided by a compare-and-set race between the accept path (`Created → Failed`) and the worker (`Created → Started`), backed by a first-start deadline carried on the message (§4.1).
4. **Tracking is best effort; accepted work still runs.** When tracking state is missing or the store is unavailable, the worker runs the handler untracked by default (warning log + metric); fail-closed is a per-subscriber opt-in. Tracking loss therefore costs status accuracy, cancellation and duplicate suppression, not the work itself. A `404` or stale status after `202` is not proof of the business outcome and is not a safe retry signal (§5.5).
5. **At-least-once, honestly.** Fencing and execution leases prevent the common duplicate paths; they do not make execution exactly-once. Handlers remain idempotent (inbox and/or idempotent business writes).
6. **Domain-owned.** Each domain's API host owns its operations resource and its tracking keyspace. There is no global operations service.
7. **One generic operations resource per domain host**, not per-controller status/result/cancel endpoints.
8. **The public contract is a DTO, never `WorkState`.** Internal fields (type name, owner, ETag, lease, trace) never leave the server.
9. **Fail closed** on ownership; every state-changing call is non-`GET`.
10. **Idempotency is the developer's explicit choice** via `[IdempotencyKey]` on the publish endpoint; templates and skills emit it by default.

### Why not a shared-database `IWorkProvider` + outbox?

| Concern | Shared DB/UoW | Independent provider |
|---|---|---|
| Gap closed | Only "state created, dispatch failed" (a lost commit acknowledgement is still ambiguous) | Same gap handled by the CAS fence + start deadline (§4.1), including ambiguous delivery |
| Provider matrix | SQL Server, Postgres, Cosmos, ... each needing an outbox-aware provider | One contract; Redis + InMemory exist today |
| Cosmos | `TransactionalBatch` needs the same logical partition: operation ids would have to encode the business partition | No constraint |
| Write churn | Start/heartbeat/progress/complete writes land on the business DB | Isolated, TTL-native store |
| Retention | Relational needs a purge job | TTL is native (Redis) |
| Non-DB hosts (direct publish, Functions) | Not supported | Supported |

Conclusion: independent provider. A DB-backed `IWorkProvider` remains possible as a plain provider (no transactional coupling).

## 2. Components

| Component | Package | Role |
|---|---|---|
| `WorkOrchestrator` / `IWorkProvider` / `WorkState` | `CoreEx` (exists) | Engine: CAS lifecycle, expiry, TTL. Amendments in §8. |
| `RedisWorkProvider` | `CoreEx.Redis` (exists) | Shared state across the domain's API and Subscribe hosts. |
| `OperationStatus` (+ `OperationError`) | `CoreEx` | Public wire contract, used only when tracking is enabled. |
| `WebApiPublishOptions<TRequest[,TMessage]>` | `CoreEx.AspNetCore` | Optional `IValidator<TRequest>`, optional request-to-message mapper, optional `OperationPublishOptions`. |
| `WebApi.PublishAsync(...)` (MVC + Minimal) | `CoreEx.AspNetCore` | Validate and publish; publish-only by default, optionally create a tracked operation. |
| `MapCoreExOperations(...)` | `CoreEx.AspNetCore` | Domain host's generic status/result/cancel endpoints; relevant only to tracked mode. |
| `OperationOptions` | `CoreEx` (shared by API + Subscribe) | Route prefix, start timeout, retry-after policy, max wait, per-type authorization, missing-state policy. |
| `ISubscriberBehavior` + pipeline on `EventSubscriberBase` | `CoreEx.Events` | **Generic** around-processing extension point (not operations-specific). §5. |
| `OperationSubscriberBehavior` + `IOperationContext` | `CoreEx.Events` | First consumer of the behavior pipeline: worker-side lifecycle. |
| CloudEvent extensions `operationid`, `operationstartby` | `CoreEx.Events` | Explicit per-message operation metadata in tracked mode. CoreEx-defined, not a CloudEvents standard (§5.2). |

## 3. HTTP contract

Follows the Azure/Microsoft REST API guidelines long-running-operation shape and RFC 9110/7240/9457.

### 3.0 Domain scoping

- Each domain's API host maps `MapCoreExOperations()` at a configurable prefix (default `/operations`) relative to that host. Behind a gateway the domain prefix is the gateway's job (e.g. `/shopping/operations/{id}`).
- Tracking keys are namespaced by `IHostSettings.DomainName` (e.g. `work:{domain}:{id}`), so domains can share a Redis instance without collision. The domain's Subscribe host uses the same namespace.
- `Location`/`Operation-Location` are generated from the request's effective base (honouring `UseForwardedHeaders`) or an explicit `OperationOptions.BaseAddress`, so gateway-relative links resolve correctly.
- A client polls the domain it called. Cross-domain observation is out of scope (see open question on completion events).

### 3.1 Naming

| Name | Recommendation |
|---|---|
| `WebApi.PublishAsync(...)` | **Preferred.** Names the action, fits the existing WebApi helper surface, and covers both event and command publishing. |
| `WebApiPublisher` | Avoid as the main public API name; it sounds like a separate service and does not convey optional tracking. Retain only if implementation needs a focused internal/injectable component. |
| `WebApi.DispatchAsync(...)` | Viable, but can imply local handler invocation rather than message publication. |
| `WebApi.AcceptAsync(...)` | Describes the HTTP status/contract rather than the core action. |
| `WebApi.EnqueueAsync(...)` | Too transport-specific; the configured publisher may use a topic or outbox. |

`WebApiPublishOptions<TRequest[,TMessage]>` is the proposed options name, matching CoreEx's request-options naming while making the publish operation explicit.

### 3.2 Publish — default is untracked fire-and-forget

Illustrative endpoint API (names/signatures remain proposals):

```csharp
await webApi.PublishAsync(request, new WebApiPublishOptions<SubmitOrder>
{
    Validator = validator
});
```

This path optionally validates, then awaits publishing the incoming message as an event through the configured `IEventPublisher`. It creates **no** work state, adds **no** `operationid` metadata, and does not promise a status/result/cancel resource. It returns `202 Accepted` after the configured publisher/outbox confirms acceptance. A publish/commit failure remains an HTTP error; this is not `Task.Run` or detached in-process work. The outbox choice remains the host's configured publishing concern.

A different wire message may be produced via `WebApiPublishOptions<TRequest,TMessage>` and an explicit mapper; absent a mapper, the request itself is published as the message. Publishing as a command versus an event is selected by the message/destination configuration, not by whether tracking is enabled.

### 3.3 Tracked operation — explicit opt-in

```csharp
await webApi.PublishAsync(request, new WebApiPublishOptions<SubmitOrder>
{
    Validator = validator,
    Operation = new OperationPublishOptions { /* tracking configuration */ }
});
```

When `Operation` is supplied, validation still happens first; then operation state is created before publishing. The message carries `operationid` and `operationstartby`, and the response includes `OperationStatus` and `Location`/`Operation-Location` as described below. The same optional short wait observes this tracked execution; `Prefer: wait=N` does **not** itself turn tracking on. Configuring `Prefer: wait` on an endpoint without tracking is ignored or rejected explicitly (TBD), never silently creates an operation.

- Validator type: `IValidator<TRequest>?`, using CoreEx's async validation path. If validation fails, return the existing CoreEx validation response (`400` by default), with no state created and no message published. The worker must still enforce business rules because they can change between acceptance and execution.
- Idempotency remains an endpoint decision expressed through `[IdempotencyKey]` / `[IdempotencyKey(true)]`; it is independent of tracking and validation. Templates/skills add it to publish endpoints by default; `WarnWhenNotIdempotent` can flag missing annotations.
- For tracked endpoints, `Prefer: wait=N` (capped by `OperationOptions.MaxWait`) only observes the already-dispatched operation. Terminal within the window → `200`/`201` + result (or `resultLocation`) and `Preference-Applied`; otherwise `202`. Client disconnect stops waiting, never the work.

### 3.4 Status — `GET /operations/{opId}`


- **Always `200`** for an existing operation, including failed/canceled.
- `404` for unknown, wrong type, or not owned (no enumeration oracle).
- `ETag`; `If-None-Match` → `304`.
- `Retry-After` while non-terminal; adaptive with age (1s → 2s → 5s → 15s cap), overridable per type.
- Optional long-poll: `Prefer: wait=N` blocks until the ETag changes or N elapses.
- Terminal success sets `resultLocation` and `Location`. No automatic redirect; `303` is opt-in.

### 3.5 Result — `GET /operations/{opId}/result`

- `200` + stored result (small JSON) when `succeeded`; `204` when succeeded with no data.
- `409` problem details when not terminal or terminal-but-not-succeeded.
- `404` unknown/not owned.
- When the result is a domain resource, only `resultLocation` is stored and this endpoint is not used.
- Open: a single generic `/result` returns heterogeneous types, so its OpenAPI schema is weak (`oneOf` per registered type or untyped). Typed results are best described on the publish endpoint's `200`/`201` (wait path) and on the domain resource behind `resultLocation`.

### 3.6 Cancel — `POST /operations/{opId}:cancel`

- `202` + status when the cancellation *request* is recorded (cooperative).
- `409` when already terminal; `405` when cancellation is disabled for the type.
- The status becomes `canceled` only when it is safe: immediately if still `notStarted` (CAS `Created → Canceled`, so a late message is skipped); if `running`, `cancelRequested` is set and the handler decides whether to honour it. Business compensation for partially-done work is the handler's responsibility.

### 3.7 `OperationStatus`

```json
{
  "id": "…",
  "status": "notStarted | running | succeeded | failed | canceled",
  "createdAt": "…",
  "lastUpdatedAt": "…",
  "expiresAt": "…",
  "cancelRequested": false,
  "percentComplete": 40,
  "message": "Processed 400 of 1000 lines",
  "error": { "type": "…", "title": "…", "status": 422, "detail": "…", "code": "…" },
  "resultLocation": "/operations/{opId}/result"
}
```

Mapping from `WorkStatus`: `Created → notStarted`; `Started`/`Indeterminate → running` (`Indeterminate` adds a non-fatal `message`); `Completed → succeeded`; `Failed`/`Expired → failed`; `Canceled → canceled`.

**Unknown outcome.** An operation that started but never reported back before `Expiry` may or may not have had its business effect. It is reported as `status: failed` with `error.code = "outcome-unknown"`, not as a new status value. Rationale: the guideline status set is what generic pollers (Azure SDK-style, hand-written loops) treat as terminal; an unknown sixth value would make them spin or crash. The error code makes the ambiguity explicit for clients that care. Never-started expiry uses `error.code = "not-started"` (definitely did not run).

### 3.8 API description (OpenAPI + AsyncAPI)

- **OpenAPI** describes the HTTP surface. Each tracked publish endpoint declares every status code with its own schema/headers: `202` → `OperationStatus` + `Location`/`Operation-Location`/`Retry-After`; `200`/`201` → final result (only when `Prefer: wait` is enabled); `400`/`409`/`503` → `ProblemDetails`. A `links` entry on `202` points at `getOperation`. Status/result/cancel are documented once by `MapCoreExOperations`. Untracked publish endpoints document `202` and their validation/publish errors; they have no operation links.
- **Generator hints**: `x-ms-long-running-operation: true` + `x-ms-long-running-operation-options.final-state-via: operation-location` for AutoRest/Azure SDK-style generators (NSwag/Kiota just see a normal `202`). MVC: `[ProducesResponseType]` per status (proposed `[AcceptedOperation<TResult>]` convenience); Minimal API: `Results<Accepted<OperationStatus>, Ok<T>, ProblemHttpResult>`.
- **AsyncAPI** describes the broker side only: the command consumed by the worker, including the `operationid`/`operationstartby` extensions. It does not describe the HTTP polling contract.

## 4. Publish flow

```mermaid
sequenceDiagram
    participant C as Client
    participant I as Idempotency middleware (if configured)
    participant W as WebApi.PublishAsync
    participant O as WorkOrchestrator
    participant P as IEventPublisher (outbox or direct)
    C->>I: POST (Idempotency-Key if endpoint is marked)
    I->>W: first attempt (or replay stored response)
    W->>W: bind + optional validate
    alt validation fails
        W-->>C: Validation response; nothing created or published
    else valid
        alt Operation options absent (default)
            W->>P: publish incoming event (or explicitly mapped message)
            P-->>W: accepted
            W-->>C: 202 Accepted (no operation resource)
        else Operation options supplied
            W->>O: CreateAsync (Created, random id, domain, owner, type, StartBy)
            W->>P: publish tracked command (operationid + operationstartby)
            alt publish returned success
                W-->>C: 202 + Location/Operation-Location + OperationStatus
            else publish threw (delivery UNKNOWN)
                W->>O: FailAsync(id, dispatch-failed) — CAS, only from Created
                alt fence won (state now Failed)
                    W->>I: release attempt for retry (if idempotency configured)
                    W-->>C: 503 problem details + Retry-After
                else fence lost (worker already Started/terminal)
                    W-->>C: 202 + current OperationStatus
                else fence write failed (store unavailable)
                    W-->>C: 202 + OperationStatus(notStarted); start deadline reconciles
                end
            end
        end
    end
```

- The tracked initiating message is normally a **command** (one logical handler, competing consumers), routed by `NamedDestinationProvider` to `{Destination}-{domain}`. Self-addressed when the domain processes its own operation. The default untracked path publishes the request as an event unless the caller explicitly configures a command destination.
- Operation id: random (`Runtime.NewId()`), **not** derived from the idempotency key, CloudEvent id, correlation id, or trace id.
- **Tracked publish is "state + dispatch" only.** Business mutations belong in the worker. Writes in the publish transaction could commit on an ambiguous outbox commit while the fence marks the operation Failed and the worker skips the message.

### 4.1 Dispatch outcome: definite vs unknown

A publisher exception does not prove the message was not sent (send timeout after broker acceptance; outbox commit with lost acknowledgement). Exception classification is unreliable, so the design does not rely on it:

1. **CAS fence.** On any tracked-publish exception the publish endpoint attempts `Created → Failed` (`error.code = "dispatch-failed"`). The worker must win `Created → Started` before running the handler. The transition API reports whether *this caller* won (§8.10), so exactly one side proceeds:
   - Fence wins → operation definitely failed; any later copy of the message finds a terminal state and is skipped.
   - Fence loses with `Started`/terminal → the message was delivered; return `202` with the real status.
2. **First-start deadline (`StartBy`).** Set at create time (`OperationOptions.StartTimeout`, default 15 minutes) and carried as `operationstartby`. It covers the case where the fence write fails:
   - The engine expires a `Created` operation at `StartBy` (`not-started`) via the same CAS, so either the worker's first start or the deadline wins, never both.
   - The worker refuses any *first* start past `operationstartby`, even without consulting the store.
   - **Not** mapped to broker message TTL: a started operation's legitimate retries (abandon/redeliver) keep the original message and must survive past `StartBy`. Likewise CloudEvents `expirytime` ("event no longer useful") is related but not equivalent, because the message remains useful for an already-started operation; `operationstartby` stays a distinct CoreEx extension.
   - Clock skew matters only near `StartBy`; minutes-scale timeouts make seconds of skew tolerable, not irrelevant. Hosts should run synchronised clocks.

**Tracked-publish response semantics under the idempotency middleware** (only when the endpoint is marked). `PublishAsync` always writes a definite response and never lets a dispatch exception escape (an escaping exception would leave the key *indeterminate* and block retry):

| Situation | Response | Same-key retry |
|---|---|---|
| Publish succeeded | `202` + status | Replays the `202`. |
| Publish threw, fence won | `503` problem details + `Retry-After` | **Re-executes** (attempt released; creates a new operation). Safe because the failed operation is fenced. |
| Publish threw, fence lost | `202` + current status | Replays the `202`. |
| Publish threw, fence write failed | `202` + `notStarted` | Replays the `202`; polling resolves to `running` or `failed`/`not-started`. |
| State create failed (before publish) | `503` problem details | Re-executes (attempt released); nothing was dispatched. |

Releasing an attempt requires a small idempotency amendment (§8.11): today the store's `TryResolveAsync(allowRetry: true)` only applies to indeterminate/abandoned attempts, and the owned attempt is not reachable from the handler.

## 5. Worker side — subscriber behaviors

### 5.1 Generic extension point: `ISubscriberBehavior`

Operations should not get a bespoke base class (`OperationSubscribedBase` is dropped): it would only cover one subscriber shape and the next cross-cutting feature would need another. Instead `EventSubscriberBase` gets a **behavior pipeline**, registered in one place only. `ServiceBusSubscriberBase`, `ServiceBusSubscriberBase<T>` and `ServiceBusSubscribedSubscriber` all derive from it, so direct typed subscribers and `[Subscribe]`-dispatched `SubscribedBase` handlers are covered without touching `SubscribedBase`.

Illustrative shape (names not final):

```csharp
public interface ISubscriberBehavior
{
    Task<SubscriberOutcome> InvokeAsync(SubscriberBehaviorContext context, SubscriberBehaviorDelegate next, CancellationToken cancellationToken);

    Task OnDispositionAsync(SubscriberBehaviorContext context, SubscriberDisposition disposition, CancellationToken cancellationToken) => Task.CompletedTask;
}
```

- **Registration**: an ordered `Behaviors` collection on `EventSubscriberBase`, populated from DI (`services.AddSubscriberBehavior<T>()`, optionally filtered by subscriber type) or added explicitly in the subscriber's constructor. Behaviors resolve from the receive scope.
- **`SubscriberBehaviorContext`**: the `EventData`/CloudEvent metadata, `EventSubscriberArgs`, the matched handler (dispatched path) and a per-attempt property bag. No business payload is required for routing decisions.
- **Execution boundary (once per receive attempt)**:
  1. Transport decode → `EventData`.
  2. Dispatched path: `SubscribedManager` matches the handler. Unmatched messages never enter the pipeline (they are not "handled").
  3. **Behaviors run here**, wrapping steps 4–6.
  4. Inbox check (`IEventSubscriberInbox`).
  5. Deserialize + validate.
  6. Handler.
  The nested `ReceiveWrapperAsync` layers are not the seam; a per-attempt guard on `EventSubscriberArgs` ensures the pipeline executes exactly once even where wrappers nest.
- **`SubscriberOutcome`** classifies processing, distinct from broker settlement: `Handled`, `Skipped(reason)` (inbox duplicate, filtered), `Failed(exception, ErrorHandling)`. Today `ErrorHandler.Handle` returns failure even for `CompleteAsSilent`; the outcome keeps the resulting `ErrorHandling` so behaviors can tell "business failure, message completed" from "transient, will retry". Behaviors must never infer success from settlement success.
- **`OnDispositionAsync`** is invoked by the host adapter *after* retry determination and settlement decision (`ServiceBusReceiverBase<T>`, and `FunctionsServiceBusExtensions.FunctionReceiveAsync` for Functions), carrying `Completed`, `Abandoned` (will retry), `DeadLettered`, or `Catastrophic`. This is where "retries exhausted" becomes visible. Broker-autonomous dead-lettering (TTL/max delivery without invocation) never reaches application code; operation expiry covers it.

### 5.2 Operation metadata (which messages participate, and their work id)

- **Explicit per-message metadata** — CoreEx-defined CloudEvent extension context attributes:
  - `operationid` (String): the random operation id. Opaque.
  - `operationstartby` (Timestamp): first-start deadline.
- Set by `PublishAsync` via `EventData` extension helpers (e.g. `WithOperation(id, startBy)` / `TryGetOperation(...)`) using `EventData.Attributes`; `EventFormatter` already round-trips unknown attributes as CloudEvent extensions, so no business DTO changes.
- **Not** reused:
  - `id` (CloudEvent): identifies a message instance (`source`+`id`); duplicates may share it, republishing may not. Wrong lifecycle.
  - `correlationid`/`causationid` (CloudEvents correlation extension): logical-flow grouping, shared by many messages and many handlers.
  - `traceparent`: telemetry, not ownership.
  - `dataref`: claim-check pointer to payload content that must equal `data`; not a status/result link.
- Standards position: CloudEvents explicitly permits custom extensions (lowercase alphanumeric, ≤ 20 chars recommended). No CloudEvents extension defines HTTP operation lifecycle; the extension catalog itself carries no official standing. These names are CoreEx conventions, documented in the AsyncAPI message definition.
- **Participation policy** (`OperationSubscriberBehavior`):

| Behavior registered | Metadata | Result |
|---|---|---|
| No | any | Normal subscriber; metadata ignored. |
| Yes | absent | Normal processing (same handler serves tracked and untracked callers). |
| Yes | valid | Tracked (§5.3). |
| Yes | malformed (bad id, unparseable timestamp) | Explicit failure → dead-letter; never guessed. |
| Yes, `RequireOperation = true` | absent | Explicit failure (operations-only handlers). |

- **Trust**: metadata is not authorization. The behavior verifies the stored `WorkState` type matches the handler's registered operation type and domain/tenant before transitioning; a mismatch is treated as malformed.
- **No propagation**: `operationid` is never copied onto events/commands the handler publishes. Otherwise a downstream handler with the behavior registered could complete or fail the wrong operation. Operation linkage downstream, if wanted, is a business field or `correlationid`.

### 5.3 Tracked processing

1. Read `operationid`/`operationstartby`; load state (domain-namespaced key).
2. **Past `operationstartby` with no `Created` → `Started` decision possible** (store unavailable or state missing) → skip; the deadline is carried on the message and needs no store.
3. **Store unavailable** (provider throws, including `WorkOutcomeUnknownException`) → default: run the handler untracked with a warning log + metric. Opt-in fail-closed: transient, so the broker redelivers. See §5.5.
4. **State not found** → default: run the handler untracked with a warning log + metric. Opt-in fail-closed: skip with a logged error and complete the message (`Skipped("operation-missing")`). See §5.5.
5. **Terminal** (fence Failed, `not-started`, Canceled, already Completed) → skip, complete the message. Handler never runs.
6. **`Created`**: past `operationstartby` → skip (deadline). Otherwise CAS `Created → Started` with a new **execution lease** (`LeaseId`, `LeaseUntil`). Won → run handler. Lost → reload and re-evaluate.
7. **`Started`/`Indeterminate` (redelivery)**:
   - Lease still live → another instance is (probably) still running it (e.g. peek-lock lost while the handler runs). Do not run concurrently: return transient so the broker redelivers later.
   - Lease expired → take over: CAS to `Indeterminate` with a new lease, run the handler. Allowed past `StartBy`; the deadline gates only the first start.
8. During the run the behavior renews the lease (heartbeat updates `LastUpdated`), throttles `ReportProgressAsync`, and links `IOperationContext.CancellationToken` to the cancel request.
9. Outcome mapping (all CAS, conditioned on the current `LeaseId` so a superseded worker cannot finish someone else's run):
   - `Handled` → `CompleteAsync(result | resultLocation)`.
   - `Failed` with a completing `ErrorHandling` (business/validation) → `FailAsync(OperationError)` (sanitised).
   - `Failed` transient/retry → state untouched; lease released early so the next delivery can take over.
   - `Skipped` → state untouched, except inbox duplicate of a `Started` op whose result is unknown → leave for expiry/`outcome-unknown`.
   - Cancel honoured → `Canceled`.
10. `OnDispositionAsync(DeadLettered | Catastrophic)` → `FailAsync` (`error.code = "processing-failed"`) if still non-terminal.

This is still at-least-once: a lease can expire while a stalled-but-alive worker resumes. Handlers stay idempotent; `IEventSubscriberInbox` remains the duplicate-suppression tool.

### 5.4 Handler view

Handlers do not change base class. They optionally take `IOperationContext` (scoped, resolved from DI; a no-op instance when untracked):

```csharp
[Subscribe("order.fulfil")]
public sealed class FulfilOrderSubscriber(IFulfilmentService svc, IOperationContext op) : SubscribedBase<FulfilOrder>
{
    protected override async Task<Result> OnReceiveAsync(FulfilOrder value, EventSubscriberArgs args, CancellationToken ct)
    {
        await op.ReportProgressAsync(10, "Reserving stock", ct).ConfigureAwait(false);
        var order = await svc.FulfilAsync(value, op.CancellationToken).ConfigureAwait(false);
        op.SetResultLocation($"/orders/{order.Id}");
        return Result.Success;
    }
}
```

### 5.5 Tracking loss and store outage (best effort)

The tracking store is best effort. The goal is that **accepted work completes even if the store fails**; tracking accuracy is secondary. The Redis provider is a deliberately simple, low-effort implementation, not a durable store. Applications that need durable tracking supply another `IWorkProvider`.

Default **proceed untracked** (warning log + metric) when a worker finds the operation record missing or the store unavailable. The message's `operationstartby` is still honoured without the store: a first delivery after the deadline is skipped. Opt-in **fail-closed** per subscriber: missing → skip + error log + complete; unavailable → transient retry (and eventually dead-letter).

Consequences of the default, accepted by design:

- **Cancellation and fencing can be lost.** If a fenced or cancelled record disappears, a late delivery runs the handler anyway. In particular, an accept that returned `503` after an ambiguous publish can still execute.
- **Duplicates.** If the record disappears after a handler ran, a redelivery runs it again; if the store is unreachable, the live-lease check cannot defer a redelivery, so two handlers can overlap. Handlers must be idempotent anyway; this widens the window.
- **Status lies.** An untracked run cannot record progress or completion; mid-run renew/complete writes that fail are logged and the handler continues (it is not abandoned). Status may show `404`, a stale `notStarted`/`running`, or later `failed/not-started`/`outcome-unknown` for work that actually succeeded. Clients must treat these as unknown, not as proof of non-execution, and must not automatically resubmit. Retrying with a new idempotency key can dispatch duplicate work; retrying the same key only replays the original response if the separate idempotency record survived.
- If business-level deduplication is required, the handler must use a stable command/business idempotency identity and persist its deduplication atomically with the business effect where possible. The operation id alone cannot recover lost tracking or prove whether a side effect committed.
- Operational guidance: keep the work keyspace non-evictable (Redis `maxmemory-policy noeviction` or a dedicated instance/database) to make loss rare, but nothing above depends on it.

## 6. Failure matrix

| Failure | Outcome visible to client | Business consistency |
|---|---|---|
| Validation fails | 400, no operation | n/a |
| State create fails | `503`; same key retries | Nothing dispatched |
| Publish threw, never sent | `503`; state Failed | Nothing dispatched |
| Publish threw, actually delivered, fence won | `503`; state Failed | Late message skipped; handler never runs |
| Publish threw, worker started first | `202`; `running` | Work runs; client told the truth |
| Publish threw, fence write failed | `202` `notStarted` → `running` or `failed/not-started` | Exactly one of first start or deadline expiry wins |
| Message first delivered after `StartBy` | `failed/not-started` | Worker skips without needing the store |
| 202 lost in transit | Same-key retry replays the 202 | Single dispatch |
| Peek-lock lost while handler runs; message redelivered | `running` | Live lease → redelivery deferred, no concurrent run |
| Worker crashes after business commit, before Complete | Lease expires → takeover → `running` → `succeeded` | Handler/inbox idempotency |
| Retries exhausted (dead-letter via CoreEx) | `failed/processing-failed` | Via `OnDispositionAsync` |
| Broker-autonomous dead-letter | `failed/outcome-unknown` at `Expiry` (or `not-started`) | No app callback; expiry covers it |
| Tracking state lost before first start | `404` | Handler runs untracked (default); a lost fence/cancel is not honoured. Fail-closed opt-in skips |
| Tracking state lost after handler start | `404`; outcome unknown | Handler continues; a redelivery may rerun it. Do not automatically resubmit based on `404` |
| Store unavailable at delivery | Stale status, later `outcome-unknown`/`not-started` | Handler runs untracked (default); overlap with a live worker possible. Fail-closed opt-in retries |
| Cancel while `notStarted` | `canceled` | Late message skipped |
| Cancel races Complete | First terminal CAS wins | Unaffected |
| Never finishes | `failed/outcome-unknown` at `Expiry` | Late Complete ignored; effect may exist |

## 7. Security

- Ownership fails closed: owned operations only for the same authenticated caller; anonymous → 404.
- Per-type authorization on `MapCoreExOperations`, checked before disclosure.
- Ids random and opaque.
- `operationid` on a message is not authority: type/domain/tenant binding is verified against stored state (§5.2).
- Failure details are sanitised `OperationError`s; exception messages never stored.

## 8. Required engine amendments

1. Fix owner check fail-open in `GetWithTypeAsync` when the current user is null.
2. Allow `Fail` and `Cancel` from `Created`.
3. Progress: `PercentComplete`, `Message`, `LastUpdated`; `ReportProgressAsync` (non-terminal, CAS).
4. Structured `OperationError` (`code`, `title`, `detail`, `status`).
5. `ResultLocation` alongside data.
6. Optional change notification (Redis pub/sub) for long-poll/cancel observation; polling fallback.
7. `WorkArgs.StartTimeout` → `WorkState.StartBy`; expire `Created` at `StartBy` with `not-started`.
8. Retention invariant: terminal TTL ≥ `max(Retention, StartBy - now)`; validated at configuration.
9. Expiry codes: `not-started` vs `outcome-unknown`.
10. **Transition results report the winner**: e.g. `WorkTransitionResult { Won, Current }`. Returning an existing terminal state must not look like success.
11. **Idempotency attempt release**: an `HttpContext` feature (e.g. `IIdempotencyAttemptFeature.ReleaseForRetry()`) honoured by `IdempotencyProvider` by deleting the owned attempt instead of capturing the response. Used only for definite, fenced failures.
12. **Execution lease**: `LeaseId`, `LeaseUntil` on `WorkState`; start/takeover/renew/complete/fail conditioned on lease.
13. **Cancel request flag**: `CancelRequested` (non-terminal) distinct from the `Canceled` status.
14. **Domain keyspace**: provider keys include the domain name.

## 9. Phasing

1. Engine amendments (§8) + `OperationStatus` contract + race tests (start vs fence, start vs deadline, lease takeover).
2. `ISubscriberBehavior` pipeline on `EventSubscriberBase` + `SubscriberOutcome` + `OnDispositionAsync` in Service Bus receiver and Functions adapter. Valuable on its own (metrics, auditing, tenancy).
3. `WebApi.PublishAsync` publish-only default + optional validator + `MapCoreExOperations`; then idempotency release feature and OpenAPI metadata for tracked endpoints.
4. `OperationSubscriberBehavior` + `IOperationContext` + metadata helpers.
5. `Prefer: wait` + long-poll + change notification.
6. Contoso sample endpoint, docs, AI skills/instructions, Aspire E2E.

## 10. Open questions

1. `ISubscriberBehavior` naming and whether `OnDispositionAsync` lives on the same interface or a separate `ISubscriberDispositionObserver`.
2. Behavior registration filter: all subscribers in the host vs per subscriber/handler type (proposed: DI registration with an optional predicate).
3. Lease duration/renewal defaults relative to Service Bus lock duration (proposed: lease ≈ 2× lock renewal interval, renewed by heartbeat).
4. Generic `/result` OpenAPI typing (§3.3): per-type `oneOf` vs untyped vs dropping `/result` in favour of `resultLocation` only.
5. Default `StartTimeout` value (proposed 15 minutes for all hosts; outbox hosts raise it if relay lag can exceed it). No auto-detection of outbox publishers.
6. `Prefer: wait` on an endpoint configured for publish-only: reject with a clear error or ignore the preference (proposed: reject, since waiting for an untracked result is meaningless).
7. `WorkOutcomeUnknownException` classification at the API boundary: decide whether it should become CoreEx `TransientException` (enabling WebApi 503/`Retry-After`) or remain an explicit unknown-outcome exception with boundary-specific handling. At the subscriber it is treated as store-unavailable (run untracked by default; transient only under fail-closed, §5.5). Any retry must re-read operation state; a missing tracking record does not prove that the handler did not run, and retry safety still depends on business-level idempotency.

## 11. Decisions

| # | Topic | Decision |
|---|---|---|
| D1 | Scope | Domain-owned operations resource and keyspace; no global service (§3.0). |
| D2 | Cancel verb | `POST /operations/{id}:cancel`; optional `DELETE` = forget, not cancel. Design choice following Azure/Google guidelines; no RFC mandates it. |
| D3 | Missing state or store unavailable at the worker | Proceed untracked by default (warning + metric); fail-closed per-subscriber opt-in. Accepted work completing beats tracking accuracy (§5.5). |
| D4 | Completion event for cross-domain consumers | Deferred. Domain events published by the worker's own business logic are the cross-domain signal. |
| D5 | Batch/collection accept | Deferred. |
| D6 | Idempotency | Developer marks the endpoint `[IdempotencyKey]`; not forced; templates/skills default it on; startup warning when absent. |
| D7 | Start deadline | Single `StartTimeout` (default 15 min), not tied to broker TTL; CloudEvents `expirytime` not reused. |
| D8 | Unknown outcome | `failed` + `error.code = "outcome-unknown"`; no new status value. |
| D9 | Fence-won retry | Same key re-executes (attempt released, §8.11); no forced new key. |
| D10 | Worker integration | `ISubscriberBehavior` on `EventSubscriberBase` only; `OperationSubscribedBase` dropped. |
| D11 | Work id | Explicit `operationid` extension; not CloudEvent `id`, `correlationid`, `traceparent` or `dataref`. |
| D12 | API naming | Prefer action `WebApi.PublishAsync(...)` with `WebApiPublishOptions<TRequest[,TMessage]>`; alternatives and rationale in §3.1. |
| D13 | Tracking mode | `Operation` options default to absent: publish-only, no tracking, no operation metadata. Explicit `Operation` configuration opts into async request-reply ceremony. |
| D14 | Pre-validation | Optional `IValidator<TRequest>` on publish options; validate before state creation or publish in both modes. |
