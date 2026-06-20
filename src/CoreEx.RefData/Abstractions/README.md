# CoreEx.RefData.Abstractions

> Provides the core abstract implementations for reference data items and collections: the identity, validity, mapping, and thread-safe indexing infrastructure from which all concrete reference data types derive.

## Overview

`CoreEx.RefData.Abstractions` contains `ReferenceDataCore<TId>` and `ReferenceDataCollectionCore<TId, TRef>` — the two foundational abstract classes that all reference data types and their collections build on top of. These types are intentionally separated from the concrete specializations (`ReferenceData<TSelf>`, `ReferenceDataCollection<TRef>`, etc.) to allow advanced scenarios such as custom Id types or alternative collection strategies without coupling to the framework defaults.

`ReferenceDataCore<TId>` defines the canonical property contract: `Id`, `Code`, `Text`, `Description`, `SortOrder`, `IsActive`, `StartsOn`, `EndsOn`, `IsValid`, and `IsInactive`. Text and Description are resolved through `LText` on each get so they honour the ambient locale. Equality and hashing use only `Id` and `Code` to remain key-stable across context switches. Invalid state can be forced via `SetInvalid` to represent items that failed mapping or were rejected by the orchestrator.

`ReferenceDataCollectionCore<TId, TRef>` manages items in concurrent dictionaries keyed by both `Id` and `Code`, enforces uniqueness at add time (throwing `InvalidOperationException` on duplicate Id or Code), and exposes `GetItems(sortOrder, activeOnly, isValid)` returning a stable, sorted `IEnumerable<TRef>`. An internal `StringComparer` for Code comparisons is injected at construction, defaulting to `OrdinalIgnoreCase`.

## Key capabilities

- 🧱 **Canonical reference data contract**: `ReferenceDataCore<TId>` exposes the full set of reference data properties, value-semantic equality keyed on `Id`/`Code`, JSON property-order attributes for consistent serialization, and read/write access compatible with EF Core and other ORMs.
- 🔍 **Orchestrator lookup hooks**: static `TryGetById` and `TryGetByCode` members are wired to the ambient `ReferenceDataOrchestrator`, allowing any reference data type to resolve itself without an injected dependency.
- 📋 **Thread-safe dual-key indexing**: `ReferenceDataCollectionCore<TId, TRef>` maintains separate `ConcurrentDictionary` caches for Id and Code lookups, with a reader— writer — locked `List<TRef>` backing store for `GetItems` ordering.
- **Validity and active filtering**: `GetItems` accepts optional `activeOnly` and `isValid` flags; item validity is evaluated against the ambient `ReferenceDataContext` date, so temporal reference data is correctly filtered at read time.
- **Extensible initialization**: `OnInitialization()` is called at the end of the collection constructor, providing a clean hook for subclass-specific setup without overriding the constructor.

## Key types

| Type | Description |
|------|-------------|
| _[`ReferenceDataCore<TId>`](./ReferenceDataCore.cs)_ | Abstract base for all reference data; defines `Id`, `Code`, `Text`, `Description`, `SortOrder`, `IsActive`, `StartsOn`/`EndsOn`, `IsValid`, `IsInactive`, and mapping support. Equality and hash code use `Id` and `Code`. |
| _[`ReferenceDataCollectionCore<TId, TRef>`](./ReferenceDataCollectionCore.cs)_ | Abstract base for reference data collections; maintains concurrent Id-keyed and Code-keyed dictionaries, enforces duplicate-key constraints at add time, and exposes sorted/filtered `GetItems`. |

## Related namespaces

- **[`CoreEx.RefData`](../README.md)** - Contains the concrete `ReferenceData<TSelf>`, `ReferenceData<TId, TSelf>`, `ReferenceDataCollection<TRef>`, and `ReferenceDataCollection<TId, TRef>` specializations built on top of these abstractions.