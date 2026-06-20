# CoreEx — AI Usage Guide

`CoreEx` is the shared kernel of the CoreEx framework. All other CoreEx packages depend on it.

## Semantic Exceptions

Throw the most specific exception; the framework maps each to its HTTP status automatically.

```csharp
// 400 — field-level validation errors
throw new ValidationException(new MessageItemCollection { new("sku", "Sku is required.") });

// 404 — resource not found
throw new NotFoundException();

// 400 — business rule violation safe to surface to the caller
throw new BusinessException("Basket is already checked out.");

// 412 — optimistic concurrency conflict
throw new ConcurrencyException();

// 409 — duplicate record
throw new DuplicateException();
```

## ExecutionContext

`ExecutionContext` is `AsyncLocal`-scoped and carries user identity, tenant ID, timestamp, and operation type for the lifetime of a request. Access it via DI injection, or use the static accessors when outside the DI graph.

```csharp
// Preferred — inject via DI (scoped per request)
public class MyService(ExecutionContext context) { }

// Static access — returns the ambient instance for the current async context
var ctx = ExecutionContext.Current;          // throws if none set
var ctx = ExecutionContext.TryGetCurrent();  // returns null if none set
```

## Result / Result\<T\>

Use `Result<T>` for expected business-error paths instead of throwing exceptions. Compose pipelines with `Then`, `ThenAs`, `GoAsync`, `ThenAsAsync`.

```csharp
public async Task<Result<Order>> GetOrderAsync(Guid id)
{
    var order = await _repo.GetAsync(id).ConfigureAwait(false);
    return order is null ? Result.NotFoundError() : Result.Ok(order);
}

// Pipeline composition
return await GetOrderAsync(id)
    .ThenAsAsync(order => _validator.ValidateAndThrowAsync(order))
    .ConfigureAwait(false);
```

## Entity Contracts

Use the standard interfaces on your contracts so the framework's ETag, paging, and change-log handling works automatically.

For entities authored in a project that references `CoreEx.CodeGen`, prefer the Roslyn source generator — declare a `partial` class with `[Contract]` and the generator emits the boilerplate (constructors, `CopyFrom`, `Clone`, equality, `CleanUp`, etc.).

```csharp
// Source-generated contract — preferred when CoreEx.CodeGen is referenced
[Contract]
public partial class Product : IIdentifier<Guid>, IETag, IChangeLog
{
    public Guid Id { get; set; }
    public string? ETag { get; set; }
    public ChangeLog? ChangeLog { get; set; }
    public string? Name { get; set; }
}

// Reference-data contract — generator emits the full ReferenceData shape
[ReferenceData]
public partial class Status { }
```

When `CoreEx.CodeGen` is not available, implement the interfaces directly:

```csharp
public class Product : IIdentifier<Guid>, IETag, IChangeLog
{
    public Guid Id { get; set; }
    public string? ETag { get; set; }
    public ChangeLog? ChangeLog { get; set; }
}
```

Server-managed fields (e.g. `ETag`, `ChangeLog`) should be marked `[ReadOnly(true)]` on generated contracts so they are excluded from inbound deserialization.

## Dependency Injection Attributes

Mark implementation classes with `[ScopedService]`, `[SingletonService]`, or `[TransientService]` and register them all at once via `AddDynamicServicesUsing<T>()`.

```csharp
[ScopedService<IProductService>]
public class ProductService : IProductService { }

// Program.cs
builder.Services.AddDynamicServicesUsing<ProductService>();
```

## PrecisionTimeProvider

Register in `Program.cs` to ensure timestamps are truncated to database-compatible precision.

```csharp
builder.Services.AddPrecisionTimeProvider();
```

## Do Not

- Do not catch `IExtendedException` types to re-wrap them — let the framework middleware translate them.
- Do not use `AutoMapper` — use explicit `Mapper<TSource, TDest>` or `BiDirectionMapper<TFrom, TTo>`.
- Do not use `DateTime.UtcNow` directly — use `Runtime.UtcNow`, which returns `ExecutionContext.Timestamp` when a context is active and falls back to `TimeProvider.System.GetUtcNow()` otherwise.

## Further Reading

- [README](./README.md) — full API surface and namespace index.
- [CoreEx.AspNetCore](../CoreEx.AspNetCore/README.md) — HTTP translation of these exceptions and Result types.
- [CoreEx.Validation](../CoreEx.Validation/README.md) — ValidationException production.
- [Contracts layer](../../samples/docs/contracts-layer.md) — how entity contracts, `[Contract]`, `[ReferenceData]`, and standard interfaces are used in practice.
- [Application layer](../../samples/docs/application-layer.md) — real-world usage of `Result<T>`, semantic exceptions, and `ExecutionContext` in application services.
- [Patterns](../../samples/docs/patterns.md) — pattern catalogue covering error handling, railway-oriented flows, and cross-cutting concerns.
- [Layers overview](../../samples/docs/layers.md) — full layer dependency diagram and design-time tooling overview.
