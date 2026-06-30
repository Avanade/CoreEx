---
description: Create or modify a CoreEx Application-layer service — CRUD operations, business actions, unit-of-work + events, CQRS read service, adapter interface, and policy
---

Guide this workspace through creating or modifying a CoreEx Application-layer service.

Use `.github/skills/coreex-app-service/SKILL.md` and its referenced workflow as the authoritative workflow when they exist.

Operational contract:
- Ask upfront: entity name, operations needed, exception-based or Result<T> pipeline (check project style), any cross-domain calls, read queries needed.
- [ScopedService<IInterface>] on every service — no manual DI wiring.
- Inject only: repositories, unit of work, adapter interfaces, logger — never validators, mappers, or policies.
- ValidateAndThrowAsync (exception style) or ValidateWithResultAsync (Result<T>) — never bare ValidateAsync.
- Service assigns Id: Runtime.NewId() (string) or Runtime.NewGuid() (Guid) — after validation, before transaction.
- All mutations in _unitOfWork.TransactionAsync(...); event added inside, never outside.
- WhereMutated(v => ...) for Create/Update; WhereMutated(() => ...) for Delete.
- Delete event: EventData.CreateEvent<T>(EventAction.Deleted).WithKey(id) — no value body.
- CQRS: mutations + GetAsync → {Name}Service; queries + GetAsync → {Name}ReadService.
- Policies and validators: instantiate/call at point of use, never register in DI.
- Always .ConfigureAwait(false) on every await.
- If any prompt text conflicts with the skill, the skill wins.

Outcome:
- The service is declared with an interface, wired via [ScopedService], validates correctly, commits mutations atomically with events, builds cleanly, and integration test coverage is offered.
