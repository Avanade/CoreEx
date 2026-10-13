# CoreEx.Data.Querying

> Provides the safe, explicitly-configured OData-like `$filter`, `$orderby` and `$search` LINQ query translation pipeline: `QueryArgsConfig`, `QueryFilterParser`, `QueryOrderByParser`, `QuerySearchParser`, field configurations, expression AST types, and the `IQueryable<T>.Where` and `OrderBy` extensions.

## Overview

`CoreEx.Data.Querying` implements a constrained dynamic query layer. It does not expose arbitrary model properties: every filterable and sortable field must be explicitly registered in a `QueryArgsConfig` instance. This allows the data owner to control the query surface while delivering flexible client-driven queries via the `$filter`, `$orderby` and `$search` query-string arguments from `QueryArgs`. Allow-listing and parameterized predicates are guardrails, not substitutes for authorization, appropriate indexes or query-cost controls.

The filter pipeline is: `QueryArgs.Filter` string → `QueryFilterParser.Parse()` → expression token AST → LINQ `QueryStatement` (a `System.Linq.Dynamic.Core`-compatible predicate string + parameter array). `QueryArgsConfig.Parse()` combines filter, search and order-by validation into a `QueryArgsParseResult`. The `QueryExtensions.Where` and `OrderBy` extensions apply its filter, keyword search and ordering to the `IQueryable<T>`; ranked search is executed by the repository.

Field configurations (`QueryFilterFieldConfig<T>`, `QueryFilterParseableFieldConfig<T>`, `QueryFilterNullFieldConfig`, `QueryFilterReferenceDataFieldConfig<T>`, `QueryFilterEnumFieldConfig<T>`) map each registered field name to its underlying model property, allowed operators, value transformations, and custom statement generators.

## Key capabilities

- ⚙️ **Explicit field allow-listing**: Only fields registered via `QueryFilterParser.AddField<T>` or `QueryOrderByParser.AddField` are accepted; all others produce a structured parse error (`IQueryParseError`) rather than a runtime exception.
- 🔍 **Filter operator support**: String fields support `eq`, `ne`, `startswith`, `endswith`, `contains`, `in`; numeric/date fields support `eq`, `ne`, `lt`, `le`, `gt`, `ge`, `in`; null fields support `eq null` / `ne null`.
- 📐 **Order-by support**: `$orderby` accepts comma-separated `field asc|desc` pairs; each field's model name and direction are validated and translated to LINQ `.OrderBy()`/`.ThenBy()`.
- 🔠 **Value normalization**: `AsUpperCase()` / `AsLowerCase()` on string fields apply `ToUpper`/`ToLower` transforms to both the filter value and the model field in the LINQ predicate, enabling case-insensitive matching at the data layer.
- 🏷️ **Reference data fields**: `AddReferenceDataField<TRef>` resolves a `Code` string in the filter to the reference data `Id`, so callers filter by code while the query runs against the numeric/GUID identifier column.
- 🔢 **Enum fields**: `AddEnumField<TEnum>` parses enum name strings to their underlying numeric value for persistence queries.
- 🔲 **Null fields**: `AddNullField` maps an abstract nullable concept (e.g. `terminated`) to a model property null-check expression, hiding physical schema detail from the caller.
- 🛑 **Structured parse errors**: `IQueryParseError` carries the error message and field name; `QueryFilterParserException` / `QueryOrderByParserException` are thrown when parsing fails, translating cleanly to `400 Bad Request` in `WebApi`.
- 🔗 **Dynamic LINQ integration**: Generated `QueryStatement` objects are applied via `System.Linq.Dynamic.Core` `.Where(predicate, args)` and `.OrderBy(statement)`, keeping the translation generic across EF Core, Cosmos, and in-memory queryables, etc.
- 🔎 **Free-text search (`$search`)**: `WithSearch(Action<QuerySearchParser>)` enables the OData-style `$search` argument (`QueryArgs.Search`). The text is opaque (no grammar); it is normalized and validated (`MaxLength`, `MaxTerms`). See [Free-text search](#free-text-search-search).

## Structured filtering (`$filter`)

`$filter` answers **"which records satisfy these conditions?"** It is an explicit boolean expression, not a search phrase. For example (shown without URL encoding):

```text
GET /api/products?$filter=startswith(sku, 'SPEC') and contains(text, 'Comp')
```

Enable filtering with `WithFilter` and register each public field, its type and its permitted operators:

```csharp
var config = QueryArgsConfig.Create()
    .WithFilter(filter => filter
        .AddField<string>("sku", nameof(Product.Sku), c => c
            .WithOperators(QueryFilterOperator.EqualityOperators | QueryFilterOperator.StartsWith)
            .AsUpperCase())
        .AddField<string>("text", nameof(Product.Text), c => c
            .WithOperators(QueryFilterOperator.StringFunctions)
            .AsUpperCase()));
```

Public names can differ from model properties; `WithDefaultModelPrefix` supports queries over nested projections. The registered type determines how constants are parsed, and `WithOperators` restricts what callers can do with that field. Expressions support `and`, `or` and parentheses, comparisons such as `eq`/`ne`, and configured string functions such as `startswith`, `contains` and `endswith`. String literals use single quotes; an embedded quote is escaped by doubling it (`'O''Brien'`). Reference-data and enum field configurations translate public values into their persistence representation.

The parser validates the expression and emits a Dynamic LINQ predicate with separate parameter values; client text is not executed as arbitrary LINQ. Unknown fields, unsupported operators and invalid values produce `QueryFilterParserException`, surfaced as `400 Bad Request` by `WebApi`. A filter supplied without a configured filter parser is also rejected. `ToJsonSchema()` describes the allowed fields and operators.

Filtering and search are complementary: when both are supplied, records must satisfy the filter **and** the search. The repository remains responsible for applying mandatory authorization and tenant restrictions independently of optional client criteria.

## Ordering (`$orderby`)

`$orderby` answers **"in what sequence should matching records be returned?"** It accepts comma-separated fields with an optional `asc` or `desc` direction; omitting the direction means ascending:

```text
GET /api/products?$filter=startswith(sku, 'SPEC')&$orderby=text desc, sku
```

Enable ordering with `WithOrderBy` and register the sortable fields:

```csharp
var config = QueryArgsConfig.Create()
    .WithOrderBy(orderby => orderby
        .AddField("sku", nameof(Product.Sku), c => c.WithDefault().WithAlwaysInclude())
        .AddField("text", nameof(Product.Text)));
```

`WithDefault` defines the ordering when the client supplies none; multiple default fields are applied in registration order. `WithAlwaysInclude` appends a field when it is missing from the chosen ordering. Using a unique field as the final tie-breaker gives deterministic ordering for paging (but does not prevent page drift when records change between requests). `WithDirection` can restrict a field to ascending or descending ordering, and model names/prefixes can be mapped independently of public field names.

Unknown fields and disallowed directions produce `QueryOrderByParserException` (`400` in `WebApi`); an explicit order-by without a configured parser is also rejected. `ToJsonSchema()` describes the allowed fields, directions and default ordering.

For non-ranked queries, parse once and apply the validated criteria before materializing or paging:

```csharp
var parsed = config.Parse(queryArgs).ThrowOnError();
var ordered = products.Where(parsed).OrderBy(parsed);
```

Keyword search can use client/default ordering. Ranked search instead owns relevance ordering: an explicit `$orderby` is rejected and configured default/always-included ordering is not applied.

## Free-text search (`$search`)

`$search` is deliberately separate from `$filter`. `$filter` is a precise boolean predicate; `$search` is a *"find me things like this"* request whose execution strategy belongs to the data owner. Clients always send **text**, never vectors; embedding the text (where applicable) is a server-side Infrastructure concern.

The `QuerySearchMode` determines who executes the search:

| Mode | Executed by | Ordering |
|------|-------------|----------|
| `Keyword` (default) | The framework: `QueryExtensions.Where` applies a provider-agnostic Dynamic LINQ predicate where **every term** must match **at least one** configured keyword field (`AddKeywordField`). | `$orderby` (or default) as per usual. |
| `FullText`, `Semantic`, `Hybrid` | The repository: ranked modes are *never* applied by `Where`; the repository checks `QueryArgsParseResult.IsRankedSearch` and uses `SearchResult.Text` (e.g. PostgreSQL `tsvector`, pgvector `<=>`, Cosmos `VectorDistance`, reciprocal rank fusion). | Relevance; an explicit `$orderby` is rejected, and the default order-by is not applied. |

```csharp
public class ProductQueryArgsConfig : QueryArgsConfig<ProductQueryArgsConfig>
{
    public ProductQueryArgsConfig()
    {
        WithFilter(...);
        WithOrderBy(...);
        WithSearch(search => search
            .WithDefaultModelPrefix("Product")
            .AddKeywordField(nameof(Product.Text), c => c.AsUpperCase())
            .AddKeywordField(nameof(Product.Sku), c => c.WithOperator(QueryFilterOperator.StartsWith).AsUpperCase()));
    }
}

// Repository: keyword search is applied alongside the filter.
var parsed = ProductQueryArgsConfig.Default.Parse(query).ThrowOnError();
var q = products.Where(parsed);
q = parsed.IsRankedSearch ? /* rank by relevance using parsed.SearchResult!.Text */ q : q.OrderBy(parsed);
```

A `$search` against a `QueryArgsConfig` without a configured search parser is rejected (`400`) with `QuerySearchParserException`. The `ToJsonSchema` output includes a `search` entry (`mode`, `ranked`, `maxLength`, `maxTerms`) without exposing model names. Clients send it via `HttpRequestBuilder.WithSearch`/`HttpRequestMessage.WithSearch` (or `QueryArgs.Search` with `WithQuery(QueryArgs)`); API endpoints advertise it with `[Query(SupportsSearch = true)]`; GraphQL-lite list roots expose a `search` argument.

The defaults of **200 characters** (`WithMaxLength`) and **8 distinct terms** (`WithMaxTerms`) are configurable CoreEx policy choices, not OData or search-industry standards. Length is measured after whitespace normalization using .NET `string.Length` (UTF-16 code units), in every mode. The term limit applies only to keyword mode: with `T` terms and `F` fields, the generated predicate contains `T × F` field comparisons. These defaults suit short catalog queries; longer natural-language requests may warrant a larger length limit for semantic/hybrid search, chosen against the repository's actual costs and usage.

`SearchResult.Text` preserves word order, repetitions and casing, collapsing whitespace only. `SearchResult.Terms` is separately deduplicated case-insensitively so repeated keyword terms do not generate redundant predicates or consume the term allowance. Deduplication deliberately happens **after** constructing and length-checking `Text`: removing repeated words from a natural-language query can change its meaning or ranking, and repetition must not bypass the length limit.

## Key types

| Type | Description |
|------|-------------|
| **[`QueryArgsConfig`](./QueryArgsConfig.cs)** | Root builder: `Create()`, `WithFilter(Action<QueryFilterParser>)`, `WithOrderBy(Action<QueryOrderByParser>)`, `WithSearch(Action<QuerySearchParser>)`; holds `FilterParser`, `OrderByParser`, `SearchParser`, and `ParsingConfig`. |
| **[`QuerySearchParser`](./QuerySearchParser.cs)** | Configures and parses `$search`: `WithMode`, `WithMaxLength`, `WithMaxTerms`, `AddKeywordField`, `WithValidator`, `Parse(string)`. |
| [`QuerySearchParserResult`](./QuerySearchParserResult.cs) | `$search` parse output: `Mode`, `IsRanked`, normalized `Text`, distinct `Terms`, `ToLinqString` (keyword only), and `Error`. |
| [`QuerySearchFieldConfig`](./QuerySearchFieldConfig.cs) | Keyword field config: model name/prefix, `WithOperator` (`Contains`, `StartsWith`, `EndsWith`, `Equal`), `AsUpperCase`/`AsLowerCase`, `WithNoNullCheck`. |
| [`QuerySearchMode`](./QuerySearchMode.cs) | `Keyword` (framework-executed), `FullText`, `Semantic`, `Hybrid` (repository-executed, relevance-ranked). |
| **[`QueryFilterParser`](./QueryFilterParser.cs)** | Registers filterable fields and parses `$filter` strings: `AddField<T>`, `AddNullField`, `AddReferenceDataField<T>`, `AddEnumField<T>`, `WithDefault`, `OnQuery`, `Parse(string)`. |
| **[`QueryOrderByParser`](./QueryOrderByParser.cs)** | Registers sortable fields and parses `$orderby` strings: `AddField`, `WithDefault`, `Parse(string)`. |
| **[`QueryExtensions`](./QueryExtensions.cs)** | `IQueryable<T>.Where` and `OrderBy` — drives both parsers, applies filter and order-by to the queryable, and throws on parse errors. |
| **[`QueryArgsParseResult`](./QueryArgsParseResult.cs)** | Combined parse output: `FilterResult`, `OrderByResult`, `SearchResult`, `IsRankedSearch`, `HasErrors` and resulting `Error` (where applicable). |
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
