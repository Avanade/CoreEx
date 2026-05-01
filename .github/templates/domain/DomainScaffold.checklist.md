# Domain Scaffold Checklist

Use this checklist after scaffolding a new domain from templates/prompts.

## Inputs Confirmed

- [ ] Solution prefix confirmed.
- [ ] Domain name confirmed.
- [ ] Root entity name confirmed.
- [ ] Child entity name confirmed (or explicitly omitted).
- [ ] CRUD operations confirmed.
- [ ] Reference data/status values confirmed.
- [ ] Event subjects confirmed.

## Projects Created

- [ ] {Solution}.{Domain}.Contracts.
- [ ] {Solution}.{Domain}.Application.
- [ ] {Solution}.{Domain}.Infrastructure.
- [ ] {Solution}.{Domain}.Api.
- [ ] {Solution}.{Domain}.Database.

## Contracts Layer

- [ ] [Contract] classes are partial.
- [ ] Id, ETag, ChangeLog are [ReadOnly(true)].
- [ ] ReferenceData code properties are partial.
- [ ] Reference data classes and collections exist.

## Application Layer

- [ ] Interfaces for service/read-service/repository created.
- [ ] Validator created and invoked in mutate methods.
- [ ] All mutate methods wrapped in _unitOfWork.ExecuteAsync.
- [ ] Outbox events added in WhereMutated callbacks.
- [ ] All awaited calls use ConfigureAwait(false).

## Infrastructure Layer

- [ ] Persistence models created.
- [ ] Mapper(s) created and wired.
- [ ] EfDb + DbContext created and configured.
- [ ] Repository implementation includes QueryArgsConfig.
- [ ] Outbox publisher points to [{Domain}].[spOutboxEnqueue].

## API Layer

- [ ] Mutation and read controllers split.
- [ ] POST endpoints use [IdempotencyKey].
- [ ] GET/HEAD dual route used for get-by-id.
- [ ] PATCH implemented with get + put delegates.
- [ ] Program.cs includes cache, SQL, outbox, OpenAPI, telemetry, health checks.

## Database Layer

- [ ] dbex.yaml includes all required tables.
- [ ] Schema + table migrations created.
- [ ] Outbox tables migration created.
- [ ] All six outbox stored procedures created.
- [ ] Reference data seed file created.
- [ ] Program.cs DataResetFilterPredicate scoped to schema.

## Final Validation

- [ ] Diagnostics check returns no errors.
- [ ] Project compiles.
- [ ] Added to solution file where required.
- [ ] README/docs updated where required.
