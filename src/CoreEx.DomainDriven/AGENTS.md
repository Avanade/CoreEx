# CoreEx.DomainDriven — AI Usage Guide

Provides DDD building blocks: typed entities, aggregate roots with integration-event support, persistence-state tracking, and mutation-guard helpers.

## Entity Base

Extend `Entity<TId, TSelf>` for domain entities that require identity-based equality and mutation guards.

```csharp
public class Order : Entity<Guid, Order>
{
    public string? Reference { get; private set; }

    // Mutations always go through Modify/Remove to advance PersistenceState
    public Result SetReference(string? value) =>
        Modify(() => Reference = value);

    // Pre-mutation business-rule validation — called automatically before every Modify/Remove
    protected override Result OnCheckCanMutate() =>
        IsReadOnly ? Result.InvalidError("Order cannot be changed once dispatched.") : Result.Success;
}
```

## Aggregate Root

Use `Aggregate<TId, TSelf>` when the entity accumulates integration events. Call `AddEvent` within mutations, and let the application service drain `Events` into the unit-of-work publisher inside `TransactionAsync`.

```csharp
public class Basket : Aggregate<Guid, Basket>
{
    public Result Checkout()
    {
        return Modify(() =>
        {
            Status = BasketStatus.CheckedOut;
            AddEvent(new EventData { Subject = "contoso.shopping.basket.checkedout.v1", Key = Id.ToString() });
        });
    }
}

// Application service
await _uow.TransactionAsync(async () =>
{
    var basket = await _repo.GetAsync(id, ct).ConfigureAwait(false);
    basket.Checkout().ThrowOnError();
    await _repo.UpdateAsync(basket, ct).ConfigureAwait(false);

    // Drain aggregate events into the outbox
    foreach (var e in basket.Events)
        _uow.Events.Publish(e);

    basket.ClearEvents();
}).ConfigureAwait(false);
```

## PersistenceState

Infrastructure layers use `SetPersistenceState`, `AsNew()`, `AsNotModified()` — never `Modify()` — to hydrate an entity from the database.

```csharp
// Infrastructure mapper
entity.AsNotModified()
      .SetChangeLog(changeLog)
      .SetETag(etag);
```

## Do Not

- Do not call `Modify()` or `Remove()` from infrastructure or persistence code — they are for domain mutations only.
- Do not set `IsReadOnly` from outside the entity; call `MakeReadOnly()` on the entity itself.
- Do not add domain events to this package — it intentionally supports only integration events (`EventData`).

## Further Reading

- [README](./README.md) — full `Entity`, `Aggregate`, `PersistenceState`, and mutation-guard API reference.
- [CoreEx](../CoreEx/README.md) — `IIdentifier`, `IChangeLog`, `IETag`, `EventData`, and `Result<T>`.
- [CoreEx.EntityFrameworkCore](../CoreEx.EntityFrameworkCore/README.md) — persists `Entity`/`Aggregate` types using `PersistenceState`.
- [Domain layer](../../samples/docs/domain-layer.md) — real-world aggregate design, mutation guards, integration-event accumulation, and `Result<T>` pipeline usage in the Shopping sample.
- [Patterns](../../samples/docs/patterns.md) — aggregate-oriented service patterns, domain event flow, and mutation-state tracking.
