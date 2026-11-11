# Generation-first inventory file imports

This Functions test-host capability demonstrates simple and complex CSV, JSON, and JSONL imports generated from checked-in File CodeGen definitions. Each format has a thin native Blob-triggered Function, generated nullable wire models/mappings/validators/processor/context, a handwritten scoped handler, and executable invocation/runtime tests. The CodeGen console is design-time only and does not become a Functions runtime dependency.

## Example matrix

| Format | Scenario and definition | Function / input container | Example payload |
|---|---|---|---|
| CSV | [Independent rows](../../CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml) | `ImportInventoryFile` / `inventory-adjustments` | [example.csv](./example.csv) |
| CSV | [Ordered adjustment/child rows with header and trailer](../../CoreEx.Azure.Functions.TestApp.CodeGen/inventory-csv-batch-file.yaml) | `ImportInventoryCsvBatch` / `inventory-csv-batch` | [example-batch.csv](./example-batch.csv) |
| JSON | [Independent objects in a root array](../../CoreEx.Azure.Functions.TestApp.CodeGen/inventory-json-file.yaml) | `ImportInventoryJson` / `inventory-json` | [example.json](./example.json) |
| JSON | [Envelope with nested adjustment lines, header, and trailer](../../CoreEx.Azure.Functions.TestApp.CodeGen/inventory-json-batch-file.yaml) | `ImportInventoryJsonBatch` / `inventory-json-batch` | [example-batch.json](./example-batch.json) |
| JSONL | [One independent object per line](../../CoreEx.Azure.Functions.TestApp.CodeGen/inventory-jsonl-file.yaml) | `ImportInventoryJsonl` / `inventory-jsonl` | [example.jsonl](./example.jsonl) |
| JSONL | [Routed header/trailer and nested adjustment objects](../../CoreEx.Azure.Functions.TestApp.CodeGen/inventory-jsonl-batch-file.yaml) | `ImportInventoryJsonlBatch` / `inventory-jsonl-batch` | [example-batch.jsonl](./example-batch.jsonl) |

Every input route uses `{name}`, connection `ImportStorage`, and `BlobTriggerSource.EventGrid`. Route configuration selects the definition; file content does not select a processor. All definitions use `ValidateThenExecute` and `CollectErrors`. The simple scenarios execute each row/object independently; they may contain many such units. JSONL still requires each entire object, including its child items, on one physical line.

The complex scenarios represent the same business data: a batch header, two adjustments for SKU `DEMO`, three bin lines in total, and a trailer declaring two adjustments and total quantity five. CSV uses contiguous `A` parents and `L` children between `H`/`T` metadata records. JSON selects `/Adjustments`, `/Header`, and `/Trailer` using disjoint JSON Pointers; member order is not a First/Last constraint. JSONL uses the direct `type` discriminator with First/Last metadata routes. Headers and trailers are metadata, and child items belong to their parent unit; none execute as separate inventory commands.

## Generate and customize

From the repository root, change into the CodeGen project so discovery runs beside `Program.cs` and `inventory-file.yaml`:

```powershell
Set-Location tests\CoreEx.Azure.Functions.TestApp.CodeGen
dotnet run -- file --validate
dotnet run -- file
dotnet run -- file --expect-no-changes
```

The configured `runtimePath` is `InventoryImports/Files` in namespace `CoreEx.Azure.Functions.TestApp.InventoryImports`. File CodeGen owns `InventoryAdjustment`, `InventoryAdjustmentMapping`, `InventoryAdjustmentValidator`, `InventoryContext`, `InventoryProcessor`, and the create-only `InventoryHandler`. Do not hand-author these generated artifacts. `InventoryHandler` owns scoped per-unit business delegation. The handwritten `InventoryProcessor` partial owns trusted context setup and report hooks; it uses the generated protected `Services` property to resolve invocation-scoped options/report adapters without a constructor conflict. Unit business dependencies must resolve from the handler's fresh child scope, not the processor provider.

The other five definitions use sibling output folders `JsonFiles`, `JsonlFiles`, `CsvBatchFiles`, `JsonBatchFiles`, and `JsonlBatchFiles`, with definition-prefixed type names in the same capability namespace. Each definition owns a non-overlapping folder. Generated `.g.cs` files are replaced by the generator; handler starters are create-only and preserve their handwritten implementations. Handwritten processor/context partials live beside the shared adapter rather than in generated output folders.

## Complex-unit validation and reconciliation

Generated validators enforce required identifiers, positive quantities, nonempty child collections, and metadata shape. The complex handlers additionally reject combined quantities exceeding the application's integer quantity range. Each accepted group's bin quantities are summed into one inventory adjustment for its parent SKU; bins demonstrate nested wire data and are not separately persisted stock locations.

The `OnValidatedUnitAsync` hook adds only each valid parent's count and combined quantity to pass-local `InventoryBatchTotals`. The EOF hook compares these with the typed trailer and emits `inventory.trailer.mismatch` for a discrepancy. It does not reconcile invalid records as if they were trustworthy totals. Missing required metadata, invalid children, and late trailer mismatches prevent all replay execution. Fresh generated replay contexts do not share validation accumulators.

## Application transaction and retries

The application command contains `CommandId`, `Sku`, and `Quantity`. Simple wire records carry these directly; complex wire units project their children into that same command. Validation is generated from the YAML; the handwritten handler delegates to the inventory application operation and preserves its original CoreEx `Result`. Trusted tenant context comes from deployment configuration, never from file fields, blob names, or report identity.

The application uses `SqlServerUnitOfWork` and `SqlServerOutboxPublisher` so an inventory `Items` update, durable `Receipts` entry, and outbox write commit in the same SQL Server transaction. Receipt identity includes trusted tenant, definition/version, credential-free source identity, source revision, and command ID; the delivery attempt is excluded. A repeat with the same command content is a durable no-op, while reuse with different content fails. The Aspire test schema and existing Functions AppHost SQL Server resource provide the database; the Function does not provision schema or seed business rows during invocation.

Whole-file acceptance is not a whole-file transaction: earlier units can commit before a later unit fails. Retrying the same immutable source revision with a new attempt relies on durable receipt deduplication to avoid repeating earlier mutations. Receipt retention remains an application policy. The transaction proves the database write boundary only; an outbox row does not prove relay publication or downstream exactly-once processing.

## Native Blob and report boundary

All six Functions share the same Blob SDK adapter and `ImportStorage` binding connection. Pre-provision each selected input container, the report container configured by `InventoryImportReports`, database/schema/business rows selected by `InventoryImportDatabase`, and the trusted tenant configured by `InventoryImportTenant`. The Blob SDK adapter chooses and records the current ETag at invocation time, then pins both validation and execution reads to that revision. This is not historical Event Grid event-revision processing: a stale notification may select the current blob. Use a version-specific source contract if every historical revision must be processed.

The handwritten processor streams diagnostics and terminal outcome metadata through `AppendBlobClient`; it does not create local report files. Report identity isolates definition, source, revision, and attempt. Metadata moves from `InProgress` to a terminal outcome only when the corresponding report lifecycle hook completes. A completed report is not an idempotency ledger, and report failure cannot undo SQL effects already committed.

## Tests and runtime limits

[`InventoryImportExamplesTests`](../../CoreEx.Azure.Functions.Test.Unit/InventoryImportExamplesTests.cs) runs all six checked-in payloads through `FunctionTester` and the real host composition, with recording application/report boundaries. It checks correct routing, quantity projection, trusted tenant propagation, ETag-pinned replay, and awaited report finalization. Component rejection cases cover late invalid quantities, missing headers/trailers/children, trailer count/quantity mismatches, and combined quantity overflow. These recording tests do not claim persisted business behavior.

The existing `tests/aspire/CoreEx.Azure.Functions.Test.Aspire` AppHost orchestrates Azurite, SQL Server, and Functions Core Tools. [`BlobRuntimeTests.InventoryExamples`](../../aspire/CoreEx.Azure.Functions.Test.Aspire/BlobRuntimeTests.InventoryExamples.cs) adds the five new routes alongside the existing simple CSV runtime proof. It uploads the actual example payloads with isolated SKUs, checks two persisted adjustments and outbox writes, and resubmits the same revision to check durable deduplication and separate attempt reports. The runtime tests create a storage-shaped event with `EventGridExtensionMethods.CreateBlobCreatedEvent` and post it through `Test.FunctionsWebhook(...).PostBlobCreatedAsync(...)`. That webhook call simulates notification delivery only: Azurite does not emit Event Grid events. The Functions Blob binding, Azure Blob SDK reads/writes, SQL Server transaction, and Append Blob report operations are real in the local test topology.

This does not prove Azure Event Grid subscription provisioning, authentication, or cloud delivery.
