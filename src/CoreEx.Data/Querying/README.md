# CoreEx.Data.Querying

> Provides the safe, explicitly-configured OData-like `$filter` and `$orderby` LINQ query translation pipeline: `QueryArgsConfig`, `QueryFilterParser`, `QueryOrderByParser`, field configurations, expression AST types, and the `IQueryable<T>.Where` and `OrderBy` extensions.

## Overview

`CoreEx.Data.Querying` implements a constrained, secure dynamic query layer. Unlike full OData or GraphQL, it does not expose arbitrary model properties: every filterable and sortable field must be explicitly registered in a `QueryArgsConfig` instance. This design prevents accidental data leakage, index misuse, and injection-style attacks while still delivering flexible client-driven queries via the `$filter` and `$orderby` query-string arguments from `QueryArgs`.

The parse pipeline is: `QueryArgs.Filter` string → `QueryFilterParser.Parse()` → expression token AST → LINQ `QueryStatement` (a `System.Linq.Dynamic.Core`-compatible predicate string + parameter array). `QueryExtensions.Where` and `OrderBy` drives both parsers, validates errors, and applies the resulting predicates to the `IQueryable<T>`.

Field configurations (`QueryFilterFieldConfig<T>`, `QueryFilterParseableFieldConfig<T>`, `QueryFilterNullFieldConfig`, `QueryFilterReferenceDataFieldConfig<T>`, `QueryFilterEnumFieldConfig<T>`) map each registered field name to its underlying model property, allowed operators, value transformations, and custom statement generators.

## Key capabilities

- ⚙️ **Explicit field allow-listing**: Only fields registered via `QueryFilterParser.AddField<T>` or `QueryOrderByParser.AddField` are accepted; all others produce a structured parse error (`IQueryParseError`) rather than a runtime exception.
- 🔍 **Filter operator support**: String fields support `eq`, `ne`, `startswith`, `endswith`, `contains`, `in`; numeric/date fields support `eq`, `ne`, `lt`, `le`, `gt`, `ge`, `in`; null fields support `eq null` / `ne null`.
- 📐 **Order-by support**: `$orderby` accepts comma-separated `field asc|desc` pairs; each field's model name and direction are validated and translated to LINQ `.OrderBy()`/`.ThenBy()`.
- 🔠 **Value normalization**: `UseUpperCase()` / `UseLowerCase()` on string fields apply `ToUpper`/`ToLower` transforms to both the filter value and the model field in the LINQ predicate, enabling case-insensitive matching at the data layer.
- 🏷️ **Reference data fields**: `AddReferenceDataField<TRef>` resolves a `Code` string in the filter to the reference data `Id`, so callers filter by code while the query runs against the numeric/GUID identifier column.
- 🔢 **Enum fields**: `AddEnumField<TEnum>` parses enum name strings to their underlying numeric value for persistence queries.
- 🔲 **Null fields**: `AddNullField` maps an abstract nullable concept (e.g. `terminated`) to a model property null-check expression, hiding physical schema detail from the caller.
- 🛑 **Structured parse errors**: `IQueryParseError` carries the error message and field name; `QueryFilterParserException` / `QueryOrderByParserException` are thrown when parsing fails, translating cleanly to `400 Bad Request` in `WebApi`.
- 🔗 **Dynamic LINQ integration**: Generated `QueryStatement` objects are applied via `System.Linq.Dynamic.Core` `.Where(predicate, args)` and `.OrderBy(statement)`, keeping the translation generic across EF Core, Cosmos, and in-memory queryables, etc.

## Key types

| Type | Description |
|------|-------------|
| **[`QueryArgsConfig`](./QueryArgsConfig.cs)** | Root builder: `Create()`, `WithFilter(Action<QueryFilterParser>)`, `WithOrderBy(Action<QueryOrderByParser>)`; holds `FilterParser`, `OrderByParser`, and `ParsingConfig`. |
| **[`QueryFilterParser`](./QueryFilterParser.cs)** | Registers filterable fields and parses `$filter` strings: `AddField<T>`, `AddNullField`, `AddReferenceDataField<T>`, `AddEnumField<T>`, `WithDefault`, `OnQuery`, `Parse(string)`. |
| **[`QueryOrderByParser`](./QueryOrderByParser.cs)** | Registers sortable fields and parses `$orderby` strings: `AddField`, `WithDefault`, `Parse(string)`. |
| **[`QueryExtensions`](./QueryExtensions.cs)** | `IQueryable<T>.Where` and `OrderBy` — drives both parsers, applies filter and order-by to the queryable, and throws on parse errors. |
| **[`QueryArgsParseResult`](./QueryArgsParseResult.cs)** | Combined parse output: `FilterResult`, `OrderByResult`, `HasErrors` and resulting `Error` (where applicable). |
| **[`QueryStatement`](./QueryStatement.cs)** | Carries a Dynamic LINQ predicate string and its parameter array, applied to `IQueryable<T>` via `.Where(statement.Predicate, statement.Args)`. |
| [`QueryFilterParserResult`](./QueryFilterParserResult.cs) | `$filter` parse output: `QueryStatement`, per-field parsed values dictionary, and `ParseErrors`. |
| [`QueryOrderByParserResult`](./QueryOrderByParserResult.cs) | `$orderby` parse output: `QueryStatement` and `ParseErrors`. |
| _[`QueryFilterFieldConfigBase<T>`](./QueryFilterFieldConfigBaseT.cs)_ | Abstract base for all filter field configurations: model name, model prefix, default value, allowed operators, `ResultWriter`. |
| [`QueryFilterParseableFieldConfig<T>`](./QueryFilterParseableFieldConfigT.cs) | Concrete string/numeric/date field config: `Operators(...)`, `UseUpperCase()`, `UseLowerCase()`, `Default(value)`. |
| [`QueryFilterNullFieldConfig`](./QueryFilterNullFieldConfig.cs) | Null-concept field config: maps `eq null` / `ne null` to a model property null-check expression. |
| [`QueryFilterReferenceDataFieldConfig<TRef>`](./QueryFilterReferenceDataFieldConfigT.cs) | Reference data field config: resolves `Code` → `Id` before emitting the LINQ predicate. |
| [`QueryFilterEnumFieldConfig<TEnum>`](./QueryFilterEnumFieldConfigT.cs) | Enum field config: parses enum name strings to their underlying numeric value. |
| [`QueryOrderByFieldConfig`](./QueryOrderByFieldConfig.cs) | Sortable field config: model property name, allowed directions, default direction. |
| [`QueryFilterOperator`](./QueryFilterOperator.cs) | Enum of supported filter operators: `Equal`, `NotEqual`, `LessThan`, `LessOrEqual`, `GreaterThan`, `GreaterOrEqual`, `StartsWith`, `EndsWith`, `Contains`, `In`. |
| [`IQueryFilterFieldConfig`](./IQueryFilterFieldConfig.cs) | Interface implemented by all filter field configurations; consumed by `QueryFilterParser` for field lookup. |
| [`IQueryParseError`](./IQueryParseError.cs) | Parse error contract: `Message` and `Field` name. |

## Related Namespaces

- **[`CoreEx.Data`](../README.md)** - Root package; `QueryArgs` (filter/orderby strings) and `PagingArgs` are defined there and passed into the `IQueryable<T>.Where` and `OrderBy` extensions.
- **[`CoreEx.RefData`](../../CoreEx/RefData/README.md)** - `QueryFilterReferenceDataFieldConfig<TRef>` calls `ReferenceDataOrchestrator` to resolve codes to IDs during filter parsing.
- **[`CoreEx.Database`](../../CoreEx.Database/README.md)** - ADO.NET query builders call `Where`/`OrderBy` to apply filter/orderby to SQL-backed `IQueryable<T>`.
- **[`CoreEx.EntityFrameworkCore`](../../CoreEx.EntityFrameworkCore/README.md)** - EF Core query extensions consume `QueryArgsConfig` identically via the same `Where`/`OrderBy` extension methods.