# CoreEx.IO.Csv

CSV parsing, mapping, validation, and processing for flat rows, unordered discriminator records, and contiguous ordered hierarchies. All processors use the [shared CoreEx.IO lifecycle](../README.md#processing-lifecycle), including reporting, scoped handlers, immutable replay, and accounting.

## Key types

| Type | Purpose |
|---|---|
| [`CsvFileProcessor<TRecord>`](./CsvFileProcessor.cs) | Processes independent flat rows. |
| [`CsvRoutedFileProcessor<TUnit>`](./CsvRoutedFileProcessor.cs) | Processes unordered heterogeneous records projected into a common unit type. |
| [`CsvHierarchyFileProcessor<TGroup>`](./CsvHierarchyFileProcessor.cs) | Processes ordered parent/child subtrees projected into business groups. |
| [`CsvFileMapping<TRecord>`](./CsvFileMapping.cs) | Configures named or positional fields and their converters. |
| [`CsvFieldConverters`](./CsvFieldConverters.cs) | Supplies invariant nullable scalar and date conversions. |
| [`CsvFileOptions`](./CsvFileOptions.cs) | Configures dialect and strict decoding. |
| [`CsvRoutedDefinition<TUnit>`](./CsvRoutedDefinition.cs) | Declares unordered routes and optional metadata. |
| [`CsvHierarchyDefinition<TGroup>`](./CsvHierarchyDefinition.cs) | Declares routes, an ordered business structure, metadata, and group projection. |
| [`CsvAggregateNode`](./CsvAggregateNode.cs) | Exposes typed subtree records, child projections, and physical provenance. |

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

In this example, `ImportRow` is a caller-owned class with a public parameterless constructor and writable `int? Id`, `string? Name`, and `int? Quantity` properties. `rowValidator` is an optional pure `IValidator<ImportRow>`. `openReadAsync` is the cancellation-aware source factory described in the [package guide](../README.md#processing-lifecycle). For positional input, supply a zero-based `index` to each `MapProperty`; if any selector is named, the first nonblank/noncomment record is the column-name header. Named headers are case-insensitive; duplicate or missing required names reject the file before data mapping. Extra fields are allowed by default. Set `CsvFileMapping<TRecord>` to `CsvHeaderPolicy.Exact` to reject extra header columns and data records whose field count differs from the header; exact mode requires every mapped selector to use a named column. An absent mapped field differs from a present blank field.

Converters receive already-unquoted/unescaped text and return `false` for invalid nonblank input. `CsvFieldConverters.Nullable<T>()` uses invariant `ISpanParsable<T>` conversion and preserves zero/false; whitespace-only input becomes null. `String` preserves nonblank whitespace; `DateOnly(format)` performs exact invariant date conversion, and `DateTimeOffset(format)` requires/preserves an explicit offset. Custom `CsvFieldConverter<T>` delegates support other formats/types. Conversion and property-setter programming faults propagate unchanged. CoreEx requiredness remains a validator concern: use `NotNull` rather than `Mandatory` when zero/false is valid.

`CsvFileOptions` defaults to comma, doubled double quotes, no comments, and strict UTF-8. An explicit delimiter, quote, comment character, or encoding can be supplied. A stateful `StreamReader` handles split multibyte sequences; the native CsvHelper UTF-8 stream path is deliberately not used because it silently replaces invalid bytes. CoreEx clones the configured encoding and enforces strict decoding even if the caller supplied a replacement fallback. See [input ownership and encoding](#input-ownership-and-encoding) for BOM behavior.

Blank lines and configured comment records are skipped without incrementing record counts. Multiline records report their physical starting line, including preceding blanks/comments and CR, LF, or CRLF endings. Malformed quoting produces `csv.record.invalid`; those recovered parser records are never mapped, validated, or executed. Collection continues only along the parser's logical boundaries: an unterminated quoted field can consume the rest of the input, so subsequent physical lines are not invented as independent records.

Other stable diagnostic codes are `csv.header.duplicate`, `csv.header.missing`, `csv.field.missing`, `csv.field.invalid`, and `csv.validation`. Invalid headers stop validation as incomplete because data cannot safely be mapped. A missing header on empty input is rejected after reaching EOF. A record with multiple mapping/validation errors increments `InvalidRecords` once; file reconciliation errors do not invent invalid rows. User hooks choose their own stable business-rule/reconciliation codes.

For dependency-backed validation or execution, pass `serviceProvider` and a `handlerFactory` resolving a registered `FileUnitHandler<ImportRow>` from the supplied scoped provider. Register `AddExecutionContext()` (or its custom factory) and the handler/dependencies; execution without a handler factory fails before opening input. Pure validation runs under the file context, while custom validation and execution use distinct async scopes and contexts for each row and pass. Override `OnConfigureUnitContext(FileUnitContextArgs<TUnit> args)` and configure `args.UnitContext` to propagate trusted identity explicitly before resolving the handler; CSV fields are not authentication. Override `OnCreateExecutionContext` when a derived file context needs fresh replay reconciliation state.

Diagnostic callbacks run under the captured file-level ambient execution context even when emitted by a unit-scoped validator. They are awaited before parsing the next record; execution-context flow must be enabled. Normal EOF invokes `OnValidateFileAsync`, including after collected errors, but fail-fast/invalid-header termination does not. Content failures never open replay. Two-pass execution rebuilds fresh rows/context without repeating validators or custom validation; reconstruction/count drift faults explicitly, but equal-count changed values cannot prove or disprove the caller's immutable-source guarantee.

## Unordered routed CSV

`CsvRoutedFileProcessor<TUnit>` supports heterogeneous records without parent/child grouping. Configure a `CsvRoutedDefinition<TUnit>` with exact discriminator matching and typed `CsvRoutedRecord<TRecord, TUnit>` routes. Each mapped record is independently projected into the common unit type (an application-owned base class, interface, or wrapper) without object casts; a pure route validator still receives its concrete wire type:

```csharp
CsvRoutedDefinition<ImportRecord> CreateDefinition() => new(0,
[
    new CsvRoutedRecord<SupplierRecord, ImportRecord>("supplier", "S", CreateSupplierMapping, row => row,
        validator: supplierValidator, cardinality: FileCardinality.OneOrMore),
    new CsvRoutedRecord<AdjustmentRecord, ImportRecord>("adjustment", "A", CreateAdjustmentMapping, row => row,
        validator: adjustmentValidator)
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

In this example, `SupplierRecord` and `AdjustmentRecord` derive from the caller-owned `ImportRecord`; the mappings are positional, and `ImportHandler : FileUnitHandler<ImportRecord>` dispatches to the appropriate typed application operation. [File CodeGen](../../CoreEx.CodeGen/Files/README.md#unordered-routed-definition) generates a definition-specific common base and typed validation/execution dispatch so handwritten handlers do not need casts or a manual type switch.

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

Accepted metadata follows the [shared metadata contract](../README.md#metadata): typed reads return fresh copies, and complete accepted metadata is available from the beginning of replay and at successful completion, never during single-pass unit execution.

`CsvAggregateNode` exposes typed records, ordered child projections, and explicit physical provenance through `LineNumber`/`GetLineNumber(record)`. Carry needed coordinates into the projected group for custom diagnostics; logical child/property paths do not imply physical lines. Missing-child diagnostics anchor the parent's physical start; missing file-level occurrences anchor the last logical record's physical start, or line 1 for an empty file. Missing requirements and group/file validation errors do not increment `InvalidRecords`; multiple physical-record errors increment it only once.

The invocation's `FileProcessingContext.ErrorMode` applies uniformly to record, group, boundary, and EOF content failures. There are no route-specific overrides. Fail-fast stops after the current discovered diagnostics and skips the EOF hook, even when the failing rule is discovered during group closure or EOF checks; `ValidationCompleted` is false. Collected errors still permit reconciliation through normal EOF and prevent replay; single-pass execution may already have processed valid groups.

Hierarchy-specific stable codes are `csv.discriminator.missing`, `csv.discriminator.unknown`, `csv.structure.required`, `csv.structure.duplicate`, `csv.structure.orphan`, `csv.structure.child-order`, `csv.metadata.required`, `csv.metadata.duplicate`, `csv.metadata.first-position`, and `csv.metadata.after-last`. Mapping/parsing/validation codes are shared with flat CSV. Override `OnValidateFileAsync` for metadata totals; use pass-local metadata access and `HasValidationErrors` to avoid treating invalid input as trustworthy reconciliation data.

Multiple definitions may share the same projected group CLR type. Register definition-specific facades or explicit keyed processors/handlers; do not register an undifferentiated generic singleton and rely on registration order. Each invocation needs a distinct context, fresh mapping state, and thread-safe shared dependencies.

## Input ownership and encoding

CSV delimiter, quote, and optional comment markers must be distinct single non-surrogate characters other than CR, LF, and NUL. Both YAML preparation and direct runtime construction reject invalid dialects before opening a source. Embedded quotes use doubled-quote escaping.

Source admission, input size, concurrency, timeouts, and reporting remain [consumer-owned](../README.md#input-ownership). There is no file/record/group byte quota or default group-record maximum. A large CSV hierarchy can consume substantial memory.

`StreamReader` handles the configured encoding's preamble with BOM autodetection disabled; CsvHelper also handles a leading U+FEFF. Default UTF-8 and CodeGen's UTF-16/32 encodings accept matching leading BOMs or BOM-less data; a later embedded U+FEFF remains input data. There is no custom conflicting-BOM check or `csv.encoding.bom` diagnostic: mismatched signatures may fail decoding or decode as unwanted characters. The configured encoding always remains authoritative; there is no statistical/locale-based encoding guessing.

Invalid byte sequences, truncated multibyte sequences, and incomplete UTF-16/32 code units produce `csv.encoding.invalid`. Decoding failures stop with incomplete rejection and no EOF hook or replay. No physical line is guessed for an encoding error because decoder/parser read-ahead may precede record dispatch. The strict .NET decoder throws `DecoderFallbackException` internally; CoreEx catches only that failure at the decoding boundary and emits one terminal diagnostic. This is not a zero-exception decoder claim. Normal field/structure failures continue to avoid per-record exception control flow; operational I/O and application callback faults still propagate.

Tests cover strict Unicode/BOM behavior, nonseekable split asynchronous reads, exact byte accounting, caller-owned cancellation/fault propagation, processing beyond the removed quotas, business collection bounds, and replay failure semantics.

## Related documentation

- [CoreEx.IO](../README.md): shared lifecycle, handlers, metadata, reporting, and telemetry.
- [File CodeGen](../../CoreEx.CodeGen/Files/README.md): generation-first definitions and handler starters.
- [Functions inventory examples](../../../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md#example-matrix): simple flat CSV and hierarchical CSV with header/trailer metadata, child rows, EOF reconciliation, immutable Blob reads, and durable reporting.
