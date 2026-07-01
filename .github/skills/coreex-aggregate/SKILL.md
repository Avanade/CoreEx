---
name: coreex-aggregate
description: "Add or modify a DDD domain object in a CoreEx Domain layer. USE FOR: new aggregate root (Aggregate<TId,TSelf>), new child entity (Entity<TId,TSelf>) owned by an aggregate, new value object (sealed record), adding mutation methods, PersistenceState-aware factory methods, mutation guards (OnCheckCanMutate/OnMutate). Interviews the developer to determine which of the three (aggregate root / entity / value object) applies, then follows the appropriate pattern. DO NOT USE FOR: CRUD-oriented domains with no Domain layer (use coreex-app-service directly against repositories), Application-layer mappers between aggregate and contract (see coreex-application-services.instructions.md), Infrastructure persistence of aggregates (use coreex-repository)."
argument-hint: "Optional: domain object kind (aggregate root / entity / value object), identifier type, mutation operations needed, invariants to enforce"
tags: ["ddd", "domain-driven-design", "aggregate", "entity", "value-object", "domain-layer", "coreex"]
---

# CoreEx: Aggregate (DDD Domain Object)

Guides you through adding or modifying a domain object in a CoreEx Domain layer — aggregate roots,
child entities, and value objects. This skill is scoped to solutions with `--domain-driven-enabled true`
(the `*.Domain` project exists). It interviews you to determine which kind of domain object you need,
then follows the corresponding pattern from `CoreEx.DomainDriven`.

## When to Use

- Add a new **aggregate root** — a consistency boundary with mutation guards and (optionally) integration events
- Add a new **child entity** — owned exclusively by an aggregate, with identity but no independent lifecycle
- Add a new **value object** — a concept with no identity, defined entirely by its values
- Add mutation methods, factory methods, or invariant guards to an existing domain object
- Decide whether a domain concept needs a Domain layer at all, or should stay CRUD-oriented

## When Not to Use

- The domain is CRUD-oriented with no meaningful invariants to protect at the model level — let the
  Application service orchestrate directly against repository interfaces; skip the Domain layer entirely
- Application-layer mapping between aggregate and contract — see `.github/instructions/coreex-application-services.instructions.md`
- Infrastructure persistence of the aggregate (EF mapping, `PersistenceState`-driven insert/update/delete) — use `coreex-repository`
- Validating primitive request DTOs before they reach the domain — use `coreex-validator`

## Quick Reference

- **Aggregate root** → `Aggregate<TId, TSelf>` (extends `Entity<TId, TSelf>`) — adds `Events`/`AddEvent`/`ClearEvents` for integration-event accumulation
- **Child entity** → `Entity<TId, TSelf>` — identity-based equality (`Id` only), no independent construction path from outside the aggregate
- **Value object** → `sealed record` — structural equality, `init`-only properties, invariants enforced in the initialiser
- Two factory methods only: `CreateNew(...)` (`.AsNew()`) and `CreateFrom(...)` (`.AsNotModified()`) — constructor is `private`
- `Modify(...)` / `Remove(...)` are the **only** mutation paths — they invoke `OnCheckCanMutate()`, apply the change, transition `PersistenceState`, then call `OnMutate()`
- `OnCheckCanMutate()` returns `Result` but is invoked via `.ThrowOnError()` internally — a failing guard throws the exception carried by the `Result` (typically `BusinessException` for `Result.BusinessError(...)`) from `Modify`/`Remove`, regardless of the `Result` return type declared here
- Public mutation methods on the aggregate should return `Result`/`Result<T>` for consistency with Application-layer `Result<T>` pipelines — even though the internal guard throws
- Child entity mutation methods are `internal` — only the owning aggregate may invoke them
- No async I/O in domain classes — ever. Async belongs in Application services or Policies.
- No native domain-event dispatch (MediatR-style) — only integration events via `Aggregate<TId,TSelf>.Events`, forwarded through `IUnitOfWork.Events` in the Application layer
- Aggregates are the best unit-test target in the codebase (no injected dependencies) — cover every mutation method's happy path and rejection path with `WithGenericTester<EntryPoint>` + `Test.Scoped(...)`, calling the aggregate directly (no repository, no Application service); assert `OnCheckCanMutate()` guard failures as thrown exceptions, and failed-`Result` returns as `Result` assertions

For full workflow and code examples see [`references/workflow.md`](references/workflow.md).

## Key References

- `.github/instructions/coreex-domain.instructions.md` — full Domain layer conventions (aggregates, entities, value objects, `PersistenceState`)
- `.github/instructions/coreex-application-services.instructions.md` — how Application services construct, mutate, and map aggregates (`Domain.Xxx.CreateNew(...)`, `Application/Mapping/` `Mapper<TSource,TDest,TSelf>`)
- `src/CoreEx.DomainDriven/README.md` — package overview, key types, and the "Domain Events — Intentionally Not Supported" rationale
- `src/CoreEx.DomainDriven/Aggregate.cs`, `Entity.cs`, `EntityBase.cs` — actual base-class implementation (`Modify`, `Remove`, `OnCheckCanMutate`, `OnMutate`, `PersistenceState` transitions)
- `src/CoreEx.DomainDriven/PersistenceState.cs`, `DomainDrivenExtensions.cs` — state enum and filter helpers (`IsNew`, `IsModified`, `IsNewOrModified`, `IsNotRemoved`)
- `src/CoreEx.Template/README.md` — `--domain-driven-enabled true` template flag and generated `*.Domain` project shape
- `.github/instructions/coreex-tests.instructions.md` — `*.Test.Unit` conventions (`WithGenericTester<EntryPoint>`, `Test.Scoped(...)`) reused for aggregate unit tests
