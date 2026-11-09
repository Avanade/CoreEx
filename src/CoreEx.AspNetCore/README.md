# CoreEx.AspNetCore

> Provides the ASP.NET Core integration layer for CoreEx: the `WebApi` execution helper (MVC and Minimal API variants), middleware for `ExecutionContext` scoping and exception-to-ProblemDetails translation, idempotency key handling, health check configuration, and OpenAPI/NSwag extensions.

## Overview

`CoreEx.AspNetCore` is the server-side HTTP integration package. It bridges CoreEx domain logic — semantic exceptions, `Result<T>` pipelines, paging, entity contracts — into ASP.NET Core HTTP responses, following RFC 7807 ProblemDetails conventions throughout.

The centrepiece is `WebApi`, which controllers and Minimal API handlers inject and call (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`). `WebApi` handles request body deserialization, `ETag`/`If-Match` concurrency checking, `Result<T>` unwrapping, paging header stamping, info/warning message header propagation, and exception-to-`ProblemDetails` translation — all in a single invocation boundary so individual action methods stay focused on business logic.

Two concrete `WebApi` implementations ship: `Mvc.WebApi` returning `IActionResult` for MVC controllers, and `Http.WebApi` returning `IResult` for Minimal API endpoints. Both share the same abstract base (`WebApi<TResult>`) and invoker infrastructure.

For HTTP adapters with a pre-processing exception boundary, `webApi.CreateErrorResult(request, exception, cancellationToken)` creates an error result using the same instance's classification, logging, ProblemDetails/custom representation and unexpected-exception policy. Own-operation cancellation propagates. The caller executes the returned result; errors during that execution are not caught and rendered again.

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
| **[`HybridCacheIdempotencyProvider`](./Idempotency/HybridCacheIdempotencyProvider.cs)** | `IIdempotencyProvider` backed by `IHybridCache`; stores serialized responses keyed by `Idempotency-Key` with configurable expiry. |
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

## Specialized JSON representations

`WebApiBase.DefaultJsonMediaType` is an instance setting shared by MVC and Minimal WebApi. It defaults to `application/json`. Generated JSON responses resolve their content type as `operation ContentType override -> instance DefaultJsonMediaType -> application/json`. Use the generic `WithContentType(...)` option extension for an operation override; it works with GET's `WebApiOptionsBase` as well as typed options and survives option copies, including JSON Merge Patch's GET-to-update wrapping. Passing `null` clears an operation override.

```csharp
builder.Services
    .AddMvcWebApi((_, webApi) => webApi.DefaultJsonMediaType = "application/json")
    .AddHttpWebApi((_, webApi) => webApi.DefaultJsonMediaType = "application/json");

// Use the same extension with either MVC or Minimal WebApi.
return webApi.GetAsync(request, (options, ct) =>
{
    options.WithContentType("application/vnd.example.resource+json");
    return service.GetAsync(id, ct);
}, cancellationToken: cancellationToken);
```

These settings label JSON already serialized using that instance's `JsonSerializerOptions`. They do not select a serializer, change request-body formats, perform content negotiation, enable XML, or update OpenAPI response metadata. Assignments reject empty/malformed media types, wildcards, duplicate charset parameters, and charsets other than UTF-8. Other valid media-type parameters are preserved. Explicit `IActionResult`/`IResult` values are returned untouched, and generated HEAD, 204, and 304 responses remain bodyless. Success media types never relabel framework ProblemDetails or the explicit plain-text unsupported-merge-patch response.

### Opt-in Accept validation

Call `options.WithAccept()` to assert immediately that the client accepts the operation's effective JSON media type (`ContentType` override, otherwise the owning instance's `DefaultJsonMediaType`). Like `WithIfMatchRequired()`, this is a point-of-use guard: set any override first and check before calling the service, especially for mutations. It is not an automatic policy, and it does not select or change a representation.

```csharp
return webApi.GetAsync(request, (options, ct) =>
{
    options.WithContentType("application/vnd.example.resource+json").WithAccept();
    return service.GetAsync(id, ct);
}, cancellationToken: cancellationToken);
```

Missing or empty Accept headers permit any representation. Matching supports exact types (case-insensitive), `type/*`, `*/*`, media parameters, and quality values; the most specific matching range controls acceptance, so `application/json;q=0, */*;q=1` rejects an ordinary JSON response. A vendor `+json` type is not interchangeable with `application/json`. Generated JSON uses UTF-8, including for charset matching. Incompatible requests throw a `ValidationException` with status **406**, malformed headers use **400**, and existing exception rendering remains responsible for the error body independently of Accept. The guard preserves existing `Vary` values and adds `Accept`.

The generic guard works on GET's base options and all typed options; copies retain the owning WebApi default, including JSON Merge Patch's GET-to-update copy. Independently constructed options use `application/json` unless overridden. Changing the content type after checking requires another check. Explicit results are not inspected, and the guard does not invent bodies for HEAD/204/304; only opt in where validating the declared JSON representation makes sense. No call means no Accept enforcement and no behavior change. For negotiated formats or serializers, use native ASP.NET Core endpoints/results rather than WebApi.

### Override only exception representation

Both `Mvc.WebApi` and `Http.WebApi` expose `protected virtual CreateResultForException(HttpContext httpContext, Exception exception, IExtendedException? extendedException)`, returning `IActionResult` and `IResult` respectively. The original exception and request/response context are available; `extendedException` is the classified failure, or `null` for an unexpected failure. Default implementations preserve their respective ProblemDetails factories, validation details, extensions, and diagnostic configuration.

Propagation policy, classification, logging decisions, logging bypass, and applicable `Retry-After` headers execute before the hook. An override must not log the same failure again. A renderer failure propagates rather than being fed back into the renderer. The following protocol-neutral examples use a separate error media type and serialize with the configured instance options without exposing exception messages, details, or stack traces. Place imports in the consuming project's `GlobalUsing.cs`.

```csharp
/// <summary>
/// Demonstrates a specialized MVC error representation.
/// </summary>
public sealed class ExampleMvcWebApi(JsonSerializerOptions options, ILogger<CoreEx.AspNetCore.Mvc.WebApi>? logger = null, CoreEx.ExecutionContext? executionContext = null)
    : CoreEx.AspNetCore.Mvc.WebApi(options, logger, executionContext)
{
    /// <inheritdoc/>
    protected override IActionResult CreateResultForException(HttpContext httpContext, Exception exception, IExtendedException? extendedException)
        => new ContentResult
        {
            StatusCode = (int)(extendedException?.StatusCode ?? HttpStatusCode.InternalServerError),
            ContentType = "application/vnd.example.error+json",
            Content = JsonSerializer.Serialize(new { Error = extendedException?.ErrorType ?? "unexpected", TraceId = httpContext.TraceIdentifier }, JsonSerializerOptions)
        };
}

/// <summary>
/// Demonstrates a specialized Minimal API error representation.
/// </summary>
public sealed class ExampleHttpWebApi(JsonSerializerOptions options, ILogger<CoreEx.AspNetCore.Http.WebApi>? logger = null, CoreEx.ExecutionContext? executionContext = null)
    : CoreEx.AspNetCore.Http.WebApi(options, logger, executionContext)
{
    /// <inheritdoc/>
    protected override IResult CreateResultForException(HttpContext httpContext, Exception exception, IExtendedException? extendedException)
        => Microsoft.AspNetCore.Http.Results.Content(
            JsonSerializer.Serialize(new { Error = extendedException?.ErrorType ?? "unexpected", TraceId = httpContext.TraceIdentifier }, JsonSerializerOptions),
            "application/vnd.example.error+json", statusCode: (int)(extendedException?.StatusCode ?? HttpStatusCode.InternalServerError));
}
```

### Isolate specialized instances with keyed DI

Keep the ordinary registrations and add keyed instances only where needed. Each specialized instance has independent serializer options, success media type, and error representation; no mutable static defaults or additional framework DI extensions are required.

```csharp
builder.Services.AddMvcWebApi().AddHttpWebApi();
builder.Services.AddKeyedScoped<CoreEx.AspNetCore.Mvc.WebApi>("specialized", (sp, _) =>
    new ExampleMvcWebApi(
        new JsonSerializerOptions(JsonDefaults.SerializerOptions) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower },
        sp.GetRequiredService<ILogger<CoreEx.AspNetCore.Mvc.WebApi>>(), sp.GetService<CoreEx.ExecutionContext>())
    {
        DefaultJsonMediaType = "application/vnd.example.resource+json",
        ConvertUnhandledExceptionsToProblemDetails = true // Explicit opt-in; the hook now emits the specialized error representation for unexpected failures too.
    });

builder.Services.AddKeyedScoped<CoreEx.AspNetCore.Http.WebApi>("specialized", (sp, _) =>
    new ExampleHttpWebApi(
        new JsonSerializerOptions(JsonDefaults.SerializerOptions) { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower },
        sp.GetRequiredService<ILogger<CoreEx.AspNetCore.Http.WebApi>>(), sp.GetService<CoreEx.ExecutionContext>())
    {
        DefaultJsonMediaType = "application/vnd.example.resource+json",
        ConvertUnhandledExceptionsToProblemDetails = true
    });
```

Inject the appropriate base type with `[FromKeyedServices("specialized")]` in a controller constructor or Minimal API handler. Configure instance properties at construction, not by mutating shared serializer options during a request. A FHIR consumer can supply its Firely-configured options, set `application/fhir+json`, and render OperationOutcome in its own subclasses; Firely models, domain extensions, search parsing, and Bundle construction remain outside CoreEx.

Despite its historical name, `ConvertUnhandledExceptionsToProblemDetails` controls whether unexpected failures are converted inside WebApi, not the output format. Its default remains `false`, and overriding the hook does not enable it. With it disabled, both thrown unexpected exceptions and unexpected failures carried by `Result`/`Result<T>` propagate; with it enabled they reach the hook. Cancellation attributable to the supplied operation token propagates in either form, while unrelated cancellation remains an unexpected failure. DELETE's configured not-found-to-default-status behavior is unchanged.

**Compatibility change:** Minimal WebApi previously converted unexpected failed results even when the flag was `false`. It now propagates them, matching MVC and thrown exceptions. Consumers relying on the previous behavior must explicitly enable the flag or handle the failure outside WebApi.

**Boundary:** this hook does not guarantee a protocol-specific envelope for every failure. MVC model binding and automatic validation, authentication/authorization, middleware, and failures during result execution can occur outside WebApi. `UseCoreExExceptionHandler()` resolves the unkeyed `Http.WebApi`, preserving its normal ProblemDetails fallback and avoiding duplicate logging; a keyed registration does not replace it. Replacing the unkeyed instance intentionally changes that middleware's representation too. Configure any wider protocol policy in the consuming host, and declare matching success/error OpenAPI metadata separately.

## HTTP correlation

`UseExecutionContext()` captures `X-Correlation-Id` before its custom configuration callback and opens the execution's logging scope. A supplied header must contain one 1-256-character visible ASCII token without whitespace or commas; empty, repeated, comma-combined, control/non-ASCII and oversized values produce safe HTTP 400 before application execution, including endpoints which do not use WebApi. Absence preserves deliberately factory-configured correlation and otherwise leaves it null. The callback and application may replace or clear it.

Set `CoreEx:Correlation:Enabled` to `false` to disable automatic capture, validation, response echo, scopes and correlation activity tags. Incoming headers are then left to the application, and response headers are not modified by the correlation boundary. Other explicit correlation and stored event metadata remain intact; see [shared correlation semantics](../CoreEx/README.md#workflow-correlation).

The effective context value is sent on successful and error responses using `Response.OnStarting`, including streamed/deferred rendering; null omits/removes the feature-owned header. Correlation cannot be changed after response headers have committed. Idempotency defaults omit correlation from fingerprints and cached response headers; explicitly configured header lists are honored. The execution boundary overrides any cached correlation header, including legacy entries, before committing the response: a retry echoes its current correlation without re-executing the business operation. Explicitly including correlation in fingerprinting makes different correlation values different requests. WebApi and exception handling remain result/error renderers, not correlation owners.

Middleware coverage is explicit. Place the execution boundary before every endpoint/short-circuit whose correlation you need; requests rejected before it do not participate. Put `UseCoreExExceptionHandler()` before `UseExecutionContext()` so configuration-callback failures also receive standard error conversion. The execution boundary's registered `OnStarting` callback still echoes the effective correlation when the exception handler renders the response. Framework exception logs are emitted after the execution's correlation scope is disposed and do not carry that scope. Exception handling does not establish correlation scopes or repair response headers; coverage outside the execution boundary is not guaranteed. Review authorization/other early middleware coverage. Logs emitted before capture cannot be enriched retroactively.

For browser clients, allow `X-Correlation-Id` in your CORS request policy and expose it in response headers, for example `.WithHeaders("Content-Type", "X-Correlation-Id").WithExposedHeaders("X-Correlation-Id")`, alongside your existing required headers. No CORS policy is enabled automatically. See [shared logging and tracing semantics](../CoreEx/README.md#workflow-correlation) and [Functions integration](../CoreEx.Azure.Functions/README.md#workflow-correlation).

## Related Namespaces

- **[`CoreEx`](../CoreEx/README.md)** - Semantic exceptions, `ExecutionContext`, `Result<T>`, and `PagingArgs` are the domain primitives translated by this package into HTTP responses.
- **[`CoreEx.Http`](../CoreEx.Http/README.md)** - Client-side `TypedHttpClientBase<TSelf>` consumes `ProblemDetails` responses produced by this package; `HttpNames` defines the shared header/query-string constants.
- **[`CoreEx.Validation`](../CoreEx.Validation/README.md)** - `ValidationException` raised by validators is translated to `400 Bad Request` with a field-level `errors` extension in `ProblemDetails`.

## Additional Resources

- [RFC 7807 — Problem Details for HTTP APIs](https://tools.ietf.org/html/rfc7807) - The specification implemented by `WebApi` exception handling.
- [ASP.NET Core Health Checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks) - The underlying infrastructure extended by `HealthCheckOptions`.

## AI Usage Guide

An [`AGENTS.md`](./AGENTS.md) file is included with this package. AI coding assistants (GitHub Copilot, Claude, Cursor, etc.) that support workspace-injected package documentation will automatically surface concise usage guidance, code examples, and `Do Not` rules for this package without requiring a local CoreEx checkout.
