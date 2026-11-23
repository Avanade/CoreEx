# CoreEx v4 — Web API Publishing and Async Operations Design

Status: implementation baseline for P2 (Web API publishing and operation resources); successor to v3 `WebApiPublisher` + `WorkStateOrchestrator`. P0/P1 and P2 are implemented; later phases remain proposals. The following implementation notes record verified differences and deferred behavior.

**P2 implementation notes.** `WebApi.PublishAsync` supports request-body and pre-deserialized inputs, optional validation, identity or explicit request-to-message mapping, publish-only default, and opt-in tracking. `MapCoreExOperations` maps owner-checked status, result, and cancel routes; scope defaults to domain-wide and explicit tenant scope uses `ExecutionContext.TenantId`. The host is responsible for establishing the execution user and tenant. The HTTP response uses `CoreEx.AspNetCore.Operations.OperationStatus` and its safe `OperationError` projection; persisted engine failures remain `CoreEx.Hosting.Work.WorkError`, and `WorkState` is never exposed. Tracked publish metadata (`[PublishTracking]` or `.WithPublishTracking()`) both enables runtime tracking and documents the endpoint; `WebApiPublishOperationOptions` only tunes start timeout and expiry. Without a routed endpoint, a non-null `WebApiPublishOperationOptions` explicitly enables tracking for direct `WebApi.PublishAsync` calls. Untracked publishing uses standard 202 response metadata. P2 does not implement `Prefer: wait`, long polling, `OperationSubscriberBehavior`, `IOperationContext`, or a worker that consumes `operationid`/`operationstartby`; until that later phase lands, tracked status is not a worker-side execution guarantee. Cosmos's current outbox requires an active unit of work and is not a generic publish-only publisher.

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
| `OperationStatus` (+ `OperationError`) | `CoreEx.AspNetCore` (`CoreEx.AspNetCore.Operations`) | Public HTTP response contract; persisted engine errors use `CoreEx.Hosting.Work.WorkError`. |
| `WebApiPublishOptions<TRequest[,TMessage]>` | `CoreEx.AspNetCore` | Optional `IValidator<TRequest>`, optional request-to-message mapper, optional `WebApiPublishOperationOptions`. |
| `WebApi.PublishAsync(...)` (MVC + Minimal) | `CoreEx.AspNetCore` | Validate and publish; publish-only by default, optionally create a tracked operation. |
| `MapCoreExOperations(...)` | `CoreEx.AspNetCore` | Domain host's generic status/result/cancel endpoints; relevant only to tracked mode. |
| `OperationsOptions` / `OperationTypeOptions<TMessage>` | `CoreEx.AspNetCore` (API host) | Route prefix, key scope, tenant accessor, per-type authorization, result/error projection, cancellation and retry-after policy. |
| `WebApiPublishOperationOptions` | `CoreEx.AspNetCore` (publish endpoint) | Start timeout and operation expiry. Worker-specific options remain deferred. |
| `ISubscriberBehavior` + pipeline on `EventSubscriberBase` | `CoreEx.Events` | **Generic** around-processing extension point (not operations-specific). §5. |
| `OperationSubscriberBehavior` + `IOperationContext` | `CoreEx.Events` | First consumer of the behavior pipeline: worker-side lifecycle. |
| CloudEvent extensions `operationid`, `operationstartby` | `CoreEx.Events` | Explicit per-message operation metadata in tracked mode. CoreEx-defined, not a CloudEvents standard (§5.2). |

## 3. HTTP contract

Follows the Azure/Microsoft REST API guidelines long-running-operation shape and RFC 9110/7240/9457.

### 3.0 Domain scoping

- Each domain's API host maps `MapCoreExOperations()` at a configurable prefix (default `/operations`) relative to that host. Behind a gateway the domain prefix is the gateway's job (e.g. `/shopping/operations/{id}`).
- Tracking keys are namespaced by `IHostSettings.DomainName` (e.g. `work:{domain}:{id}`), so domains can share a Redis instance without collision. The domain's Subscribe host uses the same namespace.
- `Location`/`Operation-Location` are relative to the request origin by default and include `PathBase`. `WebApiBase` applies the same shared link policy to ordinary resource locations and paging links; set `UseAbsolutePaths` to use the request's effective base (honouring `UseForwardedHeaders`) or `BaseAddress` to supply an explicit external base.
- A client polls the domain it called. Cross-domain observation is out of scope (see open question on completion events).

### 3.1 Naming

| Name | Recommendation |
|---|---|
| `WebApi.PublishAsync(...)` | **Preferred.** Names the action, fits the existing WebApi helper surface, and covers both event and command publishing. |
| `WebApiPublisher` | Avoid as the main public API name; it sounds like a separate service and does not convey optional tracking. Retain only if implementation needs a focused internal/injectable component. |
| `WebApi.DispatchAsync(...)` | Viable, but can imply local handler invocation rather than message publication. |
| `WebApi.AcceptAsync(...)` | Describes the HTTP status/contract rather than the core action. |
| `WebApi.EnqueueAsync(...)` | Too transport-specific; the configured publisher may use a topic or outbox. |

`WebApi.PublishAsync(request, options)` accepts a single `WebApiPublishOptionsBase`, created via the `WebApiPublishOptions` factories: `Create<TRequest>()` (parse the request body), `Create<TRequest>(value)` (publish an already-deserialized value), `CreateMapped<TRequest,TMessage>(mapper)` (explicit CoreEx mapping), `CreateWithValueFactory<TMessage>(...)` (bodyless custom preparation returning a payload), and `CreateWithEventFactory<TMessage>(...)` (bodyless custom preparation returning a complete event). The two custom preparation modes are distinct factory methods, so the selected mode is fixed at creation. Shared fluent configuration covers the destination, publisher resolution, event transformation (`WithEvent`) and operation options; typed options add validation and payload-aware event factories. These configuration objects are separate from request-bound `WebApiOptions`, which is supplied to custom preparation callbacks.

### 3.2 Publish — default is untracked fire-and-forget

Illustrative endpoint API:

```csharp
await webApi.PublishAsync(request, WebApiPublishOptions.Create<SubmitOrder>().WithValidator(validator));
```

This path optionally validates, then awaits publishing the incoming message through the configured `IEventPublisher`. By default the message is a self-addressed **command** with the `publish` action; `.WithEvent(...)` can instead change the message type (for example, to an event with the `published` action), domain, action or any other envelope property, and typed options can supply an explicit message factory. It creates **no** work state, adds **no** `operationid` metadata, and does not promise a status/result/cancel resource. It returns `202 Accepted` after the configured publisher/outbox confirms acceptance. A publish/commit failure remains an HTTP error; this is not `Task.Run` or detached in-process work. The outbox choice remains the host's configured publishing concern.

A different wire message may be produced via `WebApiPublishOptions.CreateMapped<TRequest,TMessage>(mapper)`; absent a mapper, the request itself is published as the message. Publishing as a command versus an event is selected by the message/destination configuration, not by whether tracking is enabled.

For custom preparation without automatic request-body parsing, `CreateWithValueFactory<TMessage>(...)` returns a payload wrapped in the default message, while `CreateWithEventFactory<TMessage>(...)` returns a caller-owned `EventData` that receives no framework defaults. Both return a `Result`; failures may be returned or thrown. The callback owns any parsing and validation and must not publish. Returned failures use the existing WebApi error policy before publisher resolution or operation creation. All variants use the same framework-owned dispatch, operation metadata, fencing and acceptance path. Explicit `TMessage` must match the payload type used for tracking registration; it cannot be inferred or verified from the serialized message.

### 3.3 Tracked operation — explicit opt-in

```csharp
[HttpPost, PublishTracking]
public Task<IActionResult> Submit(CancellationToken cancellationToken = default) => webApi.PublishAsync(Request, WebApiPublishOptions.Create<SubmitOrder>().WithValidator(validator), cancellationToken);
```

When an endpoint has `[PublishTracking]` or `.WithPublishTracking()`, validation still happens first; then operation state is created before publishing. The message carries `operationid` and `operationstartby`, and the response includes `OperationStatus` and `Location`/`Operation-Location` as described below. `WebApiPublishOperationOptions` optionally tunes `StartTimeout` and `Expiry`; it does not enable tracking for a routed endpoint. For direct `WebApi.PublishAsync` calls that have no routed endpoint, a non-null `WebApiPublishOperationOptions` explicitly enables tracking. `Prefer: wait=N` does **not** itself turn tracking on; P2 ignores the preference and never creates an operation for a publish-only endpoint.

- Validator type: `IValidator<TRequest>?`, using CoreEx's async validation path. If validation fails, return the existing CoreEx validation response (`400` by default), with no state created and no message published. The worker must still enforce business rules because they can change between acceptance and execution.
- Idempotency remains an endpoint decision expressed through `[IdempotencyKey]`; it is independent of tracking and validation. Templates/skills are expected to add it to publish endpoints by default. `OpenApiOptions.WarnWhenPublishMissingIdempotencyKey` optionally emits a trace warning for tracked publishing endpoints during OpenAPI generation; there is no startup/runtime enforcement.
- **Deferred:** P2 does not implement `Prefer: wait=N`, `OperationOptions.MaxWait`, `Preference-Applied`, or a final `200`/`201` response. P2 always returns `202` after successful or potentially accepted tracked publication.

### 3.4 Status — `GET /operations/{opId}`


- **Always `200`** for an existing operation, including failed/canceled.
- `404` for unknown, wrong type, or not owned (no enumeration oracle).
- `ETag`; `If-None-Match` → `304`.
- `Retry-After` while non-terminal; adaptive with age (1s → 2s → 5s → 15s cap), overridable per type.
- **Deferred:** optional long-poll (`Prefer: wait=N`) is not part of P2.
- Terminal success sets `resultLocation` and `Location`. No automatic redirect; `303` is opt-in.

### 3.5 Result — `GET /operations/{opId}/result`

- `200` + stored result (small JSON) when `succeeded`; `204` when succeeded with no data.
- `409` problem details when not terminal or terminal-but-not-succeeded.
- `404` unknown/not owned.
- When the result is a domain resource, only `resultLocation` is stored and this endpoint is not used.
- Open: a single generic `/result` returns heterogeneous types, so its OpenAPI schema is weak (`oneOf` per registered type or untyped). P2 leaves the result response untyped; typed results are described by the application-owned result reader or the domain resource behind `resultLocation`.

### 3.6 Cancel — `POST /operations/{opId}:cancel`

- `202` + status when the cancellation *request* is recorded (cooperative).
- `409` when already terminal; `405` when cancellation is disabled for the type.
- The status becomes `canceled` only when it is safe: immediately if still `not-started` (CAS `Created → Canceled`, so a late message is skipped); if `running`, `cancelRequested` is set and the handler decides whether to honour it. Business compensation for partially-done work is the handler's responsibility.

### 3.7 `OperationStatus`

```json
{
  "id": "…",
  "status": "not-started | running | succeeded | failed | canceled",
  "createdOn": "…",
  "updatedOn": "…",
  "expiryOn": "…",
  "cancelRequested": false,
  "percentComplete": 40,
  "message": "Processed 400 of 1000 lines",
  "error": { "code": "execution-failed", "title": "The operation failed." },
  "resultLocation": "/products/123"
}
```

Mapping from `WorkStatus`: `Created → not-started`; `Started`/`Indeterminate → running` (`Indeterminate` adds a non-fatal `message`); `Completed → succeeded`; `Failed`/`Expired → failed`; `Canceled → canceled`.

**Unknown outcome.** An operation that started but never reported back before `Expiry` may or may not have had its business effect. It is reported as `status: failed` with `error.code = "outcome-unknown"`, not as a new status value. Rationale: the guideline status set is what generic pollers (Azure SDK-style, hand-written loops) treat as terminal; an unknown sixth value would make them spin or crash. The error code makes the ambiguity explicit for clients that care. Never-started expiry uses `error.code = "not-started"` (definitely did not run).

### 3.8 API description (OpenAPI + AsyncAPI)

- **P2 OpenAPI** documents tracked publish as `202` + `OperationStatus`, `Location`, `Operation-Location`, `Retry-After`, a `getOperation` link, and long-running-operation extensions. Untracked publication is bodyless `202` without polling metadata. `MapCoreExOperations` documents status/result/cancel once; generic result JSON is intentionally untyped. `[PublishTracking]` and `.WithPublishTracking()` are the shared runtime/OpenAPI switch. Final `200`/`201` responses and wait/long-poll behavior are deferred.
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
                    W-->>C: 202 + OperationStatus(not-started); start deadline reconciles
                end
            end
        end
    end
```

- The tracked initiating message **must** be exactly one self-addressed **command** (one logical handler, competing consumers), routed by `NamedDestinationProvider` to `{Destination}-{domain}`. The operation state, status endpoint and processing belong to the accepting domain; another domain has no record of the operation, so a tracked message must not be an event (topic fan-out) or a command to another domain. Before creating state, tracked publishing rejects (`InvalidOperationException`) an explicit destination override, an event, a command whose `DomainName` differs (ordinal) from `IHostSettings.DomainName`, a `TenantId` that differs from the ambient tenant, or a message already carrying `operationid`; a null `DomainName` is set to the owning domain. The host must configure the publisher and any destination provider to route tracked commands to its own domain; custom publishers or providers may route differently. These rules apply **only** to tracked publishing: untracked (fire-and-forget) publishing may use destination overrides and emit any event or command to any domain, for example as a proxy broadcasting an event. The handler may publish ordinary downstream messages, but they do not inherit operation tracking and operation completion does not imply their consumers have finished; tracking fan-out/join is workflow orchestration (DTS/Durable Functions), not this feature.
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
| Publish threw, fence write failed | `202` + `not-started` | Replays the `202`; polling resolves to `running` or `failed`/`not-started`. |
| State create failed (before publish) | `503` problem details | Re-executes (attempt released); nothing was dispatched. |

P2 adds a separate idempotency-attempt release capability. The HTTP provider exposes it only during endpoint execution and honors it after response capture, using an owner/state/deadline-checked transition. `PublishAsync` requests release after a confirmed pre-dispatch failure or an acknowledged Created-only fence. Unsupported stores, lost ownership, and ambiguous release acknowledgements do not establish retry safety; ordinary service errors do not release an attempt.

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

**As implemented (P1).** The shape above was simplified; see the [CoreEx.Events README](../../src/CoreEx.Events/README.md#subscriber-behaviors) for the authoritative contract.

- `InvokeAsync(EventSubscriberArgs, SubscriberBehaviorDelegate, CancellationToken)` returns CoreEx `Result`; there is no separate context or `SubscriberOutcome` type. `EventSubscriberArgs` carries `Event` (null for native), `IsNative`, `Subscriber` (matched dispatched handler), `CloudEvent`, `AttemptCount` and `Properties`. The subscriber result is already classified through `ErrorHandler`, so behaviors read the `ErrorHandling` from the returned `EventSubscriber*Exception`. Short-circuits use `SubscriberBehaviorResult.Skip/Retry/DeadLetter`.
- Registration is per subscriber only (`EventSubscriberBase.Behaviors`, instance or `Add<T>()` resolved from the delivery's `ExecutionContext.ServiceProvider` and reused across retries); there is no host-wide DI registration or predicate. The pipeline runs per delivery attempt, so `MessageResiliency` retries re-enter it when they reach subscriber processing. `AttemptCount` starts at zero and increments before each in-process retry, including preprocessing failures, with or without behaviors; it is not the broker delivery count.
- A behavior exception (or a second `next` call) is classified `ErrorHandling.None`, never the subscriber's `UnhandledErrorHandling`.
- `SubscriberDisposition.Settlement` reports the settlement actually applied: `Completed`, `Abandoned`, `DeadLettered`, `NotSettled` (host cancellation, or a Functions failure rethrown to the host) or `SettlementFailed` (with `AttemptedSettlement`, `SettlementError`, `IsLockLost`). Catastrophic is a `Classification`, not a settlement; the Service Bus receiver abandons and reports `ReceiverPaused`. Callbacks run once per delivery, in reverse order, for participating behaviors only; failures are logged and never change settlement.
- Inbox (step 4) does not exist yet; a later phase implements it as a behavior.
- The Functions callback overload (`actions.FunctionReceiveAsync(message, entityPath, callback)` and `FunctionsServiceBusReceiveArgs`) was removed so every Functions receive path flows through `EventSubscriberBase`.

### 5.2 Operation metadata (which messages participate, and their work id)

- **Explicit per-message metadata** — CoreEx-defined CloudEvent extension context attributes:
  - `operationid` (String): the random operation id. Opaque.
  - `operationstartby` (Timestamp): first-start deadline.
- Set by tracked `PublishAsync` through `EventData.OperationId`, `OperationName`, and `OperationStartBy`, which are typed properties backed by `EventData.Attributes`; `EventFormatter` round-trips them as CloudEvent extensions without business DTO changes. Tracked-message boundary validation and mapping from `WorkState` remain owned by the WebApi acceptance path.
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
   - `Failed` with a completing `ErrorHandling` (business/validation) → `FailAsync(WorkError)`; the HTTP layer maps persisted errors to a sanitized `OperationError`.
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
- **Status lies.** An untracked run cannot record progress or completion; mid-run renew/complete writes that fail are logged and the handler continues (it is not abandoned). Status may show `404`, a stale `not-started`/`running`, or later `failed/not-started`/`outcome-unknown` for work that actually succeeded. Clients must treat these as unknown, not as proof of non-execution, and must not automatically resubmit. Retrying with a new idempotency key can dispatch duplicate work; retrying the same key only replays the original response if the separate idempotency record survived.
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
| Publish threw, fence write failed | `202` `not-started` → `running` or `failed/not-started` | Exactly one of first start or deadline expiry wins |
| Message first delivered after `StartBy` | `failed/not-started` | Worker skips without needing the store |
| 202 lost in transit | Same-key retry replays the 202 | Single dispatch |
| Peek-lock lost while handler runs; message redelivered | `running` | Live lease → redelivery deferred, no concurrent run |
| Worker crashes after business commit, before Complete | Lease expires → takeover → `running` → `succeeded` | Handler/inbox idempotency |
| Retries exhausted (dead-letter via CoreEx) | `failed/processing-failed` | Via `OnDispositionAsync` |
| Broker-autonomous dead-letter | `failed/outcome-unknown` at `Expiry` (or `not-started`) | No app callback; expiry covers it |
| Tracking state lost before first start | `404` | Handler runs untracked (default); a lost fence/cancel is not honoured. Fail-closed opt-in skips |
| Tracking state lost after handler start | `404`; outcome unknown | Handler continues; a redelivery may rerun it. Do not automatically resubmit based on `404` |
| Store unavailable at delivery | Stale status, later `outcome-unknown`/`not-started` | Handler runs untracked (default); overlap with a live worker possible. Fail-closed opt-in retries |
| Cancel while `not-started` | `canceled` | Late message skipped |
| Cancel races Complete | First terminal CAS wins | Unaffected |
| Never finishes | `failed/outcome-unknown` at `Expiry` | Late Complete ignored; effect may exist |

## 7. Security

- Ownership fails closed: owned operations only for the same authenticated caller; anonymous → 404.
- Per-type authorization on `MapCoreExOperations`, checked before disclosure.
- Ids random and opaque.
- `operationid` on a message is not authority: type/domain/tenant binding is verified against stored state (§5.2).
- Persisted failure details use `WorkError`; the HTTP layer maps them to sanitized `OperationError`s and does not expose raw exception messages.

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

1. **P0 complete:** engine amendments (§8), `OperationStatus`, and lifecycle/fence race tests.
2. **P1 complete:** `ISubscriberBehavior` pipeline on `EventSubscriberBase` with disposition callbacks in the Service Bus receiver and Functions adapter.
3. **P2 complete:** `WebApi.PublishAsync` publish-only default + optional validator + `MapCoreExOperations`; owner-checked idempotency release and tracked/untracked OpenAPI metadata.
4. **Deferred:** `OperationSubscriberBehavior` + `IOperationContext` + worker-side operation metadata handling.
5. **Deferred:** `Prefer: wait` + long-poll + change notification.
6. **Deferred:** Contoso sample endpoint, AI skills/instructions, Aspire E2E.

## 10. Open questions

1. ~~`ISubscriberBehavior` naming and whether `OnDispositionAsync` lives on the same interface or a separate `ISubscriberDispositionObserver`.~~ Resolved: D15.
2. ~~Behavior registration filter: all subscribers in the host vs per subscriber/handler type (proposed: DI registration with an optional predicate).~~ Resolved: D16.
3. Lease duration/renewal defaults relative to Service Bus lock duration (proposed: lease ≈ 2× lock renewal interval, renewed by heartbeat).
4. Generic `/result` OpenAPI typing (§3.3): per-type `oneOf` vs untyped vs dropping `/result` in favour of `resultLocation` only.
5. Default `StartTimeout` value (proposed 15 minutes for all hosts; outbox hosts raise it if relay lag can exceed it). No auto-detection of outbox publishers.
6. `Prefer: wait` on an endpoint configured for publish-only: P2 currently ignores the preference and never creates tracking; a later phase may choose explicit rejection.
7. `WorkOutcomeUnknownException` classification at the API boundary: decide whether it should become CoreEx `TransientException` (enabling WebApi 503/`Retry-After`) or remain an explicit unknown-outcome exception with boundary-specific handling. At the subscriber it is treated as store-unavailable (run untracked by default; transient only under fail-closed, §5.5). Any retry must re-read operation state; a missing tracking record does not prove that the handler did not run, and retry safety still depends on business-level idempotency.

## 11. Decisions

| # | Topic | Decision |
|---|---|---|
| D1 | Scope | Domain-owned operations resource and keyspace; no global service (§3.0). |
| D2 | Cancel verb | `POST /operations/{id}:cancel`; optional `DELETE` = forget, not cancel. Design choice following Azure/Google guidelines; no RFC mandates it. |
| D3 | Missing state or store unavailable at the worker | Proceed untracked by default (warning + metric); fail-closed per-subscriber opt-in. Accepted work completing beats tracking accuracy (§5.5). |
| D4 | Completion event for cross-domain consumers | Deferred. Domain events published by the worker's own business logic are the cross-domain signal. |
| D5 | Batch/collection accept | Deferred. |
| D6 | Idempotency | Developer marks the endpoint `[IdempotencyKey]`; not forced; templates/skills default it on. P2 offers an optional OpenAPI-generation trace warning when the metadata is absent; it does not enforce idempotency. |
| D7 | Start deadline | Single `StartTimeout` (default 15 min), not tied to broker TTL; CloudEvents `expirytime` not reused. |
| D8 | Unknown outcome | `failed` + `error.code = "outcome-unknown"`; no new status value. |
| D9 | Fence-won retry | Same key re-executes (attempt released, §8.11); no forced new key. |
| D10 | Worker integration | `ISubscriberBehavior` on `EventSubscriberBase` only; `OperationSubscribedBase` dropped. |
| D11 | Work id | Explicit `operationid` extension; not CloudEvent `id`, `correlationid`, `traceparent` or `dataref`. |
| D12 | API naming | Prefer action `WebApi.PublishAsync(...)` with `WebApiPublishOptions<TRequest[,TMessage]>`; alternatives and rationale in §3.1. |
| D13 | Tracking mode | `Operation` options default to absent: publish-only, no tracking, no operation metadata. Explicit `Operation` configuration opts into async request-reply ceremony. |
| D14 | Pre-validation | Optional `IValidator<TRequest>` on publish options; validate before state creation or publish in both modes. |
| D15 | Disposition callback | Single `ISubscriberBehavior` with a default no-op `OnDispositionAsync`; no separate observer interface. Only behaviors that participated in the delivery are notified (§5.1 as implemented). |
| D16 | Behavior registration | Per-subscriber `EventSubscriberBase.Behaviors` only; no host-wide DI registration or predicate filtering. |
| D17 | Behavior failure | A throwing behavior is classified `ErrorHandling.None` regardless of subscriber policy; disposition callback failures are logged and never alter settlement. |
| D18 | Functions callback overload | Removed (breaking, v4.4); all Functions Service Bus receives go through a CoreEx subscriber. |

## 12. Review: work-tracking approach and Durable Task Scheduler (proposal, not a decision)

Status: review appendix (21 Nov). Nothing in this section changes D1–D18 or the implemented P0–P2 baseline. It records an honest assessment of the tracking approach and a suggested direction for later phases. **Superseded in part by §13:** tracked operation state is proposed for the domain database; Redis remains for general coordination and bounded HTTP idempotency. §12.2's best-effort Redis positioning for work tracking and the DTS-as-status-store idea are not the current recommendation.

### 12.1 Assessment

The HTTP contract (§3) follows established practice and should stay: `202 Accepted` plus an operation status resource, as in the Azure Async Request-Reply pattern, the Microsoft REST API Guidelines long-running operations, ARM `Azure-AsyncOperation`, and Google AIP-151. Publish-only as the default (D13) is also right.

The tracking engine is the part that reinvents existing technology. Fencing, execution leases, first-start deadlines, cancel-request flags, missing-state policy and unknown-outcome classification (§4–§6, §8) are the core of a durable workflow engine. Built on Redis, they cannot be fully reliable:

1. **Redis is not a durable source of truth.** Eviction, failover or TTL expiry can lose a record, after which the outcome is unknown. Every guarantee then carries a caveat (§5.5).
2. **Fencing protects only systems that check the token.** A fencing token stops a stale worker only where the target store checks it. Business writes go to the domain database, which never sees the Redis token. The fence therefore protects the tracking record, not business effects. Business idempotency or an inbox provides the real duplicate protection.
3. **Most operations already have a better status source.** When the work creates or changes a business resource, that resource's own state (`pending`, `accepted`, `rejected`) is more accurate than a separate tracking record. Many production APIs simply return `202` with `Location` pointing at the resource.

| Approach | Examples | Relevance |
|---|---|---|
| Status resource over messaging | Azure Async Request-Reply; this design's P2 | Fine as a best-effort view. |
| Business resource as status | Typical order or payment APIs | Simplest; no extra store. |
| Durable workflow engine | Durable Task Scheduler (DTS), Durable Functions, Temporal, Dapr Workflow | Owns identity, status, retries, timers, cancellation and results durably. |
| Message-bus state machines | MassTransit/NServiceBus sagas, Wolverine | Same problem, solved inside the bus. |

### 12.2 Suggested direction

1. **Keep** `WebApi.PublishAsync`, the publish-only default, validation, `MapCoreExOperations` and the `OperationStatus` contract.
2. **Reposition `IWorkProvider` tracking as best effort.** It is an observability view with version-checked (CAS) status updates so a stale worker cannot overwrite a newer status. Document that it does not provide correctness. Principle 4 already moves in this direction.
3. **Do not build** execution leases (§8.12) or an enforced first-start fence (§4.1, §8.7) in later phases unless a concrete requirement survives review. Duplicate protection remains the business handler's responsibility (inbox and/or idempotent writes).
4. **Introduce a status-source seam** so the operations resource is not tied to `IWorkProvider`:

```csharp
public interface IOperationStatusSource
{
    Task<OperationStatus?> GetAsync(string operationId, CancellationToken cancellationToken);
    Task<OperationCancelResult> RequestCancelAsync(string operationId, CancellationToken cancellationToken);
    Task<OperationResultReference?> GetResultAsync(string operationId, CancellationToken cancellationToken);
}
```

Candidate implementations: `WorkProviderOperationStatusSource` (current Redis behaviour), a resource-backed source where the application maps its own entity state, and `DurableTaskOperationStatusSource` (§12.3). Owner and tenant authorization stays in `MapCoreExOperations`, above the source. The names are illustrative.

### 12.3 Durable Task Scheduler as the status source

CoreEx has no DTS integration today; `samples\src\Contoso.Order.Workflow.*` is an unfinished sample. This section therefore proposes a **new optional package** (for example `CoreEx.DurableTask`) built on `Microsoft.DurableTask.Client`/`Worker`. It would not be a dependency of `CoreEx.AspNetCore`.

**Identity.** The operation ID is the orchestration instance ID. It must be unguessable and scoped, for example `{domain}:{random}`. When the endpoint uses `[IdempotencyKey]`, derive the instance ID from `HMAC(secret, scope + idempotency key)` so a retried request schedules, or finds, the same instance. What the backend does when the instance ID already exists (reject, ignore or reuse) depends on DTS and the SDK version, and **must be verified in a spike** before relying on it.

**Start.** Call `DurableTaskClient.ScheduleNewOrchestrationInstanceAsync(name, input, new StartOrchestrationOptions(instanceId))`, then return `202` with `Operation-Location`. Store the owner and tenant in the orchestration input envelope (or tags, if supported) for the operations resource to check. DTS performs no CoreEx authorization.

**Status mapping** (`GetInstanceAsync(instanceId, getInputsAndOutputs: false)` → `OrchestrationMetadata`):

| `OrchestrationRuntimeStatus` | `OperationStatus.Status` | Notes |
|---|---|---|
| `Pending` | `not-started` | |
| `Running`, `ContinuedAsNew` | `running` | Progress from `SetCustomStatus` → `PercentComplete`/`Message`. |
| `Suspended` | `running` | `Message` indicates suspended; no new public status value. |
| `Completed` | `succeeded` or `failed` | A business rejection should be a typed output mapped to `failed` with a safe `OperationError`, not an orchestration failure. |
| `Failed` | `failed` | Map `FailureDetails` to a sanitized `OperationError`; never expose stack traces. |
| `Terminated` | `canceled` | Includes administrative termination; distinguish it with an error code. |
| Not found | `404` | Purged, expired or never existed. Same unknown-outcome rule as §5.5. |

`CreatedOn`/`UpdatedOn` map from `CreatedAt`/`LastUpdatedAt`; `ExpiryOn` from the configured purge/retention policy.

**Result.** For small results, use `ReadOutputAs<T>()` with `getInputsAndOutputs: true`. For anything else, return `ResultLocation` pointing at the business resource. DTS payloads have size limits, so the orchestration should pass references, not large data.

**Cancel.** Prefer cooperative cancellation: `RaiseEventAsync(instanceId, "cancel")`, which the orchestrator observes via `WaitForExternalEvent`, sets `CancelRequested`, and compensates if needed. `TerminateInstanceAsync` is the hard fallback. It does not stop an activity already executing and runs no compensation.

**`Prefer: wait`.** Use `WaitForInstanceCompletionAsync` with a linked cancellation token bounded by the configured maximum wait. This is the RFC 7240 behaviour from §3 without a custom change-notification mechanism (§8.6).

**Retention.** Use `PurgeInstanceAsync` or the scheduler's retention policy; `ExpiryOn` reflects it.

### 12.4 DTS as the processing engine instead of publishing to Service Bus

Yes, for some workloads. In this mode the endpoint schedules an orchestration instead of publishing a message, and activities call existing CoreEx application services. This removes the separate tracking store, fence, lease and start deadline: DTS owns them durably.

| Concern | Tracked Service Bus command + relational operation store (§13) | Schedule a DTS orchestration |
|---|---|---|
| Status, retries, cancellation and result | Relational operation state with bounded retention; delivery retries remain broker-managed | Built in and durable; supports durable timers and orchestration history |
| Accept-path atomicity | Operation state + outbox event in one domain-database transaction | Scheduling is a separate call. Replay safety for a deterministic instance ID must be verified. |
| Duplicate business effects | Delivery is at-least-once; transactional completion fences local writes, while external effects still need idempotency | Activities are at-least-once; external effects still need idempotency |
| Multi-step flows (validate, reserve, charge, compensate) | One command handler; not a workflow engine | Natural orchestration code |
| Fan-out to multiple consumers | Tracked commands are single-handler and self-addressed; untracked events support fan-out | Not a pub/sub bus; publish domain events from an activity |
| Cross-domain decoupling | Strong: untracked messages use CloudEvents (AsyncAPI); tracked commands stay in-domain | Weaker: caller and worker share a task hub and orchestration contract; keep it inside one domain |
| Programming-model constraints | No durable timers or workflow history; operation tracking is for bounded work, not scheduled waits | Deterministic orchestrator code; versioning of in-flight instances |
| Infrastructure and portability | Service Bus and the domain's relational database | Additional managed service; production backend is Azure DTS (verify alternatives); emulator exists for local development |

**Recommendation.** Use both, chosen per operation, and do not hide them behind one abstraction:

- **Fire-and-forget, events and cross-domain commands** → `WebApi.PublishAsync` to Service Bus (P2 as implemented, untracked by default).
- **Tracked, single-command, in-domain work** → use the relational operation store for acceptance, bounded processing, retries and pollable status/result. Its inactivity timeout is a delivery/recovery window, not a work-duration deadline; work should finish promptly when dependencies are available.
- **Work that needs durable waits or timers, multi-step orchestration, fan-out/join, human or external-event waits, compensation, or replayable execution history** → schedule a DTS or Durable Functions workflow and expose its status. Do not stretch operation inactivity expiry into a scheduling mechanism.

Cross-domain effects from an orchestration are still published as domain events through `IEventPublisher` (outbox where atomicity with business data is required) from an activity. Starting an orchestration from a Service Bus subscriber is possible (a cross-domain command arrives, then the owning domain orchestrates), but an HTTP endpoint inside the same domain should schedule directly rather than add a bus hop. The operation store's default 24-hour inactivity timeout allows for a substantial delivery/recovery outage; it does not mean work is expected to wait or run for 24 hours.

### 12.5 Questions for the user before any further phase

1. Should deferred worker-side tracking (`OperationSubscriberBehavior`, fence, leases) be descoped in favour of best-effort status plus a DTS option?
2. Is a DTS dependency (Azure-managed production backend) acceptable as an optional CoreEx package?
3. Should the API surface be `WebApi.ScheduleAsync(...)` (DTS) alongside `WebApi.PublishAsync(...)`, or a single entry point with a pluggable dispatcher? Separate methods are recommended: their semantics differ.
4. Spike first: verify DTS duplicate-instance-ID behaviour, payload limits, purge/retention and emulator parity before committing to the design.

## 13. Proposal: relational work state (Redis retained for coordination and HTTP idempotency)

Status: proposal (21 Nov), not yet an implementation; revised 22 Nov with the decisions below. The working direction is to make the domain's relational database the source of truth for tracked operation state, while retaining Redis for general synchronization leases and bounded HTTP idempotency. Redis-backed work state is not the target architecture. This deliberately revisits the "independent provider" stance of §1.2/§2: co-locating operation state with business data permits the transactional guarantees below when the same transaction is actually used.

**Decisions (22 Nov).** Operations, HTTP idempotency (`CoreEx.Idempotency`) and coordination (`CoreEx.Coordination`) are three independent capabilities; none references another, and work can be created without an HTTP endpoint. Operation state uses one purpose-built operations table, not a generic `StateStore`, and holds no idempotency or lease records. Create-or-get on the complete operation identity, committed with the outbox event, is the sole acceptance de-duplication guarantee. Endpoints derive the operation name and `CompositeKey`; they may use the `Idempotency-Key` value as an input, but that is endpoint code, not a library coupling. Ordinary `[IdempotencyKey]` response replay remains optional and is never the correctness guarantee for tracked work. Existing outbox stored routines remain unchanged; only new operation-store SQL uses embedded parameterized statements. Cosmos is deferred until the relational proof is complete. Text below that conflicts with these decisions has been revised; §13.7 records what remains open.

### 13.1 Why the database changes the answer

The weaknesses in §12.1 all stem from the tracking store being separate from the business store. Put the operation row in the domain database and the same transaction can cover it:

| Concern | Redis (separate store) | Domain database (same store) |
|---|---|---|
| Accept: create state + publish | Dual write; publish error = unknown delivery; fence + start deadline needed (§4.1) | Operation state + outbox event in **one transaction**. Broker publishing happens later in the relay. A definite rollback is not accepted; a lost commit acknowledgement is unknown. A retry deriving the same operation name and key resolves to the existing operation while it is retained (§13.4). |
| Completion vs business effect | Separate writes; status can disagree with reality | Business write + operation completion + domain events can commit in **one transaction**. This covers those local writes, not external effects or earlier commits. |
| Fencing concurrent redeliveries | Token not checked by business store (§12.1.2) | A conditional completion update is in the business transaction; a lost comparison must cause that transaction to roll back. This fences the writes in that transaction only. |
| Cancellation | Request flag; race with completion is ambiguous | Conditional updates can arbitrate cancellation and completion in the database. A cancellation request is not an acknowledgement, and it cannot undo external effects or prior commits. |
| Durability / eviction | Eviction, failover loss, TTL | Normal database durability and explicit retention/purge; this is still a bounded guarantee. |
| Work-state store | Redis | No additional service where the domain already has a suitable database and outbox. |

Limits that remain, stated honestly:

- Only effects inside the **same database transaction** are covered. Calls to other domains (HTTP adapters, e.g. Shopping's inventory reservation) are outside it; those still need business idempotency or compensation.
- Delivery is still at-least-once. A redelivery that finds retained terminal state can be skipped, but concurrent deliveries may both start before either completes. A local completion fence does not prevent duplicate external calls.
- A long-running handler may claim work in a short independent transaction and need a renewable lease for recovery. Takeover and completion must check the current owner/attempt and expiry atomically; a timestamp alone is not a fence. Progress, heartbeat and cancellation can change the row version, so completion must account for those changes rather than blindly reuse an old ETag.

### 13.2 Operations table (relational)

Use one purpose-built, framework-owned operations table per domain database (provider-specific naming and schema are configurable). It stores only operation state: no idempotency receipts, no general `Lease` records and no other record kinds. A generic JSON `StateStore` was considered and rejected (YAGNI): a future feature with a real requirement gets its own purpose-built table. Existing outbox tables remain separate. `ISynchronizer` remains Redis-backed; the operation's execution claim belongs to the operation row and its transaction rules.

| Column | Purpose |
|---|---|
| `KeyHash` | Primary key: versioned, length-framed SHA-256 of the complete operation identity (domain, tenant presence and value, operation name, instance ID), preserving exact ordinal identity including case and trailing spaces independently of database collation. The encoder is introduced with the relational provider (Phase 2) and must not be lossy: strings containing unpaired surrogates must be rejected or encoded losslessly, because UTF-8 replacement maps distinct strings to the same bytes. |
| `OperationId` | Unique GUID for this incarnation; a new value on recreation after purge, fencing stale messages and links. |
| Scope columns | Domain, tenant, operation name and instance ID, stored for authorization, diagnostics and queries; identity is enforced by `KeyHash`. |
| `Status` | Indexed lifecycle status used by atomic transitions and active/purge queries: `Accepted`, `Running` (claimed), `Retrying` (claim released after a retryable error, awaiting redelivery), `Succeeded`, `Failed`, `Cancelled`, `Expired` (abandoned: no activity before `AbandonAfter`, e.g. after broker dead-lettering or a lost message). A stalled run is `Running` with an expired claim, determined by database time. |
| Initiator | Stable initiating principal used by default status/result authorization. |
| Claim columns | Attempt, claim owner and claim expiry for the renewable execution claim. |
| Cancellation | Cooperative cancel-request flag/timestamp. |
| Progress, error, result link | Safe projections only; result payloads stay with the application, which exposes them via a reader or resource link. The error pair holds the most recent safe error: the retryable error recorded on release (retained while a later attempt runs) or the terminal error on `Failed`; success or cancellation clears it, and expiry retains it. |
| `Data` | Opaque application JSON; not queried by the store. |
| `CreatedOn`, `UpdatedOn`, `CompletedOn`, `PurgeAfter` | Database-observed UTC timestamps; `PurgeAfter` is the earliest eligible purge time for terminal operations. |
| `AbandonAfter` | Inactivity deadline for unfinished operations: now + `InactivityTimeout` on create and release, claim expiry + `InactivityTimeout` on claim and renew (so a held claim is never expired); cleared when finished. `InactivityTimeout` defaults to 24 hours and must exceed the broker's full redelivery window. It is a delivery/recovery window, not a work-duration or scheduling deadline; use DTS or Durable Functions for durable waits, timers or long-running orchestration. |
| Concurrency token | Opaque, provider-specific ETag; see the PostgreSQL `xmin` limitations below. |

`KeyHash` is the only identity key; raw keys and secrets are never placed in key columns. Database comparisons must not inherit a case-insensitive collation for identity. SQL Server uses `RowVersion`; PostgreSQL uses `xmin`, accepting that it is transaction-scoped and can repeat after transaction-ID wraparound. It is not a unique per-mutation or per-incarnation token. `OperationId` distinguishes incarnations, while the claim owner, attempt and expiry provide the execution fence.

**Composite instance identity.** The instance ID is `CompositeKey.ToCanonicalString()` (the validated form of the default `ToString()`; typed and configurable formatters were removed): component values are invariant-formatted without CLR type tags and joined with a fixed `~` without escaping; the result is one-way and replay-stable (a canonical string used as a single-component key is unchanged). The tilde is URI-unreserved, avoiding percent-encoding when keys are used in URLs, and is absent from built-in scalar formats. Thus `123` and `"123"` are the same component, and case and trailing spaces remain distinct. Empty keys, null components, NUL, unpaired surrogates and `~` within a multi-component key are rejected. A single-component key's canonical form equals its string value, so a stored instance ID round-trips through `CompositeKey.Create(instanceId)`. Canonical output is not secret and is not itself the full operation `KeyHash`; the relational provider must still frame and hash the complete identity.

**Identity resolution (implemented).** `OperationKey` and `OperationKeyScope` were removed. Operations, work, coordination and HTTP idempotency take an operation name and a `CompositeKey`; the store or orchestrator qualifies them with `IHostSettings.DomainName` and the ambient `ExecutionContext.TenantId`. A present tenant is an explicit isolation boundary (tenant-scoped); otherwise the identity is domain-wide, and there is no tenant-present domain-wide mode. Processing hosts must establish the same ambient tenant as the accepting host; the published message conveys the operation domain and tenant through the standard `EventData.DomainName` and `EventData.TenantId` (`tenantid`), not duplicate operation attributes; only `operationid`, `operationname` and `operationstartby` are added.

Index unfinished operations by `AbandonAfter` and terminal operations by `PurgeAfter` only. Never purge active work; unfinished work is first expired, then purged after retention. Use database-observed UTC time for claim and expiry decisions, not `Runtime.UtcNow`, which can be frozen for an execution context. For claims and fenced transitions, observe SQL Server `SYSUTCDATETIME()` or PostgreSQL `clock_timestamp()` **after** acquiring the row lock; statement-start time can be stale after a lock wait. Other PostgreSQL statements may use `statement_timestamp()`, not transaction-start `now()`.

**Schema provisioning.** CoreEx owns provider-specific operations templates as embedded resources in its database-provider packages. DbEx 3.5.0 discovers registered templates in `--help` and exports a timestamped migration with `script operations`. The application reviews and checks in that snapshot; its Database project embeds `Migrations/**` and must be rebuilt before DbEx `Migrate` can apply it. Applied migrations are immutable; package upgrades require a new migration rather than replacing one. DbEx remains feature-agnostic and does not execute SQL during export.

All new operation-store runtime operations, including execution claims and cleanup, use parameterized SQL statements embedded in the provider assembly and loaded with `SqlStatement.FromResource<T>()`; there are no new stored procedures or stored functions. Execute through the appropriate `IDatabase`, with values passed as parameters and any configurable schema identifier validated and correctly quoted. This follows the existing `ProductSearchIndex` embedded-statement pattern. The existing SQL Server and PostgreSQL outboxes keep their own stored procedure/function; replacing them or migrating their provisioning is out of scope.

### 13.3 Components

| Component | Package | Responsibility |
|---|---|---|
| Relational `IOperationStore` (SQL Server, PostgreSQL) | Provider-neutral base in `CoreEx.Database`; implementations and embedded runtime/schema SQL in `CoreEx.Database.SqlServer` and `CoreEx.Database.Postgres` | Purpose-built operation lifecycle (create-or-get, claim/renew/release, fenced completion, cancellation, purge) over the operations table, with transaction-aware access for acceptance and completion. Provider-owned schema resources are exported as checked-in application migrations through DbEx's generic `Script` command. Replaces the `IWorkProvider`/`WorkState` path once proven. |
| Redis `ISynchronizer` and `IIdempotencyStore` | `CoreEx.Redis` (retained) | General renewable coordination leases and bounded HTTP idempotency response replay. Independent of operations. |
| Operation cleanup | Provider packages | Batched `CleanupAsync` pass: first expires unfinished operations past `AbandonAfter` (logging a warning per operation, so expiry is alertable), then purges terminal operations past `PurgeAfter`. Never purges active work. |
| `CosmosDbWorkProvider` | `CoreEx.Cosmos` | Deferred until after the relational proof; may not be supported (§13.6). |

There are distinct **storage and transaction roles**. Acceptance and atomic completion must join the exact connection and transaction used by the business unit of work and outbox. Execution claims/heartbeats use short independent relational operations and must not enlist in a long-lived business transaction. General coordination leases remain the responsibility of `ISynchronizer` (Redis); HTTP middleware idempotency remains the responsibility of `IIdempotencyStore` (Redis). Neither participates in operation acceptance or completion.

The intended tracked flow is:

1. **Accept:** in one database transaction, create-or-get the operation by its identity and, only when newly created, enqueue the command through the same-database outbox. Commit before returning `202`; an existing operation returns `202` for its `OperationId` with no second message. The relay later publishes to Service Bus.
2. **Claim:** if needed, claim or renew the operation in a separate short transaction, with an owner/attempt token and an atomic expiry check.
3. **Complete:** in the business transaction, verify ownership and cancellation, write business data, enqueue domain events, and conditionally transition the operation to its terminal state. If the transition loses its comparison, abort the transaction; a zero-row update must never be treated as success.
4. **Retry:** on a retryable (transient) error, roll back the business transaction, then release the claim with a safe error, moving the operation to `Retrying`, and let the broker redeliver; the next claim increments `Attempt`. A permanent error completes as `Failed` and the message is settled without retry. Broker max-delivery dead-lettering (or a lost message or crashed worker) leaves an operation `Retrying`, `Accepted` or stalled `Running`; cleanup marks it `Expired` once `AbandonAfter` passes. As with Temporal's and Step Functions' timed-out status, expiry is a distinct terminal outcome rather than `Failed`, and it does not prove that no business effect committed. A later redelivery or dead-letter resubmission finds the operation finished and must be settled without executing. Optionally, the worker can detect the final delivery attempt and complete as `Failed` (`RetriesExhausted`) immediately instead of waiting for expiry (Phase 3).

The `EventSubscriberBase` behavior seam remains a candidate for coordination, but it does not automatically make a handler's own unit of work, connection, nested transactions or external calls participate. Verify those boundaries before selecting an integration; application services may need to own the transactional completion explicitly.

Consequences for earlier sections when tracked mode uses a database provider:

- The accept-path fence, `operationstartby` and `StartTimeout` (§4.1, §8.7–8.9, D7) are not needed to atomically create state and enqueue in a single transaction. Operational deadlines may still be useful.
- Execution leases (§8.12) may still be needed for long-running work and takeover; they are not a substitute for checking ownership in the committing transaction or for business idempotency.
- Missing operation state is fail-closed: the worker does not execute the message. Missing state can still mean expiry or lost tracking after earlier execution; it does not prove that business effects did not occur.
- CoreEx recommends an outbox enlisted in the same database transaction for tracked acceptance. A direct publisher reintroduces the dual write; CoreEx does not enforce the recommendation, and an application that chooses a different configuration owns that risk. No equivalent atomicity guarantee is claimed for that configuration.

### 13.4 Idempotency

HTTP idempotency and operation acceptance are separate. Redis-backed `[IdempotencyKey]` support provides **basic, bounded HTTP idempotency**: it suppresses duplicate requests and replays retained responses, but eviction, failover, data loss and retention expiry can lose that guarantee, and its receipt cannot commit atomically with a relational transaction or outbox. It is therefore never the correctness guarantee for tracked work.

Recommendation, by endpoint kind:

1. **Tracked publish endpoints:** the endpoint derives a deterministic operation name and `CompositeKey` (a business identity is preferred; the `Idempotency-Key` value may be used as an input in endpoint code). Create-or-get on that key, committed with the outbox event, prevents duplicate operations and duplicate messages for retries of the same key. There is no separate idempotency record, fingerprint comparison or Redis claim for the same request. `[IdempotencyKey]` may additionally be applied for ordinary response replay; it is optional and independent.
2. **Untracked publish endpoints:** unchanged. Ordinary Redis-backed HTTP idempotency can provide bounded response replay, but it does not make the receipt atomic with the outbox write. The relay and subscribers can still deliver or execute more than once.
3. **Ordinary mutating endpoints (`POST` create etc.):** unchanged; keep the existing `IIdempotencyProvider`/`IIdempotencyStore` integration backed by Redis for bounded replay.

After an unknown commit outcome, a client retry that derives the same operation name and key reconciles to the existing operation while it is retained; this is a property of the operation key, not of HTTP idempotency. After terminal retention expires, the same key creates a new operation (new `OperationId`); a missing operation is not proof that earlier effects did not happen.

### 13.5 Redis remains for coordination and ordinary HTTP idempotency

Redis is **not** being removed from CoreEx or reduced to a FusionCache dependency:

- `ISynchronizer` (`RedisCoordination`) remains the provider for general distributed leases, such as synchronized timers and singleton coordination. Lease loss is cooperative unless the protected resource checks an appropriate fencing token. This is distinct from any operation-execution claim needed by tracked work, which belongs to the relational work-state design.
- `IIdempotencyStore` (`RedisCoordination`) remains available for bounded HTTP idempotency and response replay, with the durability and retention limits above. It plays no part in tracked acceptance, which relies on operation create-or-get (§13.4).
- FusionCache's Redis backplane/L2 remains caching, independently of these state and coordination uses.

The relational-first refactor should remove `RedisWorkProvider` as the tracked-work implementation after the relational provider is proven end to end; it should **not** delete `CoreEx.Redis`, `RedisCoordination`, or their registration from hosts that still need distributed leases or ordinary HTTP idempotency.

### 13.6 Cosmos

The single relational table does not translate to a single generic Cosmos container with the same transaction guarantees. `TransactionalBatch` is restricted to **one container and one logical partition**. Operation, business and outbox documents can be atomic only when all participating documents are deliberately placed there; a separate state container cannot join that transaction. The current Cosmos outbox also requires an active unit of work bound by an enlisted mutation and a `/partitionKey` container path.

If the business partition is not known at acceptance, atomic completion with that business record is not available without a different design. Similarly, an operation identity cannot be made unique across arbitrary partitions by a single partition-local batch. Either require an appropriate partition and scope, or explicitly design a weaker multi-write flow; do not present the latter as equivalent. General synchronization leases remain separate from this deferred Cosmos work-state question and continue to use the configured `ISynchronizer`.

### 13.7 Questions for the user

Resolved (22 Nov): HTTP idempotency does not integrate with acceptance (former Q3; §13.4); SQL Server and PostgreSQL ship first and there is no `IStateStore` (former Q4; §13.2–13.3); existing outbox routines are not replaced (former Q6); Cosmos is deferred until after the relational proof (former Q5).

Resolved (23 Nov): use a same-database transactional outbox as the recommended tracked-acceptance configuration, without enforcing it in the API; only work found in the operation store may execute, while `NotFound` and finished work are settled without invoking the handler; broker message TTL is an optional expiry hint where supported and never the correctness mechanism; relational store implementations belong in the provider packages, with a provider-neutral base in `CoreEx.Database`; CoreEx owns provider-specific schema resources, exported as checked-in application migrations through DbEx's generic `Script` command. DbEx 3.5.0 provides feature-agnostic named-resource discovery, `--help` listing, defaults and overrides.

No user decisions are currently open in this section; implementation details and relational proof outcomes remain for Phase 2.

### 13.8 Phase 2 implementation notes

Phase 2 delivers the schema and stores only; HTTP acceptance, subscriber integration and the end-to-end proof follow in later phases. `RedisWorkProvider`, Redis coordination and HTTP idempotency are unchanged.

- **Types.** `DatabaseOperationStoreBase<TDatabase>` (`CoreEx.Database.Operations`), whose primary key is the provider-neutral `OperationStoreBase.ComputeKeyHash`, the `CompositeKey.ToCanonicalHash` of (domain, tenant, operation name, instance ID) (reusable by future non-relational stores); `SqlServerOperationStore` (`AddSqlServerOperationStore`) and `PostgresOperationStore` (`AddPostgresOperationStore`). Runtime statements are embedded `Resources/Operations/Operation*.sql|pgsql` files loaded with `SqlStatement.FromResource<T>()`; the only substituted text is the table identifier and column list, delimited and escaped by `IDatabase.QuoteIdentifier`. State rows are read through `DatabaseRecord`, with timestamps read as UTC regardless of the configured `DateTimeTransform`.
- **Naming.** The store schema defaults to the outbox convention (`DomainName`; PostgreSQL `snake_case`), the table to `Operation` / `operation`. The store escapes any name, but the DbEx export does not, so use simple identifiers; if the default domain name is unsuitable, such as a dotted name, pass `schema` to the constructor or `AddSqlServerOperationStore` / `AddPostgresOperationStore` registration.
- **Schema resources.** `Resources/ScriptOperations_sql.hbs` and `Resources/ScriptOperations_pgsql.hbs` conditionally create the schema and create one table. Import `CoreEx.Database.SqlServer` or `CoreEx.Database.Postgres` and register `AddAssembly<SqlServerDatabase>()` or `AddAssembly<PostgresDatabase>()` before `AddAssembly<Program>()`, then run `dotnet run -- script operations` from the Database project. Construct the migration console directly: `Create<Program>()` registers the application too early, and DbEx ignores duplicates while probing in reverse registration order. The schema defaults to top-level `schema` in `dbex.yaml`, or the literal `Schema` when absent; the table defaults to `Operation` / `operation`. Override both with `script operations <Schema> <Table>` or `script operations -p Param1=<Schema> -p Param2=<Table>`. Names must match the store and use simple identifiers; template substitution does not escape SQL delimiters. Review and check in the export, embed it in the Database assembly and rebuild before `Migrate`. A same-second filename collision fails without changing the existing snapshot.
- **Identity.** `KeyHash` is `CompositeKey.ToCanonicalHash`: a versioned SHA-256 over per-component presence bytes and length-framed strict UTF-8 (a null tenant differs from an empty one; unpaired surrogates are rejected). Every lookup also verifies the stored scope columns ordinally, so collation never affects identity.
- **Time and ETag.** All claim, expiry and timestamp decisions use database time. Claim, renew, release and complete observe time after locking the operation row, so waiting past claim expiry cannot revive a stale fence or issue an already-expired claim. Durations are passed in milliseconds and sub-millisecond values round up. The SQL Server ETag comes from the database-managed `RowVersion` column; PostgreSQL uses the system `xmin` column, both read through the provider `RowVersionConverter`. PostgreSQL's `xmin` identifies the writing transaction, not each write: multiple writes in one transaction can share it (creation itself inserts and then updates), and transaction IDs can wrap around. Consequently it is not guaranteed to change on every mutation or recreation; a recreated operation receives a new `OperationId`, but not a guaranteed distinct ETag. Execution fencing uses `OperationId`, owner, attempt and expiry, not the ETag. See [PostgreSQL system columns](https://www.postgresql.org/docs/current/ddl-system-columns.html).
- **Transactions.** The provider-neutral C# store owns transaction control: mutations join an existing explicit `DbTransaction` (`IDatabase.UseTransaction`/`IUnitOfWork.TransactionAsync`) or begin, commit and roll back one when called without an existing transaction. Embedded SQL contains no transaction-control statements. Create and complete use the scoped `IDatabase`, allowing acceptance/outbox and business/completion writes to share the caller's transaction; claims, renewals, releases and cancellation use an independent connection and their own short transaction. Cleanup uses bounded single-statement mutations on an independent connection. SQL Server clones the scoped connection; PostgreSQL opens a connection from the registered `NpgsqlDataSource`; `OperationDatabaseFactory` overrides both. `System.Transactions`, `TransactionScope`, ambient enlistment management and distributed transactions are not supported. Do not claim an operation inside the transaction that created it before commit: the independent claim blocks on the uncommitted row.
- **Concurrency.** Create-or-get uses `UPDLOCK, HOLDLOCK` (SQL Server) or `ON CONFLICT DO NOTHING` (PostgreSQL). SQL Server observes the creation time after locking the key range. PostgreSQL finalizes a newly inserted row's timestamps after any conflicting insert resolves, within the explicit transaction owned by the C# store; existing rows are not changed. This preserves the full inactivity window even when a competing creation rolls back after a long wait. If a concurrent purge removes a conflicting row before it can be read, the store re-reads or raises an error the caller can retry. Cleanup batches use `READPAST` / `SKIP LOCKED` to skip contended rows; SQL Server page locks may still block.
- **Tests.** The shared `OperationStoreTestsBase` suite runs against InMemory, SQL Server and PostgreSQL, using a separate store per concurrent caller for relational providers. Provider fixtures exercise DbEx discovery, defaults, overrides, safe failures, embedded migration application and journaling, as well as distinct tables, explicit-transaction rollback, `IUnitOfWork.TransactionAsync` commit and rollback of business writes with operation create and completion, PostgreSQL store-owned transaction rollback on failure, claim isolation and database-time decisions under lock contention.
