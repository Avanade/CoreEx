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

Preserve custom `ICacheKeyProvider` registrations; `AddRedisCoordination()` supplies the default only if none exists. Establish the intended tenant/domain context before acquisition; domain-wide work must not inherit a request tenant. The qualified key is captured for the lifetime of the lease or attempt.

## Usage in Application Code

Dispose each acquired lease and link protected work to its ownership-loss token:

```csharp
await using var lease = await synchronizer.TryAcquireAsync("scheduled-work", cancellationToken).ConfigureAwait(false);
if (lease is null)
    return;

using var work = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.OwnershipLost);
work.Token.ThrowIfCancellationRequested();
await ExecuteProtectedWorkAsync(work.Token).ConfigureAwait(false);
lease.OwnershipLost.ThrowIfCancellationRequested();
```

`null` means contention; backend failures propagate. Lease ownership loss is cooperative and does not fence stale writes.

## Idempotency

Completed HTTP responses, including rendered errors, replay. An exception, cancellation, or rendering failure after application entry can leave an indeterminate outcome; do not automatically retry it. Reconcile only after verifying the old operation cannot commit, using its expected revision. Indeterminate records eventually expire, so retention is a bounded guarantee.

Redis defaults to a 1 MiB serialized response limit. Store a non-empty response for replay, or `null` for a completed/no-replay outcome. HTTP applies its response-body limit as well as the store limit.

## Work state

`services.AddRedisWorkProvider()` registers a singleton `RedisWorkProvider` as `IWorkProvider` (plus a scoped `WorkOrchestrator`) over the same host-owned multiplexer. Create is insert-if-absent and every transition is an ETag compare-and-set, so check `Result` failures: `ConflictException` means the transition was not permitted (for example already started or finished). Result data defaults to a 512 KiB limit.

## Do Not

- Do not use in-memory coordination as a Redis outage fallback or across replicas.
- Do not store work state in `IHybridCache` or another evictable cache; use an `IWorkProvider` with atomic create and compare-and-set.
- Do not treat Redis leases or retained outcomes as fencing or exactly-once business execution; failover, eviction, deletion and expiry can lose coordination data.
- Do not bypass an indeterminate outcome with a new key or infer that an error proves no side effects occurred.
- Do not run old HybridCache and new Redis replicas concurrently for the same protected operations; their claims and receipts are not shared.

## Further Reading

- [README](./README.md) - registration, guarantees, limits and migration.
- [ISynchronizer](../CoreEx/Coordination/ISynchronizer.cs) and [IIdempotencyStore](../CoreEx/Idempotency/IIdempotencyStore.cs) - coordination and idempotency contracts.
- [ASP.NET Core idempotency](../CoreEx.AspNetCore/Idempotency/README.md) - request identity and HTTP behavior.
- [Hosts layer](../../samples/docs/hosts-layer.md) - host composition patterns.
