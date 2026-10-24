# Domain Layer

The Domain layer is **optional** and is introduced only when a domain contains aggregates with meaningful business rules that must be protected by invariants. Shopping includes this layer; Products, being a largely CRUD-oriented domain, does not.

**Within a domain that has a Domain layer, adoption is per-aggregate, not all-or-nothing.** Not every entity needs to become a full `Aggregate<TId, TSelf>`/`Entity<TId, TSelf>` with mutation guards and `PersistenceState` tracking — only those that benefit from it (real invariants, state transitions, integration events). Simpler, CRUD-oriented entities can coexist in the same domain, handled directly by an Application service against a repository. Mixing and matching within a single domain is expected and fine.

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
    public static Basket CreateNew(string customerId, Address? shippingAddress = null) => new Basket(Runtime.NewId())
    {
        CustomerId = customerId,
        ShippingAddress = shippingAddress,
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
    public required Contracts.UnitOfMeasure UnitOfMeasure
    {
        get;
        init
        {
            value.ThrowIfNull();
            if (!value.IsValid || !value.IsActive)
                throw new BusinessException("Pricing requires a valid, active unit of measure.").WithErrorCode("invalid-unit-of-measure");

            field = value;
        }
    }
    public decimal UnitPrice { get; init => field = value < 0 ? throw new BusinessException("Unit price cannot be negative.").WithErrorCode("negative-unit-price") : value; }
    public decimal Quantity { get; init => field = value < 0 ? throw new BusinessException("Quantity cannot be negative.").WithErrorCode("negative-quantity") : value; }
    public decimal Total => UnitPrice * Quantity;

    public ItemPricing EnsureIsValid() => DecimalRuleHelper.CheckScale(Quantity, UnitOfMeasure.Scale) ? this
        : throw new BusinessException($"Quantity decimal places exceed the unit-of-measure scale of {UnitOfMeasure.Scale}.").WithErrorCode("quantity-scale-exceeded");
}
```

Single-property business invariants are enforced in `init` setters; the owning entity invokes `EnsureIsValid` for cross-property invariants before accepting the value object. `with` expressions run setters, but do not automatically run `EnsureIsValid`; retain that ownership check.

### Business invariants versus request validation

Application validators/policies use `ValidationException` / `Result.ValidationError` for request/input validity, with request-field feedback where known. Aggregates, entities, and value objects enforce business invariants with `BusinessException` / `Result.BusinessError`, meaningful domain messages, and stable codes where useful, not request paths. Both normally map to HTTP 400; they remain different semantic and diagnostic contracts. Never reference request DTOs or derive request JSON paths inside Domain. Application may selectively translate a known domain error when a specific request-field mapping is unambiguous, but must not blanket-convert business errors into validation.

Keep domain enforcement after request validation: other callers, persisted state, and concurrent changes still require invariant protection. Exception versus `Result` is separate: constructors/setters may throw business exceptions, result-returning operations generally return business failures, and `OnCheckCanMutate` failures throw internally through `Modify`/`Remove`.

Argument/invalid-operation exceptions remain appropriate for programming misuse or broken internal assumptions, such as a required internal object being null or an internally assigned status being invalid. The general `ThrowIfLessThanZero` helper throws `ArgumentOutOfRangeException`; reference-data `ThrowIfInvalid`/`ThrowIfInactive` throw `InvalidOperationException`, not business exceptions. Do not use those guards as substitutes for expected business-rule rejection or change their shared semantics. Shopping pricing and discount application use explicit business checks, while internal identifier/object/status guards remain programming-assumption checks.

> **See also**: [Value Object pattern](https://learn.microsoft.com/en-us/dotnet/architecture/microservices/microservice-ddd-cqrs-patterns/implement-value-objects)

---

## Domain Events — Intentionally Not Supported

CoreEx deliberately does not provide a native domain-event mechanism (e.g. MediatR `INotification` dispatch or an in-process event bus). This is a conscious architectural decision:

- **Chatty emission** — fine-grained domain events (`PropertyChanged`, `ItemAdded`, etc.) generate high volumes of events that produce implicit, hard-to-trace side-effects throughout the application layer.
- **Non-explicit side-effects** — handler chains driven by in-process events obscure control flow, making it difficult to reason about what a single aggregate mutation causes.
- **Integration events are sufficient** — coarse-grained integration events are added to `IUnitOfWork.Events` by the application service after a successful repository operation, committed atomically via the transactional outbox, and consumed by other systems explicitly and auditably.

```csharp
// Events are added by the application SERVICE after the repository operation — not by the aggregate itself
return ur.ThenAs(dr =>
{
    var contract = BasketMapper.Map(dr.Value);
    dr.WhereMutated(_ => _unitOfWork.Events.Add(EventData.CreateEventWith(contract, EventAction.CheckedOut)));
    return contract;
});
```

Here `ur` is the repository's `Result<DataResult<Domain.Basket>>`, evaluated inside the write transaction. Domain mapping preserves `WasMutated` until Application decides whether to announce a mutation; success alone, an earlier successful read, or `HasChanges` is insufficient. Successful no-ops return a contract without a mutation event. Commands and deliberate attempt messages follow their own semantics; provider mutation flags do not guarantee committed rows or exactly-once delivery.

A developer can opt in to a domain-event mechanism if genuinely needed — for example, dispatching via MediatR after the transaction commits — but this is an explicit extension, not a framework default.

> **See also**: [`IAggregateRoot`](../../src/CoreEx.DomainDriven/IAggregateRoot.cs) · [`CoreEx.DomainDriven` README](../../src/CoreEx.DomainDriven/README.md#domain-events--intentionally-not-supported)
