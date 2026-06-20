# CoreEx.RefData

> Provides the `ReferenceDataOrchestrator` and supporting interfaces for managing, caching, and resolving collections of reference data (look-up / code-list) types across a CoreEx application.

## Overview

`CoreEx.RefData` defines the canonical pattern for reference data — think Status codes, Country lists, Gender options — within a CoreEx application. Reference data types are first-class strongly-typed entities (implementing `IReferenceData<TId>`) with `Id`, `Code`, `Text`, `IsActive`, `SortOrder`, and optional `StartDate`/`EndDate` validity windows.

`ReferenceDataOrchestrator` is the central singleton that manages one or more `IReferenceDataProvider` implementations, loads their collections into an `IReferenceDataCache` (backed by `IHybridCache`), and exposes a unified async API for querying any reference data type by CLR type, name, or code. It also maintains an `AsyncLocal` current instance, so JSON converters and validator rules can resolve reference data without explicit orchestrator injection.

The `IReferenceDataContext` scoping mechanism allows per-request filtering: e.g. returning only `IsActive` items for public API responses while allowing infrastructure to load all items. The `[ReferenceData<T>]` source-generator attribute on a contract property wires up the JSON code serialization and validator integration automatically.

## Key capabilities

- 📚 **Centralized collection management**: `ReferenceDataOrchestrator` manages multiple `IReferenceDataProvider` instances, loading each collection once and serving all subsequent requests from the cache.
- 🏎️ **Cached loading**: `IReferenceDataCache` (backed by `IHybridCache`) stores loaded collections; the orchestrator reloads on cache miss and honours `LocalExpiration`/`DistributedExpiration` settings.
- 🔍 **Multi-key lookup**: Collections are queryable by `Id`, `Code` (case-insensitive), or `Text` wildcard; `GetByTypeAsync<TRef>()` returns the full collection, and individual item resolution is available via `GetAsync<TRef>(code)`.
- 🌐 **JSON code serialization**: `JsonReferenceDataConverter` (in `CoreEx.Json`) uses the orchestrator's `AsyncLocal` current instance to deserialize JSON code strings back to strongly typed reference data instances.
- 🔐 **Validity filtering**: `IReferenceDataContext` filters returned collections to `IsActive` and within `StartDate`/`EndDate` windows; the context is scoped per request via `ExecutionContext`.
- 🏷️ **Code validation**: `IReferenceDataCodeCollection` provides `ContainsCode(string)` for fast O(1) code existence checks used by validation rules.
- 📡 **OpenTelemetry tracing**: `ReferenceDataOrchestratorInvoker` wraps every collection load with an `InvokerTracer` span, tagging the cache type, cache state, and item count.

## Key types

| Type | Description |
|------|-------------|
| **[`ReferenceDataOrchestrator`](./ReferenceDataOrchestrator.cs)** | Singleton managing reference data provider registration, cache-backed collection loading, and multi-key resolution via `GetByTypeAsync<TRef>()`, `GetByNameAsync()`. |
| **[`ReferenceDataOrchestratorInvoker`](./ReferenceDataOrchestratorInvoker.cs)** | `InvokerBase<ReferenceDataOrchestrator>` wrapping collection loads with OpenTelemetry spans and structured log entries. |
| **[`ReferenceDataMultiDictionary`](./ReferenceDataMultiDictionary.cs)** | Dictionary of reference data collections keyed by type name; the serialization-friendly result type returned by multi-type reference data API endpoints. |
| [`IReferenceData<TId>`](./IReferenceDataT.cs) | Core reference data interface: `Id`, `Code`, `Text`, `IsActive`, `SortOrder`, `StartDate`, `EndDate`, and validity helpers. |
| [`IReferenceDataCollectionT`](./IReferenceDataCollectionT.cs) | Strongly-typed collection interface adding `GetById`, `GetByCode`, `GetByText`, and `GetByPredicate` lookup methods. |
| [`IReferenceDataCodeCollection`](./IReferenceDataCodeCollection.cs) | Slim interface exposing `ContainsCode(string)` for O(1) code membership tests used by validators. |
| [`IReferenceDataProvider`](./IReferenceDataProvider.cs) | Pluggable provider interface: `GetTypes()` returns the reference data types managed by this provider; `GetAsync(Type)` loads and returns a collection. |
| [`IReferenceDataCache`](./IReferenceDataCache.cs) | Cache interface wrapping `IHybridCache` for reference data collection storage and retrieval with configurable expiry options. |
| [`IReferenceDataContext`](./IReferenceDataContext.cs) | Per-request scoping interface that controls which reference data items are visible (active-only vs all) for the current execution context. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - `ExecutionContext` carries the ambient `IReferenceDataContext` and `IServiceProvider` used by the orchestrator.
- **[`CoreEx.Caching`](../Caching/README.md)** - `IHybridCache` backs `IReferenceDataCache` for L1/L2 reference data caching.
- **[`CoreEx.Json`](../Json/README.md)** - `JsonReferenceDataConverter` uses `ReferenceDataOrchestrator.Current` to deserialize code strings to typed reference data instances.
- **[`CoreEx.Invokers`](../Invokers/README.md)** - `ReferenceDataOrchestratorInvoker` emits OpenTelemetry spans for every collection load.
- **[`CoreEx.RefData`](../../CoreEx.RefData/README.md)** - The separate `CoreEx.RefData` NuGet package extends these primitives with `ReferenceDataBase<TId, TSelf>` and additional entity/collection base types.