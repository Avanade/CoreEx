# CoreEx.UnitTesting.Data

> Provides `JsonDataReader` — a hierarchical mutating reader for JSON or YAML data with dynamic property substitution — and supporting option and argument types that configure its behaviour.

## Overview

`CoreEx.UnitTesting.Data` centres on `JsonDataReader`, which parses a JSON or YAML document into a `JsonNode` tree and then deep-copies it while resolving `^token` placeholders and `(^token)` embedded substitutions at every node. The result is a fully substituted `JsonNode` that the caller can navigate or deserialise as needed — typically to seed a test database, drive an API request body, or assert expected response content.

Substitution values come from the `JsonDataReaderOptions.Parameters` dictionary, which maps token names (case-insensitive) to factory functions that receive a `JsonDataReaderArgs` instance. Nine tokens are registered by default: `^id`, `^guid`, `^now`, `^tomorrow`, `^yesterday`, `^tenant_id`, `^user_id`, `^user_name`, and `^index`. The caller can add further tokens or override existing ones at any time. In addition, `JsonDataReaderOptions.Properties` holds key-value pairs that are injected into every root-most `JsonObject` in the output where the property is not already present; the `AddStandardProperties()` helper pre-registers `CreatedOn`, `CreatedBy`, and `TenantId` for change-log seeding scenarios.

The `TryGetPath` method retrieves a deep clone of a subtree by dot-notation path without performing substitution; `TryCreateData` combines path resolution with full substitution and returns the mutated node; `Deserialize<T>` is a convenience wrapper that calls `TryCreateData` and then deserialises the substituted node to the requested type, automatically aligning `JsonSerializerOptions.PropertyNamingPolicy` with the configured `JsonPropertyNamingConvention`.

## Key capabilities

- 🧬 **Hierarchical mutating reader**: `JsonDataReader` deep-copies the parsed `JsonNode` tree during substitution so the original document is never modified; objects, arrays, and scalar values are all visited recursively.
- 🔧 **`^token` and `(^token)` substitution**: scalar string values prefixed with `^` are replaced wholesale; `(^token)` patterns embedded within a longer string are substituted in-place; both forms resolve against the same `Parameters` dictionary and support chained substitution.
- **Property injection**: `JsonDataReaderOptions.Properties` are merged into every root-most `JsonObject` in the output when the key is absent; a `RootNodePreProcessor` delegate allows arbitrary node manipulation before standard substitution runs.
- 📝 **Configurable naming conventions**: `JsonPropertyNamingConvention` (`PascalCase`, `CamelCase`, `SnakeCase`, `KebabCase`, `None`) controls the name transformation applied when `Deserialize<T>` constructs its `JsonSerializerOptions`.
- 📦 **Direct deserialisation**: `Deserialize<T>(path)` resolves the substituted subtree and deserialises it to any type in one call, using the naming convention to align property mapping automatically.
- **YAML and JSON input**: static factory methods `ParseYaml` and `ParseJson` accept strings, streams, `TextReader`, embedded resources (via a `TResource` type parameter), or raw `JsonNode` instances, making it straightforward to load fixtures from any source.

## Key types

| Type | Description |
|------|-------------|
| **[`JsonDataReader`](./JsonDataReader.cs)** | Hierarchical mutating reader; parses JSON or YAML into a `JsonNode` root, resolves `^token`/`(^token)` placeholders via `TryCreateData`, and exposes `Deserialize<T>` for direct typed deserialisation. Factory methods: `ParseJson`, `ParseYaml`. |
| **[`JsonDataReaderArgs`](./JsonDataReaderArgs.cs)** | Per-node runtime context passed to every `Parameters` factory function; exposes `Root`, `CurrentPropertyName`, `CurrentNode`, `Index`, and the live `Parameters` dictionary. |
| **[`JsonDataReaderOptions`](./JsonDataReaderOptions.cs)** | Configuration for a `JsonDataReader` instance: `NamingConvention`, `Parameters` token dictionary, `Properties` injection map, `SerializerOptions`, `TenantId` fallback, `RootNodePreProcessor` hook, and the `AddStandardProperties()` / `ForReferenceData()` convenience builders. |
| **[`JsonPropertyNamingConvention`](./JsonPropertyNamingConvention.cs)** | Enum: `PascalCase`, `CamelCase`, `SnakeCase`, `KebabCase`, `None`; controls the property-name transformation applied during deserialisation. |

## Related namespaces

- **[`CoreEx.UnitTesting`](../README.md)** - Root namespace; `JsonDataReader` is consumed by the DbEx-based SQL Server and PostgreSQL database-seeding helpers to populate test databases from JSON/YAML fixture files.