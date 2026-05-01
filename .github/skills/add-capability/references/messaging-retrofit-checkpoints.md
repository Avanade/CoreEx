# Messaging Retrofit Checkpoints

Use these checkpoints when inspecting an existing domain before adding messaging and integration capabilities.

## 1. Domain Shape Detection

Look for these project patterns first:

- `{Solution}.{Domain}.Api`
- `{Solution}.{Domain}.Application`
- `{Solution}.{Domain}.Infrastructure`
- `{Solution}.{Domain}.Database`
- `{Solution}.{Domain}.Outbox.Relay`
- `{Solution}.{Domain}.Subscribe`

If the domain does not follow a recognizable CoreEx-style layered shape, treat the retrofit as ambiguous and ask before proceeding.

## 2. Host Detection Signals

| Capability or host | Evidence to inspect | Positive signal |
|---|---|---|
| API host | `Program.cs`, controllers, `*.Api.csproj` | `AddMvcWebApi`, `AddHttpWebApi`, controllers, OpenAPI setup |
| Relay host | `*.Outbox.Relay\\Program.cs`, relay csproj | `AddSqlServerOutboxRelay`, `AddSqlServerOutboxRelayHostedService`, `AddAzureServiceBusPublisher` |
| Subscribe host | `*.Subscribe\\Program.cs`, `Subscribe\\**\\*.cs` | `AddSubscribedManager`, `AzureServiceBusReceiving`, `MapHostedServices`, subscriber classes |
| Outbox publisher in API | API `Program.cs`, infrastructure repository/publisher files | `AddEventFormatter`, `AddSqlServerOutboxPublisher<T>` |
| Service Bus support | affected host `Program.cs`, csproj references | `AddAzureServiceBusClient("ServiceBus")`, `CoreEx.Azure.Messaging.ServiceBus` |
| Telemetry alignment | `Program.cs` | `WithCoreExTelemetry`, `WithCoreExSqlServerTelemetry`, `WithCoreExServiceBusTelemetry`, `UseOtlpExporter` |

## 3. Database and Outbox Detection

When adding a relay or reliable publication support, inspect for:

- `*.Database` project.
- outbox migrations.
- outbox stored procedures:
  - `spOutboxEnqueue.g.sql`
  - `spOutboxLeaseAcquire.g.sql`
  - `spOutboxLeaseRelease.g.sql`
  - `spOutboxBatchClaim.g.sql`
  - `spOutboxBatchComplete.g.sql`
  - `spOutboxBatchCancel.g.sql`
- database `Program.cs` and `dbex.yaml`.

If relay is requested and these assets are missing, plan to add them or stop and ask if the domain is intentionally non-SQL/outbox-based.

## 4. Subscriber Detection

Inspect subscriber code for:

- `[ScopedService]`
- `[Subscribe("...")]`
- inheritance from `SubscribedBase`
- `OnReceiveAsync`
- optional shared `ErrorHandler`
- delegation to Application services rather than embedded business logic

## 5. Recommended MVP Retrofit Modes

| Current state | Requested need | Recommended retrofit |
|---|---|---|
| API + Database, no relay | reliable integration-event publishing | Add `Outbox.Relay`, align API outbox publisher wiring |
| API + Database, no subscribe | consume external events | Add `Subscribe` host and initial subscribers |
| API + Database, no relay, no subscribe | publish and consume | Add both relay and subscribe |
| Subscribe host exists | new subjects or handlers | Add subscriber classes and registration only |
| API exists, no recognizable database/outbox shape | relay | Ask before proceeding; MVP assumes SQL Server/outbox path |

## 6. Ambiguity Triggers

Ask before changing anything when:

- multiple similarly named domains could match the request.
- there is already partial relay or subscribe wiring that does not match the sample conventions.
- the domain appears to use non-SQL Server persistence for write workflows.
- the domain appears to use a broker other than Azure Service Bus.
- event subjects, payload contracts, or application service entry points are unclear.

## 7. Default Initial Assumptions

Unless the user says otherwise, the MVP retrofit assistant should assume:

- SQL Server for outbox-backed write workflows.
- Azure Service Bus for publish/subscribe integration.
- OpenTelemetry-compatible host telemetry wiring should be preserved or aligned.
- relay and subscribe hosts should mirror the sample architecture, not invent a new host style.
