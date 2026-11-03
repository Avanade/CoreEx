# CoreEx.IO

**Work in progress; not packable or included in release publishing.** Steps 1 through 4 provide format-neutral contracts, safe nested execution contexts, and flat/hierarchical CSV validation and execution. Resource limits/full encoding policy, CodeGen, and release integration remain planned. See the [design and implementation plan](../../docs/file-processing-design.md).

Shared types live in `CoreEx.IO`. CSV mappings, converters, options, `CsvFileProcessor<TRecord>`, and `CsvHierarchyFileProcessor<TGroup>` live in `CoreEx.IO.Csv`; future formats can reuse the root contracts without CSV line/header assumptions. No universal parser/plugin abstraction is being introduced.

`FileProcessor.ProcessAsync` accepts a cancellation-aware stream-opening factory and an invocation-local `FileProcessingContext`, and returns `FileProcessingSummary` directly. The caller guarantees identical immutable bytes on every open; the processor owns/disposes the streams. Validation-only is the default. Validate-then-execute must complete whole-file validation, including EOF reconciliation, before opening a replay stream or performing any execution.

`FileDiagnostic` uses CoreEx `MessageType` and `LText`, a stable code/category, and optional `FileLocation` and logical record type. `FileLocation` separates one-based physical line/character position, zero-based field index/name, and opaque logical property paths. Physical coordinates are not mandatory, and raw input values are not automatically captured. File-wide diagnostic history is not part of the context or summary.

`FileUnitHandler<TUnit>` separates row/root-group lifetimes from the file coordinator. Its custom validation emits diagnostics through an awaited sink; its execution returns the application's original `Result`. Generated validation will use CoreEx validator messages without per-record exceptions. Default execution throws `NotSupportedException` rather than reporting false success.

The file coordinator exposes the three reporting hooks `OnDiagnosticAsync`, `OnRejectedAsync`, and `OnCompletedAsync`, plus `OnValidateFileAsync` for EOF reconciliation. Reports are ordered/awaited; hooks do not choose a built-in report format or destination. Business Result failure returns `ExecutionFailed` with attempted/successful counts and the original failure; operational faults and cancellation propagate. Neither terminal reporting hook runs for those failures.

Identity includes definition/version, credential-free source/revision, and a caller-supplied attempt ID. It is not authenticated identity or an automatic business idempotency key. Contexts are pass-local; validation and execution share immutable identity, not mutable reconciliation state. The CSV engine uses `CoreEx.ExecutionContext.RunScopedAsync` for fresh unit scopes and registered contexts, with explicit trusted configuration before handler resolution and parent restoration on every exit.

Progress counters are `long`, count validation input once (never again on replay), and distinguish physical records, invalid records, validated units, attempted execution, successful execution, and diagnostic severities. Summary construction enforces outcome/mode/error/completeness invariants; whole-file acceptance does not promise atomicity or exactly-once effects.

## Flat CSV

The parser dependency is **CsvHelper 34.0.0**, used only for low-level record parsing. CoreEx owns decoding, mapping, validation, diagnostics, and execution. This is the rewritten version 34 API, not the older ClassMap/converter API. The package is strong-named, offers net8.0/net10.0 assets (net9.0 uses net8.0), has no additional runtime dependencies on those assets, and is licensed `MS-PL OR Apache-2.0`.

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

In this example, `ImportRow` is a caller-owned class with a public parameterless constructor and writable `int? Id`, `string? Name`, and `int? Quantity` properties. `rowValidator` is an optional pure `IValidator<ImportRow>`. `openReadAsync` is the cancellation-aware factory described above. For positional input, supply a zero-based `index` to each `MapProperty`; if any selector is named, the first nonblank/noncomment record is the column-name header. Named headers are case-insensitive; duplicate or missing required names reject the file before data mapping. Extra fields are allowed; an absent mapped field differs from a present blank field.

Converters receive already-unquoted/unescaped text and return `false` for invalid nonblank input. `CsvFieldConverters.Nullable<T>()` uses invariant `ISpanParsable<T>` conversion and preserves zero/false; whitespace-only input becomes null. `String` preserves nonblank whitespace; `DateOnly(format)` performs exact invariant date conversion. Custom `CsvFieldConverter<T>` delegates support other formats/types. Conversion and property-setter programming faults propagate unchanged. CoreEx requiredness remains a validator concern: use `NotNull` rather than `Mandatory` when zero/false is valid.

`CsvFileOptions` defaults to comma, doubled double quotes, no comments, and strict UTF-8. An explicit delimiter, quote, comment character, or encoding can be supplied. A stateful `StreamReader` handles split multibyte sequences and rejects malformed bytes with the default encoding; the native CsvHelper UTF-8 stream path is deliberately not used because it silently replaces invalid bytes. Encoding autodetection is disabled at this stage; do not rely on UTF-16/32 BOM detection. Broader BOM/encoding guarantees and numeric resource bounds remain Step 5 work. This unfinished package is not yet a bounded untrusted-upload boundary.

Blank lines and configured comment records are skipped without incrementing record counts. Multiline records report their physical starting line, including preceding blanks/comments and CR, LF, or CRLF endings. Malformed quoting produces `csv.record.invalid`; those recovered parser records are never mapped, validated, or executed. Collection continues only along the parser's logical boundaries: an unterminated quoted field can consume the rest of the input, so subsequent physical lines are not invented as independent records.

Other stable diagnostic codes are `csv.header.duplicate`, `csv.header.missing`, `csv.field.missing`, `csv.field.invalid`, and `csv.validation`. Invalid headers stop validation as incomplete because data cannot safely be mapped. A missing header on empty input is rejected after reaching EOF. A record with multiple mapping/validation errors increments `InvalidRecords` once; file reconciliation errors do not invent invalid rows. User hooks choose their own stable business-rule/reconciliation codes.

For dependency-backed validation or execution, pass `serviceProvider` and a `handlerFactory` resolving a registered `FileUnitHandler<ImportRow>` from the supplied scoped provider. Register `AddExecutionContext()` (or its custom factory) and the handler/dependencies; execution without a handler factory fails before opening input. Pure validation runs under the file context, while custom validation and execution use distinct async scopes and contexts for each row and pass. Override `OnConfigureUnitContext` to propagate trusted identity explicitly; CSV fields are not authentication. Override `OnCreateExecutionContext` when a derived file context needs fresh replay reconciliation state.

Diagnostic callbacks run under the captured file-level ambient execution context even when emitted by a unit-scoped validator. They are awaited before parsing the next record; execution-context flow must be enabled. Normal EOF invokes `OnValidateFileAsync`, including after collected errors, but fail-fast/invalid-header termination does not. Rejected files never open replay. Valid execution rebuilds fresh rows/context without repeating validators or custom validation; reconstruction/count drift faults explicitly, but equal-count changed values cannot prove or disprove the caller's immutable-source guarantee.

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

Route mapping and optional pure `IValidator<TRecord>` validation run for each recognized physical record in the validation pass. Only an accepted subtree is projected; its optional pure group validator and scoped custom group validation then run once per root. There is no DI scope per descendant. Replay reconstructs groups without repeating record/group validators or custom validation, and each execution gets a fresh scope. Completed groups are discarded; the framework retains only the current subtree and accepted header/trailer, not a file-wide group collection. Numeric bounds on a single group remain Step 5 work.

Pass-local `CsvHierarchyContext.GetRecordCount(key)` includes recognized records whose mapping/validation failed, not unknown or syntactically malformed records. Malformed syntax invalidates the active group without trusting a recovered discriminator. `HeaderAvailable`/`TrailerAvailable` and `GetHeader<T>()`/`GetTrailer<T>()` expose only accepted envelopes; a duplicate never replaces the first accepted value. Derive the context for application-owned typed accessors and reconciliation accumulators, and override `OnCreateExecutionContext` to return a fresh derived instance preserving identity/mode/policy. A hierarchy context cannot be reused for another pass or invocation.

`CsvAggregateNode` exposes typed records, ordered child projections, and explicit physical provenance through `LineNumber`/`GetLineNumber(record)`. Carry needed coordinates into the projected group for custom diagnostics; logical child/property paths do not imply physical lines. Missing-child diagnostics anchor the parent's physical start; missing file-level occurrences anchor the last logical record's physical start, or line 1 for an empty file. Missing requirements and group/file validation errors do not increment `InvalidRecords`; multiple physical-record errors increment it only once.

Route-specific `errorMode` overrides the file default for that route's mapping/field failures and rules it owns. Missing-child rules use the missing child's policy, group validation uses the root policy, and records after the trailer use the trailer policy. Unknown/malformed records use the file policy. Fail-fast stops after the current discovered diagnostics and skips the EOF hook, even when the failing rule is discovered during group closure or EOF checks; `ValidationCompleted` is false. Collected errors still permit reconciliation through normal EOF but always prevent every execution.

Hierarchy-specific stable codes are `csv.discriminator.missing`, `csv.discriminator.unknown`, `csv.structure.required`, `csv.structure.duplicate`, `csv.structure.orphan`, `csv.structure.child-order`, `csv.structure.before-header`, `csv.structure.header-position`, and `csv.structure.after-trailer`. Mapping/parsing/validation codes are shared with flat CSV. Override `OnValidateFileAsync` for trailer counts/totals; use accepted envelope availability and `HasValidationErrors` to avoid treating invalid input as trustworthy reconciliation data.

Multiple definitions may share the same projected group CLR type. Register definition-specific facades or explicit keyed processors/handlers; do not register an undifferentiated generic singleton and rely on registration order. Each invocation needs a distinct context, fresh mapping state, and thread-safe shared dependencies.
