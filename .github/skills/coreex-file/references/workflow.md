# File ingestion workflow

## 1. Inspect before asking

Read the installed CoreEx.IO/CodeGen guides, existing CodeGen `Program.cs`, runtime project references and GlobalUsing, existing definitions, handlers and host composition. Resolve recorded project choices first. Ask only unresolved input shape, execution intent, source revision ownership, and durable deduplication questions. For Functions ingestion, also read the host's Functions instructions and use the `coreex-functions` skill for the trigger boundary.

Choose flat for one row type; unordered routing for independent heterogeneous records; hierarchy only when contiguous descendants form business units. A typed header/trailer is a data envelope, not a column-name header.

## 2. Configure and generate

Add a top-level `*-file.yaml` with required `name`, opaque `version`, existing `runtimeProjectPath`, and `records`; generate before adding handwritten business behavior. Run `dotnet run -- file ...` with the CodeGen console as the current working directory—`dotnet run --project <path>` selects the project to run but does not change the process directory used for top-level configuration discovery. Use `runtimePath` and `runtimeNamespace` overrides when appropriate. For routing specify `discriminatorIndex` and exact record `match`; unordered types use per-file `cardinality`/`position`, hierarchies declare relationship cardinality in `structure`.

Each property specifies exactly one zero-based `index` or named `column`. Routed files use indexes. Logical types: String, Integer, Int64, Decimal, Double, Boolean, Guid, DateOnly, DateTime. Dates require an exact invariant `format`. Keep raw values nullable; use `required: true` when zero/false are valid or `mandatory: true` when defaults must fail.

Declare compatible length/regex/email, inclusive min/max, precision/scale, typed options, or string-code referenceData rules. Do not add ad-hoc converters to duplicate native CoreEx validation. Reference-data lookup state is initialized by the caller before validation.

Point the editor to the installed `/.github/docs/coreex/schema/coreex-files.json` using a relative yaml-language-server schema directive (or use the release-pinned remote schema). Run validation for all selected files before generation, then generate. Do not manually edit `.g.cs`. Add runtime global imports from the installed configuration guide; preserve handwritten handler files across regeneration.

## 3. Implement scoped business behavior

Keep `[ScopedService]` on processors and handlers and scan their assembly with `AddDynamicServicesUsing<TProcessor>()`. Register normal CoreEx execution-context services. Put execution in the handwritten handler's typed overrides; call application services and preserve their Result. A generated processor exposes protected `Services` for handwritten partial hooks that need invocation-scoped options/report adapters without a conflicting constructor. Do not resolve unit business services there: the generated handler resolves them from the fresh child scope. Leave validation-only mode until execution is implemented and tested.

Propagate trusted user/tenant context explicitly before resolving business services; CSV data is not authentication. Keep custom unit validation read-only and use the awaited diagnostic sink. Override EOF reconciliation for totals/counts and reporting hooks for caller-owned output without accumulating every error.

## 4. Adapt source and retries

Use a cancellation-aware fresh-stream factory. Guarantee identical immutable bytes on both opens (protected local read view or revision/ETag-pinned Blob reads); a seekable stream or current path is not a snapshot. Choose whether a Blob invocation targets the current or historical event revision and document the limitation.

Use credential-free full source identity, revision and distinct attempt ID. Isolate reports by definition/source/revision/attempt and choose report-failure retry semantics. For production Blob imports, pin both validation and execution reads to the selected ETag/version, write reports to durable Blob storage, and commit a durable unit-deduplication marker, business mutation, and transactional outbox write together through the application's supported unit of work. The framework InventoryImports example uses SQL Server. Earlier units may already have committed when a later unit fails. Do not silently downgrade execution failure to successful trigger completion or claim exactly-once delivery.

## 5. Verify the actual generated consumer

Compile and run the generated consumer, not just the generator. Prove valid execution, late invalid content causes zero writes, partial-failure retry avoids duplicate business mutations, and incorrect identity fails before opening input. Test mappings/rules, streamed report failures, source revision pinning, cancellation, and scope isolation. For the Aspire Functions proof, upload the real input to Azurite and post a storage-shaped notification through the Functions webhook; `EventGridExtensionMethods.CreateBlobCreatedEvent` creates the notification, not an Event Grid delivery. The Functions binding, Blob SDK operations, SQL Server transaction, and Blob report writes are real in the harness. Azurite does not emit Event Grid events, so this does not prove Azure subscription provisioning, authentication, or cloud delivery.

Regenerate schemas/docs through tooling when changing framework config types. Keep packaged AI guides, schema references and template asset assertions aligned. Renaming/removing a definition does not authorize deleting arbitrary output directories.
