# Generation-first inventory CSV import

This Functions test-host capability demonstrates a typed CSV import generated from a checked-in File CodeGen definition. The source of generated wire and runtime artifacts is `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml` (`name: Inventory`, `version: '1'`, record `Adjustment`); the CodeGen console is design-time only and does not become a Functions runtime dependency.

From the repository root, change into the CodeGen project so discovery runs beside `Program.cs` and `inventory-file.yaml`:

```powershell
Set-Location tests\CoreEx.Azure.Functions.TestApp.CodeGen
dotnet run -- file --validate
dotnet run -- file
dotnet run -- file --expect-no-changes
```

The configured `runtimePath` is `InventoryImports/Files` in namespace `CoreEx.Azure.Functions.TestApp.InventoryImports`. File CodeGen owns `InventoryAdjustment`, `InventoryAdjustmentMapping`, `InventoryAdjustmentValidator`, `InventoryContext`, `InventoryProcessor`, and the create-only `InventoryHandler`. Do not hand-author these generated artifacts. `InventoryHandler` owns scoped per-unit business delegation. The handwritten `InventoryProcessor` partial owns trusted context setup and report hooks; it uses the generated protected `Services` property to resolve invocation-scoped options/report adapters without a constructor conflict. Unit business dependencies must resolve from the handler's fresh child scope, not the processor provider.

## Application transaction and retries

The wire record contains `CommandId`, `Sku`, and `Quantity`. Validation is generated from the YAML; the handwritten handler delegates to the inventory application operation and preserves its original CoreEx `Result`. Trusted tenant context comes from deployment configuration, never from CSV fields, blob names, or report identity.

The application uses `SqlServerUnitOfWork` and `SqlServerOutboxPublisher` so an inventory `Items` update, durable `Receipts` entry, and outbox write commit in the same SQL Server transaction. Receipt identity includes trusted tenant, definition/version, credential-free source identity, source revision, and command ID; the delivery attempt is excluded. A repeat with the same command content is a durable no-op, while reuse with different content fails. The Aspire test schema and existing Functions AppHost SQL Server resource provide the database; the Function does not provision schema or seed business rows during invocation.

Whole-file acceptance is not a whole-file transaction: earlier units can commit before a later unit fails. Retrying the same immutable source revision with a new attempt relies on durable receipt deduplication to avoid repeating earlier mutations. Receipt retention remains an application policy. The transaction proves the database write boundary only; an outbox row does not prove relay publication or downstream exactly-once processing.

## Native Blob and report boundary

The Functions host uses the native Event Grid-source Blob trigger for `inventory-adjustments/{name}` with the `ImportStorage` binding connection. The Blob SDK adapter chooses and records the current ETag at invocation time, then pins both validation and execution reads to that revision. This is not historical Event Grid event-revision processing: a stale notification may select the current blob. Use a version-specific source contract if every historical revision must be processed.

The handwritten processor streams diagnostics and terminal outcome metadata through `AppendBlobClient`; it does not create local report files. Report identity isolates definition, source, revision, and attempt. Metadata moves from `InProgress` to a terminal outcome only when the corresponding report lifecycle hook completes. A completed report is not an idempotency ledger, and report failure cannot undo SQL effects already committed.

## Aspire runtime proof and limits

The existing `tests/aspire/CoreEx.Azure.Functions.Test.Aspire` AppHost orchestrates Azurite, SQL Server, and Functions Core Tools. The runtime test uploads the actual CSV input to Azurite, creates a storage-shaped event with `EventGridExtensionMethods.CreateBlobCreatedEvent`, and posts it through `Test.FunctionsWebhook(...).PostBlobCreatedAsync(...)`. That webhook call simulates notification delivery only: Azurite does not emit Event Grid events. The Functions Blob binding, Azure Blob SDK reads/writes, SQL Server transaction, and Append Blob report operations are real in the local test topology.

This does not prove Azure Event Grid subscription provisioning, authentication, or cloud delivery.
