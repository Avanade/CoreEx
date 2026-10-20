# CoreEx.Http

> Provides shared HTTP abstractions — `ProblemDetails`, `ProblemDetailsException`, standard header/query-string names, and paging-result HTTP header conventions — used by both client and server HTTP layers without requiring ASP.NET Core dependencies.

## Overview

`CoreEx.Http` defines the portable HTTP contract types that are shared between typed HTTP client code and the ASP.NET Core server layer, deliberately avoiding a hard dependency on `Microsoft.AspNetCore.*`. This allows `CoreEx.Database`, service-bus subscribers, and other non-HTTP projects to reference paging or problem-details types without pulling in the web stack.

`ProblemDetails` follows [RFC 7807](https://tools.ietf.org/html/rfc7807) and adds a `GetValidationErrors()` helper for extracting field-level errors from the `errors` extension property. `ProblemDetailsException` wraps a `ProblemDetails` into an exception and provides `ToException<TException>()` to map it back to a typed CoreEx semantic exception. `HttpNames` centralizes all standard query-string and response-header names used for paging so that client and server agree on field names without duplication.

The `Abstractions` sub-namespace defines additional lightweight types and interfaces used across both client and server without an ASP.NET Core reference.

## Key capabilities

- 📄 **RFC 7807 Problem Details**: `ProblemDetails` represents a typed HTTP API error with `type`, `title`, `status`, `detail`, `instance`, and an `extensions` dictionary; `GetValidationErrors()` extracts field-level error messages from the standard `errors` key.
- 🔁 **Problem Details ↔ exception round-trip**: `ProblemDetailsException.ToException<TException>()` maps a received `ProblemDetails` back to a typed CoreEx `ExtendedException<T>`, preserving `Detail`, `StatusCode`, `ShouldBeLogged`, and optional message items.
- 🏷️ **Standard HTTP names**: `HttpNames` provides configurable static properties for the paging query-string names (`$skip`, `$take`, `$count`) and response-header names (`X-Paging-Skip`, `X-Paging-Take`, `X-Paging-Total-Count`), allowing application-wide renaming from a single location.
- 🔒 **Idempotency key header**: `HttpNames.IdempotencyKeyHeaderName` (`Idempotency-Key`) is the agreed header name for idempotency key propagation between clients and the `[IdempotencyKey]` server filter.
- 🌐 **Portable HTTP result types**: `HttpResult` and `HttpResult<T>` wrap an `HttpResponseMessage` alongside a deserialized value or `ProblemDetails`, enabling clients to inspect both the raw response and the typed outcome without re-parsing.

## Key types

| Type | Description |
|------|-------------|
| **[`ProblemDetails`](./Abstractions/ProblemDetails.cs)** | RFC 7807 problem-details record with `Type`, `Title`, `Status`, `Detail`, `Instance`, and an `Extensions` dictionary; includes `GetValidationErrors(string? key)`. |
| **[`ProblemDetailsException`](./ProblemDetailsException.cs)** | Exception wrapping a `ProblemDetails`; provides `ToException<TException>()` to convert it back to a typed CoreEx semantic exception. |
| **[`HttpNames`](./HttpNames.cs)** | Static class of configurable string properties for all standard paging query-string names, response-header names, and the idempotency-key header name. |
| **[`HttpRequestBuilder`](./HttpRequestBuilder.cs)** | Single-use fluent HTTP request builder returned by `client.Send(...)` or the verb shortcuts; executes requests and manages automatic response disposal across typed deserialization, ROP result conversion, validation, and error-handling methods. |
| **[`ValidatedHttpRequestBuilder<T>`](./ValidatedHttpRequestBuilderT.cs)** | Wrapper returned by `builder.WithValidator(validator)` that executes the request, validates the deserialized response value, and automatically disposes the response. |
| **[`ValidatedHttpResponse<T>`](./ValidatedHttpResponseT.cs)** | Thin wrapper returned by `HttpResponseMessage.WithValidator(validator)`; validates successful non-null response values when consumed via `ToResult*` or `GetValue*`. Does not own the response. |
| **[`HttpResult`](./HttpResult.cs)** | Readonly struct pairing an `HttpResponseMessage` with either a deserialized value or a `ProblemDetails` from the response; exposes `IsSuccess`, `StatusCode`, and `ThrowOnError()`. |
| **[`HttpResult<T>`](./HttpResultT.cs)** | Typed variant of `HttpResult` adding a strongly-typed `Value` property for the deserialized response body. |
| [`IExtendedHttpClient`](./Abstractions/IExtendedHttpClient.cs) | Interface implemented by `TypedHttpClientBase<TSelf>` for type-safe HTTP client wrappers; exposes `SendAsync`, `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync` returning `HttpResult<T>`. |

## Sending requests

The `CoreEx` namespace provides `HttpClient.Send(method, uri)` and the `Get`, `Post`, `Put`, `Patch`, and `Delete`
shortcuts returning an `HttpRequestBuilder`, as well as direct asynchronous `SendAsync` overloads.
Relative URIs are resolved against `HttpClient.BaseAddress` before the configuration callback, so
`WithQuery`, `WithPaging`, and header helpers can be applied there. Absolute URIs do not require a base address.

### Fluent request execution and automatic response disposal

`client.Send(...)` and the verb shortcuts only build a request. Calling any terminal consumption
method executes the request and automatically disposes the underlying `HttpResponseMessage`:

```csharp
// Single await with automatic response disposal:
var product = await client.Get("api/products/123")
    .GetValueAsync<Product>(cancellationToken).ConfigureAwait(false);

// Fluent query, paging, and headers directly chained:
var products = await client.Get("api/products")
    .WithQuery("startswith(sku, 'ABC')")
    .WithPaging(take: 10)
    .GetValueAsync<ProductLite[]>(cancellationToken).ConfigureAwait(false);

// Post JSON payload with typed response:
var created = await client.Post("api/baskets")
    .WithJson(newBasket)
    .GetValueAsync<Basket>(cancellationToken).ConfigureAwait(false);

// With custom status mapping (short-circuits validation/deserialization on match):
var customer = await client.Send(HttpMethod.Get, "api/customers/123")
    .WithStatusResult(HttpStatusCode.NotFound, () => Result.NotFoundError())
    .WithValidator(customerValidator)
    .ToResultAsync(cancellationToken).ConfigureAwait(false);

// With response value validation:
var validated = await client.Send(HttpMethod.Get, "api/products/123")
    .WithValidator(productValidator)
    .GetValueAsync(cancellationToken).ConfigureAwait(false);

// Where raw HttpResponseMessage is needed directly (caller owns disposal):
using var response = await client.Send(HttpMethod.Get, "api/products")
    .GetResponseAsync(cancellationToken).ConfigureAwait(false);
```

### Request bodies and single-use execution

Use `WithJson<T>(value, mediaType: ..., jsonSerializerOptions: ...)` for a JSON body. It uses
`JsonDefaults.SerializerOptions` and `application/json` by default. A null value sends JSON `null`;
a string sends a JSON string, not raw text. For explicit raw text, form data, multipart content, or
streams, use `WithContent(HttpContent)`. Matching helpers are available on `HttpRequestMessage`.

Only one body may be assigned: repeated `WithJson`, repeated `WithContent`, or mixing the two throws
`InvalidOperationException`. Low-level `Configure(...)` callbacks may supply a body only when no body
has already been assigned; replacing or removing an existing body is rejected before sending.
Callbacks may still modify content headers. `Patch(uri)` only selects the HTTP verb; use
`.WithJson(patch).WithMergePatchJsonContentType()` to explicitly select JSON Merge Patch.

Each builder allows exactly one execution attempt, even if it fails or is cancelled. A second or
concurrent terminal call, or configuration after execution begins, throws `InvalidOperationException`.
Configure the builder before executing it; concurrent configuration is not supported. The validated
builder shares the same single-use request. Retries within the HTTP resilience pipeline are not new
builder executions.

Accepted `WithContent` content is disposed with the request during execution, including failures.
The caller must dispose content if assignment is rejected or the builder is never executed.
JSON content is created when execution begins. Terminal consumption methods dispose the response;
`GetResponseAsync` and `SendAsync` return a raw response that the caller must dispose.

### Direct asynchronous methods

For direct response inspection, `SendAsync` returns the raw `HttpResponseMessage` (which the caller must dispose):

```csharp
using var response = await client.SendAsync(HttpMethod.Get, "api/products",
    r => r.WithQuery("startswith(sku, 'ABC')").WithPaging(take: 10), cancellationToken).ConfigureAwait(false);
await response.ThrowOnErrorAsync(cancellationToken).ConfigureAwait(false);
```

## Reading response content

`HttpResponseMessage` provides extensions for reading typed JSON or raw string content:

- **Typed JSON**: `GetValueAsync<T>` and `GetValueOrDefaultAsync<T>` deserialize the body using `JsonDefaults.SerializerOptions` and throw on HTTP error (with `GetValueAsync<T>` also rejecting empty or null bodies). Their ROP counterparts are `ToResultAsync<T>` and `ToResultOrDefaultAsync<T>`.
- **Raw string**: `GetStringAsync` reads the response as a string, returning `null` for empty content or 204 No Content, and throws on HTTP error. Its ROP counterpart is `ToStringResultAsync`, returning `Result<string?>`.

```csharp
using var response = await client.SendAsync(HttpMethod.Get, "api/products/123", cancellationToken).ConfigureAwait(false);
var content = await response.GetStringAsync(cancellationToken).ConfigureAwait(false);
```

## Validating response values

Use `WithValidator(validator)` with an `IValidator<T>` for opt-in response-contract validation across two patterns:

### 1. Fluent request builder (preferred)

Chaining `.WithValidator(...)` onto `HttpRequestBuilder` returns a `ValidatedHttpRequestBuilder<T>`. Calling any terminal method executes the request, validates the deserialized response, and **automatically disposes** the underlying `HttpResponseMessage`:

```csharp
// Fluent request with validation and automatic response disposal:
var product = await client.Send(HttpMethod.Get, "api/products/123")
    .WithValidator(productValidator)
    .GetValueAsync(cancellationToken).ConfigureAwait(false);

// ROP result pipeline variant:
var result = await client.Send(HttpMethod.Get, "api/products/123")
    .WithValidator(productValidator)
    .ToResultAsync(cancellationToken).ConfigureAwait(false);
```

### 2. Existing response message

Calling `response.WithValidator(...)` on an existing `HttpResponseMessage` returns a `ValidatedHttpResponse<T>`. The wrapper does not own or dispose the response — the caller **retains ownership** and must dispose it:

```csharp
using var response = await client.SendAsync(HttpMethod.Get, "api/products/123", cancellationToken).ConfigureAwait(false);
var product = await response.WithValidator(productValidator).GetValueAsync(cancellationToken).ConfigureAwait(false);
```

### Validation semantics

- **Returned value**: Both wrappers return the validation result's `Value`, preserving any value transformations or normalizations performed during validation.
- **Status interception**: Calling `.WithStatusResult(statusCode, result)` (or `.WithErrorStatus(...)`) on either builder or validated response short-circuits execution before deserialization and validation when the HTTP status matches, returning the mapped result directly.
- **Required vs optional**: `GetValueAsync` / `ToResultAsync` reject null or empty bodies both before and after validation. `GetValueOrDefaultAsync` / `ToResultOrDefaultAsync` allow null responses and skip validation when no body is present.
- **Error handling**: Reported validation errors are packaged into an `HttpRequestException` with the inner `ValidationException` preserving diagnostics. Under standard CoreEx exception handling, upstream validation failures map to internal **500** errors rather than caller **400** errors.
- **Full guide**: See [the full usage guide and inline client example](../README.md#validating-http-response-values).

## Related Namespaces

- **[`CoreEx`](../README.md)** - Semantic exception types (`ValidationException`, `NotFoundException`, etc.) are the target for `ProblemDetailsException.ToException<TException>()` mapping.
- **[`CoreEx.AspNetCore`](../../CoreEx.AspNetCore/README.md)** - Server-side `WebApi` helpers consume `ProblemDetails` and `HttpNames` and translate `Result`/exceptions to `ProblemDetails` HTTP responses.
- **[`CoreEx.Http`](../../CoreEx.Http/README.md)** - The separate `CoreEx.Http` NuGet package builds on these abstractions to provide `TypedHttpClientBase<TSelf>`, retry policies, and client-side HTTP result handling.

## Additional Resources

- [RFC 7807 — Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807) - The specification that `ProblemDetails` implements.
