# CoreEx.IO multi-format: Phase 0 contract

## User-directed simplification (supersedes the original quota design)

The consumer owns source admission, overall input size, concurrency, timeouts, and report-output policy for CSV and both JSON formats. CoreEx.IO imposes no file/line/token/metadata byte quota, unit/diagnostic count quota, CSV group-record quota, or default collection maximum. JSON retains the built-in parser's configurable MaxDepth (64 by default), business cardinality, and optional explicitly declared positive collection maxItems. A large JSON document, JSONL line, or CSV hierarchy can consume substantial memory; the framework is an application integration helper, not a bulk-data platform. XML proposals must follow the same no-quota policy unless a new concrete requirement is approved. Existing CSV parser-span findings remain provenance evidence, not a reason to implement a byte-preserving CSV tokenizer.

The subsequent approved simplification uses ordinary System.Text.Json metadata snapshots, removes exhaustive JSONL UTF-8/ignored-string validation, uses one invocation-level content-error policy for every format, and consolidates CSV/JSON cardinality into FileCardinality. Dates use standard invariant .NET parsing without a format; an explicit format is exact with no fallback, including bounds/options and scalar collections. DateTimeOffset still requires/preserves an explicit offset; JSON DateTime still rejects offsets. Metadata DTOs use serializer attributes to exclude transient properties. Shared lifecycle, source paths, scopes, business cardinality, and replay guarantees remain.

## Status and scope

Phase 0 specifies the approved implementation contract, representative source files, exact observable scenarios, migration inventory, and framework decision. Phase 1 implemented the shared lifecycle/CSV migration; Phase 2 implemented nested JSONL, opt-in metadata routing, and File CodeGen/compiled consumers; Phase 3 implemented streamed JSON documents, pointer selection, metadata, and generated consumers. CSV, Json, and Jsonl are implemented formats; XML remains a future capability. The original expected.json matrix is a design reference and is not a substitute for the runtime and generated-consumer tests.

Phase 2 implementation notes: JSONL frames actual consumed UTF-8 bytes separately from read-ahead accounting. It uses a single JsonDocument parse per line, with built-in AllowDuplicateProperties = false on .NET 10+ and a small duplicate-name walk on older targets. It does not exhaustively decode ignored string values or perform a separate UTF-8 validation pass. Consumed malformed strings produce localized conversion errors; malformed ignored content can remain unexamined. All formats snapshot declared metadata objects with default System.Text.Json serialization, including serializable handwritten properties; metadata validators do not rerun on replay. The default System.Text.Json round-trip requirement still applies to accepted typed reads. No new dependency or target-framework floor change was needed.

The implementation order is shared foundation, JSONL, JSON, release consolidation, and XML. XML has a shape/behavior target here, not an implementation commitment ahead of JSON. Preserve processing guarantees rather than accidental CSV coupling. Fix a newly exposed constraint at its owner instead of compensating in every consumer.

**User-confirmed decisions:** ValidateThenExecute receives immutable accepted metadata from the beginning of replay. ValidateAndExecute validates and executes units in one pass, with diagnostics and explicit partial-execution outcomes. Valid-unit reporting is an optional awaited hook in all modes, not a processing mode. The modes are ValidateOnly, ValidateThenExecute, and ValidateAndExecute; outcomes are Succeeded, ContentFailed, and ExecutionFailed, independent of mode. CSV/JSONL metadata routing is opt-in; JSONL routing ships with its initial capability. Metadata declarations are shared across formats. YAML defaults minimize boilerplate, including XML field kind Attribute and empty namespace.

## 1. Terminology and accounting

| Term | Contract |
|---|---|
| Source record | A format-defined record boundary, not a physical text line. CSV: each parser data/envelope record, excluding column-name headers and skipped comments/blanks. JSONL: each non-skipped line, even malformed. JSON/XML: each selected unit boundary, including non-object/wrong-shaped selections; envelope metadata and nested children are not separate records. |
| Business unit | The declared input to one handler. CSV flat/routed: one row. CSV hierarchy: one root and its descendants. JSON/XML: one selected object/element and its descendants. |
| Unit ordinal | One-based encounter order of candidate business units, including invalid candidates. Replay preserves ordinals, not merely the sequence number of successfully validated units. |
| Occurrence | A reusable wire type mapped at a particular source location, with its own owner/name/cardinality. BillingAddress and ShippingAddress can reference the same Address type. |
| Source path | Format-native location: JSON Pointer for JSON, unit-relative JSON Pointer plus physical line for JSONL, and expanded-name XML path later. No path is interpreted as a physical coordinate. |
| Property path | Generated model location, such as `Lines[0].Quantity`. Keep it distinct from `/orders/0/Lines/0/Quantity`. |
| Metadata | Explicitly declared, bounded file data that is not independently executed. Authentication and source identity never come from it. |
| Accepted file | Entire syntax, structure, mapping, unit validation, scoped validation, metadata, and EOF reconciliation completed without errors. |

Keep `RecordsRead` and `InvalidRecords`, correcting their physical-record documentation to source-record semantics. Add `UnitsRead` and `InvalidUnits` so rejected groups are not confused with bad CSV rows. Do not remove useful CSV counters just to achieve a superficially uniform model.

Increment RecordsRead and UnitsRead when their boundaries are recognized, even if the candidate later fails. A CSV hierarchy root increments UnitsRead; its descendants increment RecordsRead, not UnitsRead. Unknown/orphan CSV records increment RecordsRead and possibly InvalidRecords without inventing a unit.

InvalidRecords counts each source record with a localized parsing/mapping/field/structural error once. InvalidUnits counts each candidate that fails mapping, structure, pure validation, or scoped validation once. A CSV group-level error can invalidate a unit without attributing failure to a particular row. A nested JSON field error invalidates its selected source object and its owning unit once, regardless of diagnostic count. File/envelope/EOF errors do not retroactively invalidate otherwise valid units.

Shared invariants are nonnegative counters, InvalidRecords <= RecordsRead, InvalidUnits <= UnitsRead, UnitsValidated + InvalidUnits <= UnitsRead, UnitsAttempted <= UnitsValidated, and UnitsExecuted <= UnitsAttempted. Do not tie validated units to valid source-record counts; these are different domains. A successful completed file requires UnitsValidated = UnitsRead and no invalid units/records/errors. A completed content-failed CSV file can include orphan records outside every unit. A terminal document error inside an already recognized unit marks that unit/record invalid; an error between units does not.

Counters describe validation only except for attempted/executed/report-hook counts. Replay does not increment source/unit-read counters. Recognized metadata and route occurrence counts remain separate typed-context information. BytesRead counts source bytes actually read, including parser read-ahead; do not pretend it is the byte position of the current unit.

## 2. Lifecycle contract

Retain `ProcessAsync(Func<CancellationToken, Task<Stream>>, FileProcessingContext, CancellationToken)`, stream ownership, original execution Result preservation, and awaited reporting hooks. Support three processing modes: ValidateOnly (default), ValidateThenExecute, and ValidateAndExecute. The optional `OnValidatedUnitAsync` observation/reporting hook runs for every individually valid unit regardless of mode. Only ValidateThenExecute opens replay and requires identical immutable bytes on both opens. Single-pass modes open once; retry-safe source identity remains caller-owned. A version label, count equality, or content hash computed too late cannot make already executed writes safe against a mutable source.

Validation proceeds as syntax/framing, unit mapping/structural checks, pure nested validation, fresh scoped custom validation, and optional unit observation/reporting. Mapping/structure failure suppresses downstream validators for that unit; pure validation failure suppresses scoped validation. The optional hook is awaited before the next unit is read; it is application-owned side effect, not a diagnostic and has no separate progress counter. Custom validation remains read-only. A later content error may return ContentFailed after earlier valid units were reported.

At EOF, finish metadata checks and perform file reconciliation. If any errors exist, never open replay; ValidateAndExecute may already have written valid units and must preserve that fact. In FailFast mode stop at the first content error and mark validation incomplete when checks remain; in CollectErrors mode reach EOF when syntax permits. Reconciliation may emit file errors even after every unit passed.

For ValidateThenExecute, freeze the accepted-metadata snapshot after acceptance, create a fresh context/mappings/reader, reopen immutable input, reconstruct each unit, and execute in a fresh scope. Do not rerun pure/scoped validators. Stop at the first failed business Result; earlier successful units are not rolled back. Operational faults and cancellation propagate, preserve partial progress, and do not call rejection/completion hooks.

An accepted empty repeated stream invokes zero unit hooks. To preserve the existing contract, ValidateThenExecute still requires a handler factory and performs its second open for accepted empty input. Unimplemented execution/report hooks fail explicitly when reached, not with success-shaped defaults.

### Single-pass ValidateAndExecute

Parse/assemble a complete bounded unit, map it, apply pure and scoped validation, then execute it in a separate fresh scope before processing the next unit. CSV hierarchy waits until the root group closes. Do not run execution in the validation scope or use the valid-unit observation/reporting hook to perform business writes.

Emit and await normal content diagnostics in this mode too. Invalid units never execute. FailFast stops on the first content error; CollectErrors skips invalid units and continues only where framing/parser state is trustworthy. EOF metadata/reconciliation still runs when traversal completes. Later errors never erase prior execution counts or trigger automatic rollback.

AcceptedMetadata is unavailable during single-pass execution; only encountered validated pass-local metadata is available. If a handler requires a trailer or complete accepted-file metadata, select ValidateThenExecute. After successful EOF reconciliation, every mode exposes the frozen snapshot to successful completion, including an explicit empty accepted view when no metadata is declared; never expose it retroactively to earlier unit hooks.

Return ContentFailed for content failures in any mode, including incomplete parsing with prior successful reports or single-pass writes. It requires content errors, preserves all partial progress, and carries no failed business Result. Return Succeeded only after error-free completed traversal/reconciliation and, when execution was requested, every validated unit executed. Do not make outcome names encode whether a report or execution action occurred.

Both execution modes use ExecutionFailed when a handler returns a failed Result. Add an awaited protected OnExecutionFailedAsync hook receiving context, FileUnitInfo, and the original Result before returning the final summary. Stop on the first returned business failure regardless of errorMode; preserve any earlier content diagnostics and the identical failed Result. ValidationCompleted reflects whether EOF validation actually finished, not whether a unit executed. ExecutionFailed therefore permits incomplete validation and preceding content errors in the single-pass mode, while the two-pass mode still requires completed error-free acceptance.

Replace OnRejectedAsync with OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken), the awaited finalizer for ContentFailed regardless of partial reporting or execution. The neutral name deliberately avoids implying zero writes. OnCompletedAsync is successful finalization only. Business failure uses OnExecutionFailedAsync, not OnContentFailedAsync. Operational exceptions/cancellation propagate without either content/business-failure finalizer; reporting-hook faults propagate rather than replacing an operational failure with a content outcome.

Diagnostic events stream through OnDiagnosticAsync in every mode, without a framework count cap or retained history. Business failures travel through the original Result hook without being recast as malformed input. Consumer code owns reporting policy.

## 3. Shared runtime and public API direction

Extract `FileProcessor<TUnit> : FileProcessor` as the owner of lifecycle, scopes, progress, reporting, and replay. Existing CSV processors derive from it; new JsonLinesFileProcessor<TUnit>, JsonFileProcessor<TUnit>, and later XmlFileProcessor<TUnit> use the same implementation. Names are the Phase 1 implementation target, not a demand to duplicate existing code under a new name.

Remove `FileProcessingOptions` rather than keeping an empty options shell. `CsvFileOptions`, `JsonFileOptions`, `JsonLinesFileOptions`, and later `XmlFileOptions` own only their format policies. CSV dialect/header/ordered-assembly settings remain CSV-specific. JSONL composes JSON mapping options plus optional line routing rather than inheriting document-envelope path options that make no sense for lines.

Keep built-in readers and their mutable pass/accounting machinery internal. This release does not promise a public third-party parser plug-in SPI. Public customization is generated or direct runtime mappings, derived built-in processors with protected hooks, validators, and scoped handlers. Another reader can be added later without exposing ProcessingState or making CSV's private-protected parser method the universal API.

Preserve named `CsvFileProcessorBase<TUnit>` only if it still contains useful shared CSV work. Do not keep it as a forwarding compatibility shell after a breaking base extraction.

Extend `FileUnitInfo` with required Ordinal, record/root type, source location, and optional source byte offset. Extend `FileLocation` with a separate SourcePath while retaining PropertyPath for model paths. An empty SourcePath is a valid root JSON Pointer, unlike an invalid whitespace-only PropertyPath. A missing source member can have an intended source path with no physical coordinates. Unknown input keys must not be echoed into diagnostics by default; anchor an unknown-member error to the declared parent object instead.

Thread FileUnitInfo through validation, execution, trusted scope configuration, and reporting consistently. The intended handler override signatures are:

```csharp
protected override Task OnValidateAsync(FileUnitValidationArgs<TUnit> args, CancellationToken cancellationToken);

protected override Task<Result> OnExecuteAsync(FileUnitArgs<TUnit> args, CancellationToken cancellationToken);
```

Use focused argument objects for multi-parameter extension hooks, with get-only properties and framework-controlled construction; cancellation stays separate and last. FileValidationArgs carries Context and the awaited ReportDiagnosticAsync sink; FileDiagnosticArgs carries Context and Diagnostic; FileSummaryArgs carries Context and Summary for completion/content failure; FileExecutionFailedArgs carries Context, Info, and the original Result. FileUnitArgs<TUnit> carries Unit, Info, and Context for execution/observation; FileUnitValidationArgs<TUnit> adds the diagnostic sink for validation; FileUnitContextArgs<TUnit> carries Unit, Info, Context, Parent, and UnitContext for trusted scope configuration. Referenced units and pass-local contexts remain mutable. Generated unordered dispatch narrows the same unit via As<TRecord>(), preserving provenance/context/sink; custom context members are accessed by casting Context. Keep the simple OnCreateExecutionContext(FileProcessingContext) hook unwrapped. Update generated routed dispatch and handwritten handlers together. Add optional unit ordinal to FileDiagnostic so a diagnostic sink can correlate a malformed unit without access to a model.

FileProcessor<TUnit> owns protected Services, exposing the provider supplied at construction. Its lifetime follows registration, not an automatic invocation or unit scope; resolve business dependencies through the fresh scoped handler factory. DI remains optional for pure validation, but accessing Services without a supplied provider throws InvalidOperationException. Generated processors do not duplicate or shadow this property.

Introduce neutral `CoreEx.IO` tracing and `file.*` lifecycle metrics/tags. Retain `csv.*` codes for CSV syntax/mapping/structure checks; use `json.*`, `jsonl.*`, and later `xml.*` for format content checks. Shared lifecycle, limits, and typed validation use `file.*`. Telemetry names changing is an intentional migration, not an indefinite double-emission policy.

## 4. Accepted metadata contract

Generated context exposes typed pass-local metadata access plus a distinct read-only AcceptedMetadata view. A handler can distinguish unavailable from optional/missing metadata without testing incidental default values. The shared runtime carries the view without knowing generated metadata types; a generated context supplies typed access.

AcceptedMetadata is absent during unit validation and valid-unit callbacks, and during ValidateAndExecute unit execution. Build it only after successful EOF reconciliation and expose it to accepted completion and ValidateThenExecute replay. Only replay execution handlers receive the complete accepted view independent of source order. The ordinary pass-local metadata view starts empty on replay and is reconstructed as encountered.

Only explicitly declared metadata objects are eligible. Snapshot their serializable properties using default System.Text.Json behavior, including handwritten DTO properties; use JsonIgnore for transient properties. The accepted snapshot must be deeply immutable, not merely a mutable record with init-only collection references. Keep services, authentication, counters, tracked entities, accumulated diagnostics, and arbitrary context state outside metadata DTOs. Repeated units cannot become metadata through a selector. The snapshot is source data, not a business idempotency key.

Phase 1 baseline: the immutable accepted view uses `IReadOnlyDictionary<string, JsonElement>` for every source format rather than a separate immutable metadata DTO hierarchy. Wire models remain mutable; default `System.Text.Json` serialization freezes their values, and typed accessors deserialize a fresh copy on every read. Metadata types must round-trip with those defaults and their serializer attributes. This isolates accepted values without another public abstraction; callers should reuse a local typed copy when reading it repeatedly.

Metadata mapping is performed as encountered, with declared field validation before it can be accepted. Reconciliation consumes the final metadata. Checks needing late metadata belong in EOF reconciliation and maintain bounded aggregate state; no per-unit I/O validation dependent on unseen metadata is promised. No hidden third pass or retained order list.

## 5. YAML contract

Use lower-camel-case configuration keys, with format-specific objects (`csv`, `json`, `jsonl`, later `xml`) at the owner that needs them. Keep common singular concepts unprefixed: a property's date `format`, `required`, `type`, and validation rules remain direct property settings. There is no `limits` block. No `csvDelimiter`/`jsonUnitPath` families.

`format` selects Csv, Jsonl, Json, or later Xml. An omitted definition-level format continues to mean Csv during the initial migration. Blocks never infer the format. Reject unknown keys, inapplicable blocks, unsupported selectors, and old/new duplicate settings before writing any generated files.

### Defaults and required declarations

Every optional setting has one documented, schema-aligned default; examples show only deviations unless explaining a full configuration. Do not invent defaults for identity/version, runtime output project, an ambiguous record reference, a discriminator field, or a required source selector. Infer a unit record only when exactly one eligible business record exists; otherwise require unit.record.

Defaults include mode ValidateOnly, errorMode FailFast, absent metadata = none, required false, scalar type String, repeated unit/collection cardinality Many, JSON kind Array and unitPath empty (root), JSON member matching exact, XML field kind Attribute and namespace empty. Single-object JSON defaults its unit cardinality to One; explicit incompatible cardinality fails. Keep the existing documented CSV dialect/header defaults and consumer-owned source admission.

JSON kind means the expected JsonValueKind of the value selected by unitPath, not necessarily the document root. Permit Array and Object only; the two modes have the selection restrictions below. XML field kind separately means Attribute or Element; use distinct configuration types/enums, not a universal Kind enum. Reject the old shape spelling rather than introducing aliases.

An omitted XML field block is equivalent to xml: { kind: Attribute, namespace: '' }. Element fields specify kind Element; namespaced fields specify the URI/declared namespace mapping explicitly. Empty namespace means no namespace, not wildcard or inherited-default guessing; unprefixed attributes do not inherit XML default namespaces. Document/element selection paths remain namespace-aware. XML collection/container details remain Phase 5 work, not an excuse to override the chosen scalar-field defaults.

### Unified metadata and routed JSONL

Declare all file metadata in a shared metadata collection, with arbitrary logical names and typed record references. CSV no longer owns separate header/trailer metadata declarations under csv.structure; that structure describes only business roots/children. Column-name headers remain parser/header-mapping configuration and are never business metadata.

```yaml
metadata:
- name: Header
  record: BatchHeader
  required: true
  csv:
    position: First
- name: Trailer
  record: BatchTrailer
  required: true
  csv:
    position: Last
```

Referenced CSV records carry csv.match and positional field mappings. Metadata entries allow zero or one occurrence by default, exactly one with required true; duplicates are errors. CSV metadata csv.position defaults Any; First/Last are explicit boundary restrictions excluding skipped comments/blanks and the column-name header. JSON metadata uses json.path and XML uses namespace-aware paths. Logical names Header/Trailer impose no JSON/XML ordering. Any XML ordering constraint must be explicitly configured later.

CSV/JSONL default to ordinary business units without envelope metadata. Existing unordered CSV records with First/Last positions remain executable units unless explicitly moved to metadata. A route cannot simultaneously identify metadata and a business unit.

JSONL discriminator routing is supported in Phase 2, not deferred:

```yaml
format: Jsonl
jsonl:
  discriminator: type
unit:
  record: Order
metadata:
- name: Header
  record: BatchHeader
  required: true
  jsonl:
    position: First
- name: Trailer
  record: BatchTrailer
  required: true
  jsonl:
    position: Last
records:
- name: BatchHeader
  jsonl:
    match: header
  properties:
  - { name: batchId }
- name: Order
  jsonl:
    match: order
  properties:
  - { name: id }
  - { name: quantity, type: Integer }
- name: BatchTrailer
  jsonl:
    match: trailer
  properties:
  - { name: orderCount, type: Integer }
  - { name: quantityTotal, type: Integer }
```

The discriminator is a declared direct JSON string member, matched exactly with unique explicit record matches. It is structural input, excluded from unexpected-member checks and generated DTOs unless explicitly mapped. Missing/unknown/non-string discriminators are recoverable line structure errors under CollectErrors. Route metadata without discriminator configuration is invalid. No automatic type-property guessing, cross-line parent/child reconstruction, or multiple heterogeneous business unit types in this initial routed JSONL capability.

JSONL metadata lines count in RecordsRead and metadata occurrence counts, not UnitsRead/ordinals; order lines remain independent candidate units. Unroutable/malformed lines increment invalid source records but do not invent business units in routed mode. Simple nonrouted JSONL retains one candidate unit per non-skipped line. JSONL metadata position defaults Any; First/Last/required/duplicate semantics match CSV. Matching and unit selection occur before typed validation.

### Document definitions

```yaml
name: Orders
version: '2'
format: Json
runtimeProjectPath: ../Contoso.Integration
mode: ValidateThenExecute
errorMode: CollectErrors
json:
  kind: Array
  unitPath: /orders
  unknownMemberPolicy: Reject
  maxDepth: 64
unit:
  record: Order
  cardinality: OneOrMore
metadata:
- name: Batch
  record: Batch
  required: true
  json:
    path: /batch
records:
- name: Order
  properties:
  - { name: Id, required: true }
  - { name: EffectiveDate, type: DateOnly, required: true }
  - { name: SubmittedAt, type: DateTimeOffset, required: true }
  - { name: BillingAddress, record: Address, required: true }
  - { name: ShippingAddress, record: Address, required: true }
  - name: Lines
    collection:
      record: Line
      cardinality: OneOrMore
      maxItems: 1000
  - name: Tags
    collection:
      type: String
      cardinality: Many
      maxItems: 100
- name: Address
  properties:
  - { name: Code, required: true }
- name: Line
  properties:
  - { name: Sku, required: true }
  - { name: Quantity, type: Integer, required: true, min: 1 }
- name: Batch
  properties:
  - { name: Id, required: true }
  - { name: ExpectedOrders, type: Integer, required: true, min: 0 }
  - { name: ExpectedQuantity, type: Integer, required: true, min: 0 }
```

### Source-first field naming

For entries under `properties`, `name` is the exact source field name. Derive the generated C# property by splitting on underscore/hyphen, PascalCasing each nonempty part, and concatenating, following DbEx's `DbTableSchema.CreateDotNetName` convention. An optional `property` explicitly supplies the generated identifier and wins over derivation. Never transform the source selector. There is no `propertyCasing`, `fieldCasing`, or configurable naming orientation.

```yaml
properties:
- { name: order_id, type: Guid }
- { name: effective-date, type: DateOnly, format: yyyyMMdd }
- { name: customer_ref, property: CustomerReference }
```

These select literal `order_id`, `effective-date`, and `customer_ref`, and generate OrderId, EffectiveDate, and CustomerReference. PascalCase is the generated public-property convention, not a C# language requirement. Explicit `property` values are exact identifiers, not input to another normalization pass.

The source name means a CSV named column, a JSON/JSONL direct member, or an XML element/attribute local name. XML defaults to Attribute with empty namespace; override kind/namespace only when deviating. Its source name alone cannot distinguish an element from an attribute. Positional CSV uses `csv: { index: 2 }`, with `name` serving as the logical source-field label rather than a header selector. No `json.name` or `csv.column` alias is needed.

Record-level `name` remains the logical type name. Metadata-entry `name` remains its logical context accessor; explicit metadata/unit paths remain format-specific selectors. Do not apply property source-name semantics indiscriminately to every name in the schema.

Validate derived and explicit identifiers before generation: reject empty results, illegal C# identifiers, containing/inherited-member conflicts, and destination collisions after normalization. For example, `order_id` and `order-id` cannot both generate OrderId; require an explicit distinct `property`. Do not strip arbitrary punctuation, repair leading digits, append numeric suffixes, or silently change source matching. Pin the DbEx-style helper's behavior for camel case and acronyms with tests before implementation; this is algorithm reuse or equivalent behavior, not a new runtime dependency on DbEx.

Keep `format: yyyyMMdd` alongside the property's type for an exact date override. Diagnostic source paths/field labels use the original name while model paths and validator property expressions use the resolved property.

Use exactly one of scalar `type`, object `record`, or `collection` on a property; omitted type means String only when neither object nor collection is declared. A collection specifies exactly one element `record` or scalar `type`. Generated document models use nullable scalar/object/list properties; valid empty collections stay empty, not null. Document members can appear in any order. Reject cyclic record references and duplicate destination properties/selectors.

`required: true` rejects null/absence while allowing 0/false for scalars. `mandatory` preserves the existing reject-default behavior for scalars and is invalid for object/collection definitions. Optional object absence/null is allowed; present objects always validate their declared fields. Collection cardinality governs absent/null as zero items and enforces count; `required: true` additionally distinguishes absent/null from a present empty collection. Elements are non-null by default; null elements are mapping errors. Nullability overrides for collection elements are deferred.

Many = zero or more, One = exactly one, Optional = zero or one, OneOrMore = one or more. `maxItems` is an additional numeric bound. Metadata entries reference object record types and use required to govern absence; explicit null is rejected for required metadata. Unknown members inside units and declared metadata objects are rejected by default. Undeclared envelope sections are ignored by default but syntax/duplicate/parser-depth checks still apply; repeating or overlapping selected paths is rejected, never interpreted as an implicit join.

Mode-specific configuration:

| Mode | Required selection |
|---|---|
| JSON root array | `format: Json`; `unit: { kind: Array, unitPath: '' }` with `unit.record` naming the element record. Kind, path, and cardinality default to Array, root, and Many. |
| JSON envelope array | `format: Json`; `unit.unitPath: /orders` selects a nested array through literal object members with JSON Pointer escaping. |
| JSON single object | `format: Json`; `unit.kind: Object` selects only the document root and requires unit cardinality One. |
| JSONL | `format: Jsonl`, unit naming the per-line business object type, json holding shared member-mapping settings, and optional jsonl framing/routing settings. JSON document selectors are inapplicable; shared metadata is supported with explicit discriminator routing. |
| XML later | Explicit repeated-element/root selection under `xml`, namespace URI mapping, and element/attribute property selectors; finalized after JSON rather than inventing a full XML schema now. |

Array index/wildcard/filter/recursive-descent selector traversal is unsupported. Unit-relative mappings name a direct member; represent nested paths with reusable nested records, not arbitrary leaf queries. Metadata selectors are disjoint from unit paths and from each other; array traversal is prohibited. JSON duplicate object member names are errors even in skipped sections, with duplicate-name checks.

### CSV migration

Before:

```yaml
delimiter: '|'
encoding: Utf8
records:
- name: Row
  properties:
  - { name: EffectiveDate, property: Date, type: DateOnly, format: yyyyMMdd }
```

After, keeping the surrounding identity/path fields unchanged:

```yaml
format: Csv
csv:
  delimiter: '|'
  encoding: Utf8
records:
- name: Row
  properties:
  - name: EffectiveDate
    property: Date
    type: DateOnly
    format: yyyyMMdd
```

Move delimiter, quote, comment, encoding, headerPolicy, discriminatorIndex, and business-root structure under definition-level csv; move record route match, executable position, and unordered cardinality under record-level csv. Migrate old structure.header/trailer into shared metadata entries, keeping route matches on their record types and explicit metadata First/Last positions. Remove record-level errorMode; the invocation's policy applies uniformly. Replace CsvRecordCardinality with shared FileCardinality. Migrate a named CSV property's old column into its new name and preserve its old generated name with property when necessary; remove column. Move positional index under property-level csv, retaining the source label and using property when derivation would otherwise change an existing generated identifier. Structure nodes remain CSV business occurrences with their existing cardinality/order semantics. Do not convert CSV assembled groups into document DTOs in this phase.

No automatic flat/grouped dual spelling: provide precise migration diagnostics and a documented mechanical rewrite. Generated output is regenerated, not edited. Handwritten handler and processor partial signatures are migrated explicitly.

## 6. Conversion and presence contract

JSON native tokens are required: numbers for numeric types, booleans for Boolean, and strings for text/Guid/date types. Numeric strings, fractional integers, overflow, non-finite doubles, and wrong token types reject mapping. Do not add general coercion options in the first version without a concrete integration case.

Preserve empty and whitespace strings. Missing/null scalar values map to null unless required/mandatory rejects them. Ordinary wire DTOs do not get Optional wrappers. Reject duplicate members before mapping can silently choose a winner. Preserve JSON numeric precision with typed TryGet conversions, not an intermediate Double.

Include DateTimeOffset as a logical scalar in the runtime and File generator, including bounds/options validation and CSV conversion support. JSON integrations commonly carry offsets; deferring it would create an avoidable limitation. Preserve the represented offset; do not silently convert to local machine time.

CSV and JSON DateOnly, DateTime, and DateTimeOffset use standard invariant .NET parsing when format is omitted. An explicit format uses exact parsing without fallback; the same policy applies to date bounds/options and scalar collection elements. DateTimeOffset requires/preserves a trailing Z or numeric offset rather than assuming machine-local time. JSON DateTime rejects offset-bearing values and remains unspecified; use DateTimeOffset for instants. No custom default ISO grammar, required seconds, or fractional-digit policy is imposed beyond the chosen .NET parser.

System.Text.Json owns object construction and conversion. Report its first deserialization error per unit in encounter order, without a second required error for the same property; do not rebuild exhaustive mapping-error collection through converters. Pure validation runs only after successful deserialization and can still emit multiple ordinary messages. CollectErrors continues to subsequent units where framing permits. Collection bounds and null reference-type item checks are generated validator rules after deserialization, producing file.validation rather than pre-allocation mapping/structure errors.

Shared codes include `file.validation`, `file.structure.cardinality`, `file.metadata.required`, `file.reconciliation.count`, and `file.reconciliation.quantity`. Format codes include `json.syntax.invalid`, `json.unit.selection`, `json.metadata.type`, `json.field.type`, `json.member.unexpected`, `json.unit.type`, `jsonl.record.invalid`, and `jsonl.line.blank`. JSONL syntax, duplicate-member, and depth failures use the same invalid-line diagnostic; JSON document syntax/duplicate failures use `json.syntax.invalid` and cannot recover at a record boundary. Existing CSV mapping codes remain except shared validation that deliberately migrates to file.*. Missing metadata may be diagnosed at EOF with a logical path but no fabricated line.

The reconciliation codes in the fixtures belong to the example's handwritten reconciliation hook, not built-in order/count knowledge in CoreEx.IO.

## 7. Input ownership, framing, and recovery

The consumer owns source admission, overall input size, concurrency, timeouts, and report-output policy for CSV and both JSON formats. CoreEx.IO imposes no file/line/token/metadata byte quota, unit/diagnostic count quota, CSV group-record quota, or default collection maximum. JSON retains the built-in parser's configurable MaxDepth (64 by default), business cardinality, and optional explicitly declared positive collection maxItems. A large JSON document, JSONL line, or CSV hierarchy can consume substantial memory; the framework is an application integration helper, not a bulk-data platform.

JSONL accepts LF/CRLF and a final nonempty unterminated line, without a phantom record after a final separator. Input is parsed as UTF-8 without encoding autodetection. A UTF-8 BOM produces terminal jsonl.encoding.bom; other encoding signatures fail ordinary JSON parsing. Well-framed syntax/depth and consumed-string conversion errors recover under CollectErrors. Malformed UTF-8 or escaped surrogates in consumed strings produce localized conversion errors; ignored string values are not exhaustively decoded. Blank lines reject by default; explicit Skip excludes them from record/unit counts but not physical lines or bytes read. JSON documents are also UTF-8 without encoding autodetection; parser, duplicate-member, and depth errors stop traversal, including errors in skipped sections.

Each JSONL line is parsed once using JsonDocument, with configured json.maxDepth (64 by default), then deserialized by JsonSerializer. Duplicate members reject even in ignored content; ignored string values are not exhaustively decoded. Syntax and nesting-depth errors can recover at the next line under CollectErrors. JSON documents use an incremental Utf8JsonReader for selection, provenance, and duplicate checks, exposing each selected object's original bytes as a non-owning stream to JsonSerializer.DeserializeAsync<T>. There is no framework selected-object capture, per-object DOM, or custom token-to-model engine. Standard JsonConverter<T> extensions retain scalar date/finite-number policies. The typed graph, metadata, duplicate-name sets, reader/serializer buffers, and property-specific scalar-collection converter buffers still consume memory. Structural/parser errors prevent execution of the affected object; malformed syntax terminates traversal because there is no record boundary for recovery.

No limits YAML block or FileProcessingOptions remains. No default collection maxItems is supplied. Optional positive maxItems is a business constraint checked by the generated validator after successful deserialization and collection allocation. Conversion failures suppress validation; there is no cardinality-over-conversion precedence or allocation bound. Very large object graphs are outside this helper's intended workload, and admission/concurrency remain consumer-owned. Source/callback faults and cancellation propagate, preserve partial progress, and do not invent a content-limit diagnostic or finalizer.

## 8. Representative fixtures and exact expectations

The original expected.json matrix below remains specifically the ValidateThenExecute baseline. The additional execution-expectations.json contains eight explicit single-pass/routed scenarios. It expects one valid execution after an invalid first group under CollectErrors; zero calls under FailFast; two prior executions on late-total/trailing-syntax failure; and an original-Result business failure on unit two. routed.jsonl has four source records, two units, explicit Header/Trailer metadata, and equal order/quantity totals. Two-pass handlers see both accepted metadata objects; single-pass handlers see the Header but not the later Trailer. The fixture inspector verifies data and expectation consistency, not future runtime execution.

The accompanying `phase0-fixtures` directory contains small literal files. `expected.json` defines observables for the future runtime tests, not claims that nonexistent JSON/XML processors have passed. Its normalized model is the document wire graph; CSV's existing assembled wrappers are projected to that model only for fixture comparison.

The accepted feed has Order A with BillingAddress B1, ShippingAddress S1, two lines with quantities 2 and 1, and Tags [priority, web]; and Order B with B2/S2, one line with quantity 3, and an empty Tags collection. Both use a DateOnly and an offset-bearing SubmittedAt. The Address type is reused at two properties. Expected total orders = 2 and quantity = 6.

CSV uses H/O/B/S/L/G/T routes and fixed sibling order. Header H and trailer T carry batch identity/totals; flat address/tag/line records assemble the same conceptual roots. The document formats use natural nesting. No CSV transport model change is implied by conceptual equivalence.

The enveloped JSON deliberately puts Batch after orders. All unit validation and valid-unit observation/report callbacks observe no Batch; accepted execution handlers see Batch.Id = B123 from their first call. The root-array JSON/JSONL omit file metadata and use the same Order type/rules, so they have equivalent units without claiming metadata parity.

| Scenario | Records / invalid records | Units read / invalid / validated | Errors | Validation completed | ValidateThenExecute result |
|---|---|---|---|---|---|
| valid.csv | 13 / 0 | 2 / 0 / 2 | 0 | true | Succeeded, attempted 2, executed 2, opens 2. |
| valid.jsonl | 2 / 0 | 2 / 0 / 2 | 0 | true | Same. |
| valid-array.json | 2 / 0 | 2 / 0 / 2 | 0 | true | Same. |
| valid-envelope.json | 2 / 0 | 2 / 0 / 2 | 0 | true | Same, with complete accepted Batch metadata. |
| valid.xml, future | 2 / 0 | 2 / 0 / 2 | 0 | true | Same once XML is implemented. |
| invalid-child.jsonl, CollectErrors | 2 / 1 | 2 / 1 / 1 | 1 | true | ContentFailed, attempted/executed 0, opens 1; valid second unit is still validated. |
| malformed-line.jsonl, CollectErrors | 3 / 1 | 3 / 1 / 2 | 1 | true | ContentFailed, attempted/executed 0, opens 1. |
| late-total.json | 2 / 0 | 2 / 0 / 2 | 1 | true | ContentFailed at reconciliation; no replay. |
| trailing-garbage.json | 2 / 0 | 2 / 0 / 2 | 1 | false | ContentFailed on trailing syntax; no replay. |
| missing-child.jsonl | 1 / 1 | 1 / 1 / 0 | 1 | true | ContentFailed; Lines cardinality error only. |
| empty-array.json with OneOrMore | 0 / 0 | 0 / 0 / 0 | 1 | true | ContentFailed at EOF cardinality; no invented invalid record/unit. |
| empty-array.json with Many | 0 / 0 | 0 / 0 / 0 | 0 | true | Succeeded with zero calls and two opens. |
| Zero-byte JSONL input with Many | 0 / 0 | 0 / 0 / 0 | 0 | true | Same accepted empty repeated stream. |
| Zero-byte JSON input with any valid document selection | 0 / 0 | 0 / 0 / 0 | 1 | false | ContentFailed as invalid/missing JSON document, not an empty array. |
| single-object.json | 1 / 0 | 1 / 0 / 1 | 0 | true | Succeeded, one unit, two opens. |

ValidateOnly accepts valid cases as Succeeded with one open and no business execution; the optional valid-unit hook runs for individually valid units in this and every other mode. Content errors return ContentFailed, preserve any prior report callbacks, and never replay. Specifically late-total can report two units before EOF rejection. FailFast invalid-child stops after the first unit: RecordsRead = UnitsRead = InvalidRecords = InvalidUnits = 1, UnitsValidated = 0, errors = 1, ValidationCompleted = false.

Baseline validation has no warning diagnostics. For invalid-child, expect `file.validation`, ordinal 1, model path `Lines[0].Quantity`, source path `/Lines/0/Quantity`, physical line 1. For missing-child, expect `file.validation`, ordinal 1, model path `Lines`, source path `/Lines`. For malformed-line, expect `jsonl.record.invalid`, ordinal/line 2, without field values or guessed property path.

The zero-byte scenarios are represented by `inputText: ""` in the manifest, not by a newline-only fixture that would actually be an invalid blank JSONL record. Every nonempty fixture ends in exactly one LF and has no BOM.

CSV fixture declaration: discriminator index 0; Header H exactly once first, Trailer T exactly once last; root Order O OneOrMore; ordered children Billing B exactly One, Shipping S exactly One, Line L OneOrMore, Tag G Many. Header maps BatchId at index 1. Order maps Id at 1, DateOnly at 2 with format yyyyMMdd, and DateTimeOffset at 3 with format `yyyy-MM-dd'T'HH:mm:ssK`. Billing/Shipping map Code at 1; Line maps Sku at 1 and Integer Quantity at 2 with required/min 1; Tag maps String Value at 1; Trailer maps Integer ExpectedOrders at 1 and ExpectedQuantity at 2. All declared scalar fields are required. The fixture normalizer turns header/trailer into Batch metadata and CSV group children into document DTO fields; this is test projection, not a second runtime mapping feature.

Additional mandatory derived scenarios:

- Duplicate Id on an object: one `jsonl.record.invalid` error, invalid owner once; reject before any last-wins assignment.
- Quantity supplied as `"2"`: `json.field.type`, no duplicate required diagnostic. Quantity 0: a `file.validation` min error. Missing/null Quantity: a single required validation error. False/0 remain valid for required Boolean/Integer fields without other constraints.
- Missing Lines, Lines null, and Lines []: each is one cardinality error under OneOrMore; present empty Tags succeeds under Many. Tags [null] is an element mapping error.
- BillingAddress missing: one required validation error. BillingAddress {}: required Code error. Address type reused at both properties retains distinct source/property paths.
- Unknown field: reject by default; an explicit Ignore policy skips it without echoing its name/value. Unknown envelope sections remain syntax/resource-checked.
- Source-name derivation: order_id and order-id each derive OrderId; customerRef derives CustomerRef. Verify exact acronym behavior against the selected shared conversion helper. Source selectors remain literal. An explicit property resolves collisions; invalid names, empty normalization results, leading digits, and inherited-member conflicts fail configuration before writes.
- Late metadata missing: one `file.metadata.required` file error, no count reconciliation cascade. ExpectedOrders mismatch and ExpectedQuantity mismatch emit their respective example reconciliation code only when prerequisites mapped/validated.
- Returned execution failure on the second valid unit: ExecutionFailed, UnitsAttempted = 2, UnitsExecuted = 1, UnitsValidated = 2; retain the identical business error object.
- Source/callback faults and cancellation: throw, expose partial progress, and call neither terminal content hook. No extra unit scopes after failure.

## 9. Framework/API decision

**Phase 0 decision: no framework-floor change is currently justified by a required API.** Incremental Utf8JsonReader/JsonReaderState, typed numeric access, JsonDocument unit mapping, async stream reads, and XmlReader already provide the primitives. JSONL deliberately uses line framing, not the newer multiple-top-level-values serializer overload, because those are different formats. Choosing the older common API here does not lose required functionality or require a parallel fallback parser.

Current projects inherit net8.0/net9.0/net10.0 from root/src build props. Central System.Text.Json versions are selected per target; IO directly references CsvHelper and CoreEx, and CodeGen directly references OnRamp rather than IO. Current checked-in direct IO consumers are its unit-test project and the Functions test host. The machine has .NET 10 SDK plus an .NET 11 preview SDK; no preview feature is part of this design.

Do not add a direct System.Text.Json package solely to obtain a serializer already supplied by the runtime. Reassess if the chosen implementation requires a newer package API; central policy and transitive resolution must agree.

If a later vertical slice demonstrates a material correctness/resource-bound/maintenance benefit needing newer APIs, prefer a supported Microsoft package where clean, otherwise raise the affected package to net10.0 and update consumer/test/package compatibility explicitly. Do not preserve net8.0/net9.0 by downgrading behavior. Do not move all framework packages or the design-time console automatically. No conditional feature gaps or duplicate parsers.

This is not a commitment to retain out-of-support targets indefinitely; repository lifecycle retirement can proceed separately. Phase 1 should record the selected targets in release notes, and Phase 2/3 must revisit this gate if actual APIs change.

## 10. Intentional migration inventory

| Surface | Required change |
|---|---|
| Shared processor base | Move lifecycle/scoped hooks into FileProcessor<TUnit>; retain only useful CSV-specific intermediate code. |
| Processor construction/options | Remove common quotas and keep format options focused; migrate all generated constructor calls and direct runtime callers. |
| Progress constructors | Add UnitsRead/InvalidUnits and update summary consistency rules, tests, and hand-created snapshots. |
| Unit hooks | Include FileUnitInfo in focused argument objects for validation/execution/context configuration/reporting; regenerate typed routed dispatch and manually migrate handwritten partials. |
| Locations/diagnostics | Separate source/model paths; add ordinal correlation; no fabricated CSV lines for document errors. |
| Contexts | Move HasValidationErrors to common context if shared logic needs it; CSV route counts stay format-specific. Migrate header/trailer access into shared typed metadata and read-only accepted metadata. |
| Modes/outcomes | Add single-pass ValidateAndExecute, mode-independent Succeeded/ContentFailed/ExecutionFailed outcomes, original-Result execution-failure reporting, and summary invariants/finalization. |
| Content finalization | Rename OnRejectedAsync to OnContentFailedAsync; migrate handwritten overrides explicitly, with summary progress retained and no forwarding compatibility trap. |
| YAML | Group format-specific settings; move old flat CSV spellings with explicit diagnostics; use source-first property name plus optional property override with fixed DbEx-style PascalCase derivation and no casing options; default absent definition format to Csv only. |
| Generated document output | Natural nested DTOs/validators/mappings, not CSV Group wrappers; Address can be referenced twice. |
| Scalar catalog | Add DateTimeOffset with optional exact formats and standard invariant parsing otherwise; migrate schema/docs/options/bounds handling together. |
| Diagnostics/telemetry | Neutral lifecycle/validation names are documented changes; preserve CSV-owned syntax/mapping names. |
| Input ownership | Remove generic admission/work quotas from CSV and JSONL; admission belongs to consumers. |
| Samples/tests | Update existing generation/IO tests and Functions InventoryImports integration; do not hand-edit generated files. |
| Shipped guidance | Update runtime/generator guides, generated schema/config docs, instructions/skill examples, AI bundle copies, and template validation expectations via their owners. |

Before deleting generated or handwritten artifacts after a rename, enumerate exact files and review ownership. No broad directory cleanup or automatic removal of handler files.

## 11. Phase 0 completion and Phase 1 handoff

The representative literal fixtures and expectation manifest accompany this specification. Run `phase0-fixtures\verify-fixtures.ps1` with PowerShell 7.5 or later to check their syntax, line/record counts, totals, normalized conceptual parity, and negative-case deltas. This is an independent fixture inspection, not an unimplemented CoreEx parser. Full runtime behavior remains a Phase 1-3 acceptance obligation.

Phase 1 starts with current CSV regression expectations, extracts the shared coordinator, implements the new provenance/progress/options/metadata contracts, updates CSV generation/configuration and handwritten consumers, and publishes migration examples. Do not implement the document parser in the extraction change merely to make the abstraction look proven; validate its necessary shape against the Phase 0 JSONL contract, then deliver that vertical slice in Phase 2.

The main plan is amended to replace strictly pass-local execution metadata with the explicitly accepted immutable view and to record the stronger scalar and migration decisions here.

## 12. Final design challenge: clarified edge contracts

The final review challenged modes, counters, metadata timing, naming, configuration applicability, and bounded streaming. The following rules close gaps rather than add new format capabilities.

- A missing unitPath target, explicit null, or wrong container kind is a file structure error even with Many cardinality. Only a present empty selected array is a valid zero-unit collection. No success-shaped interpretation of a broken envelope.
- A declared metadata object, when present, must be an object; optional means absent allowed, not arbitrary scalar/null accepted. Defaults and presence rules must distinguish missing, invalid, and validated metadata. Never expose mutable partially mapped metadata to handlers.
- file.structure.cardinality deliberately spans unit and file checks. A diagnostic with a unit ordinal is unit-scoped; absence denotes file/envelope scope. Document this discriminator for all diagnostic codes rather than expecting consumers to infer scope from physical lines.
- A failed business Result takes outcome precedence over earlier single-pass content errors: return ExecutionFailed, preserve diagnostics/counters and original Result, and call only the execution-failure finalizer. ContentFailed means content failure without a returned business failure. Exceptions still propagate with partial progress.
- AcceptedMetadata availability at successful completion is mandatory in every mode. A content-failed or operationally interrupted attempt has no accepted-file view, even when individual metadata objects validated earlier.
- CSV has no per-unit byte quota. Accurate original spans are not available across CsvHelper encodings; do not infer them from read-ahead counters.
- Cancellation during business execution can occur after an application commit; an exception is not evidence that no side effects happened. Report hooks can also fail after writes. Retry safety still requires durable receipts/idempotency and attempt-isolated reports.
- JSONL discriminator settings reserve one declared direct member. Reject duplicate routing members before choosing a route, regardless of optional DTO mapping. Position checks include malformed/unrecognized non-skipped lines, not merely valid business units, so a bad trailing line cannot disappear from Last semantics.
- XML's chosen default remains unnamespaced Attribute. The namespaced element fixture intentionally requires explicit Element/namespace selection and is not compatible with omitted XML field settings. Phase 5 should evaluate explicit definition-/record-level namespace defaults to reduce repetition without silently changing that base default or making unqualified attributes inherit an element namespace.
- The fixture manifest's scenarioBaseline denotes explicit fixture settings, not engine defaults. Preserve this distinction and mechanically check kind/unitPath spellings and values during fixture inspection.

No claim of a complete parser/API proof is made by this design review. The CSV byte-bound spike and actual incremental JSON allocation tests are implementation exit gates, not assumptions that can be signed off by prose.

## Evidence

Repository evidence: `src\CoreEx.IO\FileProcessor.cs`, `FileUnitHandler.cs`, `FileUnitInfo.cs`, `FileLocation.cs`, `FileProcessingProgress.cs`, `FileProcessingSummary.cs`, `FileProcessingContext.cs`, `Csv\CsvFileProcessorBase.cs`, `Csv\CsvHierarchyDefinition.cs`, `Csv\CsvHierarchyFileProcessor.cs`, `Csv\CsvRoutedFileProcessor.cs`, `Csv\CsvReadStream.cs`, `Csv\CsvFileOptions.cs`, and `Csv\CsvFieldConverters.cs`; `src\CoreEx.CodeGen\Files\Config\CodeGenConfig.cs`, `RecordConfig.cs`, `PropertyConfig.cs`, `NodeConfig.cs`, and `Files\Templates`; `tests\CoreEx.IO.Test.Unit\FileProcessingSummaryTests.cs`, `FileBaselineFixtureTests.cs`, and `tests\CoreEx.CodeGen.Test.Unit\FileGenerationTests.cs`; root/src build props, Directory.Packages.props, and affected csproj references.

Platform reference: [Utf8JsonReader streaming and buffer requirements](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader). In particular, tokens can span reads and incremental parsing needs correct token/duplicate tracking even outside selected units; input admission is consumer-owned.
