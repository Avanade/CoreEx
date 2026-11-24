# CoreEx.AspNetCore

> Provides the ASP.NET Core integration layer for CoreEx: the `WebApi` execution helper (MVC and Minimal API variants), middleware for `ExecutionContext` scoping and exception-to-ProblemDetails translation, idempotency key handling, health check configuration, and OpenAPI/NSwag extensions.

## Overview

`CoreEx.AspNetCore` is the server-side HTTP integration package. It bridges CoreEx domain logic — semantic exceptions, `Result<T>` pipelines, paging, entity contracts — into ASP.NET Core HTTP responses, following RFC 7807 ProblemDetails conventions throughout.

The centrepiece is `WebApi`, which controllers and Minimal API handlers inject and call (`GetAsync`, `PostAsync`, `PutAsync`, `PatchAsync`, `DeleteAsync`). `WebApi` handles request body deserialization, `ETag`/`If-Match` concurrency checking, `Result<T>` unwrapping, paging header stamping, info/warning message header propagation, and exception-to-`ProblemDetails` translation — all in a single invocation boundary so individual action methods stay focused on business logic.

Two concrete `WebApi` implementations ship: `Mvc.WebApi` returning `IActionResult` for MVC controllers, and `Http.WebApi` returning `IResult` for Minimal API endpoints. Both share the same abstract base (`WebApi<TResult>`) and invoker infrastructure.

For HTTP adapters with a pre-processing exception boundary, `webApi.CreateErrorResult(request, exception, cancellationToken)` creates an error result using the same instance's classification, logging, ProblemDetails/custom representation and unexpected-exception policy. Own-operation cancellation propagates. The caller executes the returned result; errors during that execution are not caught and rendered again.

## Asynchronous publishing and operation resources

`WebApi.PublishAsync` accepts one `WebApiPublishOptionsBase` instance and owns the common validation, publishing, operation tracking and HTTP response pipeline. `WebApiPublishOptions.Create<TRequest>()` parses the request body and publishes the request as the message by default; `Create<TRequest>(value)` publishes an already-deserialized value. Both support `.WithValidator(...)` and `.WithEventFactory(...)` for a payload-aware event factory. `CreateMapped<TRequest,TMessage>(mapper)` explicitly maps a parsed or supplied request value to the published message type; both types must be classes, and `Mapper.Create<TRequest,TMessage>(...)` supports inline mappings. Default messages are self-addressed commands with the `publish` action. `.WithEvent(...)` transforms the created event (message type, domain, action, key, etc.) before the tracked WebApi acceptance path attaches operation metadata; tracked publishing requires preserving the declared payload type. `.WithPublisher(...)` accepts an `IServiceProvider` for keyed or custom publisher resolution, using `ExecutionContext.ServiceProvider` or falling back to HTTP request services; a configured resolver must return a publisher. Publishing is untracked by default and returns bodyless `202 Accepted`; it does not create work state or attach operation metadata.

For custom message preparation without automatic request-body parsing, use `WebApiPublishOptions.CreateWithValueFactory<TMessage>(...)` to create a payload wrapped in the default message, or `CreateWithEventFactory<TMessage>(...)` to create a complete `EventData`. The factory choice is required at creation, so the two modes cannot accidentally replace one another. Both callbacks return a `Result`, so failures can be returned or thrown. These callbacks receive the request-bound `WebApiOptions`, own any custom input parsing and validation, and must declare the payload type used for operation registration. For tracked publishing, `.WithOperationKey(...)` can derive the submission key from `WebApiOptions.Request`, including route values or headers. Failures prevent publisher resolution and operation creation, while cancellation follows the existing request-token policy. Configuration can be chained with `.WithPublisher(...)`, `.WithEvent(...)` and `.WithOperationOptions(...)`; `.WithDestination(...)` overrides are supported for untracked publishing only. Tracked commands use the publisher's default routing; the host must configure the publisher and any destination provider to route to its own domain.

Tracked acceptance is enabled per HTTP endpoint with `[PublishTracking]` on MVC actions or `.WithPublishTracking()` on Minimal API routes. `WebApiPublishOperationOptions` selects a registered operation name, an optional submission `CompositeKey`, and optional per-operation `InactivityTimeout` and `TerminalRetention` overrides; it does not enable tracking on a routed endpoint, and configuring it on an unmarked endpoint is an error. Omitted durations use the store defaults (24 hours each); positive durations round up to whole milliseconds. The effective durations are captured when first created and preserved across duplicates and later transitions, even if store defaults change. Inactivity is the delivery/recovery window and must exceed the broker's complete redelivery window; it is not a work-duration or first-start deadline. For direct calls without a routed endpoint, non-null operation options enable tracking. Typed publishing can derive a repeatable key from both request-bound `WebApiOptions` and validated input with `.WithOperationKey(...)`; mapped requests use the original input. Custom preparation can derive it from request-bound `WebApiOptions` with the same method. A submission key need not identify a business entity. Without a key, each call creates fresh work: retries cannot reconcile unless an independent HTTP idempotency policy replays the response. Keys are not secrets: their canonical form is retained in the operation store, but neither the key nor its hash is returned in the default HTTP representation.

Register stable application-owned names with `AddCoreExOperation<TMessage>("operation-name")`, register an `IOperationStore`, and map one domain-owned operations resource with `AddCoreExOperations` and `MapCoreExOperations`. If a message type has multiple registrations, explicitly select the name. The store creates or finds work using domain, tenant, name and submission key; only a newly created operation publishes a command. An authorized duplicate returns `202` with the existing GUID handle and current status, even if its payload differs. The command carries that GUID and name, not the submission key. Ordinary HTTP idempotency and coordination remain independent; an `Idempotency-Key` header is not automatically used as the operation key.

This replaces the unfinished WorkOrchestrator-based HTTP tracking API in place. Custom `IOperationStore` implementations must implement the lifetime-options create overload and persist the effective durations; `OperationStoreBase` subclasses must update `OnCreateAsync`. `OperationState` now requires those durations and uses `InitiatedBy` instead of `InitiatorId`. Existing relational operation tables require a reviewed migration and backfill.

For atomic acceptance, configure a scoped relational operation store and scoped `IUnitOfWork` whose events queue uses the same database instance and transactional outbox. The unit of work commits the operation and initiating outbox message together and owns the outbox flush. This acceptance transaction must contain no business activities or writes. A publisher outside the unit-of-work queue sends after the acceptance transaction scope returns. Without an enclosing transaction, a send failure returns an error after the operation is retained, and a same-key retry finds it without sending again; an enclosing transaction may still roll back later. A direct publisher configured as the unit-of-work queue is flushed by that unit of work and is not atomic with the database, even if a send failure rolls the transaction back. Applications must address these dual-write gaps and recovery; this API does not enforce outbox configuration.

The resource exposes owner-checked status, result and cooperative cancellation routes under `/operations` by default; `groupConfigure` can add host authorization or rate limits. Hosts must establish a trusted `ExecutionContext.User` and tenant. Ownership compares `AuthenticationUser.UserName` ordinally against `InitiatedBy`, following the ChangeLog username convention; a username must be unique within its domain and tenant, and changing it changes ownership access. The default environment user and unknown or unauthenticated users do not establish HTTP ownership. Additional operation authorization cannot override ownership. Missing or unauthorized work returns `404`.

Status returns `200` with `accepted`, `running`, `retrying`, `succeeded`, `failed`, `canceled` or `expired`; expiry does not prove that no effects occurred. `ExpiryOn` is the current inactivity deadline for unfinished work, not a fixed processing deadline. Status uses a hash of the public representation for conditional HTTP responses, not the provider's ETag: PostgreSQL `xmin` is not guaranteed to change per mutation or recreation. The result route returns `200` for an application reader, `204` for no data or a separate result resource, and `409` before success. Cancellation immediately cancels accepted or retrying work; for running work it only requests cooperative cancellation. Operation and result links honor `PathBase`, `UseAbsolutePaths` and `BaseAddress`.

Operations options and metadata live in `CoreEx.AspNetCore.Operations`. Registration and endpoint mapping are provided by the existing `CoreExAspNetCoreExtensions` classes in `Microsoft.Extensions.DependencyInjection` and `Microsoft.AspNetCore.Builder`, respectively.

This HTTP acceptance API is not worker integration. Subscriber metadata handling, fenced business completion and the end-to-end execution proof remain deferred; a `202`, `404`, stale status or expiry is not proof of the business outcome. Cosmos is deferred.

For OpenAPI and runtime behavior, mark tracked MVC actions with `[PublishTracking]` and tracked Minimal API routes with `.WithPublishTracking()`. Tracked metadata documents `OperationStatus`, polling headers, the named status link, and Microsoft long-running-operation extensions. Untracked publishing is described with standard metadata, `[ProducesResponseType(StatusCodes.Status202Accepted)]` or `.Produces(StatusCodes.Status202Accepted)`, as a bodyless 202 without operation links. This phase does not document wait-based 200/201 responses.

## Key capabilities

- 🌐 **WebApi execution helper**: `WebApi` encapsulates GET/HEAD, POST (with idempotency), PUT, PATCH (JSON Merge Patch), and DELETE patterns with standardized request validation, deserialization, ETag checks, and response serialization.
- 📄 **RFC 7807 ProblemDetails**: All `IExtendedException` types (`ValidationException`, `NotFoundException`, `ConcurrencyException`, etc.) are translated to typed `ProblemDetails` responses with appropriate HTTP status codes and optional field-level `errors` extension.
- 🔁 **JSON Merge Patch (PATCH)**: `WebApi.PatchAsync` reads the request body as a merge-patch document, deserializes the current entity via a provided function, applies the patch, and re-validates before saving.
- ⚙️ **ExecutionContext middleware**: `ExecutionContextMiddleware` resolves the DI-scoped `ExecutionContext` per request, wires `ServiceProvider`, and propagates info/warning messages to response headers on completion.
- 🛡️ **Idempotency key middleware**: `IdempotencyKeyMiddleware` + `[IdempotencyKey]` attribute intercepts `POST` operations and uses a pluggable `IIdempotencyProvider` backed by an explicit `IIdempotencyStore`.
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
| [`IIdempotencyProvider`](./Idempotency/IIdempotencyProvider.cs) | HTTP idempotency integration boundary; register with `AddIdempotencyProvider()` and provide a separate coordination store. |
| **[`WebApiPublishOptionsBase`](./Publishing/WebApiPublishOptionsBase.cs)** | Common message, destination, publisher and optional tracking configuration accepted by `WebApi.PublishAsync`. |
| **[`WebApiPublishOptions`](./Publishing/WebApiPublishOptions.cs)** | Factory for callback, typed request and mapped request publishing options. |
| **[`WebApiPublishCustomOptions`](./Publishing/WebApiPublishCustomOptions.cs)** | Custom asynchronous payload or event-envelope preparation without automatic request-body parsing. |
| **[`WebApiPublishOptions<TRequest>`](./Publishing/WebApiPublishOptionsT.cs)** | Parses or accepts a typed request value, optionally validates it, and creates the message. |
| **[`WebApiPublishOptions<TRequest,TMessage>`](./Publishing/WebApiPublishOptionsT2.cs)** | Adds explicit request-to-message mapping and optional request validation. |
| **[`WebApiPublishOperationOptions`](./Publishing/WebApiPublishOperationOptions.cs)** | Selects the registered operation name, optional submission key and lifetime overrides. |
| **[`OperationsOptions`](./Operations/OperationsOptions.cs)** | Configures registered operation policies and route prefix. |
| **[`OperationStatus`](./Operations/OperationStatus.cs)** | Public HTTP operation DTO; provider state, owner, lease, type and trace details remain private. |
| **[`OperationError`](./Operations/OperationError.cs)** | Safe public HTTP error projection of operation state. |
| **[`HealthCheckOptions`](./HealthChecks/HealthCheckOptions.cs)** | Configures live/startup/ready/detail health check endpoint paths, tag filters, and JSON detail writer; registered via `MapCoreExHealthChecks()`. |
| **[`WebApiOptions`](./WebApiOptions.cs)** | Per-request options controlling response status code, `ETag`, location header, paging, and alternate status code behavior. |
| **[`OpenApiOptions`](./OpenApiOptions.cs)** | NSwag/OpenAPI document-processor and operation-processor registration for CoreEx paging, query, accepts, and idempotency attributes. |
| [`WebApiBase`](./Abstractions/WebApiBase.cs) | Abstract base for both `WebApi<TResult>` variants: holds `JsonSerializerOptions`, `Logger`, `ExecutionContext`, shared link resolution, and the `ConvertUnhandledExceptionsToProblemDetails` flag. |

## Namespaces

| Namespace | Description |
|-----------|-------------|
| [**`Abstractions`**](./Abstractions/README.md) | Abstract `WebApi<TResult>` base, `WebApiInvoker<TResult>`, `WebApiResult<TResult>`, `WebApiPagingResult`, and request/response option interfaces. |
| [**`HealthChecks`**](./HealthChecks/README.md) | `HealthCheckOptions` and endpoint registration extensions for live/startup/ready/detail health probes. |
| [**`Http`**](./Http/README.md) | Minimal API `WebApi` (`IResult`-returning) and its `WebApiInvoker`. |
| [**`Idempotency`**](./Idempotency/README.md) | `IIdempotencyProvider`, `IdempotencyKeyMiddleware`, and explicit store registration and outcome policy. |
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
