# CoreEx.IO 4.3 to 4.4 migration

The multi-format release adds JSON documents and JSONL while moving CSV onto a shared lifecycle. Existing CSV integrations require source and configuration changes; updating NuGet packages alone is not sufficient. XML remains unimplemented.

## Upgrade sequence

Update CoreEx.IO, CoreEx.Validation, and the design-time CoreEx.CodeGen together, and refresh the version-matched CoreEx.Template AI bundle using `/coreex-docs-sync` after the release is available. Preserve handwritten handlers and partials, migrate YAML, validate definitions, regenerate owned output, and then migrate handwritten hook overrides. Do not edit `.g.cs` files. Definition renames do not remove obsolete artifacts automatically; review exact obsolete paths before deleting them, and never delete handwritten handlers as generated cleanup.

Run generation from the CodeGen console directory:

```powershell
dotnet run -- file --validate
dotnet run -- file
dotnet run -- file --expect-no-changes
```

Keep the developer-maintained definition `version` aligned with changes in interpretation, mapping, validation, or structure. It is not the package version, a source revision, or a business idempotency key.

## Modes and returned outcomes

| Previous API | Replacement |
|---|---|
| `ValidateOnly` | Still validates without business execution, but now also invokes an overridden `OnValidatedUnitAsync` hook. |
| `ValidateAndReport` | Use `ValidateOnly` plus `OnValidatedUnitAsync`; reporting is now an optional hook in every mode. |
| `ValidateThenExecute` | Still validates through EOF before replaying immutable input into fresh scoped handlers. |
| No single-pass execution mode | Opt in to `ValidateAndExecute` only when earlier writes are acceptable before a later content failure. |
| `Validated`, `Reported`, or `Executed` | `Succeeded`; inspect the requested mode and progress when the distinction matters. |
| `Rejected` or `ReportedWithErrors` | `ContentFailed`; inspect progress, because reports or single-pass writes may already exist. |
| `ExecutionFailed` | Still carries the application's original failed `Result`; execution stops at that unit. |

Do not carry persisted enum integers across the upgrade: mode/outcome values have changed. Migrate stored settings and dashboards using the intended named behavior. Operational faults and cancellation still propagate instead of returning an outcome. A returned business failure takes precedence over earlier collected content errors and invokes `OnExecutionFailedAsync`.

The validated-unit hook runs before whole-file acceptance, once per valid unit on the validation pass, and is not repeated on replay. Existing overrides that assumed invocation only in `ValidateAndReport` must explicitly guard their side effects if they should not run in other modes. Reporting exceptions propagate; completion is not reported as successful when finalization fails.

## Handwritten hooks and direct runtime use

Replace positional context/unit/report parameters with framework-created argument objects. Cancellation remains a separate final parameter.

| Hook | Current arguments before cancellation |
|---|---|
| Handler `OnValidateAsync` | `FileUnitValidationArgs<TUnit>`: `Unit`, `Info`, `Context`, `ReportDiagnosticAsync`. |
| Handler `OnExecuteAsync` | `FileUnitArgs<TUnit>`: `Unit`, `Info`, `Context`. |
| Processor `OnValidatedUnitAsync` | `FileUnitArgs<TUnit>`. |
| Processor `OnValidateFileAsync` | `FileValidationArgs`: `Context`, `ReportDiagnosticAsync`. |
| Processor `OnDiagnosticAsync` | `FileDiagnosticArgs`: `Context`, `Diagnostic`. |
| Processor `OnRejectedAsync` | Rename to `OnContentFailedAsync(FileSummaryArgs, CancellationToken)`. |
| Processor `OnCompletedAsync` | `FileSummaryArgs`: `Context`, `Summary`. |
| New processor `OnExecutionFailedAsync` | `FileExecutionFailedArgs`: `Context`, `Info`, `Result`. |

For example, migrate a handler execution override from `(TUnit unit, FileProcessingContext context, CancellationToken cancellationToken)` to:

```csharp
protected override Task<Result> OnExecuteAsync(FileUnitArgs<InventoryAdjustment> args, CancellationToken cancellationToken)
    => _service.ApplyAsync(args.Unit, args.Context.Identity, cancellationToken);
```

This is the [inventory handler's application boundary](https://github.com/Avanade/CoreEx/blob/main/tests/CoreEx.Azure.Functions.TestApp/InventoryImports/Files/InventoryHandler.cs); return the application's `Result` unchanged. Use `args.Info.Ordinal` for the one-based candidate ordinal, not an accepted-unit counter or a guessed line number. Generated unordered dispatch now narrows with `args.As<TRecord>()`; migrate handwritten typed validation/execution overrides too. Cast `args.Context` to the generated context when accessing custom accumulators.

`OnConfigureUnitContext` now accepts `FileUnitContextArgs<TUnit>` without a cancellation parameter; trusted configuration belongs on `args.UnitContext` before handler resolution. Processor `Services` is inherited from `FileProcessor<TUnit>` and exposes the construction-time provider, not a fresh unit scope. Remove handwritten members that duplicate the generated/inherited provider and keep unit business services in scoped handlers.

Construct concrete format processors or derive from them. Framework-only base constructors, reader dispatch, handler invocation methods, progress/summary/provenance constructors, and hook-argument constructors are no longer public extension seams. Exercise integrations through `ProcessAsync`, not by fabricating framework bookkeeping objects.

## CSV YAML

The default format remains Csv when omitted, but old flat CSV option names are not aliases. Unknown or inapplicable configuration rejects before batch output is written.

Before:

```yaml
name: Suppliers
version: '1'
runtimeProjectPath: ../Contoso.Integration
delimiter: '|'
encoding: Utf8
mode: ValidateAndReport
records:
- name: Row
  properties:
  - { name: Code, column: supplier_code, required: true }
```

After, preserving the generated `Code` property:

```yaml
name: Suppliers
version: '2'
runtimeProjectPath: ../Contoso.Integration
format: Csv
csv:
  delimiter: '|'
  encoding: Utf8
mode: ValidateOnly
records:
- name: Row
  properties:
  - { name: supplier_code, property: Code, required: true }
```

Move definition-level `delimiter`, `quote`, `comment`, `encoding`, `headerPolicy`, `discriminatorIndex`, and business `structure` under `csv`. Move record `match`, unordered `cardinality`, and executable `position` under record `csv`. Move positional property `index` under property `csv.index`.

Property `name` is now the source column/member name. Replace old named `column` with `name` and use `property` to preserve the previous CLR identifier when it differs from fixed PascalCase derivation. Positional fields keep a source label in `name`; explicit `property` also preserves unusual existing CLR casing. No property/field casing option or `csv.column` alias is provided.

Remove `maxInputBytes`, `maxGroupRecords`, and record-level `errorMode`; only the invocation error policy applies. The consumer must now enforce admission, size, concurrency, timeout, and report-output policies. There is no replacement quota knob or default collection maximum.

For direct runtime composition replace `CsvRecordCardinality` with `FileCardinality`, and `CsvRecordPosition` with `FileRecordPosition`. Remove per-route error-policy arguments and use `FileProcessingContext.ErrorMode`. Flat CSV parsing, named-column matching, routed discriminator matching, and ordered contiguous child semantics remain format-specific.

## Header/trailer metadata

Column-name headers remain parser configuration. Move old `structure.header`/`structure.trailer` business metadata into shared file-level `metadata` declarations, retaining the typed record and its `csv.match`. Use `required: true` for exactly one occurrence and `csv.position: First` or `Last` when needed. `csv.structure` now describes only business roots/children. Unordered First/Last routes remain executable units unless explicitly declared as metadata.

Replace `GetHeader<T>()`/`GetTrailer<T>()` with `GetMetadata<T>(logicalName)` or generated typed accessors. Replace availability checks with null checks and header/trailer line access with `GetMetadataLineNumber(logicalName)`. These are encountered, pass-local validated values. Handlers needing complete metadata, including a late trailer, use `ValidateThenExecute` and `GetAcceptedMetadata<T>(logicalName)` or generated accepted accessors.

Accepted metadata now uses immutable System.Text.Json snapshots and deserializes a fresh typed copy on every read. Metadata must round-trip with default serializer behavior; mark transient handwritten properties `[JsonIgnore]`. Keep reconciliation state on the context. Replay receives a fresh context and the accepted snapshot, not validation accumulators.

## Progress, diagnostics, and telemetry

Progress distinguishes `RecordsRead`/`InvalidRecords` from candidate `UnitsRead`/`InvalidUnits`. Counts are `long`; validation input is counted once, not again during replay. `UnitsReported` is removed; track reporting externally if needed. Metadata counts as source records, not business units.

Subscribe to `CoreEx.IO` instead of `CoreEx.IO.Csv` for activities and metrics. Replace `CsvFileProcessorInvoker` registrations with `FileProcessorInvoker`, and `CsvProcessingTelemetry.MeterName` with `FileProcessingTelemetry.MeterName`. Rename `csv.files.processed` to `file.files.processed`, `csv.outcome` to `file.outcome`, and lifecycle span tags from `csv.*` to `file.*`; there is no dual emission. Metric outcomes are `succeeded`, `content-failed`, `execution-failed`, `faulted`, and `cancelled`.

Shared unit-validator findings now use `file.validation`; CSV route/metadata validators still use `csv.validation`, and CSV parser/mapping/structure codes remain CSV-owned. The custom `csv.encoding.bom` rejection is removed: configured strict decoding remains authoritative, with StreamReader/CsvHelper handling matching preambles and no BOM-driven encoding switching.

## Framework and verification boundaries

CoreEx.IO and CoreEx.CodeGen retain `net8.0;net9.0;net10.0`. CoreEx.IO's .NET 8 package now requires System.Text.Json 9.0.21 or later because version 8 buffers complete ignored containers during asynchronous deserialization; update any consumer pin that forces an older serializer. This does not raise the .NET runtime floor or introduce a custom parser. JSONL duplicate rejection uses the built-in .NET 10 option and a DOM member-name check on .NET 8/9, with the same intended content contract. The native Functions examples target .NET 10; they do not raise the libraries' framework floor.

Compatibility execution belongs to PR CI: the core job builds all supported frameworks and runs the multi-target runtime/CodeGen suites, the Functions runtime job covers native binding/Blob/SQL/report paths, and template validation packs and scaffolds the release-matched AI/assets and generated consumers. Local runtime proofs do not establish Azure Event Grid provisioning, authentication, or deployed delivery. JSON document streaming avoids a whole-file/per-object DOM, not the current typed graph, metadata, duplicate tracking, parser/serializer buffers, or scalar-collection converter buffering.

See the [runtime guide](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.IO/README.md), [File CodeGen](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.CodeGen/Files/README.md), and [six Functions examples](https://github.com/Avanade/CoreEx/blob/main/tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md) for current composition and operating contracts.
