# CoreEx.IO

Host-neutral CSV and nested JSONL validation/execution with shared lifecycle contracts, safe scoped execution, strict encodings, [File CodeGen](../CoreEx.CodeGen/Files/README.md), and the [generation-first Functions inventory example](../../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md). JSON document streaming and XML are not implemented.

Shared contracts and the lifecycle coordinator (`FileProcessor<TUnit>`) live in `CoreEx.IO`. CSV mappings, converters, options, `CsvFileProcessor<TRecord>`, `CsvRoutedFileProcessor<TUnit>`, and `CsvHierarchyFileProcessor<TGroup>` live in `CoreEx.IO.Csv`; all three processors use the shared lifecycle while retaining CSV-specific parsing and mapping. The package does not impose CSV line/header assumptions on its shared contracts and exposes no universal parser/plugin abstraction.

`CoreEx.IO.Json` supplies reusable natural-object mappings and strict scalar converters; `CoreEx.IO.JsonLines` supplies `JsonLinesFileProcessor<TUnit>`, line framing/options, and opt-in metadata routes. JSONL uses the same lifecycle, not another implementation of scopes, reporting, accounting, or replay.

`FileRecordPosition` provides shared `Any`, `First`, and `Last` boundary restrictions for CSV business routes and CSV/JSONL metadata. Each format retains its own participating-record rules: CSV excludes skipped blank/comment records and column-name headers; JSONL includes every non-skipped line, including invalid or unroutable lines.

Public types support consumer configuration, generated mappings, extension hooks, and observable results. Untyped JSON mapping dispatch, readers, and handler invocation methods are internal; the signed unit-test assembly has friend access. Progress, summary, unit provenance, and hook arguments are framework-constructed, while consumers retain public read access. Derive from the concrete format processors and override protected handler hooks, rather than calling framework-only base constructors or bypassing the lifecycle.

`FileProcessor.ProcessAsync` accepts a cancellation-aware stream-opening factory and a fresh invocation-local `FileProcessingContext`, and returns `FileProcessingSummary` directly. The processor owns/disposes the streams. The mode is chosen per invocation, not fixed by handler registration; CodeGen supplies a default that callers can override.

| Mode | Reads | Business execution |
|---|---|---|
| `ValidateOnly` (default) | One pass | None; scoped custom validation still runs when a handler is supplied. |
| `ValidateThenExecute` | Validation, then replay of identical immutable bytes | Starts only after whole-file validation and EOF reconciliation succeed. |
| `ValidateAndExecute` | One pass | Executes each valid unit immediately; later content errors can follow earlier writes. |

Reporting is optional and independent of mode. For a generated integration, implement business operations in its handler, call `CreateContext` with source/attempt identity and the desired mode, then call `ProcessAsync` with the source factory. Only add processor overrides when reporting, trusted scope configuration, or EOF reconciliation is needed.

`FileDiagnostic` uses CoreEx `MessageType` and `LText`, a stable code/category, and optional `FileLocation` and logical record type. `FileLocation` separates one-based physical line/character position, zero-based field index/name, and opaque logical property paths. Physical coordinates are not mandatory, and raw input values are not automatically captured. File-wide diagnostic history is not part of the context or summary.

`FileUnitHandler<TUnit>` separates row/root-group lifetimes from the file coordinator. Its custom validation emits diagnostics through an awaited sink; its execution returns the application's original `Result`. Generated validation uses CoreEx validator messages without per-record exceptions. Default execution throws `NotSupportedException` rather than reporting false success.

The shared coordinator exposes optional hooks; the defaults are no-ops. Calls are ordered and awaited, providing backpressure without selecting a report format or destination.

| Hook | When it runs |
|---|---|
| `OnDiagnosticAsync(FileDiagnosticArgs, cancellationToken)` | For each emitted parsing, mapping, structure, validation, or reconciliation finding, including warnings/information. |
| `OnValidatedUnitAsync(FileUnitArgs<TUnit>, cancellationToken)` | Once per individually valid unit during validation in every mode, before whole-file acceptance; not repeated on replay. |
| `OnValidateFileAsync(FileValidationArgs, cancellationToken)` | After normal traversal and structural checks complete, for application-owned EOF reconciliation. |
| `OnContentFailedAsync(FileSummaryArgs, cancellationToken)` | Once before returning `ContentFailed`, unless a returned business failure takes precedence. Earlier single-pass writes may exist. |
| `OnExecutionFailedAsync(FileExecutionFailedArgs, cancellationToken)` | Once when business execution returns a failed `Result`; receives the failed unit's provenance and original result. |
| `OnCompletedAsync(FileSummaryArgs, cancellationToken)` | Once before returning `Succeeded`, after all requested validation/execution succeeds. |

Hook argument objects have framework-controlled construction and get-only properties; their referenced unit and pass-local context remain mutable. Cancellation stays separate and last. Scoped handlers override `OnValidateAsync(FileUnitValidationArgs<TUnit> args, CancellationToken cancellationToken)` and `OnExecuteAsync(FileUnitArgs<TUnit> args, CancellationToken cancellationToken)`. Validation arguments expose the awaited `ReportDiagnosticAsync` delegate. Generated unordered dispatch uses `args.As<TRecord>()` to narrow the same unit while preserving provenance, context, and diagnostic sink; it rejects mismatched types. Access custom reconciliation members by casting `args.Context` to the definition's generated context.

Unexpected exceptions and cancellation abort processing and propagate without a content/business-failure finalizer or a successful summary. Partial progress remains on the original context. The caller owns fault handling, durable report recovery, and retry policy. There is no checkpoint/resume or automatic rollback: a retry reads the file again, and earlier business effects require application-owned idempotency or durable completion tracking.

Identity includes definition/version, credential-free source/revision, and a caller-supplied attempt ID. It is not authenticated identity or an automatic business idempotency key. Contexts are pass-local; validation and execution share immutable identity, not mutable reconciliation state. The shared lifecycle uses `CoreEx.ExecutionContext.RunScopedAsync` for fresh unit scopes and registered contexts, with explicit trusted configuration before handler resolution and parent restoration on every exit.

Progress counters are `long`, count validation input once (never again on replay), and distinguish source records, invalid records, candidate/invalid units, validated units, attempted execution, successful execution, and diagnostic severities. Reporting remains an application-owned side effect and has no separate progress counter. Outcomes are independent of mode: `Succeeded`, `ContentFailed`, and `ExecutionFailed`. Summary construction enforces the applicable error, completeness, and execution invariants; whole-file acceptance does not promise atomicity or exactly-once effects. `ValidateAndExecute` validates and executes each complete unit in one pass; later content errors retain earlier execution counts and return `ContentFailed`.

## Executable integration example

The [generation-first inventory import example](../../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md) is defined by `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml` and generated into `InventoryImports/Files`. It demonstrates `InventoryAdjustment`, its generated mapping/validator/context/processor, and a create-only handler while keeping `CoreEx.IO` host-neutral. The native Functions adapter uses the Azure Blob SDK and ETag-pinned input reads; report output is a durable Append Blob.

The application uses a supported SQL Server unit of work to commit inventory changes, durable receipts, and an outbox write atomically. Earlier units may commit before a later unit fails, and outbox persistence does not prove broker publication or exactly-once delivery. The Aspire runtime path uploads a real blob to Azurite, then simulates the Event Grid notification through the Functions webhook; it exercises the actual Functions binding, Blob SDK I/O, SQL Server work, and report writes. Azurite does not emit Event Grid events, so the local proof does not establish Azure subscription provisioning, authentication, or delivery.

## Nested JSONL

Prefer generation from [orders-jsonl-file.yaml](../../tests/CoreEx.CodeGen.Test.Unit/orders-jsonl-file.yaml) or [routed-jsonl-file.yaml](../../tests/CoreEx.CodeGen.Test.Unit/routed-jsonl-file.yaml). Both generate executable consumers under `tests/CoreEx.IO.Test.Unit/Files`. The handwritten `OrdersJsonlHandler` injects `IOrderImportApplication` in the fresh unit scope and preserves its business Result. `GeneratedJsonLinesIntegrationTests` pins both opens to immutable byte content and demonstrates source-revision/unit receipts using an in-memory application double; that demonstrates ownership and retry behavior, not durable transactions or exactly-once delivery.

Direct runtime composition is also supported:

```csharp
static JsonUnitMapping<Order> CreateMapping() => new JsonUnitMapping<Order>()
    .MapString("order_id", x => x.Id)
    .MapObjects("lines", x => x.Lines, new JsonUnitMapping<Line>()
        .MapString("sku", x => x.Sku)
        .MapValue("quantity", x => x.Quantity, JsonFieldConverters.Integer),
        FileCardinality.OneOrMore, maxItems: 1000);

var processor = new JsonLinesFileProcessor<Order>(() => new("Order", CreateMapping()),
    validator: orderValidator);
var context = new JsonLinesContext(new FileProcessingIdentity("orders", "1", "store/orders.jsonl", "attempt", "revision"),
    errorMode: FileErrorMode.CollectErrors);
var summary = await processor.ProcessAsync(openReadAsync, context, cancellationToken).ConfigureAwait(false);
```

`Order`/`Line` are caller-owned nullable wire types with writable direct properties; the optional pure validator validates the mapped nested graph. `MapObject` supports optional/required object occurrences (requiredness belongs in the validator); `MapObjects` and `MapValues` enforce cardinality and optional explicit `maxItems` before element allocation. Missing/null scalars and optional objects map to null. Missing/null collections count zero; an explicit empty array remains an empty list. Null collection elements reject mapping. Mapping/structural failure suppresses downstream pure/scoped validation for the unit.

Member names match exactly and case-sensitively. Unknown members reject by default; `JsonUnknownMemberPolicy.Ignore` still checks syntax, duplicates, and parser depth but does not exhaustively decode ignored string values. Duplicate decoded member names reject before mapping, including ignored objects. Unknown-member diagnostics anchor to the declared parent without echoing untrusted keys. `FileLocation.PropertyPath` uses CLR paths such as `Lines[0].Quantity`; `SourcePath` is a unit-relative escaped JSON Pointer such as `/lines/0/quantity`, combined with the physical JSONL line. Root errors use the empty pointer. `FileUnitInfo.SourceByteOffset` is the actual zero-based source line start, not a read-ahead-distorted stream position; `BytesRead` separately counts actual reads.

Strict native number/Boolean/string tokens are required. Numeric strings, fractional integers, overflow, and nonfinite doubles fail without coercion. Empty/whitespace strings are preserved. Dates use standard invariant .NET parsing when no format is supplied; an explicit format uses exact parsing with no fallback. JSON DateTime rejects offsets; DateTimeOffset requires and preserves `Z` or a numeric offset.

JSONL accepts LF/CRLF and a final nonempty unterminated line, with no phantom record after a final separator. Input is parsed as UTF-8 without encoding autodetection. A UTF-8 BOM produces terminal `jsonl.encoding.bom`; other encoding signatures fail ordinary JSON parsing. Well-framed syntax/depth errors can recover at the next line under CollectErrors. Invalid UTF-8 or escaped surrogates in consumed strings produce localized conversion errors; malformed ignored string content can remain unexamined. There is no separate whole-line UTF-8 validation pass. Blank lines reject by default; explicit `BlankLinePolicy.Skip` excludes them from record/unit counts but not physical line numbering or byte accounting. Plain non-skipped lines are candidate units even when malformed. Routed lines without an identifiable business discriminator are records, not invented units.

Enable routing explicitly with `JsonLinesFileOptions.Discriminator`, a business `JsonLinesDefinition.Match`, and `JsonLinesMetadata<T>` declarations. Matches are exact/disjoint and the direct discriminator member is structural, excluded from unexpected-member checks and DTOs unless explicitly mapped. This delivery supports one business wire type plus bounded zero-or-one metadata routes, not heterogeneous business dispatch. First/Last metadata rules include malformed/unknown non-skipped lines; requiredness is checked at EOF without fabricated coordinates. Metadata validates as encountered but never reaches unit hooks. `JsonLinesContext.GetRecordCount` exposes recognized route counts.

Pass-local metadata is available only after encounter. Immutable accepted metadata is absent during validation/report hooks and single-pass execution, available from the first two-pass execution and successful completion, and typed reads return fresh copies. All formats snapshot declared metadata objects using default System.Text.Json serialization, including serializable handwritten properties. Use `[JsonIgnore]` for transient properties, and keep reconciliation state on the context rather than metadata DTOs. Put trailer-dependent checks in EOF reconciliation; select ValidateThenExecute when handlers need complete metadata.

### Processing policy and parser ownership

The consumer owns source admission, overall input size, concurrency, timeouts, and report-output policy for both CSV and JSONL. CoreEx.IO imposes no file/line/token/metadata byte quota, unit/diagnostic count quota, CSV group-record quota, or default collection maximum. JSON retains the built-in parser's configurable MaxDepth (64 by default), business cardinality, and optional explicitly declared positive collection maxItems. A large JSONL line/CSV hierarchy can consume substantial memory; the framework is an application integration helper, not a bulk-data platform.

JSONL frames lines using the built-in span newline search and calls `JsonDocument.Parse` once per line. On .NET 10+, `AllowDuplicateProperties = false` delegates duplicate rejection to Microsoft; older targets retain a small object-name walk solely for duplicate rejection. Only consumed strings are decoded. Syntax, duplicate, invalid member-name decoding, and depth failures share `jsonl.record.invalid`; CoreEx does not implement JSON tokenization. The reusable line buffer retains capacity for the largest encountered line; the DOM is disposed after each line and completed units are not collected file-wide.

## Flat CSV

The parser dependency is **CsvHelper 34.0.0**, used only for low-level record parsing. CoreEx owns decoding, mapping, validation, diagnostics, and execution. The package is strong-named, offers net8.0/net10.0 assets (net9.0 uses net8.0), has no additional runtime dependencies on those assets, and is licensed `MS-PL OR Apache-2.0`.

Define a new mapping for each pass:

```csharp
static CsvFileMapping<ImportRow> CreateMapping() => new CsvFileMapping<ImportRow>()
    .MapProperty("Id", x => x.Id, CsvFieldConverters.Nullable<int>())
    .MapProperty("Name", x => x.Name, CsvFieldConverters.String)
    .MapProperty("Quantity", x => x.Quantity, CsvFieldConverters.Nullable<int>());

var processor = new CsvFileProcessor<ImportRow>(CreateMapping, validator: rowValidator);
var context = new FileProcessingContext(new FileProcessingIdentity("import", "1", "store/data.csv", "attempt", "revision"));
var summary = await processor.ProcessAsync(openReadAsync, context, cancellationToken).ConfigureAwait(false);
```

In this example, `ImportRow` is a caller-owned class with a public parameterless constructor and writable `int? Id`, `string? Name`, and `int? Quantity` properties. `rowValidator` is an optional pure `IValidator<ImportRow>`. `openReadAsync` is the cancellation-aware factory described above. For positional input, supply a zero-based `index` to each `MapProperty`; if any selector is named, the first nonblank/noncomment record is the column-name header. Named headers are case-insensitive; duplicate or missing required names reject the file before data mapping. Extra fields are allowed by default. Set `CsvFileMapping<TRecord>` to `CsvHeaderPolicy.Exact` to reject extra header columns and data records whose field count differs from the header; exact mode requires every mapped selector to use a named column. An absent mapped field differs from a present blank field.

Converters receive already-unquoted/unescaped text and return `false` for invalid nonblank input. `CsvFieldConverters.Nullable<T>()` uses invariant `ISpanParsable<T>` conversion and preserves zero/false; whitespace-only input becomes null. `String` preserves nonblank whitespace; `DateOnly(format)` performs exact invariant date conversion, and `DateTimeOffset(format)` requires/preserves an explicit offset. Custom `CsvFieldConverter<T>` delegates support other formats/types. Conversion and property-setter programming faults propagate unchanged. CoreEx requiredness remains a validator concern: use `NotNull` rather than `Mandatory` when zero/false is valid.

`CsvFileOptions` defaults to comma, doubled double quotes, no comments, and strict UTF-8. An explicit delimiter, quote, comment character, or encoding can be supplied. A stateful `StreamReader` handles split multibyte sequences; the native CsvHelper UTF-8 stream path is deliberately not used because it silently replaces invalid bytes. CoreEx clones the configured encoding and enforces strict decoding even if the caller supplied a replacement fallback. The encoding/BOM guarantees are detailed below.

Blank lines and configured comment records are skipped without incrementing record counts. Multiline records report their physical starting line, including preceding blanks/comments and CR, LF, or CRLF endings. Malformed quoting produces `csv.record.invalid`; those recovered parser records are never mapped, validated, or executed. Collection continues only along the parser's logical boundaries: an unterminated quoted field can consume the rest of the input, so subsequent physical lines are not invented as independent records.

Other stable diagnostic codes are `csv.header.duplicate`, `csv.header.missing`, `csv.field.missing`, `csv.field.invalid`, and `csv.validation`. Invalid headers stop validation as incomplete because data cannot safely be mapped. A missing header on empty input is rejected after reaching EOF. A record with multiple mapping/validation errors increments `InvalidRecords` once; file reconciliation errors do not invent invalid rows. User hooks choose their own stable business-rule/reconciliation codes.

For dependency-backed validation or execution, pass `serviceProvider` and a `handlerFactory` resolving a registered `FileUnitHandler<ImportRow>` from the supplied scoped provider. Register `AddExecutionContext()` (or its custom factory) and the handler/dependencies; execution without a handler factory fails before opening input. Pure validation runs under the file context, while custom validation and execution use distinct async scopes and contexts for each row and pass. Override `OnConfigureUnitContext(FileUnitContextArgs<TUnit> args)` and configure `args.UnitContext` to propagate trusted identity explicitly before resolving the handler; CSV fields are not authentication. Override `OnCreateExecutionContext` when a derived file context needs fresh replay reconciliation state.

The shared `FileProcessor<TUnit>` exposes protected `Services`, the provider supplied at construction, for coordinator options and report adapters. Its lifetime follows the processor's registration; it is not automatically invocation-scoped and is never the fresh unit scope. Resolving unit business dependencies belongs in the handler factory. DI remains optional for pure validation; accessing `Services` without a supplied provider throws `InvalidOperationException`.

Diagnostic callbacks run under the captured file-level ambient execution context even when emitted by a unit-scoped validator. They are awaited before parsing the next record; execution-context flow must be enabled. Normal EOF invokes `OnValidateFileAsync`, including after collected errors, but fail-fast/invalid-header termination does not. Content failures never open replay. Two-pass execution rebuilds fresh rows/context without repeating validators or custom validation; reconstruction/count drift faults explicitly, but equal-count changed values cannot prove or disprove the caller's immutable-source guarantee.

## Unordered routed CSV

`CsvRoutedFileProcessor<TUnit>` supports heterogeneous records without parent/child grouping. Configure a `CsvRoutedDefinition<TUnit>` with exact discriminator matching and typed `CsvRoutedRecord<TRecord, TUnit>` routes. Each mapped record is independently projected into the common unit type (an application-owned base class, interface, or wrapper) without object casts; a pure route validator still receives its concrete wire type:

```csharp
CsvRoutedDefinition<ImportRecord> CreateDefinition() => new(0,
[
    new CsvRoutedRecord<SupplierRecord, ImportRecord>("supplier", "S", CreateSupplierMapping, row => row,
        validator: supplierValidator, cardinality: FileCardinality.OneOrMore),
    new CsvRoutedRecord<AdjustmentRecord, ImportRecord>("adjustment", "A", CreateAdjustmentMapping, row => row,
        validator: adjustmentValidator, errorMode: FileErrorMode.CollectErrors)
]);

var processor = new CsvRoutedFileProcessor<ImportRecord>(CreateDefinition,
    serviceProvider: serviceProvider,
    handlerFactory: scoped => scoped.GetRequiredService<ImportHandler>());
var context = new CsvRoutedContext(
    new FileProcessingIdentity("imports", "1", "store/import.csv", "attempt", "revision"),
    FileProcessingMode.ValidateThenExecute,
    FileErrorMode.CollectErrors);
var summary = await processor.ProcessAsync(openReadAsync, context, cancellationToken).ConfigureAwait(false);
```

In this example, `SupplierRecord` and `AdjustmentRecord` derive from the caller-owned `ImportRecord`; the mappings are positional, and `ImportHandler : FileUnitHandler<ImportRecord>` dispatches to the appropriate typed application operation. [File CodeGen](../CoreEx.CodeGen/Files/README.md#unordered-routed-definition) generates a definition-specific common base and typed validation/execution dispatch so handwritten handlers do not need casts or a manual type switch.

Routes may occur in any order. `Many` (default), `One`, `Optional`, and `OneOrMore` apply per file and count recognized occurrences even when fields are invalid. An optional `FileRecordPosition.First` or `Last` requires `One` or `Optional` cardinality; at most one route may claim each boundary. First must precede every parsed record, including unknown/malformed records; a required First also forbids records before it. Last forbids every subsequent parsed record. Blank/comment lines do not participate. Boundary records remain ordinary business units unless their route is explicitly referenced by the shared `metadata` collection; declared metadata is still counted as a source record and route occurrence, but never consumes a unit ordinal or reaches unit validation/report/execution hooks. Use `CsvMetadataDefinition` to declare metadata requiredness and its optional Any/First/Last position. A route cannot be both metadata and a business unit.

Exact matches, positional fields, strict encoding, streamed/awaited diagnostics, whole-file acceptance, fresh replay models/context, original application Results, and scoped business handlers share the existing CSV lifecycle. Every accepted non-metadata routed record is one validated/executed unit; no file-wide record collection or root buffer is retained. Input admission belongs to the consumer. Replay never repeats pure or custom validators. Callers remain responsible for an immutable source revision and application-level transactions/idempotency.

Use a fresh `CsvRoutedContext` (or derived context with a matching replay-context override) per invocation. `GetRecordCount(key)` includes recognized invalid records, while unknown/malformed records are not assigned a route. `EndOfFileLineNumber` anchors missing requirements at the last parsed record's physical start, or line 1 for empty input. `HasValidationErrors` reflects streamed validation diagnostics. Put reconciliation accumulators in a derived context and implement `OnValidateFileAsync`; missing/duplicate/position/reconciliation errors prevent all replay, even when earlier records passed validation.

The invocation's `FileProcessingContext.ErrorMode` applies to every mapping, validation, boundary, and missing-occurrence failure; routes have no continuation overrides. Collected errors reach EOF reconciliation; fail-fast skips it and reports incomplete validation. Stable structural codes are `csv.structure.required`, `csv.structure.duplicate`, `csv.structure.before-first`, `csv.structure.first-position`, and `csv.structure.after-last`; discriminator, parsing, mapping, and validation codes are shared with the other CSV modes. Multiple physical errors increment `InvalidRecords` only once, and EOF/reconciliation failures do not invent invalid rows.

## Routed hierarchical CSV

`CsvHierarchyFileProcessor<TGroup>` shares the flat processor's parser, reporting, whole-file barrier, replay, and scoped-handler lifecycle through `FileProcessor<TUnit>` and a CSV-specific parser base. Declare exact discriminator routes, a business root, ordered children, file-level metadata, and a projection from an accepted subtree:

```csharp
CsvHierarchyDefinition<OrderGroup> CreateDefinition() => new(
    discriminatorIndex: 0,
    routes:
    [
        new CsvRecordRoute<BatchHeader>("header", "H", CreateHeaderMapping, headerValidator),
        new CsvRecordRoute<OrderRecord>("order", "O", CreateOrderMapping, orderValidator),
        new CsvRecordRoute<OrderLine>("line", "L", CreateLineMapping, lineValidator),
        new CsvRecordRoute<BatchTrailer>("trailer", "T", CreateTrailerMapping, trailerValidator)
    ],
    root: new CsvHierarchyNode("order", FileCardinality.OneOrMore,
        new CsvHierarchyNode("line", FileCardinality.OneOrMore)),
    project: node => new OrderGroup(node.GetRecord<OrderRecord>(),
        node.GetChildren("line", child => child.GetRecord<OrderLine>())),
    metadata:
    [
        new("Batch", "header", true, FileRecordPosition.First),
        new("Summary", "trailer", true, FileRecordPosition.Last)
    ]);

var processor = new CsvHierarchyFileProcessor<OrderGroup>(CreateDefinition,
    serviceProvider: serviceProvider,
    handlerFactory: scoped => scoped.GetRequiredService<OrderGroupHandler>());

var context = new CsvHierarchyContext(
    new FileProcessingIdentity("orders", "1", "store/orders.csv", "attempt", "revision"),
    FileProcessingMode.ValidateThenExecute,
    FileErrorMode.CollectErrors);

var summary = await processor.ProcessAsync(openReadAsync, context, cancellationToken).ConfigureAwait(false);
```

The wire types, `OrderGroup`, pure validators, and `OrderGroupHandler : FileUnitHandler<OrderGroup>` are caller-owned. The mapping factories return fresh **positional** `CsvFileMapping<T>` instances, including the discriminator field in the zero-based indexes (for example, map a name after `O` at index 1). Named column headers belong to flat files; a typed `H` metadata record is not a column-name header or business unit. Route keys must be unique ignoring case; match values are exact case-sensitive strings with no trimming. Every route must occur exactly once across the business structure and metadata declarations, and a definition has one business-root type. Configuration faults fail before opening input.

Cardinality is per parent for children and per file for the root: `Many` means zero or more, `One` exactly one, `Optional` zero or one, and `OneOrMore` at least one. Metadata permits zero or one occurrence and has no children; set `Required` to require one. Descendants can have arbitrary depth, but subtrees must remain contiguous and siblings follow declared order. Reopening an earlier sibling or placing a child without an active parent rejects the file. A new root or metadata record closes the prior group before handling the new record; EOF closes the final group before required-record checks and reconciliation.

Route mapping and optional pure `IValidator<TRecord>` validation run for each recognized physical record in the validation pass. Only an accepted subtree is projected; its optional pure group validator and scoped custom group validation then run once per root. There is no DI scope per descendant. Replay reconstructs groups without repeating record/group validators or custom validation, and each execution gets a fresh scope. Completed groups are discarded; the framework retains only the current subtree and declared metadata, not a file-wide group collection.

Pass-local `CsvHierarchyContext.GetRecordCount(key)` includes recognized records whose mapping/validation failed, not unknown or syntactically malformed records. Malformed syntax invalidates the active group without trusting a recovered discriminator. File-level records are declared through the shared `metadata` collection, not as hierarchy children; `GetMetadata<T>(name)` exposes validated pass-local values, while `AcceptedMetadata` is absent until whole-file acceptance and holds an immutable JSON-value snapshot. Derive the context for application-owned reconciliation accumulators and override `OnCreateExecutionContext` to return a fresh derived instance preserving identity/mode/policy. A hierarchy context cannot be reused for another pass or invocation.

Accepted metadata deliberately uses `System.Text.Json` values for every source format, isolating the snapshot from mutable wire models without introducing a second metadata type system. Metadata types must round-trip with default `System.Text.Json` behavior; serializer attributes on those types apply. `GetAcceptedMetadata<T>(name)` deserializes a fresh typed copy on every call, so modifying that copy cannot alter the accepted snapshot; reuse a local copy within a handler when needed repeatedly. Generated contexts provide typed accessors. Complete accepted metadata is available from the beginning of replay and at successful completion in every mode, but never during single-pass unit execution.

`CsvAggregateNode` exposes typed records, ordered child projections, and explicit physical provenance through `LineNumber`/`GetLineNumber(record)`. Carry needed coordinates into the projected group for custom diagnostics; logical child/property paths do not imply physical lines. Missing-child diagnostics anchor the parent's physical start; missing file-level occurrences anchor the last logical record's physical start, or line 1 for an empty file. Missing requirements and group/file validation errors do not increment `InvalidRecords`; multiple physical-record errors increment it only once.

The invocation's `FileProcessingContext.ErrorMode` applies uniformly to record, group, boundary, and EOF content failures. There are no route-specific overrides. Fail-fast stops after the current discovered diagnostics and skips the EOF hook, even when the failing rule is discovered during group closure or EOF checks; `ValidationCompleted` is false. Collected errors still permit reconciliation through normal EOF and prevent replay; single-pass execution may already have processed valid groups.

Hierarchy-specific stable codes are `csv.discriminator.missing`, `csv.discriminator.unknown`, `csv.structure.required`, `csv.structure.duplicate`, `csv.structure.orphan`, `csv.structure.child-order`, `csv.metadata.required`, `csv.metadata.duplicate`, `csv.metadata.first-position`, and `csv.metadata.after-last`. Mapping/parsing/validation codes are shared with flat CSV. Override `OnValidateFileAsync` for metadata totals; use pass-local metadata access and `HasValidationErrors` to avoid treating invalid input as trustworthy reconciliation data.

Multiple definitions may share the same projected group CLR type. Register definition-specific facades or explicit keyed processors/handlers; do not register an undifferentiated generic singleton and rely on registration order. Each invocation needs a distinct context, fresh mapping state, and thread-safe shared dependencies.

## Input ownership and encoding

CSV delimiter, quote, and optional comment markers must be distinct single non-surrogate characters other than CR, LF, and NUL. Both YAML preparation and direct runtime construction reject invalid dialects before opening a source. Embedded quotes use doubled-quote escaping.

The consumer owns source admission, overall input size, concurrency, timeouts, and report-output policy for both CSV and JSONL. CoreEx.IO imposes no file/line/token/metadata byte quota, unit/diagnostic count quota, CSV group-record quota, or default collection maximum. JSON retains the built-in parser's configurable MaxDepth (64 by default), business cardinality, and optional explicitly declared positive collection maxItems. A large JSONL line/CSV hierarchy can consume substantial memory; the framework is an application integration helper, not a bulk-data platform.

The source factory need not support seeking or expose a length. Reading and source disposal remain asynchronous, with cancellation propagated. Source I/O faults propagate unchanged. Replay encoding/shape drift is an operational fault, not a late content rejection after possible earlier writes. Earlier committed units are not rolled back; the immutable-source contract still applies.

`StreamReader` handles the configured encoding's preamble with BOM autodetection disabled; CsvHelper also handles a leading U+FEFF. Default UTF-8 and CodeGen's UTF-16/32 encodings accept matching leading BOMs or BOM-less data; a later embedded U+FEFF remains input data. There is no custom conflicting-BOM check or `csv.encoding.bom` diagnostic: mismatched signatures may fail decoding or decode as unwanted characters. The configured encoding always remains authoritative; there is no statistical/locale-based encoding guessing.

Invalid byte sequences, truncated multibyte sequences, and incomplete UTF-16/32 code units produce `csv.encoding.invalid`. Decoding failures stop with incomplete rejection and no EOF hook or replay. No physical line is guessed for an encoding error because decoder/parser read-ahead may precede record dispatch. The strict .NET decoder throws `DecoderFallbackException` internally; CoreEx catches only that failure at the decoding boundary and emits one terminal diagnostic. This is not a zero-exception decoder claim. Normal field/structure failures continue to avoid per-record exception control flow; operational I/O and application callback faults still propagate.

Tests cover strict Unicode/BOM behavior, nonseekable split asynchronous reads, exact byte accounting, caller-owned cancellation/fault propagation, processing beyond the removed quotas, business collection bounds, and replay failure semantics.

## Observability

Every file-processing attempt uses `FileProcessorInvoker` and the standard CoreEx invoker tracing/configuration pipeline (`ActivitySource` name `CoreEx.IO`). Traces identify definition/version and policies, then capture outcome and final or partial record/unit, execution, byte, and error counts through reporting completion, faults, and cancellation. Reporting has no dedicated progress counter. Source identity, source revision, attempt identifiers, and raw field values are not automatically attached. Outcome logs use the optional service provider's logger factory and its level filters; register a custom `FileProcessorInvoker` there to customize the invoker behavior. Existing CoreEx settings control standard invoker tracing/debug logs.

Subscribe to the `FileProcessingTelemetry.MeterName` meter (`CoreEx.IO`) for `file.files.processed`, one increment per attempt, with only the bounded `file.outcome` dimension: `succeeded`, `content-failed`, `execution-failed`, `faulted`, or `cancelled`. Retries count as separate attempts; a reporting failure is faulted rather than successful. Instrumentation stores no diagnostic history. Configure normal host OpenTelemetry subscriptions/exporters; this package does not select an exporter or require one.
