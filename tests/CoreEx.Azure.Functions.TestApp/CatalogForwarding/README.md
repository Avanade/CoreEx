# Catalog forwarding

This is the executable Section 5 example. Use the repository revision that matches your CoreEx release; the projects use the dependencies pinned by [`Directory.Packages.props`](../../../Directory.Packages.props).

[`catalog-forwarding-file.yaml`](../../CoreEx.Azure.Functions.TestApp.CodeGen/catalog-forwarding-file.yaml) generates the CSV processor. [`CatalogForwardingFunction`](CatalogForwardingFunction.cs) selects the current Blob revision, pins both reads, and writes attempt, diagnostic and completion records. [`CatalogForwardingHandler`](Files/CatalogForwardingHandler.cs) publishes one binary command per validated row. Each invocation has a fresh report, but the command ID is stable for the same source revision and row. The capability does not write a database or use an outbox.

The handler uses the keyed Service Bus publisher so direct catalog sends do not replace the shared test host's default SQL outbox publisher for inventory.

[`CatalogForwardingTests`](../../CoreEx.Azure.Functions.Test.Unit/CatalogForwardingTests.cs) checks real client configuration without network calls, late rejection, second-publish failure and binary conversion. [`BlobRuntimeTests.CatalogForwarding`](../../aspire/CoreEx.Azure.Functions.Test.Aspire/BlobRuntimeTests.CatalogForwarding.cs) uses the existing Core Tools/Aspire harness to upload a Blob, post a simulated Event Grid notification, receive a real emulator message and read the terminal report. It does not prove cloud Event Grid delivery or a backend application.

From the repository root:

```powershell
$env:CoreExLocalTargetFramework = "net10.0"
dotnet test tests\CoreEx.Azure.Functions.Test.Unit --filter "FullyQualifiedName~CatalogForwardingTests"
dotnet test tests\aspire\CoreEx.Azure.Functions.Test.Aspire --filter "FullyQualifiedName~CatalogBlobNotification"
```

The runtime case requires containers and Azure Functions Core Tools. The AppHost sets `ImportStorage`, `CatalogImportReports` and `ServiceBus`, and configures the non-session `catalog-item-upserts` queue. The existing shared runtime harness also starts SQL Server for the emulator and inventory tests; the forwarding code has no application database dependency.
