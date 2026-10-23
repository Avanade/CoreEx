# CoreEx.Json

> Provides CoreEx-specific `System.Text.Json` defaults, converters, naming policies, JSON Merge Patch (RFC 7396), JSON property filtering (include/exclude), and reference-data-aware serialization.

## Overview

`CoreEx.Json` builds on `System.Text.Json` to provide a cohesive, pre-configured serialization layer for CoreEx applications. Rather than each service independently configuring `JsonSerializerOptions`, all serialization flows through `JsonDefaults`, which exposes a centrally configured `JsonSerializerOptions` instance (defaulting to camelCase, `WhenWritingDefault`, `JsonStringEnumConverter`, and CoreEx-specific converters) and resolves the runtime instance from the ambient `ExecutionContext` where available.

Beyond basic serialization, the namespace provides two advanced document-level operations: `JsonMergePatch` (RFC 7396) for partial update semantics used in `PATCH` endpoints, and `JsonFilter` for surgical include/exclude projection of JSON properties — used by the `WebApi` helper to honor `$fields` query parameters. Custom converters handle CoreEx entity types (`DataMap<TValue>` and reference data codes) transparently.

Property names use camelCase and the configured substitutions (for example, `ETag` becomes `etag`), but dictionary keys are data and are preserved exactly by default (`DictionaryKeyPolicy = null`). This preserves identifiers such as `P1` across JSON round-trips, including when an unrelated merge-patch change causes the target to be deserialized again. Validation and expression-based error paths preserve dictionary keys by default and follow an explicitly configured `DictionaryKeyPolicy` when supplied. Applications that intentionally transform keys can configure that policy at startup; deserialization does not reverse it.

Merge-patch member matching is case-sensitive by default (`JsonMergePatchOptions.PropertyNameComparer = StringComparer.Ordinal`), as required by RFC 7396, including for dictionary members. Clients should use the exact serialized property names: `name` and `Name` are distinct JSON members. The serializer's case-insensitive CLR property binding does not change JSON member identity; typed results remain subject to the configured deserializer and cannot necessarily represent distinct names that bind to the same CLR property. Setting `PropertyNameComparer = StringComparer.OrdinalIgnoreCase` explicitly restores the previous non-standard matching behavior at every object level. Object patches are always processed recursively, treating non-object targets as empty objects; null removes existing members and is ignored for nonexistent members. `HasChanges` describes changes to the JSON document, not necessarily the resulting CLR value.

## Key capabilities

- ⚙️ **Centralized defaults**: `JsonDefaults.Configuration` provides a single, customizable `JsonDefaultConfiguration` instance; `JsonDefaults.SerializerOptions` resolves the runtime options from `ExecutionContext` or the static configuration.
- 🔤 **Substitute naming policy**: `JsonSubstituteNamingPolicy` wraps any naming policy and applies registered name substitutions (e.g. `Id` → `id`), enabling per-property JSON name overrides without `[JsonPropertyName]` attributes on every type.
- 🔀 **JSON Merge Patch (RFC 7396)**: `JsonMergePatch` merges a patch JSON document into a target object, returning a `JsonMergePatchResult<T>` carrying the merged value and a `HasChanges` flag.
- 📐 **JSON property filtering**: `JsonFilter` applies include or exclude path-based filters to a `JsonNode` or raw JSON string, removing unwanted properties. Used to implement `$fields` response projection. Supports recursive descent paths (`$..Foo`) to match a property at any depth in the hierarchy.
- ⚡ **Streaming exclude**: `JsonFilter.TryExcludeUtf8Json` applies an exclude filter directly against UTF-8 JSON bytes via a single-pass `Utf8JsonReader`/`Utf8JsonWriter` copy, without ever materializing a `JsonNode` document — used internally wherever an exclude filter runs on raw JSON (e.g. `EventFormatter.DataExcludePaths`), meaningfully cheaper for high-volume paths than the `JsonNode`-based route that `Include` still requires.
- 🗃️ **Reference data converter**: `JsonReferenceDataConverter` serializes reference data types as their `Code` string and deserializes back to the full reference data instance via `ReferenceDataOrchestrator`.
- 🗺️ **Data map converter**: `JsonDataMapConverterFactory` handles `DataMap<TValue>` serialization, preserving exact key casing rather than applying the global naming policy.
- ⚠️ **Exception converter**: `JsonExceptionConverterFactory` serializes `Exception` instances to a structured JSON object (type, message, inner exception chain), used when surfacing exception details in diagnostic responses.

## Key types

| Type | Description |
|------|-------------|
| **[`JsonDefaults`](./JsonDefaults.cs)** | Central access point for the application's `JsonSerializerOptions`; resolves from `ExecutionContext` or the static `JsonDefaultConfiguration`. |
| **[`JsonDefaults.JsonDefaultConfiguration`](./JsonDefaults.cs)** | Sealed inner class holding the default `JsonSerializerOptions` with CoreEx-specific converters and naming policy; customize at startup before any serialization occurs. |
| **[`JsonSubstituteNamingPolicy`](./JsonSubstituteNamingPolicy.cs)** | `JsonNamingPolicy` implementation that applies registered name substitutions on top of a delegate naming policy (default: camelCase). |
| **[`JsonMergePatch`](./JsonMergePatch.cs)** | Performs RFC 7396 JSON Merge Patch: merges a patch `BinaryData`/string into a typed target, returning `Result<JsonMergePatchResult<T>>` with the merged value and a change flag. |
| **[`JsonMergePatchOptions`](./JsonMergePatchOptions.cs)** | Options for `JsonMergePatch`: serializer options and the `PropertyNameComparer` (default: `StringComparer.Ordinal`). |
| **[`JsonMergePatchResult<T>`](./JsonMergePatchResult.cs)** | Result from `JsonMergePatch.Merge<T>()`: carries the `Merged` value and a `HasChanges` flag. |
| **[`JsonFilter`](./JsonFilter.cs)** | Static utility applying include/exclude JSON path filters to a `JsonNode`, JSON string, or (via `TryExcludeUtf8Json`, exclude-only) raw UTF-8 bytes with no `JsonNode` allocation; exact path matching with optional indexed or non-indexed path support, plus recursive descent (`$..Foo`) paths that match at any depth. |
| **[`JsonFilterOption`](./JsonFilterOption.cs)** | Enum controlling `JsonFilter` behavior: `Include` retains only listed paths; `Exclude` removes listed paths. |
| [`JsonReferenceDataConverter`](./JsonReferenceDataConverter.cs) | `JsonConverter` that serializes reference data instances as their `Code` string and deserializes by looking up the code in `ReferenceDataOrchestrator`. |
| [`JsonDataMapConverterFactory`](./JsonDataMapConverterFactory.cs) | `JsonConverterFactory` that handles `DataMap<TValue>` serialization with key-casing preservation. |
| [`JsonExceptionConverterFactory`](./JsonExceptionConverterFactory.cs) | `JsonConverterFactory` that serializes `Exception` instances to structured JSON for diagnostic purposes. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - `ExecutionContext` carries the runtime `JsonSerializerOptions` instance consumed by `JsonDefaults.SerializerOptions`.
- **[`CoreEx.Entities`](../Entities/README.md)** - `DataMap<TValue>` is serialized by `JsonDataMapConverterFactory`; `ReferenceDataBase` types are serialized by `JsonReferenceDataConverter`.
- **[`CoreEx.RefData`](../RefData/README.md)** - `ReferenceDataOrchestrator` is called by `JsonReferenceDataConverter` to resolve codes back to reference data instances during deserialization.
- **[`CoreEx.AspNetCore`](../../CoreEx.AspNetCore/README.md)** - `WebApi` helpers use `JsonMergePatch` for `PATCH` operations and `JsonFilter` for `$fields` response projection.

## Additional Resources

- [RFC 7396 — JSON Merge Patch](https://tools.ietf.org/html/rfc7396) - The specification that `JsonMergePatch` implements.
