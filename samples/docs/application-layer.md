# Application Layer

The Application layer is the **business logic orchestration hub**. It owns all use-case logic: input validation, state loading, mutation coordination, and event publishing. It depends only on the Contracts layer and on its own interface abstractions — it has no knowledge of EF Core, HTTP clients, or any persistence technology.

**Example projects**
- [`samples/src/Contoso.Products.Application`](../src/Contoso.Products.Application)
- [`samples/src/Contoso.Shopping.Application`](../src/Contoso.Shopping.Application)

---

## Services and interfaces

Application services implement the domain use-cases. They are decorated with `[ScopedService<TInterface>]` so that the DI container registers them automatically when the host calls `AddDynamicServicesUsing<T>()` — no manual `services.AddScoped<…>()` wiring is needed.

The pattern for a mutating operation is consistent across both domains:

1. **Guard inputs** — null/empty checks, `ThrowIfNull()`.
2. **Validate** — invoke a CoreEx `Validator<T>` and, either throw on failure or return a `Result<T>` indicating validation errors.
3. **Load current state** — retrieve via the repository interface.
4. **Mutate within a unit of work** — call `_unitOfWork.TransactionAsync(...)` which wraps the operation in a transaction and guarantees events are only dispatched on commit.
5. **Raise events** — add an `EventData` instance to `_unitOfWork.Events` inside the same transactional scope.

```csharp
// samples/src/Contoso.Products.Application/ProductService.cs
return await _unitOfWork.TransactionAsync(async () =>
{
    var dr = await _repository.CreateAsync(product).ConfigureAwait(false);
    return dr.WhereMutated(v => _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
}).ConfigureAwait(false);
```

Shopping uses `Result<T>` pipelines instead of exception-based control flow:

```csharp
// samples/src/Contoso.Shopping.Application/BasketService.cs
return _unitOfWork.TransactionAsync(async () =>
{
    var br = await _repository.CreateAsync(aggregate).ConfigureAwait(false);
    return br.ThenAs(b =>
    {
        var contract = BasketMapper.Map(b);
        _unitOfWork.Events.Add(EventData.CreateEventWith(contract, EventAction.Created));
        return contract;
    });
});
```

Each service interface lives in `Application/Interfaces/` and exposes only what consumers (for example, controllers) need, hiding implementation details behind the abstraction boundary.

> **See also**: [`IUnitOfWork`](../../src/CoreEx/Entities/IUnitOfWork.cs) · [`EventData`](../../src/CoreEx.Events/EventData.cs) · [`[ScopedService]`](../../src/CoreEx/DependencyInjection/ScopedServiceAttribute.cs)

---

## Repository interfaces

Repository interfaces (`Application/Repositories/`) define the **data contract** for the Application layer without coupling it to any persistence technology. They surface domain-idiomatic operations (`GetAsync`, `CreateAsync`, `QueryAsync`, etc.) and return CoreEx result types (`DataResult<T>`, `ItemsResult<T>`) rather than raw EF entities.

```csharp
// samples/src/Contoso.Products.Application/Repositories/IProductRepository.cs
public interface IProductRepository
{
    Task<Contracts.Product?> GetAsync(string id);
    Task<DataResult<Contracts.Product>> CreateAsync(Contracts.Product product);
    Task<ItemsResult<Contracts.ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging);
    // ...
}
```

The Infrastructure layer implements these interfaces; the Application layer never references Infrastructure assemblies directly.

> **See also**: [`DataResult<T>`](../../src/CoreEx/Entities/DataResult.cs) · [`ItemsResult<T>`](../../src/CoreEx/Entities/ItemsResult.cs)

---

## Validators

Validators live in `Application/Validators/` and extend CoreEx's `Validator<T>`. They combine two complementary styles that can be used independently or together:

**Declarative** — property rules are composed fluently in the constructor using a rich built-in rule set (`Mandatory()`, `MaximumLength()`, `IsValid()`, `PrecisionScale()`, `GreaterThanOrEqualTo()`, `Dictionary()`, `Entity()`, etc.). These run synchronously and cheaply before any I/O is attempted.

**Programmatic** — `OnValidateAsync` is overridden to add behavior that cannot be expressed declaratively: repository lookups, cross-property business rules, or dynamically-constructed validators built from retrieved data. The override pattern calls `context.ValidateFurtherAsync(...)` to compose additional property-level rules in the same structured way.

The two phases integrate naturally: `OnValidateAsync` should guard with `if (context.HasErrors) return;` to fail fast and avoid unnecessary I/O when declarative rules have already failed.

```csharp
// samples/src/Contoso.Products.Application/Validators/ProductValidator.cs
// Declarative-only — all rules expressed in the constructor.
public class ProductValidator : Validator<Contracts.Product, ProductValidator>
{
    public ProductValidator()
    {
        Property(p => p.Sku).Mandatory().MaximumLength(50);
        Property(p => p.SubCategory).Mandatory().IsValid();
        Property(p => p.Price).PrecisionScale(null, 2).GreaterThanOrEqualTo(0, _ => "zero");
    }
}
```

```csharp
// samples/src/Contoso.Products.Application/Validators/MovementRequestValidator.cs
// Declarative + programmatic — constructor rules run first, then OnValidateAsync
// performs a repository lookup and builds a second-pass validator from the result.
public class MovementRequestValidator : Validator<Contracts.MovementRequest>
{
    private static readonly Validator<MovementRequestProduct> _productValidator = Validator.Create<MovementRequestProduct>()
        .HasProperty(x => x.UnitOfMeasure, c => c.Mandatory().IsValid())
        .HasProperty(x => x.Quantity, c => c.GreaterThanOrEqualTo(0)
            .PrecisionScale(ctx => ctx.Entity.UnitOfMeasure!.Precision, ctx => ctx.Entity.UnitOfMeasure!.Scale)
            .DependsOn(x => x.UnitOfMeasure));

    private readonly IProductRepository _repository;

    public MovementRequestValidator(IProductRepository repository)
    {
        _repository = repository.ThrowIfNull();

        // Phase 1 — cheap declarative rules; run before any I/O.
        Property(x => x.Id).Mandatory().MaximumLength(50);
        Property(x => x.Products).Mandatory().Dictionary(c => c
            .WithKeyValidator("Product", k => k.Mandatory().MaximumLength(50))
            .WithValueValidator(v => v.Mandatory().Entity(_productValidator)));
    }

    protected async override Task OnValidateAsync(ValidationContext<MovementRequest> context, CancellationToken cancellationToken)
    {
        // Phase 2 — fail fast if phase 1 already found errors.
        if (context.HasErrors) return;

        // Load product data needed for business-rule validation.
        var ids = context.Value.Products!.Select(kvp => kvp.Key).ToArray();
        var products = await _repository.GetForReservationAsync(ids).ConfigureAwait(false);

        // Dynamically build a validator using the retrieved data, then run it
        // as a further structured validation pass on the same context.
        var dv = Validator.Create<MovementRequestProduct>()
            .HasProperty(x => x.UnitOfMeasure, c => c.Equal(
                ctx => products[ctx.GetDictionaryKey<string>()].UnitOfMeasureCode));

        await context.ValidateFurtherAsync(c => c
            .HasProperty(x => x.Products, c => c.Dictionary(c => c
                .WithKeyValidator("Product", k => k
                    .NotFound().WhenValue(v => !products.ContainsKey(v))
                    .Error("{0} is non-stocked and therefore cannot be transacted.").WhenValue(v => products[v].IsNonStocked)
                    .Error("{0} is not active and therefore cannot be transacted.").WhenValue(v => products[v].IsInactive))
                .WithValueValidator(dv))
            ), cancellationToken).ConfigureAwait(false);
    }
}
```

> **See also**: [`Validator<T>`](../../src/CoreEx.Validation/Validator.cs) · [`IValidatorEx`](../../src/CoreEx.Validation/IValidatorEx.cs)

---

## Adapters (anti-corruption layer)

When a domain needs to interact with another domain or external service, it defines an **adapter interface** in `Application/Adapters/`. This acts as an anti-corruption layer (ACL): the Application layer depends on a domain-idiomatic abstraction, not on the remote API's schema or transport. The concrete implementation lives in Infrastructure.

Shopping's `IProductAdapter` is a clear example — it exposes basket-centric operations (`GetAsync`, `ReserveInventoryAsync`, `CancelReservationAsync`) rather than mirroring the Products API surface:

```csharp
// samples/src/Contoso.Shopping.Application/Adapters/Products/IProductAdapter.cs
public interface IProductAdapter
{
    Task<Result<Product>> GetAsync(string id);
    Task<Result> ReserveInventoryAsync(Domain.Basket basket);
    Task<Result> CancelReservationAsync(Domain.Basket basket);
}
```

A second interface, `IProductSyncAdapter`, handles the **event-driven data replication** path — receiving published product events from the Products domain and maintaining a local read-optimised copy within Shopping's own store.

[`IProductSyncAdapter`](../src/Contoso.Shopping.Application/Adapters/Products/IProductSyncAdapter.cs)

---

## Mapping (Application-level)

Shopping places a `Mapping/` sub-folder inside the Application layer to handle the translation between the **Domain aggregate** and the **Contract**. This mapping is an Application-layer concern because it sits at the boundary between the domain model and the public surface — it is not tied to any persistence technology.

```csharp
// samples/src/Contoso.Shopping.Application/Mapping/BasketMapper.cs
public class BasketMapper : Mapper<Domain.Basket, Contracts.Basket, BasketMapper>
{
    protected override Contracts.Basket OnMap(Domain.Basket source) => new()
    {
        Id = source.Id,
        StatusCode = source.Status,
        Pricing = new BasketPricing { Total = source.Total, ... },
        Items = [.. source.Items.Select(i => BasketItemMapper.Map(i))]
    };
}
```

(see [Infrastructure → Mapping](infrastructure-layer.md#mapping)).

> **See also**: [`Mapper<TSource, TDest, TSelf>`](../../src/CoreEx/Mapping/Mapper.cs)

---

## Policies

Policies (`Application/Policies/`) encapsulate **domain-level guard logic** that sits above a single validator but below a full service. They are used to enforce invariants that span adapter or repository calls — for example, confirming that a referenced entity actually exists before proceeding with a mutation.

```csharp
// samples/src/Contoso.Shopping.Application/Policies/ProductPolicy.cs
public class ProductPolicy(IProductAdapter productAdapter)
{
    public Task<Result<Product>> EnsureExistsAsync(string productId) => Result
        .GoAsync(() => _productAdapter.GetAsync(productId))
        .OnFailure(r => r.IsNotFoundError
            ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(productId), "Product was not found."))
            : r);
}
```
