# Messaging Retrofit Checklist

Use this checklist as the completion gate for `/add-capability` messaging and integration retrofits.

## Discovery

- [ ] Identified the target domain and its existing project/host shape.
- [ ] Determined which of Api, Database, Outbox.Relay, and Subscribe projects already exist.
- [ ] Identified the database engine in use: **SQL Server** or **PostgreSQL**.
- [ ] Determined whether outbox infrastructure (migration file + `dbex.yaml outbox: true`) is already present.
- [ ] Determined whether Azure Service Bus wiring is already present or missing.
- [ ] Determined whether reference data is present (`ReferenceDataService` / `*.CodeGen` project).
- [ ] Confirmed any choices that could not be inferred safely (engine, subjects, payloads).

## Project and Package Alignment

- [ ] Added only the missing projects required by the requested retrofit.
- [ ] Added only the missing package and project references for the affected hosts.
- [ ] `<RuntimeHostConfigurationOption Include="Azure.Experimental.EnableActivitySource" Value="true" Trim="true" />` present in csproj for every new Relay and Subscribe project.
- [ ] Preserved existing layered references and naming conventions.
- [ ] No second database engine introduced — all new wiring uses the engine already in the domain.

## Relay Retrofit (Mode A)

- [ ] `*.Outbox.Relay` project created or aligned.
- [ ] Relay `Program.cs` database wiring is engine-correct:
  - SQL Server: `AddSqlServerClient` → `.AddSqlServerDatabase().AddSqlServerUnitOfWork().AddSqlServerOutboxRelay()` → `AddSqlServerOutboxRelayHostedService()`
  - PostgreSQL: `AddAzureNpgsqlDataSource` → `.AddPostgresDatabase().AddPostgresUnitOfWork().AddPostgresOutboxRelay()` → `AddPostgresOutboxRelayHostedService()`
- [ ] Service Bus publisher wired: `AddAzureServiceBusClient` + `AddAzureServiceBusPublisher(o => o.SessionIdStrategy = ServiceBusSessionStrategy.UsePartitionKeyConvertedToAnId)`
- [ ] `AddHostedServiceManager()` registered in services.
- [ ] `app.MapHostedServices()` called in middleware pipeline.
- [ ] Telemetry uses split names: `WithCoreExSqlServerTelemetry()` or `WithCoreExPostgresTelemetry()` **and** `WithCoreExServiceBusTelemetry()`.
- [ ] API host has outbox publisher wiring (no generic type parameter):
  - SQL Server: `AddSqlServerOutboxPublisher()`
  - PostgreSQL: `AddPostgresOutboxPublisher()`
- [ ] Database project has outbox migration file and `dbex.yaml` contains `outbox: true` and `outboxName: outbox`.

## Subscribe Retrofit (Mode B)

- [ ] `*.Subscribe` project created or aligned.
- [ ] Subscribe `Program.cs` database wiring is engine-correct:
  - SQL Server: `AddSqlServerClient` → `.AddSqlServerDatabase().AddSqlServerUnitOfWork().AddSqlServerOutboxPublisher().AddSqlServerEfDb<{Domain}EfDb>()`
  - PostgreSQL: `AddAzureNpgsqlDataSource` → `.AddPostgresDatabase().AddPostgresUnitOfWork().AddPostgresOutboxPublisher().AddPostgresEfDb<{Domain}EfDb>()`
- [ ] Redis + FusionCache wiring present: `AddRedisDistributedCache` + `AddFusionCache().WithDistributedCache().WithStackExchangeRedisBackplane()`.
- [ ] If reference data present: `AddReferenceDataOrchestrator<ReferenceDataService>()` included.
- [ ] Service Bus receiver wired: `AddAzureServiceBusClient`, `AddSubscribedManager(...)`, `AzureServiceBusReceiving().WithSessionReceiver(...).WithSubscribedSubscriber().WithHostedService().Build()`.
- [ ] `AddHostedServiceManager()` registered in services.
- [ ] `app.MapHostedServices()` called in middleware pipeline.
- [ ] Telemetry uses split names: `WithCoreExSqlServerTelemetry()` or `WithCoreExPostgresTelemetry()` **and** `WithCoreExServiceBusTelemetry()`.
- [ ] Subscriber classes inherit `SubscribedBase<T>` (generic).
- [ ] Subscriber classes carry `[ScopedService]` and `[Subscribe("...")]` attributes.
- [ ] Subscriber logic delegates to Application services — no business logic embedded in subscriber classes.

## Host and Convention Alignment

- [ ] Middleware order follows repo conventions for each host type.
- [ ] Dynamic service registration (`AddDynamicServicesUsing<T>`) used where already established in the domain.
- [ ] Health endpoints (`MapHealthChecks("/health")`) present in all new host `Program.cs` files.
- [ ] OpenTelemetry wiring preserves or aligns existing telemetry across the affected hosts.

## Validation

- [ ] All affected projects build cleanly with no compiler errors or nullable warnings.
- [ ] Any related tests were added or updated where practical.
- [ ] The final summary distinguishes completed retrofits from any blocked or deferred items.
- [ ] Any remaining user decisions are listed explicitly as follow-up items.
