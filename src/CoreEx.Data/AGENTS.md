# CoreEx.Data — AI Usage Guide

Provides `IUnitOfWork` (the transactional boundary), `DataResult` mutation outcomes, and the `QueryArgsConfig` safe dynamic-query pipeline.

## IUnitOfWork — Transactional Boundary

Wrap all database mutations **and** event publishing inside `TransactionAsync`. Both the database write and the outbox event are committed atomically or rolled back together.

```csharp
public class OrderService(IUnitOfWork uow, IOrderRepository repo)
{
    public async Task<Order> CreateAsync(Order order, CancellationToken ct = default)
    {
        // validate first (outside the transaction)
        await _validator.ValidateAndThrowAsync(order, ct).ConfigureAwait(false);

        return await uow.TransactionAsync(async () =>
        {
            var created = await repo.CreateAsync(order, ct).ConfigureAwait(false);

            // enqueue event — published atomically with the DB commit via the outbox
            uow.Events.Add(EventData.CreateEventWith(created, EventAction.Created));

            return created;
        }).ConfigureAwait(false);
    }
}
```

## QueryArgsConfig — Safe Dynamic Filter / OrderBy

Configure an explicit allow-list of filterable and sortable fields. Never expose raw `$filter` strings to LINQ without this.

```csharp
private static readonly QueryArgsConfig _queryConfig = QueryArgsConfig.Create()
    .WithFilter(f => f
        .AddField<string>(nameof(Order.Status), c => c.WithDefault("A"))
        .AddField<DateTimeOffset>(nameof(Order.CreatedOn)))
    .WithOrderBy(o => o
        .AddField(nameof(Order.CreatedOn))
        .WithDefault($"{nameof(Order.CreatedOn)} desc"));

// In the repository
public async Task<ItemsResult<Order>> GetAllAsync(QueryArgs args, CancellationToken ct = default)
    => await _efDb.Model<OrderModel>()
        .Query(new EfDbArgs(args, _queryConfig))
        .ToItemsResultAsync(MapToEntity, ct).ConfigureAwait(false);
```

## DataResult

Return `DataResult` (no value) or `DataResult<T>` from mutation operations to distinguish "mutated" from "not found" without throwing.

```csharp
public async Task<DataResult> DeleteAsync(Guid id, CancellationToken ct = default)
    => await uow.TransactionAsync(async () =>
    {
        var dr = await repo.DeleteAsync(id, ct).ConfigureAwait(false);
        dr.WhereMutated(() =>
            uow.Events.Add(EventData.CreateEventWith<Order>(default, EventAction.Deleted).WithKey(id)));
        return dr;
    }).ConfigureAwait(false);
```

## Do Not

- Prefer enqueuing events inside `TransactionAsync` so they are committed or rolled back atomically with the database write. Events added outside a transaction scope are still published but will not be rolled back if a subsequent operation fails — only do this intentionally when at-least-once delivery without rollback is the desired behaviour.
- Do not expose raw `$filter` or `$orderby` strings to LINQ — always use `QueryArgsConfig`.

## Further Reading

- [README](./README.md) — full `IUnitOfWork`, `QueryArgsConfig`, and `DataResult` API reference.
- [CoreEx.Database.SqlServer](../CoreEx.Database.SqlServer/README.md) / [CoreEx.Database.Postgres](../CoreEx.Database.Postgres/README.md) — concrete `IUnitOfWork` implementations.
- [CoreEx.EntityFrameworkCore](../CoreEx.EntityFrameworkCore/README.md) — `QueryArgsConfig` consumption via `EfDbModel`.
- [Application layer](../../samples/docs/application-layer.md) — real-world `TransactionAsync` usage, event enqueuing inside the unit-of-work, and service orchestration patterns.
- [Patterns](../../samples/docs/patterns.md) — transactional outbox, atomic commit with event publishing, and dynamic query patterns.
