---
applyTo: "**/Domain/**/*.cs"
description: "Domain layer conventions: aggregates, entities, value objects, PersistenceState tracking, and Result-based mutation methods"
tags: ["domain", "ddd", "aggregates", "entities", "value-objects", "result"]
---

# Domain Layer Conventions

The Domain layer is **optional**. It is introduced only when a domain contains aggregates with meaningful business rules and invariants that must be enforced at the model level — not in orchestration code. Shopping includes this layer; Products, being a largely CRUD-oriented domain, does not.

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx.DomainDriven` | `Aggregate<TId, TSelf>`, `Entity<TId, TSelf>`, `PersistenceState`, `.AsNew()`, `.AsNotModified()`, `.SetPersistenceState()` |
| `CoreEx` | `Result`, `Result<T>`, `Runtime.NewId()`, `.ThrowIfNull()`, `.ThrowIfNullOrEmpty()`, `.ThrowIfInactive()`, `.ThrowIfLessThanZero()`, `ValidationException` |
| `CoreEx.Results` | `Result.GoAsync()`, `.ThenAs()`, `.ThenAsAsync()`, `Result.BusinessError()`, `Result.NotFoundError()`, `Result.ValidationError()` |

## Aggregates

Aggregates are clusters of related entities treated as a single consistency boundary. Extend `Aggregate<TId, TSelf>`:

```csharp
public sealed class Basket : Aggregate<string, Basket>
{
    private List<BasketItem> _items = [];

    // Factory methods are the only public construction paths.
    public static Basket CreateNew(string customerId) => new Basket(Runtime.NewId())
    {
        CustomerId = customerId,
        Status = BasketStatus.Empty
    }.AsNew();

    public static Basket CreateFrom(string id, string customerId, BasketStatus status,
        IEnumerable<BasketItem>? items, ChangeLog? changeLog, string? etag) => new Basket(id)
    {
        CustomerId = customerId,
        Status = status,
        _items = items is null ? [] : [.. items.Select(i => i.Clone(PersistenceState.NotModified))],
        ChangeLog = changeLog,
        ETag = etag
    }.AsNotModified();

    private Basket(string id) : base(id) { }

    public string CustomerId { get; private set => field = value.ThrowIfNullOrEmpty(); } = null!;
    public BasketStatus Status { get; private set => field = value.ThrowIfNull().ThrowIfInactive(); } = null!;
    public IReadOnlyList<BasketItem> Items => _items;
    public decimal Total => _items.Where(i => i.PersistenceState.IsNotRemoved).Sum(i => i.Pricing.Total);
}
```

### Factory Methods

Provide two factory methods per aggregate:

- `CreateNew(...)` — constructs a new aggregate with a generated ID, initial state, and calls `.AsNew()` to mark it as `PersistenceState.New`.
- `CreateFrom(...)` — reconstructs from persisted data and calls `.AsNotModified()`.

Both are the **only** public construction paths. The constructor is `private` to prevent partially-constructed instances.

### Mutation Guards — `OnCheckCanMutate`

Override `OnCheckCanMutate()` to enforce the conditions under which the aggregate may accept mutations. Return `Result.BusinessError(...)` (not an exception) when the condition is not met:

```csharp
protected override Result OnCheckCanMutate() => Status.CanBeMutated
    ? Result.Success
    : Result.BusinessError($"Basket has a status of '{Status}' and cannot be modified.",
        c => c.WithKey(Id).WithErrorCode("invalid-status"));
```

### Post-Mutation Recalculation — `OnMutate`

Override `OnMutate()` to re-derive any dependent state after a mutation is applied. This is called automatically by `Modify(...)` after the mutation succeeds:

```csharp
protected override void OnMutate()
{
    if (Status.CanBeMutated)
        Status = _items.Any(i => i.PersistenceState.IsNotRemoved) ? BasketStatus.Active : BasketStatus.Empty;
}
```

### Public Mutation Methods

All public mutation methods must return `Result` or `Result<T>` so the Application layer can compose them in pipelines. Use `Modify(...)` to apply the mutation, which enforces the `OnCheckCanMutate()` guard:

```csharp
public Result ItemAdd(BasketItem item) => Modify(() =>
{
    item.ThrowIfNull();
    if (_items.FirstOrDefault(i => i.ProductId == item.ProductId && i.PersistenceState.IsNotRemoved) is BasketItem existing)
        existing.IncreaseQuantity(item.Pricing.Quantity);
    else
        _items.Add(item.Clone(PersistenceState.New));

    return Result.Success;
});

public Result ItemUpdate(string basketItemId, decimal quantity, string? etag)
{
    var item = _items.FirstOrDefault(i => i.Id == basketItemId.ThrowIfNullOrEmpty() && i.PersistenceState.IsNotRemoved);
    if (item is null)
        return Result.NotFoundError();

    if (quantity != item.Pricing.Quantity)
        Modify(() =>
        {
            item.OverrideQuantity(quantity);
            item.SetETag(etag);
        });

    return Result.Success;
}
```

## Entities

Child entities within an aggregate extend `Entity<TId, TSelf>`. Apply the same factory-method and private-constructor pattern:

```csharp
public sealed class BasketItem : Entity<string, BasketItem>
{
    public static BasketItem CreateNew(string productId, string sku, string text, ItemPricing pricing)
        => new BasketItem(Runtime.NewId()) { ProductId = productId, Sku = sku, Text = text, Pricing = pricing }.AsNew();

    public static BasketItem CreateFrom(string id, string productId, string sku, string text, ItemPricing pricing, string? etag)
        => new BasketItem(id) { ProductId = productId, Sku = sku, Text = text, Pricing = pricing, ETag = etag }.AsNotModified();

    private BasketItem(string id) : base(id) { }

    public string ProductId { get; private set => field = value.ThrowIfNullOrEmpty(); } = null!;
    public string Sku { get; private set => field = value.ThrowIfNullOrEmpty(); } = null!;
    public ItemPricing Pricing { get; private set => field = value.ThrowIfNull().EnsureIsValid(); } = null!;

    // Internal mutation helpers — only callable by the owning aggregate.
    internal void OverrideQuantity(decimal quantity) => Modify(() => Pricing = Pricing with { Quantity = quantity });
    internal void Delete() => Remove();
}
```

Keep mutation methods on child entities `internal` so they can only be invoked by the owning aggregate — never directly from the Application layer.

## PersistenceState

`PersistenceState` tracks the lifecycle of each aggregate and entity so the Infrastructure layer knows exactly what to persist without being told explicitly:

| State | Meaning |
|---|---|
| `New` | Newly created; insert on next commit |
| `NotModified` | Loaded from store; no action required |
| `Modified` | Changed since load; update on next commit |
| `Removed` | Marked for deletion; delete on next commit |

Use the helpers on `PersistenceState` for filtering:

```csharp
_items.Where(i => i.PersistenceState.IsNotRemoved)   // active items
_items.Any(i => i.PersistenceState.IsNewOrModified)   // HasChanges check
```

## Value Objects

Value objects represent concepts with no independent identity — defined entirely by their values. Implement as `sealed record` to get structural equality and `with`-expression mutation for free. Enforce invariants in property initialisers:

```csharp
public sealed record class ItemPricing
{
    public required Contracts.UnitOfMeasure UnitOfMeasure { get; init => field = value.ThrowIfInactive(); }
    public decimal UnitPrice { get; init => field = value.ThrowIfLessThanZero(); }
    public decimal Quantity { get; init => field = value.ThrowIfLessThanZero(); }
    public decimal Total => UnitPrice * Quantity;

    // Additional validation that cannot be expressed in a single property rule.
    public ItemPricing EnsureIsValid() => DecimalRuleHelper.CheckScale(Quantity, UnitOfMeasure.Scale) ? this
        : throw new ValidationException($"Quantity decimal places exceed the unit-of-measure scale of {UnitOfMeasure.Scale}.");
}
```

Place value objects in a `ValueObjects/` sub-folder within the Domain project.

## When to Introduce the Domain Layer

Only introduce a Domain layer when the domain genuinely has:

- Aggregates with invariants that must be enforced at the model level (e.g., state-machine transitions, child-collection rules).
- Business rules that depend on the current aggregate state, not on external I/O.
- The need to protect consistency boundaries across multiple child entities.

For CRUD-oriented domains (like Products), skip the Domain layer entirely and let the Application service orchestrate directly against repository interfaces.

## Do Not

- Do not perform async I/O (repository calls, HTTP requests) inside domain classes — async work belongs in Application services or Policies.
- Do not expose child entity mutation methods as `public` — use `internal` so only the owning aggregate can drive mutations.
- Do not throw exceptions for expected business failures in domain methods — return `Result.BusinessError(...)` or `Result.NotFoundError()` and let the Application layer propagate.
- Do not reference Infrastructure, Application, or host assemblies from the Domain layer — it depends only on Contracts and CoreEx.
- Do not model value objects as classes with mutable properties — use `sealed record` with `init` setters and invariant enforcement at construction.

## Further Reading

- [`samples/docs/domain-layer.md`](../../../samples/docs/domain-layer.md) — aggregates, entities, value objects, and `PersistenceState` walkthrough.
- [`samples/docs/patterns.md`](../../../samples/docs/patterns.md) — Aggregate, Entity, and Value Object pattern entries with cross-links.
- [`samples/docs/layers.md`](../../../samples/docs/layers.md) — when to introduce the Domain layer and its position in the dependency graph.
- [`src/CoreEx.DomainDriven/README.md`](../../../src/CoreEx.DomainDriven/README.md) — `Aggregate<TId,TSelf>`, `Entity<TId,TSelf>`, and `PersistenceState`.
