# CoreEx.EntityFrameworkCore

> Provides the Entity Framework Core integration layer: `EfDb<TDbContext>` as the CoreEx-EF bridge, `EfDbModel<TModel>` and `EfDbMappedModel<TValue, TModel, TMapper>` for typed CRUD + query operations, `EfDbExtensions` for paged `IQueryable<T>` mapping helpers, `EfDbInvoker` for structured operation logging, and EF `ValueConverter` bridges for CoreEx converter types.

## Overview

`CoreEx.EntityFrameworkCore` wraps EF Core's `DbContext` with the CoreEx data conventions: `IUnitOfWork` transaction management, `ETag`/concurrency-token checking, multi-tenancy filtering, logical-delete filtering, change-log stamping, `PagingArgs` paging, `QueryArgsConfig` dynamic filter/orderby, and `Result<T>` pipeline integration.

The central type is `EfDb<TDbContext>`, which holds the `DbContext`, bridges its `IDatabase` (for transaction sharing with raw SQL), and exposes `Model<TModel>()` as the entry point for all strongly-typed CRUD. `EfDbModel<TModel>` provides `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, and `Query` — each applying the full CoreEx cross-cutting pipeline. `EfDbMappedModel<TValue, TModel, TMapper>` adds a `IBiDirectionMapper` layer for use cases where the EF model type differs from the domain entity type.

`EfDbExtensions` provides `IQueryable<T>` extension methods for mapping, paging, and `ItemsResult<T>` collection building, enabling consistent paged-list patterns across all EF-backed repositories without boilerplate.

## Key capabilities

- 🔗 **EF + IDatabase bridge**: `EfDb<TDbContext>` synchronizes EF Core's transaction with the underlying `IDatabase.CurrentTransaction`, so raw SQL commands and EF operations participate in the same ADO.NET transaction.
- 📖 **Typed CRUD**: `EfDbModel<TModel>` provides `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` with automatic ETag/concurrency-token validation, tenant isolation, logical-delete filtering, and change-log stamping.
- 🔁 **Mapped CRUD**: `EfDbMappedModel<TValue, TModel, TMapper>` layers a `IBiDirectionMapper<TValue, TModel>` over `EfDbModel<TModel>`, mapping between the domain entity type and the EF model type transparently for all CRUD operations.
- 🔍 **Query with configured filters**: `EfDbModel<TModel>.Query(args?)` / `QueryTracked(args?)` return a plain `IQueryable<TModel>` with any `WithFilter`/`WithTenantFilter`/`WithLogicalDeleteFilter` predicates from `EfDbModelOptions` applied; pair with `CoreEx.Data`'s `QueryArgsConfig` and the `EfDbExtensions` paging helpers to build dynamic filter/orderby/paged queries.
- 📄 **Paged `IQueryable` extensions**: `EfDbExtensions.ToItemsResultAsync`, `ToMappedItemsResultAsync`, `ToMappedItemsAsync` convert an `IQueryable<TSource>` to paged `ItemsResult<T>` or collections with a mapper function or `IMapper<T>`.
- 🏷️ **ETag / concurrency token**: for a detached `UpdateAsync`, `EfDbModel` compares the incoming model's `ETag` against the freshly-fetched entity's concurrency token via `ETag.TryCompare`, returning a `ConcurrencyError` result on mismatch.
- 🔒 **Multi-tenancy**: non-query operations (`GetAsync`/`CreateAsync`/`UpdateAsync`/`DeleteAsync`) automatically reject a mismatched `IReadOnlyTenantId.TenantId` as not-found; `Query()` only applies the equivalent `TenantId == executionContext.TenantId` predicate when `EfDbModelOptions.WithTenantFilter()` has been configured.
- 🗑️ **Logical delete**: entities implementing `IReadOnlyLogicallyDeleted` are soft-deleted (`IsDeleted = true`) on `DeleteAsync` rather than physically removed; non-query operations automatically treat a logically-deleted row as not-found, while `Query()` only excludes them when `EfDbModelOptions.WithLogicalDeleteFilter()` has been configured.
- 🔌 **ValueConverter bridge**: `ValueConverterBridge<TModel, TProvider>` and `JsonElementStringEfConverter` allow CoreEx `IConverter<T, U>` implementations to be used directly as EF Core `ValueConverter` instances in `OnModelCreating`.
- 📝 **Structured logging**: `EfDbInvoker` wraps every `EfDb` operation with structured log entries (tracing/`Activity` spans are intentionally disabled via `IsTracingDisabled`).

## Key types

| Type | Description |
|------|-------------|
| **[`EfDb<TDbContext>`](./EfDb.cs)** | CoreEx EF bridge: holds `DbContext`, `IDatabase`, `EfDbOptions`, `ExecutionContext`; exposes `Model<TModel>()` entry point; synchronizes EF and ADO.NET transactions. |
| **[`EfDbModel<TModel>`](./EfDbModel.cs)** | Strongly-typed CRUD + query for a single EF model type: `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`, `Query(args?)`; applies full CoreEx cross-cutting pipeline. |
| **[`EfDbMappedModel<TValue, TModel, TMapper>`](./EfDbMappedModel.cs)** | Adds a `IBiDirectionMapper<TValue, TModel>` layer over `EfDbModel<TModel>` for domain entity ↔ EF model type conversion; provides `GetAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`. |
| **[`EfDbExtensions`](./EfDbExtensions.cs)** | `IQueryable<T>` extensions: `ToMappedItemsResultAsync`, `ToItemsResultAsync`, `ToMappedCollectionAsync`, `BuildQuery(QueryArgs, QueryArgsConfig)`. |
| **[`EfDbArgs`](./EfDbArgs.cs)** | Per-operation options: `QueryTracking`, `ClearChangeTrackerAfterGet`, `SaveChanges`, `BypassFilters` (plus inherited `Refresh`/`TransformException`); defaults sourced from `EfDbModelOptions<TModel>.Args` then `EfDb.Options.Args`. |
| **[`EfDbOptions`](./EfDbOptions.cs)** | Instance-level options for `EfDb<TDbContext>`: default `EfDbArgs`, per-model options registry via `GetOrAddModelOptions<TModel>()`. |
| **[`EfDbModelOptions<TModel>`](./EfDbModelOptions.cs)** | Per-model configuration: `WithArgs`, `WithGetKey`, `WithFilter`/`WithTenantFilter`/`WithLogicalDeleteFilter`, `WithOnBeforeCreateOrUpdate`, `WithUpdateModelMapper`. |
| **[`EfDbInvoker`](./EfDbInvoker.cs)** | `InvokerBase<IEfDb>` emitting structured log entries for every EfDb operation (tracing intentionally disabled); `Default` singleton used by `EfDb<TDbContext>`. |
| **[`ValueConverterBridge<TModel, TProvider>`](./Converters/ValueConverterBridgeT2.cs)** | EF Core `ValueConverter<TModel, TProvider>` that delegates to a CoreEx `IConverter<TModel, TProvider>`, bridging CoreEx converter types into EF model configuration. |
| **[`JsonElementStringEfConverter`](./Converters/JsonElementStringEfConverter.cs)** | EF Core `ValueConverter` serializing `JsonElement` values to/from `string` for storing JSON fragments in a text column. |
| [`IEfDb`](./IEfDb.cs) | Interface exposing `DbContext`, `IDatabase`, `EfDbOptions`, `ExecutionContext`, and `EfDbInvoker`; implemented by `EfDb<TDbContext>`. |
| [`IEfDbContext`](./IEfDbContext.cs) | Marker interface for `DbContext` subclasses wiring the `IDatabase` bridge via `BaseDatabase`. |

## Namespaces

| Namespace | Description |
|-----------|-------------|
| [**`Converters`**](./Converters/) | `ValueConverterBridge<TModel, TProvider>` and `JsonElementStringEfConverter` for use in EF Core model configuration. |

## Related Namespaces

- **[`CoreEx.Data`](../CoreEx.Data/README.md)** - `IUnitOfWork`, `DataResult`, `QueryArgsConfig`; EF unit-of-work is typically composed using `EfDb` with an outbox publisher.
- **[`CoreEx.Database`](../CoreEx.Database/README.md)** - `IDatabase` is bridged into `EfDb<TDbContext>` for transaction sharing and raw SQL fallback; `IDatabaseUnitOfWork` can wrap `EfDb`.
- **[`CoreEx.Mapping`](../CoreEx/Mapping/README.md)** - `IBiDirectionMapper<TValue, TModel>` is the mapper contract used by `EfDbMappedModel`; `Mapper.MapStandardFrom` handles standard entity-contract properties.
- **[`CoreEx.Invokers`](../CoreEx/Invokers/README.md)** - `EfDbInvoker` extends `InvokerBase<IEfDb>` using the standard OpenTelemetry tracing and logging pipeline.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.