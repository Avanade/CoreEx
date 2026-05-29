# Domain Scaffold Checklist

Use this checklist after scaffolding a new domain from the templates.

## Scaffolding Inputs Confirmed

- [ ] Solution prefix confirmed (e.g. `Contoso`).
- [ ] Domain name confirmed (e.g. `Orders`).
- [ ] Primary entity name confirmed (e.g. `Order`).
- [ ] **Database engine** answered: SQL Server (default) or PostgreSQL.
- [ ] **Reference Data** answered: Yes (default) or No — drives whether CodeGen project is included.
- [ ] **Domain project** answered: No (default) or Yes — drives whether a Domain aggregate layer is included.
- [ ] **Outbox.Relay** answered: Yes (default) or No — drives whether a relay host is scaffolded.
- [ ] **Subscribe** answered: Yes (default) or No — drives whether an event-consumer host is scaffolded.
- [ ] **Railway Oriented Programming** answered: No (default) or Yes — drives `Result<T>` vs exception-based service/repository patterns.
- [ ] **Child entity** answered: No (default) or Yes — drives whether child-entity templates and migration are included.

> **Generated files**: Never manually create or edit `*.g.cs`, `*.g.sql`, or `*.g.pgsql` files.
> These are produced by running the CodeGen and Database projects after scaffolding.

## Projects Created

- [ ] All domain projects are grouped under a Visual Studio solution folder named `{Domain}`.
- [ ] All new domain projects are added to the solution file.
- [ ] `{Solution}.{Domain}.Contracts`
- [ ] `{Solution}.{Domain}.Application`
- [ ] `{Solution}.{Domain}.Infrastructure`
- [ ] `{Solution}.{Domain}.Api`
- [ ] `{Solution}.{Domain}.Database`
- [ ] `{Solution}.{Domain}.CodeGen` — if Reference Data = Yes.
- [ ] `{Solution}.{Domain}.Domain` — if Domain project = Yes.
- [ ] `{Solution}.{Domain}.Outbox.Relay` — if Outbox.Relay = Yes.
- [ ] `{Solution}.{Domain}.Subscribe` — if Subscribe = Yes.
- [ ] `{Solution}.{Domain}.Test.Unit`
- [ ] `{Solution}.{Domain}.Test.Api`

## Contracts Layer

- [ ] `[Contract]` classes are `partial`.
- [ ] `Id`, `ETag`, `ChangeLog` properties carry `[ReadOnly(true)]`.
- [ ] Reference-data code properties use `[ReferenceData<T>]`.
- [ ] Mutable contracts implement `IIdentifier<T>`, `IETag`, and `IChangeLog`.

## Application Layer

- [ ] Interfaces exist for service, read-service, and repository.
- [ ] Validator created and invoked in all mutate methods.
- [ ] All mutate methods wrapped in `_unitOfWork.TransactionAsync`.
- [ ] Outbox events added inside `WhereMutated` callbacks.
- [ ] All awaited calls use `.ConfigureAwait(false)`.
- [ ] `QuerySchemaAsync()` implemented on read-service, delegating to repository.

## Infrastructure Layer

- [ ] Persistence models created (hand-written, no `*.g.*`).
- [ ] Mapper(s) created and wired (`BiDirectionMapper<TFrom, TTo, TSelf>`).
- [ ] `{Domain}DbContext` is `partial`; calls `AddGeneratedModels(modelBuilder)`.
- [ ] `{Domain}EfDb` uses `EfDbOptions` with `WithLogicalDeleteFilter()` for the primary entity.
- [ ] Repository implementation includes `QueryArgsConfig` for filtering and ordering.
- [ ] Repository implements `QuerySchemaAsync()` returning `_queryConfig.ToJsonSchema()`.
- [ ] Outbox publisher (`{Domain}OutboxPublisher`) registered — SQL Server only.

## API Layer

- [ ] Mutate and read controllers are split into separate classes.
- [ ] POST endpoints carry `[IdempotencyKey]`.
- [ ] Get-by-id uses dual `[HttpGet("{id}"), HttpHead("{id}")]` route.
- [ ] PATCH implemented with `get` + `put` delegates.
- [ ] `QuerySchemaAsync` endpoint exposed at `GET /api/{entityPluralKebab}/$schema`.
- [ ] `Program.cs` includes: `AddPrecisionTimeProvider`, cache, database, outbox, OpenAPI, telemetry, health checks.

## Database Layer

- [ ] `dbex.yaml` includes all required tables; `outboxName: outbox` set.
- [ ] Schema migration created (`{MigrationTimestamp}-000001-create-{domainKebab}-schema.sql`).
- [ ] Reference-data table migration created — if Reference Data = Yes.
- [ ] Primary entity migration created.
- [ ] Child entity migration created — if Child Entity = Yes.
- [ ] Outbox tables migration created.
- [ ] Reference-data seed file created in `Data/` — if Reference Data = Yes.
- [ ] `Program.cs` `DataResetFilterPredicate` scoped to the `{Domain}` schema.

## CodeGen Layer (if Reference Data = Yes)

- [ ] `ref-data.yaml` populated with all reference-data entity names.
- [ ] CodeGen project runs successfully and produces `*.g.cs` artefacts.
- [ ] Generated `{Domain}DbContext.g.cs` partial method `AddGeneratedModels` is present.
- [ ] Generated model properties added to `{Domain}EfDb` after running CodeGen.

## Post-Scaffold Tooling Steps

1. Run `{Solution}.{Domain}.CodeGen` — generates `*.g.cs` artefacts (ref-data models, mappers, service entries, controller endpoints).
2. Run `{Solution}.{Domain}.Database` — applies schema migrations and seeds reference data.
3. If Outbox.Relay = Yes — wire the relay host to the Service Bus topic and subscription.
4. If Subscribe = Yes — configure subscriber bindings and register with the message broker.

## Final Validation

- [ ] No compiler errors or nullable warnings.
- [ ] Project compiles clean.
- [ ] Unit tests run and pass for `{Solution}.{Domain}.Test.Unit`.
- [ ] API tests run and pass for `{Solution}.{Domain}.Test.Api`.
- [ ] All projects added to the solution file and organised under the `{Domain}` solution folder.
- [ ] README / docs updated where applicable.
