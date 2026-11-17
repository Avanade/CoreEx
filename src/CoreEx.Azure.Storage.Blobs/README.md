# CoreEx.Azure.Storage.Blobs

Host-neutral Azure Blob input and report helpers. The package references `CoreEx` and the `Aspire.Azure.Storage.Blobs` client integration (which supplies the Azure SDK), not Azure Functions or `CoreEx.IO`. Use the registered SDK clients for other storage operations.

## Host client registration

Prefer `builder.AddAzureBlobServiceClient("ImportStorage")` in the host composition root and derive container clients from its registered `BlobServiceClient`, preserving the integration's configuration, telemetry and health options. This is a client integration, not an AppHost requirement or storage provisioner. CoreEx helpers accept caller-owned SDK clients and add no host startup behavior themselves.

Aspire's named connection configuration is separate from native Functions binding app settings. For a Functions app that already uses an `ImportStorage` binding connection, explicitly map that setting into the client registration's settings or provide the equivalent named connection string. Keep the native trigger-supplied input `BlobClient`; use the registered service client for report containers. Preserve existing custom clients/options and avoid constructing parallel raw clients that bypass host configuration.

```csharp
builder.AddAzureBlobServiceClient("ImportStorage", settings =>
{
    if (settings.ConnectionString is null && settings.ServiceUri is null)
        settings.ConnectionString = builder.Configuration["ImportStorage"];
});
```

This fallback preserves an already configured connection or service URI; use deployment-appropriate service URI/credential configuration for managed identity instead of adding a connection string where none is intended.

## Revision-pinned input

```csharp
var source = await BlobReadSource.CreateAsync(blob, cancellationToken);
// Apply your admission/size policy using source.ContentLength before opening content.
var identity = new FileProcessingIdentity(definition, version, source.SourceUri.AbsoluteUri, attemptId, source.ETag.ToString());
var summary = await processor.ProcessAsync(source.OpenReadAsync, context, cancellationToken);
```

`CreateAsync` selects the revision current at the supplied client's address. Every `OpenReadAsync` uses the captured ETag as `IfMatch` and returns a fresh stream owned by the caller (or file processor). Both validation and replay therefore read that revision or fail; replacement/deletion never silently switches to newer bytes. There is no implicit snapshot or buffering. Supply an explicitly version- or snapshot-specific `BlobClient` when historical revisions matter. `SourceUri` removes user information, fragments, SAS and all query values except version/snapshot selectors; it is an identity, not an authenticated download URL. Never log the original client URI or raw SDK errors containing credentials.

## Streamed JSONL attempt reports

```csharp
var name = BlobJsonlReport.CreateName(definition, version, source.SourceUri.AbsoluteUri, source.ETag.ToString(), attemptId);
var report = new BlobJsonlReport(container.GetAppendBlobClient(name));
await report.StartAsync(cancellationToken);
await report.WriteAsync(new { kind = "attempt", identity }, cancellationToken);
await report.WriteAsync(new { kind = "diagnostic", code = "invalid-price" }, cancellationToken);
await report.CompleteAsync(new { kind = "summary", accepted = false }, "ContentFailed", cancellationToken);
```

The container must already exist. Start is create-only (`IfNoneMatch = *`), with `application/x-ndjson` content type and `outcome=InProgress` metadata. Records use compact UTF-8 JSON plus LF, even when supplied serializer options enable indentation. No file-wide record history is retained; memory is proportional to one serialized record. The caller owns record-size/count limits, sensitive-data policy, attempt identity and terminal outcome semantics.

JSONL is not a single JSON document: read completed lines individually. Earlier records remain readable after failure, although the final line may be incomplete. A streamed JSON array/object is not a complete JSON document until its closing syntax is written, so it is a poor default for incomplete operational attempts. If a consumer needs only a finished JSON summary, serialize and upload that document using the Blob SDK separately; there is no `BlobJsonReport` API.

Completion awaits its terminal record before publishing terminal metadata. There is no automatic completion/disposal side effect: failures and cancellation leave incomplete attempts observable. Each append is conditioned on the last acknowledged ETag and byte position, and final metadata uses `IfMatch`. Await calls sequentially; overlapping calls are rejected. A failed operation faults the instance and is never silently retried by this helper. The caller's SDK retry policy still applies: after an ambiguous acknowledgement an SDK retry can fail its conditions even if the first write succeeded. Start a distinct attempt; do not blindly append again. A final metadata failure can likewise leave an accepted outcome without acknowledgement. These are reports, not a transactional log or exactly-once protocol.

`CreateName` hashes a JSON-framed component array, distinguishing ambiguous concatenations and producing a bounded lowercase SHA-256 name. Include full credential-free source identity, revision and a distinct attempt ID. It does not choose business idempotency keys or enforce admission policy.

## Flat CSV reports

Use `BlobCsvReport` for supplier-facing rejection or reconciliation rows. Keep JSONL for mixed attempt/diagnostic/summary records. Both formats share the same create-only, conditional append and explicit-completion lifecycle.

```csharp
var name = BlobCsvReport.CreateName(definition, version, source.SourceUri.AbsoluteUri, source.ETag.ToString(), attemptId);
var report = new BlobCsvReport(container.GetAppendBlobClient(name),
    ["row-number", "supplier-code", "supplier-sku", "field", "error-code", "message"],
    CsvFormulaHandling.Reject);
await report.StartAsync(cancellationToken);
await report.WriteAsync([2, "ACME", "AC-100", "unit-price", "price.invalid", "Price must not be negative."], cancellationToken);
await report.CompleteAsync("ContentFailed", cancellationToken);
```

Declare ordered, nonempty, ordinally distinct column names explicitly; the constructor copies them. Start creates `text/csv; charset=utf-8` output and awaits exactly one header record before returning. Every row must match that width. Cells support null, strings and scalar `IFormattable`/`IConvertible` values, with invariant formatting and no property reflection. Project complex objects yourself; supply preformatted strings for schema-specific dates, decimal scale or other formats. Null and empty string share an empty field. Commas, double quotes and embedded CR/LF are escaped using standard CSV quoting. Encoding is strict UTF-8 without a BOM: invalid Unicode throws rather than silently replacing characters. Record separators are CRLF.

`CsvFormulaHandling` is a required constructor choice, not a silent default. `Reject` throws before appending a formula-like row; `PrefixApostrophe` deliberately changes such values by adding an apostrophe before CSV quoting; `Preserve` accepts spreadsheet interpretation risks and leaves them unchanged. The policy also applies to headers. Detection covers leading tab/CR/LF, and `=`, `+`, `-` or `@` after leading whitespace, including formatted negative numbers. Ordinary quoting does not prevent spreadsheet formulas. This conservative policy is not a universal spreadsheet-safety guarantee; choose it for your consumer and trust boundary.

Completion updates outcome metadata only: it never inserts an out-of-schema summary row. Keep counts or heterogeneous operational details in a separate JSONL report if needed. Formatting/schema failures fault the instance without appending a partial row; a failed header append leaves an incomplete attempt. Storage/cancellation failures and ambiguous acknowledgements follow the same rules as JSONL. CSV adds no schema inference, configurable dialect, automatic worksheet formatting, extra infrastructure or CoreEx.IO dependency.

## Boundaries

Functions owns triggers/retries. Applications own schema, diagnostics, business acceptance, deduplication, transactions, publishing, retention and archive/delete policy. Copy/delete is not an atomic move; leases are not business idempotency. This package creates no container, receiver, snapshot, temporary file or background service. Storage and cancellation failures propagate.
