# CoreEx.IO.Json

JSON document streaming and shared natural-object mapping for both JSON documents and [JSONL](../JsonLines/README.md). Processing uses the [shared CoreEx.IO lifecycle](../README.md#processing-lifecycle), not another implementation of scopes, reporting, accounting, or replay.

## Key types

| Type | Purpose |
|---|---|
| [`JsonFileProcessor<TUnit>`](./JsonFileProcessor.cs) | Streams selected business objects from one UTF-8 JSON document. |
| [`JsonFileDefinition<TUnit>`](./JsonFileDefinition.cs) | Declares the unit kind, JSON Pointer, cardinality, and disjoint metadata selectors. |
| [`JsonFileContext`](./JsonFileContext.cs) | Carries invocation-local document state and metadata. |
| [`JsonFileMetadata<TMetadata>`](./JsonFileMetadataT.cs) | Declares a selected metadata object and its pure validator. |
| [`JsonUnitMapping<TRecord>`](./JsonUnitMappingT.cs) | Configures standard System.Text.Json contracts, source names, and scalar converters. |
| [`JsonFieldConverters`](./JsonFieldConverters.cs) | Supplies native scalar converters and date/finite-double policy extensions. |
| [`JsonFileOptions`](./JsonFileOptions.cs) | Configures shared JSON depth and unknown-member policy. |
| [`JsonDocumentKind`](./JsonDocumentKind.cs) | Selects array-of-objects or root-object processing. |

## Mapping and validation

`JsonUnitMapping<T>` configures standard System.Text.Json contracts for caller-owned nullable wire types with writable direct properties. The serializer owns object construction, property assignment, nested objects, and collections. One CLR type has one consistent source-name contract within a definition. Requiredness, collection cardinality/`maxItems`, and null reference-type collection elements belong in the pure validator, not the deserializer; [File CodeGen](../../CoreEx.CodeGen/Files/README.md) generates these rules. Direct runtime composition supplies equivalent CoreEx rules such as `Property(x => x.Lines).NotNull().Collection(minCount: 1, maxCount: 1000, with: v => v.WithItemValidator(lineValidator))`. These checks occur after collection allocation and produce `file.validation`. Missing/null values remain null; an explicit empty array remains an empty list.

Member names match exactly and case-sensitively. Unknown members reject by default; `JsonUnknownMemberPolicy.Ignore` still checks syntax, duplicates, and parser depth but does not exhaustively decode ignored string values. Duplicate decoded member names reject before mapping, including ignored objects. Unknown-member diagnostics anchor to the declared parent without echoing untrusted keys. `FileLocation.PropertyPath` uses CLR paths such as `Lines[0].Quantity`; `SourcePath` is a unit-relative escaped JSON Pointer such as `/lines/0/quantity`. Root errors use the empty pointer; physical provenance depends on the framing format.

Strict native number/Boolean/string tokens are required. Numeric strings, fractional integers, overflow, and nonfinite doubles fail without coercion. Empty/whitespace strings are preserved. Dates use standard invariant .NET parsing when no format is supplied; an explicit format uses exact parsing with no fallback. JSON DateTime rejects offsets; DateTimeOffset requires and preserves `Z` or a numeric offset.

Deserialization reports the serializer's first conversion/unknown-member error per unit, in source encounter order, and suppresses downstream validators for that unit. CollectErrors continues with subsequent units where framing permits; successful deserialization still permits multiple ordinary validation messages. This intentionally replaces exhaustive declaration-ordered mapping errors and cardinality-over-conversion precedence.

Custom scalar conversions use standard `System.Text.Json.Serialization.JsonConverter<T>`, not `JsonValueConverter<T>` or the removed `JsonFieldValue` abstraction. `JsonFieldConverters` supplies native scalar converters and small date/finite-double policy converters. Regenerate mappings/validators so collection bounds move from `MapObjects`/`MapValues` arguments into validators. Neither object nor collection token-to-model deserialization is reimplemented by CoreEx.

## JSON documents

`JsonFileProcessor<TUnit>` accepts one UTF-8 JSON document with a selected root or nested array of business objects, or one root business object. The selector traverses literal object members only; JSON Pointer segments use RFC 6901 escaping (`~0` for `~`, `~1` for `/`) and never interpret array indexes as selector segments. `JsonFileDefinition<TUnit>` declares the unit kind/path/cardinality and optional non-overlapping metadata object pointers.

For arrays, a missing selector, explicit null, or wrong JSON kind is a file structure error even when cardinality is `Many`; only a present empty array is accepted as zero units when cardinality allows it. Object mode selects only the document root and requires cardinality `One`. Array elements must be objects. A selected metadata value must also be an object; requiredness is checked at EOF, after the complete document has been read. See the [shared metadata contract](../README.md#metadata) for pass-local versus accepted metadata.

`JsonFileOptions` shares JSON mapping policy and `MaxDepth` (64 by default) with JSONL. Parser, duplicate-member, and depth errors stop document traversal because the reader cannot reliably recover. Unit mapping and scoped business execution still follow the shared lifecycle: ValidateThenExecute reopens the identical immutable source only after whole-file acceptance; ValidateAndExecute may have earlier writes when later syntax or EOF checks fail. No file/token/metadata quota is imposed.

## Streaming and memory

The document reader uses `Utf8JsonReader` incrementally across arbitrary non-seekable stream reads, validates closing syntax and trailing content, and scans decoded property names for duplicates even in skipped sections. A non-owning selected-object stream passes the original bytes for exactly one object to `JsonSerializer.DeserializeAsync<T>`; it performs framing, not object mapping. Selected objects are not captured into a raw-object buffer or per-object DOM. After a conversion failure, the reader drains the selected object for structural checks before continuing. JSONL instead uses `JsonSerializer` over its existing per-line DOM.

Memory still includes the current typed unit, metadata, duplicate-name sets for open objects, reader/serializer buffers, and incomplete tokens with their prefixes. A root-object unit or an unbounded mapped collection can therefore still be large. Collection bounds are business validation after deserialization, not allocation limits. Property-specific scalar-collection converters delegate to the standard serializer with their scalar format and can require buffering that collection inside System.Text.Json. Source admission and invocation concurrency remain [host-owned](../README.md#input-ownership); this is not a bulk-data platform or an absolute memory cap.

The .NET 8 package explicitly depends on System.Text.Json 9 to avoid System.Text.Json 8 buffering entire ignored containers during asynchronous deserialization. This retains the .NET 8 runtime target and standard serializer behavior without introducing a custom deserializer.

## Related documentation

- [CoreEx.IO](../README.md): shared lifecycle, handlers, metadata, reporting, and telemetry.
- [CoreEx.IO.JsonLines](../JsonLines/README.md): line framing, discriminator routes, and a runtime mapping example.
- [File CodeGen](../../CoreEx.CodeGen/Files/README.md#json-document-definition): generation-first JSON document definitions.
- [Functions inventory examples](../../../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md#example-matrix): a simple JSON array and a complex envelope with nested child items, selected header/trailer metadata, and EOF reconciliation.
