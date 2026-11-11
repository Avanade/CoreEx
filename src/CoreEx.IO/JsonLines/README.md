# CoreEx.IO.JsonLines

UTF-8 JSONL framing, nested object units, and opt-in metadata routes. Object deserialization and scalar policies come from [CoreEx.IO.Json](../Json/README.md); reporting, scopes, accounting, and replay use the [shared CoreEx.IO lifecycle](../README.md#processing-lifecycle).

## Key types

| Type | Purpose |
|---|---|
| [`JsonLinesFileProcessor<TUnit>`](./JsonLinesFileProcessor.cs) | Processes one business object per non-skipped line. |
| [`JsonLinesDefinition<TUnit>`](./JsonLinesDefinition.cs) | Declares the business mapping, optional discriminator match, and metadata routes. |
| [`JsonLinesFileOptions`](./JsonLinesFileOptions.cs) | Configures shared JSON options, discriminator, and blank-line handling. |
| [`JsonLinesContext`](./JsonLinesContext.cs) | Carries invocation-local state, recognized route counts, and metadata. |
| [`JsonLinesMetadata<TMetadata>`](./JsonLinesMetadataT.cs) | Declares a bounded metadata route and its pure validator. |
| [`JsonLinesBlankLinePolicy`](./JsonLinesBlankLinePolicy.cs) | Selects rejection or skipping of blank lines. |

## Nested JSONL

Prefer generation from [orders-jsonl-file.yaml](../../../tests/CoreEx.CodeGen.Test.Unit/orders-jsonl-file.yaml) or [routed-jsonl-file.yaml](../../../tests/CoreEx.CodeGen.Test.Unit/routed-jsonl-file.yaml). Both generate executable consumers under `tests/CoreEx.IO.Test.Unit/Files`. The handwritten `OrdersJsonlHandler` injects `IOrderImportApplication` in the fresh unit scope and preserves its business Result. `GeneratedJsonLinesIntegrationTests` pins both opens to immutable byte content and demonstrates source-revision/unit receipts using an in-memory application double; that demonstrates ownership and retry behavior, not durable transactions or exactly-once delivery.

Direct runtime composition is also supported:

```csharp
static JsonUnitMapping<Order> CreateMapping() => new JsonUnitMapping<Order>()
    .MapString("order_id", x => x.Id)
    .MapObjects("lines", x => x.Lines, new JsonUnitMapping<Line>()
        .MapString("sku", x => x.Sku)
        .MapValue("quantity", x => x.Quantity, JsonFieldConverters.Integer));

var processor = new JsonLinesFileProcessor<Order>(() => new("Order", CreateMapping()),
    validator: orderValidator);
var context = new JsonLinesContext(new FileProcessingIdentity("orders", "1", "store/orders.jsonl", "attempt", "revision"),
    errorMode: FileErrorMode.CollectErrors);
var summary = await processor.ProcessAsync(openReadAsync, context, cancellationToken).ConfigureAwait(false);
```

`Order`/`Line` are caller-owned nullable wire types with writable direct properties. The example uses `CoreEx.IO`, `CoreEx.IO.Json`, and `CoreEx.IO.JsonLines`. See [mapping and validation](../Json/README.md#mapping-and-validation) for exact member matching, scalar conversions, generated collection rules, and first-error-per-unit deserialization.

## Framing and diagnostics

JSONL accepts LF/CRLF and a final nonempty unterminated line, with no phantom record after a final separator. Input is parsed as UTF-8 without encoding autodetection. A UTF-8 BOM produces terminal `jsonl.encoding.bom`; other encoding signatures fail ordinary JSON parsing. Well-framed syntax/depth errors can recover at the next line under CollectErrors. Invalid UTF-8 or escaped surrogates in consumed strings produce localized conversion errors; malformed ignored string content can remain unexamined. There is no separate whole-line UTF-8 validation pass. Blank lines reject by default; explicit `BlankLinePolicy.Skip` excludes them from record/unit counts but not physical line numbering or byte accounting. Plain non-skipped lines are candidate units even when malformed. Routed lines without an identifiable business discriminator are records, not invented units.

Logical CLR paths and source pointers are combined with the physical JSONL line. `FileUnitInfo.SourceByteOffset` is the actual zero-based source line start, not a read-ahead-distorted stream position; `BytesRead` separately counts actual reads.

## Routing and metadata

Enable routing explicitly with `JsonLinesFileOptions.Discriminator`, a business `JsonLinesDefinition.Match`, and `JsonLinesMetadata<T>` declarations. Matches are exact/disjoint and the direct discriminator member is structural, excluded from unexpected-member checks and DTOs unless explicitly mapped. This delivery supports one business wire type plus bounded zero-or-one metadata routes, not heterogeneous business dispatch. First/Last metadata rules include malformed/unknown non-skipped lines; requiredness is checked at EOF without fabricated coordinates. Metadata validates as encountered but never reaches unit hooks. `JsonLinesContext.GetRecordCount` exposes recognized route counts.

Pass-local metadata is available only after encounter. Complete accepted metadata is available from the beginning of two-pass execution and at successful completion, never during validation/report hooks or single-pass unit execution. Typed reads return fresh copies. Follow the [shared metadata contract](../README.md#metadata), keep reconciliation state on the context, and put trailer-dependent checks in EOF reconciliation.

## Processing policy and parser ownership

JSONL frames lines using the built-in span newline search and calls `JsonDocument.Parse` once per line, then delegates object construction to `JsonSerializer`. On .NET 10+, `AllowDuplicateProperties = false` delegates duplicate rejection to Microsoft; older targets retain a small object-name walk solely for duplicate rejection. Only consumed strings are decoded. Syntax, duplicate, invalid member-name decoding, and depth failures share `jsonl.record.invalid`; CoreEx does not implement JSON tokenization. The reusable line buffer retains capacity for the largest encountered line; the DOM is disposed after each line and completed units are not collected file-wide.

Source admission, input size, concurrency, timeouts, and reporting remain [consumer-owned](../README.md#input-ownership). No line/token/metadata byte quota or default collection maximum is imposed. JSON retains configurable `MaxDepth` (64 by default), business cardinality, and explicitly declared positive collection `maxItems`, checked after deserialization. A large line or mapped collection can still consume substantial memory.

## Related documentation

- [CoreEx.IO](../README.md): shared lifecycle, handlers, metadata, reporting, and telemetry.
- [CoreEx.IO.Json](../Json/README.md): shared serializer contracts and streaming JSON documents.
- [File CodeGen](../../CoreEx.CodeGen/Files/README.md#nested-jsonl-definition): generation-first JSONL definitions.
- [Functions inventory examples](../../../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md#example-matrix): independent JSONL objects and nested units with routed header/trailer metadata and EOF reconciliation.
