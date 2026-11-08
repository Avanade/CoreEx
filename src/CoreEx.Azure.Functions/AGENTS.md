# CoreEx.Azure.Functions — AI Usage Guide

## Purpose and boundaries

Azure Functions isolated-worker integration for CoreEx execution context, existing MVC WebApi HTTP handling, explicit HTTP idempotency and classified Service Bus settlement. Functions owns routing, bindings, scheduling, retries, listener lifecycle and scaling. Do not install a second CoreEx receiver or imitate ASP.NET endpoint middleware on the worker.

`coreex-functions` scaffolds an empty host and matching `*.Test.Functions` project. Shared-core mode references existing Application/Infrastructure libraries; standalone mode has no such requirements. Both follow effective CPM with inline fallback when disabled. New standalone repositories use `coreex-solution` for root conventions/Test.Common; the optional `--test-common-project` wires its test-only reference. Thin triggers delegate to shared services or capability-local handlers. Multiple hosts are independent deployments; test names derive from each full host name.

## Invocation context

Correlation is automatically captured for the existing `HttpRequest`/`IActionResult` integration before the invocation callback, returned through deferred `OnStarting`, and reflected in worker-side logging scopes and `correlation.id`. Null remains absent; malformed HTTP headers return safe 400 before the function. Native `HttpRequestData`/`HttpResponseData` and Functions host-process logs are outside coverage. Service Bus subscribers/raw callbacks import message correlation; invalid metadata warns, clears ambient correlation and still processes. Raw callbacks without host logging services supply `FunctionsServiceBusReceiveArgs.Logger`. Idempotency replay must echo the current correlation, never the cached original. See [workflow correlation](./README.md#workflow-correlation).

Register `AddExecutionContext()` and call `UseCoreExExecutionContext()` on the worker builder. For HTTP, call `ConfigureFunctionsWebApplication()` first. The middleware initializes/clears ambient invocation state, supports custom context factories and adds HTTP informational headers before rendering.

The optional `UseCoreExExecutionContext((context, executionContext, cancellationToken) => ...)` callback is singleton-owned: resolve scoped dependencies via `context.InstanceServices`, not captured root services. Request headers, baggage, message identifiers and function keys do not authenticate a tenant/user automatically.

## HTTP

Register `AddMvc()` and `AddMvcWebApi()`, then use `CoreEx.AspNetCore.Mvc.WebApi` with native `HttpRequest` / `IActionResult`. Choose `ConvertUnhandledExceptionsToProblemDetails = true` when unexpected exceptions inside WebApi should be sanitized. Exceptions outside WebApi and non-HTTP failures propagate; this package is not universal HTTP exception middleware.

```csharp
var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.AddHostSettings();
builder.Services.AddExecutionContext();
builder.Services.AddMvc();
builder.Services.AddMvcWebApi((_, webApi) => webApi.ConvertUnhandledExceptionsToProblemDetails = true);
builder.UseCoreExExecutionContext();
builder.Build().Run();
```

Host settings must be configured; imports belong in `GlobalUsing.cs`. Register application/local handlers and their dependencies only when required. An empty host does not need MVC, health functions or speculative dependency wiring.

`webApi.FunctionWithIdempotencyAsync(request, ct => webApi.PostAsync(..., cancellationToken: ct), isRequired: true, cancellationToken: cancellationToken)` wraps a deferred operation with the supplied WebApi instance and an explicitly registered `IIdempotencyProvider`. Configure its cache as usual; nothing selects a cache automatically.

The helper renders inside the capture boundary and returns `EmptyResult`; return it directly. Worker middleware around `next(context)` or `[IdempotencyKey]` alone does not capture the later ASP.NET proxy rendering. Do not use this helper for streaming, extra HTTP output bindings or already-started responses.

Existing policies apply: required/invalid key 400, overlapping duplicate 409, different fingerprint 422, successful 2xx replay. Default retention is eight hours and replay limit 512 KB; oversized successful responses cannot replay. Fingerprints exclude query strings; configure tenant/caller key isolation. Redis L2/backplane alone is not cross-instance mutual exclusion. Business invariants still need transactional enforcement.

## Service Bus

Both `subscriber.FunctionReceiveAsync(message, actions, entityPath, ..., cancellationToken)` and `actions.FunctionReceiveAsync(message, entityPath, handler, ..., cancellationToken)` require `AutoCompleteMessages = false`.

Use `ServiceBusSubscribedSubscriber` for CloudEvents dispatch; use `ServiceBusSubscriberBase<T>` for uniform JSON with validation/error classification, without CloudEvents/subject routing. A native callback owns parsing, logging and classification. Choose poison policy explicitly: default validation/business handling can complete as error rather than dead-letter. Set typed subscriber `InvalidDataHandling` and `ErrorHandler` to the intended dispositions.

| Outcome | Bridge behavior |
|---|---|
| Success / classified complete | Complete once. |
| Retry | Abandon once, no processing retry loop. |
| DeadLetter | Dead-letter once with bounded metadata. |
| Unclassified / catastrophic failure | Propagate original error, no bridge settlement. Catastrophic does not pause the listener. |
| Invocation cancellation | Propagate, no bridge settlement. |
| Settlement failure | Propagate, no second settlement attempt. |

Share trigger entity constants/expressions with `entityPath`, including topic/subscription paths. Expressions like `%Topic%/Subscriptions/%Subscription%` resolve from ambient worker `IConfiguration` for metrics; missing/malformed settings fail before processing. This label does not select the listener or settle a different entity. The Functions host must receive consistent binding settings too.

Receive enrichment uses the existing activity/producer links without replacing the worker span. Baggage is bounded and opt-in (`PropagateBaggage` on the subscriber or `FunctionsServiceBusReceiveArgs` for callbacks), not authentication. Choose telemetry sampler/exporter separately; CoreEx does not install one.

Prefer a Subscribe host when receiver-level backpressure, circuit-breaker pause/resume or its full receiver resilience policy is required. Native single-message non-session delivery is proven; sessions/batches need requirements-specific verification.

## Native Blob / Timer and reliability

Use native binding extensions and SDK types. No CoreEx Blob base class, synthetic CloudEvents wrapper or competing timer scheduler is required. Configure binding/entity/schedule connections as Functions host settings; worker `appsettings.json` alone is not sufficient.

Separate explicit content rejection from operational failure. Preserve cancellation and let failed I/O propagate. A BlobClient sees the current object, not necessarily an event's historical revision; use conditional ETag reads or versions/snapshots when needed. Deterministic outputs aid replay but do not make Blob-to-Service-Bus sends transactional. Output bindings are not an outbox.

Blob poison queues, Timer retry behavior, Event Grid retries and Service Bus broker delivery limits are different mechanisms. Do not promise uniform retries or exactly-once. Provision output containers/subscriptions separately.

For schema-defined CSV, use File CodeGen rather than hand-authoring wire models, mappings, or validators. The framework test-host example is driven by `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml`; generated types stay in `InventoryImports/Files`, while the handwritten processor partial owns trusted context and report hooks. The native Blob trigger uses Azure Storage SDK clients, durable Append Blob reports, and a supported SQL Server unit of work that commits item changes, receipts, and outbox writes atomically. Its Aspire proof uploads input to Azurite and posts a storage-shaped event through the Functions webhook; `EventGridExtensionMethods.CreateBlobCreatedEvent` simulates only notification delivery because Azurite does not emit Event Grid events. This local test does not verify Azure Event Grid provisioning, authentication, or delivery.

## Testing and discovery

Use UnitTestEx 6 `FunctionTester.Create<TFunction>()` against the real host; `HttpTrigger<T>()` / `Trigger<T>()` run worker composition and middleware. Deferred results use `ToHttpResponseMessageAssertor(request)`. Eager idempotency output uses `SnapshotHttpResponse(response)` with a caller-owned readable/seekable response buffer; it preserves stream ownership/position and does not execute the result again.

Test exact outcomes, error/cancellation propagation and settlement actions/counts. Invocation tests do not prove bindings, broker redelivery, authorization or cloud provisioning. Core Tools/Aspire runtime tests exercise those boundaries locally; simulated Azurite Event Grid notifications do not prove Azure subscriptions or authentication.

For creation/modification use `/coreex-functions`; for the empty host use `/coreex-scaffold`. Scoped Functions instructions and the skill ship in the same version-pinned AI bundle as this guide.
