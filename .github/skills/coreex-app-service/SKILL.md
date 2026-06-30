---
name: coreex-app-service
description: "Create or modify a CoreEx Application-layer service. USE FOR: new service class (exception-based or Result<T>), adding CRUD/business operations, CQRS read service (XxxReadService), adapter interface in Application/Adapters/, policy class in Application/Policies/, application-level mapper (Domain → Contract). DO NOT USE FOR: Infrastructure repositories (use coreex-repository), validators (use coreex-validator), controller endpoints."
argument-hint: "Optional: entity name, operations needed (get/create/update/delete/query/custom), exception-based or Result<T> pipeline, cross-domain calls needed"
tags: ["application-service", "application-layer", "unit-of-work", "cqrs", "events", "adapter", "policy", "result", "coreex"]
---

# CoreEx: Application Service

Guides you through creating or modifying a CoreEx Application-layer service in `Application/`. Covers CRUD operations, business actions, unit-of-work + events, CQRS read services, adapters, and policies.

## When to Use

- New service class for an entity (scaffold interface + implementation)
- Adding a mutation operation (create / update / delete / custom business action)
- Adding a CQRS read service (`{Name}ReadService`) for queries and read-model shapes
- Adding an adapter interface (`Application/Adapters/`) for a cross-domain or external-service call
- Adding a policy class (`Application/Policies/`) for guard logic that requires I/O
- Switching a method between exception-based and `Result<T>` pipeline styles

## When Not to Use

- Infrastructure repositories — use the `coreex-repository` skill
- Validators — use the `coreex-validator` skill
- Controller endpoints — see the host conventions
- Domain aggregates, entities, value objects — those belong in the Domain layer

## Quick Reference

**Clarifying questions before writing any code:**
1. Exception-based or `Result<T>` pipeline style? (→ Path A or B — per-project choice)
2. Which operations? Get / Create / Update / Delete / custom business action? (**never assume Query**)
3. Any cross-domain or external-service calls? (→ adapter interface, Path D)
4. Any policy guard checks requiring I/O? (→ policy class, Path D)
5. Domain layer present? (→ `Application/Mapping/` mapper; used in Path B)
6. Read-only queries or collection results needed? (→ CQRS read service, Path C)

**Key rules at a glance:**
- `[ScopedService<IInterface>]` on every service — auto-registers via `AddDynamicServicesUsing<T>()`
- **Only inject**: repository, unit of work, adapter interfaces, logger — **never** validators, mappers, policies
- `ValidateAndThrowAsync` (exception style) / `ValidateWithResultAsync` (Result&lt;T&gt;) — **never bare `ValidateAsync`**
- Service assigns `Id` after validation: `value.Id = Runtime.NewId()` (string key) / `Runtime.NewGuid()` (Guid)
- All mutations in `_unitOfWork.TransactionAsync(...)` — event added inside, never outside
- `WhereMutated(v => ...)` for Create/Update (`DataResult<T>` carries value); `WhereMutated(() => ...)` for Delete
- Delete event: `EventData.CreateEvent<T>(EventAction.Deleted).WithKey(id)` — **no value body**
- `NotFoundException.ThrowIfDefault(entity)` after any Get that must find the entity
- Validators / Mappers / Policies: **not DI-registered** — call or instantiate at point of use
- `Validator<T, TSelf>`: call via singleton — `{Name}Validator.Default.ValidateAndThrowAsync(...)`
- `Validator<T>` (with injection): instantiate at call site — `new {Name}Validator(_dep).ValidateAndThrowAsync(...)`
- CQRS: mutations + `GetAsync` → `{Name}Service`; queries + `GetAsync` → `{Name}ReadService` (both have `GetAsync`)
- Always `.ConfigureAwait(false)` on every `await`

For full workflow and code examples see [`references/workflow.md`](references/workflow.md).

## Key References

- [`/.github/instructions/coreex-application-services.instructions.md`](/.github/instructions/coreex-application-services.instructions.md) — full conventions: guard clauses, events, CQRS, adapters, policies, Result&lt;T&gt; operators
- [`/samples/src/Contoso.Products.Application/`](/samples/src/Contoso.Products.Application/) — `ProductService` (exception-based CRUD + business actions), `ProductReadService` (CQRS read)
- [`/samples/src/Contoso.Shopping.Application/`](/samples/src/Contoso.Shopping.Application/) — `BasketService` (Result&lt;T&gt; + adapter + policy), `BasketReadService`
- [`/samples/src/Contoso.Shopping.Application/Policies/`](/samples/src/Contoso.Shopping.Application/Policies/) — `ProductPolicy` example
- [`/samples/src/Contoso.Shopping.Application/Adapters/`](/samples/src/Contoso.Shopping.Application/Adapters/) — `IProductAdapter` example
