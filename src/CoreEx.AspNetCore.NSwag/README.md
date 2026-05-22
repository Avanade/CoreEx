# CoreEx.AspNetCore.NSwag

> Provides the NSwag `IOperationProcessor` integration that reads CoreEx MVC attributes (`[Paging]`, `[Query]`, `[Accepts]`, `[IdempotencyKey]`, `[ProducesNotFoundProblem]`) and injects the corresponding parameters, request bodies, and response entries into the generated OpenAPI specification.

## Overview

`CoreEx.AspNetCore.NSwag` bridges CoreEx's declarative MVC attributes with NSwag's OpenAPI document generation pipeline. Without this package, NSwag cannot infer paging query-string parameters, idempotency-key request headers, or standard `ProblemDetails` error responses from CoreEx's attribute-based conventions.

`NSwagOpenApiOperationProcessor` is registered as a NSwag `IOperationProcessor` and runs once per operation during document generation. It reads endpoint metadata for each of the CoreEx MVC attributes and adds the corresponding OpenAPI artifacts — parameters, request-body content types, response entries — to the generated `OpenApiOperation`. The processor is controlled by `OpenApiOptions` (from `CoreEx.AspNetCore`), which exposes flags and display-text properties for every injected element.

`CoreExNSwagExtensions.AddCoreExConfiguration()` (on `OpenApiDocumentGeneratorSettings`) is the single registration call that wires both the operation processor and the CoreEx-aligned `System.Text.Json` schema settings.

## Key capabilities

- 📋 **Paging parameters**: Reads `[PagingAttribute]` and adds `$skip`, `$take`, and optionally `$count` / `$page` query-string parameters with descriptions controlled by `OpenApiOptions.PagingSkipText`, `PagingTakeText`, etc.
- 🔍 **Query parameters**: Reads `[QueryAttribute]` and adds `$filter` and/or `$orderby` query-string parameters when `SupportsFilter` / `SupportsOrderBy` are set.
- 📦 **Request body content types**: Reads `[AcceptsAttribute]` and populates the operation `RequestBody` with the declared content type(s) and NSwag-inferred JSON schema for the body type.
- 🔑 **Idempotency-key header**: Reads `[IdempotencyKeyAttribute]` and adds an `x-idempotency-key` header parameter to the operation.
- 🚫 **Not-found response**: Reads `[ProducesNotFoundProblemAttribute]` and adds a `404 application/problem+json` response entry.
- ⚠️ **Standard ProblemDetails responses**: Optionally injects `400`, `422`, and `500` `application/problem+json` response entries for all operations via `OpenApiOptions.IncludeStandardProblemDetailsResponses`.
- 📡 **Fields query string**: When `OpenApiOptions.IncludeFieldsRequestHeaders` is set, adds the `$fields` query-string parameter for response field projection.
- 💬 **Message response headers**: When `OpenApiOptions.IncludeMessagesResponseHeaders` is set, documents `x-messages-warning` and `x-messages-info` response headers.
- ⚙️ **STJ schema settings**: `ConfigureSchemaSettings()` aligns NSwag's JSON schema generation with `JsonDefaults.SerializerOptions` (camelCase, enum-as-string, `WhenWritingDefault`) so the generated schema matches the actual serialized output.

## Key types

| Type | Description |
|------|-------------|
| **[`NSwagOpenApiOperationProcessor`](./NSwagOpenApiOperationProcessor.cs)** | NSwag `IOperationProcessor` that reads CoreEx endpoint metadata and injects paging, query, accepts, idempotency-key, not-found, ProblemDetails, fields, and message-header artifacts into each `OpenApiOperation`. |
| **[`CoreExNSwagExtensions`](./CoreExNSwagExtensions.DependencyInjection.cs)** | `OpenApiDocumentGeneratorSettings` extension methods: `AddCoreExConfiguration()` (registers processor + schema settings), `AddOpenApiDocumentExtensions(configure?)`, `ConfigureSchemaSettings(jsonSerializerOptions?)`. |

## Related Namespaces

- **[`CoreEx.AspNetCore`](../CoreEx.AspNetCore/README.md)** - Defines `OpenApiOptions` (processor configuration), and the MVC attributes (`[PagingAttribute]`, `[QueryAttribute]`, `[AcceptsAttribute]`, `[IdempotencyKeyAttribute]`, `[ProducesNotFoundProblemAttribute]`) read by this processor.
- **[`CoreEx.Http`](../CoreEx/Http/README.md)** - `HttpNames` provides the configurable query-string and header name constants used when creating OpenAPI parameter names.
- **[`CoreEx.Json`](../CoreEx/Json/README.md)** - `JsonDefaults.SerializerOptions` is the options instance applied to NSwag schema settings by `ConfigureSchemaSettings()`.

## Additional Resources

- [NSwag GitHub](https://github.com/RicoSuter/NSwag) - The NSwag library this package extends.