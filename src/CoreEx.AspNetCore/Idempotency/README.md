# CoreEx.AspNetCore.Idempotency

> Provides the ASP.NET Core integration for idempotency keys. The HTTP provider uses a host-neutral `IIdempotencyStore`; choose Redis-backed storage or an explicitly process-local implementation.

## Overview

`IdempotencyKeyMiddleware` intercepts endpoints marked with `[IdempotencyKey]` and delegates request execution to `IIdempotencyProvider`. Register the HTTP integration with `AddIdempotencyProvider()` and register its storage independently. For a multi-instance deployment, use `CoreEx.Redis`; use `AddInMemoryCoordination()` only for tests or intentionally single-instance applications.

```csharp
builder.AddRedisDistributedCache("redis");
builder.Services
    .AddRedisCoordination()
    .AddIdempotencyProvider();
```

FusionCache and `IHybridCache` remain for ordinary caching and are not the idempotency store. HTTP idempotency requires an `OperationKey` and does not use `ICacheKeyProvider`. The default identity uses `IHostSettings.DomainName`; register host settings and, for tenant-scoped requests, a scoped `ExecutionContext`.

Configure tenant scope and establish a trusted tenant in the request's CoreEx execution context:

```csharp
builder.Services.AddIdempotencyProvider((_, provider) => provider.Scope = OperationKeyScope.Tenant);
```

Tenant scope rejects missing or empty tenant IDs. With `Scope` unset, requests without a tenant are domain-wide, but a present tenant requires an explicit scope. Set `OperationKeyScope.Domain` to intentionally share outcomes across tenants. An optional `OperationKeyFactory` can choose another domain, operation name or scope; it must preserve the supplied instance ID, which incorporates the decoded HTTP key and caller identity. Keep the operation name stable across endpoints if a reused key with a different request should continue to conflict.

## Outcome policy

Completed responses, including normally rendered application errors, are replayed. A thrown exception, cancellation, or rendering failure after application execution begins may have an unknown business outcome; that attempt is retained as indeterminate and is not automatically executed again. Do not generate a new key to bypass an indeterminate outcome. Reconcile only after verifying that the original work can no longer commit.

The HTTP provider also exposes an endpoint-scoped `IIdempotencyAttemptFeature` for a narrowly proven safe retry. `WebApi.PublishAsync` requests release only when operation-state creation fails before dispatch or when the Created-only dispatch fence is acknowledged as applied. The response is captured before the owner-checked store transition; unsupported stores, lost ownership, or ambiguous release acknowledgements fail conservatively and do not imply that retry is safe. Ordinary 503 responses and publish-only failures do not release an attempt.

CoreEx HTTP clients emit a quoted Structured Field string value. `StrictHeaderValidation` defaults to `false`, accepting standard quoted strings and supported legacy bare keys; set it to `true` to accept only standard quoted strings. Key identity is not a substitute for authorization, which must run before replay.

Idempotency is not exactly-once business execution. Redis failover to stale data, data restoration, eviction, manual deletion, or retention expiry can erase claims or receipts and permit re-execution. A process-local store does not coordinate across processes.

## Request identity and responses

The standards baseline is [draft-ietf-httpapi-idempotency-key-header-08](https://www.ietf.org/archive/id/draft-ietf-httpapi-idempotency-key-header-08.html), an Internet-Draft rather than a published Idempotency-Key RFC. Quoted keys use its Structured Field string representation; CoreEx requires 8-128 decoded printable ASCII characters and rejects parameters. Relaxed mode additionally accepts bare keys of 8-128 ASCII letters, digits, hyphens or underscores. Equivalent bare and quoted values identify the same operation. Duplicate/list headers and malformed quoted values are rejected in both modes. Replaying completed application errors is CoreEx's explicit policy, not a claim that draft-08 requires replay of every HTTP failure.

The instance identifier hashes the idempotency key and, when available, the authenticated caller's issuer and subject to avoid placing them directly in provider keys. This is not a secret or an authorization credential. Configure `CallerIdentityProvider` when authentication uses another stable identity or when an application needs different caller boundaries. Caller identities and decoded keys must contain valid Unicode; malformed inputs are rejected rather than conflated. Anonymous callers and authenticated callers without a supported subject claim have no inferred caller component. Run authentication and authorization before the idempotency boundary, including in Functions.

`IdempotencyProvider.CreateOperationKey(context, decodedKey)` reconstructs the explicit identity for reconciliation when the same caller and scope inputs are available; retain the identity and attempt revision if those inputs may not be recoverable later. A custom store must implement the `OperationKey` contract without re-qualifying it from ambient context.

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

The new operation-key namespace does not see existing legacy claims, completed responses or indeterminate outcomes. Coordinate any switch across replicas and account for in-flight work, retained receipts and the retry window; otherwise an old request can execute again. No automatic migration or fallback is performed.

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
