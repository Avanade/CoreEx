# CoreEx.Redis

> Provides Redis-backed synchronization leases, idempotency storage and work-state tracking for CoreEx.

## Overview

`CoreEx.Redis` implements the host-neutral `ISynchronizer`, `IIdempotencyStore` and `IWorkProvider` contracts with atomic Redis operations. It depends on CoreEx and StackExchange.Redis, not ASP.NET Core, FusionCache, or `IHybridCache`. CoreEx also provides an explicit in-memory implementation for single-process use.

## Key capabilities

- **Renewable leases**: coordinate work using owner-checked acquisition, renewal and release, with a cancellation token that signals ownership loss.
- **Idempotency outcomes**: atomically claim a request, replay a retained response, or report an in-progress, conflicting, unavailable or indeterminate outcome.
- **Scoped keys**: use explicit `OperationKey` identities without ambient qualification; Redis formats them as discoverable, escaped segments.
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

For a coordination-only host, reference `Aspire.StackExchange.Redis` and use `builder.AddRedisClient("redis")` instead. Do not register both Aspire Redis client integrations for the same resource; distributed caching already adds the Redis client. Coordination does not use or register an `ICacheKeyProvider`; preserve that registration separately for ordinary caching.

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
var key = OperationKey.ForDomain(settings.DomainName, "scheduled-work", "singleton");
await using var lease = await synchronizer.TryAcquireAsync(key, cancellationToken).ConfigureAwait(false);
if (lease is null)
    return;

using var work = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.OwnershipLost);
work.Token.ThrowIfCancellationRequested();
await ExecuteProtectedWorkAsync(work.Token).ConfigureAwait(false);
lease.OwnershipLost.ThrowIfCancellationRequested();
```

Disposal stops renewal and releases only the owner's claim. A lease is not a fencing token: code from a former owner can continue after expiry, so the protected resource must enforce fencing or durable uniqueness where stale writes matter.

Choose `OperationKey.ForTenant(domainName, tenantId, operationName, instanceId)` for tenant-specific work, or `OperationKey.ForDomain(...)` deliberately for domain-wide work. Supply the same stable instance ID on all replicas; omitting it generates a new ID and therefore does not coordinate repeated attempts. Keys are purpose-separated between leases, idempotency and work and are not re-qualified from `ExecutionContext` or `ICacheKeyProvider`. A synchronized timer defaults to a domain-wide key derived from host settings and the service type and name; set `SynchronizerKey` for a different explicit identity.

## Idempotency outcomes

HTTP idempotency uses one authoritative claim/outcome record. Completed responses, including normally rendered application errors, are replayed. A thrown exception, cancellation, or response-rendering failure after application execution begins can leave an unknown business outcome; CoreEx retains an indeterminate record rather than automatically re-executing. Do not generate a new key to bypass an indeterminate result.

`CoordinationOptions` defaults to a 60-second lease, renewal every 20 seconds, a five-second backend-operation timeout, and eight-hour idempotency retention. Terminal retention starts at completion; an expired processing claim remains indeterminate for one retention window after its deadline, then expires and can execute again. Duplicate reads do not extend retention. Reconciliation requires the expected revision and external verification that the old operation cannot commit.

The HTTP publishing integration can explicitly release a live idempotency attempt only after proving that no message was dispatched or winning the tracked-publish Created-state fence. Redis serializes `release` against renewal and completion and deletes only the same owner's pending, unexpired claim. A timeout or lost acknowledgement is not proof of release; the HTTP provider returns a store failure and does not promise that same-key retry can proceed.

`AddRedisCoordination` advertises a 1 MiB maximum serialized response payload by default through `IIdempotencyStore.MaxPayloadSize`. Configure `maxIdempotencyPayloadSize` to a different positive byte limit, or `null` for no configured ceiling. A replayable response must be non-empty; `null` records a completed/no-replay outcome. HTTP idempotency stores a response only when both `MaxCachedResponseBodySize` and the store's payload ceiling permit it; otherwise the original caller still receives the response but subsequent requests cannot replay it.

The default HTTP header parser accepts quoted Structured Field strings and supported legacy bare keys; `StrictHeaderValidation = true` accepts only quoted strings. HTTP keys use the host domain and require an explicit tenant or domain-wide scope when a tenant is present. Run authentication and authorization before replay. See the [HTTP idempotency guide](../CoreEx.AspNetCore/Idempotency/README.md) for scope configuration, request identity and response handling.

## Work state

`AddRedisWorkProvider()` registers a singleton `RedisWorkProvider` as the `IWorkProvider` and a scoped `WorkOrchestrator` over the host-owned multiplexer; `AddInMemoryWorkProvider()` is the process-local alternative.

```csharp
builder.Services.AddRedisWorkProvider();
```

Every work item requires an explicit identity with a domain, tenant-or-domain-wide scope, operation name and instance ID. `WorkState` persists those named fields directly and reconstructs its computed `OperationKey` for provider calls; the nested key object is not serialized. Redis stores each item as one hash (state JSON, incarnation and version) under the namespace described below. The provider does not use ambient `ExecutionContext` or `ICacheKeyProvider` to infer work scope; callers must carry the same key across API, queue/job and worker boundaries. Create is insert-if-absent and each update/delete compares the incarnation and version (`WorkState.ETag`), so stale snapshots cannot change a recreated record. A `WorkSnapshot` returns the state and Redis server time from the same script. Work payloads belong in the domain's own store, with only an app-relative `ResourceLocation` retained in `WorkState`.

Configure the provider with `AddRedisWorkProvider(options => ...)` or construct `RedisWorkProviderOptions` directly. `OperationTimeout` defaults to five seconds. The DI registration requires the operation timeout multiplied by three to fit within `WorkOrchestratorOptions.LeaseDuration`. `TimeProvider` is unset by default so reads use Redis `TIME`; set it explicitly in tests to advance lease and deadline time without waiting. An override used by separate production hosts reintroduces clock skew. A write that exceeds `OperationTimeout` throws `WorkOutcomeUnknownException` because the abandoned command may still apply; caller cancellation throws `OperationCanceledException`. Redis replicates asynchronously, so failover can lose recently acknowledged transitions. This is an accepted risk: the provider does not use `WAIT`, which would block the shared multiplexed connection and still would not make Redis strongly consistent.

## Redis key layout

Redis uses provider-specific, case-sensitive namespaces. Lease and idempotency keys start with `CoreEx:coordination:v2:lease:` and `CoreEx:coordination:v2:idempotency:` respectively; work keys start with `CoreEx:work:v2:` by default. Each is followed by `domain:{domain}:scope:{domain|tenant}:tenant:{tenant|-}:operation:{operation}:instance:{instance}`. Domain-wide keys use `-` for the tenant segment. A custom work `KeyPrefix` is separated from the identity by a colon.

Each identity value is UTF-8 percent-encoded, including colons and Redis glob metacharacters, so components cannot escape their segments. Redis rejects malformed Unicode in identity values and custom prefixes rather than replacing it with a character that could collide with another key. For example, a tenant-scoped orders import for tenant `acme` can be discovered with `SCAN 0 MATCH 'CoreEx:work:v2:domain:orders:scope:tenant:tenant:acme:*'`. These segments are visible to Redis operators and tooling; do not put secrets or sensitive personal data in them. HTTP idempotency deliberately hashes its caller and HTTP key into the instance identifier, but that hash is not an authorization credential or a guarantee against guessing. Other providers choose their own representation and need not hash or format `OperationKey`.

## Guarantees and limits

The Redis work provider is a deliberately simple, best-effort tracking store, not durable storage; applications that need durable operation tracking should supply another `IWorkProvider`. Tracked subscribers are designed so that accepted work still runs when tracking state is lost or Redis is unavailable, at the cost of status accuracy, cancellation and duplicate suppression.

Redis provides atomic decisions against the participating authoritative primary, not exactly-once business execution. A lease epoch prevents stale workers from updating operation state after takeover, but cannot stop their in-flight handlers; lease expiry can therefore permit overlapping handler execution. Asynchronous failover, restoring an older snapshot, eviction, manual deletion, or retention expiry can erase claims or receipts and permit re-execution. Use persistence and a no-eviction policy for coordination data where operationally appropriate, bound Redis operation timeouts, and do not silently fall back to in-memory state. A timed-out write may have committed; callers must treat `WorkOutcomeUnknownException` as an unknown outcome rather than blindly retrying.

Owner-checked decisions use single-key Lua scripts embedded from [`Scripts/`](./Scripts/); each file documents its `KEYS`/`ARGV` contract. Idempotency deadline comparisons use Redis server time (`TIME`); scripts that read time and then write require effects replication (the default since Redis 5; always on in Redis 7+). Single-key scripts are Redis Cluster safe.

## Breaking migration and rollout

Coordination and idempotency now require `OperationKey` and use `CoreEx:coordination:v2:` rather than the former `CoreEx:coordination:v1:` string-key namespace. These namespaces do not share leases or receipts, even when they describe the same operation. Coordinate any cutover across replicas, drain active leases and requests, and account for retained or indeterminate v1 outcomes and their retry windows. A retry sent to v2 cannot see its v1 receipt and can execute again; records are not automatically migrated. Custom providers must implement the `OperationKey` contracts.

The work-provider API requires `OperationKey`; use an explicit key and include its identity fields in every queued or scheduled work envelope. `WorkState` persists the identity fields directly rather than a nested key object.

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
