---
applyTo: "**/Application/**/*.cs"
description: "Application service conventions: ScopedService registration, dependency injection, validation, unit of work, CQRS, policies, adapters, and Result<T> pipelines"
tags: ["services", "application-layer", "dependency-injection", "validation", "unit-of-work", "cqrs", "policies", "adapters"]
---

# Application Service Conventions

> **Related skill:** to scaffold a new application service, invoke the [`coreex-app-service`](/.github/skills/coreex-app-service/SKILL.md) skill;
> for the validator it calls invoke [`coreex-validator`](/.github/skills/coreex-validator/SKILL.md), and for a domain-guard policy invoke [`coreex-policy`](/.github/skills/coreex-policy/SKILL.md).
> This file holds the invariants that must hold on **any** edit to an Application-layer file; the skills drive the
> step-by-step **creation** procedures.

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

> **Do not inject validators (or mappers/policies) into the service constructor.** A `Validator<T, TSelf>` is invoked via its static `Default` singleton, so it is never a constructor parameter.
>
> ```csharp
> // ❌ Wrong — validator injected
> public class EmployeeService(IUnitOfWork unitOfWork, IEmployeeRepository repository, EmployeeValidator validator) : IEmployeeService
>
> // ✅ Correct — only the unit of work and repository (validator called via EmployeeValidator.Default)
> public class EmployeeService(IUnitOfWork unitOfWork, IEmployeeRepository repository) : IEmployeeService
> ```

## Service Operations — Confirm Scope

Before generating a service, confirm which operations it should expose — never silently assume the full set.

> **Agent instruction:**
> - When asked to create a service **without** specifying the operations, confirm the standard CRUD set with the user — **Get**, **Create**, **Update**, **Delete** — presenting each as **default-selected** so the user can deselect any that are not wanted.
> - Even when the user asks for "CRUD" (or "the usual"), still confirm the four operations (Get / Create / Update / Delete, each default-selected) rather than assuming all four — they may want only a subset.
> - **Never add a Query (collection/search) operation unless it is explicitly requested.** Querying needs deliberate, additional design — `QueryArgsConfig` filter/order fields, paging, and a purpose-built read shape (see [CQRS — Read Services](#cqrs--read-services)) — so it must not be inferred from a generic "create a service"/"CRUD" request. If querying is asked for, gather those specifics first.

## Guard Clauses

`.ThrowIfNull()` and `.ThrowIfNullOrEmpty()` **return the guarded value** when the check passes, so they can be used inline at the point of first use rather than as separate pre-checks. This keeps code tight without sacrificing safety:

```csharp
// Constructor injection — the assignment is the first use; guard inline
private readonly IProductRepository _repository = repository.ThrowIfNull();

// Inline at point of first use in a method body
var current = await _repository.GetAsync(product.Id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);

// Guards chain — each returns the value if it passes, so further checks can follow
public BasketStatus Status { get; private set => field = value.ThrowIfNull().ThrowIfInactive(); }
```

Use a top-of-method pre-check (non-inline) only when the value is not immediately consumed:

```csharp
public async Task<Product> UpdateAsync(Product product, CancellationToken cancellationToken = default)
{
    product.ThrowIfNull(); // checked here; not passed anywhere yet
    await ProductValidator.Default.ValidateAndThrowAsync(product, cancellationToken).ConfigureAwait(false);
    var current = await _repository.GetAsync(product.Id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);
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
        Property(p => p.SubCategory).Mandatory().IsValid();   // typed ref-data nav property, not SubCategoryCode
        Property(p => p.Price).PrecisionScale(null, 2).GreaterThanOrEqualTo(0, _ => "zero");
    }
}

// Call via Default singleton — never use new ProductValidator() at the call site:
await ProductValidator.Default.ValidateAndThrowAsync(product, cancellationToken);
```

**Choose the invocation that matches the flow — never bare `ValidateAsync`:**

| Flow | Method | Behaviour |
|---|---|---|
| Exception style (non-ROP) | `ValidateAndThrowAsync(value)` | Throws `ValidationException` on failure — stops execution |
| `Result<T>` pipeline (ROP) | `ValidateWithResultAsync(value)` | Returns a `Result` to compose / short-circuit on |

`ValidateAsync(value)` merely **returns the validation result without throwing** — calling it and ignoring the return *swallows the errors and continues*. Do **not** use it for fail-fast validation. In a non-ROP service use `ValidateAndThrowAsync`; in a `Result<T>` pipeline use `ValidateWithResultAsync`.

```csharp
// ❌ Wrong — does not throw; the failure is swallowed and execution continues
await EmployeeValidator.Default.ValidateAsync(employee, cancellationToken).ConfigureAwait(false);

// ✅ Correct (non-ROP) — throws ValidationException on failure
await EmployeeValidator.Default.ValidateAndThrowAsync(employee, cancellationToken).ConfigureAwait(false);
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

    protected async override Task OnValidateAsync(ValidationContext<MovementRequest> context, CancellationToken cancellationToken)
    {
        if (context.HasErrors) return; // fail fast — skip I/O if declarative phase found errors

        var ids = context.Value.Products!.Select(kvp => kvp.Key).ToArray();
        var products = await _repository.GetForReservationAsync(ids, cancellationToken).ConfigureAwait(false);

        await context.ValidateFurtherAsync(c => c
            .HasProperty(x => x.Products, c => c.Dictionary(c => c
                .WithKeyValidator("Product", k => k
                    .NotFound().WhenValue(v => !products.ContainsKey(v))))),
            cancellationToken).ConfigureAwait(false);
    }
}

// Instantiate directly — _repository is already injected into the service:
await new MovementRequestValidator(_repository).ValidateAndThrowAsync(request, cancellationToken);
```

Both phases apply to both base classes. For `Result<T>` pipelines, use `ValidateWithResultAsync` instead of `ValidateAndThrowAsync`:

```csharp
var result = await Result.GoAsync(() => MyValidator.Default.ValidateWithResultAsync(value, cancellationToken));
if (result.IsFailure) return result.AsResult();
```

## Not Found Handling

After loading an entity, throw immediately if it does not exist:

```csharp
var current = await _repository.GetAsync(id, cancellationToken).ConfigureAwait(false);
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

## Assigning the identifier on Create

The **service** assigns the new identifier on `Create` — the database does **not** generate it (the migration scaffold's value-generation default is dropped unless a key is explicitly DB-generated; see the tooling guidance). Set it from the ambient `Runtime` **after validation, before the transaction**, alongside any other server-controlled fields:

```csharp
public async Task<Product> CreateAsync(Product product, CancellationToken cancellationToken = default)
{
    product.ThrowIfNull();
    await ProductValidator.Default.ValidateAndThrowAsync(product, cancellationToken).ConfigureAwait(false);

    product.Id = Runtime.NewId();           // service-assigned identity — never left to the caller or the DB
    product.CategoryCode = product.SubCategory!.CategoryCode;   // derive any other server-set fields here
    product.IsInactive = true;

    return await _unitOfWork.TransactionAsync(async ct =>
    {
        var dr = await _repository.CreateAsync(product, ct).ConfigureAwait(false);
        return dr.WhereMutated(v =>
            _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
    }, cancellationToken).ConfigureAwait(false);
}
```

- **Match the generator to the identifier type:** `string` key → `Runtime.NewId()`; `Guid` key → `Runtime.NewGuid()`. Never use `Guid.NewGuid()` / `Guid.NewGuid().ToString()` directly — always go through `Runtime` so the clock/GUID source stays test-controllable.
- **Always assign on Create** so the value is deterministic and present before the event is published — don't rely on the caller-supplied `Id` and don't defer to the database.
- **Exception — DB-generated keys only:** if a key is explicitly an identity/sequence column (the rare, explicitly-requested case), the database assigns it; the service does **not** set `Id` and reads it back from the create result instead.

## Unit of Work and Events

Wrap all side-effectful database operations in `_unitOfWork.TransactionAsync(...)`. Both the database write and the outbox event publication are committed atomically inside this scope — events are only dispatched if the transaction commits successfully.

```csharp
return await _unitOfWork.TransactionAsync(async ct =>
{
    var dr = await _repository.CreateAsync(product, ct).ConfigureAwait(false);
    return dr.WhereMutated(v =>
        _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
}, cancellationToken).ConfigureAwait(false);
```

**When eventing is enabled, every mutating operation must publish its event inside the transaction.** `Create`, `Update`, and `Delete` each add their event (`EventAction.Created` / `Updated` / `Deleted`) via `_unitOfWork.Events.Add(...)` within the `TransactionAsync` scope — a transaction that writes but adds no event is a bug. (Eventing is enabled when the solution was scaffolded with it, or whenever the domain publishes domain events; if a domain is genuinely event-free, omit it.)

```csharp
// ❌ Wrong — writes inside the transaction but never adds the event
return await _unitOfWork.TransactionAsync(async ct =>
{
    var result = await _repository.CreateAsync(employee, ct).ConfigureAwait(false);
    return result.Value!;
}, cancellationToken).ConfigureAwait(false);

// ✅ Correct — event added within the same transaction, only on mutation
return await _unitOfWork.TransactionAsync(async ct =>
{
    var dr = await _repository.CreateAsync(employee, ct).ConfigureAwait(false);
    return dr.WhereMutated(v =>
        _unitOfWork.Events.Add(EventData.CreateEventWith(v, EventAction.Created)));
}, cancellationToken).ConfigureAwait(false);
```

- `WhereMutated(action)` — executes `action` only when the data result records a mutation; add the event inside this callback. **Mind the overload:** `DataResult<T>` (from `Create`/`Update`) carries the value, so use `WhereMutated(v => ...)`; `DataResult` (from `Delete`) has **no value**, so use the parameterless `WhereMutated(() => ...)` — not `WhereMutated(_ => ...)`.
- `EventData.CreateEventWith(value, action)` — a typed event **carrying the entity value** (Create/Update only — pass the **real** mutated value `v`). For a **no-value** event (Delete), use `EventData.CreateEvent<T>(action).WithKey(id)` — the type + action + key, **no value**. **Never fabricate a value to feed `CreateEventWith` on a delete** (e.g. `CreateEventWith(new Employee { Id = id }, …)` or `CreateEventWith<T>(default, …)`): a delete event must have **no body** — a synthetic entity wrongly serialises a near-empty value, adds a version suffix that delete events must not have, and buries the id in the body instead of the metadata key. The id belongs in `.WithKey(id)`.
- `EventAction.Created`, `EventAction.Updated`, `EventAction.Deleted` — use the standard constants.

For delete the `DataResult` has no value, so use the parameterless `WhereMutated(() => ...)` and carry the identity via `.WithKey(id)`:

```csharp
public async Task DeleteAsync(string id, CancellationToken cancellationToken = default)
{
    await _unitOfWork.TransactionAsync(async ct =>
    {
        var dr = await _repository.DeleteAsync(id.ThrowIfNullOrEmpty(), ct).ConfigureAwait(false);

        // ❌ Wrong — fabricates a throwaway value to carry the id; attaches a (near-empty) body,
        //    adds a version suffix delete must not have, and puts the id in the body, not the key.
        dr.WhereMutated(() =>
            _unitOfWork.Events.Add(EventData.CreateEventWith(new Contracts.Employee { Id = id }, EventAction.Deleted)));

        // ✅ Correct — no value; type + action + key only.
        dr.WhereMutated(() =>                                    // () — no value on a delete DataResult
            _unitOfWork.Events.Add(
                EventData.CreateEvent<Employee>(EventAction.Deleted).WithKey(id)));
    }, cancellationToken).ConfigureAwait(false);
}
```

## Result&lt;T&gt; Pipeline Style

Using `Result<T>` chains is a developer choice — it is not restricted to DDD aggregate services. It can be applied to any service method where explicit, composable failure propagation is preferred over exceptions. Compose with `Result.GoAsync`, `.ThenAs`, `.ThenAsAsync`. The unit of work is still `TransactionAsync`:

```csharp
public Task<Result<Basket>> CreateAsync(string customerId, CancellationToken cancellationToken = default)
{
    var aggregate = Domain.Basket.CreateNew(customerId.ThrowIfNullOrEmpty());

    return _unitOfWork.TransactionAsync(async ct =>
    {
        var br = await _repository.CreateAsync(aggregate, ct).ConfigureAwait(false);
        return br.ThenAs(b =>
        {
            var contract = BasketMapper.Map(b);
            _unitOfWork.Events.Add(EventData.CreateEventWith(contract, EventAction.Created));
            return contract;
        });
    }, cancellationToken);
}
```

For multi-step orchestration with early exit on the first failure:

```csharp
var pr = await Result.GoAsync(() => SomeValidator.Default.ValidateWithResultAsync(input, cancellationToken))
    .ThenAsAsync(v => _someAdapter.EnsureExistsAsync(v.Id!, cancellationToken));

if (pr.IsFailure)
    return pr.AsResult();
```

#### Operator reference and the `As` / `Async` naming convention

The pipeline operators come in **families**, each with consistent modifier suffixes. **Read the suffix to know what an operator does:**

- **`As`** — the operation **changes the result type** (`Result` → `Result<T>`, `Result<T>` → `Result<U>`, or `Result<T>` → `Result`). The non-`As` form keeps the same type. This is **by design**: you must explicitly opt into a type change, so a `T` flowing through unchanged uses `Then`, while producing a different type uses `ThenAs`. If the compiler complains a delegate returns the "wrong" type, you almost certainly want the `As` variant.
- **`Async`** — the supplied delegate is asynchronous (returns a `Task`).
- **`AsAsync`** — both: an async delegate that also changes the type.

| Family | Runs the delegate when… | Same-type / type-changing |
|---|---|---|
| `Then` | result is **success** | `Then` / `ThenAs` |
| `When` | success **and** a condition holds | `When` / `WhenAs` |
| `Any` | **always** (success or failure) | `Any` / `AnyAs` |
| `OnFailure` | result is **failure** | `OnFailure` / `OnFailureAs` |
| `Match` | branches on success vs failure, returning a value | `Match` / `MatchAs` |

Each has `Async` and `AsAsync` variants too (e.g. `ThenAsync`, `ThenAsAsync`). Start a pipeline with `Result.Go(...)` / `Result.GoAsync(...)`; also available: `Bind`, `Combine`, and `.AsResult()` to drop a `Result<T>` to a `Result`.

Failure factories (return a failed result of the matching type): `Result.ValidationError(...)`, `NotFoundError(...)`, `BusinessError(...)`, `ConflictError(...)`, `ConcurrencyError(...)`, `DuplicateError(...)`, `AuthenticationError(...)`, `AuthorizationError(...)`, `TransientError(...)`. Success factories: `Result.Success`, `Result<T>.Ok(value)`. See the [CoreEx Results README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/Results/README.md) for the full set.

## CQRS — Read Services

Split a domain's service operations **by mutation** — this is the convention:

- **Mutating** operations — `Create`, `Update`, `Delete`, and any other state-changing operation — live in **`XxxService`** (`IXxxService`). These own validation, the unit of work, and event publication. `XxxService` **also** exposes a primary by-id `GetAsync(id)` to support its own mutation flows (the PATCH pre-fetch, fetch-then-update, concurrency/not-found checks).
- **Query and read-model** operations — `QueryAsync` (collections/search) and other purpose-built read shapes — live in **`XxxReadService`** (`IXxxReadService`), which also exposes the by-id `GetAsync` for the read API.

This is the surface expression of CQRS: the write model (mutations + events) and the read model (queries returning purpose-built shapes) are designed and scaled independently. Both interfaces live in `Interfaces/` side by side (`IProductService.cs`, `IProductReadService.cs`); both implementations live together in the service folder.

A primary by-id `GetAsync` therefore appears on **both** services — this is intentional. Each is a single line delegating to the shared `IXxxRepository.GetAsync`, so there is no real logic duplication; having `XxxService` own a `GetAsync` keeps the mutating controller dependent on **one** service (no need to also resolve `IXxxReadService` just for the PATCH fetch). The meaningful divergence — queries and read-optimized shapes — stays exclusive to `XxxReadService`.

```csharp
[ScopedService<IProductReadService>]
public class ProductReadService(IProductRepository repository) : IProductReadService
{
    private readonly IProductRepository _repository = repository.ThrowIfNull();

    public Task<Product?> GetAsync(string id, CancellationToken cancellationToken = default) => _repository.GetAsync(id, cancellationToken);
    public Task<ItemsResult<ProductLite>> QueryAsync(QueryArgs? query, PagingArgs? paging, CancellationToken cancellationToken = default)
        => _repository.QueryAsync(query, paging, cancellationToken);
}
```

**The repository stays singular — CQRS is a service-layer split, not a data-layer one.** Both `XxxService` and `XxxReadService` inject the **same** `IXxxRepository` when they share a data source (e.g. the one SQL database) — do **not** split the repository to mirror the services. Only when a specific operation targets a **different** data source (e.g. a read served from a separate store or search index) is an additional repository introduced; the owning service injects and calls the appropriate repository per operation.

## Anti-Corruption Layer (Adapters)

When a service needs to call another domain's API, inject an adapter interface (e.g., `IProductAdapter`) rather than calling `HttpClient` directly. Implement the adapter in the Infrastructure layer using a typed HTTP client. The interface surface should be domain-idiomatic — not a mirror of the remote API.

Adapter interfaces live in `Application/Adapters/` (one interface per external domain). The Infrastructure implementation lives in `Infrastructure/Adapters/`.

```csharp
// Application/Adapters/IProductAdapter.cs — interface only (domain-idiomatic, not a mirror of the remote API)
public interface IProductAdapter
{
    Task<Result<Product>> GetAsync(string id, CancellationToken cancellationToken = default);
    Task<Result> ReserveInventoryAsync(Domain.Basket basket, CancellationToken cancellationToken = default);
    Task<Result> CancelReservationAsync(Domain.Basket basket, CancellationToken cancellationToken = default);
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

    public Task<Result<Product>> EnsureExistsAsync(string productId, CancellationToken cancellationToken = default) => Result
        .GoAsync(() => _productAdapter.GetAsync(productId, cancellationToken))
        .OnFailure(r => r.IsNotFoundError
            ? Result.ValidationError(MessageItem.CreateErrorMessage(nameof(productId), "Product was not found."))
            : r);
}

// In the calling service — _productAdapter is already injected into the service:
var result = await new ProductPolicy(_productAdapter).EnsureExistsAsync(productId, cancellationToken);
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
- Do not put **query/collection or read-model** operations in `XxxService`, nor mutating operations in `XxxReadService` — queries belong in `XxxReadService`. (A primary by-id `GetAsync` legitimately appears on **both**: the write service uses it to support its own mutations, e.g. the PATCH pre-get.)
- Do not split the repository to mirror the CQRS services — both share one `IXxxRepository` per data source; add another repository only for a genuinely different data source.
- Do not call `HttpClient` directly from services — always go through an adapter interface.
- Do not reference Infrastructure assemblies from the Application layer — all persistence and transport concerns are reached through interfaces.
- Do not implement rules in `OnValidateAsync` that require I/O without first guarding with `if (context.HasErrors) return;`.
- Do not add business logic to controllers — services own all use-case orchestration.
- Do not register Validators, Mappers, or Policies in DI or inject them into service constructors — call or instantiate them directly at the point of use (YAGNI: refactor to DI only when there is a real need to mock or replace them).
- Do not use `new ProductValidator()` at the call site when `Validator<T, TSelf>` provides a `Default` singleton — use `ProductValidator.Default`.
- Do not inject a validator into a service constructor — a `Validator<T, TSelf>` is invoked via its `Default` singleton, never as a constructor dependency.
- Do not call bare `ValidateAsync(...)` for fail-fast validation — it returns the result without throwing, silently swallowing errors. Use `ValidateAndThrowAsync` (non-ROP) or `ValidateWithResultAsync` (ROP).
- Do not perform a mutating `TransactionAsync` without adding its event (`_unitOfWork.Events.Add(...)`) when eventing is enabled — the write and the event must be committed together.
- Do not use `WhereMutated(_ => ...)` on a delete — `DataResult` (delete) has no value; use the parameterless `WhereMutated(() => ...)`. The value-carrying `WhereMutated(v => ...)` is only for `DataResult<T>` (create/update).
- Do not reach for a non-`As` Result operator when the delegate changes the result type — use the `As` variant (e.g. `ThenAs`/`ThenAsAsync`); the `As` suffix exists to make the type change explicit.
- Do not generate service operations the user did not confirm — confirm the CRUD set (Get/Create/Update/Delete, each default-selected) when operations are unspecified, and never add a Query operation without an explicit request.

## Further Reading

- [Application Layer Guide](/.github/docs/coreex/application-layer.md) — full walkthrough of services, validators, adapters, policies, mapping, and the unit-of-work pattern (docs-sync cache; after `/coreex-docs-sync`).
- [Pattern Catalog](/.github/docs/coreex/patterns.md) — CQRS, Service, Unit of Work, Validator, Policy, Adapter, and Event patterns with cross-links (docs-sync cache; after `/coreex-docs-sync`).
- [Layer Dependencies](/.github/docs/coreex/layers.md) — layer dependency rules: Application depends inward only on Contracts and its own interfaces (docs-sync cache; after `/coreex-docs-sync`).
- [CoreEx.Validation guide](/.github/docs/coreex/agents/CoreEx.Validation.md) — `Validator<T>`, rule set, `OnValidateAsync`, and `ValidateFurtherAsync` (docs-sync cache; after `/coreex-docs-sync`). Source: [CoreEx.Validation README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.Validation/README.md).
- [CoreEx guide](/.github/docs/coreex/agents/CoreEx.md) — `IUnitOfWork`, `Result<T>`, `[ScopedService]`, and CoreEx exception types (docs-sync cache; after `/coreex-docs-sync`). Source: [CoreEx README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/README.md).
- [CoreEx Results README](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx/Results/README.md) — `Result<T>` type, pipeline operators (`.GoAsync`, `.ThenAs`, `.ThenAsAsync`), and error propagation semantics.
- Related skills: [`coreex-app-service`](/.github/skills/coreex-app-service/SKILL.md) (scaffold a service), [`coreex-validator`](/.github/skills/coreex-validator/SKILL.md) (scaffold a validator), [`coreex-policy`](/.github/skills/coreex-policy/SKILL.md) (scaffold a domain-guard policy).
