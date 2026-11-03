# CoreEx.IO contract and migration baseline

Step 1 covers format-neutral contract invariants and extension-point behavior. There is deliberately no CSV engine or parser dependency yet. Fixture integrity tests do not claim to prove parsing, whole-file acceptance, stream ownership, scope isolation, or hook orchestration. Those become executable processor tests in Steps 3 and 4.

## Portable fixtures

`Fixtures/baseline.json` records required `ValidateThenExecute` outcomes under `CollectErrors`, using UTF-8 without a BOM and LF endings. Flat files have a column-name header and typed fields `Id` (integer), `Name` (string), and `Quantity` (integer; `NotNull`, not `Mandatory`, so zero is valid). Their CSV header is not a data record.

Hierarchical fixtures are positional and have exact, case-sensitive discriminator values: `H` is a single required header, `O` a root order, `L` a required contiguous child of an order, and `T` a single required final trailer. Each order requires at least one line. The trailer carries root count and sum of line quantities. Header/trailer records count as physical records, not execution units. The orphan and unknown record in `orders-invalid-structure.csv` are diagnostic failures, not contributors to accepted order totals.

Flat records begin on physical lines 2 and 3 even when the second record spans two lines. The final flat conversion failure and hierarchical trailer mismatch both require zero execution calls. EOF reconciliation anchors to the last record's physical starting line; logical child/property paths never imply physical coordinates. The manifest is a migration acceptance baseline, not a claim that every behavior already exists in the source demo.

## Source characterization

The reviewed source is `C:\Repos\Playground\Sor.Demo`. Its unit solution `Sor.Demo.Test.Unit.slnf` passed 423 generator tests and 196 sample unit tests during the design review. Its Aspire/transport runtime tests were not run. The useful starting coverage includes `CsvProcessingTest`, `OrderExecutionTest`, `BuilderExtensionsTest`, `GenerationTest`, `GeneratedRulesTest`, `MultiRecordGenerationTest`, `HierarchyConfigurationTest`, `OnRampMapCompatibilityTest`, and `ProjectBatchTest`. Keep generated-project compilation coverage during migration; string comparisons alone are insufficient.

| Area | Source behavior or dependency | Migration action |
|---|---|---|
| Parser | TinyCsvParser 3.0.11; mapping outcomes and converters can represent invalid content without throwing from framework control flow. | Integrate asynchronous enumeration and cancellation. Characterize parser-internal malformed quoting/header exceptions separately; do not hide them behind Result wrappers. |
| Parser compatibility | The reviewed package has net8.0/net10.0 assets, MIT licensing, and a net8.0 System.IO.Pipelines 10.0.8 dependency. | Verify actual restored assets, signing, supported TFMs, and transitive dependencies before introducing the package; Step 1 adds no parser reference. |
| CodeGen API | Demo pins OnRamp 2.4.0; CoreEx pins 2.3.1. Demo uses CodeGenPropertyMap, PrepareMapAsync, and MapEntryKey. | Upgrade deliberately in Step 6 and run RefData/Count regressions; do not copy source against incompatible APIs now. |
| Expected errors | Demo validation flattens errors into strings and keeps a file-wide list. | Preserve localization/severity/location in individual diagnostics; do not construct ValidationException or Result.ValidationError per bad record. |
| Input ownership | Demo has path, stream snapshot, and revision-pinned Blob APIs. | Core only accepts a stream-opening factory; caller guarantees immutable bytes, processor owns returned streams, and there is no compulsory snapshot. |
| Execution | Demo has hierarchical validation/replay support. | Give flat input the same whole-file and EOF acceptance barrier; fresh models/context on replay. |
| Lifetimes | Multi-record registrations share CsvFileProcessor&lt;CsvRecordEnvelope&gt;; first registration wins. Handlers are singletons. | Distinct generated service identities; fresh scoped handlers per row/root group; never share tracked services across a file. |
| CoreEx ambient context | Resolving the current scoped registration resets/disposes its parent. | Add safe nesting in Step 2 before implementing unit execution; saving/restoring a disposed reference is not sufficient. |

The runtime acceptance matrix in `docs/file-processing-design.md` remains authoritative for the larger suite: headers/selectors, quoting, conversions, requiredness, record policies, missing records, EOF reconciliation, no-write validation, report failures/backpressure, replay/ownership/cancellation, bounds/encoding, context isolation, multi-definition registration, and concurrent invocations. Numeric limits and precise BOM support remain Step 5 work, not assumed parser defaults.
