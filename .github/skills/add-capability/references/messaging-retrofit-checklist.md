# Messaging Retrofit Checklist

Use this checklist as the completion gate for `/add-capability` messaging and integration retrofits.

## Discovery

- [ ] Identified the target domain and its existing project/host shape.
- [ ] Determined whether API, Database, Outbox.Relay, and Subscribe projects already exist.
- [ ] Determined whether SQL Server/outbox and Azure Service Bus are already present, missing, or intentionally not used.
- [ ] Confirmed any user choices that could not be inferred safely.

## Project and Package Alignment

- [ ] Added only the missing projects required by the requested retrofit.
- [ ] Added only the missing package and project references required by the affected hosts.
- [ ] Preserved the existing layered references and naming conventions.

## Relay Retrofit

- [ ] Relay host was added or aligned when requested.
- [ ] Relay `Program.cs` uses the expected CoreEx host setup, SQL Server relay wiring, Service Bus publisher wiring, health checks, and telemetry.
- [ ] API host has event formatter and outbox publisher wiring when the domain is expected to publish integration events.
- [ ] Database project contains required outbox tables and stored procedures when relay support is added.

## Subscribe Retrofit

- [ ] Subscribe host was added or aligned when requested.
- [ ] Subscribe `Program.cs` uses hosted service manager, subscribed manager, Service Bus receiver, hosted service mapping, health checks, and telemetry.
- [ ] Subscriber classes inherit from `SubscribedBase`.
- [ ] Subscriber classes use `[ScopedService]` and `[Subscribe("...")]`.
- [ ] Subscriber logic delegates to Application services rather than embedding business logic.
- [ ] Shared subscriber error handling is added where needed.

## Host and Convention Alignment

- [ ] Middleware order follows repo conventions.
- [ ] Dynamic service registration is used where expected.
- [ ] OpenTelemetry-compatible wiring is preserved or aligned for the affected hosts.
- [ ] Health endpoints and hosted service mapping are present where applicable.

## Validation

- [ ] Affected projects build or pass diagnostics.
- [ ] Any related tests were added or updated where practical.
- [ ] The final summary distinguishes completed retrofits from any blocked or intentionally deferred items.
- [ ] Any remaining user decisions are listed explicitly as follow-up items.
