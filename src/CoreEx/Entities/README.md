# CoreEx.Entities

> Provides the core entity contract interfaces, audit change-log types, composite key support, message items, string/datetime cleaning utilities, and identifier generation used as the common domain contract shape across all CoreEx layers.

## Overview

`CoreEx.Entities` defines the shared vocabulary for domain entities within the CoreEx framework. Rather than mandating a base class, it exposes a set of fine-grained interfaces that consuming types opt into selectively — `IIdentifier<T>` for typed identity, `IETag` for optimistic concurrency, `IChangeLog` for audit tracking, `IEntityKey` for key-equality, and `IContract` as the aggregate contract used by generated and hand-authored domain types alike.

These interfaces are intentionally lightweight and read/write split. Every mutable interface (e.g. `IIdentifier<T>`, `IETag`, `IChangeLog`) has a corresponding read-only counterpart (`IReadOnlyIdentifier<T>`, `IReadOnlyETag`, `IReadOnlyChangeLog`), enabling infrastructure layers to consume and produce values without inadvertently mutating the entity.

The namespace also includes supporting utilities: `Cleaner` for normalizing string and `DateTime` values before persistence, `MessageItem` and `MessageItemCollection` for structured field-level validation messages, `CompositeKey` for allocation-efficient multi-part keys, and `IdentifierGenerator` for new GUID-based identifier creation.

## Key capabilities

- 🆔 **Identity contracts**: `IIdentifier<T>` / `IReadOnlyIdentifier<T>` provide a typed identifier property used uniformly across API, application, and infrastructure layers.
- 🏷️ **ETag / optimistic concurrency**: `IETag` / `IReadOnlyETag` carry the entity tag string used for HTTP and data-layer concurrency checking; `ETag` static utility handles comparison and conflict detection.
- 📋 **Audit change log**: `IChangeLog` / `IChangeLogEx` track `CreatedBy`, `CreatedDate`, `UpdatedBy`, `UpdatedDate`; `ChangeLog` record implements the full audit trail populated from the ambient `ExecutionContext`.
- 🔑 **Composite keys**: `CompositeKey` is an immutable, allocation-efficient struct supporting 1–4 typed arguments without boxing, used as a key type across data-layer filtering and repository operations.
- 📨 **Message items**: `MessageItem` and `MessageItemCollection` carry structured field-level messages (Error, Warning, Info) from validation and service operations back to the consumer.
- 🧹 **Cleaning and normalization**: `Cleaner` applies configurable `StringTrim`, `StringTransform` (null/empty), `StringCase` (upper/lower/title), and `DateTimeTransform` (UTC/local) policies across entity properties before save.
- 📐 **Feature detection**: `FeatureSupport` and extension methods allow infrastructure code to detect which interfaces an entity implements and act accordingly (e.g. auto-populate ETags or change logs).
- ✍️ **Write-intent descriptors**: `Writable` enum and `[Writable]` attribute annotate entity properties with create-only / update-only / never-writable semantics for OpenAPI documentation and tooling.
- 🔧 **Identifier generation**: `IdentifierGenerator` and `IIdentifierGenerator` provide DI-replaceable new `Guid`/`string` identifier creation (using `Guid.CreateVersion7()` on .NET 9+).

## Key types

| Type | Description |
|------|-------------|
| **[`ChangeLog`](./ChangeLog.cs)** | Record implementing `IReadOnlyChangeLogEx`; captures created/updated by and timestamp, populated from the ambient `ExecutionContext`. |
| **[`Cleaner`](./Cleaner.cs)** | Static utility applying configurable string trimming, transformation, casing, and `DateTime` normalization to entity values. |
| **[`CompositeKey`](./CompositeKey.cs)** | Immutable struct representing a multi-part entity key with boxing-free generic `Create<T1,T2,...>` overloads for up to four arguments. |
| **[`CompositeKeyComparer`](./CompositeKeyComparer.cs)** | `IEqualityComparer<CompositeKey>` for use in dictionaries and collections keyed by `CompositeKey`. |
| **[`DataMap<TValue>`](./DataMap.cs)** | String-keyed dictionary whose keys are preserved as-is (no camelCase conversion) during JSON serialization; used for arbitrary data maps on entities. |
| **[`ETag`](./ETag.cs)** | Static utility for comparing ETag strings and throwing `ConcurrencyException` on mismatch; also generates ETag hashes from entity values. |
| **[`FeatureSupport`](./FeatureSupport.cs)** | Enum of flags indicating which entity contract interfaces a type implements, used by infrastructure to decide which fields to auto-populate. |
| **[`IdentifierGenerator`](./Extended/IdentifierGenerator.cs)** | Default `IIdentifierGenerator` producing `Guid` or `string` identifiers (`Guid.CreateVersion7()` on .NET 9+; `Guid.NewGuid()` on earlier targets). |
| **[`MessageItem`](./MessageItem.cs)** | Record carrying a single structured message — type (Error/Warning/Info), text (`LText`), and optional property name — returned from validation and service operations. |
| **[`MessageCollection`](./MessageCollection.cs)** | Collection of `MessageItem` instances; attached to `ValidationException` and surfaced via `ExecutionContext.Messages`. |
| **[`ValueResult`](./ValueResult.cs)** | Wraps a value alongside an `IResult` to carry both a typed result and an associated result state. |
| **[`Writable`](./Writable.cs)** | Enum describing property write intent: `Always`, `Never`, `CreateOnly`, or `UpdateOnly`; used with `[WritableAttribute]` for OpenAPI tooling. |
| [`IContract`](./IContract.cs) | Marker interface combining `IRuntimeMetadata`, `ICopyFrom`, and `IDefault` — the aggregate contract implemented by generated domain types. |
| [`IContract<T>`](./IContractT.cs) | Generic version of `IContract` providing strongly-typed `CopyFrom(T)` and `IsDefault()` implementations via `RuntimeMetadata`. |
| [`IEntityKey`](./IEntityKey.cs) | Interface exposing `EntityKey` as a `CompositeKey` for uniform entity identity across repository and service boundaries. |
| [`IIdentifier<T>`](./IIdentifierT.cs) | Mutable interface adding a typed `Id` property; counterpart to `IReadOnlyIdentifier<T>`. |
| [`IETag`](./IETag.cs) | Mutable interface adding an `ETag` string property; counterpart to `IReadOnlyETag`. |
| [`IChangeLog`](./IChangeLog.cs) | Mutable interface adding `CreatedBy`, `CreatedDate`, `UpdatedBy`, `UpdatedDate` audit properties. |
| [`IChangeLogEx`](./IChangeLogEx.cs) | Extended version of `IChangeLog` using `DateTimeOffset` instead of `DateTime` for timezone-aware audit timestamps. |
| [`IIdentifierGenerator`](./Extended/IIdentifierGenerator.cs) | DI-replaceable interface for generating new typed identifiers (`Guid`, `string`). |
| [`ICopyFrom`](./Extended/ICopyFrom.cs) | Interface enabling an entity to copy its state from another instance of itself. |
| [`IDefault`](./Extended/IDefault.cs) | Interface enabling an entity to determine whether it is in its default (unset) state. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - Defines `ExecutionContext` and `AuthenticationUser` consumed by `ChangeLog` to populate audit fields.
- **[`CoreEx.Metadata`](../Metadata/README.md)** - `RuntimeMetadata` and `IPropertyRuntimeMetadata` underpin `IContract<T>` equality, copy, cleaning, and hash operations.
- **[`CoreEx.Mapping`](../Mapping/README.md)** - `Mapper` utility maps standard entity properties (`IIdentifier`, `IETag`, `IChangeLog`, etc.) between source and destination types.
- **[`CoreEx.Data`](../Data/README.md)** - Data-layer primitives such as `IPrimaryKey`, `IPartitionKey`, and `ILogicallyDeleted` extend the entity contract interfaces defined here.
- **[`CoreEx.Validation`](../../CoreEx.Validation/README.md)** - Validation rules produce `MessageItem` instances that are collected into a `ValidationException`.