# CoreEx.IO — AI Usage Guide

Host-neutral, CSV-first file acceptance and scoped business execution. Reference `CoreEx.IO` at runtime and `CoreEx.CodeGen` only in the design-time console. See [runtime guide](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.IO/README.md) and [File generation](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.CodeGen/Files/README.md).

## Entry point and ownership

`FileProcessor.ProcessAsync` accepts `Func<CancellationToken, Task<Stream>>`, a new invocation-local `FileProcessingContext`, and cancellation. The processor opens/disposes each stream; the caller must guarantee identical immutable bytes on both opens. Validation-only is the default. Validate-then-execute validates through EOF before opening replay or executing a business unit. It is not a whole-file transaction or exactly-once guarantee.

Use `CsvFileProcessor<TRecord>` for flat rows, `CsvHierarchyFileProcessor<TGroup>` for contiguous ordered hierarchies, and `CsvRoutedFileProcessor<TUnit>` for unordered discriminator records. Mappings are fresh per pass; named selectors imply a column-name header, routed fields are positional. Routing matches exact case-sensitive discriminator values. Hierarchical cardinality applies per parent; unordered cardinality applies per file.

## Generation and business logic

Start from a checked-in top-level `*-file.yaml` in an existing CodeGen console, then run `dotnet run -- file --validate` and `dotnet run -- file` with that console as the current working directory. `dotnet run --project <path>` does not change the process directory used for top-level definition discovery. Required fields: `name`, opaque `version`, `runtimeProjectPath`, and `records`. Generate wire models, mappings, validators, contexts, processors, and handler starters instead of hand-authoring them. Generated partial types share the configured runtime project and namespace. Do not edit `.g.cs`; modify YAML/templates and regenerate. Handler starters are create-once: put application behavior in handwritten handler/processor extensions, keep `[ScopedService]`, and register the assembly using `AddDynamicServicesUsing<TProcessor>()`. Supply the documented `GlobalUsing.cs` imports.

The generation-first Functions example at `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml` emits `InventoryAdjustment`, `InventoryAdjustmentMapping`, `InventoryAdjustmentValidator`, `InventoryContext`, `InventoryProcessor`, and `InventoryHandler` under the configured `InventoryImports/Files` path and namespace. Generated processors expose protected `Services` so handwritten partial hooks can resolve invocation-scoped options/report adapters without a constructor conflict; unit business dependencies still resolve through the generated handler's fresh child scope. Keep the Azure Blob trigger and `AppendBlobClient` report adapter outside CoreEx.IO; use the supported SQL Server unit of work for the business update, durable receipt, and outbox write in one transaction.

Generated properties are nullable wire values. `required: true` means `NotNull()` and permits zero/false; `mandatory: true` rejects defaults. String length/regex/email, typed bounds/options, decimal precision/scale, and reference-data rules use CoreEx validators and one fluent chain per property. Reference-data properties stay string codes: initialize the ambient orchestrator/collections before validation; replay neither reruns validators nor canonicalizes codes.

`FileUnitHandler<TUnit>` owns scoped custom validation/execution. Validation sends individual `FileDiagnostic` values to an awaited sink; execution returns the application's original `Result`. Register normal CoreEx execution-context services. Fresh unit scopes use `ExecutionContext.RunScopedAsync`; override trusted context configuration before resolving tenant-sensitive services, never infer authenticated identity from file fields. Override execution or inherit an explicit NotSupported failure, not a success-shaped placeholder.

## Reporting, limits, and faults

Override `OnDiagnosticAsync`, `OnRejectedAsync`, `OnCompletedAsync`, and `OnValidateFileAsync` for caller-owned reports and reconciliation. In `ValidateAndReport` mode, override the CSV processor's `OnValidatedUnitAsync` to observe units only after mapping, pure validation, and scoped custom validation succeed; a later file-level rejection may follow earlier unit reports. `UnitsReported` counts hook invocations that returned successfully, not durable writes. `CsvHeaderPolicy.Exact` requires all flat mapped selectors to be named and rejects unmapped header columns. The framework stores no diagnostic history. Invalid content produces diagnostics; operational faults and cancellation propagate, without a rejected/completed report. Logical paths do not encode physical line numbers.

Input bytes default to 64 MiB per pass; hierarchical roots default to 10,000 parser records. Limits stop validation incompletely and prevent replay. Use strict explicit encoding; matching Unicode BOMs are optional, conflicting BOMs do not switch encoding. Choose workload-appropriate admission/concurrency/time budgets. These are input/work bounds, not heap ceilings.

The `CoreEx.IO.Csv` ActivitySource and Meter expose file-level progress/outcomes; `csv.files.processed` has only bounded outcome tags. Do not add source names, attempts, raw rows, or tenant IDs to metric dimensions. Definition version is not a business idempotency key: durable deduplication belongs in the same transaction as the business mutation, keyed by immutable source revision and stable unit identity.

## Do Not

- Do not add Azure SDKs, trigger bindings, report storage, or implicit temporary snapshots to this transport-neutral package.
- Do not execute while still validating, retain all errors/rows, or use exceptions for ordinary malformed fields.
- Do not describe replay, report overwrites, or a filename key as exactly-once business execution or delivery.
- Do not bypass an immutable source guarantee by reopening a mutable local path or an unconditioned current Blob.
