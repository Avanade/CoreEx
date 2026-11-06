# CoreEx.IO

Host-neutral file processing with format-neutral contracts, safe nested execution contexts, CSV validation and execution, finite input/group bounds, strict encoding/BOM handling, [File CodeGen](../CoreEx.CodeGen/Files/README.md), and the [generation-first Functions inventory example](../../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md).

Shared contracts live in `CoreEx.IO`. CSV mappings, converters, options, `CsvFileProcessor<TRecord>`, and `CsvHierarchyFileProcessor<TGroup>` live in `CoreEx.IO.Csv`. The package does not impose CSV line/header assumptions on its shared contracts and exposes no universal parser/plugin abstraction.

`FileProcessor.ProcessAsync` accepts a cancellation-aware stream-opening factory and an invocation-local `FileProcessingContext`, and returns `FileProcessingSummary` directly. The caller guarantees identical immutable bytes on every open; the processor owns/disposes the streams. Validation-only is the default. Validate-then-execute must complete whole-file validation, including EOF reconciliation, before opening a replay stream or performing any execution. Validate-and-report invokes an awaited callback for each unit that passes every unit-level check; it never executes business units and does not imply whole-file acceptance.

`FileDiagnostic` uses CoreEx `MessageType` and `LText`, a stable code/category, and optional `FileLocation` and logical record type. `FileLocation` separates one-based physical line/character position, zero-based field index/name, and opaque logical property paths. Physical coordinates are not mandatory, and raw input values are not automatically captured. File-wide diagnostic history is not part of the context or summary.

`FileUnitHandler<TUnit>` separates row/root-group lifetimes from the file coordinator. Its custom validation emits diagnostics through an awaited sink; its execution returns the application's original `Result`. Generated validation uses CoreEx validator messages without per-record exceptions. Default execution throws `NotSupportedException` rather than reporting false success.

The file coordinator exposes the reporting hooks `OnDiagnosticAsync`, `OnRejectedAsync`, and `OnCompletedAsync`, plus `OnValidateFileAsync` for EOF reconciliation. CSV processors additionally expose `OnValidatedUnitAsync` in ValidateAndReport mode; it receives only units that passed mapping, pure validation, and scoped custom validation, with physical/logical location metadata. Override this hook before using the mode; the default throws rather than silently counting a no-op as a report. Reports are ordered/awaited; hooks do not choose a built-in report format or destination. A later row or EOF error may reject the file after earlier valid units were reported; `ReportedWithErrors` is unsuccessful and preserves accurate report counts. Business Result failure returns `ExecutionFailed` with attempted/successful counts and the original failure; operational faults and cancellation propagate. Neither terminal reporting hook runs for those failures.

Identity includes definition/version, credential-free source/revision, and a caller-supplied attempt ID. It is not authenticated identity or an automatic business idempotency key. Contexts are pass-local; validation and execution share immutable identity, not mutable reconciliation state. The CSV engine uses `CoreEx.ExecutionContext.RunScopedAsync` for fresh unit scopes and registered contexts, with explicit trusted configuration before handler resolution and parent restoration on every exit.

Progress counters are `long`, count validation input once (never again on replay), and distinguish physical records, invalid records, validated units, completed report callbacks, attempted execution, successful execution, and diagnostic severities. `UnitsReported` counts callbacks that returned successfully, not durable report writes. Summary construction enforces outcome/mode/error/completeness invariants; whole-file acceptance does not promise atomicity or exactly-once effects.

## Executable integration example

The [generation-first inventory import example](../../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md) is defined by `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml` and generated into `InventoryImports/Files`. It demonstrates `InventoryAdjustment`, its generated mapping/validator/context/processor, and a create-only handler while keeping `CoreEx.IO` host-neutral. The native Functions adapter uses the Azure Blob SDK and ETag-pinned input reads; report output is a durable Append Blob.

The application uses a supported SQL Server unit of work to commit inventory changes, durable receipts, and an outbox write atomically. Earlier units may commit before a later unit fails, and outbox persistence does not prove broker publication or exactly-once delivery. The Aspire runtime path uploads a real blob to Azurite, then simulates the Event Grid notification through the Functions webhook; it exercises the actual Functions binding, Blob SDK I/O, SQL Server work, and report writes. Azurite does not emit Event Grid events, so the local proof does not establish Azure subscription provisioning, authentication, or delivery.

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

Converters receive already-unquoted/unescaped text and return `false` for invalid nonblank input. `CsvFieldConverters.Nullable<T>()` uses invariant `ISpanParsable<T>` conversion and preserves zero/false; whitespace-only input becomes null. `String` preserves nonblank whitespace; `DateOnly(format)` performs exact invariant date conversion. Custom `CsvFieldConverter<T>` delegates support other formats/types. Conversion and property-setter programming faults propagate unchanged. CoreEx requiredness remains a validator concern: use `NotNull` rather than `Mandatory` when zero/false is valid.

`CsvFileOptions` defaults to comma, doubled double quotes, no comments, and strict UTF-8. An explicit delimiter, quote, comment character, or encoding can be supplied. A stateful `StreamReader` handles split multibyte sequences; the native CsvHelper UTF-8 stream path is deliberately not used because it silently replaces invalid bytes. CoreEx clones the configured encoding and enforces strict decoding even if the caller supplied a replacement fallback. The encoding/BOM and limit guarantees are detailed below.

Blank lines and configured comment records are skipped without incrementing record counts. Multiline records report their physical starting line, including preceding blanks/comments and CR, LF, or CRLF endings. Malformed quoting produces `csv.record.invalid`; those recovered parser records are never mapped, validated, or executed. Collection continues only along the parser's logical boundaries: an unterminated quoted field can consume the rest of the input, so subsequent physical lines are not invented as independent records.

Other stable diagnostic codes are `csv.header.duplicate`, `csv.header.missing`, `csv.field.missing`, `csv.field.invalid`, and `csv.validation`. Invalid headers stop validation as incomplete because data cannot safely be mapped. A missing header on empty input is rejected after reaching EOF. A record with multiple mapping/validation errors increments `InvalidRecords` once; file reconciliation errors do not invent invalid rows. User hooks choose their own stable business-rule/reconciliation codes.

For dependency-backed validation or execution, pass `serviceProvider` and a `handlerFactory` resolving a registered `FileUnitHandler<ImportRow>` from the supplied scoped provider. Register `AddExecutionContext()` (or its custom factory) and the handler/dependencies; execution without a handler factory fails before opening input. Pure validation runs under the file context, while custom validation and execution use distinct async scopes and contexts for each row and pass. Override `OnConfigureUnitContext` to propagate trusted identity explicitly; CSV fields are not authentication. Override `OnCreateExecutionContext` when a derived file context needs fresh replay reconciliation state.

Diagnostic callbacks run under the captured file-level ambient execution context even when emitted by a unit-scoped validator. They are awaited before parsing the next record; execution-context flow must be enabled. Normal EOF invokes `OnValidateFileAsync`, including after collected errors, but fail-fast/invalid-header termination does not. Rejected files never open replay. Valid execution rebuilds fresh rows/context without repeating validators or custom validation; reconstruction/count drift faults explicitly, but equal-count changed values cannot prove or disprove the caller's immutable-source guarantee.

## Unordered routed CSV

`CsvRoutedFileProcessor<TUnit>` supports heterogeneous records without parent/child grouping. Configure a `CsvRoutedDefinition<TUnit>` with exact discriminator matching and typed `CsvRoutedRecord<TRecord, TUnit>` routes. Each mapped record is independently projected into the common unit type (an application-owned base class, interface, or wrapper) without object casts; a pure route validator still receives its concrete wire type:

```csharp
CsvRoutedDefinition<ImportRecord> CreateDefinition() => new(0,
[
    new CsvRoutedRecord<SupplierRecord, ImportRecord>("supplier", "S", CreateSupplierMapping, row => row,
        validator: supplierValidator, cardinality: CsvRecordCardinality.OneOrMore),
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

Routes may occur in any order. `Many` (default), `One`, `Optional`, and `OneOrMore` apply per file and count recognized occurrences even when fields are invalid. An optional `CsvRecordPosition.First` or `Last` requires `One` or `Optional` cardinality; at most one route may claim each boundary. First must precede every parsed record, including unknown/malformed records; a required First also forbids records before it. Last forbids every subsequent parsed record. Blank/comment lines do not participate. Boundary records are ordinary business units here, not hierarchy envelopes: they receive their own validation and execution scopes.

Exact matches, positional fields, strict encoding, finite input-byte limits, streamed/awaited diagnostics, whole-file acceptance, fresh replay models/context, original application Results, and scoped business handlers share the existing CSV lifecycle. Every accepted physical record is one validated/executed unit; no file-wide record collection or root buffer is retained. `MaxGroupRecords` is irrelevant to unordered input, while the independent positive `MaxInputBytes` budget still bounds input. Replay never repeats pure or custom validators. Callers remain responsible for an immutable source revision and application-level transactions/idempotency.

Use a fresh `CsvRoutedContext` (or derived context with a matching replay-context override) per invocation. `GetRecordCount(key)` includes recognized invalid records, while unknown/malformed records are not assigned a route. `EndOfFileLineNumber` anchors missing requirements at the last parsed record's physical start, or line 1 for empty input. `HasValidationErrors` reflects streamed validation diagnostics. Put reconciliation accumulators in a derived context and implement `OnValidateFileAsync`; missing/duplicate/position/reconciliation errors prevent all replay, even when earlier records passed validation.

Record-specific policies apply to mapping, pure/custom validation, and rules owned by the route, including missing occurrences. A record after Last uses the Last route's policy, and a record before required First uses the First route's policy, rather than the current record's override. Unknown/malformed records use the file policy. Collected errors reach EOF reconciliation; fail-fast skips it and reports incomplete validation. Stable structural codes are `csv.structure.required`, `csv.structure.duplicate`, `csv.structure.before-first`, `csv.structure.first-position`, and `csv.structure.after-last`; discriminator, parsing, mapping, and validation codes are shared with the other CSV modes. Multiple physical errors increment `InvalidRecords` only once, and EOF/reconciliation failures do not invent invalid rows.

## Routed hierarchical CSV

`CsvHierarchyFileProcessor<TGroup>` shares the flat processor's parser, reporting, whole-file barrier, replay, and scoped-handler lifecycle through `CsvFileProcessorBase<TUnit>`. Declare exact discriminator routes, a business root, ordered children, optional first/last envelopes, and a projection from an accepted subtree:

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
    root: new CsvHierarchyNode("order", CsvRecordCardinality.OneOrMore,
        new CsvHierarchyNode("line", CsvRecordCardinality.OneOrMore)),
    project: node => new OrderGroup(node.GetRecord<OrderRecord>(),
        node.GetChildren("line", child => child.GetRecord<OrderLine>())),
    header: new CsvHierarchyNode("header", CsvRecordCardinality.One),
    trailer: new CsvHierarchyNode("trailer", CsvRecordCardinality.One));

var processor = new CsvHierarchyFileProcessor<OrderGroup>(CreateDefinition,
    serviceProvider: serviceProvider,
    handlerFactory: scoped => scoped.GetRequiredService<OrderGroupHandler>());

var context = new CsvHierarchyContext(
    new FileProcessingIdentity("orders", "1", "store/orders.csv", "attempt", "revision"),
    FileProcessingMode.ValidateThenExecute,
    FileErrorMode.CollectErrors);

var summary = await processor.ProcessAsync(openReadAsync, context, cancellationToken).ConfigureAwait(false);
```

The wire types, `OrderGroup`, pure validators, and `OrderGroupHandler : FileUnitHandler<OrderGroup>` are caller-owned. The mapping factories return fresh **positional** `CsvFileMapping<T>` instances, including the discriminator field in the zero-based indexes (for example, map a name after `O` at index 1). Named column headers belong to flat files; a typed `H` envelope is a data record, not a column-name header. Route keys must be unique ignoring case; match values are exact case-sensitive strings with no trimming. Every route must occur exactly once in the declared structure, and a definition has one business-root type. Configuration faults fail before opening input.

Cardinality is per parent for children and per file for the root: `Many` means zero or more, `One` exactly one, `Optional` zero or one, and `OneOrMore` at least one. Envelopes support `One` or `Optional` and have no children. Descendants can have arbitrary depth, but subtrees must remain contiguous and siblings follow declared order. Reopening an earlier sibling or placing a child without an active parent rejects the file. A new root or trailer closes the prior group before handling the new record; EOF closes the final group before required-record checks and reconciliation.

Route mapping and optional pure `IValidator<TRecord>` validation run for each recognized physical record in the validation pass. Only an accepted subtree is projected; its optional pure group validator and scoped custom group validation then run once per root. There is no DI scope per descendant. Replay reconstructs groups without repeating record/group validators or custom validation, and each execution gets a fresh scope. Completed groups are discarded; the framework retains only the bounded current subtree and accepted header/trailer, not a file-wide group collection.

Pass-local `CsvHierarchyContext.GetRecordCount(key)` includes recognized records whose mapping/validation failed, not unknown or syntactically malformed records. Malformed syntax invalidates the active group without trusting a recovered discriminator. `HeaderAvailable`/`TrailerAvailable` and `GetHeader<T>()`/`GetTrailer<T>()` expose only accepted envelopes; a duplicate never replaces the first accepted value. Derive the context for application-owned typed accessors and reconciliation accumulators, and override `OnCreateExecutionContext` to return a fresh derived instance preserving identity/mode/policy. A hierarchy context cannot be reused for another pass or invocation.

`CsvAggregateNode` exposes typed records, ordered child projections, and explicit physical provenance through `LineNumber`/`GetLineNumber(record)`. Carry needed coordinates into the projected group for custom diagnostics; logical child/property paths do not imply physical lines. Missing-child diagnostics anchor the parent's physical start; missing file-level occurrences anchor the last logical record's physical start, or line 1 for an empty file. Missing requirements and group/file validation errors do not increment `InvalidRecords`; multiple physical-record errors increment it only once.

Route-specific `errorMode` overrides the file default for that route's mapping/field failures and rules it owns. Missing-child rules use the missing child's policy, group validation uses the root policy, and records after the trailer use the trailer policy. Unknown/malformed records use the file policy. Fail-fast stops after the current discovered diagnostics and skips the EOF hook, even when the failing rule is discovered during group closure or EOF checks; `ValidationCompleted` is false. Collected errors still permit reconciliation through normal EOF but always prevent every execution.

Hierarchy-specific stable codes are `csv.discriminator.missing`, `csv.discriminator.unknown`, `csv.structure.required`, `csv.structure.duplicate`, `csv.structure.orphan`, `csv.structure.child-order`, `csv.structure.before-header`, `csv.structure.header-position`, and `csv.structure.after-trailer`. Mapping/parsing/validation codes are shared with flat CSV. Override `OnValidateFileAsync` for trailer counts/totals; use accepted envelope availability and `HasValidationErrors` to avoid treating invalid input as trustworthy reconciliation data.

Multiple definitions may share the same projected group CLR type. Register definition-specific facades or explicit keyed processors/handlers; do not register an undifferentiated generic singleton and rely on registration order. Each invocation needs a distinct context, fresh mapping state, and thread-safe shared dependencies.

## Bounds and encoding

CSV delimiter, quote, and optional comment markers must be distinct single non-surrogate characters other than CR, LF, and NUL. Both YAML preparation and direct runtime construction reject invalid dialects before opening a source. Embedded quotes use doubled-quote escaping.

Both processors enforce `CsvFileOptions.MaxInputBytes` (default **64 MiB**, including a BOM) on each pass. Hierarchical processing additionally enforces `MaxGroupRecords` (default **10,000**, including the root and all parser-returned descendants, even invalid/unknown ones). Both values must be positive; there is no automatic unbounded mode. The group count resets at a new root, excludes file envelopes outside the group, and counts a multiline logical CSV record once rather than counting its physical lines. Blank/comment records skipped by the parser still consume the byte budget. Limits apply before mapping/projecting the over-budget record.

Byte overflow produces `csv.limit.input-bytes`; group overflow produces `csv.limit.group-records` with the offending record's physical start and root key. Either is a terminal `Limit` diagnostic regardless of `FailFast`/`CollectErrors` or route overrides. Processing reports rejection with `ValidationCompleted=false`, does not close a partial group or run EOF reconciliation, and never opens replay. Limits are not successful truncation. Group/file limit errors do not invent invalid physical rows. The stream wrapper reads no more than the byte limit plus one lookahead byte needed to distinguish exact-size EOF from overflow; `BytesRead` includes that actual lookahead, which is never sent to the decoder/parser. Buffers can read ahead of record dispatch, so limits do not promise an exact count of accepted prefix records.

The source factory need not support seeking or expose a length. Prefix inspection, the EOF probe, regular reading, and source disposal remain asynchronous, with cancellation propagated. On replay, new bounds/encoding failures are `InvalidDataException` faults, not late content rejections after possible earlier writes. Earlier committed units are not rolled back; the immutable-source contract still applies.

UTF-8 accepts an optional leading `EF BB BF` BOM. Explicit UTF-16 LE/BE and UTF-32 LE/BE accept their matching BOM or BOM-less data; the processor never switches to a different encoding based on a BOM. Standard Unicode signatures are matched longest-first (UTF-32 before UTF-16). A conflicting signature produces `csv.encoding.bom`, not a silently changed encoding. Only one leading BOM is consumed; a later U+FEFF is input data. Empty/BOM-only positional input has zero records; a partial signature is decoded strictly, not silently discarded. Other explicitly supplied .NET encodings retain their own repertoire with strict decoding; there is no statistical/locale-based encoding guessing.

Invalid byte sequences, truncated multibyte sequences, and incomplete UTF-16/32 code units produce `csv.encoding.invalid`. BOM conflicts and decoding failures stop with incomplete rejection and no EOF hook or replay. No physical line is guessed for an encoding error because decoder/parser read-ahead may precede record dispatch. The strict .NET decoder throws `DecoderFallbackException` internally; CoreEx catches only that failure at the decoding boundary and emits one terminal diagnostic. This is not a zero-exception decoder claim. Normal field/structure failures and numeric limit exhaustion continue to avoid per-record exception control flow; operational I/O and application callback faults still propagate.

The defaults are configurable operating budgets, **not** per-invocation heap ceilings or a throughput SLA. A local net10 Debug characterization of 100,000 64-character flat rows (6.5 MB source, scoped validation) allocated about 222 MB cumulatively; a single 10,000-record root with 1,024-character children (10.3 MB source) allocated about 57 MB cumulatively. These include temporary managed allocations, not measured peak/live memory; elapsed time and allocations vary by framework, host, schema, and handler. The 10,000-record default accommodates the measured roughly-10-MB root while preventing unchecked subtree growth; the independent 64-MiB byte cap bounds wide records, malformed framing, comments, and parser input even when record count stays small. Parser/decoded-model overhead can substantially exceed input size, and a single record can consume much of the byte budget. Lower limits for hostile uploads, constrain concurrent files, and apply host timeouts/authentication/admission controls. Caller hooks/validators can retain their own data or perform arbitrary work; framework limits cannot bound that.

Tests cover runtime and CsvHelper strong-name loading, exact byte/group boundaries, Unicode/BOM behavior, split nonseekable asynchronous reads, cancellation, and replay failure semantics on net8.0, net9.0, and net10.0.

## Observability

Every CSV processing attempt uses `CsvFileProcessorInvoker` and the standard CoreEx invoker tracing/configuration pipeline (`ActivitySource` name `CoreEx.IO.Csv`). Traces identify definition/version and policies, then capture outcome and final or partial progress, including `csv.units_reported`, through reporting completion, faults, and cancellation. Source identity, source revision, attempt identifiers, and raw field values are not automatically attached. Outcome logs use the optional service provider's logger factory and its level filters; register a custom `CsvFileProcessorInvoker` there to customize the invoker behavior. Existing CoreEx settings control standard invoker tracing/debug logs.

Subscribe to the `CsvProcessingTelemetry.MeterName` meter (`CoreEx.IO.Csv`) for `csv.files.processed`, one increment per attempt, with only the bounded `csv.outcome` dimension: `validated`, `executed`, `rejected`, `execution-failed`, `reported`, `reported-with-errors`, `faulted`, or `cancelled`. Retries count as separate attempts; a reporting failure is faulted rather than successful. Instrumentation stores no diagnostic history. Configure normal host OpenTelemetry subscriptions/exporters; this package does not select an exporter or require one.
