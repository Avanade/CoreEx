---
applyTo: "**/Application/**/*.cs"
description: "Application service conventions: ScopedService registration, dependency injection, validation, unit of work, CQRS, policies, adapters, and Result<T> pipelines"
tags: ["services", "application-layer", "dependency-injection", "validation", "unit-of-work", "cqrs", "policies", "adapters"]
---

# Application Service Conventions

## NuGet / Project References

| Package | Key types provided |
|---|---|
| `CoreEx` | `[ScopedService<T>]`, `Runtime`, `NotFoundException`, `BusinessException`, `ValidationException`, `.ThrowIfNull()`, `.ThrowIfNullOrEmpty()`, `QueryArgs`, `PagingArgs`, `ItemsResult<T>`, `Result<T>`, `Result.GoAsync()`, `.ThenAs()`, `.ThenAsAsync()` |
| `CoreEx.Data` | `IUnitOfWork`, `DataResult<T>` |
| `CoreEx.Events` | `EventData`, `EventAction` |
| `CoreEx.Validation` | `Validator<T, TSelf>`, `Validator<T>`, `.ValidateAndThrowAsync()`, `.ValidateWithResultAsync()` |
| `CoreEx.RefData` | `ReferenceDataOrchestrator` |

## Structure

- Define a public interface (e.g., `IProductService`) in the Application project, typically under an `Interfaces/` sub-folder — not a hard requirement, but a clean convention that keeps the public surface of the Application layer easy to navigate.
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

`.ThrowIfNull()` and `.ThrowIfNullOrEmpty()` **return the guarded value** when the check passes, so they can be used inline at the point of first use rather than as separate pre-checks. This keeps code tight without sacrificing safety:

```csharp
// Constructor injection — the assignment is the first use; guard inline
private readonly IProductRepository _repository = repository.ThrowIfNull();

// Inline at point of first use in a method body
var current = await _repository.GetAsync(product.Id.ThrowIfNullOrEmpty()).ConfigureAwait(false);

// Guards chain — each returns the value if it passes, so further checks can follow
public BasketStatus Status { get; private set => field = value.ThrowIfNull().ThrowIfInactive(); }
```

Use a top-of-method pre-check (non-inline) only when the value is not immediately consumed:

```csharp
public async Task<Product> UpdateAsync(Product product)
{
    product.ThrowIfNull(); // checked here; not passed anywhere yet
    await ProductValidator.Default.ValidateAndThrowAsync(product).ConfigureAwait(false);
    var current = await _repository.GetAsync(product.Id.ThrowIfNullOrEmpty()).ConfigureAwait(false);
    // ...
}
```

## Validation

Validators live in `Application/Validators/` and are **not registered in DI** — they are not injected into services (see [DI Registration Principle](#di-registration-principle) below). Choose the base class based on whether the validator needs injected dependencies:

**`Validator<T, TSelf>`** — use when no constructor injection is required. Exposes a static `Default` singleton; always call via the singleton:

```csharp
public class ProductValidator : Validator<Contracts.Product, ProductValidator>
{
    public ProductValidator()
    {
        Property(p => p.Sku).Mandatory().MaximumLength(50);
        Property(p => p.SubCategoryCode).Mandatory().IsValid();
        Property(p => p.Price).PrecisionScale(null, 2).GreaterThanOrEqualTo(0, _ => "zero");
    }
}

// Call via Default singleton — never use new ProductValidator() at the call site:
await ProductValidator.Default.ValidateAndThrowAsync(product);
```

**`Validator<T>`** — use when constructor injection is required (e.g., a repository for async I/O). No singleton; instantiate directly at the call site using dependencies already in scope in the service:

```csharp
public class MovementRequestValidator : Validator<MovementRequest>
{
    private readonly IProductRepository _repository;

    public MovementRequestValidator(IProductRepository repository)
    {
        _repository = repository.ThrowIfNull();
        Property(x => x.Id).Mandatory().MaximumLength(50);
        // ... declarative rules
    }

    protected async override Task OnValidateAsync(
        ValidationContext<MovementRequest> context, CancellationToken cancellationToken)
    {
        if (context.HasErrors) return; // fail fast — skip I/O if declarative phase found errors

        var ids = context.Value.Products!.Select(kvp => kvp.Key).ToArray();
        var products = await _repository.GetForReservationAsync(ids).ConfigureAwait(false);

        await context.ValidateFurtherAsync(c => c
            .HasProperty(x => x.Products, c => c.Dictionary(c => c
                .WithKeyValidator("Product", k => k
                    .NotFound().WhenValue(v => !products.ContainsKey(v))))),
            cancellationToken).ConfigureAwait(false);
    }
}

// Instantiate directly — _repository is already injected into the service:
await new MovementRequestValidator(_repository).ValidateAndThrowAsync(request);
```

Both phases apply to both base classes. For `Result<T>` pipelines, use `ValidateWithResultAsync` instead of `ValidateAndThrowAsync`:

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

`BusinessException` (and all CoreEx exceptions that extend `ExtendedException`) support optional fluent extension methods that enrich the error with machine-readable context. All methods return the exception so they can be chained directly on the `throw` expression:

| Method | Purpose |
|---|---|
| `.WithErrorCode(string)` | Adds a machine-readable code the caller can key on (e.g. `"product-not-inactive"`) |
| `.WithKey(object)` | Attaches the entity key — surfaces in the problem-details response under `key` |
| `.WithDetail(string)` | Adds extended human-readable detail beyond the main message |
| `.WithStatusCode(HttpStatusCode)` | Overrides the default HTTP status code (use sparingly) |
| `.WithExtension(string, object)` | Adds arbitrary key/value metadata to `extensions` in the problem-details response |
| `.AsTransient(TimeSpan?)` | Marks the error as transient so retry infrastructure knows it is safe to retry |

```csharp
// Minimal — message only
if (!product.IsInactive)
    throw new BusinessException("A product must first be deactivated before it can be deleted.");

// With machine-readable error code and entity key
if (!product.IsInactive)
    throw new BusinessException("A product must first be deactivated before it can be deleted.")
        .WithErrorCode("product-not-inactive")
        .WithKey(product.Id);

// With additional detail
if (basket.HasExpiredItems)
    throw new BusinessException("Basket cannot be checked out.")
        .WithErrorCode("basket-has-expired-items")
        .WithDetail("One or more items in the basket have expired and must be removed before checkout.");
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

## Result&lt;T&gt; Pipeline Style

Using `Result<T>` chains is a developer choice — it is not restricted to DDD aggregate services. It can be applied to any service method where explicit, composable failure propagation is preferred over exceptions. Compose with `Result.GoAsync`, `.ThenAs`, `.ThenAsAsync`. The unit of work is still `TransactionAsync`:

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

The interface lives in `Interfaces/` alongside the write service interface (e.g., `IProductReadService.cs` next to `IProductService.cs`). The implementation lives in the same folder as the write service implementation.

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

When a service needs to call another domain's API, inject an adapter interface (e.g., `IProductAdapter`) rather than calling `HttpClient` directly. Implement the adapter in the Infrastructure layer using a typed HTTP client. The interface surface should be domain-idiomatic — not a mirror of the remote API.

Adapter interfaces live in `Application/Adapters/` (one interface per external domain). The Infrastructure implementation lives in `Infrastructure/Adapters/`.

```csharp
// Application/Adapters/IProductAdapter.cs — interface only (domain-idiomatic, not a mirror of the remote API)
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

A second adapter interface (`IXxxSyncAdapter`) handles **event-driven data replication** — receiving published events from another domain and maintaining a local eventually-consistent copy in the consuming domain's own store.

## Policies

Policies (`Application/Policies/`) encapsulate **domain-level guard logic** that requires I/O (adapter or repository calls). They provide a named, independently testable home for rules that depend on external state and cannot be expressed in a validator alone (synchronous) or enforced directly in the domain model (no async I/O). A policy can be called from any point in service orchestration where the condition needs to be verified.

Policies are **not registered in DI** — they are instantiated directly at the call site using dependencies already injected into the calling service (see [DI Registration Principle](#di-registration-principle) below).

Policies return `Result` or `Result<T>` and compose naturally into `Result<T>` pipelines via `.GoAsync()` / `.ThenAsAsync()`:

```csharp
// Application/Policies/ProductPolicy.cs
public class ProductPolicy(IProductAdapter productAdapter)
{
    private readonly IProductAdapter _productAdapter = productAdapter.ThrowIfNull();

    public Task<Result<Product>> EnsureExistsAsync(string productId) => Result
        .GoAsync(() => _productAdapter.GetAsync(productId))
        .OnFailure(r => r.IsNotFoundError
            ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(productId), "Product was not found."))
            : r);
}

// In the calling service — _productAdapter is already injected into the service:
var result = await new ProductPolicy(_productAdapter).EnsureExistsAsync(productId);
```

## Application-Level Mapping

When a domain has a Domain layer, an `Application/Mapping/` sub-folder holds mappers that translate between the **Domain aggregate** and the **Contract**. This mapping is an Application-layer concern because it sits at the public surface boundary — it is not tied to any persistence technology.

Use `Mapper<TSource, TDest, TSelf>` (uni-directional). Mappers are **not registered in DI** — call them via the static `Map()` method directly at the point of use (see [DI Registration Principle](#di-registration-principle) below):

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

// Call via static Map() — no injection, no new():
var contract = BasketMapper.Map(aggregate);
```

Infrastructure-level mapping (Contract ↔ Persistence model) uses `BiDirectionMapper` and lives in `Infrastructure/Mapping/`. Do not conflate the two layers.

## DI Registration Principle

Only register a type in DI when there is a current, concrete intent to mock or replace it. Applying YAGNI, the following Application-layer types are **not** DI-registered — they are called or instantiated directly at the point of use:

| Type | How to use |
|---|---|
| `Validator<T, TSelf>` | Call via static `Default` singleton: `MyValidator.Default.ValidateAndThrowAsync(...)` |
| `Validator<T>` | Instantiate directly with already-injected deps: `new MyValidator(_repo).ValidateAndThrowAsync(...)` |
| `Mapper<TSource, TDest, TSelf>` | Call via static `Map()` method: `MyMapper.Map(source)` |
| Policy classes | Instantiate directly with already-injected deps: `new MyPolicy(_adapter).EnsureExistsAsync(...)` |

Keeping these out of DI avoids bloating service constructors with dependencies that are not realistic substitution points, and defers that complexity until there is a real need for it.

## ConfigureAwait

Always call `.ConfigureAwait(false)` on every `await` inside service and repository methods.

## Do Not

- Do not publish events outside of `_unitOfWork.TransactionAsync(...)` — events must be committed atomically with the database write.
- Do not call `HttpClient` directly from services — always go through an adapter interface.
- Do not reference Infrastructure assemblies from the Application layer — all persistence and transport concerns are reached through interfaces.
- Do not implement rules in `OnValidateAsync` that require I/O without first guarding with `if (context.HasErrors) return;`.
- Do not add business logic to controllers — services own all use-case orchestration.
- Do not register Validators, Mappers, or Policies in DI or inject them into service constructors — call or instantiate them directly at the point of use (YAGNI: refactor to DI only when there is a real need to mock or replace them).
- Do not use `new ProductValidator()` at the call site when `Validator<T, TSelf>` provides a `Default` singleton — use `ProductValidator.Default`.

## Further Reading

- [Application Layer Guide](https://github.com/Avanade/CoreEx/blob/main/samples/docs/application-layer.md) — full walkthrough of services, validators, adapters, policies, mapping, and the unit-of-work pattern.
- [Pattern Catalog](https://github.com/Avanade/CoreEx/blob/main/samples/docs/patterns.md) — CQRS, Service, Unit of Work, Validator, Policy, Adapter, and Event patterns with cross-links.
- [Layer Dependencies](https://github.com/Avanade/CoreEx/blob/main/samples/docs/layers.md) — layer dependency rules: Application depends inward only on Contracts and its own interfaces.
- [CoreEx.Validation README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Validation/README.md) — `Validator<T>`, rule set, `OnValidateAsync`, and `ValidateFurtherAsync`.
- [CoreEx README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/README.md) — `IUnitOfWork`, `Result<T>`, `[ScopedService]`, and CoreEx exception types.
- [CoreEx Results README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/Results/README.md) — `Result<T>` type, pipeline operators (`.GoAsync`, `.ThenAs`, `.ThenAsAsync`), and error propagation semantics.
