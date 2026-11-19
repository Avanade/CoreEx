# CoreEx.Redis

> Provides Redis-backed synchronization leases, idempotency storage and work-state tracking for CoreEx.

## Overview

`CoreEx.Redis` implements the host-neutral `ISynchronizer`, `IIdempotencyStore` and `IWorkProvider` contracts with atomic Redis operations. It depends on CoreEx and StackExchange.Redis, not ASP.NET Core, FusionCache, or `IHybridCache`. CoreEx also provides an explicit in-memory implementation for single-process use.

## Key capabilities

- **Renewable leases**: coordinate work using owner-checked acquisition, renewal and release, with a cancellation token that signals ownership loss.
- **Idempotency outcomes**: atomically claim a request, replay a retained response, or report an in-progress, conflicting, unavailable or indeterminate outcome.
- **Scoped keys**: qualify logical keys using the registered `ICacheKeyProvider`, including its domain and tenant policy.
- **Work state**: `RedisWorkProvider` persists `WorkOrchestrator` state with insert-if-absent create and ETag compare-and-set transitions.
- **Configurable bounds**: set lease and retention durations and the maximum serialized response payload.

## Registration

When the host uses `Aspire.StackExchange.Redis.DistributedCaching`, `AddRedisDistributedCache("redis")` registers the host-owned `IConnectionMultiplexer` that `AddRedisCoordination()` resolves. The following example also registers `CoreEx.AspNetCore` HTTP idempotency:

```csharp
builder.AddRedisDistributedCache("redis");
builder.Services
    .AddRedisCoordination()
    .AddIdempotencyProvider();
```

For a coordination-only host, reference `Aspire.StackExchange.Redis` and use `builder.AddRedisClient("redis")` instead. Do not register both Aspire Redis client integrations for the same resource; distributed caching already adds the Redis client. `AddRedisCoordination` registers a default `ICacheKeyProvider` only when none exists, preserving custom registrations. The qualified key is captured at operation start and retained for the lease or attempt.

For tests or intentionally single-instance applications, use the in-memory implementation instead:

```csharp
builder.Services
    .AddInMemoryCoordination()
    .AddIdempotencyProvider();
```

`InMemoryCoordination` shares state across DI scopes under one root service provider, without Redis. Independently built hosts/providers do not coordinate, even within the same operating-system process, and restarting loses the state. It is not a Redis failure fallback and does not coordinate across processes or replicas.

## Synchronization leases

`ISynchronizer` is a reusable acquisition service; `ISynchronizationLease` represents one acquisition. Acquisition returns null for contention and propagates backend failures. Dispose each lease, normally with `await using`, and link protected work to ownership loss:

```csharp
await using var lease = await synchronizer.TryAcquireAsync("scheduled-work", cancellationToken).ConfigureAwait(false);
if (lease is null)
    return;

using var work = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.OwnershipLost);
work.Token.ThrowIfCancellationRequested();
await ExecuteProtectedWorkAsync(work.Token).ConfigureAwait(false);
lease.OwnershipLost.ThrowIfCancellationRequested();
```

Disposal stops renewal and releases only the owner's claim. A lease is not a fencing token: code from a former owner can continue after expiry, so the protected resource must enforce fencing or durable uniqueness where stale writes matter.

## Idempotency outcomes

HTTP idempotency uses one authoritative claim/outcome record. Completed responses, including normally rendered application errors, are replayed. A thrown exception, cancellation, or response-rendering failure after application execution begins can leave an unknown business outcome; CoreEx retains an indeterminate record rather than automatically re-executing. Do not generate a new key to bypass an indeterminate result.

`CoordinationOptions` defaults to a 60-second lease, renewal every 20 seconds, a five-second backend-operation timeout, and eight-hour idempotency retention. Terminal retention starts at completion; an expired processing claim remains indeterminate for one retention window after its deadline, then expires and can execute again. Duplicate reads do not extend retention. Reconciliation requires the expected revision and external verification that the old operation cannot commit.

`AddRedisCoordination` advertises a 1 MiB maximum serialized response payload by default through `IIdempotencyStore.MaxPayloadSize`. Configure `maxIdempotencyPayloadSize` to a different positive byte limit, or `null` for no configured ceiling. A replayable response must be non-empty; `null` records a completed/no-replay outcome. HTTP idempotency stores a response only when both `MaxCachedResponseBodySize` and the store's payload ceiling permit it; otherwise the original caller still receives the response but subsequent requests cannot replay it.

The default HTTP header parser accepts quoted Structured Field strings and supported legacy bare keys; `StrictHeaderValidation = true` accepts only quoted strings. Establish the intended tenant/domain execution context before acquiring coordination; domain-wide work must not inherit a request tenant. Run authentication and authorization before replay. See the [HTTP idempotency guide](../CoreEx.AspNetCore/Idempotency/README.md) for request identity and response handling.

## Work state

`AddRedisWorkProvider()` registers a singleton `RedisWorkProvider` as the `IWorkProvider` and a scoped `WorkOrchestrator` over the host-owned multiplexer; `AddInMemoryWorkProvider()` is the process-local alternative.

```csharp
builder.Services.AddRedisWorkProvider();
```

Each work item is one hash (state JSON, incarnation, version and optional result data) keyed by a SHA-256 of its identifier under `CoreEx:work:v1:`. Create is insert-if-absent and each update/delete compares the incarnation and version (`WorkState.ETag`), so stale snapshots cannot change a recreated record. Concurrent create/start have a single winner, finished work is never overwritten, and completion with result data is atomic. Unfinished records live for the remaining expiry plus `WorkOrchestrator.RetentionTimeSpan`; finished records for the retention. Result data defaults to a 512 KiB limit (`maxDataSize`). Work identifiers are global; they are not tenant-qualified by `ICacheKeyProvider`.

## Guarantees and limits

Redis provides atomic decisions against the participating authoritative primary, not exactly-once business execution. Asynchronous failover, restoring an older snapshot, eviction, manual deletion, or retention expiry can erase claims or receipts and permit re-execution. Use persistence and a no-eviction policy for coordination data where operationally appropriate, bound Redis operation timeouts, and do not silently fall back to in-memory state.

Owner-checked decisions use single-key Lua scripts embedded from [`Scripts/`](./Scripts/); each file documents its `KEYS`/`ARGV` contract. Idempotency deadline comparisons use Redis server time (`TIME`); scripts that read time and then write require effects replication (the default since Redis 5; always on in Redis 7+). Single-key scripts are Redis Cluster safe.

## Breaking migration and rollout

Replace `AddHybridCacheSynchronizer` with `AddRedisCoordination` (or `AddInMemoryCoordination` for a deliberately single-instance host). The `ISynchronizer` contract and its extensions are now in `CoreEx.Coordination` rather than `CoreEx.Hosting.Synchronization`; update imports and implementations to use the owned-lease API. Replace `AddHybridCacheIdempotencyProvider` with the selected coordination store plus `AddIdempotencyProvider`. Retain existing custom key-provider and FusionCache registrations for ordinary cache and reference-data use; coordination no longer depends on FusionCache semantics.

The Redis key namespace is versioned and old HybridCache entries are not imported as claims or terminal receipts. Do not run old and new replicas concurrently for operations that rely on the same idempotency keys: their stores do not share ownership decisions. Drain old replicas and complete a coordinated rollout before enabling the new provider, accounting for in-flight requests and the legacy response-retention window. Retried requests after rollout may execute if their only receipt existed in the old cache; assess that exposure before cutover.

## Key types

| Type | Description |
|------|-------------|
| **[`RedisCoordination`](./RedisCoordination.cs)** | Redis-backed implementation of `ISynchronizer` and `IIdempotencyStore`. |
| **[`RedisWorkProvider`](./RedisWorkProvider.cs)** | Redis-backed `IWorkProvider` with atomic create and compare-and-set transitions. |
| **[`CoreExRedisExtensions`](./CoreExRedisExtensions.cs)** | Registers Redis coordination and the work provider using the host-owned multiplexer. |
| **[`CoordinationOptions`](../CoreEx/Coordination/CoordinationOptions.cs)** | Lease, renewal, backend timeout and idempotency retention settings. |
| **[`ISynchronizer`](../CoreEx/Coordination/ISynchronizer.cs)** | Reusable acquisition service for owned synchronization leases. |
| **[`ISynchronizationLease`](../CoreEx/Coordination/ISynchronizationLease.cs)** | Owned acquisition with an ownership-loss token and async disposal. |
| **[`IIdempotencyStore`](../CoreEx/Idempotency/IIdempotencyStore.cs)** | Atomic idempotency decisions, terminal responses and reconciliation. |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Defines the coordination and idempotency contracts and the in-memory implementation.
- **[`CoreEx.Hosting`](../CoreEx/Hosting/README.md)** - Defines `WorkOrchestrator` and `IWorkProvider`.
- **[`CoreEx.AspNetCore.Idempotency`](../CoreEx.AspNetCore/Idempotency/README.md)** - Provides HTTP idempotency integration.
- **[`CoreEx.Caching.FusionCache`](../CoreEx.Caching.FusionCache/README.md)** - Provides ordinary hybrid caching, independently of coordination.

## Additional resources

- [Host composition patterns](../../samples/docs/hosts-layer.md).
- [Embedded Redis scripts](./Scripts/).

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants can use it for registration, usage patterns and safety boundaries.
