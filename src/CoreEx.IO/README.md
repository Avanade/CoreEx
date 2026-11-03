# CoreEx.IO

**Work in progress; not packable or included in release publishing.** Steps 1 through 3 provide format-neutral contracts, safe nested execution contexts, and a flat CSV validation/execution runtime. Hierarchy assembly, resource limits/full encoding policy, CodeGen, and release integration remain planned. See the [design and implementation plan](../../docs/file-processing-design.md).

Shared types live in `CoreEx.IO`. CSV mappings, converters, options, and `CsvFileProcessor<TRecord>` live in `CoreEx.IO.Csv`; future formats can reuse the root contracts without CSV line/header assumptions. No universal parser/plugin abstraction is being introduced.

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
