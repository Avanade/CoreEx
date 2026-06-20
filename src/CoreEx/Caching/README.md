# CoreEx.Caching

> Provides the `IHybridCache` abstraction and `ICacheKeyProvider` for decoupled, strategy-driven local and/or distributed caching, with a built-in in-memory implementation for testing and simple scenarios.

## Overview

`CoreEx.Caching` defines a provider-agnostic caching contract that supports three strategies: local (in-process), distributed, and hybrid (L1 local + L2 distributed). Consuming code depends only on `IHybridCache`, which is resolved via DI — the concrete implementation (`MemoryOnlyHybridCache` for in-process, or `CoreEx.Caching.FusionCache` for production hybrid caching) is swapped without changing any call sites.

`HybridCacheEntryOptions` carries per-call configuration — strategy, local expiration, distributed expiration, and optional tags — and reads its defaults from `IConfiguration` (`CoreEx:Caching:*`), so production and test environments can tune caching behavior without code changes.

`ICacheKeyProvider` handles key namespacing: the default implementation prefixes keys with the domain name and tenant ID from `HostSettings` and `ExecutionContext`, ensuring that multi-tenant or multi-domain deployments share a cache store safely.

## Key capabilities

- 🏗️ **Strategy abstraction**: `CacheStrategy` flags enum (`Local`, `Distributed`, `Hybrid`) decouples cache topology from business logic; per-call strategy is set via `HybridCacheEntryOptions`.
- 🔑 **Structured key namespacing**: `ICacheKeyProvider` qualifies raw cache keys with domain name and tenant ID, preventing cross-tenant cache collisions in a shared store.
- 🧩 **In-memory built-in**: `MemoryOnlyHybridCache` provides a fully functional `IHybridCache` using `IMemoryCache`, suitable for unit tests and single-node scenarios without any external dependency.
- ⚙️ **Configuration-driven defaults**: `HybridCacheEntryOptions` defaults (`DefaultStrategy`, `DefaultLocalExpiration`, `DefaultDistributedExpiration`) are read from `IConfiguration`, allowing environment-specific tuning.
- 🏷️ **Cache tags**: `HybridCacheEntryOptions.Tags` supports tag-based invalidation for implementations that support it (e.g. `CoreEx.Caching.FusionCache`).

## Key types

| Type | Description |
|------|-------------|
| **[`DefaultCacheKeyProvider`](./DefaultCacheKeyProvider.cs)** | Default `ICacheKeyProvider` that qualifies keys as `{DomainName}:{TenantId}:{key}`, omitting absent segments. Also produces entity keys as `{TypeName}:{CompositeKey}`. |
| **[`HybridCacheEntryOptions`](./HybridCacheEntryOptions.cs)** | Per-entry options record specifying `CacheStrategy`, `LocalExpiration`, `DistributedExpiration`, and `Tags`; reads global defaults from `IConfiguration`. |
| **[`MemoryOnlyHybridCache`](./MemoryOnlyHybridCache.cs)** | `IHybridCache` implementation backed by `IMemoryCache`; ignores the `Strategy` setting and always stores locally. |
| **[`CacheStrategy`](./CacheStrategy.cs)** | Flags enum: `Local` (in-process), `Distributed`, or `Hybrid` (both). |
| [`IHybridCache`](./IHybridCache.cs) | Core cache interface: `TryGetByKeyAsync`, `GetOrCreateByKeyAsync`, `GetOrDefaultByKeyAsync`, `SetByKeyAsync`, `RemoveByKeyAsync`, and tag-based `RemoveByTagAsync`. |
| [`ICacheKeyProvider`](./ICacheKeyProvider.cs) | Interface for producing fully-qualified string cache keys from raw keys or `IEntityKey` / `CompositeKey` pairs. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - `ExecutionContext` and `HostSettings` (via `IHostSettings`) supply the tenant ID and domain name used for key namespacing.
- **[`CoreEx.Caching.FusionCache`](../../CoreEx.Caching.FusionCache/README.md)** - Production hybrid cache implementation wrapping ZiggyCreatures FusionCache; backs the `IHybridCache` contract with L1+L2 support.
- **[`CoreEx.Hosting`](../Hosting/README.md)** - `HybridCacheSynchronizer` (in Hosting.Synchronization) uses `IHybridCache` for distributed lock coordination.
- **[`CoreEx.RefData`](../RefData/README.md)** - `ReferenceDataOrchestrator` and `IReferenceDataCache` use `IHybridCache` to cache loaded reference data collections.

## Additional Resources

- [ZiggyCreatures FusionCache](https://github.com/ZiggyCreatures/FusionCache) - The third-party library used by `CoreEx.Caching.FusionCache` to provide the production hybrid cache implementation.