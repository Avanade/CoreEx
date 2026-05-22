# CoreEx.Schemas

> Provides `[Schema]` attribute and `ISchemaVersion` interfaces for annotating entities with schema name and version metadata, enabling versioned event payload and API schema identification.

## Overview

`CoreEx.Schemas` is a small but focused namespace that defines the schema versioning contract used when publishing or consuming typed events and API payloads. The `[SchemaAttribute]` on an entity class carries a `Name`, `Version`, and optional `Description` — metadata that event publishers include in the event envelope so consumers can route and deserialize payloads correctly.

`Schema.TryGetMetadata<TEntity>()` provides a zero-allocation lookup of the attribute, returning a default `SchemaAttribute` (version `1.0`) where none is explicitly declared, so all entities have a deterministic schema version without requiring every class to be annotated.

## Key capabilities

- 🏷️ **Schema annotation**: `[SchemaAttribute]` marks an entity with a `Name` (defaults to the type name) and `Version` (defaults to `1.0`), carried into event envelopes by the event publishing infrastructure.
- 🔢 **Default version**: `Schema.DefaultVersion` is `Version(1, 0)`, returned automatically for un-annotated types so that all entities participate in schema versioning.
- 🔍 **Attribute lookup**: `Schema.TryGetMetadata<TEntity>()` and `Schema.TryGetMetadata(Type)` provide a single, cached lookup for the attribute, returning a defaulted instance when the attribute is absent.
- 🔒 **Read/write split**: `ISchemaVersion` (mutable) and `IReadOnlySchemaVersion` (read-only) allow infrastructure to stamp schema version onto event payloads while preventing consumer code from inadvertently mutating it.

## Key types

| Type | Description |
|------|-------------|
| **[`Schema`](./Schema.cs)** | Static utility: `DefaultVersion` (`1.0`), `TryGetMetadata<TEntity>()` / `TryGetMetadata(Type)` for attribute lookup with defaulting. |
| **[`SchemaAttribute`](./SchemaAttribute.cs)** | Attribute applied to entity classes: `Name` (defaults to type name), `Version` (`System.Version`, defaults to `1.0`), and `Description`. |
| [`ISchemaVersion`](./ISchemaVersion.cs) | Mutable interface adding `SchemaVersion` property for event payload stamping. |
| [`IReadOnlySchemaVersion`](./IReadOnlySchemaVersion.cs) | Read-only counterpart to `ISchemaVersion`. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - Root package; `EventData` and event-publishing infrastructure consume schema metadata from `[SchemaAttribute]` when building event envelopes.
- **[`CoreEx.Events`](../../CoreEx.Events/README.md)** - Event publishers read `[SchemaAttribute]` via `Schema.TryGetMetadata` to populate the `EventDataSchemaVersion` envelope property.