# CoreEx.DomainDriven

> Provides the foundational Domain-Driven Design (DDD) building blocks for CoreEx: typed entities, aggregate roots with integration-event support, persistence-state tracking, and mutation-guard helpers.

## Overview

`CoreEx.DomainDriven` implements the core DDD concepts referenced throughout the CoreEx framework. It supplies base classes and interfaces for **entities** and **aggregate roots** that are identity-based, change-tracked, and mutation-guarded, integrating cleanly with CoreEx contracts (`IIdentifier`, `IChangeLog`, `IETag`) and the broader `Result<T>` pipeline.

The package intentionally stays minimal: it does not dictate a persistence strategy, an ORM, or an event bus. Instead it establishes the behavioural invariants — read-only enforcement, persistence state transitions, event accumulation — that the application and infrastructure layers rely on. Higher-level packages such as `CoreEx.EntityFrameworkCore` and `CoreEx.Database` consume these contracts when mapping and persisting domain objects.

## Key capabilities

- 🆔 **Typed entity base**: `Entity<TId, TSelf>` enforces identity-based equality (`Equals` considers `Id` only) and carries a fluent self-typed API for state manipulation.
- 🔄 **Persistence state machine**: `PersistenceState` (`Unknown → New / NotModified → Modified → Removed`) with extension helpers (`IsNew`, `IsModified`, `IsNewOrModified`, `IsRemoved`, …) and validated one-way transitions that prevent illegal state changes.
- 🛡 **Mutation guards**: `Modify(action)`, `Remove(action)`, `ModifyAndMakeReadOnly`, and `ModifyAndMakeReadOnly<TResult>` wrappers enforce `CheckReadOnly` and `OnCheckCanMutate` before any state change, then advance `PersistenceState` automatically and fire the `Mutated` event.
- 🔒 **Read-only enforcement**: `MakeReadOnly()` / `IsReadOnly` locks an entity after hydration or after terminal mutations; all `Modify`/`Remove` paths throw `InvalidOperationException` when violated.
- 📢 **Aggregate root with integration events**: `Aggregate<TId, TSelf>` adds `AddEvent(EventData)` / `ClearEvents()` and the `Events` / `HasEvents` members for transient integration-event accumulation inside a unit-of-work scope.
- 🔧 **Override hooks**: `OnCheckCanMutate()` returns a `Result` for pre-mutation business-rule validation; `OnMutate()` is a post-mutation extension point; both are called inside every `Modify`/`Remove` wrapper.
- 💧 **Hydration helpers**: `SetChangeLog`, `SetETag`, and `SetPersistenceState` bypass read-only and state-change logic by design, allowing infrastructure layers to rehydrate an entity from persistence without triggering mutation semantics.
- 🔑 **CompositeKey support**: `EntityKey` exposes the entity identity as a `CompositeKey` for infrastructure layers that need a key independent of the typed `Id`.

## Key types

| Type | Description |
|------|-------------|
| [`IEntity`](./IEntity.cs) | Core DDD entity contract: `PersistenceState`, `IsReadOnly`, `IReadOnlyIdentifier`, `IReadOnlyChangeLog`, `IReadOnlyETag`. |
| [`IAggregateRoot`](./IAggregateRoot.cs) | Extends `IEntity` with `Events` (read-only collection of `EventData`) and `HasEvents`; integration events only — domain events are not supported by design. |
| _[`EntityBase`](./EntityBase.cs)_ | Abstract base implementing `IEntity`; provides the full mutation-guard (`Modify`, `Remove`), read-only, state-machine, `SetChangeLog`/`SetETag` hydration, `Mutated` event, and `OnCheckCanMutate`/`OnMutate` hooks. |
| _[`Entity<TId, TSelf>`](./Entity.cs)_ | Abstract typed, self-referential entity (`TSelf` pattern) with identity-based equality; re-exposes `SetPersistenceState`, `AsNew`, `AsNotModified`, `MakeReadOnly`, `SetChangeLog`, and `SetETag` as fluent `TSelf`-returning methods. |
| **[`Aggregate<TId, TSelf>`](./Aggregate.cs)** | Extends `Entity<TId, TSelf>` with `AddEvent(EventData)` / `ClearEvents()` for integration-event accumulation within the aggregate lifetime. |
| **[`PersistenceState`](./PersistenceState.cs)** | Enum: `Unknown`, `New`, `NotModified`, `Modified`, `Removed`; governs the entity lifecycle from creation through persistence to deletion. |
| **[`DomainDrivenExtensions`](./DomainDrivenExtensions.cs)** | Extension methods on `PersistenceState`: `IsNew`, `IsNotModified`, `IsModified`, `IsRemoved`, `IsNotRemoved`, `IsNewOrModified`. |

## Related namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Provides `IIdentifier`, `IChangeLog`, `IETag`, `CompositeKey`, `EventData`, and `Result<T>` consumed by the DDD types.
- **[`CoreEx.EntityFrameworkCore`](../CoreEx.EntityFrameworkCore/README.md)** - Persists `Entity<TId, TSelf>` and `Aggregate<TId, TSelf>` via `EfDbModel<TModel>`; uses `PersistenceState` to determine insert/update/delete operations.
- **[`CoreEx.Validation`](../CoreEx.Validation/README.md)** - Validates entity state; `OnCheckCanMutate` can delegate to a CoreEx validator for pre-mutation rule checks.