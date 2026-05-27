# CoreEx.AspNetCore.NSwag — AI Usage Guide

Wires CoreEx MVC attributes into the NSwag OpenAPI document pipeline. Register with a single call in `Program.cs`.

## Registration

```csharp
// Program.cs
builder.Services.AddOpenApiDocument(s =>
{
    s.Title = builder.Environment.ApplicationName;
    s.AddCoreExConfiguration();   // registers the CoreEx NSwag operation processor + STJ schema settings
});

// Middleware
app.UseOpenApi();
app.UseSwaggerUi();
```

`AddCoreExConfiguration()` is the only call required. It registers `NSwagOpenApiOperationProcessor` and aligns the NSwag JSON schema with `JsonDefaults.SerializerOptions` (camelCase, enum-as-string, write-when-not-default).

## What Gets Generated Automatically

| Attribute on action | OpenAPI artifact added |
|---|---|
| `[Paging]` | `$skip`, `$take`, optionally `$count`/`$page` query params |
| `[Query]` | `$filter`, `$orderby` query params |
| `[IdempotencyKey]` | `x-idempotency-key` header parameter |
| `[ProducesNotFoundProblem]` | `404 application/problem+json` response entry |
| `[Accepts(typeof(T))]` | Request body content type and JSON schema |

## Do Not

- Do not manually add paging/idempotency parameters to NSwag operations — let the operation processor generate them from attributes.
- Do not call `AddCoreExConfiguration()` more than once per document.

## Further Reading

- [README](./README.md) — full processor and options API reference.
- [CoreEx.AspNetCore](../CoreEx.AspNetCore/README.md) — defines the MVC attributes read by this processor.
- [Hosts layer](../../samples/docs/hosts-layer.md) — shows NSwag registration in a real API host `Program.cs`.
