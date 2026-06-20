# CoreEx.Metadata

> Provides `RuntimeMetadata` and `IPropertyRuntimeMetadata` — the reflection-backed (and source-generator-backed) property introspection layer used by `IContract<T>` implementations for equality, hashing, copying, cleaning, and `IsDefault` operations.

## Overview

`CoreEx.Metadata` is the engine behind `IContract<T>` operations. When the CoreEx source generator decorates a `partial class` with `[Contract]`, it emits `GetStaticPropertyRuntimeMetadata()` implementations that return typed `IPropertyRuntimeMetadata` descriptors for each property. At runtime, `RuntimeMetadata` uses those descriptors (or falls back to reflection with caching) to implement the shared entity contract operations: `AreEqual`, `CopyInto`, `Clean`, `GetHashCode`, and `IsDefault`.

Consumers rarely interact with this namespace directly — its API surface is primarily for generated code and for the handful of framework utilities (e.g. `Cleaner`, `EntityBase`) that need property-level introspection. It is documented here for completeness and for contributors working on the source generator or advanced contract scenarios.

## Key capabilities

- 🔍 **Property runtime metadata**: `IPropertyRuntimeMetadata` describes a single entity property: name, type, whether it is a key property, whether to include in equality/hash/clean/copy/`IsDefault` operations, and getter/setter delegates.
- 🏎️ **Compiled delegates**: Property accessors are compiled to typed `Func<TEntity, TProperty>` / `Action<TEntity, TProperty>` delegates (cached per type) rather than using `PropertyInfo.GetValue`, eliminating reflection overhead on the hot path.
- 📦 **Per-type caching**: Property metadata collections are cached in a `ConcurrentDictionary` with a configurable sliding expiration (`CoreEx:Runtime:Metadata:SlidingExpirationTimespan`, default 30 min) to balance memory and startup cost.
- ✂️ **Partial static class**: `RuntimeMetadata` is a `static partial class` with separate files for each operation (`AreEqual`, `Clean`, `CopyInto`, `GetHashCode`, `IsDefault`, `Internal`), keeping each concern isolated.
- 🔁 **Reflection fallback**: When no source-generated `GetStaticPropertyRuntimeMetadata()` is available (e.g. for non-`[Contract]` types), `PropertyRuntimeMetadataReflector` builds the metadata from `PropertyInfo` via reflection and caches it.

## Key types

| Type | Description |
|------|-------------|
| **[`RuntimeMetadata`](./RuntimeMetadata.cs)** | Static partial class providing `AreEqual`, `CopyInto`, `Clean`, `GetHashCode`, `IsDefault`, and `GetForExpression` operations over `IContract<T>` types. |
| **[`PropertyRuntimeMetadataReflector`](./PropertyRuntimeMetadataReflector.cs)** | Builds `IPropertyRuntimeMetadata` collections from `PropertyInfo` via reflection for types without source-generated metadata. |
| [`IPropertyRuntimeMetadata`](./IPropertyRuntimeMetadata.cs) | Descriptor for a single property: name, type, key flag, equality/clean/copy/hash/isdefault inclusion flags, and compiled getter/setter. |
| [`PropertyRuntimeMetadata`](./PropertyRuntimeMetadata.cs) | Default `IPropertyRuntimeMetadata` implementation used by both the source generator and reflection paths. |
| [`IRuntimeMetadata`](./IRuntimeMetadata.cs) | Interface implemented by `IContract<T>` types providing `GetStaticPropertyRuntimeMetadata()` for source-generated fast-path access. |
| [`IRuntimeMetadataCore`](./IRuntimeMetadataCore.cs) | Minimal interface subset of `IRuntimeMetadata` used by non-`IContract<T>` infrastructure that still needs property-level introspection. |

## Related Namespaces

- **[`CoreEx.Entities`](../Entities/README.md)** - `IContract<T>` extends `IRuntimeMetadata`; `Cleaner` delegates to `RuntimeMetadata.Clean`; `IdentifierGenerator` and change-log population rely on property metadata.
- **[`CoreEx.Generator`](../../../gen/CoreEx.Generator/README.md)** - The Roslyn source generator emits `GetStaticPropertyRuntimeMetadata()` implementations consumed by `RuntimeMetadata`.