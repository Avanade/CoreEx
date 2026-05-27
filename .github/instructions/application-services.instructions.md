---
applyTo: "**/Application/**/*.cs"
description: "Application service conventions: ScopedService registration, dependency injection, validation, unit of work, CQRS, policies, adapters, and Result<T> pipelines"
tags: ["services", "application-layer", "dependency-injection", "validation", "unit-of-work", "cqrs", "policies", "adapters"]
---

# Application Service Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx` | `[ScopedService<T>]`, `IUnitOfWork`, `Runtime`, `NotFoundException`, `BusinessException`, `ValidationException`, `.ThrowIfNull()`, `.ThrowIfNullOrEmpty()` |
| `CoreEx.Data` | `DataResult<T>`, `ItemsResult<T>`, `QueryArgs`, `PagingArgs` |
| `CoreEx.Events` | `EventData`, `EventAction` |
| `CoreEx.Validation` | `Validator<T, TSelf>`, `.ValidateAndThrowAsync()`, `.ValidateWithResultAsync()` |
| `CoreEx.Results` | `Result<T>`, `Result.GoAsync()`, `.ThenAs()`, `.ThenAsAsync()` |
| `CoreEx.RefData` | `ReferenceDataOrchestrator` |

## Structure

- Define a public interface (e.g., `IProductService`) in the Application project.
- Implement with `[ScopedService<IInterface>]` attribute so it registers itself via dynamic DI — no manual registration required.
- Inject dependencies via primary constructor and guard every injected parameter with `.ThrowIfNull()`.

```csharp
[ScopedService<IProductService>]
public class ProductService(IUnitOfWork unitOfWork, IProductRepository repository) : IProductService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork.ThrowIfNull();
    private readonly IProductRepository _repository = repository.ThrowIfNull();
}
```

## Guard Clauses

Use CoreEx null/empty guards at the top of each method before any logic:

```csharp
public async Task<Product> UpdateAsync(Product product)
{
    product.ThrowIfNull();
    product.Id.ThrowIfNullOrEmpty();
    // ...
}
```

## Validation

Validators live in `Application/Validators/` and extend `Validator<T, TSelf>`. They combine two phases.

**Declarative phase** — property rules composed fluently in the constructor using the built-in rule set (`Mandatory()`, `MaximumLength()`, `IsValid()`, `PrecisionScale()`, `GreaterThanOrEqualTo()`, `Dictionary()`, `Entity()`, etc.). Run synchronously before any I/O.

**Programmatic phase** — `OnValidateAsync` override for rules that require I/O (repository lookups, cross-field checks, dynamically-constructed validators). Always guard with `if (context.HasErrors) return;` to fail fast when declarative rules have already failed.

```csharp
// Declarative-only validator.
public class ProductValidator : Validator<Contracts.Product, ProductValidator>
{
    public ProductValidator()
    {
        Property(p => p.Sku).Mandatory().MaximumLength(50);
        Property(p => p.SubCategoryCode).Mandatory().IsValid();
        Property(p => p.Price).PrecisionScale(null, 2).GreaterThanOrEqualTo(0, _ => "zero");
    }
}
```

```csharp
// Declarative + programmatic validator with I/O.
protected async override Task OnValidateAsync(
    ValidationContext<MovementRequest> context, CancellationToken cancellationToken)
{
    if (context.HasErrors) return; // fail fast — skip I/O if phase 1 already found errors

    var ids = context.Value.Products!.Select(kvp => kvp.Key).ToArray();
    var products = await _repository.GetForReservationAsync(ids).ConfigureAwait(false);

    await context.ValidateFurtherAsync(c => c
        .HasProperty(x => x.Products, c => c.Dictionary(c => c
            .WithKeyValidator("Product", k => k
                .NotFound().WhenValue(v => !products.ContainsKey(v))))),
        cancellationToken).ConfigureAwait(false);
}
```

Call the validator in the service before any persistence operations. Throw on the first error set (exception-based services):

```csharp
await ProductValidator.Default.ValidateAndThrowAsync(product);
```

For `Result<T>` pipelines, use `ValidateWithResultAsync`:

```csharp
var result = await Result.GoAsync(() => MyValidator.Default.ValidateWithResultAsync(value));
if (result.IsFailure) return result.AsResult();
```

## Not Found Handling

After loading an entity, throw immediately if it does not exist:

```csharp
var current = await _repository.GetAsync(id).ConfigureAwait(false);
NotFoundException.ThrowIfDefault(current);
```

## Business Rule Exceptions

Use `BusinessException` for domain rule violations that are the caller's fault but are not validation errors:

```csharp
if (!product.IsInactive)
    throw new BusinessException("A product must first be deactivated before it can be deleted.");
```

## Unit of Work and Events

Wrap all side-effectful database operations in `_unitOfWork.TransactionAsync(...)`. Both the database write and the outbox event publication are committed atomically inside this scope — events are only dispatched if the transaction commits successfully.

```csharp
return await _unitOfWork.TransactionAsync(async () =>
{
    var dr = await _repository.CreateAsync(product).ConfigureAwait(false);
    return dr.WhereMutated(v =>
        _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
}).ConfigureAwait(false);
```

- `WhereMutated(action)` — executes `action` only when the data result records a mutation; add the event inside this callback.
- `EventData.CreateEventWith(value, action)` — creates a typed event from the entity.
- `EventAction.Created`, `EventAction.Updated`, `EventAction.Deleted` — use the standard constants.

For delete where the entity value is no longer available, carry the ID via `.WithKey(id)`:

```csharp
_unitOfWork.Events.Add(
    EventData.CreateEventWith<Product>(default, EventAction.Deleted).WithKey(id));
```

## Result<T> Style (Domain-Aggregate Services)

For services operating on DDD aggregates (e.g., Shopping Basket), use `Result<T>` chains instead of exceptions for expected failures. Compose with `Result.GoAsync`, `.ThenAs`, `.ThenAsAsync`. The unit of work is still `TransactionAsync`:

```csharp
public Task<Result<Basket>> CreateAsync(string customerId)
{
    var aggregate = Domain.Basket.CreateNew(customerId.ThrowIfNullOrEmpty());

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
}
```

For multi-step orchestration with early exit on the first failure:

```csharp
var pr = await Result.GoAsync(() => SomeValidator.Default.ValidateWithResultAsync(input))
    .ThenAsAsync(v => _someAdapter.EnsureExistsAsync(v.Id!));

if (pr.IsFailure)
    return pr.AsResult();
```

## CQRS — Read Services

Split read operations into a separate service with an `IXxxReadService` interface. This is the surface expression of CQRS: the write model (mutations + events) and the read model (queries returning purpose-built shapes) are designed and scaled independently.

```csharp
[ScopedService<IProductReadService>]
public class ProductReadService(IProductRepository repository) : IProductReadService
{
    private readonly IProductRepository _repository = repository.ThrowIfNull();

    public Task<Product?> GetAsync(string id) => _repository.GetAsync(id);
    public Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging)
        => _repository.QueryAsync(query, paging);
}
```

## Anti-Corruption Layer (Adapters)

When a service needs to call another domain's API, inject an adapter interface (e.g., `IProductAdapter`) rather than calling `HttpClient` directly. Implement the adapter in the Infrastructure layer using a typed HTTP client. The interface surface should be domain-idiomatic — not a mirror of the remote API:

```csharp
// Application layer — interface only (domain-idiomatic, not a mirror of the remote API)
public interface IProductAdapter
{
    Task<Result<Product>> GetAsync(string id);
    Task<Result> ReserveInventoryAsync(Domain.Basket basket);
    Task<Result> CancelReservationAsync(Domain.Basket basket);
}

// Infrastructure layer — implementation
[ScopedService<IProductAdapter>]
public class ProductAdapter(ProductsHttpClient httpClient) : IProductAdapter { ... }
```

A second adapter interface (`IProductSyncAdapter`) handles **event-driven data replication** — receiving published events from another domain and maintaining a local eventually-consistent copy in the consuming domain's own store.

## Policies

Policies (`Application/Policies/`) encapsulate **domain-level guard logic** that requires I/O (adapter or repository calls). They provide a named, independently testable home for rules that depend on external state and cannot be expressed in a validator alone (synchronous) or enforced directly in the domain model (no async I/O). A policy can be called from any point in service orchestration where the condition needs to be verified — for example, confirming a referenced entity exists before allowing a mutation.

Policies return `Result` or `Result<T>` and compose naturally into `Result<T>` pipelines via `.GoAsync()` / `.ThenAsAsync()`:

```csharp
// Application/Policies/ProductPolicy.cs
public class ProductPolicy(IProductAdapter productAdapter)
{
    public Task<Result<Product>> EnsureExistsAsync(string productId) => Result
        .GoAsync(() => _productAdapter.GetAsync(productId))
        .OnFailure(r => r.IsNotFoundError
            ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(productId), "Product was not found."))
            : r);
}
```

## Application-Level Mapping

When a domain has a Domain layer (e.g., Shopping), an `Application/Mapping/` sub-folder holds mappers that translate between the **Domain aggregate** and the **Contract**. This mapping is an Application-layer concern because it sits at the public surface boundary — it is not tied to any persistence technology.

Use `Mapper<TSource, TDest, TSelf>` (uni-directional):

```csharp
// Application/Mapping/BasketMapper.cs
public class BasketMapper : Mapper<Domain.Basket, Contracts.Basket, BasketMapper>
{
    protected override Contracts.Basket OnMap(Domain.Basket source) => new()
    {
        Id = source.Id,
        StatusCode = source.Status,
        Items = [.. source.Items.Select(i => BasketItemMapper.Map(i))]
    };
}
```

Infrastructure-level mapping (Contract ↔ Persistence model) uses `BiDirectionMapper` and lives in `Infrastructure/Mapping/`. Do not conflate the two layers.

## ConfigureAwait

Always call `.ConfigureAwait(false)` on every `await` inside service and repository methods.

## Do Not

- Do not call `_unitOfWork.ExecuteAsync(...)` — the correct method is `_unitOfWork.TransactionAsync(...)`.
- Do not publish events outside of `_unitOfWork.TransactionAsync(...)` — events must be committed atomically with the database write.
- Do not call `HttpClient` directly from services — always go through an adapter interface.
- Do not reference Infrastructure assemblies from the Application layer — all persistence and transport concerns are reached through interfaces.
- Do not implement rules in `OnValidateAsync` that require I/O without first guarding with `if (context.HasErrors) return;`.
- Do not add business logic to controllers — services own all use-case orchestration.

## Further Reading

- [`samples/docs/application-layer.md`](../../../samples/docs/application-layer.md) — full walkthrough of services, validators, adapters, policies, mapping, and the unit-of-work pattern.
- [`samples/docs/patterns.md`](../../../samples/docs/patterns.md) — pattern catalog: CQRS, Service, Unit of Work, Validator, Policy, Adapter, and Event patterns with cross-links.
- [`samples/docs/layers.md`](../../../samples/docs/layers.md) — layer dependency rules: Application depends inward only on Contracts and its own interfaces.
- [`src/CoreEx.Validation/README.md`](../../../src/CoreEx.Validation/README.md) — `Validator<T>`, rule set, `OnValidateAsync`, and `ValidateFurtherAsync`.
- [`src/CoreEx/README.md`](../../../src/CoreEx/README.md) — `IUnitOfWork`, `Result<T>`, `[ScopedService]`, and CoreEx exception types.
