# CoreEx.Redis — AI Usage Guide

Provides Redis implementations of CoreEx's synchronization lease, idempotency-store and work-state (`IWorkProvider`) contracts. The package references CoreEx and StackExchange.Redis; HTTP integration belongs to `CoreEx.AspNetCore`.

## Registration

For an HTTP host referencing `Aspire.StackExchange.Redis.DistributedCaching` and `CoreEx.AspNetCore`:

```csharp
builder.AddRedisDistributedCache("redis");
builder.Services
    .AddRedisCoordination()
    .AddIdempotencyProvider();
```

`AddRedisCoordination()` registers `ISynchronizer` and `IIdempotencyStore` over the host-owned multiplexer. `AddRedisDistributedCache("redis")` already registers the Redis client; do not register `AddRedisClient("redis")` as well. For a coordination-only host, use `Aspire.StackExchange.Redis` and `builder.AddRedisClient("redis")` instead, and omit `AddIdempotencyProvider()`.

`AddInMemoryCoordination()` is the explicit in-memory alternative for tests or intentionally single-instance .NET applications. It uses no Redis connection; its backing state is singleton per root service provider. Independent hosts/providers do not coordinate, even within one OS process, and restarting loses the state. It is never a Redis outage fallback and provides no cross-process guarantee.

Preserve custom `ICacheKeyProvider` registrations for ordinary caching. Coordination takes a name and `CompositeKey`, qualifies them with the host domain and ambient `ExecutionContext.TenantId`, and neither registers nor uses that provider; a physical key is captured for the lifetime of the lease or attempt.

## Usage in Application Code

Dispose each acquired lease and link protected work to its ownership-loss token:

```csharp
await using var lease = await synchronizer.TryAcquireAsync("scheduled-work", CompositeKey.Create("singleton"), cancellationToken).ConfigureAwait(false);
if (lease is null)
    return;

using var work = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.OwnershipLost);
work.Token.ThrowIfCancellationRequested();
await ExecuteProtectedWorkAsync(work.Token).ConfigureAwait(false);
lease.OwnershipLost.ThrowIfCancellationRequested();
```

`null` means contention; backend failures propagate. Lease ownership loss is cooperative and does not fence stale writes.

A present ambient tenant makes the key tenant-scoped; without one it is domain-wide. Supply a stable name and key across invocations and replicas. Different Redis key layouts do not coordinate with each other; plan any cutover accordingly.

## Idempotency

Completed HTTP responses, including rendered errors, replay. An exception, cancellation, or rendering failure after application entry can leave an indeterminate outcome; do not automatically retry it. Reconcile only after verifying the old operation cannot commit, using its expected revision. Indeterminate records eventually expire, so retention is a bounded guarantee.

Redis defaults to a 1 MiB serialized response limit. Store a non-empty response for replay, or `null` for a completed/no-replay outcome. HTTP applies its response-body limit as well as the store limit.

HTTP idempotency uses the operation name `http-idempotency` and a caller-scoped key; the store qualifies it by host domain and ambient tenant. Claims and receipts written under a different key layout are not visible.

## Work state

`services.AddRedisWorkProvider(options => ...)` registers a singleton `RedisWorkProvider` as `IWorkProvider` (plus a scoped `WorkOrchestrator`) over the host-owned multiplexer. Every work operation must carry the same operation name and `CompositeKey` from creation through the worker lease, and the worker host must establish the same ambient tenant as the creator. Create is insert-if-absent and state transitions are ETag compare-and-set; `WorkTransitionResult.Outcome` explicitly distinguishes lease-held/lost, expired, already-finished, and applied transitions. `WorkSnapshot` includes Redis server time by default. Configure `RedisWorkProviderOptions.TimeProvider` only for deterministic tests or when production hosts share a controlled clock. Result payloads are not stored; use domain persistence and `WorkState.ResourceLocation`.

Redis formats the domain, tenant, operation and instance as visible percent-escaped key segments under purpose-specific prefixes. Redis rejects malformed Unicode in identity values and custom prefixes to prevent replacement-character collisions. Avoid secrets and sensitive personal data in key components. HTTP intentionally hashes the caller and HTTP key into its instance identifier.

## Do Not

- Do not use in-memory coordination as a Redis outage fallback or across replicas.
- Do not store work state in `IHybridCache` or another evictable cache; use an `IWorkProvider` with atomic create and compare-and-set.
- Do not use `ICacheKeyProvider` for coordination, idempotency or work keys; pass the same operation name and `CompositeKey` through the entire workflow under the same ambient tenant.
- Do not assume different key layouts share claims.
- Do not treat Redis leases or retained outcomes as fencing or exactly-once business execution; failover, eviction, deletion and expiry can lose coordination data.
- Do not bypass an indeterminate outcome with a new key or infer that an error proves no side effects occurred.
- Do not run old HybridCache and new Redis replicas concurrently for the same protected operations; their claims and receipts are not shared.

## Further Reading

- [README](./README.md) - registration, guarantees, limits and migration.
- [ISynchronizer](../CoreEx/Coordination/ISynchronizer.cs) and [IIdempotencyStore](../CoreEx/Idempotency/IIdempotencyStore.cs) - coordination and idempotency contracts.
- [ASP.NET Core idempotency](../CoreEx.AspNetCore/Idempotency/README.md) - request identity and HTTP behavior.
- [Hosts layer](../../samples/docs/hosts-layer.md) - host composition patterns.
