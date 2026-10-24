# CoreEx

> The foundational `CoreEx` package providing the core runtime primitives, patterns, and abstractions used across all other CoreEx libraries and consuming services.

## Overview

`CoreEx` is the shared kernel of the CoreEx framework. It defines the types that all other packages depend on and that consuming applications interact with every day: semantic exception types, execution context, railway-oriented result types, entity contracts, mapping, JSON utilities, hosting infrastructure, caching abstractions, localization, and more.

The package is deliberately non-opinionated — nothing forces a particular architectural style and every capability is opt-in. Teams can adopt individual namespaces incrementally, composing only what they need without taking on the full framework surface area at once.

All other CoreEx packages (`CoreEx.Events`, `CoreEx.Database`, `CoreEx.AspNetCore`, etc.) depend on this package, making it the natural starting point when working with the CoreEx ecosystem.

## Motivation

- **Reduce boilerplate** — recurring patterns such as exception-to-HTTP-status mapping, entity identity, ETag handling, and audit change-log tracking are defined once and reused everywhere rather than reimplemented per project.
- **Standardize errors** — a rich hierarchy of semantic exception types with built-in HTTP status codes and structured error payloads removes the need for bespoke error-handling middleware.
- **Explicit, expected errors** — the `Result`/`Result<T>` types bring railway-oriented programming to service code, allowing known business errors to be propagated without the overhead and noise of exceptions.
- **Execution context** — a consistent, `AsyncLocal`-based ambient context carries user identity, tenant, timestamps, and operation type across the full call chain without threading them explicitly through every method signature.
- **Composability** — DI registration attributes, invoker tracing, and host-settings primitives are intentionally thin so they integrate into any host model without dictating structure.

## Key capabilities

- 🚨 **Semantic exceptions**: A hierarchy of typed exceptions (`ValidationException`, `NotFoundException`, `BusinessException`, `ConcurrencyException`, etc.) each with a pre-mapped HTTP status code, structured error type, and configurable logging enablement.
- 🔐 **Execution context**: An `AsyncLocal`-scoped `ExecutionContext` that carries user identity (`AuthenticationUser`), tenant ID, operation type, request timestamp, and arbitrary attributes throughout the lifetime of a request.
- 🚂 **Railway-oriented results**: `Result` and `Result<T>` value types that represent success or a typed error without throwing, enabling functional pipeline-style error propagation for expected business outcomes.
- 📦 **Entity contracts**: Interfaces and types for identifiers (`IIdentifier<T>`), ETags (`IETag`), audit change logs (`IChangeLog`), composite keys (`CompositeKey`), and entity cleaning/transformation — used as the common contract shape across all CoreEx layers.
- 📝 **Partial-update values**: `Optional<T>` preserves omitted-versus-supplied intent for explicit request/patch DTOs without changing ordinary contracts or persistence models.
- 🗺️ **Explicit mapping**: Uni- and Bi-directional mapper base classes and a `Mapper` utility for propagating standard contract properties (identity, ETags, change logs, tenant, partition) between source and destination types without a reflection-heavy mapping library.
- 🔄 **JSON utilities**: RFC 7396 JSON Merge Patch (`JsonMergePatch`), include/exclude JSON property filtering (`JsonFilter`), and custom converters for common CoreEx types.
- 💉 **DI lifetime attributes**: `[ScopedService]`, `[SingletonService]`, and `[TransientService]` attributes that mark implementation types for automatic discovery and registration via `AddDynamicServicesUsing<T>`.
- ⏱️ **Hosting and work orchestration**: Base classes for timer-driven and synchronized hosted services, plus `WorkOrchestrator` for tracking long-running distributed work states with pluggable storage.
- ⚡ **Caching abstractions**: `IHybridCache` and `ICacheKeyProvider` decouple cache consumption from provider implementation, supporting both in-process and distributed cache strategies.
- 🌐 **Localization**: `LText` and `TextProvider` provide a lightweight, key-based localization abstraction used throughout validation messages, exception text, and reference data.
- 🃏 **Wildcard parsing**: `Wildcard` provides standardized `*` and `?` wildcard parsing, validation, and `Regex` compilation for consistent wildcard-based filtering.
- 🏷️ **Reference data interfaces**: Core `IReferenceData`, `IReferenceDataCollection`, and `ReferenceDataOrchestrator` types establish the reference data contract used by `CoreEx.RefData` and data-layer packages.
- 📐 **Schema versioning**: `SchemaAttribute` and `Schema` utilities for associating a semantic version with entity types, supporting schema-aware messaging and validation.
- 🌐 **HTTP client and request builder**: `client.Send(method, uri)` or `Get`/`Post`/`Put`/`Patch`/`Delete` return a single-use `HttpRequestBuilder`. Add exactly one body via `WithJson(value)` (JSON) or `WithContent(content)`, configure queries/paging/headers, then execute a terminal method (`GetValueAsync<T>`, `ToResultAsync`, `GetStringAsync`, etc.) with automatic response disposal except when returning the raw response. Reuse, configuration after execution begins, or duplicate body assignment throws `InvalidOperationException`. `Patch` does not imply JSON Merge Patch. See the [HTTP guide](./Http/README.md#request-bodies-and-single-use-execution) for ownership and execution semantics. `builder.AddNamedHttpClient` / `AddTypedHttpClient` configure service discovery, resilience, and idempotency key handling. For SDK-owned client names, `builder.AddNamedHttpClient("Sdk_httpClient", configureClient: null, configurationSectionName: "Service")` augments the SDK registration using `Service:BaseAddress` and `Service:Resilience`; existing overloads continue using the client name as the configuration section.

## Key types

| Type | Description |
|------|-------------|
| **[`ExecutionContext`](./ExecutionContext.cs)** | `AsyncLocal`-scoped ambient context carrying user identity, tenant ID, operation type, timestamp, and attributes for the lifetime of a request. |
| **[`Result`](./Results/Result.cs)** | Value type representing a successful or failed operation with no return value; the basis of railway-oriented programming in CoreEx. |
| **[`Result<T>`](./Results/ResultT.cs)** | Value type representing a successful operation with a value, or a typed error — used for functional pipeline-style error propagation. |
| **[`HttpRequestBuilder`](./Http/HttpRequestBuilder.cs)** | Fluent HTTP request builder for configuring requests, chaining extensions, and executing terminal deserialization and result mapping with automatic response disposal. |
| **[`DataConsistencyException`](./DataConsistencyException.cs)** | Non-error exception used to signal a potential data consistency issue without treating it as an application error. |
| **[`PrecisionTimeProvider`](./PrecisionTimeProvider.cs)** | `TimeProvider` implementation that truncates timestamps to a configurable fractional-second precision for database compatibility. |
| **[`OperationType`](./OperationType.cs)** | Enum representing the CRUD operation type (Get, Create, Update, Delete, Query) carried on the `ExecutionContext`. |
| _[`ExtendedException`](./Abstractions/ExtendedException.cs)_ | Abstract base for all CoreEx semantic exceptions; provides HTTP status, error type, error code, detail, and configurable logging. |
| [`IExtendedException`](./Abstractions/IExtendedException.cs) | Interface defining the extended exception contract: `StatusCode`, `ErrorType`, `ErrorCode`, `IsError`, and `ShouldBeLogged`. |

See the [Extended Exceptions](#extended-exceptions) section below for the full list of semantic exception types.

## Optional patch values

For partial-update request DTOs, `Optional<T>` distinguishes an omitted property from a supplied `null` or default value. `default(Optional<T>)` is unspecified; implicit conversion from `T` and `Optional<T>.Specified(value)` create a specified value. Its JSON converter reads and writes the inner value shape, while CoreEx's `WhenWritingDefault` serializer setting omits unspecified properties. OpenAPI exposes the inner type and nullability while leaving the property non-required.

Keep `Optional<T>` on the request boundary: validate with `.Unwrap()` and copy only specified values into the ordinary contract/domain object. It is not a persistence or event-model type. See [contract patch guidance](../../.github/instructions/coreex-contracts.instructions.md#optional-patch-properties) and [optional validation](../CoreEx.Validation/README.md#optional-patch-validation).

## Validating HTTP response values

HTTP success and JSON deserialization do not guarantee a valid dependency response. Use
`WithValidator` with a `CoreEx.Validation.IValidator<T>` to opt into response-contract validation.
This can be chained directly onto `HttpRequestBuilder` (which manages execution and automatic response disposal)
or applied directly to an `HttpResponseMessage`:

```csharp
// Fluent request with validator and automatic response disposal:
return await _httpClient.Send(HttpMethod.Get, $"api/customers/{Uri.EscapeDataString(id)}")
    .WithValidator(customerValidator)
    .ToResultAsync(ct).ConfigureAwait(false);

// Or on an existing HttpResponseMessage (caller owns response disposal):
using var response = await _httpClient.GetAsync($"api/customers/{Uri.EscapeDataString(id)}", ct).ConfigureAwait(false);
return await response.WithValidator(customerValidator).ToResultAsync(ct).ConfigureAwait(false);
```

The typed wrappers ([`ValidatedHttpRequestBuilder<T>`](./Http/ValidatedHttpRequestBuilderT.cs) and
[`ValidatedHttpResponse<T>`](./Http/ValidatedHttpResponseT.cs)) reuse the existing response converters and
validates only successful, non-null deserialized values. It returns the **validation result's
Value**, including replacements or normalization. `ToResultAsync` requires a non-null value both
before and after validation; `ToResultOrDefaultAsync` allows null and skips validation when the
response has no value. `GetValueAsync` and `GetValueOrDefaultAsync` provide the throwing equivalents.
All four methods accept optional JSON serializer options and a cancellation token.

Reported validation errors become `HttpRequestException` failures with the validation exception
as the inner exception, preserving structured messages. `ToResult*` returns these failures;
`GetValue*` throws them. Under standard CoreEx API exception handling, these are internal **500**
errors, not caller **400** validation errors. The exception's upstream status is diagnostic context,
not the status returned to the caller. Outer messages do not contain the response body.
Existing HTTP/ProblemDetails handling is unchanged; cancellation, deserialization exceptions,
and unexpected exceptions thrown by a validator still propagate.

Use a response-specific validator rather than assuming create/update rules apply to read responses.
For small client-only rules, prefer a private static readonly `Validator<T>` configured once with
`Validator.Create<T>().HasProperty(...)` from the `CoreEx.Validation` package inside the Infrastructure client:

```csharp
private static readonly Validator<Customer> _validator = Validator.Create<Customer>()
    .HasProperty(x => x.Email, c => c.Mandatory().Email());

// Fluent builder with custom status mapping and automatic response disposal:
return await _httpClient.Send(HttpMethod.Get, $"api/customers/{Uri.EscapeDataString(id)}")
    .WithStatusResult(HttpStatusCode.NotFound, () => Result.NotFoundError())
    .WithValidator(_validator)
    .ToResultAsync(ct).ConfigureAwait(false);
```

Extract a colocated validator only for substantial rules or genuine reuse. Validate consumed
fields rather than requiring every DTO property or matching identifiers by default.
Configure shared rules once without captured request state or scoped dependencies, and test inline
rules through client unit tests rather than exposing them solely for testing. Invalid responses
must not be cached. See the
[Customers client sample](../../samples/src/Contoso.Shopping.Infrastructure/Clients/Customers/CustomersHttpClient.cs)
for a complete implementation.
Validators can normalize values or perform I/O, so consider their effects explicitly.
When using `HttpRequestBuilder` terminal methods, the response is automatically disposed. When using
raw `HttpResponseMessage.WithValidator`, the wrapper does not own or dispose the response — retain
responsibility for disposing the response via `using var` at the call site.
Like the underlying converters, a terminal operation consumes the content stream; creating a
wrapper does not buffer the body for repeated reads.

## Errors vs Exceptions

`CoreEx` distinguishes between expected and unexpected errors:

| Aspect | **Errors** (Expected) | **Exceptions** (Unexpected) |
|--------|------------------------|----------------------------|
| **Use For** | Validation failures, business rules, "not found" | System failures, infrastructure errors |
| **Types** | `CoreEx` semantic exceptions (implement [`IExtendedException`](./Abstractions/IExtendedException.cs))  | Standard .NET, and non-semantic, exceptions |
| **Benefits** | Explicit error handling | Rich diagnostics (stack traces) |
| **Example** | Customer not found | Database connection timeout |

## Extended Exceptions

Semantic (error-oriented) exception types with automatic HTTP status mapping:

| Exception | Description | HTTP Status | Error Type |
|-----------|-------------|-------------|------------|
| [`AuthenticationException`](./AuthenticationException.cs) | User not authenticated. | 401-Unauthorized | `authentication` |
| [`AuthorizationException`](./AuthorizationException.cs) | User lacks permissions. | 403-Forbidden | `authorization` |
| [`BusinessException`](./BusinessException.cs) | Business rule violation (message shown to consumer). | 400-Bad Request | `business` |
| [`ConcurrencyException`](./ConcurrencyException.cs) | Data concurrency conflict (ETag mismatch). | 412-Precondition Failed | `concurrency` |
| [`ConflictException`](./ConflictException.cs) | Data conflict (e.g., identifier already exists on create). | 409-Conflict | `conflict` |
| [`DuplicateException`](./DuplicateException.cs) | Duplicate value (e.g., unique code already in use). | 409-Conflict | `duplicate` |
| [`NotFoundException`](./NotFoundException.cs) | Entity not found. | 404-Not Found | `not-found` |
| [`TransientException`](./TransientException.cs) | Transient failure (retry candidate). | 503-Service Unavailable | `transient` |
| [`ValidationException`](./ValidationException.cs) | Validation failure with message collection. | 400-Bad Request | `validation` |

All inherit from [`ExtendedException<TSelf>`](./Abstractions/ExtendedExceptionT.cs) implementing [`IExtendedException`](./Abstractions/IExtendedException.cs).

Choose the error by the identifier's role: a missing operation target or explicitly addressed child is `NotFoundException` / `Result.NotFoundError` (404); a missing entity referenced by a request-body field is field-level validation (400 under CoreEx conventions). An unmatched collection filter normally returns an empty result. Services/policies interpret expected lookup outcomes, not every downstream 404 (which might indicate a route/configuration failure), and preserve unrelated failures. Error-code needs do not determine classification; retain deliberate idempotent-delete and security-concealment semantics. See [application-layer guidance](../../samples/docs/application-layer.md#missing-targets-versus-invalid-references).

For a manually raised property validation error, prefer an expression over a hard-coded property name.

Use validation errors for request/input validity; aggregate, entity, and value-object business invariants use `BusinessException` / `Result.BusinessError` with domain messages/codes and no request-field paths. Keep both request validation and domain enforcement. Argument/invalid-operation exceptions remain appropriate for programming misuse; shared guard helpers are not business-error factories. See [domain failure conventions](../../samples/docs/domain-layer.md#business-invariants-versus-request-validation).

To aggregate errors without creating an exception per item, use `MessageItem.CreateErrorMessage<Person>(x => x.Name)` or supply an explicit/preformatted `LText`. The overload `(expression, options, text)` accepts custom serializer options; null text infers the standard invalid message. `ValidationException.Create<TEntity>` delegates to this message factory, sharing metadata paths and friendly labels.

Omit the message, or pass null, to use `Validation.InvalidFormat` with the leaf property's friendly metadata label: `ValidationException.Create<Person>(x => x.Name)` produces `Name is invalid.`. Localization/display annotations are respected independently of JSON naming; terminal collection indexes or dictionary keys use the containing property's label. With explicit serializer options, pass null as the message to request inference. Explicit messages remain unchanged, and existing string-property factories retain their behavior.

```csharp
throw ValidationException.Create<Person>(x => x.Name, "Name is already registered.");
throw ValidationException.Create<Person>(x => x.Address!.Street, "Street is spooky.");
// JSON paths: "name" and "address.street" with default camel-case naming.
throw ValidationException.Create<Person>(x => x.Addresses[0].Street, "Street is spooky.");
throw ValidationException.Create<Person>(x => x.AddressesByType["home"].Street, "Street is spooky.");
// Validator-compatible paths: "addresses[0].street" and "addressesByType.home.street".
```

Each path segment uses runtime metadata: explicit `[JsonPropertyName]` names take precedence over the naming policy in `JsonDefaults.SerializerOptions`. For custom naming, use `ValidationException.Create<Person>(x => x.Address!.Street, options, "Street is spooky.")`. The expression is inspected, never executed; `!` is safe for nullable intermediate properties here. Direct/nested public instance properties, outermost boxing, one-dimensional array/list indexes, and string dictionary keys are supported. Indexes/keys may be literals or captured locals; arbitrary getters, method calls, index calculations, and intermediate casts are rejected. Collections are never accessed, so indexes and keys do not need to exist (indexes must be non-negative). String dictionary keys use the effective serializer's `DictionaryKeyPolicy`, matching validators and serialized JSON (unchanged when that policy is null). No escaping is added. These are error paths, not full `$`-rooted JSONPath expressions; dots/brackets in keys can be ambiguous. The `LText` and formatting behavior matches the string-property factory: no property label or current value is automatically prepended. Unlike a validator context, this factory always uses JSON names and has no contextual name/path overrides.

Additionally, these support `With*`-style methods to add additional context that is added to the resulting `ProblemDetails`:

``` csharp
throw new BusinessException($"Product '{movement.ProductId}' does not have sufficient quantity on hand.")
    .WithErrorCode("insufficient-quantity")
    .WithKey(movement.ProductId);
```

The above would result in the following `ProblemDetails`:

``` json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Product \u002700000001-0000-0000-0000-000000000000\u0027 does not have sufficient quantity on hand.",
  "status": 400,
  "traceId": "00-a8e8623ef74c2b53820d0ff5d799850d-df28b0bc787429f5-01",
  "errorType": "business",
  "errorCode": "insufficient-quantity",
  "key": "00000001-0000-0000-0000-000000000000"
}
```


## Namespaces

| Namespace | Description | Documentation |
|-----------|-------------|---------------|
| **`CoreEx.Caching`** | `IHybridCache` abstraction and `ICacheKeyProvider` for decoupled local and/or distributed caching. | [📖 README](./Caching/README.md) |
| **`CoreEx.Data`** | Lightweight data-access primitives: paging, query arguments, partition keys, tenant IDs, logical deletion, and `ItemsResult<T>`. | [📖 README](./Data/README.md) |
| **`CoreEx.DependencyInjection`** | Service lifetime attributes (`[ScopedService]`, `[SingletonService]`, `[TransientService]`) for attribute-driven DI registration. | [📖 README](./DependencyInjection/README.md) |
| **`CoreEx.Entities`** | Entity contract interfaces and types: identifiers, ETags, change logs, composite keys, string transforms, and entity cleaning. | [📖 README](./Entities/README.md) |
| **`CoreEx.Globalization`** | `TextInfoCasing` and globalization extensions for culture-aware string casing transformations. | [📖 README](./Globalization/README.md) |
| **`CoreEx.HealthChecks`** | Health check extensions and `HealthCheckTags` constants for standardizing CoreEx service health reporting. | [📖 README](./HealthChecks/README.md) |
| **`CoreEx.Hosting`** | Timer and synchronized hosted service base classes, `HostSettings`, `WorkOrchestrator`, and distributed work state tracking. | [📖 README](./Hosting/README.md) |
| **`CoreEx.Http`** | HTTP primitives: `ProblemDetails`, `IdempotencyKeyHandler`, `HttpNames`, and HTTP request/response extensions. | [📖 README](./Http/README.md) |
| **`CoreEx.Invokers`** | Invocation pipeline abstractions providing automatic OpenTelemetry tracing and structured logging around service operations. | [📖 README](./Invokers/README.md) |
| **`CoreEx.Json`** | JSON Merge Patch (RFC 7396), JSON property filtering, and custom converters for CoreEx entity types. | [📖 README](./Json/README.md) |
| **`CoreEx.Localization`** | `LText` localization-agnostic text value and `TextProvider` for key-based message resolution. | [📖 README](./Localization/README.md) |
| **`CoreEx.Mapping`** | Explicit bi-directional mapper base classes, `Mapper` standard-property utilities, and `IConverter` value converters. | [📖 README](./Mapping/README.md) |
| **`CoreEx.Metadata`** | `RuntimeMetadata` and `IPropertyRuntimeMetadata` for compile-time and reflection-based property introspection on contract types. | [📖 README](./Metadata/README.md) |
| **`CoreEx.RefData`** | Core reference data interfaces, `ReferenceDataOrchestrator`, and provider abstractions consumed by `CoreEx.RefData`. | [📖 README](./RefData/README.md) |
| **`CoreEx.Results`** | `Result` and `Result<T>` railway-oriented types with a full suite of functional extension methods. | [📖 README](./Results/README.md) |
| **`CoreEx.Schemas`** | `SchemaAttribute` and `Schema` utilities for associating semantic version metadata with entity types. | [📖 README](./Schemas/README.md) |
| **`CoreEx.Security`** | `AuthenticationUser` record and `AuthenticationType` enum representing the authenticated user on the `ExecutionContext`. | [📖 README](./Security/README.md) |
| **`CoreEx.Text`** | `SentenceCase` utility for converting identifier-style strings into human-readable sentence-case text. | [📖 README](./Text/README.md) |
| **`CoreEx.Validation`** | Lightweight validation interfaces (`IValidator<T>`, `MultiValidator`) used as the validation contract across CoreEx. | [📖 README](./Validation/README.md) |
| **`CoreEx.Wildcards`** | `Wildcard` parser, validator, and `Regex` compiler for standardized `*` and `?` wildcard-based text matching. | [📖 README](./Wildcards/README.md) |

## Related Namespaces

- **[`CoreEx.AspNetCore`](../CoreEx.AspNetCore/README.md)** - ASP.NET Core integration: `WebApi` HTTP helpers, `IExtendedException` middleware, and `ExecutionContext` middleware binding.
- **[`CoreEx.Validation`](../CoreEx.Validation/README.md)** - Full validation rule engine built on top of the `IValidator<T>` contract defined in this package.
- **[`CoreEx.Events`](../CoreEx.Events/README.md)** - Event publishing and subscribing built on top of the `ExecutionContext`, `Result`, and entity contracts from this package.
- **[`CoreEx.Database`](../CoreEx.Database/README.md)** - Database access layer built on the data primitives (`PagingArgs`, `QueryArgs`, `IIdentifier`) defined here.
- **[`CoreEx.EntityFrameworkCore`](../CoreEx.EntityFrameworkCore/README.md)** - Entity Framework Core integration consuming the entity contracts and mapping types from this package.
- **[`CoreEx.RefData`](../CoreEx.RefData/README.md)** - Full reference data implementation extending the `IReferenceData` and `ReferenceDataOrchestrator` types defined here.
- **[`CoreEx.UnitTesting`](../CoreEx.UnitTesting/README.md)** - Test helpers and fluent assertion extensions targeting the types and patterns from this package. _(test only)_

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.
