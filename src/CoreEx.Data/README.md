# CoreEx.Data

> Provides the `IUnitOfWork` transactional orchestration contract, `DataResult` mutation outcome types, data model base classes, and the `QueryArgsConfig` / `QueryFilterParser` / `QueryOrderByParser` pipeline for safe, explicitly-configured OData-style `$filter` and `$orderby` LINQ query translation.

## Overview

`CoreEx.Data` sits between the domain layer and persistence backends (SQL, EF Core, Cosmos). It defines the contracts and utilities that keep application services free of storage-specific types while still enabling rich, safe dynamic querying.

`IUnitOfWork` is the primary transactional boundary abstraction. Application services call `TransactionAsync` to scope a sequence of mutations and event enqueuing inside one transaction — regardless of whether the underlying store is SQL Server, PostgreSQL, or EF Core. The `Events` (`IEventQueue`) property on `IUnitOfWork` enables the transactional outbox pattern without coupling the service to any messaging infrastructure.

`QueryArgsConfig` is a configuration object that defines which fields a caller is allowed to filter and sort on, what operators each field supports, and how each field maps to the underlying model property. At request time, `QueryExtensions.BuildQuery(IQueryable<T>, QueryArgs, QueryArgsConfig)` parses the `$filter` and `$orderby` strings, validates them against the configuration, and appends the equivalent `System.Linq.Dynamic.Core` predicates to the `IQueryable<T>` — safely, with no arbitrary field exposure.

## Key capabilities

- 🔁 **Unit-of-work contract**: `IUnitOfWork` provides `TransactionAsync` and the `Events` (`IEventQueue`) property for transactional outbox event enqueuing; storage implementations are injected and swappable.
- 📦 **Mutation result types**: `DataResult` (no value) and `DataResult<T>` carry a `WasMutated` flag and optional mutated value, distinguishing "found and deleted" from "not found" without throwing exceptions.
- 🗄️ **Data model base classes**: `ModelBase<TId>` and `ReferenceDataModelBase` provide ready-made persistence model types implementing `IIdentifier<T>`, `IChangeLogEx`, `IETag`, and common `IReferenceData` properties respectively.
- 🔍 **Safe dynamic filter parsing**: `QueryFilterParser` parses OData-like `$filter` expressions against an explicit field allow-list, translates them to LINQ predicates, and rejects unknown fields or disallowed operators with structured parse errors.
- 📐 **Safe dynamic order-by parsing**: `QueryOrderByParser` parses `$orderby` expressions against an explicit field allow-list and translates them to LINQ `.OrderBy()`/`.ThenBy()` calls.
- ⚙️ **Field-level configuration**: Each field is configured with its CLR type, allowed operators, model property name/prefix, case normalization, null handling, and custom statement override via a fluent `QueryArgsConfig.WithFilter` / `WithOrderBy` builder.
- 🏷️ **Reference data filter fields**: `QueryFilterReferenceDataFieldConfig<T>` maps a reference data `Code` string in the filter to its underlying `Id` for persistence queries.
- 🔗 **IQueryable integration**: `QueryExtensions.Where` and `OrderBy` applies the filter and order-by to any `IQueryable<T>`.

## Key types

| Type | Description |
|------|-------------|
| **[`IUnitOfWork`](./IUnitOfWork.cs)** | Transactional unit-of-work contract: `TransactionAsync`, `ExecuteAsync`, `AreEventsSupported`, and `Events` (`IEventQueue`) for transactional outbox support. |
| **[`DataResult`](./DataResult.cs)** | Readonly record struct carrying `WasMutated` for valueless mutation outcomes (e.g. delete). |
| **[`DataResult<T>`](./DataResultT.cs)** | Generic variant of `DataResult` adding the mutated `Value`; returned by create/update operations. |
| [`IDataArgs`](./IDataArgs.cs) | Marker interface for strongly-typed data argument objects passed to `IUnitOfWork` overloads. |
| **[`QueryArgsConfig`](./Querying/QueryArgsConfig.cs)** | Root configuration builder for `$filter` and `$orderby` parsing; holds `FilterParser`, `OrderByParser`, and `ParsingConfig`; entry point via `QueryArgsConfig.Create()`. |
| **[`QueryFilterParser`](./Querying/QueryFilterParser.cs)** | Configures and executes `$filter` parsing: `AddField<T>`, `AddNullField`, `AddReferenceDataField<T>`, `WithDefault`, `OnQuery` hooks, and `Parse(filterString)`. |
| **[`QueryOrderByParser`](./Querying/QueryOrderByParser.cs)** | Configures and executes `$orderby` parsing: `AddField`, `WithDefault`, and `Parse(orderByString)`. |
| **[`QueryExtensions`](./Querying/QueryExtensions.cs)** | `IQueryable<T>` extension methods `Where` and `OrderBy` apply the filter and order-by (respectively). |
| **[`QueryArgsParseResult`](./Querying/QueryArgsParseResult.cs)** | Combined result of filter + order-by parsing: `FilterResult`, `OrderByResult`, `HasErrors`, and `QueryStatement`. |
| [`QueryFilterParserResult`](./Querying/QueryFilterParserResult.cs) | Result of `QueryFilterParser.Parse()`: LINQ `QueryStatement`, parsed field values, and any `ParseErrors`. |
| [`QueryOrderByParserResult`](./Querying/QueryOrderByParserResult.cs) | Result of `QueryOrderByParser.Parse()`: ordered `QueryStatement` and any parse errors. |
| _[`ModelBase<TId>`](./Models/ModelBase.cs)_ | Abstract persistence model base implementing `IIdentifier<T>`, `IChangeLogEx`, `IETag`. |
| _[`ReferenceDataModelBase`](./Models/ReferenceDataModelBase.cs)_ | Persistence model base for reference data tables with `Id`, `Code`, `Text`, `IsActive`, `SortOrder`, `StartDate`, `EndDate`. |

## Namespaces

| Namespace | Description |
|-----------|-------------|
| [**`Models`**](./Models/) | `ModelBase<TId>` and `ReferenceDataModelBase` persistence model base classes. |
| [**`Querying`**](./Querying/) | `QueryArgsConfig`, `QueryFilterParser`, `QueryOrderByParser`, field configuration types, expression AST types, and `QueryExtensions`. |

## Related Namespaces

- **[`CoreEx`](../CoreEx/README.md)** - `QueryArgs` (filter/orderby strings and paging), `PagingArgs`, and `IEventQueue` are defined in the root `CoreEx` package and consumed here.
- **[`CoreEx.Database`](../CoreEx.Database/README.md)** - `IUnitOfWork` is implemented by the database unit-of-work; `QueryArgsConfig` is used by database query builders.
- **[`CoreEx.EntityFrameworkCore`](../CoreEx.EntityFrameworkCore/README.md)** - EF Core `IQueryable<T>` extensions consume `QueryArgsConfig` via `Where`/`OrderBy`.