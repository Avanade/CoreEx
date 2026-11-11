# Generation-first file integrations

## Generated and handwritten ownership

Keep the file contract in the CodeGen console's checked-in `*-file.yaml`. File CodeGen generates the nullable wire model, mapping, validator, context, processor, and create-only handler; handwritten code should add application behavior and host adapters, not duplicate generated types.

The framework's [InventoryImports example](https://github.com/Avanade/CoreEx/tree/main/tests/CoreEx.Azure.Functions.TestApp/InventoryImports) uses `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml` (`name: Inventory`, record `Adjustment`) to generate `InventoryAdjustment`, `InventoryAdjustmentMapping`, `InventoryAdjustmentValidator`, `InventoryContext`, `InventoryProcessor`, and `InventoryHandler` under `InventoryImports/Files`. Its handwritten processor partial uses inherited protected `Services` for coordinator options/reporting, not as the fresh unit provider. Keep per-unit business dependencies in the generated handler's fresh child scope.

The external [JSONL definition example](https://github.com/Avanade/CoreEx/blob/main/tests/CoreEx.CodeGen.Test.Unit/orders-jsonl-file.yaml) generates reusable Address occurrences, order-line/scalar collections, DateTimeOffset conversion, and the same scoped application boundary. Its [integration test](https://github.com/Avanade/CoreEx/blob/main/tests/CoreEx.IO.Test.Unit/GeneratedJsonLinesIntegrationTests.cs) pins replay to immutable bytes and uses an in-memory receipt double to illustrate retry ownership; it does not prove durable deduplication. [Routed JSONL](https://github.com/Avanade/CoreEx/blob/main/tests/CoreEx.CodeGen.Test.Unit/routed-jsonl-file.yaml) adds explicit metadata matches and First/Last checks without making metadata executable. These framework examples are illustrative, not files assumed to exist in a consumer project.

## Blob revisions, reports, and transactions

Keep the native Functions Blob trigger thin and use Azure Storage SDK clients for input and reports. Choose the current blob revision at invocation time and pin both validation and execution reads to that ETag; use a version-specific source contract when historical event revisions must be processed.

Stream attempt-isolated reports through durable Blob storage. The example uses `AppendBlobClient` and outcome metadata, moving from `InProgress` to a terminal outcome. The application uses `SqlServerUnitOfWork` and `SqlServerOutboxPublisher` to commit `Items`, `Receipts`, and the outbox write in one SQL Server transaction. Receipt identity excludes the delivery attempt so retries of the same source revision do not repeat already-committed units. This is not a whole-file transaction or an exactly-once trigger/relay guarantee.

## Aspire verification boundary

The existing Functions Aspire AppHost orchestrates Azurite, SQL Server, and Functions Core Tools. Its CSV scenario uploads the real input to Azurite, creates an Event Grid-shaped notification with `EventGridExtensionMethods.CreateBlobCreatedEvent`, and posts it through `Test.FunctionsWebhook(...).PostBlobCreatedAsync(...)`. That call simulates notification delivery only because Azurite does not emit Event Grid events; the Functions binding, Blob SDK operations, SQL Server transaction, and Blob report writes are real in the harness.

Do not treat this local runtime test as proof of Azure Event Grid provisioning, authentication, or delivery, and do not claim a new test passed until it has actually run. Local immutable snapshot helpers may support stream-factory tests, but they do not justify a SQLite/database or filesystem-report CLI.
