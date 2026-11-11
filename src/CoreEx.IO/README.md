# CoreEx.IO

Host-neutral CSV, JSON document, and nested JSONL validation/execution with shared lifecycle contracts, safe scoped execution, strict encodings, [File CodeGen](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.CodeGen/Files/README.md), and the [generation-first Functions inventory example](https://github.com/Avanade/CoreEx/blob/main/tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md). XML is not implemented.

Targets .NET 8, 9, and 10; this extension does not raise the package framework floor. See the [4.3-to-4.4 migration guide](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.IO/MIGRATION.md) before upgrading existing CSV integrations: processing modes/outcomes, hook signatures, metadata, YAML, and telemetry include breaking changes.

## Namespaces

| Namespace | Guide |
|---|---|
| `CoreEx.IO` | This guide: shared lifecycle, handlers, diagnostics, metadata, input ownership, and telemetry. |
| [`CoreEx.IO.Csv`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.IO/Csv/README.md) | Flat rows, unordered discriminator routes, ordered hierarchies, mapping, and strict encoding. |
| [`CoreEx.IO.Json`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.IO/Json/README.md) | Streaming JSON documents, JSON Pointer selection, shared serializer contracts, and scalar converters. |
| [`CoreEx.IO.JsonLines`](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.IO/JsonLines/README.md) | JSONL framing, nested object units, discriminator routes, and metadata. |

Shared contracts and the lifecycle coordinator (`FileProcessor<TUnit>`) live in `CoreEx.IO`. Every format processor uses this lifecycle while retaining format-specific parsing and mapping. The package does not impose CSV line/header assumptions on its shared contracts and exposes no universal parser/plugin abstraction.

`FileRecordPosition` provides shared `Any`, `First`, and `Last` boundary restrictions for CSV business routes and CSV/JSONL metadata. Each format retains its own participating-record rules: CSV excludes skipped blank/comment records and column-name headers; JSONL includes every non-skipped line, including invalid or unroutable lines; document metadata is selected independently by JSON Pointer.

Public types support consumer configuration, generated mappings, extension hooks, and observable results. Untyped JSON mapping dispatch, readers, and handler invocation methods are internal; the signed unit-test assembly has friend access. Progress, summary, unit provenance, and hook arguments are framework-constructed, while consumers retain public read access. Derive from the concrete format processors and override protected handler hooks, rather than calling framework-only base constructors or bypassing the lifecycle.

## Processing lifecycle

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

The shared `FileProcessor<TUnit>` exposes protected `Services`, the provider supplied at construction, for coordinator options and report adapters. Its lifetime follows the processor's registration; it is not automatically invocation-scoped and is never the fresh unit scope. Resolving unit business dependencies belongs in the handler factory. DI remains optional for pure validation; accessing `Services` without a supplied provider throws `InvalidOperationException`.

Progress counters are `long`, count validation input once (never again on replay), and distinguish source records, invalid records, candidate/invalid units, validated units, attempted execution, successful execution, and diagnostic severities. Reporting remains an application-owned side effect and has no separate progress counter. Outcomes are independent of mode: `Succeeded`, `ContentFailed`, and `ExecutionFailed`. Summary construction enforces the applicable error, completeness, and execution invariants; whole-file acceptance does not promise atomicity or exactly-once effects. `ValidateAndExecute` validates and executes each complete unit in one pass; later content errors retain earlier execution counts and return `ContentFailed`.

## Metadata

Pass-local metadata is available only after encounter. Immutable accepted metadata is absent during validation/report hooks and single-pass execution, available from the first two-pass execution and successful completion, and typed reads return fresh copies. All formats snapshot declared metadata objects using default System.Text.Json serialization, including serializable handwritten properties. Metadata types must round-trip with that behavior; use `[JsonIgnore]` for transient properties, and keep reconciliation state on the context rather than metadata DTOs. `GetAcceptedMetadata<T>(name)` deserializes a fresh copy on every call, so modifying that copy cannot alter the accepted snapshot; reuse a local copy within a handler when needed repeatedly. Generated contexts provide typed accessors. Put trailer-dependent checks in EOF reconciliation; select ValidateThenExecute when handlers need complete metadata.

## Executable integration example

The [generation-first inventory examples](https://github.com/Avanade/CoreEx/blob/main/tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md#example-matrix) include simple and complex native Functions for CSV, JSON, and JSONL. Each format demonstrates independent rows/objects and parent/child units with header/trailer metadata and EOF reconciliation, generated from checked-in definitions in `tests/CoreEx.Azure.Functions.TestApp.CodeGen`. The handlers call the same inventory application operation while keeping `CoreEx.IO` host-neutral. The native Functions adapter uses the Azure Blob SDK and ETag-pinned input reads; report output is a durable Append Blob.

The application uses a supported SQL Server unit of work to commit inventory changes, durable receipts, and an outbox write atomically. Earlier units may commit before a later unit fails, and outbox persistence does not prove broker publication or exactly-once delivery. The Aspire runtime path uploads a real blob to Azurite, then simulates the Event Grid notification through the Functions webhook; it exercises the actual Functions binding, Blob SDK I/O, SQL Server work, and report writes. Azurite does not emit Event Grid events, so the local proof does not establish Azure subscription provisioning, authentication, or delivery.

## Input ownership

The consumer owns source admission, overall input size, concurrency, timeouts, and report-output policy for every format. CoreEx.IO imposes no file/line/token/metadata byte quota, unit/diagnostic count quota, CSV group-record quota, or default collection maximum. JSON retains the built-in parser's configurable MaxDepth (64 by default), business cardinality, and optional explicitly declared positive collection maxItems. A large JSON unit, JSONL line, or CSV hierarchy can consume substantial memory; the framework is an application integration helper, not a bulk-data platform. Format-specific parsing and encoding guarantees are documented in the namespace guides.

The source factory need not support seeking or expose a length. Reading and source disposal remain asynchronous, with cancellation propagated. Source I/O faults propagate unchanged. Replay encoding/shape drift is an operational fault, not a late content rejection after possible earlier writes. Earlier committed units are not rolled back; the immutable-source contract still applies.

## Observability

Every file-processing attempt uses `FileProcessorInvoker` and the standard CoreEx invoker tracing/configuration pipeline (`ActivitySource` name `CoreEx.IO`). Traces identify definition/version and policies, then capture outcome and final or partial record/unit, execution, byte, and error counts through reporting completion, faults, and cancellation. Reporting has no dedicated progress counter. Source identity, source revision, attempt identifiers, and raw field values are not automatically attached. Outcome logs use the optional service provider's logger factory and its level filters; register a custom `FileProcessorInvoker` there to customize the invoker behavior. Existing CoreEx settings control standard invoker tracing/debug logs.

Subscribe to the `FileProcessingTelemetry.MeterName` meter (`CoreEx.IO`) for `file.files.processed`, one increment per attempt, with only the bounded `file.outcome` dimension: `succeeded`, `content-failed`, `execution-failed`, `faulted`, or `cancelled`. Retries count as separate attempts; a reporting failure is faulted rather than successful. Instrumentation stores no diagnostic history. Configure normal host OpenTelemetry subscriptions/exporters; this package does not select an exporter or require one.
