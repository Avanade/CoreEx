# CoreEx.Cosmos — AI Usage Guide

Azure Cosmos DB implementation of the CoreEx core CRUD + query access layer pattern (model-direct and contract-to-model), structurally mirroring `CoreEx.EntityFrameworkCore`'s `EfDb`/`EfDbModel`/`EfDbMappedModel` shape.

## Registration

```csharp
// Program.cs (host builder)
builder.AddAzureCosmosClient("Cosmos");   // Aspire resource name; registers CosmosClient + health check + telemetry
builder.Services.AddCosmosDb("MyDatabaseId");
```

`AddCosmosDb` does **not** register the `CosmosClient` itself — it is resolved from DI (registered separately via Aspire's `AddAzureCosmosClient`). No custom health check is registered either, since Aspire's client integration already provides one.

## Container access

```csharp
public class OrderRepository(ICosmosDb cosmosDb)
{
    private readonly CosmosDbContainer<OrderModel> _orders = cosmosDb.Container<OrderModel>("orders", o => o.WithPartitionKey(m => m.CustomerId));

    public Task<OrderModel?> GetAsync(string id, string customerId, CancellationToken ct = default)
        => _orders.GetAsync(CompositeKey.Create(id), new PartitionKey(customerId), ct);
}
```

- `ICosmosDb.Container<TModel>(containerId, configure?)` is cached per `containerId`; the `configure` action only runs the first time.
- `TModel` must implement `IEntityKey` (for `EntityKey`/`CompositeKey`) — everything else (`IETag`, `IPartitionKey`, `ITenantId`, `ITypeDiscriminator`, `ILogicallyDeleted`) is duck-typed via `is` checks, exactly like `EfDbModelOptions`. Use `CosmosDbItemBase` as an optional convenience base implementing the common ones.
- **Deviation from `EfDbModel<TModel>`**: `GetAsync`/`DeleteAsync` take an optional `PartitionKey?` parameter (in addition to the `CompositeKey`) because a Cosmos DB point-read/point-delete is fundamentally two-dimensional (`id` + partition key), unlike a relational primary-key lookup. Omit it to fall back to `WithFixedPartitionKey` (where configured) — otherwise it throws `InvalidOperationException`. `CreateAsync`/`UpdateAsync`/`UpsertAsync` derive the partition key from the model via `WithPartitionKey`, `WithFixedPartitionKey`, or the model's own `IReadOnlyPartitionKey.PartitionKey` (in that precedence — configuration always wins, and a configured value that disagrees with a non-null model value throws rather than silently overriding it).

## Error Mapping

`CosmosDbInvoker` catches `CosmosException` and maps by `StatusCode` via `ICosmosDb.HandleCosmosException`:

| `CosmosException.StatusCode` | CoreEx exception |
|---|---|
| `404 NotFound` | `NotFoundException` |
| `409 Conflict` | `DuplicateException` |
| `412 PreconditionFailed` | `ConcurrencyException` |

`GetAsync`'s **throwing** overload additionally honours `CosmosDbArgs.NullOnNotFound` (default `true`) — a `404` returns `null` rather than throwing. The `WithResult` (ROP) overloads always return `Result.NotFoundError()` on a `404`, irrespective of `NullOnNotFound`. `DeleteAsync` (physical delete) is idempotent — a `404` is not an error and results in `DataResult.False`.

## Optimistic concurrency

`UpdateAsync` maps the model's `IETag.ETag` into `ItemRequestOptions.IfMatchEtag` when `CosmosDbArgs.AutoMapETag` is `true` (the default) **and** the caller has not already supplied their own `ItemRequestOptions` — Cosmos DB enforces the check server-side and returns a `412`, which converts to `ConcurrencyException`/`Result.ConcurrencyError` automatically via the table above.

## Multi-type containers (type discriminator)

```csharp
cosmosDb.Container<RefDataTypeA>("refdata", o => o.WithTypeDiscriminatorFilter());
cosmosDb.Container<RefDataTypeB>("refdata", o => o.WithTypeDiscriminatorFilter());
```

`TModel` implements `ITypeDiscriminator` directly (a flat document property, auto-stamped by `Model.PrepareCreate`/`PrepareUpdate` from `SchemaAttribute.Name`); `WithTypeDiscriminatorFilter()` adds a query-time `Where` filter so several business model types can safely share one container/partition — no envelope/wrapper type is used.

## Time-to-live

```csharp
cosmosDb.Container<OrderModel>("orders", o => o
    .WithPartitionKey(m => m.CustomerId)
    .WithTimeToLive(m => m.Status == "Closed" ? 60 * 60 * 24 * 30 : null));   // 30 days for closed orders; no expiry otherwise
```

`WithTimeToLive(Func<TModel, int?>)` is applied automatically on `CreateAsync`/`UpdateAsync` (after `Model.PrepareCreate`/`PrepareUpdate` stamping, before persisting) and requires `TModel` to implement the **mutable** `ITimeToLive` (throws `NotSupportedException`, checked via `TimeToLiveSupport.IsMutable` — not just `.IsSupported`, since `ITimeToLive` needs a setter to write the computed value back onto the model; there is no separate Cosmos DB SDK request-option channel for `ttl` the way there is for a partition key). Not configuring it is the common case — a model's own `ITimeToLive.TimeToLive` value (if any) just serializes through unmodified.

## Fixed partition key

```csharp
cosmosDb.Container<LookupModel>("lookups", o => o.WithFixedPartitionKey("shared"));

// GetAsync/DeleteAsync's partitionKey parameter is now optional - omit it to use the fixed value.
var lookup = await cosmosDb.Container<LookupModel>("lookups").GetAsync(CompositeKey.Create(id));
```

`WithFixedPartitionKey(string?)` configures one constant partition key value for the whole container — suitable for small, bounded containers where a high-cardinality partition key isn't needed (see the `WithFixedPartitionKey` XML doc for the underlying Cosmos DB guidance). Unlike `WithPartitionKey(Func<TModel, string?>)` (which needs a model instance, so only ever helps `CreateAsync`/`UpdateAsync`), the fixed value is also the default for `GetAsync`/`DeleteAsync`'s now-optional `partitionKey` parameter — it is the only mechanism that can default those. The two are mutually exclusive (`InvalidOperationException` if both are configured), and both take **`string?`**, not the Cosmos DB SDK's `PartitionKey` struct — that struct has no public way to extract its own value back out once constructed, which matters because a configured value must be written back onto `TModel` (where it implements the mutable `IPartitionKey`) before `CreateAsync`/`UpdateAsync`: Cosmos DB rejects a write where the document body's value at the partition-key path disagrees with the value supplied for the operation, so this write-back is required for correctness, not just convenience. A non-null value the model already carries that *disagrees* with the configured one throws `InvalidOperationException` rather than being silently overridden.

## Querying and paging

`CosmosDbContainer<TModel>.Query(query?, args?)` returns a `CosmosDbQuery<TModel>` — a dedicated wrapper type, not a bare `IQueryable<TModel>`. Compose additional filtering/ordering via the `query`
delegate (standard LINQ); materialize via instance methods on the wrapper:

```csharp
var page = await cosmosDb.Container<OrderModel>("orders")
    .Query(q => q.Where(m => m.Status == "Open"))
    .WithPaging(PagingArgs.Create(skip: 0, take: 25, count: true))
    .ToItemsResultAsync();
```

Paging uses `Skip`/`Take` (translated by the Cosmos DB LINQ provider to `OFFSET…LIMIT`) applied via `WithPaging(PagingArgs?)`; continuation-token-based paging is not currently supported. Every
`CosmosDbQuery<TModel>` materializer (`ToListAsync`, `ToItemsResultAsync`, `SingleAsync`/`FirstAsync`-family, `ToMappedItemsAsync`, `ToMappedItemsResultAsync`, each with a `WithResultAsync` ROP
counterpart) routes through `CosmosDbInvoker` — the same structured logging + `CosmosException` mapping as every CRUD operation. Use `AsQueryable(args?)` for ad-hoc `IQueryable<TModel>` composition
(e.g. within a repository method); pass `new CosmosDbArgs { BypassFilters = true }` to skip the `CosmosDbModelOptions`-configured filters entirely.

## Do Not

- Do not construct `CosmosClient` directly in application code — resolve it from DI (Aspire's `AddAzureCosmosClient`).
- Do not assume `GetAsync`/`DeleteAsync` only need a `CompositeKey` — a `PartitionKey` is always required for a Cosmos DB point operation at the SDK level; it can be omitted from the call only when `CosmosDbModelOptions<TModel>.WithFixedPartitionKey` is configured (otherwise it throws `InvalidOperationException`).
- Do not configure both `WithPartitionKey` and `WithFixedPartitionKey` on the same `CosmosDbModelOptions<TModel>` — they are mutually exclusive and throw `InvalidOperationException` immediately if you try. `WithPartitionKey`'s function can only ever help `CreateAsync`/`UpdateAsync` (it needs a model instance); only `WithFixedPartitionKey` also defaults `GetAsync`/`DeleteAsync`.
- Do not mutate a caller-supplied `CosmosDbArgs.ItemRequestOptions` instance expecting `AutoMapETag` to still apply — `AutoMapETag` only synthesizes an `ItemRequestOptions` when the caller has not already supplied one; set `IfMatchEtag` explicitly if you need both.
- Do not use `CosmosDbMappedContainer<TValue, TModel, TMapper>.Query()` — it does not exist by design; query stays model-typed (use `CosmosDbContainer<TModel>.Query()` plus `CosmosDbQuery<TModel>.ToMappedItemsResultAsync`/`ToMappedItemsAsync`).
- Do not introduce a transactional `IUnitOfWork`/outbox here — that is a separate, not-yet-implemented concern (`ICosmosUnitOfWork`, change-feed-based relay); this package is CRUD + query only.
- Do not materialize a query by defining a bare, generic-sounding `IQueryable<T>` extension method (`ToListAsync`, `ToItemsResultAsync`, etc.) — `CoreEx.EntityFrameworkCore.EfDbExtensions` already defines several identically-shaped ones, and C# extension-method resolution has no precedence rule between two equally-applicable candidates: it's a hard `CS0121` ambiguous-call compile error in any file that imports both namespaces, not just a style clash. Add materializers as instance methods on `CosmosDbQuery<TModel>` (or an equivalent package-owned wrapper type) instead — a different receiver type cannot collide, so plain names (`ToListAsync`, `ToItemsResultAsync`, ...) are safe there. Only fall back to a `To{Provider}XxxAsync`-prefixed `IQueryable<T>` extension if a package genuinely cannot own a wrapper type.

## Further Reading

- [README](./README.md) — full API reference including `CosmosDb`, `CosmosDbContainer<TModel>`, `CosmosDbModelOptions<TModel>`, and `CosmosDbQuery<TModel>`.
- [CoreEx.EntityFrameworkCore](../CoreEx.EntityFrameworkCore/AGENTS.md) — the closest structural analogue (`EfDb`/`EfDbModel`/`EfDbMappedModel`).
- [CoreEx.Database.SqlServer](../CoreEx.Database.SqlServer/AGENTS.md) — the relational sibling family; compare DI/registration conventions (`AddAzureCosmosClient` vs `AddSqlServerClient`).
