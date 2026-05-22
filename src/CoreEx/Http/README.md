# CoreEx.Http

> Provides shared HTTP abstractions — `ProblemDetails`, `ProblemDetailsException`, standard header/query-string names, and paging-result HTTP header conventions — used by both client and server HTTP layers without requiring ASP.NET Core dependencies.

## Overview

`CoreEx.Http` defines the portable HTTP contract types that are shared between typed HTTP client code and the ASP.NET Core server layer, deliberately avoiding a hard dependency on `Microsoft.AspNetCore.*`. This allows `CoreEx.Database`, service-bus subscribers, and other non-HTTP projects to reference paging or problem-details types without pulling in the web stack.

`ProblemDetails` follows [RFC 7807](https://tools.ietf.org/html/rfc7807) and adds a `GetValidationErrors()` helper for extracting field-level errors from the `errors` extension property. `ProblemDetailsException` wraps a `ProblemDetails` into an exception and provides `ToException<TException>()` to map it back to a typed CoreEx semantic exception. `HttpNames` centralizes all standard query-string and response-header names used for paging so that client and server agree on field names without duplication.

The `Abstractions` sub-namespace defines additional lightweight types and interfaces used across both client and server without an ASP.NET Core reference.

## Key capabilities

- 📄 **RFC 7807 Problem Details**: `ProblemDetails` represents a typed HTTP API error with `type`, `title`, `status`, `detail`, `instance`, and an `extensions` dictionary; `GetValidationErrors()` extracts field-level error messages from the standard `errors` key.
- 🔁 **Problem Details ↔ exception round-trip**: `ProblemDetailsException.ToException<TException>()` maps a received `ProblemDetails` back to a typed CoreEx `ExtendedException<T>`, preserving `Detail`, `StatusCode`, `ShouldBeLogged`, and optional message items.
- 🏷️ **Standard HTTP names**: `HttpNames` provides configurable static properties for the paging query-string names (`$skip`, `$take`, `$count`, `$page`) and response-header names (`x-paging-skip`, `x-paging-take`, `x-paging-count`, `x-paging-page`), allowing application-wide renaming from a single location.
- 🔒 **Idempotency key header**: `HttpNames.IdempotencyKeyHeaderName` (`x-idempotency-key`) is the agreed header name for idempotency key propagation between clients and the `[IdempotencyKey]` server filter.
- 🌐 **Portable HTTP result types**: `HttpResult` and `HttpResult<T>` wrap an `HttpResponseMessage` alongside a deserialized value or `ProblemDetails`, enabling clients to inspect both the raw response and the typed outcome without re-parsing.

## Key types

| Type | Description |
|------|-------------|
| **[`ProblemDetails`](./Abstractions/ProblemDetails.cs)** | RFC 7807 problem-details record with `Type`, `Title`, `Status`, `Detail`, `Instance`, and an `Extensions` dictionary; includes `GetValidationErrors(string? key)`. |
| **[`ProblemDetailsException`](./ProblemDetailsException.cs)** | Exception wrapping a `ProblemDetails`; provides `ToException<TException>()` to convert it back to a typed CoreEx semantic exception. |
| **[`HttpNames`](./HttpNames.cs)** | Static class of configurable string properties for all standard paging query-string names, response-header names, and the idempotency-key header name. |
| **[`HttpResult`](./HttpResult.cs)** | Readonly struct pairing an `HttpResponseMessage` with either a deserialized value or a `ProblemDetails` from the response; exposes `IsSuccess`, `StatusCode`, and `ThrowOnError()`. |
| **[`HttpResult<T>`](./HttpResultT.cs)** | Typed variant of `HttpResult` adding a strongly-typed `Value` property for the deserialized response body. |
| [`IExtendedHttpClient`](./Abstractions/IExtendedHttpClient.cs) | Interface implemented by `TypedHttpClientBase<TSelf>` for type-safe HTTP client wrappers; exposes `SendAsync`, `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync` returning `HttpResult<T>`. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - Semantic exception types (`ValidationException`, `NotFoundException`, etc.) are the target for `ProblemDetailsException.ToException<TException>()` mapping.
- **[`CoreEx.AspNetCore`](../../CoreEx.AspNetCore/README.md)** - Server-side `WebApi` helpers consume `ProblemDetails` and `HttpNames` and translate `Result`/exceptions to `ProblemDetails` HTTP responses.
- **[`CoreEx.Http`](../../CoreEx.Http/README.md)** - The separate `CoreEx.Http` NuGet package builds on these abstractions to provide `TypedHttpClientBase<TSelf>`, retry policies, and client-side HTTP result handling.

## Additional Resources

- [RFC 7807 — Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807) - The specification that `ProblemDetails` implements.