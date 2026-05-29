# Add Capability Workflow

## Step 1: Load Context

Before making changes, load:

1. Instruction files in `/.github/instructions/`:
   - `coreex-host-setup.instructions.md`
   - `coreex-event-subscribers.instructions.md`
   - `coreex-application-services.instructions.md`
   - `coreex-tooling.instructions.md`

2. Sample host wiring — read both engines for comparison:
   - SQL Server (Shopping): `samples/src/Contoso.Shopping.Api/Program.cs`, `samples/src/Contoso.Shopping.Subscribe/Program.cs`, `samples/src/Contoso.Shopping.Outbox.Relay/Program.cs`
   - PostgreSQL (Products): `samples/src/Contoso.Products.Api/Program.cs`, `samples/src/Contoso.Products.Subscribe/Program.cs`, `samples/src/Contoso.Products.Outbox.Relay/Program.cs`

3. Domain templates under `/.github/templates/domain/` — use the engine-specific subdirectory (`sqlserver/` or `postgres/`) that matches the domain being retrofitted.

## Step 2: Inspect Domain State

Determine current shape before proposing changes.

Inspect for:
- Domain boundary and project names (`{Solution}.{Domain}.*`)
- Existing hosts: `*.Api`, `*.Outbox.Relay`, `*.Subscribe`
- **Database engine**: presence of `Microsoft.Data.SqlClient` / `CoreEx.Database.SqlServer` / `Aspire.Microsoft.Data.SqlClient` (SQL Server) vs `Npgsql` / `CoreEx.Database.Postgres` / `Aspire.Npgsql.*` (PostgreSQL)
- Outbox wiring in API host: `AddSqlServerOutboxPublisher()` or `AddPostgresOutboxPublisher()`
- Outbox infrastructure in Database project: outbox migration file + `dbex.yaml` with `outbox: true`
- Messaging support: `CoreEx.Azure.Messaging.ServiceBus`, `AddAzureServiceBusClient`, `AddAzureServiceBusPublisher`, `AddSubscribedManager`, `AzureServiceBusReceiving`
- Reference data presence: `ReferenceDataService`, `AddReferenceDataOrchestrator`, `*.CodeGen` project
- Existing telemetry: `WithCoreExSqlServerTelemetry` / `WithCoreExPostgresTelemetry`, `WithCoreExServiceBusTelemetry`
- Integration-event semantics: subjects, subscriber classes, related service methods

Use conservative detection. Ask if ambiguous. See [`messaging-retrofit-checkpoints.md`](messaging-retrofit-checkpoints.md) for detection signals.

## Step 3: Clarify User Intent

Ask only what cannot be inferred from inspection:

- Which domain to retrofit?
- Which capability: relay only, subscribe only, both, or subscriber classes only?
- **Database engine** — SQL Server or PostgreSQL? (default SQL Server if not determinable)
- If adding subscribe: does the domain use reference data? (`ReferenceDataService` / `*.CodeGen` project present)
- If adding subscribers: what subjects and payload contracts?
- Infrastructure/host wiring only, or also application-facing handlers?

## Step 4: Choose Retrofit Mode

### Mode A — Add Outbox.Relay

Use when the domain writes data and should publish integration events reliably.

Expected work:
- Create `*.Outbox.Relay` project if missing (use `/.github/templates/domain/Outbox.Relay/<engine>/` as reference)
- Add packages: `CoreEx.AspNetCore`, `CoreEx.Azure.Messaging.ServiceBus`, engine database package (`CoreEx.Database.SqlServer` or `CoreEx.Database.Postgres`), Aspire client package, OpenTelemetry packages
- Add `<RuntimeHostConfigurationOption Include="Azure.Experimental.EnableActivitySource" Value="true" Trim="true" />` to csproj
- Wire relay `Program.cs`:
  - **SQL Server**: `AddSqlServerClient` → `.AddSqlServerDatabase().AddSqlServerUnitOfWork().AddSqlServerOutboxRelay()` → `AddSqlServerOutboxRelayHostedService()`; telemetry: `WithCoreExSqlServerTelemetry()`
  - **PostgreSQL**: `AddAzureNpgsqlDataSource` → `.AddPostgresDatabase().AddPostgresUnitOfWork().AddPostgresOutboxRelay()` → `AddPostgresOutboxRelayHostedService()`; telemetry: `WithCoreExPostgresTelemetry()`
  - Both engines: `AddAzureServiceBusClient` + `AddAzureServiceBusPublisher(o => o.SessionIdStrategy = ...)`, `AddHostedServiceManager()`, `app.MapHostedServices()`; telemetry also includes `WithCoreExServiceBusTelemetry()`
- Ensure API host has `AddSqlServerOutboxPublisher()` / `AddPostgresOutboxPublisher()` (no generic type parameter)
- Ensure Database project has outbox migration file and `dbex.yaml` with `outbox: true` and `outboxName: outbox`

### Mode B — Add Subscribe

Use when the domain must consume integration events from other services.

Expected work:
- Create `*.Subscribe` project if missing (use `/.github/templates/domain/Subscribe/<engine>/` and `Subscribe/_shared/` as reference)
- Add packages: `CoreEx.AspNetCore`, `CoreEx.Azure.Messaging.ServiceBus`, engine EF package, Aspire client + Redis packages, OpenTelemetry packages
- Add `<RuntimeHostConfigurationOption Include="Azure.Experimental.EnableActivitySource" Value="true" Trim="true" />` to csproj
- Wire subscribe `Program.cs`:
  - **SQL Server**: `AddSqlServerClient` → `.AddSqlServerDatabase().AddSqlServerUnitOfWork().AddSqlServerOutboxPublisher().AddSqlServerEfDb<{Domain}EfDb>()`; telemetry: `WithCoreExSqlServerTelemetry()`
  - **PostgreSQL**: `AddAzureNpgsqlDataSource` → `.AddPostgresDatabase().AddPostgresUnitOfWork().AddPostgresOutboxPublisher().AddPostgresEfDb<{Domain}EfDb>()`; telemetry: `WithCoreExPostgresTelemetry()`
  - Both engines: Redis + FusionCache wiring, `AddAzureServiceBusClient`, `AddSubscribedManager(...)`, `AzureServiceBusReceiving().WithSessionReceiver(...).WithSubscribedSubscriber().WithHostedService().Build()`, `AddHostedServiceManager()`, `app.MapHostedServices()`; telemetry includes `WithCoreExServiceBusTelemetry()`
  - **If reference data present**: include `AddReferenceDataOrchestrator<ReferenceDataService>()` in service registration
- Add subscriber classes inheriting `SubscribedBase<T>` with `[ScopedService]` and `[Subscribe("...")]`
- Subscriber logic delegates to Application services — no business logic in subscriber classes

### Mode C — Add Both Relay and Subscribe

Apply Mode A then Mode B. Confirm both are needed before creating two new host projects.

### Mode D — Add Subscribers to Existing Subscribe Host

Use when the Subscribe host exists but subscriber classes, registration, or error handling are incomplete.

Expected work:
- Add missing subscriber classes (inherit `SubscribedBase<T>`, `[ScopedService]`, `[Subscribe("...")]`)
- Register with `AddSubscribersUsing<T>()` in `AddSubscribedManager`
- Delegate to existing Application service methods
- Do not re-create host wiring that already exists

## Step 5: Apply Incremental Changes

Prefer targeted edits over regeneration.

Rules:
1. Reuse existing project naming and layering — never invent new conventions
2. Do not duplicate wiring that already exists
3. Keep subscriber logic thin; delegate to Application services
4. Preserve host middleware order and telemetry conventions
5. Match the engine already in use — do not introduce a second database engine
6. Use domain templates only for missing pieces; apply only the engine-specific subdirectory that matches
7. If the domain shape is inconsistent with CoreEx conventions, stop and explain blockers before changing anything

## Step 6: Validate

Run [`messaging-retrofit-checklist.md`](messaging-retrofit-checklist.md) as the completion gate.

Minimum criteria:
- `Program.cs` files follow host setup conventions for the correct engine
- Required package/project references present including `RuntimeHostConfigurationOption`
- Relay: outbox migration and `dbex.yaml outbox: true` exist; relay hosted service registered
- Subscribe: subscribers inherit `SubscribedBase<T>`; `AddHostedServiceManager()` and `MapHostedServices()` present
- Telemetry uses correct engine + Service Bus split names
- Clean build; no compiler errors or nullable warnings
