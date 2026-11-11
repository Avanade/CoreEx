# CoreEx.IO runtime tests

The test suite covers format-neutral processing contracts, flat CSV processing, unordered routed records, hierarchical groups, nested JSONL, strict decoding, stream ownership, execution-context isolation, diagnostics, and CoreEx invoker/telemetry behavior. CSV parsing uses CsvHelper 34.0.0; expected malformed input and field errors are handled through diagnostics rather than per-record exception control flow.

## Portable fixtures

`Fixtures/baseline.json` records required `ValidateThenExecute` outcomes under `CollectErrors`, using UTF-8 without a BOM and LF endings. Flat files have a column-name header and typed fields `Id` (integer), `Name` (string), and `Quantity` (integer; `NotNull`, not `Mandatory`, so zero is valid). Their CSV header is not a data record.

Hierarchical fixtures are positional and have exact, case-sensitive discriminator values: `H` is a single required header, `O` a root order, `L` a required contiguous child of an order, and `T` a single required final trailer. Each order requires at least one line. The trailer carries root count and sum of line quantities. Header/trailer records count as physical records, not execution units. The orphan and unknown record in `orders-invalid-structure.csv` are diagnostic failures, not contributors to accepted order totals.

Flat records begin on physical lines 2 and 3 even when the second record spans two lines. The final flat conversion failure and hierarchical trailer mismatch both require zero execution calls. EOF reconciliation anchors to the last record's physical starting line; logical child/property paths do not imply physical coordinates.

## Flat processing

`CsvFileProcessorTests` covers late mapping and EOF rejection with zero execution, fresh models/context/scopes on replay, original `Result` failure, scoped errors/warnings and CoreEx validator messages, case-insensitive/reordered/duplicate/missing headers, positional selectors, missing fields, multiple errors per record, comments/blanks, quoted/escaped/multiline fields, CR/LF/CRLF locations, single-byte nonseekable async input, split Unicode, invalid UTF-8, invariant nullable/date conversion, and concurrent invocation mapping/counters.

Lifecycle tests cover cancellation before opening, during blocked validation/replay reads, and inside a scoped diagnostic callback; open/read/converter/business/reporting faults; null/unreadable streams; replay count/mapping drift; invalid replay contexts; missing execution handlers; awaited report backpressure under the file ambient context; scope disposal; and asynchronous disposal of every owned input. Test streams reject synchronous reads. Separate 10K conversion-error and malformed-quote cases assert zero observed first-chance exceptions during processing, and streamed diagnostic tests verify that framework state retains no diagnostic history.

## Hierarchical processing

`CsvHierarchyFileProcessorTests` processes all three order fixtures and proves zero writes for a late trailer mismatch or structural rejection. Coverage includes exact/missing/unknown discriminators, malformed-record isolation, recognized-invalid counts, accepted envelope availability and duplicate preservation, first/last rules, orphan children, arbitrary nested assembly and physical provenance, sibling-order/reopening failures, and `Many`/`One`/`Optional`/`OneOrMore` cardinality at parent and file level.

Lifecycle coverage includes fresh replay wire records/context/envelopes, field validation only on the first pass, one async handler scope per root and pass with parent restoration, group errors without fake invalid physical rows, invocation-wide continuation for mapping/missing-child/boundary/EOF failures, required-record/parent/EOF anchors, EOF reconciliation, original execution `Result` failure, count/mapping replay drift, projection/reporting faults, awaited reporting and cancellation under the file ambient context, context-reuse rejection, and invalid named route mappings before opening input.

Independent keyed hierarchy definitions sharing a group CLR type coexist with a flat definition, using their own scoped handlers under concurrent replay in either registration order. Weak-reference evidence checks that completed projected groups, aggregate nodes, and wire records are not retained by file state. A 10K bad-child conversion case processes 10,001 group records (root plus children) and asserts zero observed first-chance exceptions.

## Encoding and workload

`CsvBoundsAndEncodingTests` covers strict configured encoding, byte accounting, caller-owned cancellation, replay count/encoding drift, and processing beyond the former unit/group quotas. Source admission and resource policies belong to consumers.

## JSONL processing

JSONL tests cover exact source-byte locations on irregular nonseekable streams, recoverable invalid lines, duplicate members, nested cardinality and model/source paths, consumed versus ignored malformed strings, standard invariant dates versus explicit exact formats, metadata serializer rules and fresh accepted copies, scopes, cancellation, and deterministic replay across all three modes.

UTF-8, UTF-16 LE/BE, and UTF-32 LE/BE are exercised with and without matching BOMs on single-byte nonseekable streams, including split non-ASCII/supplementary text. Cases cover stock StreamReader/CsvHelper preamble handling, disabled encoding autodetection, partial UTF-8 signatures, empty/BOM-only input, embedded U+FEFF preservation, named-header and hierarchy-discriminator handling, BOM-inclusive byte counts, strict replacement-fallback override without mutating caller encodings, malformed/truncated code units, and ASCII repertoire violations. There is no custom conflicting-BOM classification: a mismatched BOM can decode as unwanted characters. Invalid encoding produces terminal content rejection; the strict .NET decoder throws `DecoderFallbackException` internally, which CoreEx converts to one terminal diagnostic. Normal field/structure failures avoid per-record exception control flow; operational I/O and application callback faults propagate.

The representative workload case reports cumulative allocation and elapsed time (not peak/live memory or a performance assertion) for 100,001 flat 64-character rows with scoped validation and a 10,001-record root with 1,024-character children. Tests verify strong-name tokens for both assemblies.

Run the suite for the locally selected framework with `dotnet test tests\CoreEx.IO.Test.Unit\CoreEx.IO.Test.Unit.csproj -p:CoreExLocalTargetFramework=net10.0`. To exercise all supported target frameworks, use `dotnet test tests\CoreEx.IO.Test.Unit\CoreEx.IO.Test.Unit.csproj -p:CoreExLocalTargetFramework=`.
