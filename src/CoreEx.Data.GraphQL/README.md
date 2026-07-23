# CoreEx.Data.GraphQL

> A transport-agnostic GraphQL-lite bridge over the `CoreEx.Data` OData-esque dynamic `$filter`/`$orderby`/paging query capability and `JsonFilter` field include projection.

## Overview

`CoreEx.Data.GraphQL` lets a domain expose its existing `QueryAsync`/`GetAsync` repository or service methods
through a single GraphQL-lite `/query` endpoint, without hand-authoring a GraphQL schema, resolvers, or a
new execution engine. It parses the standard GraphQL-over-HTTP request envelope (`query`, `operationName`,
`variables`) with [`GraphQL-Parser`](https://www.nuget.org/packages/GraphQL-Parser) (AST only — no
execution engine), maps root-field arguments onto `QueryArgs`/`PagingArgs` using the same convention as the
REST `$filter`/`$orderby`/`$skip`/`$take`/`$count` query strings, and flattens the requested selection set
into JSON include paths consumed by `CoreEx.Json.JsonFilter` — the exact same projection mechanism
`CoreEx.AspNetCore`'s `WebApi` already uses for `$fields`/`$exclude`.

The engine is deliberately **transport-agnostic**: it references only `CoreEx.Data` (→ `CoreEx.Events` →
`CoreEx`), has zero dependency on ASP.NET Core, and is consumed via the `IGraphQLEngine` contract
(`CoreEx.Data` namespace, `CoreEx` project) so that hosting bridges — such as a minimal API endpoint in
`CoreEx.AspNetCore` — never need to reference this package's implementation types directly.

## Key capabilities

- 🧩 **Query-only GraphQL-lite bridge**: parses a GraphQL document, resolves top-level root fields against
  explicitly registered query/item roots — no mutations, subscriptions, fragments, or cross-repository
  nested resolvers (dataloaders) in v1.
- 🔁 **Reuses existing `QueryArgsConfig`**: each registered root points at an entity's existing
  `QueryArgsConfig<TSelf>` (e.g. `ProductQueryArgsConfig.Default`) for `filter`/`orderby` validation — no
  duplicate field allow-listing.
- 🪆 **Nested DTO shape support**: selection sets may traverse arbitrarily deep into a DTO's own object graph
  (e.g. `person { address { street city } }`) since projection is performed over one already-materialized
  result via `JsonFilter`, not via per-field resolvers.
- 📐 **Fixed argument convention**: `filter`, `orderby`, `skip`, `take`, `count`, `includeText`,
  `includeInactive` GraphQL arguments map 1:1 onto `QueryArgs`/`PagingArgs`.
- 🧾 **GraphQL-shaped errors**: `QueryFilterParserException`, `QueryOrderByParserException`,
  `ValidationException`, `NotFoundException`, and unknown-field errors are mapped to `{ message, path,
  extensions.code }` error objects.
- 🔍 **Schema/discovery document**: composes `QueryArgsConfig.ToJsonSchema()` with a reflection-derived
  shape of each root's selectable output fields, exposed via `IGraphQLEngine.GetSchemaAsync()` or a
  reserved `__schema` root field.
- 🧷 **Explicit, code-based registration**: `services.AddCoreExGraphQLLite(o => o.AddQuery(...).AddGet(...))`
  — no attribute-based auto-discovery.

## Key types

| Type | Description |
|------|-------------|
| **[`GraphQLEngine`](./GraphQLEngine.cs)** | The concrete `IGraphQLEngine` implementation: parses the document, resolves root fields, applies `JsonFilter` projection, and assembles the `GraphQLEngineResult`. |
| **[`GraphQLLiteOptions`](./GraphQLLiteOptions.cs)** | The DI options builder: `AddQuery<TItem>` (list roots bound to a `QueryArgsConfig` + `QueryAsync`-shaped delegate) and `AddGet<TItem>` (single-item roots). |
| **[`GraphQLServiceCollectionExtensions`](./GraphQLServiceCollectionExtensions.cs)** | `AddCoreExGraphQLLite(IServiceCollection, Action<GraphQLLiteOptions>)` registration extension. |
| **[`GraphQLQueryRoot`](./GraphQLQueryRoot.cs)** / **[`GraphQLItemRoot`](./GraphQLItemRoot.cs)** | Registered list-query and single-item root field descriptors. |
| **`IGraphQLEngine`** (in `CoreEx`, namespace `CoreEx.Data`) | The transport-agnostic contract: `ExecuteAsync(document, operationName, variables, ct)` and `GetSchemaAsync(ct)`. |
| **`GraphQLEngineResult` / `GraphQLEngineError`** (in `CoreEx`, namespace `CoreEx.Data`) | The plain result/error POCOs returned by `ExecuteAsync`, mirroring the GraphQL-over-HTTP response shape. |

## Usage

```csharp
// Program.cs (or a domain composition extension)
builder.Services.AddHttpContextAccessor(); // Needed so root resolvers can obtain the current request's scoped services.
builder.Services.AddCoreExGraphQLLite((o, sp) =>
{
    var accessor = sp.GetRequiredService<IHttpContextAccessor>();
    IProductReadService Service() => accessor.HttpContext!.RequestServices.GetRequiredService<IProductReadService>();

    o.AddQuery<ProductLite>("products", ProductQueryArgsConfig.Default, async (qa, pa, ct) => await Service().QueryAsync(qa, pa, ct).ConfigureAwait(false))
     .AddGet<Product>("product", (args, ct) => Service().GetAsync(args["id"]!.ToString()!, ct));
});
```

A hosting bridge (e.g. `MapCoreExGraphQLLite` in `CoreEx.AspNetCore`) resolves `IGraphQLEngine` from DI and
calls `ExecuteAsync` with the parsed request envelope, returning `{ data, errors }` as the HTTP response
body. Since `IGraphQLEngine` is registered as a singleton, root resolvers that need scoped dependencies
(e.g. a repository or application service) should resolve them per-invocation from the current request's
scope — as shown above via `IHttpContextAccessor` — rather than capturing an instance resolved from the
root `IServiceProvider` at registration time.

## Non-goals (v1)

- No mutations or subscriptions — read/query only.
- No cross-repository nested resolvers (dataloaders/N+1 batching) — selection sets may traverse nested
  properties already present on the DTO returned by a single `QueryAsync`/`GetAsync` call, but cannot
  request a field that would require invoking a different registered root.
- No fragments, interfaces, unions, or directives.
- Not a replacement for the REST `$filter`/`$orderby`/`$fields` query-string endpoints — this is an
  additive bridge sharing the same underlying pipeline.
