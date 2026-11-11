# File generation

The `File` command generates host-neutral CoreEx.IO CSV and nested JSONL integrations from top-level `*-file.yaml` definitions alongside a consumer CodeGen project's `Program.cs`. It does not generate a transport trigger or require a Functions project. Runtime artifacts live together in an existing project referencing CoreEx.IO and CoreEx.Validation; CodeGen itself has no runtime dependency on CoreEx.IO. JSON document readers/selectors and XML are not implemented.

```powershell
Set-Location <CodeGen-project-directory>
dotnet run -- file
dotnet run -- file suppliers-file.yaml orders-file.yaml
dotnet run -- file --validate
dotnet run -- file --simulation
dotnet run -- file --expect-no-changes
```

Default discovery is nonrecursive and ordinal-sorted. Explicit filenames must also end with `-file.yaml`; `--config` selects a single file when no positional files are given. Paths resolve from the CodeGen output/project directory (OnRamp's `--output` overrides that basis). Script/template resources, parameters, simulation, and unchanged-output checks retain OnRamp semantics. Every selected configuration is loaded and prepared, including identity/type/path collision checks, before any output is written. This is not transactional filesystem generation: a subsequent I/O failure can leave partial output.

Final generated type names must not shadow runtime dependencies referenced by the templates, such as `DateTime`, `DateOnly`, `Guid`, `ReadOnlySpan`, or `CsvFileOptions`. This is a File-generator naming restriction, not a list of C# reserved keywords or every .NET type. Validation checks the composed definition-plus-record names and other generated types before writing any files; for example, definition `Date` with record `Time` is rejected because it would generate `DateTime`. Date conversion uses runtime converters, not per-property generated helpers.

## Generation-first Functions example

The framework's [InventoryImports example](../../../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md) keeps `name: Inventory`, `version: '1'`, and its single `Adjustment` record in `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml`. From the repository root, first change into the CodeGen project, then validate and generate:

```powershell
Set-Location tests\CoreEx.Azure.Functions.TestApp.CodeGen
dotnet run -- file --validate
dotnet run -- file
dotnet run -- file --expect-no-changes
```

`dotnet run --project <CodeGen-project>` selects the project to build/run but does not change the process working directory used for top-level `*-file.yaml` discovery. The definition generates `InventoryAdjustment`, `InventoryAdjustmentMapping`, `InventoryAdjustmentValidator`, `InventoryContext`, `InventoryProcessor`, and `InventoryHandler` under `InventoryImports/Files`; wire types, mapping, and validation are not handwritten.

Processors inherit protected `Services` from `FileProcessor<TUnit>` for handwritten partial hooks that need options and reporting adapters without adding a conflicting constructor. It exposes the construction-time provider; its lifetime follows the processor's registration rather than guaranteeing an invocation scope. Access without a supplied provider fails explicitly. Keep per-unit business services in the generated handler's fresh child scope. The native Blob trigger, SQL Server unit of work, durable receipt/outbox transaction, and Append Blob report adapter remain handwritten host/application responsibilities; no trigger or persistence code is emitted by File CodeGen.

Protected hooks receive purpose-specific argument objects with framework-owned construction and get-only properties, with cancellation separate and last. Handler validation uses `FileUnitValidationArgs<TUnit>` (`Unit`, `Info`, `Context`, and awaited `ReportDiagnosticAsync`); execution and validated-unit observation use `FileUnitArgs<TUnit>`. Generated unordered handlers narrow the unit with `args.As<TRecord>()`, preserving provenance, pass-local context, and the validation sink. Cast `args.Context` to the generated context when accessing custom reconciliation state.

## Nested JSONL definition

```yaml
name: Orders
version: '1'
format: Jsonl
runtimeProjectPath: ../Contoso.Integration
errorMode: CollectErrors
unit: { record: Order, cardinality: OneOrMore }
records:
- name: Order
  properties:
  - { name: order_id, property: Id, required: true }
  - { name: submitted_at, type: DateTimeOffset, required: true }
  - { name: billing_address, record: Address, required: true }
  - { name: shipping_address, record: Address }
  - { name: lines, collection: { record: Line, cardinality: OneOrMore, maxItems: 1000 } }
  - { name: tags, collection: { type: String, maxItems: 100 } }
- name: Address
  properties:
  - { name: code, required: true }
- name: Line
  properties:
  - { name: sku, required: true }
  - { name: quantity, type: Integer, required: true, min: 1 }
```

`name` is the exact case-sensitive direct source member; `property` overrides the CLR name derived from underscore/hyphen-separated source text. Choose exactly one scalar `type` (default String), nested `record`, or `collection` with exactly one `type`/`record`. Reuse Address at independent occurrences without duplicating its type. Missing/null scalars and optional objects remain null; a valid empty array remains empty, and missing/null collections count zero. Null collection elements reject. `required` on an object emits NotNull plus nested validation; collection `cardinality` defaults to Many, with One/Optional/OneOrMore and optional positive `maxItems` (no maximum when omitted). Scalar validation rules do not apply to nested occurrences.

`unit.record` is inferred only when exactly one non-metadata, non-child business record is eligible; otherwise select it explicitly. Unit cardinality defaults to Many and applies per file. Unknown references, recursive record graphs, duplicate source/CLR members, incompatible settings, and route collisions reject the batch before generation. Every format uses the file error policy; record-level `errorMode` is rejected.

Grouped `json` options are `unknownMemberPolicy` (Reject by default, optional Ignore) and `maxDepth` (64). Ignored content still undergoes syntax, duplicate, and parser-depth checks, but string values are decoded only when consumed. `jsonl.blankLinePolicy` defaults to Reject; explicit Skip excludes blanks from record/unit counts but not physical line numbering/byte accounting. Input is parsed as UTF-8, with LF/CRLF or a final nonempty unterminated line. A UTF-8 BOM is terminal; syntax/depth and consumed-string conversion errors can recover under CollectErrors. Malformed ignored string content can remain unexamined; there is no exhaustive UTF-8 validation pass.

The consumer owns source admission, overall input size, concurrency, timeouts, and report-output policy for both CSV and JSONL. CoreEx.IO imposes no file/line/token/metadata byte quota, unit/diagnostic count quota, CSV group-record quota, or default collection maximum. JSON retains the built-in parser's configurable `json.maxDepth` (64 by default), business cardinality, and optional explicitly declared positive collection `collection.maxItems`. A large JSONL line/CSV hierarchy can consume substantial memory; the framework is an application integration helper, not a bulk-data platform.

Native scalar tokens are strict; numeric strings, fractional integers, overflow, and nonfinite doubles reject. Empty/whitespace JSON strings are preserved. Dates use standard invariant .NET parsing without `format`, or exact parsing with no fallback when `format` is supplied. JSON DateTime rejects offsets; DateTimeOffset requires/preserves Z or a numeric offset. The same rules apply to scalar collections and date bounds/options. Mapping failure suppresses downstream validators, preventing conversion/requiredness cascades. Diagnostics separate CLR `Lines[0].Quantity` from escaped source pointers such as `/lines/0/quantity`, retaining the JSONL physical line.

### Opt-in JSONL metadata routes

Plain JSONL has no implicit discriminator. Enable routing explicitly and declare one business match plus bounded zero-or-one metadata matches:

```yaml
jsonl: { discriminator: type }
unit: { record: Order }
metadata:
- { name: Batch, record: Header, required: true, jsonl: { position: First } }
- { name: Totals, record: Trailer, required: true, jsonl: { position: Last } }
records:
- name: Header
  jsonl: { match: header }
  properties:
  - { name: batch_id, required: true }
- name: Order
  jsonl: { match: order }
  properties:
  - { name: order_id, required: true }
- name: Trailer
  jsonl: { match: trailer }
  properties:
  - { name: count, type: Integer, required: true }
```

Matches are exact, unique, and explicit on business/metadata roots only. The discriminator is a direct structural member, excluded from unexpected-member checks and DTOs unless explicitly mapped. This is not heterogeneous business dispatch. First/Last positions include malformed and unknown non-skipped lines. Metadata contributes to RecordsRead and recognized route counts, never UnitsRead or handler/report hooks; unroutable lines do not invent business candidates.

Generated typed context accessors expose encountered validated metadata and separate immutable accepted snapshots. The accepted view is unavailable during unit validation/reporting and single-pass execution; it is available from the first two-pass execution and at successful completion. Typed accepted reads return fresh copies. Default System.Text.Json serialization snapshots each declared metadata object, including serializable handwritten properties; mark transient properties `[JsonIgnore]` and keep reconciliation state on the context. Trailer-dependent totals belong in EOF reconciliation, not per-unit validation.

Checked-in [plain](../../../tests/CoreEx.CodeGen.Test.Unit/orders-jsonl-file.yaml) and [routed](../../../tests/CoreEx.CodeGen.Test.Unit/routed-jsonl-file.yaml) definitions generate the IO test consumers. Their runtime/compiled-generation tests exercise all three modes. The handwritten OrdersJsonl handler delegates to an application interface; its immutable-source/retry example uses an in-memory receipt double, not a durable exactly-once claim. Generate from the CodeGen test directory using the existing console:

```powershell
Set-Location tests\CoreEx.CodeGen.Test.Unit
dotnet run --project ..\CoreEx.Azure.Functions.TestApp.CodeGen -- file orders-jsonl-file.yaml routed-jsonl-file.yaml
dotnet run --project ..\CoreEx.Azure.Functions.TestApp.CodeGen -- file orders-jsonl-file.yaml routed-jsonl-file.yaml --expect-no-changes
```

## Flat CSV definition

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Avanade/CoreEx/refs/heads/main/schema/coreex-files.json
name: Suppliers
version: '1'
runtimeProjectPath: ../Contoso.Integration
runtimePath: Files/Suppliers
runtimeNamespace: Contoso.Integration.Files.Suppliers
mode: ValidateOnly
errorMode: CollectErrors
csv:
  headerPolicy: Exact
records:
- name: Row
  properties:
  - { name: Supplier, property: SupplierCode, required: true, maxLength: 50 }
  - name: Country
    property: CountryCode
    required: true
    referenceData:
      type: Contoso.Contracts.ReferenceData.Country
      allowInactive: false
  - { name: Quantity, type: Integer, required: true }
  - { name: EffectiveDate, type: DateOnly, format: yyyyMMdd }
```

Each property's `name` is its exact source field name; `property` optionally overrides the derived CLR name. Positional selection uses `csv.index` instead, and a named selector requires a column-name header. `csv.headerPolicy: Exact` is available for flat definitions where every property uses a named source field; it rejects extra header columns and data records whose field count differs from the header, while preserving case-insensitive, order-independent matching. The default `AllowAdditionalColumns` preserves existing behavior. All wire values are nullable; `required` emits `NotNull`, so numeric zero and Boolean false remain valid. Whitespace-only CSV strings become null, while nonblank strings are not trimmed. Supported types are String, Integer, Int64, Decimal, Double, Boolean, Guid, DateOnly, DateTime, and DateTimeOffset. CSV dates use standard invariant parsing without `format`, or exact parsing with no fallback when supplied; DateTimeOffset always requires/preserves an explicit offset.

Generated validators use the named source column as their friendly message label (`WithText`), or the property name for positional fields. Logical diagnostic property paths remain property-based; labels do not change mapping identity or attempt to infer physical coordinates. CSV syntax markers reject surrogate code units in configuration and at the runtime boundary.

Property-level validation also supports `mandatory: true` (rejects null and default values, including zero/false), String-only `minLength`, `maxLength`, `regex`, and `email`, inclusive `min`/`max` for strings/numbers/dates, Decimal-only `precision`/`scale`, and nonempty typed `options` for every supported type. Omit both presence flags for optional values; `mandatory` takes precedence over `required`. Other rules retain CoreEx's null-skipping behavior. Lengths are nonnegative (`maxLength: 0` permits only empty strings or null); precision is 1..29 and scale is 0..28, no greater than precision, without rounding. String bounds use CoreEx's culture-sensitive comparison. Date bounds/options use exact `format` when supplied and standard invariant parsing otherwise; numeric bounds/options use invariant values (Decimal/Double also accept numeric strings). Boolean/Guid do not support bounds. Invalid patterns, incompatible rule/type combinations, reversed bounds, and invalid option scalars fail before any batch output is written. Each property emits one fluent rule chain; regexes use cached `[GeneratedRegex]` methods with a one-second match timeout. Regex timeout is an operational fault, not a silently accepted value.

```yaml
- { name: Code, required: true, minLength: 2, maxLength: 8, regex: '^[A-Z]+$', options: [AA, BB] }
- { name: Amount, type: Decimal, min: 0, max: 999.99, precision: 5, scale: 2 }
- { name: Date, type: DateOnly, format: yyyyMMdd, min: '20260101', max: '20261231' }
- { name: Email, email: true }
```

`referenceData` applies only to String properties. The wire value remains a code, and the generated validator invokes CoreEx's `ReferenceData(r => r.With<TRef>())` rule, optionally allowing inactive values. Unknown/inactive codes produce normal validation messages, not exception-based content failures. Blanks are allowed unless `required` is true. Reference collections and the ambient `ReferenceDataOrchestrator` must be initialized before processing; missing setup is an operational fault, not a bad CSV code. No reference-data lookup or mutation happens in CSV conversion/replay, and no canonicalizing `Override()` is applied. These reference-backed rules are dependency-aware, unlike the other pure wire rules: the caller owns the initialized lookup context for the validation pass. Application services must still enforce business rules at execution time if reference data changes after validation.

## Unordered routed definition

Set `csv.discriminatorIndex` and omit `csv.structure` to process heterogeneous record layouts without grouping. Each physical record is an independent business unit, including records marked First or Last. ValidateOnly, ValidateThenExecute, and ValidateAndExecute are supported; the optional valid-unit hook is independent of mode, while ValidateAndExecute may retain earlier writes if later content errors occur.

```yaml
name: Imports
version: '1'
runtimeProjectPath: ../Contoso.Integration
csv:
  discriminatorIndex: 0
mode: ValidateThenExecute
errorMode: CollectErrors
records:
- name: Header
  csv: { match: H, cardinality: One, position: First }
  properties:
  - { name: Batch, csv: { index: 1 }, required: true }
- name: Supplier
  csv: { match: S, cardinality: OneOrMore }
  properties:
  - { name: Code, csv: { index: 1 }, required: true }
- name: Adjustment
  csv: { match: A }
  properties:
  - { name: Amount, type: Decimal, csv: { index: 1 }, required: true }
- name: Trailer
  csv: { match: T, cardinality: One, position: Last }
  properties:
  - { name: Count, type: Integer, csv: { index: 1 }, required: true }
```

Matches are exact and case-sensitive, without trimming; all fields are positional and exclude the discriminator index. Records may otherwise occur in any order. File-level `cardinality` defaults to Many and supports Many, One, Optional, and OneOrMore. Recognized records count even if fields are invalid. `position` defaults to Any; First/Last require One or Optional cardinality, with at most one of each boundary. Unknown/malformed records still participate in boundary-position checks, while skipped blank/comment lines do not. These routed records remain business units, never metadata unless explicitly declared under the shared `metadata` collection.

To declare file metadata, keep the typed route and positional mapping in `records`, but reference it from the top-level collection. A metadata route contributes to source-record and route-occurrence counts, but it does not consume a unit ordinal or invoke unit validation, reporting, or execution:

```yaml
metadata:
- { name: Batch, record: Header, required: true, csv: { position: First } }
- { name: Totals, record: Trailer, required: true, csv: { position: Last } }
records:
- name: Header
  csv: { match: H }
  properties:
  - { name: BatchId, csv: { index: 1 }, required: true }
- name: Supplier
  csv: { match: S }
  properties:
  - { name: Code, csv: { index: 1 }, required: true }
- name: Trailer
  csv: { match: T }
  properties:
  - { name: Count, type: Integer, csv: { index: 1 }, required: true }
```

Metadata declarations allow zero or one occurrence; `required: true` requires exactly one. CSV position defaults to Any, and First/Last constrain source order. Do not configure unordered route-level `cardinality` or `position` on a metadata record; those settings belong to its metadata declaration. First/Last routes not declared as metadata remain ordinary executable business units, preserving existing CSV behavior.

Generation emits a distinct `ImportsProcessor : CsvRoutedFileProcessor<ImportsRecord>`, typed rows deriving from the common `ImportsRecord` base, `ImportsContext : CsvRoutedContext`, and `ImportsHandlerBase` with type-safe dispatch. The create-only `ImportsHandler` inherits that generated base. Override methods such as `OnValidateSupplierAsync(FileUnitValidationArgs<ImportsSupplier> args, CancellationToken cancellationToken)` and `OnExecuteSupplierAsync(FileUnitArgs<ImportsSupplier> args, CancellationToken cancellationToken)` in a handwritten partial. These expose concrete rows and provenance without object casts or a handwritten type switch; cast `args.Context` for generated reconciliation state. Every execution method must be implemented for the business record types the file contains; metadata routes are excluded and defaults throw explicitly rather than silently skipping business units.

The generated context exposes recognized counts through `GetRecordCount`, `EndOfFileLineNumber`, and `HasValidationErrors`. Extend it with reconciliation accumulators and the processor with `OnValidateFileAsync`; fresh replay contexts do not copy validation state. Each accepted record gets a separate custom-validation scope and a separate execution scope, and pure/custom validation is not repeated on replay. No file-wide record buffer is retained; source admission is consumer-owned.

## Hierarchical definition

```yaml
name: Orders
version: '1'
runtimeProjectPath: ../Contoso.Integration
csv:
  discriminatorIndex: 0
  structure:
    root:
      record: Order
      cardinality: OneOrMore
      children:
      - { record: Line, cardinality: OneOrMore }
metadata:
- { name: Batch, record: Header, required: true, csv: { position: First } }
- { name: Totals, record: Trailer, required: true, csv: { position: Last } }
records:
- name: Header
  csv: { match: H }
  properties:
  - { name: BatchId, csv: { index: 1 }, required: true }
- name: Order
  csv: { match: O }
  properties:
  - { name: Id, type: Integer, csv: { index: 1 }, required: true }
- name: Line
  csv: { match: L }
  properties:
  - { name: Sku, csv: { index: 1 }, required: true }
- name: Trailer
  csv: { match: T }
  properties:
  - { name: Count, type: Integer, csv: { index: 1 }, required: true }
```

Hierarchies require positional selectors excluding the discriminator index. Matches are exact; route names are unique ignoring case. Every declared record appears exactly once across the business structure and metadata declarations. Ordered descendants may nest. Metadata declarations allow zero or one occurrence; set `required: true` for exactly one, with optional CSV `position: First` or `Last`. `Many`, `One`, `Optional`, and `OneOrMore` apply per parent (or per file for the root).

Wire records (`OrdersOrder`, `OrdersLine`) are separate types/files. A non-leaf position additionally gets an assembled type (`OrdersOrderGroup`) composing its `Record`, physical `LineNumber`, and ordered child lists; leaves have no wrapper. Cardinality is enforced by the runtime, not by nullable/list shape. Generated typed context exposes pass-local metadata accessors and immutable accepted metadata; the base context exposes counts and provenance. Runtime whole-file acceptance, streamed diagnostics, strict encoding, fresh replay contexts, and scoped business handlers are preserved.

## Ownership and use

Generated code assumes the runtime project's `GlobalUsing.cs` imports `CoreEx`, `CoreEx.DependencyInjection`, `CoreEx.IO`, `CoreEx.IO.Csv`, `CoreEx.Results`, `CoreEx.Validation`, `Microsoft.Extensions.DependencyInjection`, `System.Globalization`, `System.Text`, and `System.Text.RegularExpressions`, alongside the standard System/collections/IO/threading/task imports normally provided by implicit usings. Templates emit ordinary type names and extension calls, not per-file imports. Explicitly configured reference-data type names remain qualified to avoid ambiguity between consumer types.

JSONL consumers additionally import `CoreEx.IO.Json` and `CoreEx.IO.JsonLines`. No JSON document reader or transport package is required.

`name` drives the integration identity, type prefix, default `Files/<Name>` output folder, and namespace suffix. `runtimeProjectPath` accepts absolute paths or paths relative to the CodeGen project, including ordinary names not starting with a dot. `runtimePath` must stay inside that project. The default namespace uses the project directory name plus subfolder segments; specify `runtimeNamespace` when MSBuild uses another root namespace. Literal project XML is not treated as evaluated MSBuild.

Processors and handler starters carry `[ScopedService]`. Register them with `services.AddDynamicServicesUsing<SuppliersProcessor>()` and the host's normal CoreEx execution-context services; one scan registers every attributed integration in that assembly. Resolve `SuppliersProcessor`, create a context with `SuppliersProcessor.CreateContext(sourceId, attemptId, sourceRevision)`, and pass a fresh-stream factory to `ProcessAsync`. The default mode is ValidateOnly. Override `OnValidatedUnitAsync` on the generated processor to persist or emit each fully validated unit in any processing mode; the default is a no-op. `ContentFailed` is the mode-independent outcome when rows or EOF checks fail, even if earlier reports or single-pass writes completed. `SuppliersHandler.cs` is a create-only handwritten starter: override its inherited validation/execution methods to call application services, returning their Result unchanged. Unimplemented execution throws explicitly. Generated processors expose inherited reporting/reconciliation hooks through handwritten partials or subclasses.

Generated `.g.cs` files are owned by the templates, identify the generator in an auto-generated warning banner, and wrap their contents in `#nullable enable` / `#nullable restore`. Extend partial wire models/validators/contexts/processors in separate handwritten files; validators expose `OnInitialization`. Never edit generated output. Handler starters remain handwritten, without an auto-generated warning, and are never overwritten, even during unchanged-output checks. Renaming/removing definitions does not delete old artifacts automatically.

Generated constants identify the opaque developer-maintained definition revision. A processor rejects a wrong context type/name/version before opening the input. Bump version when interpretation, mapping, validation, or structure changes; it is not source revision, version negotiation, package version, or an automatic business deduplication key. Whole-file acceptance does not make execution atomic or exactly-once; caller-owned immutable sources and application transactions/idempotency remain necessary.

The [configuration reference](https://github.com/Avanade/CoreEx/blob/main/src/CoreEx.CodeGen/Files/docs/CodeGeneration.md) and [JSON schema](https://github.com/Avanade/CoreEx/blob/main/schema/coreex-files.json) describe available settings and categories. The installed AI bundle includes release-matched copies under `/.github/docs/coreex/files/` and `/.github/docs/coreex/schema/`.
