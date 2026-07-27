# CoreEx.Data.GraphQL

> A transport-agnostic GraphQL-lite bridge over the `CoreEx.Data` OData-esque dynamic `$filter`/`$orderby`/paging query capability and `JsonFilter` field include projection.

## Overview

`CoreEx.Data.GraphQL` lets a domain expose its existing `QueryAsync`/`GetAsync` repository or service methods
through a single GraphQL-lite `/query` endpoint, without hand-authoring a GraphQL schema, resolvers, or a
new execution engine. It parses the standard GraphQL-over-HTTP request envelope (`query`, `operationName`,
`variables`) with [`GraphQL-Parser`](https://www.nuget.org/packages/GraphQL-Parser) (AST only — no
execution engine), translates the GraphQL-native `where`/`orderBy` structured arguments 1:1 onto the same
OData-esque `filter`/`orderby` strings consumed by an entity's existing `QueryArgsConfig`, exposes list
query roots as [Relay Cursor Connections](https://relay.dev/graphql/connections.htm), and flattens the
requested selection set into JSON include paths consumed by `CoreEx.Json.JsonFilter` — the exact same
projection mechanism `CoreEx.AspNetCore`'s `WebApi` already uses for `$fields`/`$exclude`.

The engine is deliberately **transport-agnostic**: it references only `CoreEx.Data` (→ `CoreEx.Events` →
`CoreEx`), has zero dependency on ASP.NET Core, and is consumed via the `IGraphQLEngine` contract
(`CoreEx.Data.GraphQL` namespace, `CoreEx` project) so that hosting bridges — such as a minimal API endpoint in
`CoreEx.AspNetCore` — never need to reference this package's implementation types directly.

## Key capabilities

- 🧩 **Query-only GraphQL-lite bridge**: parses a GraphQL document, resolves top-level root fields against
  explicitly registered query/item roots — no mutations, subscriptions, cross-repository nested resolvers
  (dataloaders), interfaces, unions, or directives in v1. Fragments and inline fragments are rejected with an
  explicit `FRAGMENTS_NOT_SUPPORTED` error rather than being silently ignored.
- 🏷️ **`__typename` support**: the standard `__typename` meta-field is answerable at every selection depth
  (Connection, Edge, node, and any nested object), since mainstream GraphQL clients (Apollo Client, Relay,
  urql) auto-inject it into every selection set for cache normalization.
- 🔤 **Field aliases at every depth**: `field: realName` aliasing is honored throughout the selection set, not
  just at the root — the response is reshaped (via `GraphQLResponseShaper`) to match the client's requested
  keys.
- 🎯 **Native GraphQL `where`/`orderBy`, exact `QueryArgsConfig` compatibility**: list query roots accept a
  GraphQL-idiomatic, field-keyed `where` input (operator objects or bare-scalar equality shorthand, composed
  via `and`/`or`/`not`) and an `orderBy` list of field/direction objects — mirroring mainstream conventions
  (Hot Chocolate, Prisma). These are pure syntax translations (`GraphQLFilterTranslator`/
  `GraphQLOrderByTranslator`) onto the OData-esque `filter`/`orderby` strings; the translated string is
  always parsed/validated by the entity's own, unmodified `QueryArgsConfig` (`QueryFilterParser`/
  `QueryOrderByParser`) — so whatever operators and fields a `QueryArgsConfig` already exposes for the REST
  `$filter`/`$orderby` query strings are supported **exactly**, with no separate allow-list to maintain.
- 🔗 **Relay Cursor Connections paging**: list query roots return the spec-shaped `edges { node cursor }
  pageInfo { hasNextPage hasPreviousPage startCursor endCursor } totalCount` response via `first`/`after`
  forward pagination (backward pagination — `last`/`before` — is out of scope for v1 and rejected with an
  explicit error). `totalCount` is only computed when the client's selection actually requests it.
- 🪆 **Nested DTO shape support**: a `node`'s selection set may traverse arbitrarily deep into a DTO's own
  object graph (e.g. `node { address { street city } }`) since projection is performed over one
  already-materialized result via `JsonFilter`, not via per-field resolvers.
- 🧾 **GraphQL-shaped errors**: `GraphQLArgumentTranslationException`, `QueryFilterParserException`,
  `QueryOrderByParserException`, `ValidationException`, `NotFoundException`, and unknown-field errors are
  mapped to `{ message, path, extensions.code }` error objects.
- 🔍 **Spec-compliant introspection**: `__schema`/`__type(name:)` (plus `__typename`) implement the official
  GraphQL introspection schema, built once from the registered roots (see `Internal.GraphQLIntrospectionSchemaBuilder`)
  and exposed identically via `IGraphQLEngine.GetSchemaAsync()`. Each query root's `where`/`orderBy` arguments
  are described as fully-typed `<Item>WhereInput`/`<Item>OrderByInput` `INPUT_OBJECT` graphs (`and`/`or`/`not`
  composition, `eq`/`ne`/`gt`/`ge`/`lt`/`le`/`in`/`startsWith`/`endsWith`/`contains` operator inputs, and a shared
  `SortDirection` enum), derived directly from the root's existing `QueryArgsConfig.ToJsonSchema()` field
  descriptions — no extra configuration needed; see Non-goals below for the remaining simplifications.
- 🧷 **Explicit, code-based registration**: `services.AddCoreExGraphQLLite((o, sp) => o.AddQuery(...).AddGet(...))`
  — no attribute-based auto-discovery.

## Key types

| Type | Description |
|------|-------------|
| **[`GraphQLEngine`](./GraphQLEngine.cs)** | The concrete `IGraphQLEngine` implementation: parses the document, resolves root fields, applies `JsonFilter` projection, and assembles the `GraphQLEngineResult` (including the Relay Connection shape for query roots). |
| **[`GraphQLLiteOptions`](./GraphQLLiteOptions.cs)** | The DI options builder: `AddQuery<TItem>` (list roots bound to a `QueryArgsConfig` + `QueryAsync`-shaped delegate) and `AddGet<TItem>` (single-item roots). |
| **[`GraphQLServiceCollectionExtensions`](./GraphQLServiceCollectionExtensions.cs)** | `AddCoreExGraphQLLite(IServiceCollection, Action<GraphQLLiteOptions, IServiceProvider>)` registration extension. |
| **[`GraphQLQueryRoot`](./GraphQLQueryRoot.cs)** / **[`GraphQLItemRoot`](./GraphQLItemRoot.cs)** | Registered list-query and single-item root field descriptors. |
| **`Internal.GraphQLFilterTranslator`** / **`Internal.GraphQLOrderByTranslator`** | Translate the GraphQL-native `where`/`orderBy` structured arguments to the OData-esque `filter`/`orderby` strings consumed by `QueryArgsConfig`. |
| **`Internal.GraphQLCursor`** | Encodes/decodes the opaque, offset-based Relay Cursor Connections cursor. |
| **`IGraphQLEngine`** (in `CoreEx`, namespace `CoreEx.Data.GraphQL`) | The transport-agnostic contract: `ExecuteAsync(document, operationName, variables, ct)` and `GetSchemaAsync(ct)`. |
| **`GraphQLEngineResult` / `GraphQLEngineError`** (in `CoreEx`, namespace `CoreEx.Data.GraphQL`) | The plain result/error POCOs returned by `ExecuteAsync`, mirroring the GraphQL-over-HTTP response shape. |

## Usage

```csharp
// Program.cs (or a domain composition extension)
builder.Services.AddCoreExGraphQLLite((o, sp) =>
{
    o.AddQuery<ProductLite>("products", ProductQueryArgsConfig.Default, async (qa, pa, ct) => await CoreEx.ExecutionContext.GetRequiredService<IProductReadService>().QueryAsync(qa, pa, ct).ConfigureAwait(false))
     .AddGet<Product>("product", (args, ct) =>
     {
         // Validate explicitly rather than an indexer + null-forgiving lookup, so a missing/empty 'id' throws an ArgumentException - which the engine maps to an
         // ARGUMENT_ERROR GraphQL error - instead of an unhandled KeyNotFoundException/NullReferenceException surfacing as an opaque EXECUTION_ERROR.
         if (!args.TryGetValue("id", out var id) || id is not string { Length: > 0 } idValue)
             throw new ArgumentException("'id' argument is required and must be a non-empty string.", nameof(args));

         return CoreEx.ExecutionContext.GetRequiredService<IProductReadService>().GetAsync(idValue, ct);
     });
});

// ...
app.MapCoreExGraphQLLite("/api/query"); // Additive GraphQL-lite bridge alongside the existing REST endpoints.
```

A hosting bridge (e.g. `MapCoreExGraphQLLite` in `CoreEx.AspNetCore`) resolves `IGraphQLEngine` from DI and
calls `ExecuteAsync` with the parsed request envelope, returning `{ data, errors }` as the HTTP response
body. Since `IGraphQLEngine` is registered as a singleton, root resolvers that need scoped dependencies
(e.g. a repository or application service) should resolve them per-invocation rather than capturing an
instance resolved from the root `IServiceProvider` at registration time — as shown above via
`CoreEx.ExecutionContext.GetRequiredService<T>()`, which reads from the ambient `ExecutionContext`'s scoped
service provider (set by the `UseExecutionContext()` middleware every CoreEx host already registers), so no
extra `IHttpContextAccessor` wiring is required.

A client queries the `products` root using native GraphQL `where`/`orderBy` and `first`/`after` Relay paging
— translated 1:1 to `ProductQueryArgsConfig`'s existing `filter`/`orderby` support:

```graphql
{
  products(where: { sku: { startsWith: "spec" } }, orderBy: [{ text: DESC }], first: 10) {
    edges {
      node { sku text }
      cursor
    }
    pageInfo { hasNextPage endCursor }
    totalCount
  }
}
```

## Non-goals (v1)

- No mutations or subscriptions — read/query only.
- No cross-repository nested resolvers (dataloaders/N+1 batching) — a `node`'s selection set may traverse
  nested properties already present on the DTO returned by a single `QueryAsync`/`GetAsync` call, but cannot
  request a field that would require invoking a different registered root.
- No fragments (spreads or inline), interfaces, unions, or directives — a fragment in the document produces
  an explicit `FRAGMENTS_NOT_SUPPORTED` error rather than being silently skipped.
- No backward pagination (`last`/`before`) — Relay Cursor Connections `first`/`after` forward pagination
  only; a `last`/`before` argument produces an explicit error rather than being silently ignored.
- No standard GraphQL SDL export — the schema is only queryable at runtime via `__schema`/`__type`
  (or the equivalent `IGraphQLEngine.GetSchemaAsync()`), not printable as a `.graphql` SDL document.
- `where`/`orderBy` argument field names in the generated `<Item>WhereInput`/`<Item>OrderByInput` types are the
  all-lowercase names already reported by `QueryArgsConfig.ToJsonSchema()` (e.g. `subcategory` rather than
  `subCategory`), not the DTO's camelCase JSON naming — cosmetic only, since field matching is case-insensitive.
- Every field of a given JSON-schema type (`string`/`integer`/`number`/`boolean`) shares one generic
  `<Type>FilterInput` operator set (e.g. `StringFilterInput`) rather than a per-field-restricted shape, so a
  field may advertise an operator (e.g. `gt`) its specific configuration does not actually permit —
  `QueryFilterParser` still enforces the real legality at execution time (defense in depth).
- CLR `enum` and reference-data (`IReferenceData`) *output* properties are described as the `String` scalar
  (matching their actual JSON wire representation), not a spec `ENUM` type.
- A single-item `AddGet` root only advertises an `id: ID!` argument where its registered item type implements
  `IReadOnlyIdentifier<TId>`, since the `AddGet` registration API does not declare an argument shape today; it
  always advertises `includeText`/`includeInactive` alongside it, since the engine honours both for item roots
  too (see below).
- Not a replacement for the REST `$filter`/`$orderby`/`$fields` query-string endpoints — this is an
  additive bridge sharing the same underlying pipeline.
- **Introspection is disabled by default** (`GraphQLLiteOptions.EnableIntrospection = false`) — a request for
  `__schema`/`__type` produces an `INTROSPECTION_DISABLED` error until explicitly enabled (e.g. so client
  tooling like GraphiQL, Postman, or Apollo/Relay codegen can introspect the schema in development). The
  direct `IGraphQLEngine.GetSchemaAsync()` API is unaffected by this toggle.
- **No query-cost/complexity budget beyond `MaxRootFields`** — `GraphQLLiteOptions.MaxRootFields` (default
  `null`, unlimited) only bounds the number of root fields (including aliased repeats) in one document; there
  is no per-request node/complexity scoring, and nested selection depth is bounded only by the underlying
  `GraphQL-Parser` library's own `MaxDepth` default.
- **Introspection's advertised nesting depth is an approximation of the runtime cap** — `__type` field
  resolution mirrors `GraphQLTypeShape`'s runtime depth cap (`MaxDepth = 8`) along each type's *first*
  traversal path, but the introspection type registry is keyed by CLR type name and short-circuits on repeat
  visits (cycle guard), so a type reachable both shallow and deep keeps whichever depth it was first visited
  at — it does not perform a true per-path re-evaluation for every possible route to that type.
- **No authorization is applied by default** — `MapCoreExGraphQLLite` mounts the endpoint anonymously unless
  the caller supplies its own `configure` delegate (e.g. `rb => rb.RequireAuthorization()`); since this
  endpoint can reach the same underlying data as `[Authorize]`-protected REST controllers, hosts should
  apply equivalent authorization explicitly.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.
