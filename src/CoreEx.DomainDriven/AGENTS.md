# CoreEx.DomainDriven — AI Usage Guide

Provides DDD building blocks: aggregate roots (with integration events), typed entities, persistence-state tracking, and mutation-guard helpers.

## Aggregate Root

`Aggregate<TId, TSelf>` is an `Entity<TId, TSelf>` with an `Events` collection for accumulating integration events. The aggregate owns its invariants; the application service is responsible for forwarding any accumulated events to `IUnitOfWork.Events` after a successful repository operation.

```csharp
public sealed class Basket : Aggregate<string, Basket>
{
    public Result Checkout()
    {
        if (Status == BasketStatus.Empty)
            return Result.BusinessError("An empty basket cannot be checked out.",
                c => c.WithKey(Id).WithErrorCode("empty-basket"));

        Modify(() => Status = BasketStatus.CheckedOut);
        return Result.Success;
    }

    protected override Result OnCheckCanMutate() => Status.CanBeMutated
        ? Result.Success
        : Result.BusinessError($"Basket has a status of '{Status}' and cannot be modified.",
            c => c.WithKey(Id).WithErrorCode("invalid-status"));
}
```

In the application service, events are added to `IUnitOfWork.Events` after the repository update — typically using `EventData.CreateEventWith` on the mapped contract, not inside the aggregate itself:

```csharp
// Application service — events added by the service, not the aggregate
return await _unitOfWork.TransactionAsync(async () =>
{
    var ur = await _repository.UpdateAsync(basket).ConfigureAwait(false);
    return ur.ThenAs(b =>
    {
        var contract = BasketMapper.Map(b);
        _unitOfWork.Events.Add(EventData.CreateEventWith(contract, EventAction.CheckedOut));
        return contract;
    });
}).ConfigureAwait(false);
```

> `Aggregate<TId, TSelf>` also exposes `AddEvent` / `ClearEvents` for cases where the aggregate itself needs to accumulate events internally, but this is not the primary pattern used in the sample domains.

## Entity Base

`Entity<TId, TSelf>` provides identity-based equality, `PersistenceState` tracking, and mutation guards. All state changes go through `Modify()` or `Remove()` so the framework can track whether an entity is new, modified, or deleted.

```csharp
public class BasketItem : Entity<string, BasketItem>
{
    public decimal Quantity { get; private set; }

    public Result OverrideQuantity(decimal quantity) =>
        Modify(() => Quantity = quantity);

    protected override Result OnCheckCanMutate() =>
        PersistenceState.IsDeleted
            ? Result.BusinessError("Cannot modify a deleted item.")
            : Result.Success;
}
```

## PersistenceState

Infrastructure layers use `AsNew()`, `AsNotModified()`, and `SetPersistenceState()` to hydrate entities from the database — never `Modify()`.

```csharp
// Infrastructure mapper — hydrating from DB
entity.AsNotModified()
      .SetChangeLog(changeLog)
      .SetETag(etag);
```

## Do Not

- Do not call `Modify()` or `Remove()` from infrastructure or persistence code — they are for domain mutations only.
- Do not set `IsReadOnly` from outside the entity; call `MakeReadOnly()` on the entity itself.
- Do not add domain events to this package — it intentionally supports only integration events (`EventData`).

## Domain Events — Intentionally Not Supported

CoreEx does not provide a native in-process domain-event bus. Use `IUnitOfWork.Events` and the transactional outbox for integration events instead. See [README — Domain Events](./README.md#domain-events--intentionally-not-supported) for the full rationale.

## Further Reading

- [README](./README.md) — full `Entity`, `Aggregate`, `PersistenceState`, and mutation-guard API reference.
- [CoreEx](../CoreEx/README.md) — `IIdentifier`, `IChangeLog`, `IETag`, `EventData`, and `Result<T>`.
- [CoreEx.EntityFrameworkCore](../CoreEx.EntityFrameworkCore/README.md) — persists `Entity`/`Aggregate` types using `PersistenceState`.
- [Domain layer](../../samples/docs/domain-layer.md) — real-world aggregate design, mutation guards, integration-event accumulation, and `Result<T>` pipeline usage in the Shopping sample.
- [Patterns](../../samples/docs/patterns.md) — aggregate-oriented service patterns, domain event flow, and mutation-state tracking.

