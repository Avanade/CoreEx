# CoreEx file processing: design and implementation plan

## Status and purpose

This document records the design agreed during the CoreEx file-processing review on 2026-11-03. Steps 1 through 6 are implemented on `feature/file-processing`: format-neutral contracts, a non-packable runtime project, contract tests, portable migration fixtures, safe nested DI/execution-context orchestration, flat/hierarchical CSV engines, finite input/group budgets, strict encoding/BOM handling, and File CodeGen. Steps 7 and 8 remain planned. Public names below match the implemented contracts where applicable.

The starting implementation is `C:\Repos\Playground\Sor.Demo`: `src\Sor.Demo.Common` contains the runtime, `src\Sor.Demo.CodeGen` contains generation, and its samples and tests demonstrate flat and hierarchical feeds. Migrate the capability and useful coverage, not the demo's package boundaries and Azure assumptions wholesale. Do not modify the source demo as part of the migration.

## 1. Purpose, scope, and package boundaries

Add a CSV-first, schema-defined file-ingestion capability: typed rows, mappings, CoreEx validation, discriminator routing, ordered hierarchy assembly, file reconciliation, streamed diagnostics, and optional whole-file validation followed by execution.

| Component | Responsibility |
|---|---|
| `CoreEx.IO` | Host-neutral runtime, processing contracts, CSV mapping/routing/assembly, validation and execution orchestration. |
| `CoreEx.CodeGen.Files` | Development-time YAML configuration, validation, generators, and templates within the existing `CoreEx.CodeGen` package. |
| Host adapter | Opens transport-specific streams, establishes trusted identity/revision, invokes the processor, and chooses retry/disposition and report persistence. |
| Application services | Business invariants, concurrency enforcement, persistence, transactions, durable idempotency, and transactional outbox writes. |

Use **CsvHelper 34.0.0**, rather than implementing a CSV parser. This replaces the original TinyCsvParser choice by explicit agreement after Step 3 investigation: TinyCsvParser 3.0.11's async path retained line terminators in final fields, mishandled malformed final records, and swallowed mapping exceptions. The alternative assessment covered CsvHelper, Sylvan.Data.Csv, and Sep; CsvHelper's low-level reader supports non-throwing malformed-record callbacks, invariant TryParse-based field conversion, cancellable asynchronous reads, and physical starting lines. Version 34 is a fresh rewrite, not the older CsvHelper API. Its package is strong-named, licensed MS-PL OR Apache-2.0, and supplies net8.0/net10.0 assets without additional runtime dependencies on those targets; .NET 9 uses the net8.0 asset.

CoreEx owns typed mappings and conversion, rather than adopting the parser's automatic object mapping. Decode through a stateful strict StreamReader, not CsvHelper's native UTF-8 stream path: the assessment demonstrated silent invalid-byte replacement there even with a strict Encoding option. Parser callbacks capture sanitized current-record failures synchronously; the coordinator awaits diagnostic delivery before advancing. Recovered malformed records are never mapped or passed to business validation/execution. No framework parser fork, synchronous I/O fallback, or separate CSV framing implementation is needed.

`CoreEx.IO` must not depend on Azure Blob SDK types, Functions bindings, CodeGen, or a particular storage/report destination. It must be usable without generated code. Do not introduce a general-purpose transport framework, generic filesystem abstraction, or an ETL platform.

Initial non-goals are fixed-width, Excel, XML/JSON formats, exporting, a YAML business-expression language, automatic database imports, built-in parallel execution, automatic retries, whole-file transactions, exactly-once delivery, checkpoint/resume, and broad generated-output cleanup.

## 2. Preferred vocabulary and API roles

| Name | Meaning |
|---|---|
| `CsvFileProcessor<TRecord>` / `CsvHierarchyFileProcessor<TGroup>` | Flat and hierarchical engines sharing lifecycle orchestration through `CsvFileProcessorBase<TUnit>`; generated facades have definition-specific service identities. |
| `CsvHierarchyDefinition<TGroup>` | Exact typed routes, ordered root/children, optional first/last envelopes, and typed accepted-group projection. |
| `CsvHierarchyContext` | Pass-local recognized counts, accepted typed envelope access, physical EOF anchor, and content-error state; derivable for reconciliation accumulators. |
| `FileProcessingContext` | Invocation-local processing identity and reconciliation state; distinct from ambient `CoreEx.ExecutionContext`. |
| `FileProcessingIdentity` | Definition name/version, credential-free source/revision, and explicit attempt identity shared across passes. |
| `FileDiagnostic` | Structured diagnostic with severity, code/category, message, and optional record/property/physical location. |
| `FileLocation` | Optional physical line/character position, field index/name, and opaque logical path; no inferred physical location. |
| `FileProcessingProgress` | Immutable `long` counters without input models or diagnostic history. |
| `FileProcessingSummary` | Final outcome, progress counters, processing mode, validation completeness, and execution failure when applicable; no diagnostic history. |
| `FileProcessingMode` | `ValidateOnly` or `ValidateThenExecute`; default is `ValidateOnly`. |
| `FileErrorMode` | `FailFast` or `CollectErrors`; describes continuation after expected content errors, not operational faults. |
| `FileProcessor` | Abstract format-neutral file coordinator contract, stream-factory entry point, EOF validation, and reporting hooks; no engine implementation in Step 1. |
| `FileUnitHandler<TUnit>` | Scoped custom validation and Result-returning execution for a flat row or assembled root group, separate from the file coordinator. |
| `OnDiagnosticAsync` | Awaited notification for one diagnostic. |
| `OnRejectedAsync` | Terminal hook for content rejection. |
| `OnCompletedAsync` | Terminal hook for successful completion of the requested processing mode. |

Separate the file coordinator/reporting lifetime from the typed row/group handler lifetime. Do not place all behavior into a base class that captures unit-scoped services for an entire file.

Shared contracts live in the root `CoreEx.IO` namespace. CSV mapping, converters, discriminators, hierarchy assembly, and the engine belong in `CoreEx.IO.Csv`. Future JSONL/XML support may reuse the root semantics without requiring CSV rows, headers, or physical line coordinates; do not introduce a speculative universal parser/plugin framework.

Generated wire rows and assembled groups are input representations, not domain aggregates or persistence entities. Do not automatically add identifiers, ETags, changelogs, or DDD base classes. Map explicitly into application requests/domain operations.

## 3. Input and immutable replay

The core input is an asynchronous stream-opening factory, conceptually `Func<CancellationToken, Task<Stream>>`, plus processing context and cancellation token. Each invocation must return a readable stream at the beginning of the same immutable byte sequence. The processor owns and disposes each returned stream. Reading and disposal must be asynchronous where supported.

Validation opens one stream. Successful validation in `ValidateThenExecute` opens another stream for execution. Content rejection does not open an execution stream. Opening a second stream is replay, not an implicit retry.

The caller owns the replay guarantee. Examples are revision-pinned Blob reads, a protected local source with disposable read views, or a snapshot of a non-replayable upload. A seekable stream is not inherently immutable. The core cannot prove that an arbitrary factory honors its contract; document this explicitly.

Do not silently spool every input to disk. A single-stream convenience overload is optional, not required for the initial capability. If introduced, its snapshot behavior, byte limits, temporary storage, cleanup, and stream ownership must be explicit.

No path or `BlobClient` overload is required in the core. Transport adapters may offer convenience APIs without changing the stream-based processing contract.

## 4. Processing lifecycle and guarantees

Both flat and hierarchical files support the same whole-file acceptance barrier.

| Stage | Flat CSV | Hierarchical CSV |
|---|---|---|
| Validation pass | Map and validate rows in order. | Map/validate records, enforce structure, assemble and validate root groups in order. |
| EOF validation | Invoke file reconciliation after normal completion and required-record checks. | Complete the final group, check required records, then invoke file reconciliation. |
| Acceptance decision | Any error rejects the file and prevents every execution call. | Same. |
| Execution pass | Reopen the accepted revision and execute typed rows sequentially. | Reopen the accepted revision and execute typed root groups sequentially. |
| Completion | Finalize the requested processing mode. | Same. |

Execution rebuilds fresh input models and invocation-local reconciliation context. Do not rerun field/custom validation or transfer mutable validation accumulators into execution. Mapping and structural reconstruction still occur; an unexpected reconstruction failure is an operational fault, not a newly rejected file after earlier execution.

File-level validation is available to flat feeds too. It supports counts, totals, and handwritten reconciliation without requiring a synthetic hierarchy. Do not perform business writes in validation hooks.

Retain the demo's header-name and positional selectors, exact discriminator matching, ordered contiguous descendants, cardinality rules, per-record continuation overrides, and physical starting-line tracking. Preserve the distinction between absent fields, blank/null values, conversion failures, and requiredness. Requiredness rules must retain CoreEx semantics, including `Mandatory` versus `NotNull` for zero/false.

Retain typed accepted header/trailer access, explicit availability, recognized-record counts, and group/file reconciliation hooks. Invalid recognized records still contribute to recognized-type counts; unknown records cannot be assigned a typed count. Missing-record diagnostics use a documented EOF/parent anchor, never an invented physical record.

In `CollectErrors`, continue where structurally safe and reject the file if any error occurred. `FailFast` stops at the applicable failing record/rule while preserving its discovered diagnostics. Early termination must be distinguishable from validation through EOF. Preserve documented record-specific policies rather than flattening every failure to the root policy.

Whole-file acceptance is not whole-file atomicity. Execution can fail after earlier units committed, and reporting can fail after execution. External-state validation may become stale; application services must re-enforce write-time invariants, concurrency, and idempotency.

## 5. Diagnostics and reporting hooks

Deliver diagnostics immediately through awaited `OnDiagnosticAsync(context, diagnostic, cancellationToken)` calls. Delivery is ordered and provides backpressure. Do not retain a file-wide error list or use a background queue. Temporary diagnostics for the current validator/record/group are acceptable; file-wide historical retention is not.

Use structured category/code, CoreEx `MessageType` severity, unrendered `LText`, and optional logical record type/location. `FileDiagnosticCategory` distinguishes parsing, mapping, structure, validation, reconciliation, and limits. `FileLocation` has an optional one-based physical starting line and character position, separate zero-based field index and field name, and an opaque logical property/path. Physical coordinates are not mandatory. Do not infer descendant physical lines by parsing logical property paths. Raw input values must not be included by default.

Warnings/information do not reject a file. Error diagnostics do. Retain counts independently of diagnostic storage; counters use `long`. Distinguish invalid physical records, group/file errors, and execution progress so a reconciliation error is not counted as an invented failed row.

`OnRejectedAsync(context, summary, cancellationToken)` finalizes content rejection, including incomplete validation under `FailFast` or limits. `OnCompletedAsync(context, summary, cancellationToken)` runs only after successful validation and, when requested, successful execution. Neither hook represents operational failure or cancellation.

Hook implementations choose whether to log, stream a report, persist a ledger, or publish an outcome. The framework chooses no report format or destination. Hooks run under the file-level context; they must not inherit the last unit's ambient tenant/services. Resolve the coordinator within its file invocation lifetime, not as a singleton holding invocation-specific writers.

Await hook operations and propagate reporting failures as operational failures. Do not invoke a second terminal hook to disguise or compensate for a failed first hook. Partial reports must remain distinguishable from finalized reports; provider-specific opening/finalization/cleanup belongs to the coordinator/adapter. There is no guaranteed terminal report on fault or cancellation.

## 6. Result and exception semantics

Expected malformed content is data, not exception control flow. Conversion uses parser outcomes/`TryParse`; CoreEx validation uses `ValidateAsync` and its messages, not `ValidateAndThrowAsync`. Structural and reconciliation failures produce diagnostics.

Do not construct a CoreEx `Result.ValidationError` per diagnostic: its implementation allocates a `ValidationException` even when it is never thrown. Do not wrap throwing parser paths in `Result.Go` and claim that eliminates exceptions. Characterize the dependency's malformed-input behavior and remove expected-content throw/catch paths where its integration allows it.

Typed execution hooks return `Task<CoreEx.Results.Result>` so application-service failures compose without `ThrowOnError` followed by a catch. An execution failure stops processing immediately, preserves the original error, and produces an explicit execution-failed summary with partial progress. It does not invoke `OnRejectedAsync` or `OnCompletedAsync`.

The primary processor returns `FileProcessingSummary`, not `Result<FileProcessingSummary>`. `FileProcessingOutcome` values are `Validated`, `Executed`, `Rejected`, and `ExecutionFailed`. Preserve the execution Result separately from streamed diagnostics and keep progress accessible on execution failure. `ExecutionResult` carries the original failure only for `ExecutionFailed`, and success otherwise; inspect `Outcome`/`IsSuccess` for the file-level result, not `ExecutionResult` alone.

Step 1 defines immutable identity and progress snapshots. The file context contains identity, requested mode, content-error policy, and the latest progress snapshot; it can be derived for pass-local reconciliation accumulators. The coordinator will update `Progress` before awaited callbacks so callers retaining the context can inspect partial progress after a propagated fault. A returned summary copies identity/mode and retains an immutable progress snapshot, not mutable context state.

`FileProcessingProgress` counts `BytesRead` and `RecordsRead` during validation only; replay never increments them. Records include physical data/envelope records and malformed records returned by the parser, not column-name headers or skipped blank/comment lines. `InvalidRecords` counts distinct physical mapping/field/structural failures, not diagnostics or file/group reconciliation failures. `UnitsValidated` counts flat rows or root groups that passed unit validation even if a later file-level error prevents execution. `UnitsAttempted` counts execution hooks entered; `UnitsExecuted` counts hooks returning success, not independently verified commits. `ErrorCount`, `WarningCount`, and `InformationCount` count emitted validation diagnostics. All counters are nonnegative `long` values; validated units need distinct valid input records, and successful execution cannot exceed attempted or validated units.

Summary construction rejects contradictory states. Acceptance requires completed EOF validation and no content errors. Rejection requires a content error and zero execution attempts. `Validated` is validation-only; `Executed` requires validate-then-execute with all accepted units successful. `ExecutionFailed` requires validate-then-execute, a failed original Result, and exactly one unsuccessful attempt because processing stops on that failure. Empty-input acceptance remains a format/definition policy, not an unconditional contract rejection. `ValidationCompleted` distinguishes early stopping from completed EOF checks on rejected content.

Custom unit/file validation emits individual diagnostics through an awaited sink rather than returning a file-wide collection. `FileUnitHandler<TUnit>.ValidateAsync`/`ExecuteAsync` validate entry arguments and cancellation before invoking protected overrides; default validation adds no rules, while default execution throws `NotSupportedException` rather than claiming success. These extension-point contracts do not themselves implement the whole-file barrier or DI scoping; the later engine must do so.

Unexpected I/O failures, source-revision unavailability, programming faults, malformed callback diagnostics, and reporting faults propagate. Cancellation propagates and is not translated into rejection. Do not broadly catch errors into success-shaped summaries. Hosts decide retry/disposition explicitly.

## 7. DI scope and CoreEx execution context

Use one file invocation lifetime for processing metadata, counters, reconciliation state, reporting resources, and file-level tracing. Fresh async DI scopes and `CoreEx.ExecutionContext` instances belong to business units: one flat row or one assembled root group, not each physical descendant.

Pure generated field rules need no additional scope per field/physical record. Dependency-backed row/group validation runs in a short-lived unit scope. Execution always resolves the typed handler and its dependencies inside a fresh unit scope. Define validation orchestration so it does not create redundant nested scopes for the same business unit.

File reconciliation and terminal hooks use the file-level context, never the last unit scope. Scoped repositories, contexts, handlers, and tracked entities must not escape into file-wide state. A DI scope is not a transaction; application services own transactions and outbox writes.

Follow `TimerHostedServiceBase.ScopedExecuteAsync` for scope ownership, but add safe ambient nesting before reusing the pattern inside API/Functions invocations. Current `AddExecutionContext` resolution calls `ExecutionContext.Reset()`, which disposes the existing ambient context and clears its service provider. Saving/restoring that disposed reference is not sufficient.

Implement a narrow CoreEx infrastructure mechanism that non-destructively preserves the parent ambient context, creates/resolves a unit context through registered DI factories, configures it before business-service resolution, executes the awaited callback, disposes owned resources, and restores the parent on every exit path. Account explicitly for the existing DI registration's ambient side effects. Do not bypass custom context factories with manual construction or use `Task.Run` as an isolation workaround.

Step 2 implements `ExecutionContext.RunScopedAsync<T>` and its non-value-returning overload in the foundational CoreEx package. The helper detaches the ambient parent without disposal, creates an async scope, resolves the registered context, establishes it as current, invokes `configure(parent, context)`, then awaits the operation and scope disposal. A `Result` is returned unchanged. Cancellation is checked before scope creation and after configuration, then forwarded into the operation. Parent restoration is guaranteed even when scope creation, registration, configuration, execution, or disposal fails; absent parents remain absent. The context is explicitly disposed if another service's failing disposal prevented DI from reaching it; other skipped service disposals remain subject to the DI container's own behavior.

No identity or mutable state is copied implicitly. Callers explicitly propagate trusted user/tenant/culture and approved metadata in the configuration callback, including derived context properties, before resolving unit handlers. Registration factories/configuration still run during DI resolution and must not prematurely resolve tenant-sensitive business services. Factories must return fresh scope-owned contexts, never parent/singleton instances. Existing direct `AddExecutionContext` resolution, middleware, and timer behavior remain unchanged; only callers opting into the helper receive nested ownership. The Step 3 flat CSV engine uses this helper for custom row validation and execution, with `OnConfigureUnitContext` for trusted configuration before handler resolution.

Do not use `CreateCopy` as the nesting solution: it carries the original service provider and shares mutable messages. Explicitly propagate trusted user/tenant/culture and approved metadata, while using fresh messages, operation state, service provider, and per-unit timestamps. File start time remains separate. Custom context types require an explicit configuration extension point.

Do not infer authenticated identity from file fields. A trusted per-unit tenant resolver, if needed, runs before tenant-sensitive services are resolved. Preserve file-to-unit trace parentage without requiring per-row activities to be exported by default.

## 8. Bounds, text handling, and concurrency

Process each file sequentially. Independent files can run concurrently; mappings, hierarchy sessions, contexts, handlers, and report resources must not share mutable invocation state. Do not retain a single DbContext/unit of work across all units.

Use asynchronous parser enumeration with cancellation propagated into reading and all callbacks. Preserve narrow parser/header failure classification. If a borrowed stream view is used, it must forward asynchronous reads and cancellation correctly.

`CsvFileOptions.MaxInputBytes` defaults to 64 MiB per pass, including the BOM; `MaxGroupRecords` defaults to 10,000 parser-returned records per root, including the root and invalid/unknown descendants. Both require positive values. Blank/comments consume the byte budget; multiline records count once for the group budget. Bounds apply to validation and replay. Limit exhaustion is terminal incomplete rejection regardless of continuation overrides, not successful truncation: skip final-group closure and EOF reconciliation, emit `csv.limit.input-bytes` or `csv.limit.group-records`, and prevent replay. Replay violations are operational `InvalidDataException` faults. The byte wrapper reads at most the configured budget plus one lookahead byte to prove overflow; that actual probe counts in `BytesRead` but is not decoded. No file-wide diagnostic retention means no error-history memory cap is needed; emitted diagnostics still obey backpressure and input/work bounds. If snapshot helpers are added, they require independent byte/storage bounds.

UTF-8 remains the default. One optional matching leading BOM is consumed; explicit UTF-16 LE/BE and UTF-32 LE/BE support matching BOM or BOM-less input. Signatures are recognized longest-first; conflicting BOMs reject as `csv.encoding.bom` rather than overriding configured encoding. A later U+FEFF is preserved. Encodings are cloned with strict decoding, never silently replaced or mutated in caller-owned state. Invalid bytes/truncated sequences produce terminal `csv.encoding.invalid`, incomplete validation, no guessed physical location, and no EOF hook/replay. Strict .NET decoding throws internally; a narrow `DecoderFallbackException` catch emits one terminal diagnostic, not per-record validation exceptions. Other I/O/application faults still propagate. Retain explicit CSV syntax constraints and invariant typed conversions; do not broaden parser behavior accidentally.

Default budgets reflect representative local net10 Debug characterization: 100,000 64-character flat rows used 6.5 MB source and approximately 222 MB cumulative allocation with scoped validation; a single 10,000-record root with 1,024-character children used 10.3 MB source and approximately 57 MB cumulative allocation. These are not peak/live-memory measurements or throughput guarantees. The group default accommodates that roughly-10-MB root while capping retained record count, and the independent byte budget bounds large fields/comments/malformed input. Parser and model overhead can substantially exceed source size; one record can approach the full byte budget. Hosts must choose lower limits/admission/concurrency/time budgets for their workloads. Caller hooks and dependencies are outside the framework's retention guarantees.

## 9. Code generation and discovery

Add `CommandType.File` while preserving no-argument RefData behavior and the existing RefData/Count commands. Replace the current RefData-versus-otherwise-Count branch with explicit command dispatch. Select the script/configuration per command rather than initializing RefData inputs unconditionally.

Place file configuration/generators/templates under `src\CoreEx.CodeGen\Files`, with an embedded `Scripts\file-script.yaml`. Consumer definitions remain at the top level of the CodeGen project, beside `ref-data.yaml`, using the required `*-file.yaml` suffix.

Proposed command surface:

```powershell
dotnet run
dotnet run -- refdata
dotnet run -- count
dotnet run -- file
dotnet run -- file customer-file.yaml order-file.yaml
dotnet run -- file --validate
dotnet run -- file --simulation
dotnet run -- file --expect-no-changes
```

Default File discovery selects only top-level `*-file.yaml`, sorted deterministically. Explicit selections must use the same suffix; do not scan build outputs or recursively discover unrelated YAML. Resolve selected relative configuration paths from the CodeGen project, with explicit documented override behavior.

Load, prepare, and generation-validate the entire batch before writing. Reject missing/empty selections, duplicate definition identities, naming conflicts, and overlapping output paths. Preserve simulation and unchanged-output checks. Validation-before-write is not a promise of transactional filesystem generation; surface partial-output I/O failures explicitly.

Generate runtime processing artifacts by default. Host adapter generation is optional and must not require a paired `.Functions` project for runtime-only generation. Explicit paths/namespaces must remain possible; do not blindly copy literal `.csproj` XML namespace discovery as if it evaluates MSBuild imports/conditions.

### Step 6 decisions

Each file definition has one required `name`, also serving as its integration identity. It drives definition-prefixed generated type names, the default `Files\<Name>` folder, the namespace suffix, and `FileProcessingIdentity.DefinitionName`. The YAML filename is for discovery, not a second identity. Do not introduce separate integration/processor names without a demonstrated requirement.

Generate name/version constants and use them when constructing processing identity. The generated processor rejects mismatched definition name/version before opening input. Version is an opaque developer-maintained definition revision, not the source revision, package version, layout negotiation mechanism, or proof of the deployed code. Bump it when mapping, validation, structure, or interpretation changes affect outcomes. Do not automatically version output folders/namespaces or include it in business deduplication keys.

Keep runtime artifacts together in one configurable destination project, defaulting to `Files\<Name>` within that project. Follow RefData's distinction between configurable project/subfolder paths and resolved directory properties, with explicit namespace overrides. Resolve relative project paths against the CodeGen project directory; do not require relative paths to start with a dot. Optional host output has separate locations and does not force runtime artifacts into Contracts/Application/Infrastructure layers. Proposed configuration names are `runtimeProjectPath`, `runtimePath`, `runtimeNamespace`, `hostProjectPath`, and `hostPath`; finalize exact keys during implementation. Batch collision checks include resolved output paths and fully qualified generated type names, not only definition names.

Generated wire records and assembled representations use composition and separate top-level types, not child classes nested inside a root/group or inheritance to express ownership. Emit one top-level type per file, using definition-prefixed names such as `SupplierOrdersGroup.g.cs`, `SupplierOrdersOrder.g.cs`, and `SupplierOrdersLine.g.cs`; folder and namespace provide integration grouping. Private implementation helpers may remain nested. Flat feeds use their row directly. Hierarchical feeds get assembled group representations where children require assembly; do not manufacture wrappers for leaves. Preserve handwritten partial extension points.

Regenerate generator-owned `.g.cs` artifacts through CodeGen; create handwritten extension-point files only when absent and never overwrite business logic. Generated usage defaults to validation-only; an unimplemented execution handler fails explicitly rather than returning success.

Generate configuration reference documentation under `src\CoreEx.CodeGen\RefData\docs` and `src\CoreEx.CodeGen\Files\docs`, avoiding collisions between identically named configuration types. Update tooling destinations and incoming links, regenerating rather than hand-editing generated docs. Keep distinctly named schemas under the existing root schema directory (`coreex-refdata.json` and proposed `coreex-files.json`). Preserve RefData's generated runtime-code locations and behavior.

Use meaningful `CodeGenCategory` groupings rather than placing all File settings in Primary. Candidate categories are Primary (identity), Format (dialect/encoding), Processing (mode/policies/limits), Mapping, Validation, Structure (routing/hierarchy/cardinality), Paths, Namespaces, Host, and Collections. Each configuration type declares only categories that meaningfully group its own properties; do not add empty or artificial categories.

String wire properties may declare `referenceData: { type: Fully.Qualified.ReferenceDataType, allowInactive: false }`. Preserve the input code as `string?`; emit CoreEx's existing typed string-code validation rule, not reference-object conversion in the CSV mapper. Unknown/inactive codes yield ordinary validation messages, optional blanks remain separate from requiredness, and missing orchestrator/collection setup is an operational fault. The caller must initialize the ambient reference-data lookup context before validation; replay performs no reference-data lookup or validator rerun. Do not silently normalize codes through validation-only `Override()` because replay must reconstruct the same wire values.

Every definition has a distinct generated processor service identity, including multi-record definitions. The demo registers all multi-record processors as `CsvFileProcessor<CsvRecordEnvelope>` with `TryAddSingleton`, causing the first definition to win. Eliminate that collision through a type-safe generated facade or equivalent identity.

Remove `source.type: AzureServiceBus`; it does not describe the demo's generated Blob trigger. Keep format configuration separate from optional host configuration. Retain handwritten partial extension points and never modify generated output directly. Renaming/deleting a definition does not authorize broad deletion of generated files.

OnRamp is upgraded from 2.3.1 to 2.4.0, with focused regression coverage preserving existing reference-data output behavior.

The locally rebuilt OnRamp 2.4.0 package supplies `ConfigBase.RequireText`, explicitly invoked `RejectUnknownProperties`, `DefaultWhereNull(long?, ...)`, inferred integer schemas for long budgets, and `OnRamp.Utility.CSharpSource.StringLiteral`. File configuration uses those directly, without local guard/literal copies or explicit numeric-schema metadata. Existing `Options` validation remains OnRamp-owned. Identifier/namespace APIs were deferred upstream: retain the local ASCII-only identifier restriction as consumer policy, not the full C# identifier grammar. File routing, hierarchy/cardinality, reference-data policy, and output ownership remain CoreEx concerns. Since these APIs were added through a same-version local package rebuild, another environment must restore that updated package; the version number alone cannot distinguish the older 2.4.0 artifact.

## 10. Host adapter and sample behavior

Start with thin examples for local replay and optional native Blob Functions ingestion. Adapters configure input routing, connection names, trusted identity, revision pinning, report persistence, and retry policy. Do not hard-code shared `imports/{name}` and `import-results` assumptions into the library.

Stable processing identity includes definition name/version, credential-free source identity, source revision where available, and a separate attempt/correlation identity. Durable business deduplication additionally identifies the execution unit. Source filename alone and report naming are not sufficient idempotency keys.

Blob adapters must document whether they process the current revision at invocation time or the historical event revision. Conditional reads preserve a selected revision across passes; fetching current properties does not prove that revision caused an earlier notification. Reports must be isolated by definition and full source/revision identity.

Promote reusable Azure-specific helpers to a separate package only if they earn it. Do not require Blob dependencies in the core or a large mandatory Blob-processing base in `CoreEx.Azure.Functions`.

## 11. Logical implementation steps

Implement in the current repository only when explicitly requested. Each step should leave a coherent buildable state; no commits, branches, pushes, or pull requests are implied by this plan.

| Step | Work and deliverables | Prerequisites | Exit criteria |
|---|---|---|---|
| 1. Contracts and baseline | Finalize public names, summary/counter definitions, hook contracts, ownership, replay and failure semantics. Characterize relevant Sor.Demo behavior and dependency APIs. Establish runtime/test projects without publishing an unfinished package. | None. | API shape is unambiguous; compatibility/dependency requirements and baseline fixtures are recorded in tests/docs. |
| 2. Safe execution-context nesting | Add the minimal non-destructive ambient/DI scope mechanism in CoreEx; preserve custom context factories and host ownership. | Step 1 contracts. | Parent context survives nested success, Result failure, exception, cancellation, and disposal; isolated concurrent flows remain correct. |
| 3. Host-neutral runtime and diagnostics | Port parser integration, mappings/converters, structured diagnostics, async reads, summaries, hooks, and factory-owned streams into CoreEx.IO. Implement flat validation, EOF validation, and flat validate-then-execute with scoped unit handlers. | Steps 1 and 2. | Flat replay barrier works; expected content errors do not use framework exception control flow; no Azure dependency or retained error history. |
| 4. Multi-record and hierarchy | Port routing, cardinality/boundaries, ordered assembly, accepted envelopes, counts, per-record policies, group validation/execution, and reconciliation. | Step 3. | Existing useful behavior is preserved, flat/group execution share lifecycle semantics, and concurrent invocations isolate all mutable state. |
| 5. Bounds and compatibility | Implement input/group limits and encoding policy, choose measured defaults, assess allocations/cancellation, and validate supported frameworks/signing/package dependencies. | Steps 3 and 4. | Large/invalid inputs remain bounded; net8/net9/net10 compatibility is proven; no silent invalid-byte replacement. |
| 6. File CodeGen | Upgrade OnRamp with regression coverage; add Files config/templates/script, explicit command dispatch, suffix discovery, full-batch prevalidation, schema/docs tooling, and distinct generated processor identities. | Stable runtime contracts from Steps 3 and 4. | Generated flat/hierarchical projects compile; two multi-record definitions coexist; RefData/Count remain unchanged. |
| 7. Executable integration examples | Add local factory usage and optional thin Blob Functions output/example with explicit routing, identity, report isolation, scopes, and application-service Result integration. Include a transactional durable-idempotency example. | Steps 5 and 6. | Real execution, partial-failure/retry implications, scoped dependencies, and adapter boundaries are demonstrated rather than merely logging execution. |
| 8. Distribution and guidance | Complete package README/AGENTS, capability docs, schema associations, relevant skills/instructions, explicit AI-template asset lists, solution/release wiring, and generation/template validation. | Steps 5 through 7. | Packaged runtime/generator/assets work in a clean consumer scenario; guidance describes shipped behavior and supported boundaries. |

Step 5 and Step 6 may overlap once runtime contracts stabilize; Step 6 does not depend on numeric limit tuning. Any template-content changes require the repository's actual template-pack/scaffold validation, not just compilation of CoreEx.Template.

Step 1 deliverables are `src\CoreEx.IO` and `tests\CoreEx.IO.Test.Unit`, both registered in `CoreEx.slnx` and the core build filter; the tests also join the parallel CI test filter with its existing coverage collector. `CoreEx.IO` explicitly sets `IsPackable=false` and is not added to release publishing or the consumer AI asset bundle. Its only direct project dependency is `CoreEx`; Step 3 adds the CsvHelper parser package, but no transport SDK or generator dependency. The test README records source characterization, dependency/API migration requirements, and the interpretation of five portable flat/hierarchical fixtures. Fixture integrity and contract tests remain distinct from executable processor evidence.

Step 2 deliverables are `src\CoreEx\ExecutionContext.Scoped.cs` and `tests\CoreEx.Test.Unit\ExecutionContextScopedTests.cs`, with usage and ownership guidance in the CoreEx README. Coverage includes registered derived factories and registration configuration, fresh scoped dependencies, explicit trusted configuration before service resolution, distinct mutable state, Result identity, early/in-flight cancellation, nested/concurrent flows, missing registration, factory/configuration/operation/scope-creation/context-disposal/dependency-disposal failures, and unchanged direct-registration semantics. No changes were made to the existing ambient reset/dispose APIs or host composition.

Step 3 deliverables live under `src\CoreEx.IO\Csv` with executable coverage in `CsvFileProcessorTests`. `CsvFileMapping<TRecord>` declares named/positional property selectors with explicit `CsvFieldConverter<T>` delegates; `CsvFieldConverters` supplies invariant nullable/string/exact-date conversion; `CsvFileOptions` controls syntax and explicit decoding. `CsvFileProcessor<TRecord>` owns the validation/EOF/acceptance/replay lifecycle, ordered awaited diagnostics, counters, input disposal, fresh contexts/models/scopes, and original Result failures. Stable framework codes are `csv.record.invalid`, `csv.header.duplicate`, `csv.header.missing`, `csv.field.missing`, `csv.field.invalid`, and `csv.validation`. Headers are matched case-insensitively; duplicate/missing required names stop before data validation. Bad header termination is incomplete; an empty input's missing header is rejected after EOF. Requiredness uses CoreEx validator messages, not exception-backed validation Results.

The flat runtime captures file-level ambient execution context for diagnostic delivery from inside scoped validation; execution-context flow must be enabled. Replay maps fresh models and does not repeat validators/custom validation. Detectable reconstruction/count drift propagates as an operational fault, not rejection after partial execution; equal-count changed values remain the caller's immutable-source responsibility. Both conversion-error and malformed-quote 10K cases have non-throwing coverage. Multiline/escaped fields, physical locations, nonseekable/single-byte asynchronous input, reporting backpressure, faults, cancellation, ownership, and scope isolation are covered. An unterminated quote can consume the remaining input as one malformed logical record; collection does not invent boundaries. No numeric bounds or broader BOM guarantees are claimed until Step 5.

Step 4 adds `CsvHierarchyFileProcessor<TGroup>`, `CsvHierarchyDefinition<TGroup>`, typed `CsvRecordRoute<TRecord>`, immutable `CsvHierarchyNode`/`CsvRecordCardinality`, accepted `CsvAggregateNode` projections, and pass-local `CsvHierarchyContext`. `CsvFileProcessorBase<TUnit>` now owns the common parser, validation/EOF/acceptance/replay, scoped-unit execution, reporting, and counter lifecycle; flat behavior is preserved. Routes require positional mappings and exact discriminator matches. Logical route keys are unique ignoring case and used exactly once in the declared structure. Wire records are mapped/field-validated, then accepted contiguous subtrees project into a business unit for pure/scoped group validation and optional replay execution.

`Many` means zero or more; `One`, `Optional`, and `OneOrMore` express exact-one, zero-or-one, and at-least-one. Cardinality is per parent for children and per file for the root; envelopes permit `One`/`Optional` without children. Recognized-invalid mapping/field records contribute typed counts without becoming accepted envelopes/groups; syntax failures invalidate the current group without trusting recovered discriminator fields. Completed groups are discarded. Only the current subtree and accepted header/trailer remain in framework state; Step 5 adds finite byte/record budgets. Physical provenance is explicit on aggregate nodes, never inferred from property paths.

Missing-child diagnostics use the parent start; missing file-level requirements use the last logical record start, or line 1 when empty. Group/file errors and missing occurrences do not invent invalid rows. Route-specific continuation applies to the failing rule's owner, including missing children and root-group validation. Fail-fast closure/EOF-rule failures stop reconciliation and return incomplete validation; collected failures still reach the EOF hook. Accepted envelope access, recognized counts, and content-error state are available for handwritten reconciliation. Contexts cannot be reused across invocations/passes; derived contexts require fresh replay creation.

Step 4 evidence in `CsvHierarchyFileProcessorTests` covers all three hierarchy fixtures, late-trailer zero-write rejection, arbitrary depth/provenance, sibling ordering, every cardinality, envelope availability/duplicates, exact/missing/unknown/malformed routing, recognized-invalid counts, distinct physical/group/file accounting, per-route/root policies, fresh replay contexts/models/scopes, non-repeated validators, original Result failure, reconstruction/count drift, callback faults/backpressure/cancellation, context reuse/configuration faults, concurrent distinct definitions alongside flat processing, discarded completed groups, and non-throwing 10K bad-child conversion (with an explicit 10,001-record budget for root plus children). Stable additional codes are `csv.discriminator.missing`, `csv.discriminator.unknown`, `csv.structure.required`, `csv.structure.duplicate`, `csv.structure.orphan`, `csv.structure.child-order`, `csv.structure.before-header`, `csv.structure.header-position`, and `csv.structure.after-trailer`.

Step 5 adds bounded asynchronous source/prefix reading in `CsvReadStream`, immutable configured budgets in `CsvFileOptions`, terminal reader outcomes shared by both processors, strict decoding/matching-BOM policy, and group-bound checks before mapping another descendant. `CsvBoundsAndEncodingTests` covers exact and one-over thresholds, tiny prefix budgets, comment/unclosed-quote exhaustion, default oversized roots, no-write/no-EOF rejection, replay drift, cancellation of EOF lookahead, matching/conflicting/partial BOMs, strict Unicode/ASCII failures, named-header/discriminator BOM handling, cumulative-allocation observations, and runtime/parser strong-name tokens. The full IO suite is exercised on net8.0, net9.0, and net10.0; CsvHelper 34.0.0 and its package/license boundaries remain unchanged. No parser record-buffer size heuristic is promoted to an exact limit: CsvHelper's `MaxRecordSize` is growth-based, can accept already-buffered over-limit records, and throws. The CoreEx byte bound instead limits all source data passed to that buffer without per-row limit exceptions.

Step 6 adds `Files\Config`, generators/templates, the embedded File script, explicit command dispatch, suffix discovery, full-batch preparation/collision checks, configurable runtime project/subfolder/namespace, typed wire/group/context/processor/handler/registration output, and opaque version enforcement. Generated handlers are create-only, flat and hierarchical defaults are validation-only, and unimplemented execution fails explicitly. String columns can use typed reference-data code validation without lookup/conversion during replay. OnRamp is upgraded to 2.4.0; its existing Options/defaulting facilities are reused. The tooling regenerates distinct schemas and separate RefData/Files configuration references. `CoreEx.CodeGen.Test.Unit` covers configuration/CLI/discovery/batch/mode/ownership regressions, RefData/Count behavior, and actual compilation/execution of a generated flat processor plus two nested hierarchical definitions on net8.0/net9.0/net10.0. The compiled consumer checks identity-before-open, optional/required/active/inactive reference codes, unchanged code casing, 10K individually streamed reference-code failures without first-chance exceptions, and explicit unimplemented execution. Runtime examples and release/AI-asset distribution remain separate Steps 7 and 8.

Step 6 follow-up restores the demo's declarative string length/regex/email, typed inclusive bounds/options, decimal precision/scale, and explicit mandatory/default-value validation. `required: true` remains NotNull; `mandatory: true` opts into Mandatory. Each property generates one fluent rule chain, with cached one-second regexes. Processor and handler `[ScopedService]` attributes replace the redundant generated services extension; one dynamic assembly scan registers all definitions. Consumer global imports supply ordinary type names.

## 12. Acceptance matrix

| Area | Required evidence |
|---|---|
| Flat whole-file barrier | A final invalid row or EOF reconciliation error causes zero execution calls; valid input executes rows in order. |
| Hierarchical barrier | Late trailer/group/file errors cause zero execution; accepted groups rebuild as fresh models and execute in order. |
| Replay and ownership | Exactly one validation open and one execution open when needed; no execution open on rejection; all returned streams disposed on every exit path. |
| Factory contract failures | Failed opens/read/replay, changed/unavailable revisions, and invalid stream capabilities surface explicitly without false success or hidden retry. |
| Expected invalid content | A 10K-record malformed-content fixture emits ordered diagnostics without framework throw/catch control flow or file-wide error storage. Characterize parser-internal exceptions separately rather than hiding them. |
| Reporting | Ordered awaited diagnostics and slow-sink backpressure; correct terminal hook; no completion/rejection hook for execution failure, cancellation, or operational fault; report-write failure propagates. |
| Outcome accounting | `long` counters, EOF completeness, warnings versus errors, group/file versus row counts, and partial execution failures are represented accurately without retaining diagnostics. |
| Scope/context isolation | Fresh scoped dependencies per unit, parent restoration, custom context factories, trusted identity propagation, file-level hook context, async disposal, and no tracked-state growth across units. |
| Multi-definition DI | At least two different multi-record definitions plus a flat definition in one host resolve and process using their own mappings, policies, and handlers, independent of registration order. |
| Concurrent files | No cross-file tenant/context, reconciliation, mapping, handler, or reporting-state leakage. |
| Parser/text behavior | Named/reordered/case-insensitive headers, duplicate/missing headers, positional selectors, multiline quotes, malformed quoting, invariant numeric/date conversions, null/default requiredness, BOMs, invalid bytes, and exact physical anchors. |
| Resource limits | Oversized input and a single oversized root reject explicitly; memory is bounded by the current unit/parser state rather than diagnostic count; cancellation interrupts asynchronous I/O. |
| Execution semantics | Returned application Result failure stops the next unit and preserves progress/error; thrown faults/cancellation propagate; no implicit retry/rollback; durable deduplication example handles replay. |
| Discovery/generation | Required suffix, top-level-only discovery, deterministic order, explicit selection, batch-before-write rejection, collision checks, validate/simulation/unchanged modes, and preserved handwritten files. |
| Existing consumers | RefData and Count behavior, schema/docs generation, custom resources/options, supported target frameworks, and package signing remain valid. |
| Distribution | Generated consumers compile using shipped assets; any optional Functions binding is exercised separately from unit-level invocation; AI-template bundle includes the new guidance. |

## 13. Implementation details to settle without reopening the architecture

The format-neutral contract names, counter definitions, and CoreEx `MessageType`/`LText` representation are now defined in Step 1. The runtime steps must define stable parser/validation diagnostic codes and precise malformed-input recovery. Decide how callers configure custom execution-context propagation and dependency-backed validation without duplicate scopes.

Step 5 defines measured byte/group operating defaults and explicit BOM/strict-decoding behavior; the runtime README records their caveats. A single-stream snapshot convenience helper remains optional; the factory contract is sufficient without it. Keep optional host-output switches and project/path conventions minimal and explicit.

These are bounded implementation choices, not reasons to add speculative configuration, transports, formats, or workflow machinery.
