# CoreEx.Cosmos — AI Usage Guide

Azure Cosmos DB implementation of the CoreEx core CRUD + query access layer pattern (model-direct and contract-to-model), structurally mirroring `CoreEx.EntityFrameworkCore`'s `EfDb`/`EfDbModel`/`EfDbMappedModel` shape, plus a `TransactionalBatch`-based transactional outbox and a Change Feed Processor-based outbox relay.

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

- `ICosmosDb.Container<TModel>(containerId, configure?)` is cached per `(containerId, TModel)` pair - **not** `containerId` alone, since a container may legitimately host more than one type-discriminated model (see "Multi-type containers" below); the `configure` action only runs the first time for a given pair.
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

## Transactional Outbox

`CosmosDbUnitOfWork` implements `IUnitOfWork` directly (not a Cosmos-specific sub-interface, so application services stay provider-agnostic). Enlisted Create/Update/Delete calls made inside `TransactionAsync` accumulate into one ambient `TransactionalBatch` - Cosmos DB's only atomic multi-operation primitive, atomic only within a single container/logical partition key - and execute once, at the end. `CosmosDbEventPublisher` (an `IEventPublisher`) enlists outbox event documents into the *same* batch, so the business mutation and its event are atomic without a separate outbox table:

```csharp
// Program.cs (host builder)
builder.Services
    .AddCosmosDb("MyDatabaseId")
    .AddScoped<IEventPublisher, CosmosDbEventPublisher>()
    .AddScoped<CosmosDbUnitOfWork>();

// Application service
await unitOfWork.TransactionAsync(async ct =>
{
    var created = await orders.CreateAsync(order, ct);
    unitOfWork.Events.Add(EventData.CreateEventWith(created.Value, EventAction.Created));
});
```

Outbox documents are identified by a reserved `$outbox` `Id` prefix and auto-excluded from ordinary business queries against the same container - no opt-in filter required. A cross-container/cross-partition-key enlistment throws `InvalidOperationException` client-side, before any network call. There is no "read your own uncommitted writes" within a unit-of-work - nothing is persisted until the batch executes at the end, so a `Query()`/`GetAsync` call inside `TransactionAsync`'s `work` delegate cannot see an earlier write from the *same* unit-of-work.

`IUnitOfWork.SynchronizeETag<T>(CompositeKey, T)` resolves a mapped contract's true, server-assigned `ETag` after the batch commits (correlated by `CompositeKey`, not object reference, since the object passed here is the mapped *contract* published as an event, not the *model* actually mutated) - call it only after `TransactionAsync` has completed, never from inside `work`.

## Outbox Relay

`CosmosDbOutboxRelay` consumes outbox event documents via a Cosmos DB [Change Feed Processor](https://learn.microsoft.com/en-us/azure/cosmos-db/nosql/change-feed-processor) - push-based and SDK-managed, not a polling loop like the SQL Server/Postgres relay - decodes/publishes/cleans up each batch, and self-pauses/self-resumes via a circuit breaker on a sustained publish-failure ratio:

```csharp
// Relay host Program.cs
builder.Services.AddCosmosDb("MyDatabaseId");
builder.AddCosmosDbOutboxRelayHostedService("orders");   // one call per outbox-hosting container
builder.AddCosmosDbOutboxRelayHostedService("customers", servicesCount: 1);   // lower-volume container, fewer concurrent instances
```

Poison-message/dead-letter handling is **not yet implemented** - a permanently-failing outbox document is redelivered forever by the Change Feed Processor's own native backoff (confirmed empirically against the emulator), with no built-in give-up, and can starve delivery of other, unrelated outbox documents sharing the same physical partition-key-range/lease. Since a Change Feed Processor lease checkpoints strictly in order, this isn't a bounded delay - it blocks every later change in that lease indefinitely, until either the underlying cause is fixed (so the same, already-captured change eventually succeeds) or an operator intervenes at the lease/checkpoint level; deleting the live document does not help, since the change feed record being retried is an immutable snapshot, not a live read. To be designed as one shared pattern across the SQL Server/Postgres/Cosmos relays, not Cosmos-specific.

**Detecting a stuck lease:** `cosmos.outbox.enqueue` continuing to climb while `cosmos.outbox.relay.publish` stays flat for the same container is the signal - the write side is unaffected by a stuck relay, so a sustained divergence between the two indicates a lease is blocked. `cosmos.outbox.relay.oldest_lag`/`newest_lag` are recorded on both a successful and a failed publish attempt, so they keep climbing (rather than going silent) for as long as a batch keeps failing - alert on a sustained rise in `cosmos.outbox.relay.oldest_lag`, not just on `cosmos.outbox.relay.publish.failed`, since a low failure count can still mean one lease has been stuck for a long time.

## Do Not

- Do not construct `CosmosClient` directly in application code — resolve it from DI (Aspire's `AddAzureCosmosClient`).
- Do not assume `GetAsync`/`DeleteAsync` only need a `CompositeKey` — a `PartitionKey` is always required for a Cosmos DB point operation at the SDK level; it can be omitted from the call only when `CosmosDbModelOptions<TModel>.WithFixedPartitionKey` is configured (otherwise it throws `InvalidOperationException`).
- Do not configure both `WithPartitionKey` and `WithFixedPartitionKey` on the same `CosmosDbModelOptions<TModel>` — they are mutually exclusive and throw `InvalidOperationException` immediately if you try. `WithPartitionKey`'s function can only ever help `CreateAsync`/`UpdateAsync` (it needs a model instance); only `WithFixedPartitionKey` also defaults `GetAsync`/`DeleteAsync`.
- Do not mutate a caller-supplied `CosmosDbArgs.ItemRequestOptions` instance expecting `AutoMapETag` to still apply — `AutoMapETag` only synthesizes an `ItemRequestOptions` when the caller has not already supplied one; set `IfMatchEtag` explicitly if you need both.
- Do not use `CosmosDbMappedContainer<TValue, TModel, TMapper>.Query()` — it does not exist by design; query stays model-typed (use `CosmosDbContainer<TModel>.Query()` plus `CosmosDbQuery<TModel>.ToMappedItemsResultAsync`/`ToMappedItemsAsync`).
- Do not call `SynchronizeETag` from inside a `TransactionAsync` `work` delegate — the batch (and therefore the true server-assigned `ETag`) has not executed yet; it throws `InvalidOperationException`.
- Do not assume a `CosmosDbOutboxRelay`/relay hosted service durably handles a permanently-failing event — see "Outbox Relay" above; there is no dead-letter mechanism yet.
- Do not materialize a query by defining a bare, generic-sounding `IQueryable<T>` extension method (`ToListAsync`, `ToItemsResultAsync`, etc.) — `CoreEx.EntityFrameworkCore.EfDbExtensions` already defines several identically-shaped ones, and C# extension-method resolution has no precedence rule between two equally-applicable candidates: it's a hard `CS0121` ambiguous-call compile error in any file that imports both namespaces, not just a style clash. Add materializers as instance methods on `CosmosDbQuery<TModel>` (or an equivalent package-owned wrapper type) instead — a different receiver type cannot collide, so plain names (`ToListAsync`, `ToItemsResultAsync`, ...) are safe there. Only fall back to a `To{Provider}XxxAsync`-prefixed `IQueryable<T>` extension if a package genuinely cannot own a wrapper type.

## Further Reading

- [README](./README.md) — full API reference including `CosmosDb`, `CosmosDbContainer<TModel>`, `CosmosDbModelOptions<TModel>`, `CosmosDbQuery<TModel>`, `CosmosDbUnitOfWork`, and the `Outbox` sub-namespace.
- [CoreEx.EntityFrameworkCore](../CoreEx.EntityFrameworkCore/AGENTS.md) — the closest structural analogue (`EfDb`/`EfDbModel`/`EfDbMappedModel`).
- [CoreEx.Database.SqlServer](../CoreEx.Database.SqlServer/AGENTS.md) / [CoreEx.Database.Postgres](../CoreEx.Database.Postgres/AGENTS.md) — the relational sibling families; compare DI/registration conventions (`AddAzureCosmosClient` vs `AddSqlServerClient`/`AddAzureNpgsqlDataSource`) and outbox relay wiring (poll-loop vs Change Feed Processor), though metric names and trace-linking are shared unchanged.
