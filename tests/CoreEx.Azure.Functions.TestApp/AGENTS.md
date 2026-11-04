# Functions executable test host

This non-packable net10.0 host demonstrates native HTTP, HTTP idempotency, Service Bus, Blob, and Timer integration for framework tests. It is not a Contoso business domain. Read the [Functions package guide](../../src/CoreEx.Azure.Functions/README.md) and scoped Functions/test instructions before modifying it.

## Inventory file capability

[InventoryImports](InventoryImports/README.md) is a generation-first inventory-adjustments CSV example. Its checked-in definition is `tests/CoreEx.Azure.Functions.TestApp.CodeGen/inventory-file.yaml`; File CodeGen generates the wire model, mapping, validator, context, processor, and create-only handler under `InventoryImports/Files`. Keep application execution in the handler and trusted context/report hooks in a handwritten processor partial. The generated processor's protected `Services` property is for invocation-scoped coordinator dependencies only; unit business services resolve through the handler's fresh child scope.

The native route is `inventory-adjustments/{name}` with connection `ImportStorage`. The Blob SDK adapter pins both processor passes to the selected ETag; reports are written through `AppendBlobClient` with outcome metadata, not local files. The supported SQL Server unit of work atomically commits the inventory update, durable receipt, and outbox write. Pre-provision schema/business data, the input container, report storage, and Event Grid subscriptions outside invocation execution.

Content rejection and application failures follow explicit report/trigger policies; operational failures and cancellation propagate. Earlier business units may commit before a later unit fails, so durable receipt deduplication is required for safe retries; this is not exactly-once delivery.

The local immutable snapshot helper supports stream-factory tests only; there is no database/report provisioning CLI. The existing Aspire Functions AppHost runs Azurite, SQL Server, and Core Tools. Its CSV runtime test uploads the real input, then uses `EventGridExtensionMethods.CreateBlobCreatedEvent` and `FunctionsWebhook(...).PostBlobCreatedAsync(...)` to simulate notification delivery; Azurite itself does not emit Event Grid events. The binding, SDK operations, database transaction, and report writes are real in the harness, but Azure Event Grid provisioning/authentication/delivery are not proven. Do not claim the new runtime test passed until it has been run.
