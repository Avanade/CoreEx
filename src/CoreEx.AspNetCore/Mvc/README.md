# CoreEx.AspNetCore.Mvc

> Provides `Mvc.WebApi` — the MVC (`IActionResult`-returning) concrete `WebApi<TResult>` implementation — along with MVC action attributes for paging, query, accepts, idempotency, and not-found responses that enrich OpenAPI/NSwag output.

## Overview

`CoreEx.AspNetCore.Mvc` contains the MVC-specific pieces of the CoreEx Web API layer. `Mvc.WebApi` extends `WebApi<IActionResult>` and implements `CreateResult(WebApiResult<IActionResult>)` to map every outcome — value, `null` (alternate status), or exception — to the appropriate `IActionResult`: `OkObjectResult`, `CreatedResult`, `NoContentResult`, or an RFC 7807 `ObjectResult(ProblemDetails)` with the correct HTTP status code.

The namespace also provides a set of small, targeted MVC attributes. These carry metadata that NSwag operation processors read to inject paging query parameters, JSON schema accept types, idempotency-key request headers, and standard error response types (404, 409, etc.) into the generated OpenAPI document — without requiring controllers to declare every parameter and response code explicitly.

## Key capabilities

- 🎯 **MVC result creation**: `Mvc.WebApi.CreateResult` translates `WebApiResult<IActionResult>` to the full range of `IActionResult` types, including field-level `ValidationException` → `400 Bad Request` with `errors` extension, and `ConcurrencyException` → `409 Conflict`.
- 📑 **Paging attribute**: `[PagingAttribute]` marks operations that accept paging arguments via query string without declaring `PagingArgs` as an explicit method parameter; NSwag reads this to add `$skip`, `$take`, `$count`, `$page` query parameters to the spec.
- 🔍 **Query attribute**: `[QueryAttribute]` marks operations that accept OData-style `$filter` / `$orderby` query arguments; NSwag adds the corresponding parameters.
- ✅ **Accepts attribute**: `[AcceptsAttribute<T>]` declares the request body `Content-Type` and schema type for NSwag, replacing the need for `[Consumes]` with schema inference.
- 🚫 **Not-found response attribute**: `[ProducesNotFoundProblem]` adds a `404 application/problem+json` response entry to the NSwag spec for operations that can return `NotFoundException`.
- 🔑 **Idempotency key attribute**: `[IdempotencyKey]` marks a `POST` action as idempotent; `IdempotencyKeyMiddleware` reads this from endpoint metadata; NSwag adds the `x-idempotency-key` header parameter to the spec.
- 🔧 **DI registration**: `AddMvcWebApi(services, configure?)` registers `Mvc.WebApi` as a scoped service.

## Key types

| Type | Description |
|------|-------------|
| **[`Mvc.WebApi`](./WebApi.cs)** | MVC `IActionResult`-returning `WebApi<IActionResult>` implementation; maps `WebApiResult` to `OkObjectResult`, `CreatedResult`, `NoContentResult`, `ObjectResult(ProblemDetails)`, etc. |
| **[`Mvc.WebApiInvoker`](./WebApiInvoker.cs)** | `WebApiInvoker<IActionResult>` singleton providing OpenTelemetry spans for MVC `WebApi` invocations. |
| **[`PagingAttribute`](./PagingAttribute.cs)** | Marks an operation as supporting `PagingArgs` via query string; NSwag reads `SupportsCount` to conditionally add `$count`. |
| **[`QueryAttribute`](./QueryAttribute.cs)** | Marks an operation as accepting OData-style query arguments; NSwag adds `$filter`/`$orderby` parameters. |
| **[`AcceptsAttribute`](./AcceptsAttribute.cs)** | Declares the request body content type; generic `AcceptsAttribute<T>` adds schema type inference for NSwag. |
| **[`ProducesNotFoundProblemAttribute`](./ProducesNotFoundProblemAttribute.cs)** | Adds `404 application/problem+json` to the NSwag operation response list. |
| **[`IdempotencyKeyAttribute`](./IdempotencyKeyAttribute.cs)** | Marks a POST action as idempotent; read by `IdempotencyKeyMiddleware` and NSwag processor to add `x-idempotency-key` header. |

## Related Namespaces

- **[`CoreEx.AspNetCore.Abstractions`](../Abstractions/README.md)** - `WebApi<IActionResult>` base class providing all verb implementations inherited by `Mvc.WebApi`.
- **[`CoreEx.AspNetCore.Idempotency`](../Idempotency/README.md)** - `IdempotencyKeyMiddleware` reads `[IdempotencyKeyAttribute]` from endpoint metadata to trigger idempotency handling.
- **[`CoreEx.AspNetCore.Http`](../Http/README.md)** - Parallel Minimal API variant returning `IResult` instead of `IActionResult`.
- **[`CoreEx.AspNetCore`](../README.md)** - `AddMvcWebApi()` DI extension and NSwag `OpenApiOptions` are defined in the root package.