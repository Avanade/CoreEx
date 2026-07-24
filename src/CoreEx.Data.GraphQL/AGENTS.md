# CoreEx.Data.GraphQL — AI Usage Guide

Provides a transport-agnostic GraphQL-lite bridge that translates native GraphQL `where`/`orderBy`/Relay Cursor Connections paging 1:1 onto an entity's existing `QueryArgsConfig`-driven `QueryAsync`/`GetAsync` pipeline — no hand-authored schema, resolvers, or execution engine.

## Registration

Register roots explicitly — no attribute-based auto-discovery. Each `AddQuery`/`AddGet` binds a GraphQL root field name to an entity's *existing* `QueryArgsConfig.Default` and application-service method.

```csharp
// Program.cs (or a domain composition extension)
builder.Services.AddCoreExGraphQLLite((o, sp) =>
{
    o.AddQuery<ProductLite>("products", ProductQueryArgsConfig.Default, async (qa, pa, ct) => await CoreEx.ExecutionContext.GetRequiredService<IProductReadService>().QueryAsync(qa, pa, ct).ConfigureAwait(false))
     .AddGet<Product>("product", (args, ct) => CoreEx.ExecutionContext.GetRequiredService<IProductReadService>().GetAsync(args["id"]!.ToString()!, ct));
});

// ...
app.MapCoreExGraphQLLite("/api/products/query"); // CoreEx.AspNetCore hosting bridge; defaults to "/query".
```

Because `IGraphQLEngine` is registered as a **singleton**, resolve scoped dependencies (repositories, application services) per-invocation rather than capturing an instance from the root `IServiceProvider` at registration time — as shown above via `CoreEx.ExecutionContext.GetRequiredService<T>()`, which reads from the ambient `ExecutionContext`'s scoped service provider (set by the `UseExecutionContext()` middleware every CoreEx host already registers), so no `IHttpContextAccessor` registration/wiring is needed.

## Query Syntax

Clients use native GraphQL `where`/`orderBy` and `first`/`after` Relay paging — translated 1:1 to the registered `QueryArgsConfig`'s existing `filter`/`orderby` support, so whatever operators/fields a `QueryArgsConfig` exposes for the REST `$filter`/`$orderby` query strings are supported **exactly**:

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

`where` supports bare-scalar equality shorthand (`{ sku: "ABC" }`) or operator objects (`{ sku: { startsWith: "spec" } }`), composed via `and`/`or`/`not`. Both `where` and `orderBy` are **pure syntax translations** — real field/operator validation happens downstream in the entity's own, unmodified `QueryArgsConfig` (same as the REST `$filter`/`$orderby` query strings), so there is no separate allow-list to maintain.

## Do Not

- Do not add mutations, subscriptions, or cross-repository nested resolvers (dataloaders) — this is a read-only, single-root-per-selection bridge by design (v1).
- Do not expect `last`/`before` backward pagination — only `first`/`after` forward pagination is supported; a `last`/`before` argument produces an explicit `ARGUMENT_ERROR`.
- Do not treat the reserved `__schema` root field as spec-compliant GraphQL introspection — it returns a bespoke discovery document (composed from `QueryArgsConfig.ToJsonSchema()` plus a reflection-derived field shape), not the official introspection schema, so tooling that relies on standard introspection (GraphiQL, Apollo Sandbox, codegen) will not auto-explore this endpoint.
- Do not capture a scoped service from the root `IServiceProvider` in a resolver closure — resolve it per-invocation from the current request's scope instead (see Registration above).
- Do not bypass a `QueryArgsConfig` to add new filter/sort capability for GraphQL only — add the field/operator to the entity's existing `QueryArgsConfig` so REST and GraphQL stay in exact lockstep.

## Further Reading

- [README](./README.md) — full capability list, key types, and non-goals.
- [CoreEx.Data](../CoreEx.Data/README.md) — `QueryArgsConfig`, `QueryArgs`, `PagingArgs`, and the safe dynamic-query pipeline this package bridges to.
- [CoreEx.AspNetCore](../CoreEx.AspNetCore/README.md) — `MapCoreExGraphQLLite` hosting bridge and the GraphQL-over-HTTP request/response envelope.
- [Hosts layer](../../samples/docs/hosts-layer.md) — the GraphQL-lite query bridge in a real API host's `Program.cs`.
- [Patterns](../../samples/docs/patterns.md) — dynamic query and field-projection patterns shared by REST and GraphQL-lite.
