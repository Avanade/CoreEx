# Domain Layer

The Domain layer is **optional** and is introduced only when a domain contains aggregates with meaningful business rules that must be protected by invariants. Shopping includes this layer; Products, being a largely CRUD-oriented domain, does not.

**Example project**
- [`samples/src/Contoso.Shopping.Domain`](../src/Contoso.Shopping.Domain)

---

## Aggregates

An aggregate is a cluster of related entities treated as a single consistency boundary. In CoreEx, aggregates extend `Aggregate<TId, TSelf>` and entities within them extend `Entity<TId, TSelf>`. These base classes provide:

- **`PersistenceState` tracking** — `New`, `NotModified`, `Modified`, `Removed` — so the Infrastructure layer knows exactly what to persist without being told explicitly.
- **`Modify(...)` / `Remove()`** — mutation helpers that enforce the `OnCheckCanMutate()` guard and invoke `OnMutate()` for derived state recalculation before marking the aggregate as modified.
- **Factory methods** (`CreateNew` / `CreateFrom`) — the only public construction paths, keeping invariant enforcement centralised and preventing partially-constructed instances.

```csharp
// samples/src/Contoso.Shopping.Domain/Basket.cs
public sealed class Basket : Aggregate<string, Basket>
{
    public static Basket CreateNew(string customerId) => new Basket(Runtime.NewId())
    {
        CustomerId = customerId,
        Status = BasketStatus.Empty
    }.AsNew();

    protected override Result OnCheckCanMutate() => Status.CanBeMutated
        ? Result.Success
        : Result.BusinessError($"Basket has a status of '{Status}' and cannot be modified.", ...);

    protected override void OnMutate()
    {
        // Re-derive status automatically after any mutation.
        if (Status.CanBeMutated)
            Status = _items.Any(i => i.PersistenceState.IsNotRemoved) ? BasketStatus.Active : BasketStatus.Empty;
    }
}
```

All public domain operations return `Result` or `Result<T>` so that the Application layer can compose them in pipelines without catching exceptions for expected business failures.

> **See also**: [`Aggregate<TId, TSelf>`](../../src/CoreEx.DomainDriven/Aggregate.cs) · [`Entity<TId, TSelf>`](../../src/CoreEx.DomainDriven/Entity.cs) · [`PersistenceState`](../../src/CoreEx.DomainDriven/PersistenceState.cs) · [Domain-Driven Design aggregates](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/microservice-domain-model)

---

## Value objects

Value objects represent concepts that have no independent identity — they are defined entirely by their values and are immutable. In CoreEx they are modelled as `sealed record` types, leveraging C# record equality and `with`-expression mutation.

```csharp
// samples/src/Contoso.Shopping.Domain/ValueObjects/ItemPricing.cs
public sealed record class ItemPricing
{
    public required Contracts.UnitOfMeasure UnitOfMeasure { get; init => field = value.ThrowIfInactive(); }
    public decimal UnitPrice { get; init => field = value.ThrowIfLessThanZero(); }
    public decimal Quantity { get; init => field = value.ThrowIfLessThanZero(); }
    public decimal Total => UnitPrice * Quantity;
}
```

Value objects enforce their own invariants in property initialisers using CoreEx guard extensions (`ThrowIfInactive`, `ThrowIfLessThanZero`) so that invalid instances simply cannot exist.

> **See also**: [Value Object pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects)
