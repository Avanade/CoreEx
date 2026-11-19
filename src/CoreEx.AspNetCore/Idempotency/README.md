# CoreEx.AspNetCore.Idempotency

> Provides the ASP.NET Core integration for idempotency keys. The HTTP provider uses a host-neutral `IIdempotencyStore`; choose Redis-backed storage or an explicitly process-local implementation.

## Overview

`IdempotencyKeyMiddleware` intercepts endpoints marked with `[IdempotencyKey]` and delegates request execution to `IIdempotencyProvider`. Register the HTTP integration with `AddIdempotencyProvider()` and register its storage independently. For a multi-instance deployment, use `CoreEx.Redis`; use `AddInMemoryCoordination()` only for tests or intentionally single-instance applications.

```csharp
builder.AddRedisDistributedCache("redis");
builder.Services
    .AddDefaultCacheKeyProvider()
    .AddRedisCoordination()
    .AddIdempotencyProvider();
```

FusionCache and `IHybridCache` remain for ordinary caching and are not the idempotency store. Preserve an existing custom `ICacheKeyProvider`; it supplies the domain/tenant scope used by coordination.

## Outcome policy

Completed responses, including normally rendered application errors, are replayed. A thrown exception, cancellation, or rendering failure after application execution begins may have an unknown business outcome; that attempt is retained as indeterminate and is not automatically executed again. Do not generate a new key to bypass an indeterminate outcome. Reconcile only after verifying that the original work can no longer commit.

CoreEx HTTP clients emit a quoted Structured Field string value. `StrictHeaderValidation` defaults to `false`, accepting standard quoted strings and supported legacy bare keys; set it to `true` to accept only standard quoted strings. The accepted logical key is scoped through `ICacheKeyProvider`; key identity is not a substitute for authorization, which must run before replay.

Idempotency is not exactly-once business execution. Redis failover to stale data, data restoration, eviction, manual deletion, or retention expiry can erase claims or receipts and permit re-execution. A process-local store does not coordinate across processes.

## Request identity and responses

The standards baseline is [draft-ietf-httpapi-idempotency-key-header-08](https://www.ietf.org/archive/id/draft-ietf-httpapi-idempotency-key-header-08.html), an Internet-Draft rather than a published Idempotency-Key RFC. Quoted keys use its Structured Field string representation; CoreEx requires 8-128 decoded printable ASCII characters and rejects parameters. Relaxed mode additionally accepts bare keys of 8-128 ASCII letters, digits, hyphens or underscores. Equivalent bare and quoted values identify the same operation. Duplicate/list headers and malformed quoted values are rejected in both modes. Replaying completed application errors is CoreEx's explicit policy, not a claim that draft-08 requires replay of every HTTP failure.

The record key incorporates the idempotency key and, when available, the authenticated caller's issuer and subject, before the store applies `ICacheKeyProvider` domain/tenant qualification. Configure `CallerIdentityProvider` when authentication uses another stable identity or when an application needs different caller boundaries. Anonymous callers and authenticated callers without a supported subject claim have no inferred caller component. Run authentication and authorization before the idempotency boundary, including in Functions.

The fingerprint covers method, case-preserving path, raw query string, selected canonical headers and body. Different query order can conservatively conflict. `HttpRequestHeadersToIncludeInFingerprint` defaults to Content-Type; include any additional request headers that change the operation's meaning. Correlation is excluded by default. A reused key with a changed fingerprint conflicts instead of creating another record.

| Condition | Response |
|---|---|
| Required key missing or invalid header | 400, before application execution. |
| Active matching attempt | 409 `is-in-progress`, with transient retry guidance. |
| Changed request for an existing key | 422 `different-request`. |
| Normally completed application response | Original status, replayable headers and body, including application errors. |
| Unknown outcome after execution began | 409 `outcome-indeterminate`; no automatic takeover. |
| Completed response above the replay limit | 409 `response-unavailable`; do not execute again. |
| Backend failure or uncertain acquisition/completion | Infrastructure failure, normally 503; never treated as a cache miss. |

Set `DocumentationUri` on `IdempotencyProvider` to advertise the resource's policy using `Link: <uri>; rel="describedby"`. Header-validation and authorization rejections before application entry do not consume an application outcome. Once the protected delegate is invoked, its returned error status does not establish that no business effects occurred. An unhandled exception is indeterminate; a normally rendered application 500 is replayable.

## Retention and response buffering

`CoordinationOptions` defaults to a 60-second processing lease, 20-second renewal interval, five-second backend timeout and eight-hour terminal retention. Processing records survive lease expiry by the retention window; duplicate reads do not refresh that window. Completion starts terminal retention from completion time. Retention expiry ends replay/exclusion guarantees. Reconciliation through `IIdempotencyStore.TryResolveAsync` requires the expected revision and external confirmation that the original work can no longer commit; an active attempt cannot be reopened.

`MaxCachedResponseBodySize` defaults to 512 KB and limits the response body eligible for replay. `IIdempotencyStore.MaxPayloadSize` independently limits the serialized response snapshot, including its headers and base64-encoded body; it is `null` when the store advertises no ceiling. If either replay limit is exceeded, CoreEx commits a terminal/no-replay marker and returns the completed response to the original caller. `MaxResponseBodySize` is a separate hard capture ceiling, defaulting to 10 MB; keep it above the replay-body limit when larger responses should still complete. Capturing uses memory up to the smaller of the replay-body and capture limits, and spills larger responses to a temporary file up to the capture ceiling. Configure `TempFileDirectoryAccessor` and host storage quotas when needed. Capture failures retain an uncertain outcome and do not expose partial response bytes. Streaming/deferred responses that bypass capture or disable buffering are unsupported. Cookies and correlation headers are excluded from replay by default; original-response cookies remain intact.

## Key types

| Type | Description |
|------|-------------|
| **[`IdempotencyKeyMiddleware`](./IdempotencyKeyMiddleware.cs)** | `IMiddleware` that reads `[IdempotencyKeyAttribute]` metadata and invokes `IIdempotencyProvider`. |
| [`IIdempotencyProvider`](./IIdempotencyProvider.cs) | HTTP idempotency integration boundary used by the middleware. |
| **[`IdempotencyProviderInvoker`](./IdempotencyProviderInvoker.cs)** | Invoker that wraps provider executions with OpenTelemetry spans. |

## Related Packages

- **[`CoreEx.Redis`](../../CoreEx.Redis/README.md)** — Redis-backed synchronization and idempotency storage, including migration and Redis data-loss limits.
- **[`CoreEx`](../../CoreEx/AGENTS.md)** — host-neutral coordination/idempotency contracts and the in-memory implementation.
- **[`CoreEx.AspNetCore.Mvc`](../Mvc/README.md)** — defines `[IdempotencyKeyAttribute]`.
- **[`CoreEx.Http`](../../CoreEx/Http/README.md)** — defines the `Idempotency-Key` header name and outgoing key support.
