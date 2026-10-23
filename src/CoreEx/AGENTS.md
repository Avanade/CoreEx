# CoreEx — AI Usage Guide

`CoreEx` is the shared kernel of the CoreEx framework. All other CoreEx packages depend on it.

## Semantic Exceptions

When collecting multiple field errors, use `MessageItem.CreateErrorMessage<TEntity>(x => x.Property, text)` and wrap the collection once with `Result.ValidationError(messages)` or `new ValidationException(messages)`. Text may be omitted/null for the inferred invalid message; `(expression, options, text)` supports custom naming. Exception factories delegate to this message factory. For reusable policies without a known request type, retain explicit caller-agreed JSON paths rather than inventing a contract dependency.

Throw the most specific exception; the framework maps each to its HTTP status automatically.

```csharp
// 400 — field-level validation errors
throw ValidationException.Create<Product>(x => x.Sku, "Sku is required.");

// 404 — resource not found
throw new NotFoundException();

// 400 — business rule violation safe to surface to the caller
throw new BusinessException("Basket is already checked out.");

// 412 — optimistic concurrency conflict
throw new ConcurrencyException();

// 409 — duplicate record
throw new DuplicateException();
```

For manual property errors, prefer `ValidationException.Create<TEntity>(x => x.Property, text)` over hard-coded names. Nested paths such as `x => x.Address!.Street` resolve every segment through runtime metadata to JSON names (e.g. `address.street`), respecting `[JsonPropertyName]` and the default serializer naming policy. Use `(expression, options, format, ...values)` for explicit serializer options. Entity access is never executed, so `!` is safe for nullable intermediate properties. Array/list indexes and string dictionary keys support literals and captured locals: `x => x.Addresses[0].Street` yields `addresses[0].street`; `x => x.AddressesByType["home"].Street` yields `addressesByType.home.street`. String keys follow the effective `DictionaryKeyPolicy` (null preserves keys), matching validators and serialized JSON. No escaping is added; punctuation can be ambiguous because these are error paths, not full JSONPath expressions. Entity fields, arbitrary getters/calls for indexes/keys, index calculations, and intermediate casts are rejected. Formatting matches the existing string-property factory; no label/value arguments are prepended. Within a validator, continue using `context.AddError(...)` to preserve contextual paths, naming overrides, and validator formatting.

For lookup failures, classify by identifier role: missing target/explicit child → `NotFoundException` / `Result.NotFoundError` (404); missing body reference → field-level validation (400), using an expression over the request contract where known. Query filters normally return empty results. Translate only expected entity-not-found outcomes, not route/configuration failures; preserve unrelated failures and deliberate delete/security semantics. Business errors describe process/state rules, with more specific conflict/concurrency or authorization semantics taking precedence; error-code needs never determine classification. See [application-layer guidance](../../samples/docs/application-layer.md#missing-targets-versus-invalid-references).

Request/input validity uses validation errors; aggregate/entity/value-object business invariants use `BusinessException` / `Result.BusinessError` with domain messages/codes and no request DTO dependencies or JSON paths. Both normally map to 400 but are different semantic contracts. Keep domain guards after request validation; Application may selectively map known domain failures to fields, never blanket-convert them. Programming misuse retains argument/invalid-operation errors; shared guard helpers keep their semantics. See [domain guidance](../../samples/docs/domain-layer.md#business-invariants-versus-request-validation).

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

Expression-based `ValidationException.Create<TEntity>` and `Result.ValidationError<TEntity>`/`Result<T>.ValidationError<TEntity>` accept an omitted/null message, defaulting to `Validation.InvalidFormat` with the leaf property's friendly metadata `Text` (including localization/display annotations). `RuntimeMetadata.GetJsonPathAndText` resolves the path and label together. A terminal index/key uses its containing property's label. Explicit messages and existing string-property factories are unchanged.

For manual property validation failures, use `Result.ValidationError<Person>(x => x.Name, "Name is invalid.")` or `Result<Order>.ValidationError<Person>(x => x.Name, "Name is invalid.")`. The shorthand accepts an expression, an `LText` message (including preformatted arguments), and an optional exception configuration action. It uses the default JSON naming, nesting, and indexing from `ValidationException.Create<TEntity>`. For explicit serializer options or separate formatting arguments, use that exception factory directly; exceptions implicitly convert to `Result` and `Result<T>`.

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

## HTTP Request Builder and Extensions

Use `client.Send(method, uri)` or the `Get`, `Post`, `Put`, `Patch`, and `Delete` shortcuts to build HTTP requests. Add a JSON body with `WithJson(value)` (CoreEx serializer defaults; null sends JSON null), or explicit `HttpContent` with `WithContent(content)`. Exactly one body assignment is allowed; duplicates or mixing the two throw `InvalidOperationException`. `Patch` does not imply JSON Merge Patch; select it explicitly with `WithMergePatchJsonContentType()`.

Builders are single-use: the first terminal call consumes the builder, even on failure or cancellation. Repeated/concurrent execution or subsequent configuration throws `InvalidOperationException`; configure before sending. `Configure` cannot replace/remove an assigned body. Accepted explicit content is disposed during execution; the caller owns rejected content or content on a builder that is never executed. Terminal methods (`GetValueAsync<T>`, `GetValueOrDefaultAsync<T>`, `GetStringAsync`, `ToResultAsync`, `ToProblemDetailsAsync`, `ThrowOnErrorAsync`) automatically dispose the response; callers own raw responses from `GetResponseAsync`/`SendAsync`.

```csharp
// Fluent request with query filter, paging, and typed response deserialization + disposal
var products = await _httpClient
    .Send(HttpMethod.Get, "api/products")
    .WithQuery("contains(Text, 'hammer')")
    .WithPaging(take: 10)
    .GetValueAsync<ProductLite[]>(cancellationToken).ConfigureAwait(false);

// POST JSON body and map response to CoreEx Result with automatic disposal
return await _httpClient
    .Post("api/inventory/reserve")
    .WithJson(request)
    .ToResultAsync(cancellationToken).ConfigureAwait(false);

// Intercept specific HTTP status codes (e.g. 404 -> Result.NotFoundError)
return await _httpClient
    .Send(HttpMethod.Get, $"api/customers/{Uri.EscapeDataString(id)}")
    .WithStatusResult(HttpStatusCode.NotFound, () => Result.NotFoundError())
    .ToResultAsync<Customer>(cancellationToken).ConfigureAwait(false);
```

Register named or typed HTTP clients in `Program.cs` via `builder.AddNamedHttpClient(...)` or `builder.AddTypedHttpClient<TClient>(...)`. These automatically configure the base address, `IdempotencyKeyHandler`, service discovery, and `Microsoft.Extensions.Http.Resilience` standard resilience handlers.

## Validating HTTP Response Values

Use `WithValidator(validator)` to validate a successful, non-null deserialized dependency response.
It can be chained directly onto `HttpRequestBuilder` (which manages request execution and automatic
response disposal) or used on an existing `HttpResponseMessage`.

When combined with `.WithStatusResult(status, result)` (or `.WithErrorStatus(...)`), matched status
codes short-circuit before deserialization and validation, returning the custom result immediately.

The typed wrapper returns the validation result's `Value`; reported validation errors become failed
results containing `HttpRequestException` with structured inner validation diagnostics.
`GetValueAsync(ct)` is the throwing equivalent. Standard API handling treats these as internal 500
errors, not caller 400 validation errors.

Required methods reject missing content and null validated values; `OrDefault` methods allow
null and skip validation for absent response values. Existing HTTP/ProblemDetails handling,
deserialization exceptions, unexpected validator exceptions, and cancellation are unchanged.

When using terminal methods on `HttpRequestBuilder` (e.g. `ToResultAsync`, `GetValueAsync<T>`), response disposal
is handled automatically. When inspecting raw responses via `builder.GetResponseAsync()` or directly awaiting
`client.SendAsync`, retain `using var response = ...` to dispose the response at the call site.

For small client-only rules, configure a private static readonly `Validator<T>` once with
`Validator.Create<T>().HasProperty(...)` from the `CoreEx.Validation` package. Keep these rules
inside the Infrastructure HTTP client, not Application validators; validate consumed fields,
not create/update requirements or identifier matching by default. Test through the client.

```csharp
private static readonly Validator<Customer> _validator = Validator.Create<Customer>()
    .HasProperty(x => x.Email, c => c.Mandatory().Email());

// Fluent builder execution with custom status mapping, validation, and automatic disposal:
return await _httpClient
    .Send(HttpMethod.Get, $"api/customers/{Uri.EscapeDataString(id)}")
    .WithStatusResult(HttpStatusCode.NotFound, () => Result.NotFoundError())
    .WithValidator(_validator)
    .ToResultAsync(ct).ConfigureAwait(false);

// Or when inspecting raw response directly (caller owns response disposal):
using var response = await _httpClient
    .Send(HttpMethod.Get, $"api/customers/{Uri.EscapeDataString(id)}")
    .GetResponseAsync(ct).ConfigureAwait(false);

return await response.WithValidator(_validator)
    .WithStatusResult(HttpStatusCode.NotFound, () => Result.NotFoundError())
    .ToResultAsync(ct).ConfigureAwait(false);
```

See [usage, inline examples, and semantics](./README.md#validating-http-response-values).

## Entity Contracts

Cached runtime metadata lookups prefer the type's static `IRuntimeMetadata` provider and use reflection only for types without that interface. This applies to root/nested `GetJsonPath` and `GetJsonPathAndText` resolution, so custom/generated JSON names, friendly labels, and excluded properties are respected consistently regardless of which cache entry point runs first. Entity instances are never constructed for metadata discovery.

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

Properties that are mandatory/required but declared nullable (`string?`, `int?`) for DTO flexibility should be marked `[NonNullable]` (`CoreEx.Entities`) so OpenAPI (via `CoreEx.AspNetCore.NSwag`) reports them as `nullable: false`. Do not confuse it with `System.Diagnostics.CodeAnalysis.NotNull`.

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

- Do not catch application `IExtendedException` types to re-wrap them — let the framework middleware translate them. For reported external-response validation failures, use `WithValidator` to classify them as dependency failures instead.
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
