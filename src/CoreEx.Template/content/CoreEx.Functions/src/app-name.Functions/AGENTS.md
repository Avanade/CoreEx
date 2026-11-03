# app-name.Functions

<!-- #if shared-core -->
This project is one independently deployable Azure Functions isolated-worker app. It shares the `solution-name.Application` and `solution-name.Infrastructure` libraries; other hosts must not reference this host.
<!-- #else -->
This project is a standalone Azure Functions isolated-worker app with no shared Application/Infrastructure/Contracts project dependencies. Its host and test projects define their own package versions; no root `coreex` scaffold is required. Other hosts must not reference this host.
<!-- #endif -->

The scaffold intentionally contains no functions, health endpoint, dependency connections or trigger-specific registrations. Its corresponding `host-test-name` project is wired for UnitTestEx invocation testing but contains no invented test cases. Add only the capabilities the workload needs. An empty app can report "No job functions found" until its first trigger is added.

## Adding a capability

Use `/coreex-functions` from the version-pinned `coreex-ai` asset bundle for capability setup and tests. Its scoped `.github/instructions/coreex-functions.instructions.md` defines Functions-specific boundaries; do not apply ASP.NET host startup recipes to the worker.

Group entry points by business capability, for example `Imports/ImportFunction.cs` or `Products/ProductFunctions.cs`. Normally use one trigger class per file; related HTTP operations may share a class. Keep transport-local DTOs, binding constants and mappings beside the functions that use them. Do not create empty capability folders or a speculative `Shared` folder.

Keep trigger methods thin and delegate processing to focused handlers/services. Add the required binding package, DI registrations, configuration and UnitTestEx invocation tests together. Mirror capability folders in `host-test-name`; its name is derived from this host, not the shared solution prefix, so multiple Functions apps have independent test projects.

<!-- #if shared-core -->
The shared libraries are referenced but their services are not registered automatically. When a capability needs them, configure its dependencies and register its services, using `AddDynamicServicesUsing(typeof(solution-name.Application.AssemblyMarker).Assembly, typeof(solution-name.Infrastructure.AssemblyMarker).Assembly)` when all discovered services' dependencies are available. Scanning everything before wiring the required database/cache/adapters can prevent the worker from starting even when no function uses those services.
<!-- #else -->
Keep capability-local processing, transport contracts and mappings beside the entry points, for example `Imports/BlobForwarder.cs`. Use native bindings/SDK clients or CoreEx primitives where useful; do not require CloudEvents for a transport bridge. Extract libraries only when actual reuse or complexity warrants them, not merely to satisfy a folder diagram. Shared libraries are optional, but separation of concerns and meaningful tests are not.

A Blob-to-Service-Bus bridge is not atomic: sending successfully followed by an invocation failure can cause duplicate sends on retry. Choose stable message identity and duplicate-tolerant processing; use durable state/outbox only when delivery requirements justify it.
<!-- #endif -->

For HTTP, call `builder.ConfigureFunctionsWebApplication()` immediately after `FunctionsApplication.CreateBuilder(args)`, before `UseCoreExExecutionContext()`. Register `AddMvc()` and `AddMvcWebApi()` and use `CoreEx.AspNetCore.Mvc.WebApi` with native `HttpRequest`/`IActionResult` triggers. Do not copy ASP.NET Core endpoint routing, `MapHealthChecks`, middleware or controller discovery into the Functions worker.

Functions owns triggers, listener lifecycle, binding conversion, retries and scaling. Add native Service Bus/Blob/Timer bindings only when needed; do not start a competing CoreEx receiver in this host. Service Bus functions using CoreEx's `FunctionReceiveAsync` bridge must set `AutoCompleteMessages = false` and choose their poison-message policy explicitly.

## Configuration and verification

`appsettings.json` configures the worker, not the Functions host's listeners. Binding settings must be available to the Functions host through app settings or `local.settings.json`. Local settings are ignored by Git and never published; keep a credential-free example or document required setting names when adding a real dependency. No storage emulator is assumed by the empty scaffold.

Choose telemetry/exporters and health probes when there is an operational requirement; the scaffold does not configure them. A custom health function is optional, not proof that non-HTTP listeners or dependencies are healthy.

Build the host and test project by path after scaffolding. If using a solution file, add them to its `hosts` and `tests` folders respectively. Add meaningful UnitTestEx invocation tests with the first real function. Invocation tests do not prove binding conversion, broker delivery or deployed authorization; use Core Tools/Aspire runtime tests where those boundaries matter.
