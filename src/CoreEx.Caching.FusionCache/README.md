# CoreEx.Caching.FusionCache

> Provides a `FusionHybridCache` implementation of `IHybridCache` backed by the ZiggyCreatures FusionCache library, bridging CoreEx caching contracts to FusionCache's L1/L2 hybrid and backplane capabilities.

## Overview

`CoreEx.Caching.FusionCache` binds the `IHybridCache` abstraction defined in `CoreEx` to the open-source <a href="https://github.com/ZiggyCreatures/FusionCache">FusionCache</a> library. Application code that depends on `IHybridCache` requires no changes; swapping in `FusionHybridCache` upgrades the backing store from the built-in `MemoryOnlyHybridCache` to FusionCache's full feature set, including a configurable L1 in-memory cache, an optional L2 distributed cache (e.g. Redis), a memory backplane for multi-node cache coherence, and tag-based invalidation.

The package is deliberately thin: `FusionHybridCache` delegates every cache operation to the underlying `IFusionCache` instance after applying key qualification via `ICacheKeyProvider` and translating `HybridCacheEntryOptions` into `FusionCacheEntryOptions` via `FusionCacheExtensions.ToFusionCacheEntryOptions`. The `CacheStrategy` enum governs which tiers participate in each operation — `Local` disables the distributed tier, `Distributed` disables the in-memory tier, and `LocalAndDistributed` (the default) uses both.

## Key capabilities

- 🧩 **`IHybridCache` implementation**: `FusionHybridCache` fulfils all `IHybridCache` contract methods — `TryGetByKeyAsync`, `GetOrDefaultByKeyAsync`, `GetOrCreateByKeyAsync`, `SetByKeyAsync`, `RemoveByKeyAsync`, and `RemoveByTagAsync` — against an `IFusionCache` instance.
- 🔄 **Options translation**: `FusionCacheExtensions.ToFusionCacheEntryOptions` maps `HybridCacheEntryOptions` (`LocalExpiration`, `DistributedExpiration`, `CacheStrategy`, `Tags`) directly to a `FusionCacheEntryOptions` instance, including `SkipDistributedCache*` and `SkipMemoryCache*` flags derived from `CacheStrategy`.
- 📦 **L1/L2 and backplane support**: relies on FusionCache's own backplane packages (`ZiggyCreatures.FusionCache.Backplane.StackExchangeRedis`, `ZiggyCreatures.FusionCache.Backplane.Memory`) and serializer (`ZiggyCreatures.FusionCache.Serialization.SystemTextJson`) that are bundled as transitive dependencies.
- 🏷 **Tag-based invalidation**: `RemoveByTagAsync` delegates to FusionCache's native tag-eviction API, allowing entire categories of cache entries to be invalidated by a single call.
- 🔧 **Single-line DI registration**: `AddFusionHybridCache()` registers `FusionHybridCache` as the scoped `IHybridCache` service; composing it with FusionCache's own `AddFusionCache()` setup is the only wiring required.
- **Entry-options escape hatch**: `FusionHybridCache.ConfigureEntryOptions(Action<FusionCacheEntryOptions>)` accepts a fluent callback invoked on every translated `FusionCacheEntryOptions` before use, giving access to any FusionCache-specific setting not covered by `HybridCacheEntryOptions`.

## Key types

| Type | Description |
|------|-------------|
| **[`FusionHybridCache`](./FusionHybridCache.cs)** | `IHybridCache` implementation backed by `IFusionCache`; qualifies keys via `ICacheKeyProvider`, translates options, and supports a per-instance `ConfigureEntryOptions` callback for advanced FusionCache settings. |
| **[`FusionCacheExtensions`](./FusionCacheExtensions.cs)** | Static extension class; `ToFusionCacheEntryOptions(HybridCacheEntryOptions?)` performs the `CacheStrategy`-aware options translation. |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Defines `IHybridCache`, `HybridCacheEntryOptions`, `CacheStrategy`, and `ICacheKeyProvider` that this package implements and consumes.

## Additional resources

- [ZiggyCreatures FusionCache](https://github.com/ZiggyCreatures/FusionCache) — the underlying caching library.
- [FusionCache backplane documentation](https://github.com/ZiggyCreatures/FusionCache/blob/main/docs/Backplane.md) — configuring Redis or memory backplanes for multi-node coherence.