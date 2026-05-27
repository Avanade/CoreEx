# CoreEx.RefData

> Provides the CoreEx reference data framework: typed base classes for reference data items and collections, a hybrid-cache-backed orchestrator, contextual date-validity checking, and a code-serialization collection.

## Overview

`CoreEx.RefData` implements the domain concept of reference data — lookup tables (e.g. Status, Country, Currency) that are read frequently and change rarely. Items carry an `Id`, a `Code`, `Text`, `Description`, `SortOrder`, `IsActive`, and optional `StartsOn`/`EndsOn` date range, all of which participate in validity checking via `ReferenceDataContext`.

Two concrete base classes cover the most common identity types: `ReferenceData<TSelf>` (string `Id`) and `ReferenceData<TId, TSelf>` (arbitrary `TId`). Each exposes static `TryGetById` and `TryGetByCode` helpers that delegate to the ambient `ReferenceDataOrchestrator` registered in the DI container. Collections are thread-safe dictionaries keyed by both `Id` and `Code`; `GetItems` returns a stable, sorted `IEnumerable` by `ReferenceDataSortOrder`.

`ReferenceDataHybridCache` implements `IReferenceDataCache` on top of `IHybridCache`, using per-type semaphores to ensure that only one thread performs the expensive load while others wait, then everyone benefits from the cached result. `ReferenceDataCodeCollection<TRef>` solves the serialization problem: a property declared as a list of codes is stored as `List<string?>` on the wire but exposes `ICollection<TRef>` at the domain level.

## Key capabilities

- 📚 **Typed reference data base classes**: `ReferenceData<TSelf>` (string Id) and `ReferenceData<TId, TSelf>` (typed Id) provide `Id`, `Code`, `Text`, `Description`, `SortOrder`, `IsActive`, `StartsOn`/`EndsOn`, mapping support, and `ThrowIfInactive`/`ThrowIfInvalid` guards.
- 📋 **Thread-safe collections**: `ReferenceDataCollection<TRef>` and `ReferenceDataCollection<TId, TRef>` index items by both `Id` and `Code` (case-insensitive by default) and expose `GetItems(sortOrder, activeOnly, isValid)` for filtered, ordered enumeration.
- 🔄 **Hybrid-cache backing**: `ReferenceDataHybridCache` implements `IReferenceDataCache` via `IHybridCache`; per-type semaphores prevent thundering-herd on cold start and support configurable entry options per item type via `OnCreateCacheEntry`.
- 📅 **Contextual date validity**: `ReferenceDataContext` supplies the reference date used to evaluate `StartsOn`/`EndsOn` on every item; the date can be set globally or overridden per type, defaulting to `Runtime.UtcNow`.
- 🌐 **Localised text and description**: `Text` and `Description` are resolved through `LText` on every get, enabling locale-specific display strings without changing the domain model.
- 🏷 **Code-serialization collection**: `ReferenceDataCodeCollection<TRef>` stores codes as `List<string?>` for wire serialization while presenting an `ICollection<TRef>` interface backed by the live orchestrator lookups.
- 🔧 **DI and health-check helpers**: `AddReferenceDataOrchestrator` and related `IServiceCollection` extensions register the orchestrator and cache; `ReferenceDataOrchestratorHealthCheck` exposes a health-check endpoint reporting all registered reference data types.

## Key types

| Type | Description |
|------|-------------|
| _[`ReferenceData<TSelf>`](./ReferenceDataT.cs)_ | Abstract reference data base with `string` Id; provides static `TryGetById`/`TryGetByCode`, implicit/explicit `string` cast operators, and `IComparable<TSelf>` by code. |
| _[`ReferenceData<TId, TSelf>`](./ReferenceDataT2.cs)_ | Abstract reference data base with arbitrary typed `TId`; same feature set as `ReferenceData<TSelf>` but with both Id and Code lookups. |
| **[`ReferenceDataCollection<TRef>`](./ReferenceDataCollectionT.cs)** | Thread-safe `IReferenceDataCollection` with string `Id`; indexes by Id and Code, default sort by `ReferenceDataSortOrder.SortOrder`. |
| **[`ReferenceDataCollection<TId, TRef>`](./ReferenceDataCollectionT2.cs)** | Thread-safe `IReferenceDataCollection` with typed `TId`; otherwise identical to the string variant. |
| **[`ReferenceDataCodeCollection<TRef>`](./ReferenceDataCodeCollection.cs)** | Serializes a reference data relationship as a `List<string?>` of codes on the wire while presenting `ICollection<TRef>` at the domain level. |
| **[`ReferenceDataHybridCache`](./ReferenceDataHybridCache.cs)** | `IReferenceDataCache` implementation backed by `IHybridCache`; per-type semaphores enforce single-loader semantics on cache miss; supports per-type entry-option customisation via `OnCreateCacheEntry`. |
| **[`ReferenceDataContext`](./ReferenceDataContext.cs)** | Supplies the contextual date used to evaluate `StartsOn`/`EndsOn` validity; settable globally or per reference data type. |
| **[`ReferenceDataSortOrder`](./ReferenceDataSortOrder.cs)** | Enum: `SortOrder`, `Id`, `Code`, `Text`; controls the ordering returned by `GetItems`. |

## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.RefData.Abstractions`** | Core abstract implementations: `ReferenceDataCore<TId>` and `ReferenceDataCollectionCore<TId, TRef>` providing the full identity, validity, mapping, and collection infrastructure. | [📖 README](./Abstractions/README.md) |
| **`CoreEx.RefData.HealthChecks`** | `ReferenceDataOrchestratorHealthCheck` — reports all registered reference data types to the ASP.NET Core health-check endpoint. | [📖 README](./HealthChecks/README.md) |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Defines `IReferenceData`, `IReferenceDataCollection`, `IReferenceDataCache`, `ReferenceDataOrchestrator`, and `IHybridCache` consumed throughout.
- **[`CoreEx.Caching.FusionCache`](../CoreEx.Caching.FusionCache/README.md)** - `FusionHybridCache` is the recommended `IHybridCache` implementation for backing `ReferenceDataHybridCache` in production.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.