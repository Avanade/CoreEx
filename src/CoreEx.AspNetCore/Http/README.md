# CoreEx.AspNetCore.Http

> Provides `Http.WebApi` — the Minimal API (`IResult`-returning) concrete implementation of the CoreEx `WebApi<TResult>` execution helper — and its dedicated `WebApiInvoker`.

## Overview

`CoreEx.AspNetCore.Http` contains the Minimal API variant of the CoreEx Web API helper. `Http.WebApi` extends the abstract `WebApi<IResult>` base and implements `CreateResult(WebApiResult<IResult>)` to translate a `WebApiResult` into an ASP.NET Core `IResult` — either a typed `Results<T>` value or a `ProblemDetails` result.

It is registered via `services.AddHttpWebApi()` and injected into Minimal API endpoint delegates in the same way that `Mvc.WebApi` is injected into MVC controllers. The HTTP verb methods (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`) are inherited from `WebApi<IResult>` unchanged; only the final `IResult` creation differs.

`AspNetCoreHttpExtensions` provides `IResult`-oriented extension helpers used by the Minimal API endpoint pipeline.

## Key capabilities

- ⚡ **Minimal API integration**: Returns `IResult` types compatible with `Results<T>` and `TypedResults`, enabling full Minimal API support with OpenAPI inference.
- 📄 **ProblemDetails responses**: `CreateResult` maps every `IExtendedException` (and unhandled exception when `ConvertUnhandledExceptionsToProblemDetails` is set) to a `TypedResults.Problem(ProblemDetails)` result.
- 🔗 **Shared execution pipeline**: All verb logic (deserialization, ETag, paging, `Result<T>` unwrapping) is inherited from `WebApi<IResult>` — no duplication with the MVC variant.
- 🔧 **DI registration**: `AddHttpWebApi(services, configure?)` registers `Http.WebApi` as a scoped service with optional startup configuration action.

## Key types

| Type | Description |
|------|-------------|
| **[`Http.WebApi`](./WebApi.cs)** | Minimal API `IResult`-returning `WebApi<IResult>` implementation; maps `WebApiResult<IResult>` to `TypedResults.Ok`, `TypedResults.Created`, `TypedResults.NoContent`, or `TypedResults.Problem`. |
| **[`Http.WebApiInvoker`](./WebApiInvoker.cs)** | `WebApiInvoker<IResult>` singleton used by `Http.WebApi` to wrap invocations with OpenTelemetry spans. |
| [`AspNetCoreHttpExtensions`](./AspNetCoreHttpExtensions.cs) | `IResult` extension helpers for Minimal API result construction used internally by `Http.WebApi`. |

## Related Namespaces

- **[`CoreEx.AspNetCore.Abstractions`](../Abstractions/README.md)** - `WebApi<IResult>` base class providing all verb implementations; `Http.WebApi` supplies only `CreateResult`.
- **[`CoreEx.AspNetCore.Mvc`](../Mvc/README.md)** - Parallel MVC variant returning `IActionResult` instead of `IResult`.
- **[`CoreEx.AspNetCore`](../README.md)** - `AddHttpWebApi()` DI extension is defined in the root package's `CoreExAspNetCoreExtensions.DependencyInjection.cs`.