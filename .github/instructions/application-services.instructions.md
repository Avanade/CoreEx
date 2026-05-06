---
applyTo: "**/Application/**/*.cs"
description: "Application service conventions: ScopedService registration, dependency injection, validation, unit of work patterns, and business logic structure"
tags: ["services", "application-layer", "dependency-injection", "validation", "unit-of-work"]
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

Call the validator before any persistence operations. Throw on first error set:

```csharp
await ProductValidator.Default.ValidateAndThrowAsync(product);
```

For `Result<T>` style, use `ValidateWithResultAsync` and propagate with `ThenAs`:

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

Wrap all side-effectful database operations in `_unitOfWork.ExecuteAsync(...)`. Add integration events inside that scope so event and data writes are atomic:

```csharp
return await _unitOfWork.ExecuteAsync(async () =>
{
    var dr = await _repository.CreateAsync(product).ConfigureAwait(false);
    return dr.WhereMutated(v =>
        _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
}).ConfigureAwait(false);
```

- `WhereMutated(action)` — executes `action` only when the data result has a mutation; add the event inside this callback.
- `EventData.CreateEventWith(value, action)` — creates a typed event from the entity.
- `EventAction.Created`, `EventAction.Updated`, `EventAction.Deleted` — use the standard constants.

For delete where the entity value is gone, carry the ID via `.WithKey(id)`:

```csharp
_unitOfWork.Events.Add(
    EventData.CreateEventWith<Product>(default, EventAction.Deleted).WithKey(id));
```

## Result<T> Style (Domain-Aggregate Services)

For services operating on DDD aggregates (e.g., Shopping Basket), use `Result<T>` chains instead of exceptions for expected failures. Compose with `Result.GoAsync`, `.ThenAs`, `.ThenAsAsync`:

```csharp
public Task<Result<Basket>> CreateAsync(string customerId)
{
    var aggregate = Domain.Basket.CreateNew(customerId.ThrowIfNullOrEmpty());

    return _unitOfWork.ExecuteAsync(async () =>
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

For multi-step orchestration with early exit:

```csharp
var pr = await Result.GoAsync(() => SomeValidator.Default.ValidateWithResultAsync(input))
    .ThenAsAsync(v => _someAdapter.EnsureExistsAsync(v.Id!));

if (pr.IsFailure)
    return pr.AsResult();
```

## Read Services

Split read operations into a separate service with an `IXxxReadService` interface when the project follows CQRS. Read services do not use UnitOfWork and do not publish events:

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

When a service needs to call another domain's API, inject an adapter interface (e.g., `IProductAdapter`) rather than calling `HttpClient` directly. Implement the adapter in the Infrastructure layer using `ProductsHttpClient`:

```csharp
// Application layer — interface only
public interface IProductAdapter
{
    Task<Product?> GetAsync(string id);
    Task<MovementCollection> ReserveInventoryAsync(MovementRequest request);
}

// Infrastructure layer — implementation
[ScopedService<IProductAdapter>]
public class ProductAdapter(ProductsHttpClient httpClient) : IProductAdapter { ... }
```

## ConfigureAwait

Always call `.ConfigureAwait(false)` on every `await` inside service and repository methods.
