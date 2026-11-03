# CoreEx.IO

**Work in progress; not packable or included in release publishing.** Step 1 provides format-neutral contracts only. It does not yet read CSV, create DI scopes, or guarantee a processing lifecycle. See the [design and implementation plan](../../docs/file-processing-design.md) for the agreed behavior and remaining steps.

Shared types live in `CoreEx.IO`. CSV mapping, routing, hierarchy assembly, and the engine will live in `CoreEx.IO.Csv`; future formats can reuse the root contracts without CSV line/header assumptions. No universal parser/plugin abstraction is being introduced.

`FileProcessor.ProcessAsync` accepts a cancellation-aware stream-opening factory and an invocation-local `FileProcessingContext`, and returns `FileProcessingSummary` directly. The caller guarantees identical immutable bytes on every open; the processor owns/disposes the streams. Validation-only is the default. Validate-then-execute must complete whole-file validation, including EOF reconciliation, before opening a replay stream or performing any execution.

`FileDiagnostic` uses CoreEx `MessageType` and `LText`, a stable code/category, and optional `FileLocation` and logical record type. `FileLocation` separates one-based physical line/character position, zero-based field index/name, and opaque logical property paths. Physical coordinates are not mandatory, and raw input values are not automatically captured. File-wide diagnostic history is not part of the context or summary.

`FileUnitHandler<TUnit>` separates row/root-group lifetimes from the file coordinator. Its custom validation emits diagnostics through an awaited sink; its execution returns the application's original `Result`. Generated validation will use CoreEx validator messages without per-record exceptions. Default execution throws `NotSupportedException` rather than reporting false success.

The file coordinator exposes the three reporting hooks `OnDiagnosticAsync`, `OnRejectedAsync`, and `OnCompletedAsync`, plus `OnValidateFileAsync` for EOF reconciliation. Reports are ordered/awaited; hooks do not choose a built-in report format or destination. Business Result failure returns `ExecutionFailed` with attempted/successful counts and the original failure; operational faults and cancellation propagate. Neither terminal reporting hook runs for those failures.

Identity includes definition/version, credential-free source/revision, and a caller-supplied attempt ID. It is not authenticated identity or an automatic business idempotency key. Contexts are pass-local; validation and execution share immutable identity, not mutable reconciliation state. Safe nested execution contexts and scope orchestration are Step 2 work.

Progress counters are `long`, count validation input once (never again on replay), and distinguish physical records, invalid records, validated units, attempted execution, successful execution, and diagnostic severities. Summary construction enforces outcome/mode/error/completeness invariants; whole-file acceptance does not promise atomicity or exactly-once effects.
