# CoreEx.IO multi-format integration plan

## Phase 0 outcome

The final contract review replaces JSON shape with kind (Array default, expected kind at unitPath), makes accepted metadata at successful completion mandatory, and renames the content-failure finalizer to OnContentFailedAsync so partial writes cannot hide behind a rejection name. It also specifies missing/null selection, diagnostic scope, failure precedence, limit-boundary counters, and the CSV parser-span spike required to substantiate per-unit byte limits. The detailed edge contracts are in Phase 0 section 12.

Phase 0 is specified in the companion `coreex-io-phase0-contract.md`, with literal representative inputs, an expectation manifest, and a reproducible fixture inspector under `phase0-fixtures`. It defines the grouped YAML shape, nested type/occurrence model, API migration inventory, source-record versus business-unit accounting, resource budgets, conversion policies, and Phase 1 handoff. Runtime behavior is not implemented by these artifacts.

The user selected a bounded immutable accepted-metadata view available from the beginning of ValidateThenExecute replay, including metadata occurring after units in the source. ValidateAndExecute is a separate opt-in single-pass mode, with encountered metadata only, explicit partial-execution outcomes, and failure reporting. Metadata is shared across formats; opt-in JSONL discriminator routing ships with JSONL. The scalar contract includes DateTimeOffset rather than imposing an offset-handling workaround on JSON integrations.

Required primitives currently provide no concrete reason to raise a package's target-framework floor. Retain the package-level decision gate: use net10.0 when a material implementation benefit justifies it, rather than degrading functionality or adding parallel compatibility implementations. This does not commit the repository to retaining out-of-support targets.

## Recommendation

Extend CoreEx.IO and the existing CoreEx.CodeGen File command to support JSONL and JSON first, then XML. Extract the existing file lifecycle from its CSV implementation, introduce explicit document-unit selection and nested wire models, and retain format-specific parsing and mapping. Use .NET's built-in JSON/XML libraries; no additional third-party runtime dependency is planned.

Prefer a small, well-explained breaking change now over carrying CSV-shaped APIs into every future format. Preserve the existing processing guarantees, not every incidental type name or generated YAML shape. Any break must remove a concrete constraint, have a migration example, and be recorded before implementation; this is not permission for unrelated cleanup.

The product remains a host-neutral integration/workflow component: identify bounded typed business units, validate/report, and optionally execute accepted units using whole-file acceptance plus replay or explicit single-pass unit acceptance. It is not a lakehouse, ETL designer, schema-inference engine, general document query engine, or Microsoft Fabric replacement.

## Goals and non-goals

### Goals

- Reuse whole-file acceptance, immutable replay, scoped validation/execution, reporting, cancellation, reconciliation, and application-owned idempotency across formats.
- Support repeated, bounded business units containing nested objects and collections.
- Keep CodeGen generation-first: definitions own wire types, mappings, validators, contexts, processors, and create-only handler starters.
- Produce actionable diagnostics without retaining a whole document or file-wide error history.
- Make supported shapes, conversion semantics, error recovery, and operating limits explicit.
- Preserve the ability to use runtime APIs directly without requiring CodeGen.

### Non-goals

- Executing units before whole-file acceptance in ValidateThenExecute mode.
- Whole-file business transactions, automatic compensation, or exactly-once processing.
- Arbitrary joins, sorting, grouping, or flattening across distant document sections.
- JSONPath/XPath engines, XSLT, schema inference, JSON Schema/XSD generation or validation in the initial release.
- Transport triggers, report storage, source snapshots, archive extraction, or cloud SDKs in CoreEx.IO.
- File export/writing as part of this ingestion extension.

## Current constraints and proposed changes

These are observed implementation constraints, not hypothetical architectural objections.

| Current implementation | Constraint | Proposed treatment |
|---|---|---|
| `FileProcessor` exposes a format-neutral invocation and reporting contract. | It does not own the common lifecycle. | Retain the contract and place reusable orchestration in a generic shared implementation. |
| `CsvFileProcessorBase<TUnit>` owns lifecycle, scopes, validation, replay, counters, and CSV reading. | New formats must duplicate lifecycle or depend on CSV options/state. Its `private protected` extension points are also assembly-restricted. | Separate orchestration from format reading/assembly. Design the smallest supported extension boundary; do not make all mutable internals public. |
| `CsvReadStream` implements byte limits and strict BOM/encoding checks. | Useful stream mechanics are coupled to CSV codes and encoding policy. | Extract bounded input mechanics; keep decoding/BOM decisions with each format reader. |
| `CsvFileOptions` combines dialect, input bytes, and per-group record limits. | A document unit is not a CSV root group or a count of physical rows. | Separate common input/processing budgets from format options and format-specific assembly limits. Accept constructor/options changes where they remove this coupling. |
| `FileProcessingProgress` describes physical records and constrains validated-unit counts against valid record counts. | JSON/XML have no universal physical-record concept; descendant diagnostics must not invent invalid top-level records. | Define records as format-declared source records and units as handler boundaries. Specify counting per format, adjust API/documentation/invariants where necessary, and never inflate counters just to satisfy an old assertion. |
| `CsvHierarchyDefinition` requires each route to appear exactly once; `NodeConfig` resolves nodes by record name. | The same document type may legitimately occur at several paths, such as billing/shipping addresses. | Separate reusable wire-type definitions from mapping occurrences. Give occurrences independent source selectors and cardinalities. Leave CSV routing rules CSV-specific. |
| CSV hierarchy nodes require ordered contiguous children and generated group wrappers. | JSON object-member order is not meaningful; document nesting already supplies ownership. | Map document objects directly to typed object graphs. Do not reuse the CSV hierarchy assembler or require `Record`/`Group` wrappers for JSON/XML. |
| File CodeGen requires scalar properties with CSV `index`/`column` selectors and exact date formats. | Cannot express nested records, arrays, JSON token types, or XML attributes/namespaces naturally. | Add format-aware mapping configuration and nested object/collection references. Share validation definitions where semantics match, not where syntax merely looks similar. |
| `CsvFieldConverters` treats whitespace-only fields as null. | Applying this to JSON/XML silently changes supplied values. | Preserve source strings by default for document formats; normalization must be explicit. |
| CSV invoker, metric names, and many common diagnostic codes use `csv.*`. | New formats would emit misleading instrumentation. | Introduce neutral lifecycle instrumentation and format-specific parsing/mapping codes. Document telemetry migration; do not duplicate metrics indefinitely to avoid a clean break. |
| `FileLocation` already has optional physical coordinates and logical paths. | It is usable, but callers need an unambiguous unit ordinal and nested-path rules. | Reuse it; add explicit unit provenance to `FileUnitInfo` and relevant handler/reporting surfaces if needed. Avoid magic line numbers. |

### Compatibility policy

Keep ordinary CSV parsing, routing, header behavior, hierarchy semantics, validation, and execution outcomes unchanged unless a separately documented correction is necessary. A breaking API/configuration migration is acceptable; a silent behavioral change is not.

Do not pre-emptively rename every CSV type or rewrite its generated group model. Keep CSV-only abstractions where they accurately describe CSV. Promote shared concepts, such as cardinality, only when they genuinely have identical semantics. Phase 0 identifies new unit-size/unit-count/diagnostic budgets as intentional CSV admission changes; document these defaults rather than promising every previously accepted input remains accepted.

Prefer retaining existing YAML definitions through an absent `format` meaning CSV if that is straightforward. If a clean configuration structure requires changes, ship a before/after migration rather than a permanent maze of aliases. Regenerate owned output; never overwrite handwritten handlers or extension partials.

## Target processing model

```text
Caller-owned immutable stream factory
  -> bounded format reader and explicit unit selection
  -> bounded source unit + provenance
  -> typed mapping and structural checks
  -> nested CoreEx validation
  -> fresh scoped custom validation
  -> awaited reporting / EOF reconciliation
  -> whole-file acceptance
  -> fresh stream, reader, mappings, and pass context
  -> reconstruction and fresh scoped business execution
```

The diagram describes ValidateThenExecute. ValidateOnly and ValidateAndReport stop after their one validation pass. ValidateAndExecute instead executes each fully assembled, mapped, and validated unit immediately in a separate fresh scope, then continues reading the same stream. A later content/EOF error can leave earlier units committed; this mode does not promise whole-file acceptance before writes or require a replayable source.

All modes emit awaited content diagnostics. Single-pass FailFast stops on content failure; CollectErrors skips invalid units and continues only where recovery is trustworthy. A failed business Result always stops execution and reaches an explicit awaited execution-failure hook with original Result/unit provenance. Operational faults and cancellation propagate, not as content diagnostics. Add unsuccessful ExecutedWithErrors for single-pass content failures with accurate executed counts; make summary/finalization rules mode-aware instead of forcing partial writes into zero-write Rejected semantics.

Retain FileProcessor and the File naming family: finite file content is the processing boundary, while streams are the transport. This is not a continuous StreamProcessor.

Keep the orchestration in one place. Readers handle syntax, document traversal, source boundaries, and recoverability; mappings handle field conversion and declared structure; the coordinator handles policy, scopes, accounting, and lifecycle. Do not hide format differences behind a universal token model or force CSV to materialize JSON-like trees.

The initial document implementation should stream the outer document and materialize at most one explicitly bounded unit subtree for mapping. Unit-local mapping diagnostics should be streamed or bounded as well. A small reader buffer and bounded envelope metadata are acceptable; retaining all units, all diagnostics, or the full document is not.

The exact shared base/reader API should be settled with a working CSV extraction and a JSONL vertical slice, rather than designing a large public parser plug-in framework in advance.

## Supported document shapes

| Format/mode | Declared source shape | Handler unit |
|---|---|---|
| JSONL | One complete JSON object on each physical line. | The object and its descendants. |
| JSON root array | `[ { ... }, { ... } ]` | Each object element and its descendants. |
| JSON envelope | `{ "batch": { ... }, "orders": [ { ... } ], "totals": { ... } }` | Each object in the explicitly selected array. |
| JSON single object | `{ ... }`, selected explicitly as a single-unit document. | The root object. |
| XML repeated elements | A document with repeated elements at a declared namespace-aware child path. | Each selected element and its subtree. |
| XML single root | An explicitly selected root element. | That root subtree. |

Initial JSON business units are objects, not arbitrary scalar values. Scalar properties and scalar collections inside units are supported by the nested mapping model. Multiple concatenated JSON roots, arrays as JSONL business units, and multiple repeated unit streams in one envelope are deferred.

A nested collection belongs to its owning unit; it is not independently executed. Documents containing separate arrays related only by IDs require application-owned reconciliation or a future explicitly designed capability, not implicit joining.

Use restricted structural selectors: JSON Pointer-style navigation to a declared array/object, and namespace-aware XML child-element paths. Do not imply support for full JSON Pointer evaluation across arbitrary array indices, JSONPath, or XPath. Require selectors that can be resolved in a forward-only pass and fail configuration validation for unsupported forms.

## Shared mapping and validation semantics

### YAML configuration ergonomics

Group genuinely format-specific settings under `csv`, `json`, `jsonl`, or `xml` objects rather than introducing families of prefixed keys such as `csvDelimiter`, `jsonUnitPath`, and `xmlNamespaces`. Use the existing lower-camel-case YAML convention; C# configuration properties may naturally be named `Csv`, `Json`, and so on. Group by semantic ownership, not merely when a property count reaches three.

Keep singular, unambiguous concepts directly on their owner. In particular, a date property's `format` remains `format`, not `dateFormat`, `jsonFormat`, or `json: { format: ... }`. A definition-level `format: Json` and a property-level `format: yyyyMMdd` have different, clear scopes and need no artificial disambiguation.

Common identity, processing policy, limits, type shape, cardinality, and validation rules remain format-neutral. A format-specific object holds parser/framing settings at definition level and source-selection settings at mapping-occurrence/property level. Do not move a shared rule into several format blocks merely because each parser uses it. Every optional YAML property has a documented schema-aligned default; specify only deviations, without guessing required identity or ambiguous selectors. XML field defaults are Attribute and empty namespace; element/namespaced mappings explicitly override those defaults.

Configuration direction, incorporating the Phase 0 source-first naming decision:

```yaml
name: Orders
version: '2'
format: Json
runtimeProjectPath: ../Contoso.Integration
json:
  kind: Array
  unitPath: /orders
  unknownMemberPolicy: Reject
records:
- name: Order
  properties:
  - name: effective_date
    type: DateOnly
    format: yyyyMMdd
    required: true
```

For property declarations, name is the exact source field name. Derive the C# property using fixed DbEx-style PascalCase conversion: split underscore/hyphen separators, PascalCase each part, and concatenate. An optional property overrides the generated identifier exactly; the source selector never changes. Thus effective_date generates EffectiveDate without a mapping block. There are no propertyCasing/fieldCasing options, json.name aliases, or csv.column aliases. Reject invalid identifiers and normalized-name collisions with an actionable property-override error rather than guessing repairs.

JSON kind defaults to Array and can normally be omitted; it describes the selected value at unitPath, not the document root. Object is the other supported value. XML field kind is a separate Attribute/Element enum, not a shared cross-format kind vocabulary beyond the common property spelling.

Record names remain logical type names. Positional CSV retains csv.index and uses name as the source-field label; XML retains attribute/element kind and namespace settings under xml. These are genuine selector differences, not redundant source-name overrides.

JSONL reuses JSON mapping semantics and the json block for JSON-specific options; jsonl contains line framing and optional discriminator routing. A JSONL definition can therefore legitimately contain both blocks without duplicating JSON settings.

Validate block applicability: reject XML/CSV settings on JSON definitions, unknown keys, and conflicting selectors before generation. The explicit `format` selects the reader; block presence does not infer a format. The documented absent-format CSV default remains a possible migration convenience, not an excuse to accept ambiguous configuration.

Group existing CSV dialect settings under `csv` as part of the planned configuration migration where that yields the cleaner contract. Provide a before/after example rather than indefinitely supporting both flat and grouped spellings.

### Mapping rules

- Define reusable record types separately from occurrences: an occurrence chooses a property/element, its target type, and its cardinality. Reject cyclic type definitions in the first implementation rather than enabling unbounded recursive models.
- Support nullable scalar properties, nested objects, collections of objects, and collections of supported scalars. Enforce per-parent occurrence constraints plus numeric maximum collection sizes.
- Preserve missing/null/empty distinctions during mapping and validation. Ordinary nullable output properties may collapse missing and null when the definition does not distinguish them; when presence matters, enforce it while source presence is still known. Do not introduce `Optional<T>` into every generated wire model.
- Default JSON to case-sensitive property matching, native token types, no comments/trailing commas, and rejection of duplicate member names. Numeric strings and other coercions require explicit configuration.
- Preserve document string content by default. String trimming, blank-to-null conversion, date patterns, and other normalization are explicit choices.
- Reuse the current scalar catalog and add DateTimeOffset across runtime, generation, and validation as specified in Phase 0. Give document dates documented invariant defaults and allow exact-format overrides; preserve offsets rather than silently losing them.
- Provide explicit unknown-member policies. Recommend strict rejection for declared unit contracts, with an opt-in ignore policy for evolving partner feeds. Envelope sections may be ignored only under a declared policy and must still be syntactically consumed and budgeted.
- Map known fields with structured conversion diagnostics rather than aborting mapping on the first serializer exception. Reuse typed serializers only where their behavior meets the mapping contract.
- Nested validator messages identify the complete source/unit path, including collection indices. A failing child rejects the owning unit; do not independently execute its valid siblings.
- Keep trusted execution identity separate from input metadata. Existing reference-data initialization and application-layer rechecking requirements remain.

## Envelope metadata and reconciliation

Use one shared metadata collection for CSV, JSONL, JSON, and XML. Header/Trailer are ordinary logical metadata names, not separate competing slots. CSV structure.header/trailer migrate to metadata entries; column-name headers remain unrelated parser settings. CSV/JSONL metadata is optional and disabled by default; referenced records declare matches, metadata entries declare optional First/Last boundary positions, and required means exactly one rather than zero-or-one.

JSONL discriminator routing is part of Phase 2: a declared direct member identifies metadata versus business-unit records, with exact unique matches, duplicate/required checks, and optional First/Last ordering. Ordinary JSONL still defaults to one business object per line. Nested business relationships stay inside each JSON object, not cross-line assembly. Existing executable CSV First/Last routes are never silently reclassified as metadata.

JSON/XML select metadata by path; calling it Header/Trailer does not impose document order. XML default scalar selection is an unnamespaced attribute; examples using elements must explicitly select Element and the appropriate namespace.

Support explicitly declared, bounded metadata before and after the repeated unit collection. Metadata is not another business unit unless the definition says so. Typical uses are batch identity, declared counts, totals, and partner/version markers.

JSON member order must not become an accidental validity rule. Metadata encountered after units is available only after it has been read. Earlier report-only callbacks cannot rely on it. If custom unit validation requires metadata that occurs later, reject that processing definition/order requirement explicitly or require the producer to supply the necessary data earlier; do not introduce hidden buffering of all units or a surprise third pass.

Maintain fresh pass-local metadata and reconciliation accumulators. After complete validation and EOF reconciliation, freeze only explicitly declared, bounded metadata into a dedicated immutable accepted-file view. Make that complete view available from the beginning of ValidateThenExecute replay, including metadata occurring after units in the source; ordinary pass-local metadata is still reconstructed in source order. ValidateAndExecute handlers see only encountered metadata, never a premature accepted-file snapshot. Do not copy mutable validation contexts or arbitrary accumulated state into replay. This explicit accepted-metadata contract was selected by the user during Phase 0.

Consume and validate the entire envelope, including trailing content, duplicate selected members, and closing syntax. A valid selected array followed by garbage is not an accepted file.

## Limits, recovery, and diagnostics

| Concern | Planned behavior |
|---|---|
| Input size | Positive maximum source bytes per pass, including ignored sections and envelopes. |
| Unit size | Maximum unit bytes enforced before or during buffering, not after subtree allocation. |
| JSONL framing | Bounded line length; accept LF/CRLF and a final line without a newline. Reject blank lines by default; any lenient mode is explicit. No physical multi-line objects. |
| Depth and cardinality | Explicit nesting depth, child-count, and per-unit work bounds. Parser defaults alone are not the complete resource policy. |
| Oversized units | Terminal incomplete validation initially; no promise to scan unbounded content looking for a recovery point. |
| JSONL content errors | Continue at the next trusted line boundary under CollectErrors when framing and budgets permit. |
| JSON/XML syntax errors | Terminal incomplete validation when reliable traversal cannot resume. Do not guess the next object/element boundary. |
| Mapping/validation errors | Collect within a bounded, well-formed unit and continue to later units under CollectErrors. |
| Replay | Reconstruct the same unit sequence under the same budgets; invariant failures propagate, never masquerade as ordinary successful execution. |
| Coordinates | Logical path plus unit ordinal; physical line/column or byte offsets only where accurately known. Convert parser zero-based coordinates deliberately. |
| Sensitive content | Stable codes and declared paths without raw input values or parser exception text leaking payloads by default. |
| Operational faults | Cancellation, source I/O, missing dependencies, and callback failures propagate; do not classify them as malformed content. |

JSON/JSONL initially use strict UTF-8. Define the BOM policy explicitly: reject it for strict JSONL; permit or reject a matching UTF-8 BOM for JSON according to a documented option, never infer another encoding. CSV retains its explicit encoding contract.

XML should use a separate, documented encoding contract that honors permitted XML declarations/BOMs while rejecting contradictions and invalid bytes. Disable DTD processing and external resolvers; impose depth and document/unit budgets. Match namespace URI plus local name, support attributes and `xsi:nil` explicitly, and defer mixed-content mapping. XML element ordering is enforced only when declared, unlike CSV's required contiguous record ordering.

Limits are input/work safeguards, not precise managed-heap ceilings. Prove bounded behavior using adversarial fixtures and measured allocation trends; do not advertise a fixed memory ceiling based only on input bytes.

## Dependencies and packaging

Use `System.Text.Json` for JSON parsing/mapping primitives, bounded line framing plus the same JSON mapper for JSONL, and `System.Xml.XmlReader` with optional bounded `XElement` subtrees for XML. No Newtonsoft.Json, JSONPath, XPath, or XML helper package is justified by the initial scope.

`JsonSerializer.DeserializeAsyncEnumerable` is useful for simple root arrays but is not the complete architecture: it does not by itself provide selected envelope traversal, strict JSONL line framing, or multi-field conversion diagnostics. Likewise, whole-document `JsonDocument`/`XDocument` loading is not the streaming strategy.

Keep the initial implementation in CoreEx.IO with format namespaces, and keep CodeGen design-time-only. Do not split out format NuGet packages merely because more namespaces exist. Existing CsvHelper remains a dependency of the combined package; separating CSV into its own package can be reconsidered if dependency isolation becomes a concrete consumer requirement.

### Target-framework policy

Do not constrain the new design to the oldest target framework or maintain inferior fallback implementations solely to preserve it. Check whether a required API is supplied by the target runtime or by a supported Microsoft package version; a newer System.Text.Json package can sometimes provide functionality on an older runtime, but this must be verified rather than assumed.

If a newer API materially improves correctness, resource enforcement, maintainability, or the public contract and cannot be supplied cleanly to older targets, prefer raising the minimum target framework for the affected package over carrying conditional feature gaps, compatibility shims, or parallel parser implementations. .NET 10 is the preferred baseline in that case, not an unconditional repository-wide retargeting decision.

Microsoft currently lists November 10, 2026 as the end of support for both .NET 8 and .NET 9. Their remaining support lifetime is not a good reason to bake a lasting constraint into newly designed functionality. See the [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

Make the decision at package level: record the concrete API/benefit, supported target frameworks, dependency compatibility, and consumer migration. Check CoreEx.IO's consumers, generated-code requirements, and CoreEx.CodeGen's design-time hosting needs separately; changing the IO runtime floor does not automatically require changing every CoreEx package. Older applications may remain on the previous compatible package release, but do not imply that this extends Microsoft's runtime support or promises a maintained CoreEx backport branch.

A framework-driven package split is justified only if it preserves a useful, coherent older-runtime capability without duplicating orchestration or fragmenting the normal consumer experience. Do not split packages or drop targets speculatively: select the cleanest supported implementation once the actual API requirements are known.

## Implementation phases and exit gates

### Phase 0 - Settle the contract with representative fixtures

Write the API/configuration sketch and migration inventory before changing runtime behavior. Define terminology for source records, nested occurrences, business units, metadata, counters, paths, and recovery. Finalize the supported scalar/presence policies, grouped format-specific YAML settings, and small default budgets with explicit rationale. Evaluate the required runtime/package APIs and choose the affected packages' framework floors before committing to compatibility work.

Use a representative order feed in CSV, JSONL, root-array JSON, enveloped JSON, and a future XML equivalent. Include nested addresses reused at different paths, line collections, scalar collections, metadata after units, invalid child values, and empty input cases.

**Exit gate:** each fixture has a clear unit boundary, expected diagnostics/counters, acceptance outcome, and handler behavior; all intentional breaking changes have a before/after example.

### Phase 1 - Extract the shared runtime using CSV

Move lifecycle/scoping/replay/accounting into the shared coordinator. Add ValidateAndExecute with diagnostics, partial-execution outcomes, and business-failure reporting. Extract bounded input mechanics and make option ownership explicit. Establish unit provenance and format-neutral instrumentation. Unify metadata declarations and migrate CSV header/trailer configuration while preserving explicit executable route behavior.

Make necessary public API breaks together instead of sprinkling compatibility shims through later phases. Update generated CSV code, current consumers/examples, runtime and generation guides, and migration notes in the same change.

**Exit gate:** existing CSV behavior fixtures remain equivalent except documented migrations/admission changes; all four processing modes, partial-write error outcomes, scoped lifetime/context behavior, faults, cancellation, EOF failure, limits, and replay invariants pass through the shared implementation.

### Phase 2 - JSONL vertical slice and nested CodeGen

Implement bounded UTF-8 line framing and object-unit parsing, including opt-in discriminator-based metadata routing in the first JSONL delivery. Introduce the shared JSON unit mapper, nested model/occurrence definitions, validators, processor generation, and handler starters. Deliver runtime APIs and File CodeGen together, not a runtime feature consumers must hand-wire for months.

Prove recoverable bad-line behavior, nested diagnostics/cardinality, strict token conversion, duplicate detection, bounded lines/units, and deterministic replay. Include a realistic integration example using application-owned execution and immutable source identity.

**Exit gate:** a generated JSONL integration supports all four modes, plain and metadata-routed input, valid lines after recoverable errors, zero execution on ValidateThenExecute rejection, and accurately reported partial execution for ValidateAndExecute.

### Phase 3 - JSON document streaming

Reuse JSONL's JSON unit mapping and nested generation. Add root-array, explicitly selected envelope-array, and single-object modes. Add bounded envelope metadata and EOF reconciliation. Ensure skipped sections are still syntax-checked, depth-limited, and input-budgeted.

Pay particular attention to partial stream reads, tokens split across buffers, huge tokens/subtrees, late envelope errors, duplicate selected properties, member ordering, malformed closing syntax, and cancellation between reads.

**Exit gate:** JSONL and JSON produce equivalent business models/validation for equivalent units; generated document examples work without whole-file materialization or lifecycle forks. Root-array and envelope modes are both production-ready.

### Phase 4 - Consolidate and release JSON support

Review the public API/configuration with both CSV and JSON implementations in place. Remove provisional abstractions that do not earn their keep. Finalize migration guidance, package descriptions, schemas, docs, AI workflow guidance, and the release notes.

Test generated artifacts by compiling/running them, not only comparing template text. Update the CoreEx.Template AI/docs bundle and its validation scenarios where guidance or packaged schema/docs change. Include examples demonstrating report-only's possible late file rejection and application-owned deduplication.

**Exit gate:** supported frameworks build and relevant runtime/CodeGen tests pass; generated artifacts compile; the shipped guidance reflects all formats; memory/work-bound measurements support the documented claims.

JSON support can ship here without waiting for XML.

### Phase 5 - XML, lower priority

Implement forward-only namespace-aware element selection and bounded unit subtree mapping. Add element/attribute/nil handling, nested cardinality validation, encoding policy, secure parser settings, XML-specific diagnostics, and matching CodeGen configuration/templates.

Reuse the shared lifecycle and nested type/validation definitions, not JSON-specific member or token semantics. Prove repeated-element and single-root modes against the same conceptual integration fixtures.

**Exit gate:** generated XML integration passes lifecycle/replay parity, namespace/attribute/nil tests, malformed-document tests, and DTD/external-resource rejection tests without any network/file entity resolution.

## Validation strategy

Expand `tests/CoreEx.IO.Test.Unit` and `tests/CoreEx.CodeGen.Test.Unit`. Retain existing CSV regression fixtures and add shared lifecycle contract tests exercised by each reader. Cover nested structure/mapping separately from application handler behavior.

Use non-seekable streams with tiny/irregular read chunks to exercise streaming correctness; immutable two-open factories to prove replay; deliberately changing factories to exercise detectable invariant failures without pretending the framework can prove source immutability; and faults/cancellation in reads, diagnostics, reports, validation, and execution.

Assert exact counters, emitted diagnostic codes/paths, scope/context isolation, report-only outcomes, zero execution on ValidateThenExecute rejection, explicit partial execution on ValidateAndExecute content failure, and stop-on-execution-failure reporting. Test limits at the boundary, one beyond, and across split buffers. No full diagnostic history should be required by tests or runtime.

Measure large-file working-set/allocation trends with fixed unit size and increasing unit count, and with increasing unit size up to the configured bound. Capture results without flaky absolute-memory assertions in ordinary unit tests.

Compile generated output for each supported shape, test regeneration/unchanged-output checks and handwritten file preservation, and validate configuration/schema agreement, including format-block applicability and unprefixed date `format`. Use the repository's net10.0 Debug inner loop; run framework-sensitive API/dependency checks against each affected package's deliberately selected targets. Update the release matrix and consumer compatibility checks if a package floor changes rather than retaining obsolete targets merely to keep the old matrix green.

## Other formats: deliberately narrow scope

| Candidate | Recommendation | Reason |
|---|---|---|
| TSV, pipe-delimited, other delimiters | Already covered by CSV dialect configuration; document examples. | These are not new parser engines. |
| Fixed-width text | Best candidate after JSON, if partner feeds justify it; not part of this implementation. | Common in operational/legacy integrations and fits bounded record mapping. Needs byte-versus-character positions, padding, encoding, and overflow rules designed explicitly. |
| XLSX | Demand-led, separate future capability/package if needed. | Valuable for human-upload workflows, but workbook semantics, ZIP/XML expansion, formulas, dates, merged cells, and a library/license decision are substantial scope. Never evaluate formulas/macros. |
| Plain line-oriented text | Defer unless a real workflow needs it. | A small framing/adapter capability may be enough; do not build a grammar language. |
| Gzip/ZIP inputs | Treat as explicit caller-owned input composition, not logical file formats. | Decompression must be deterministic and bounded in compressed and expanded bytes; archives also require entry-selection and safety policies. |
| EDI (X12/EDIFACT) | Use a specialist parser/adapter when required. | Standards, envelopes, validation, and partner-specific guides are a separate domain. |
| Parquet, Avro, ORC, Arrow | Out of scope absent a concrete operational integration case. | These pull toward analytical/bulk data tooling already served by data platforms. |
| YAML | Do not prioritize as inbound business data. | Alias/tag/schema behavior and dependency surface add complexity without a demonstrated workflow. Existing YAML configuration support is not an ingestion requirement. |
| Binary payloads/documents/images | Keep in transport/application adapters. | They are not naturally typed repeating record feeds for this processor. |

Keep an extension seam capable of a future fixed-width reader, but do not implement or generalize for it speculatively. The prioritized roadmap is shared foundation -> JSONL -> JSON -> consolidation/release -> XML; fixed-width and XLSX require separate demand-led proposals.

## Decisions to preserve throughout implementation

1. A business unit is explicit and bounded; nesting supplies ownership, not transactional or execution semantics.
2. Share orchestration and applicable validation concepts; keep framing, mapping, recovery, and encoding truthful to each format.
3. Break an awkward newly released API when that removes a real constraint; document the migration instead of preserving permanent accidental complexity.
4. No new third-party parser dependency without a demonstrated gap in the built-in libraries.
5. Whole-file acceptance and immutable replay remain the ValidateThenExecute safety boundary. ValidateAndExecute deliberately uses unit acceptance, can fail after writes, and reports that honestly. Application transactions and durable idempotency remain application responsibilities.
6. Deliver runtime, generation, examples, schema, documentation, and shipped AI guidance together for each released capability.
7. Group format-specific YAML settings under their format object; keep clear shared concepts such as a date property's `format` on their natural owner.
8. Prefer a justified package-level framework floor increase over long-lived design constraints or weaker old-framework implementations.
9. Declare field source names first, derive PascalCase properties deterministically, and provide only an explicit property override; do not introduce casing configuration.

## Implementation evidence

The plan is grounded in the current `src/CoreEx.IO/FileProcessor.cs`, `FileUnitHandler.cs`, `FileLocation.cs`, `FileUnitInfo.cs`, `FileProcessingProgress.cs`, `Csv/CsvFileProcessorBase.cs`, `Csv/CsvReadStream.cs`, `Csv/CsvFileOptions.cs`, `Csv/CsvHierarchyDefinition.cs`, `Csv/CsvHierarchyNode.cs`, and `Csv/CsvFieldConverters.cs`; and `src/CoreEx.CodeGen/Files/Config/{CodeGenConfig,RecordConfig,PropertyConfig,NodeConfig}.cs`, `Files/Templates/FileProcessor_cs.hbs`, and `Files/README.md`.

Platform references: [System.Text.Json streaming deserialization](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonserializer.deserializeasyncenumerable?view=net-10.0) and [streaming XML processing](https://learn.microsoft.com/en-us/dotnet/standard/linq/perform-streaming-transform-large-xml-documents). Convenience APIs are implementation options, not substitutes for the processing contract.
