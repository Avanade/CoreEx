# CoreEx.Caching.FusionCache — AI Usage Guide

Binds the CoreEx `IHybridCache` abstraction to the FusionCache library, providing L1 (in-process) + L2 (Redis) hybrid caching with a multi-node backplane.

## Registration

```csharp
// Program.cs — standard L1/L2 hybrid setup with Redis backplane
builder.Services.AddMemoryCache();
builder.AddRedisDistributedCache("redis");   // Aspire Redis resource name

builder.Services.AddFusionCache()
    .WithRegisteredMemoryCache()
    .WithRegisteredDistributedCache()
    .WithBackplane(sp => new RedisBackplane(new RedisBackplaneOptions
    {
        Configuration = sp.GetRequiredService<IConnectionMultiplexer>().Configuration
    }))
    .WithSystemTextJsonSerializer(JsonDefaults.SerializerOptions);

builder.Services
    .AddFusionHybridCache()           // registers FusionHybridCache as IHybridCache
    .AddDefaultCacheKeyProvider()     // registers ICacheKeyProvider
    .AddHybridCacheIdempotencyProvider(); // registers IIdempotencyProvider backed by IHybridCache
```

## Usage in Application Code

Depend only on `IHybridCache` — never on `IFusionCache` directly in application/domain code.

```csharp
public class ReferenceDataService(IHybridCache cache)
{
    public async Task<IEnumerable<Status>> GetStatusesAsync(CancellationToken ct = default)
        => await cache.GetOrCreateByKeyAsync("ref.statuses",
            _ => LoadStatusesAsync(),
            new HybridCacheEntryOptions { LocalExpiration = TimeSpan.FromMinutes(5) },
            cancellationToken: ct).ConfigureAwait(false);
}
```

## Do Not

- Do not inject `IFusionCache` directly into application or domain services — use `IHybridCache`.
- Do not configure FusionCache without `AddFusionHybridCache()` — the `IHybridCache` registration is separate from the FusionCache builder setup.

## Further Reading

- [README](./README.md) — options translation and entry-options escape-hatch reference.
- [CoreEx](../CoreEx/README.md) — defines `IHybridCache` and `HybridCacheEntryOptions`.
- [FusionCache](https://github.com/ZiggyCreatures/FusionCache) — underlying caching library.
- [Hosts layer](../../samples/docs/hosts-layer.md) — `AddFusionCache()` / `AddFusionHybridCache()` registration in real API and subscriber hosts.
- [Infrastructure layer](../../samples/docs/infrastructure-layer.md) — Redis backplane configuration and idempotency provider wiring.
