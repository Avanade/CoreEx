# Getting Started with CoreEx: Functions and File Processing

This guide builds a file-driven integration using Azure Functions, CoreEx.IO, CoreEx.Azure.Storage.Blobs, Azure Service Bus, and a mocked HTTP backend. It complements the [microservice getting-started guide](./getting-started.md): there is no API host, domain database, or outbox relay in this walkthrough.

The recommended path uses the CoreEx AI skills to scaffold and implement the solution. Review each plan before approving implementation; the templates create an empty host, not the finished integration. The prompts and examples below describe the behavior to build and verify, not code that already exists in your new solution.

## What we're building

Northstar, a fictional distributor, receives a supplier's CSV price book in Blob Storage. Each row describes a catalog item and its price effective date. The integration validates the entire file, publishes one command per item to a Service Bus queue, and uses a second Function to map each command into an OData-style request to a mocked catalog backend.

```text
Supplier CSV -> Blob Storage -> Blob-triggered Function
    -> CoreEx.IO validate and replay -> one command per row -> Service Bus queue
    -> Service Bus-triggered Function -> application service / adapter
    -> mocked OData-style catalog backend
```

By the end, a valid two-row file should produce two queue messages and two backend updates. An invalid file should produce diagnostics and **no messages**. The walkthrough demonstrates two mapping boundaries: supplier CSV to an internal command, then command to the backend's representation.

## Choose the solution shape

| Choice | Shape | When to use it |
|---|---|---|
| **Layered (recommended)** | One `Functions` deployment with `Contracts`, `Application`, and `Infrastructure` libraries, plus design-time CodeGen and tests. | The integration has business rules, external-system mappings, multiple capabilities, or a likely need to share logic. |
| **Standalone** | One `Functions` deployment with capability-local contracts, handlers, and adapters, plus design-time CodeGen and tests. | A small integration with little shared logic; extract libraries later when they earn their keep. |

Both shapes run the same workflow. One Functions project is one deployment, identity, and configuration boundary. Separate Blob and queue Function Apps are possible later if they need independent scaling, permissions, or deployment cadence; they are not required here.

In the layered shape, keep the internal command in Contracts, orchestration and adapter interfaces in Application, and the Service Bus publisher and HTTP client/adapter in Infrastructure. The Functions host owns triggers, Blob access, reports, and composition. Generated file integration types belong together in a runtime project; for this walkthrough, place them in the Functions host and have the scoped handler call Application. CodeGen is a separate design-time console, not a runtime dependency.

## Prerequisites

- .NET SDK 10 or later and Azure Functions Core Tools for the isolated worker.
- Podman or Docker for local services, and an AI coding assistant capable of using the installed CoreEx skills.
- Azure CLI for creating local Blob containers and uploading the sample file.
- A CoreEx.Template release that includes `coreex-functions`, `coreex-codegen`, the current File processing capabilities and the `CoreEx.Azure.Storage.Blobs` guide, with the matching Blob integration package available. Use the same version for the template, packages, and AI assets; if the needed capabilities are not in the latest stable release yet, use an explicitly available version that contains them.
- Local Azurite, the Azure Service Bus emulator and a mock HTTP backend, or equivalent Azure resources. The Service Bus emulator also needs its SQL Server backing container; that is emulator infrastructure, **not** an application database.

Local emulator credentials are for development only. Never commit production credentials or `local.settings.json`.

## 1. Create the solution and install the AI assets

Create a new repository and install a specific compatible template version. Replace `<version>` with the version you resolved above; do not literally type the angle brackets.

```powershell
mkdir Northstar.Supplier.Catalog
cd Northstar.Supplier.Catalog
git init
dotnet new install CoreEx.Template::<version>
dotnet new list --tag CoreEx
dotnet new coreex-ai
```

Open the folder in your IDE and reload the AI context. The installed `/coreex-bootstrap` and `/coreex-scaffold` skills can create the root guidance and solution; the implementation prompts below use `/coreex-file` and `/coreex-functions`.

The short setup prompts rely on the conventions shipped in that AI bundle, including the scaffold skill's local infrastructure recipe. When verifying unreleased changes to this guide, install the matching locally built template pack first; an older published bundle will not acquire the new guidance automatically.

## 2. Scaffold the selected shape

For the recommended layered shape, give your AI assistant this prompt:

```text
Use coreex-bootstrap to create Northstar.Supplier.Catalog as a layered Functions integration, following the scaffold skill's standard project and test conventions. Show the plan and wait for approval.

Use Contracts, Application and Infrastructure, one Functions host and a separate CodeGen console for File generation. We need Azure Service Bus, but no application database, reference data, outbox, Domain layer or API/Relay/Subscribe hosts. Stop at a built empty scaffold; capabilities and local infrastructure follow separately.
```

For standalone instead, give your AI assistant this prompt:

```text
Use coreex-bootstrap to create Northstar.Supplier.Catalog as a standalone Functions integration, following the scaffold skill's baseline and test conventions. Show the plan and wait for approval.

Use one Functions host with capability-local contracts, handlers and adapters, plus a separate CodeGen console for File generation. We need Azure Service Bus, but no business-layer libraries, application database, reference data, outbox or API/Relay/Subscribe hosts. Stop at a built empty scaffold; capabilities and local infrastructure follow separately.
```

The full `coreex` scaffold and the `coreex-solution` baseline are alternatives, not steps to run together. With no database and reference data, add the independent `coreex-codegen` tool explicitly; a CodeGen console is not guaranteed by the full scaffold. See the [template reference](../src/CoreEx.Template/README.md) for manual commands and options.

The layered scaffold retains `Test.Unit` for isolated shared-library logic and `Test.Common` for test support; the Functions addon supplies `Test.Functions` for host invocation/composition and host-local components. They complement one another. Standalone keeps its capability-local tests in `Test.Functions` without another test project.

## 3. Prepare the local infrastructure

Both solution shapes need the same local infrastructure, but their templates do not emit the same files:

| Selected shape | Template output | Action |
|---|---|---|
| Layered: `coreex` with messaging provider `ServiceBus` | Root `docker-compose.yml` with Service Bus, its SQL Server backing container, Redis and the dashboard; `servicebus\Config.json` starts with no entities. | Preserve the generated files and add Azurite. |
| Standalone: `coreex-solution` plus `coreex-functions --shared-core false` | No Compose file or Service Bus emulator configuration. | Create a minimal Compose stack and empty emulator configuration; do not run `coreex` over the standalone solution. |

Neither Functions host shape supplies trigger connections automatically. Compose owns the emulators; run Functions Core Tools and the mock backend on the host machine for this walkthrough. If you add Aspire later, let it reference the existing services rather than launching a second set of emulators on the same ports.

Ask your assistant to apply the scaffold skill's [local infrastructure recipe](../.github/skills/coreex-scaffold/references/local-infrastructure.md), which supplies the Compose, topology, safe setup-fixture and connection conventions:

```text
Use coreex-scaffold and its local infrastructure recipe to prepare Azurite and the Service Bus emulator for this integration. Show the plan and wait for approval.

We need catalog-imports and catalog-reports Blob containers, and one non-session catalog-item-upserts queue with a one-minute lock, ten maximum deliveries and a one-hour TTL. Keep queue topology in Test.Common and emulator JSON entity arrays empty.

Provide the recipe's guarded manual setup fixture and local settings using ImportStorage, CatalogImportReports and ServiceBus. Prepare files and commands only; I will start the emulators and run setup. Defer Functions capabilities, runtime host harnesses and the mock backend.
```

The setup uses `Test.Common\ServiceBus.cs` as the topology's single source of truth, rather than duplicating queue definitions in `Config.json`. Its deliberately selected `InfrastructureSetupTests` fixture prepares the queue without starting Functions. SQL Server backs the Service Bus emulator only; no application database is involved. Redis and the dashboard are optional and need not start.

**Queue setup deletes and recreates the specified entities, discarding messages.** Use an isolated local broker with Functions listeners and all other producers/consumers stopped. The recipe requires both explicit fixture selection and `CoreExInfrastructureSetup=true`; ordinary unit/invocation runs do not reset the broker. Production provisioning belongs in deployment infrastructure.

### Start the emulators and create the containers

From the solution root:

```powershell
podman compose config
podman compose up -d azurite db-sql-server servicebus-emulator
podman compose ps
podman compose logs servicebus-emulator
```

Use `docker` instead of `podman` if preferred. Container startup order is not readiness: wait for Service Bus to finish initializing and its management health endpoint at `http://localhost:5300/health` to report healthy before running the Functions host or tests. If the standard ports are already in use, change the published ports and all affected connection settings together.

Create the two application containers once Azurite is ready:

```powershell
az storage container create --name catalog-imports --connection-string "UseDevelopmentStorage=true"
az storage container create --name catalog-reports --connection-string "UseDevelopmentStorage=true"
```

Azurite's Blob, Queue and Table endpoints are exposed on their standard ports. Functions uses storage for its own host operations as well as the input Blob binding. The durable Azurite volume preserves local files across container recreation; it is not a production backup strategy.

### Prepare the local queue before starting Functions

Run only the prepared setup fixture against the ready emulator, with Functions stopped:

```powershell
$env:ConnectionStrings__ServiceBus = "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;"
$env:CoreExInfrastructureSetup = "true"
try {
    dotnet test tests\Northstar.Supplier.Catalog.Test.Functions --filter "FullyQualifiedName~InfrastructureSetupTests"
    if ($LASTEXITCODE -ne 0) { throw "Local queue setup failed." }
}
finally {
    Remove-Item Env:\CoreExInfrastructureSetup
}
```

The fixture must consume that opt-in and connection setting explicitly. The environment connection uses the standard `ConnectionStrings:ServiceBus` hierarchy for test administration, whereas the native Functions bindings below use a setting named `ServiceBus`. Supply the same broker endpoint to both; an environment variable in this terminal does not automatically configure Core Tools in another terminal. If you change ports, update both configurations and the administration port.

Run this preparation again after recreating the emulator or whenever you deliberately need a clean local queue. Do not run the whole test suite just to provision infrastructure. Production queue provisioning belongs in deployment infrastructure, not this setup fixture.

### Configure the Functions host

In `src\Northstar.Supplier.Catalog.Functions\local.settings.json`, preserve existing settings and add:

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "ImportStorage": "UseDevelopmentStorage=true",
    "ServiceBus": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;",
    "CatalogImportReports": "catalog-reports"
  }
}
```

`AzureWebJobsStorage` is Functions host storage; `ImportStorage` is the named connection for the Blob trigger and the input/report SDK clients; `ServiceBus` is the connection for both the queue trigger and publisher. `CatalogImportReports` is an application setting that the report adapter implemented in step 5 must consume. Use `Connection = "ImportStorage"` and `Connection = "ServiceBus"` on the respective native bindings, with queue name `catalog-item-upserts`.

These settings assume Core Tools runs on the host machine. `UseDevelopmentStorage=true` and `localhost` do not point to sibling containers from a container-hosted Function; that topology needs explicit storage endpoints and container-addressable broker settings.

Bindings read Functions host settings, not just worker `appsettings.json`. Separately configure the mock backend's actual base URL using the typed HTTP client's configuration convention when implementing step 6. Keep `local.settings.json` ignored and unpublished; use deployment app settings and managed identity where supported in Azure. Do not start the Functions host until its capabilities and mock backend are implemented.

## 4. Define the supplier file and internal command

Create a `data` folder in the solution root (alongside `docker-compose.yml`) and save this example as `data\catalog.csv` for later upload. This is sample input data, not generated code or test seed data:

```csv
supplier-code,supplier-sku,description,unit,currency,unit-price,effective-date
ACME,AC-100,Stainless steel bolt,EA,USD,1.25,2026-11-01
ACME,AC-200,Steel washer,EA,USD,0.10,2026-11-01
```

Use one row per item, an exact column-name header, `yyyy-MM-dd` dates, and invariant decimal values. Require all fields; price may be zero but not negative. Limit codes and descriptions to practical lengths, and constrain currency to exactly one of `USD`, `AUD`, `NZD`, or `GBP` using the property's `options` rule in `catalog-file.yaml`. Options are matched as supplied, not canonicalized, so `usd` is rejected rather than uppercased. Duplicate `(SupplierCode, SupplierSku, EffectiveDate)` keys within a file should reject the file during reconciliation rather than let concurrent queue consumers decide which row wins. File contents are business data, not trusted identity.

Use the kebab-case headers as each property's `name` in `catalog-file.yaml`, without a `property` override. File CodeGen automatically derives PascalCase C# properties: `supplier-code` becomes `SupplierCode`, `supplier-sku` becomes `SupplierSku`, `unit-price` becomes `UnitPrice`, and `effective-date` becomes `EffectiveDate`. The CSV headers remain unchanged; the internal command and backend DTO use their own C# property names.

The internal `CatalogItemUpsert` command should carry the seven business fields and a stable command ID derived from the credential-free source identity, selected Blob revision, and row ordinal. Use an unambiguous canonical encoding and a bounded hash for the ID; don't put a SAS token or a transient invocation ID in it. The ID is for correlation and duplicate detection, not a guarantee of broker deduplication.

Ask your assistant:

```text
Plan and wait for approval, then use coreex-file to add a checked-in catalog-file.yaml and generated CSV processor for the Northstar supplier price book in the guide.

Generate into the Functions runtime project, using an exact column header, ValidateThenExecute and CollectErrors. Use the source names supplier-code, supplier-sku, description, unit, currency, unit-price and effective-date in the YAML; omit property overrides to use the automatic PascalCase C# naming. Validate the required fields, nonnegative decimal price, currency restricted by YAML options to USD, AUD, NZD or GBP, and yyyy-MM-dd date. Reject duplicate SupplierCode/SupplierSku/EffectiveDate keys at EOF. The per-invocation duplicate-key set is application-owned; explicitly document that it grows with the number of distinct rows and that the caller owns input-size and concurrency limits.

Generate the wire types, mapping, validators, processor, context and create-only handler. Do not hand-edit generated files. The scoped handler maps an accepted row into an internal CatalogItemUpsert command and delegates publication through an application abstraction, or a capability-local abstraction for standalone. Derive a stable bounded command ID from credential-free source identity, selected Blob revision and row ordinal, not the invocation ID. Propagate cancellation and failures. Add focused tests for valid data, a late invalid row, an unsupported currency, duplicate keys and mapping.

Stop at the command-publishing abstraction and use a test double to capture commands in tests. Do not implement or register the Service Bus publisher or any native triggers yet; step 5 adds the real publisher and Blob trigger.
```

The duplicate-key check is a deliberate simplicity tradeoff: it retains one key per row for the duration of validation. For large feeds, use an application-owned bounded or durable strategy and decide admission limits explicitly. CoreEx.IO does not impose file-size or diagnostic-count quotas.

Optional, as the underlying skills should have executed the code-generation. From the CodeGen project directory, validate and generate the definition, then confirm a second generation is unchanged:

```powershell
Set-Location tools\Northstar.Supplier.Catalog.CodeGen
dotnet run -- file --validate
dotnet run -- file
dotnet run -- file --expect-no-changes
Set-Location ..\..
```

Running `dotnet run --project ...` from the solution root does not change the directory used for definition discovery. Regenerate after changing the YAML; preserve the handwritten handler and never edit generated `.g.cs` files.

## 5. Receive the Blob and publish commands

The Blob Function is a thin native trigger. Select the current Blob revision when processing begins, and pin **both** validation and replay reads to the selected ETag. If the Blob changes between reads, fail operationally rather than process different bytes. This policy processes the revision current at invocation time; it does not guarantee processing a historical revision associated with an older notification. Use Blob versions or snapshots and a revision-aware source contract when every historical revision matters.

Reference `CoreEx.Azure.Storage.Blobs` in the Functions host and add its namespace to `GlobalUsing.cs`. Use `BlobReadSource.CreateAsync` once and pass `source.OpenReadAsync` to the file processor; `source.SourceUri.AbsoluteUri` is credential-free and `source.ETag.ToString()` identifies the selected revision. For reports, use `BlobJsonlReport` with a name derived from definition/version/source/revision/attempt, then await `StartAsync`, individual `WriteAsync` calls and `CompleteAsync(summary, outcome)`. Keep file-specific record shapes in a thin adapter. The helper supplies conditional reads, create-only output, compact JSONL and explicit terminal metadata; it does not provision containers, choose admission limits, or make publishing transactional. Report failures propagate; an ambiguous write faults the instance instead of being blindly appended again.

Create a fresh report attempt ID for every processing attempt, including retries of the same Blob revision. Reusing an attempt ID conflicts with create-only reports; do not overwrite or resume an earlier report. Command IDs remain stable for the same source identity, revision and row ordinal, independently of the report attempt ID.

Keep JSONL for this walkthrough's mixed lifecycle records: completed lines remain readable after failure, although the final line may be incomplete. A streamed JSON array/object is not a complete JSON document until its closing syntax is written; plain JSON is better suited to a finished summary. If suppliers also need a flat rejection export, `BlobCsvReport` supports explicit columns such as `row-number,supplier-code,supplier-sku,field,error-code,message`, invariant scalar formatting and standard CSV quoting. Its constructor requires a `CsvFormulaHandling` choice; quoting alone does not prevent spreadsheet formulas. StartAsync writes one header and CompleteAsync(outcome) updates metadata without inserting a summary row. That business-facing CSV is optional, not another required capability in this walkthrough.

Use `ValidateThenExecute`: all rows and EOF reconciliation must pass before publishing starts. Stream diagnostics to a durable, attempt-specific Blob report; don't retain an unbounded error history. A content-failed report is a handled rejection, not a successful import. A `Succeeded` import report means all row commands were published, not that the backend applied them; verify downstream processing separately. Let storage, reporting, broker, and cancellation failures propagate so the host can apply its retry policy.

Use CloudEvents 1.0 **binary mode** for the queue command: the body is the `CatalogItemUpsert` JSON and the CloudEvent metadata is carried separately in Service Bus properties. Binary describes the envelope layout, not a binary encoding of the JSON. This remains a command addressed to one queue, not a broadcast domain event.

| Wire contract | Value |
|---|---|
| CloudEvent `id` / Service Bus `MessageId` | The stable command ID derived in step 4. |
| CloudEvent `source` | `urn:northstar:supplier:catalog` (the logical producer; credential-free Blob identity remains an input to the command ID). |
| CloudEvent `type` | `northstar.catalog.item.upsert.v1`. |
| CloudEvent `datacontenttype` / Service Bus `ContentType` | `application/json`. |
| CloudEvent `data` / Service Bus body | The `CatalogItemUpsert` command, without a structured CloudEvent JSON wrapper. |

Use CoreEx's existing CloudEvent-to-Service-Bus mapping rather than hand-building transport properties. Explicitly select `ContentMode.Binary`; `ServiceBusPublisher` defaults to structured mode. Keep CloudEvents and transport concerns behind the publishing abstraction, not in the command contract.

```text
Plan and wait for approval, then use coreex-functions and coreex-file to connect the catalog processor to a native Blob-triggered Function for the catalog-imports container.

Reuse CoreEx.Azure.Storage.Blobs in the Functions host: select once with BlobReadSource.CreateAsync, use SourceUri.AbsoluteUri and ETag.ToString() for source/revision identity, and pass source.OpenReadAsync to the processor. Create a fresh report attempt ID for every processing attempt, including retries, while keeping command IDs stable for the same source/revision/row. Wrap BlobJsonlReport in a thin file-specific report adapter using CreateName, StartAsync, awaited WriteAsync diagnostics and CompleteAsync for the terminal summary/outcome in a separate reports container. Do not duplicate its SDK mechanics, create snapshots/containers during invocation, or retry ambiguous report appends. Content failure must produce no queue sends; operational failures and failed publication must propagate. Succeeded describes completed command publication, not downstream backend completion.

Use the infrastructure and local settings prepared in step 3: ImportStorage for Blob access, CatalogImportReports for the report container, and ServiceBus for the publisher. Do not create another emulator stack or provision resources during invocation.

Implement a Service Bus queue publisher behind the command-publishing abstraction. Publish one CloudEvents 1.0 binary-mode message per accepted row to catalog-item-upserts and await each send. Use the existing CoreEx mapping with ContentMode.Binary (not the default structured mode), source urn:northstar:supplier:catalog, type northstar.catalog.item.upsert.v1, and datacontenttype application/json. Set the stable command ID as both CloudEvent id and Service Bus MessageId; the body is the CatalogItemUpsert JSON without a structured CloudEvent wrapper. Do not hand-build CloudEvent transport properties or describe the queue send as transactional with Blob processing.

Add invocation and component tests for ETag-pinned replay, invalid-file rejection, reports, cancellation and send failures, including a failure after an earlier send. Assert the binary wire contract, stable identity and successful reconstruction using CoreEx's receiver conversion.
```

Whole-file acceptance is **not** an atomic multi-message send. If publishing fails after one or more sends, retrying the Blob may send those commands again. A stable `MessageId` alone does not ensure broker deduplication; its behavior depends on queue configuration and its duplicate-detection window. Downstream operations must tolerate redelivery.

## 6. Consume the queue and call the mock backend

For the exercise, the fictional backend accepts an OData-style keyed replacement:

```http
PUT /odata/CatalogItems(SupplierCode='ACME',SupplierSku='AC-100',EffectiveDate=2026-11-01)
Content-Type: application/json

{
  "SupplierCode": "ACME",
  "SupplierSku": "AC-100",
  "Name": "Stainless steel bolt",
  "UnitOfMeasure": "EA",
  "Price": { "Amount": 1.25, "CurrencyCode": "USD" },
  "EffectiveDate": "2026-11-01"
}
```

The mock defines this particular key and `PUT` as create-or-replace; do not assume every OData service supports upsert or uses this URL or payload. Escape OData string literals and URI components correctly. Repeating the same request has the same final state in this mock. The command's description, unit, price, and currency map to different backend field names and a nested `Price` object; don't leak the backend DTO into the internal command.

```text
Plan and wait for approval, then use coreex-functions and coreex-adapter to implement the queue consumer and mocked catalog backend described in the guide.

Use a native Service Bus trigger on catalog-item-upserts delegating to a ServiceBusSubscriberBase<CatalogItemUpsert> implementation through subscriber.FunctionReceiveAsync. Keep its default DataRepresentation of MessageRepresentation.CloudEvent and explicitly set ContentMode.Binary. Use the inherited ValueValidator and error-classification pipeline, without SubscribedManager, [Subscribe] dispatch or a competing receiver. The typed subscriber decodes the envelope and deserializes its data; validate source urn:northstar:supplier:catalog, type northstar.catalog.item.upsert.v1 and datacontenttype application/json through args.CloudEvent, and require the envelope id to match the command's stable ID before calling the application service. Do not bypass envelope validation by deserializing only the message body.

Delegate processing to an application service and an infrastructure adapter/typed HTTP client, or capability-local equivalents in the standalone shape. Map CatalogItemUpsert to the documented OData-style keyed PUT and safely escape key values. Configure the backend base URL; do not call HttpClient directly from the trigger or application service.

Choose and document an explicit invalid-message, permanent-backend-failure and transient-failure policy using InvalidDataHandling for malformed envelopes/payloads and ErrorHandler for validation/application failures. Retry transient errors such as 408, 429 and 5xx; treat authentication/configuration failures as operational failures, not invalid customer messages. Disable auto-completion for the CoreEx FunctionReceiveAsync bridge, configure classification deliberately, and test exact settlement. Do not start a competing receiver.

Provide a local mock that records requests and implements the documented create-or-replace behavior. Test mapping and escaping, repeated delivery, a transient failure followed by retry, invalid messages (including missing/wrong CloudEvent metadata and mismatched command identity) and the chosen settlement behavior. Do not claim real OData conformance or exactly-once processing.
```

This replacement operation makes identical redeliveries harmless in the mock, but it does not resolve conflicting updates with the same key: Service Bus consumers can process them out of order. A real backend needs an explicit ordering/version or concurrency policy and an idempotency contract covering ambiguous HTTP outcomes. The integration must not report delivery as successful merely because a message was published.

## 7. Run locally and prove the flow

Ask your assistant to prepare the local topology and a reproducible runtime test:

```text
Plan and wait for approval, then configure a local end-to-end test of the Northstar flow using the Compose-managed Azurite and Service Bus emulator prepared in step 3, Functions Core Tools and the mock backend. Verify the input/report containers exist. Use the shared Test.Common ServiceBus queue definition and reset only the isolated test entities after broker readiness and before starting Functions listeners; keep Config.json entity arrays empty. Require stopped manual hosts and no concurrent producers/consumers when using the walkthrough's fixed queue. Keep Compose as the infrastructure owner; any Aspire orchestration must reference these services, not launch duplicates. Explicitly configure test administration, Functions host bindings and the publisher against the intended broker; keep local credentials out of source control. Leave the separately opted-in InfrastructureSetupTests disabled in ordinary test runs.

Upload the guide's two-row CSV, trigger the real Blob binding, and wait for two keyed PUTs at the mock. Assert the Succeeded publication report and independently verify the exact mapped backend payloads, with bounded timeouts. Also exercise a late invalid row and assert a content-failed report and no sends; exercise a transient backend failure and redelivery. Include a repeated delivery case that leaves the same backend state. Run the generated-consumer build, focused tests and runtime test, and report any infrastructure or delivery boundary that could not be exercised.
```

For a manual run, prepare the queue with the filtered setup fixture from step 3 while Functions is stopped, then start the mock backend with its documented command and configure its base URL. In a separate terminal, build and start the Functions host:

```powershell
dotnet build Northstar.Supplier.Catalog.slnx
Set-Location src\Northstar.Supplier.Catalog.Functions
func start
```

Once the listeners are ready, upload the example from the solution root in another terminal:

```powershell
az storage blob upload --container-name catalog-imports --name catalog.csv --file .\data\catalog.csv --connection-string "UseDevelopmentStorage=true"
```

The empty emulator configuration does not create the queue: the local setup fixture or isolated runtime-test setup must do that before listeners start. Azurite does not emit Event Grid events. If using an Event Grid-backed Blob trigger locally, upload the Blob **and** simulate the storage-shaped event through the Functions webhook, as in the [inventory runtime example](../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md). Alternatively, deliberately configure and test a supported polling trigger. A simulated webhook proves neither Azure Event Grid provisioning nor cloud event delivery.

At minimum, check:

1. The generated consumer compiles, and generation is unchanged on a second run.
2. The valid file yields two messages and a `Succeeded` publication report; independently verify two correctly mapped mock requests and the resulting backend state.
3. A late invalid row or duplicate business key produces a content-failed report and **zero** messages.
4. A send failure after a prior send and a transient backend failure are visible and retryable; repeated delivery does not change the mock's final state.
5. Real local Blob and broker delivery have been exercised separately from in-process invocation tests. Record any unverified boundary rather than treating a mock-only test as end-to-end.

## Production decisions

- **Delivery and idempotency:** This walkthrough has no application database or transactional outbox. For stronger guarantees, introduce a durable receipt and outbox in an application's supported transaction, a relay, and a backend idempotency/version contract. Even then, external HTTP effects and message delivery are not magically exactly-once.
- **Admission and observability:** Set file-size, concurrency, timeout, report-retention, alerting, dead-letter and replay policies. Reports can contain sensitive business data; control access and retention.
- **Revision and ordering:** Decide whether every Blob revision must be processed, how conflicting price-book revisions are ordered, and how the backend rejects stale updates.
- **Deployment:** Provision Blob containers, Event Grid subscriptions if used, Service Bus queue, credentials and least-privilege permissions. Configure Functions host binding settings separately from worker-only configuration. Local runtime tests do not establish cloud provisioning, authentication or delivery.

For the underlying patterns, see the [CoreEx.IO guide](../src/CoreEx.IO/README.md), [Blob integration guide](../src/CoreEx.Azure.Storage.Blobs/README.md), [File CodeGen guide](../src/CoreEx.CodeGen/Files/README.md), and [Functions guide](../src/CoreEx.Azure.Functions/README.md). The [inventory example](../tests/CoreEx.Azure.Functions.TestApp/InventoryImports/README.md) demonstrates ETag-pinned file processing and a durable SQL/outbox boundary; it is a different integration, not an implementation of this tutorial.
