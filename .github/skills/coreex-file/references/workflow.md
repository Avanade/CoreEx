# File ingestion workflow

## 1. Inspect before asking

Read the installed CoreEx.IO/CodeGen guides, existing CodeGen `Program.cs`, runtime project references and GlobalUsing, existing definitions, handlers and host composition. Resolve recorded project choices first. Ask only unresolved input shape, execution intent, source revision ownership, and durable deduplication questions. For Functions ingestion, also read the host's Functions instructions and use the `coreex-functions` skill for the trigger boundary.

Reuse an existing CodeGen console. If none exists, obtain explicit tool-creation scope and route through `coreex-scaffold` to `coreex-codegen -n <full-name>.CodeGen`; do not run the full `coreex` scaffold or enable reference data/database just to get a tool. The console follows effective CPM and has no runtime-project reference. A missing repository baseline is a separate scaffold decision, not an excuse to overwrite root policy.

Choose CSV flat for one row type, unordered routing for independent heterogeneous records, and hierarchy only when contiguous descendants form business units. Choose JSONL for one natural nested object per UTF-8 line, optionally with discriminator-routed metadata. A typed header/trailer is a data envelope, not a column-name header. JSON document/XML readers are not implemented.

## 2. Configure and generate

Add a top-level `*-file.yaml` with required `name`, opaque `version`, existing `runtimeProjectPath`, and `records`; generate before adding handwritten business behavior. Run `dotnet run -- file ...` with the CodeGen console as the current working directory—`dotnet run --project <path>` selects the project to run but does not change the process directory used for top-level configuration discovery. Use `runtimePath` and `runtimeNamespace` overrides when appropriate. Format defaults to Csv; CSV routing uses grouped `csv.discriminatorIndex` and record `csv.match`, with unordered per-file `csv.cardinality`/`csv.position` or hierarchical `csv.structure` occurrences.

Each property's `name` is its source name; `property` optionally overrides the derived CLR name. Positional CSV uses grouped `csv.index`; routed CSV requires indexes. Logical scalars: String, Integer, Int64, Decimal, Double, Boolean, Guid, DateOnly, DateTime, DateTimeOffset. Dates use standard invariant parsing without `format`, or exact parsing without fallback when supplied; bounds/options follow the same rule. Keep raw values nullable; use `required: true` when zero/false are valid or `mandatory: true` when defaults must fail. Every format uses one file error policy, without record-level overrides.

JSONL selects `format: Jsonl`, optionally grouped `json` nesting-depth/unknown-member policy, and `unit.record` when more than one eligible root exists. Nested properties use `record: Address` or `collection: { record: Line, cardinality: OneOrMore, maxItems: 1000 }`; scalar collections use `type` instead of `record`. No CSV selectors or document paths apply. JSON native tokens are strict and preserve empty strings; JSON DateTime rejects offsets and DateTimeOffset requires/preserves an explicit offset. Ignored strings are not exhaustively decoded. Optional metadata routing requires explicit `jsonl.discriminator`, root record `jsonl.match` values, and shared `metadata` declarations with optional `jsonl.position`. It does not enable heterogeneous business dispatch. Accepted metadata uses ordinary System.Text.Json snapshots; exclude transient DTO properties with JsonIgnore.

Declare compatible length/regex/email, inclusive min/max, precision/scale, typed options, or string-code referenceData rules. Do not add ad-hoc converters to duplicate native CoreEx validation. Reference-data lookup state is initialized by the caller before validation.

Point the editor to the installed `/.github/docs/coreex/schema/coreex-files.json` using a relative yaml-language-server schema directive (or use the release-pinned remote schema). Run validation for all selected files before generation, then generate. Do not manually edit `.g.cs`. Add runtime global imports from the installed configuration guide; preserve handwritten handler files across regeneration.

## 3. Implement scoped business behavior

Keep `[ScopedService]` on processors and handlers and scan their assembly with `AddDynamicServicesUsing<TProcessor>()`. Register normal CoreEx execution-context services. Put execution in the handwritten handler's typed overrides; call application services and preserve their Result. Processors inherit protected `Services` from `FileProcessor<TUnit>` for coordinator options/report adapters; it is the construction-time provider, not an automatic invocation/unit scope. Do not resolve unit business services there: the generated handler resolves them from the fresh child scope. Leave validation-only mode until execution is implemented and tested.

Propagate trusted user/tenant context explicitly before resolving business services; CSV data is not authentication. Keep custom unit validation read-only and use the awaited diagnostic sink. Override EOF reconciliation for totals/counts and reporting hooks for caller-owned output without accumulating every error.

## 4. Adapt source and retries

The consumer owns source admission, input size, concurrency, timeouts, and report-output policy; the framework does not impose resource quotas. Use a cancellation-aware fresh-stream factory. Guarantee identical immutable bytes on both opens (protected local read view or revision/ETag-pinned Blob reads); a seekable stream or current path is not a snapshot. Choose whether a Blob invocation targets the current or historical event revision and document the limitation.

Use credential-free full source identity, revision and distinct attempt ID. Isolate reports by definition/source/revision/attempt and choose report-failure retry semantics. For production Blob imports, pin both validation and execution reads to the selected ETag/version, write reports to durable Blob storage, and commit a durable unit-deduplication marker, business mutation, and transactional outbox write together through the application's supported unit of work. The framework InventoryImports example uses SQL Server. Earlier units may already have committed when a later unit fails. Do not silently downgrade execution failure to successful trigger completion or claim exactly-once delivery.

## 5. Verify the actual generated consumer

Compile and run the generated consumer, not just the generator. Prove valid execution, zero execution after late ValidateThenExecute rejection, honest ValidateAndExecute partial writes, application-owned retry deduplication, and incorrect identity rejection before opening input. Test nested paths/cardinality, malformed lines followed by valid units, consumed-string decoding failures versus ignored content, duplicate members (also ignored), exact source-byte accounting, standard/exact dates, parser depth and business collection bounds, streamed report faults, cancellation, and scope isolation. For the Aspire Functions proof, upload real input to Azurite and post a storage-shaped notification through the Functions webhook; this simulates notification delivery, while the Functions binding, Blob SDK operations, SQL Server transaction, and Blob report writes are real. It does not prove Azure subscription provisioning, authentication, or cloud delivery.

Regenerate schemas/docs through tooling when changing framework config types. Keep packaged AI guides, schema references and template asset assertions aligned. Renaming/removing a definition does not authorize deleting arbitrary output directories.
