# Messaging Retrofit Checkpoints

Use these checkpoints when inspecting an existing domain before adding messaging and integration capabilities.

## 1. Domain Shape Detection

Look for these project patterns first:

- `{Solution}.{Domain}.Contracts`
- `{Solution}.{Domain}.Application`
- `{Solution}.{Domain}.Infrastructure`
- `{Solution}.{Domain}.Database`
- `{Solution}.{Domain}.Api`
- `{Solution}.{Domain}.Outbox.Relay`
- `{Solution}.{Domain}.Subscribe`

If the domain does not follow a recognizable CoreEx-style layered shape, treat the retrofit as ambiguous and ask before proceeding.

## 2. Database Engine Detection

Determine the engine before choosing any wiring. Look for package references in `*.csproj` files and `Program.cs` registration calls:

| Signal | Engine |
|--------|--------|
| `Aspire.Microsoft.Data.SqlClient`, `CoreEx.Database.SqlServer`, `Microsoft.EntityFrameworkCore.SqlServer` | SQL Server |
| `Aspire.Npgsql.*`, `CoreEx.Database.Postgres`, `Npgsql.EntityFrameworkCore.PostgreSQL` | PostgreSQL |
| `AddSqlServerClient(...)`, `AddSqlServerDatabase()`, `UseSqlServer(...)` | SQL Server |
| `AddAzureNpgsqlDataSource(...)`, `AddPostgresDatabase()`, `UseNpgsql(...)` | PostgreSQL |

If both signals appear, ask before proceeding — do not assume.

## 3. Host Detection Signals

| Host / capability | Evidence to inspect | Positive signal |
|---|---|---|
| API host | `Program.cs`, controllers, `*.Api.csproj` | `AddMvcWebApi`, `AddHttpWebApi`, controllers, OpenAPI setup |
| Outbox publisher in API | API `Program.cs` | `AddSqlServerOutboxPublisher()` or `AddPostgresOutboxPublisher()` (no generic type parameter) |
| Relay host | `*.Outbox.Relay/Program.cs`, relay csproj | Engine relay registration + `AddSqlServerOutboxRelayHostedService()` / `AddPostgresOutboxRelayHostedService()` |
| Subscribe host | `*.Subscribe/Program.cs`, `Subscribe/**/*.cs` | `AddSubscribedManager`, `AzureServiceBusReceiving`, `AddHostedServiceManager`, `MapHostedServices`, subscriber classes |
| Service Bus support | `Program.cs`, csproj references | `AddAzureServiceBusClient("ServiceBus")`, `CoreEx.Azure.Messaging.ServiceBus` |
| Telemetry alignment | `Program.cs` | `WithCoreExSqlServerTelemetry` / `WithCoreExPostgresTelemetry` **and** `WithCoreExServiceBusTelemetry` |
| Reference data | Application layer, `Program.cs` | `ReferenceDataService`, `AddReferenceDataOrchestrator`, `*.CodeGen` project |

## 4. Database and Outbox Detection

When adding a relay or reliable publication support, inspect for:

- `*.Database` project with a `dbex.yaml` file.
- `dbex.yaml` contains `outbox: true` and `outboxName: outbox`.
- An outbox migration file (e.g. `*-000005-create-{domainKebab}-outbox.sql` or `.pgsql`).

If the relay is requested and these assets are missing, plan to add the outbox migration and update `dbex.yaml`. Do not hand-write stored procedures — DbEx generates the outbox infrastructure from `dbex.yaml`.

## 5. Subscriber Detection

Inspect subscriber code for:

- Inheritance from `SubscribedBase<T>` (generic — the type parameter is the payload contract)
- `[ScopedService]` and `[Subscribe("...")]` attributes on the class
- `OnReceiveAsync` implementation
- Delegation to Application service methods — no business logic in the subscriber body
- Registration via `AddSubscribersUsing<T>()` inside `AddSubscribedManager`

## 6. Recommended Retrofit Modes

| Current state | Requested need | Recommended retrofit |
|---|---|---|
| Api + Database, no relay | Reliable integration-event publishing | Mode A: add `Outbox.Relay`, align API outbox publisher wiring, ensure outbox migration + dbex.yaml |
| Api + Database, no subscribe | Consume external events | Mode B: add `Subscribe` host and initial subscribers |
| Api + Database, no relay, no subscribe | Publish and consume | Mode C: both |
| Subscribe host exists, subscribers incomplete | New subjects or handlers | Mode D: add subscriber classes and registration only |
| Api exists, no recognisable Database/outbox shape | Relay requested | Ask before proceeding — outbox infrastructure must exist first |

## 7. Ambiguity Triggers

Ask before changing anything when:

- Multiple similarly named domains could match the request.
- There is already partial relay or subscribe wiring that does not match the sample conventions.
- The database engine cannot be determined from the existing codebase.
- The domain appears to use a broker other than Azure Service Bus.
- Event subjects, payload contracts, or application service entry points are unclear.
- Relay is requested but there is no Database project or outbox migration.

## 8. Default Assumptions

Unless determinable from the codebase or stated by the user:

- **Database engine**: SQL Server.
- **Broker**: Azure Service Bus with session receiver (`UsePartitionKeyConvertedToAnId` strategy).
- **Telemetry**: OpenTelemetry with both engine telemetry and Service Bus telemetry wired together.
- Relay and subscribe hosts mirror the sample architecture for the matched engine — do not invent a new host style.
- `RuntimeHostConfigurationOption Azure.Experimental.EnableActivitySource = true` is always included in Relay and Subscribe csproj files.
