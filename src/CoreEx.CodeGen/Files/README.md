# File generation

The `File` command generates host-neutral CoreEx.IO CSV integrations from top-level `*-file.yaml` definitions alongside a consumer CodeGen project's `Program.cs`. It does not generate a transport trigger or require a Functions project. Runtime artifacts live together in an existing project referencing CoreEx.IO and CoreEx.Validation; CodeGen itself has no runtime dependency on CoreEx.IO.

```powershell
dotnet run -- file
dotnet run -- file suppliers-file.yaml orders-file.yaml
dotnet run -- file --validate
dotnet run -- file --simulation
dotnet run -- file --expect-no-changes
```

Default discovery is nonrecursive and ordinal-sorted. Explicit filenames must also end with `-file.yaml`; `--config` selects a single file when no positional files are given. Paths resolve from the CodeGen output/project directory (OnRamp's `--output` overrides that basis). Script/template resources, parameters, simulation, and unchanged-output checks retain OnRamp semantics. Every selected configuration is loaded and prepared, including identity/type/path collision checks, before any output is written. This is not transactional filesystem generation: a subsequent I/O failure can leave partial output.

## Flat definition

```yaml
# yaml-language-server: $schema=https://raw.githubusercontent.com/Avanade/CoreEx/refs/heads/main/schema/coreex-files.json
name: Suppliers
version: '1'
runtimeProjectPath: ../Contoso.Integration
runtimePath: Files/Suppliers
runtimeNamespace: Contoso.Integration.Files.Suppliers
mode: ValidateOnly
errorMode: CollectErrors
records:
- name: Row
  properties:
  - { name: SupplierCode, column: Supplier, required: true, maxLength: 50 }
  - name: CountryCode
    column: Country
    required: true
    referenceData:
      type: Contoso.Contracts.ReferenceData.Country
      allowInactive: false
  - { name: Quantity, type: Integer, column: Quantity, required: true }
  - { name: EffectiveDate, type: DateOnly, column: EffectiveDate, format: yyyyMMdd }
```

Each property specifies exactly one zero-based `index` or named `column`. A named selector requires a column-name header. All wire values are nullable; `required` emits `NotNull`, so numeric zero and Boolean false remain valid. Whitespace-only strings become null, while nonblank strings are not trimmed. Supported types are String, Integer, Int64, Decimal, Double, Boolean, Guid, DateOnly, and DateTime. Dates require an exact invariant format.

Property-level validation also supports `mandatory: true` (rejects null and default values, including zero/false), String-only `minLength`, `maxLength`, `regex`, and `email`, inclusive `min`/`max` for strings/numbers/dates, Decimal-only `precision`/`scale`, and nonempty typed `options` for every supported type. Omit both presence flags for optional values; `mandatory` takes precedence over `required`. Other rules retain CoreEx's null-skipping behavior. Lengths are nonnegative (`maxLength: 0` permits only empty strings or null); precision is 1..29 and scale is 0..28, no greater than precision, without rounding. String bounds use CoreEx's culture-sensitive comparison. Date bounds/options use the property's exact `format`; numeric bounds/options use invariant values (Decimal/Double also accept numeric strings). Boolean/Guid do not support bounds. Invalid patterns, incompatible rule/type combinations, reversed bounds, and invalid option scalars fail before any batch output is written. Each property emits one fluent rule chain; regexes use cached `[GeneratedRegex]` methods with a one-second match timeout. Regex timeout is an operational fault, not a silently accepted value.

```yaml
- { name: Code, column: Code, required: true, minLength: 2, maxLength: 8, regex: '^[A-Z]+$', options: [AA, BB] }
- { name: Amount, column: Amount, type: Decimal, min: 0, max: 999.99, precision: 5, scale: 2 }
- { name: Date, column: Date, type: DateOnly, format: yyyyMMdd, min: '20260101', max: '20261231' }
- { name: Email, column: Email, email: true }
```

`referenceData` applies only to String properties. The wire value remains a code, and the generated validator invokes CoreEx's `ReferenceData(r => r.With<TRef>())` rule, optionally allowing inactive values. Unknown/inactive codes produce normal validation messages, not exception-based content failures. Blanks are allowed unless `required` is true. Reference collections and the ambient `ReferenceDataOrchestrator` must be initialized before processing; missing setup is an operational fault, not a bad CSV code. No reference-data lookup or mutation happens in CSV conversion/replay, and no canonicalizing `Override()` is applied. These reference-backed rules are dependency-aware, unlike the other pure wire rules: the caller owns the initialized lookup context for the validation pass. Application services must still enforce business rules at execution time if reference data changes after validation.

## Hierarchical definition

```yaml
name: Orders
version: '1'
runtimeProjectPath: ../Contoso.Integration
discriminatorIndex: 0
structure:
  root:
    record: Order
    cardinality: OneOrMore
    children:
    - { record: Line, cardinality: OneOrMore }
records:
- name: Order
  match: O
  properties:
  - { name: Id, type: Integer, index: 1, required: true }
- name: Line
  match: L
  properties:
  - { name: Sku, index: 1, required: true }
```

Hierarchies require positional selectors excluding the discriminator index. Matches are exact; route names are unique ignoring case. Every declared record appears exactly once in `structure`. Ordered descendants may nest; optional `header` and `trailer` are leaf nodes with `One` or `Optional` cardinality. `Many`, `One`, `Optional`, and `OneOrMore` apply per parent (or per file for the root).

Wire records (`OrdersOrder`, `OrdersLine`) are separate types/files. A non-leaf position additionally gets an assembled type (`OrdersOrderGroup`) composing its `Record`, physical `LineNumber`, and ordered child lists; leaves have no wrapper. Cardinality is enforced by the runtime, not by nullable/list shape. Generated typed context exposes accepted envelopes; base context exposes counts and provenance. Runtime whole-file acceptance, streamed diagnostics, finite input/group budgets, strict encoding, fresh replay contexts, and scoped business handlers are preserved.

## Ownership and use

Generated code assumes the runtime project's `GlobalUsing.cs` imports `CoreEx`, `CoreEx.DependencyInjection`, `CoreEx.IO`, `CoreEx.IO.Csv`, `CoreEx.Validation`, `Microsoft.Extensions.DependencyInjection`, `System.Globalization`, `System.Text`, and `System.Text.RegularExpressions`, alongside the standard System/collections/IO/threading/task imports normally provided by implicit usings. Templates emit ordinary type names and extension calls, not per-file imports. Explicitly configured reference-data type names remain qualified to avoid ambiguity between consumer types.

 Templates emit ordinary type names and extension calls, not per-file imports. Explicitly configured reference-data type names remain qualified to avoid ambiguity between consumer types.

`name` drives the integration identity, type prefix, default `Files/<Name>` output folder, and namespace suffix. `runtimeProjectPath` accepts absolute paths or paths relative to the CodeGen project, including ordinary names not starting with a dot. `runtimePath` must stay inside that project. The default namespace uses the project directory name plus subfolder segments; specify `runtimeNamespace` when MSBuild uses another root namespace. Literal project XML is not treated as evaluated MSBuild.

Processors and handler starters carry `[ScopedService]`. Register them with `services.AddDynamicServicesUsing<SuppliersProcessor>()` and the host's normal CoreEx execution-context services; one scan registers every attributed integration in that assembly. Resolve `SuppliersProcessor`, create a context with `SuppliersProcessor.CreateContext(sourceId, attemptId, sourceRevision)`, and pass a fresh-stream factory to `ProcessAsync`. The default mode is ValidateOnly. `SuppliersHandler.cs` is a create-only handwritten starter: override its inherited validation/execution methods to call application services, returning their Result unchanged. Unimplemented execution throws explicitly. Generated processors expose inherited reporting/reconciliation hooks through handwritten partials or subclasses.

When regenerating an existing integration that previously emitted a services extension, remove its obsolete `<Name>ServiceCollectionExtensions.g.cs` and replace its host registration with the assembly scan. Existing create-only handlers are preserved: add `[ScopedService]` to those handlers manually (and import `CoreEx.DependencyInjection`) rather than expecting regeneration to overwrite them.

Generated `.g.cs` files are owned by the templates, identify the generator in an auto-generated warning banner, and wrap their contents in `#nullable enable` / `#nullable restore`. Extend partial wire models/validators/contexts/processors in separate handwritten files; validators expose `OnInitialization`. Never edit generated output. Handler starters remain handwritten, without an auto-generated warning, and are never overwritten, even during unchanged-output checks. Renaming/removing definitions does not delete old artifacts automatically.

Generated constants identify the opaque developer-maintained definition revision. A processor rejects a wrong context type/name/version before opening the input. Bump version when interpretation, mapping, validation, or structure changes; it is not source revision, version negotiation, package version, or an automatic business deduplication key. Whole-file acceptance does not make execution atomic or exactly-once; caller-owned immutable sources and application transactions/idempotency remain necessary.

The [configuration reference](./docs/CodeGeneration.md) and [JSON schema](../../../schema/coreex-files.json) describe available settings and categories. Optional host adapter examples are a separate integration step.
