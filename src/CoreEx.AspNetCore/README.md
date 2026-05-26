# CoreEx.AspNetCore

> Provides the ASP.NET Core integration layer for CoreEx: the `WebApi` execution helper (MVC and Minimal API variants), middleware for `ExecutionContext` scoping and exception-to-ProblemDetails translation, idempotency key handling, health check configuration, and OpenAPI/NSwag extensions.

## Overview

`CoreEx.AspNetCore` is the server-side HTTP integration package. It bridges CoreEx domain logic — semantic exceptions, `Result<T>` pipelines, paging, entity contracts — into ASP.NET Core HTTP responses, following RFC 7807 ProblemDetails conventions throughout.

The centrepiece is `WebApi`, which controllers and Minimal API handlers inject and call (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`). `WebApi` handles request body deserialization, `ETag`/`If-Match` concurrency checking, `Result<T>` unwrapping, paging header stamping, info/warning message header propagation, and exception-to-`ProblemDetails` translation — all in a single invocation boundary so individual action methods stay focused on business logic.

Two concrete `WebApi` implementations ship: `Mvc.WebApi` returning `IActionResult` for MVC controllers, and `Http.WebApi` returning `IResult` for Minimal API endpoints. Both share the same abstract base (`WebApi<TResult>`) and invoker infrastructure.

## Key capabilities

- 🌐 **WebApi execution helper**: `WebApi` encapsulates GET/HEAD, POST (with idempotency), PUT, PATCH (JSON Merge Patch), and DELETE patterns with standardized request validation, deserialization, ETag checks, and response serialization.
- 📄 **RFC 7807 ProblemDetails**: All `IExtendedException` types (`ValidationException`, `NotFoundException`, `ConcurrencyException`, etc.) are translated to typed `ProblemDetails` responses with appropriate HTTP status codes and optional field-level `errors` extension.
- 🔁 **JSON Merge Patch (PATCH)**: `WebApi.PatchAsync` reads the request body as a merge-patch document, deserializes the current entity via a provided function, applies the patch, and re-validates before saving.
- ⚙️ **ExecutionContext middleware**: `ExecutionContextMiddleware` resolves the DI-scoped `ExecutionContext` per request, wires `ServiceProvider`, and propagates info/warning messages to response headers on completion.
- 🛡️ **Idempotency key middleware**: `IdempotencyKeyMiddleware` + `[IdempotencyKey]` attribute intercepts `POST` operations, checks a pluggable `IIdempotencyProvider` for a prior response, and either replays the cached result or executes and caches the new one.
- ♥ **Health check endpoints**: `HealthCheckOptions` configures `/health/live`, `/health/startup`, `/health/ready`, and `/health/detail` endpoints with tag-based filtering, JSON detail output, and per-endpoint enable/disable.
- 🏷️ **MVC attributes**: `[PagingAttribute]`, `[QueryAttribute]`, `[AcceptsAttribute]`, `[ProducesNotFoundProblem]`, and `[IdempotencyKey]` enrich OpenAPI/NSwag output without cluttering action signatures.
- 🔍 **OpenAPI extensions**: `OpenApiOptions` and `AspNetCoreExtensions` wire NSwag operation processors that read CoreEx MVC attributes and add paging/query parameters, idempotency-key headers, and standard error response types to the generated spec.
- 📡 **OpenTelemetry**: `CoreExAspNetCoreExtensions.AddCoreExAspNetCoreOpenTelemetry` wires CoreEx `WebApiInvoker` activity sources into the OTEL tracer provider.

## Key types

| Type | Description |
|------|-------------|
| **[`Mvc.WebApi`](./Mvc/WebApi.cs)** | MVC `IActionResult`-returning Web API helper; injected into controllers for typed GET/POST/PUT/PATCH/DELETE handling. |
| **[`Http.WebApi`](./Http/WebApi.cs)** | Minimal API `IResult`-returning Web API helper; used in `MapGet`/`MapPost` etc. endpoint delegates. |
| **[`ExecutionContextMiddleware`](./ExecutionContextMiddleware.cs)** | Per-request `ExecutionContext` scoping, optional custom configuration, and info/warning message response header propagation. |
| **[`ExceptionHandlingMiddleware`](./ExceptionHandlingMiddleware.cs)** | `UseExceptionHandler` callback that converts unhandled exceptions to `ProblemDetails` using the ambient `WebApi` instance. |
| **[`IdempotencyKeyMiddleware`](./Idempotency/IdempotencyKeyMiddleware.cs)** | Middleware that checks `[IdempotencyKey]`-marked endpoints and delegates to `IIdempotencyProvider` for replay/cache. |
| **[`HybridCacheIdempotencyProvider`](./Idempotency/HybridCacheIdempotencyProvider.cs)** | `IIdempotencyProvider` backed by `IHybridCache`; stores serialized responses keyed by `x-idempotency-key` with configurable expiry. |
| **[`HealthCheckOptions`](./HealthChecks/HealthCheckOptions.cs)** | Configures live/startup/ready/detail health check endpoint paths, tag filters, and JSON detail writer; registered via `MapCoreExHealthChecks()`. |
| **[`WebApiOptions`](./WebApiOptions.cs)** | Per-request options controlling response status code, `ETag`, location header, paging, and alternate status code behavior. |
| **[`OpenApiOptions`](./OpenApiOptions.cs)** | NSwag/OpenAPI document-processor and operation-processor registration for CoreEx paging, query, accepts, and idempotency attributes. |
| [`WebApiBase`](./Abstractions/WebApiBase.cs) | Abstract base for both `WebApi<TResult>` variants: holds `JsonSerializerOptions`, `Logger`, `ExecutionContext`, and `ConvertUnhandledExceptionsToProblemDetails` flag. |

## Namespaces

| Namespace | Description |
|-----------|-------------|
| [**`Abstractions`**](./Abstractions/README.md) | Abstract `WebApi<TResult>` base, `WebApiInvoker<TResult>`, `WebApiResult<TResult>`, `WebApiPagingResult`, and request/response option interfaces. |
| [**`HealthChecks`**](./HealthChecks/README.md) | `HealthCheckOptions` and endpoint registration extensions for live/startup/ready/detail health probes. |
| [**`Http`**](./Http/README.md) | Minimal API `WebApi` (`IResult`-returning) and its `WebApiInvoker`. |
| [**`Idempotency`**](./Idempotency/README.md) | `IIdempotencyProvider`, `IdempotencyKeyMiddleware`, `HybridCacheIdempotencyProvider`, and `IdempotencyStatus`. |
| [**`Mvc`**](./Mvc/README.md) | MVC `WebApi` (`IActionResult`-returning), MVC attributes (`[PagingAttribute]`, `[QueryAttribute]`, `[IdempotencyKey]`, `[Accepts]`, `[ProducesNotFoundProblem]`), and `WebApiInvoker`. |

## Related Namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Semantic exceptions, `ExecutionContext`, `Result<T>`, and `PagingArgs` are the domain primitives translated by this package into HTTP responses.
- **[`CoreEx.Http`](../CoreEx.Http/README.md)** - Client-side `TypedHttpClientBase<TSelf>` consumes `ProblemDetails` responses produced by this package; `HttpNames` defines the shared header/query-string constants.
- **[`CoreEx.Validation`](../CoreEx.Validation/README.md)** - `ValidationException` raised by validators is translated to `422 Unprocessable Entity` with a field-level `errors` extension in `ProblemDetails`.

## Additional Resources

- [RFC 7807 — Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807) - The specification implemented by `WebApi` exception handling.
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks) - The underlying infrastructure extended by `HealthCheckOptions`.