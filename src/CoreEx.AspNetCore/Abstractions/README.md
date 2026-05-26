# CoreEx.AspNetCore.Abstractions

> Contains the abstract `WebApi<TResult>` base class, `WebApiInvoker<TResult>`, `WebApiResult<TResult>`, `WebApiPagingResult`, and the request/response option interfaces shared by both the MVC and Minimal API `WebApi` implementations.

## Overview

`CoreEx.AspNetCore.Abstractions` is the shared execution kernel for the two concrete `WebApi` flavours. `WebApi<TResult>` provides the complete implementation of every HTTP verb method (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`) in terms of a generic `TResult`, while the concrete subclasses in `Mvc` and `Http` supply only the `CreateResult(WebApiResult<TResult>)` method that maps a `WebApiResult` to either `IActionResult` or `IResult`.

`WebApiResult<TResult>` is the internal transfer object that carries the HTTP response state — optional result value, exception, status code, ETag, location URI, and paging metadata — between the execution pipeline and `CreateResult`. `WebApiInvoker<TResult>` wraps each invocation with an OpenTelemetry span (via `InvokerBase<WebApi<TResult>>`) and structured logging.

The request/response option types (`IWebApiRequestOptions`, `IWebApiResponseOptions`, `WebApiOptionsBase`) define the per-operation configuration surface that controllers pass when calling `WebApi` helpers.

## Key capabilities

- 🔀 **Verb method implementations**: `GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync` all live in `WebApi<TResult>` as complete implementations; subclasses need only implement `CreateResult`.
- 📦 **Request body deserialization**: `PostAsync`/`PutAsync`/`PatchAsync` deserialize the request body with `JsonSerializer`, enforcing `Content-Type` and returning `400 Bad Request` with a descriptive `ProblemDetails` on failure.
- 🏷️ **ETag / If-Match concurrency**: `PutAsync` and `PatchAsync` read the `If-Match` request header and stamp the `ETag` response header from the entity; `PatchAsync` additionally validates the ETag before applying the patch.
- 📃 **Paging header stamping**: When an operation returns an `ICollectionResult` / `ItemsResult<T>`, `WebApiPagingResult` stamps the `x-paging-skip`, `x-paging-take`, `x-paging-count`, and `x-paging-page` response headers.
- ⚡ **WebApiInvoker tracing**: Every `InvokeAsync` call is wrapped by `WebApiInvoker<TResult>`, which creates an `Activity` span tagged with operation result and error details.
- 🔌 **Extension points**: `WebApiOptionsBase` exposes `AlternateStatusCode`, `AlternateOnDefault`, and `OnCreateBeforeResponseAsync` hooks for customizing per-operation behavior without subclassing `WebApi<TResult>`.

## Key types

| Type | Description |
|------|-------------|
| _[`WebApi<TResult>`](./WebApi.cs)_ | Abstract base providing all HTTP verb implementations; `TResult` is either `IActionResult` (MVC) or `IResult` (Minimal API). |
| _[`WebApiBase`](./WebApiBase.cs)_ | Root abstract base holding `JsonSerializerOptions`, `Logger`, `ExecutionContext`, and `ConvertUnhandledExceptionsToProblemDetails`. |
| **[`WebApiResult<TResult>`](./WebApiResult.cs)** | Internal transfer object carrying `Result`, `Exception`, `StatusCode`, `ETag`, `Location`, `BypassExceptionLogging`, and `HttpResponse`. |
| **[`WebApiPagingResult`](./WebApiPagingResult.cs)** | Carries `PagingResult` metadata stamped onto the response as `x-paging-*` headers. |
| **[`WebApiInvoker<TResult>`](./WebApiInvoker.cs)** | `InvokerBase<WebApi<TResult>>` creating OpenTelemetry spans and structured log entries for every `WebApi` invocation. |
| [`WebApiHeader`](./WebApiHeader.cs) | Constants for internal response headers used by `WebApi` (e.g. `x-messages`). |
| [`IWebApiRequestOptions`](./IWebApiRequestOptions.cs) | Per-operation request options: `HttpRequest`, `RequestBodyType`, and request body handling configuration. |
| [`IWebApiResponseOptions`](./IWebApiResponseOptions.cs) | Per-operation response options: `StatusCode`, `AlternateStatusCode`, `Location` URI, and `ETag` configuration. |
| [`WebApiOptionsBase`](./WebApiOptionsBase.cs) | Abstract base for concrete options types combining request and response option interfaces. |

## Related Namespaces

- **[`CoreEx.AspNetCore`](../README.md)** - The root package; `ExecutionContextMiddleware` and `ExceptionHandlingMiddleware` provide the per-request setup that `WebApi<TResult>` assumes is in place.
- **[`CoreEx.AspNetCore.Mvc`](../Mvc/README.md)** - `Mvc.WebApi` extends `WebApi<IActionResult>` with MVC-specific `CreateResult` and `WebApiInvoker`.
- **[`CoreEx.AspNetCore.Http`](../Http/README.md)** - `Http.WebApi` extends `WebApi<IResult>` with Minimal API-specific `CreateResult` and `WebApiInvoker`.
- **[`CoreEx.Invokers`](../../CoreEx/Invokers/README.md)** - `WebApiInvoker<TResult>` extends `InvokerBase<WebApi<TResult>>` using the standard tracing/logging pipeline.