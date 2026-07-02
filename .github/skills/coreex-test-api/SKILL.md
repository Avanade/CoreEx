---
name: coreex-test-api
description: "Write or update an integration test in a CoreEx *.Test.Api project. USE FOR: new XxxReadTests/XxxMutateTests partial classes, per-operation test files (Get/Query/Create/Update/Patch/Delete), OneTimeSetUp seeding + cache + outbox wiring, seed data (read-data.seed.yaml/mutate-data.seed.yaml), .res.json/.req.json resources, ETag/concurrency and soft-delete scenarios, outbox event assertions, inter-domain HTTP mocking. DO NOT USE FOR: Subscribe host tests (use coreex-test-subscribe), Outbox Relay host tests (use coreex-test-relay), pure unit tests with no infrastructure (validators/aggregates/adapters already cover their own Test.Unit guidance), the controller/endpoint implementation itself (use coreex-api)."
argument-hint: "Entity/endpoint name, operations to cover (Get/Query/Create/Update/Patch/Delete), database provider (PostgreSQL/SQL Server)"
tags: ["testing", "integration-tests", "api", "unittestex", "coreex"]
---

# CoreEx: API Integration Test

Guides you through writing or extending `*.Test.Api` integration tests — the real host, real DB, real
cache, real outbox, over `WithApiTester<{Solution}.Api.Program>`. This is the canonical integration-test
setup shared with Subscribe host tests (`coreex-test-subscribe` links back here for the common parts).

## When to Use

- Adding a new entity's read/mutate API tests from scratch
- Adding a test for a single new operation (Get/Query/Create/Update/Patch/Delete) on an existing entity
- Adding an ETag/concurrency, soft-delete, idempotent-delete, or outbox-event scenario
- Wiring `OneTimeSetUp` (DB migrate/seed, cache clear, outbox publisher, inter-domain HTTP mocks) for a new test class
- Called standalone ("write the test for X") or as the hand-off step from `coreex-api` once an endpoint is implemented

## When Not to Use

- Subscribe host integration tests — use `coreex-test-subscribe` (shares this skill's DB/cache/outbox setup, differs in trigger mechanism)
- Outbox Relay host tests — use `coreex-test-relay` (mostly templated, rarely extended)
- Validator/aggregate/adapter unit tests (`*.Test.Unit`) — already covered by `coreex-validator`/`coreex-aggregate`/`coreex-adapter`
- Implementing the controller/endpoint itself — use `coreex-api`

> **Resolve project-wide choices from state before asking.** Read the solution-root `AGENTS.md`
> **Feature Configuration**: `data-provider` (PostgreSQL vs SQL Server) selects the migrate/seed and
> outbox helper family used throughout — never mix them; `outbox-enabled` and `messaging-provider`
> determine whether outbox-event assertions (`ExpectXxxOutboxEvents`) apply at all. Only prompt for
> what is unrecorded; re-state resolved values for confirmation.

## Quick Reference

- **One class pair per entity**: `XxxReadTests` (seeds `read-data.seed.yaml`) / `XxxMutateTests` (seeds `mutate-data.seed.yaml`) — each a `partial class`, one operation per sub-file: `Xxx{Read|Mutate}Tests.{Operation}.cs`
- **`[OneTimeSetUp]`** lives in the base partial file: migrate + seed (named-file overload) → `Test.ClearFusionCacheAsync()` → provider-specific `Test.UseExpected{Postgres|SqlServer}OutboxPublisher()` → HTTP mocks if the domain has adapters
- **Seed → Tests → Resources**, in that order — seed known `^N` rows first, write tests referencing them, capture `.res.json`/`.req.json` from the actual run
- **One seed row per destructive test** (not per operation) — NUnit randomises order, so two mutate tests sharing a row collide non-deterministically
- **`Test.Http()` / `Test.Http<T>()`** fluent chain: expectations → `.Run(...)` → assert
- **Expectation helpers auto-exclude from JSON compare**: `ExpectIdentifier()`, `ExpectETag()`, `ExpectChangeLogCreated()`/`Updated()` — omit those fields from `.res.json`
- **412 vs 409 vs 428**: stale ETag → `AssertPreconditionFailed()` (412); duplicate/business conflict → `AssertConflict()` (409); no ETag supplied → `428`
- **Delete is idempotent**: always `AssertNoContent()` (204), never `AssertNotFound()` on DELETE — the 404 belongs to the follow-up GET; only the first delete emits an outbox event
- **Outbox assertions are provider-specific** — `ExpectPostgresOutboxEvents`/`ExpectSqlServerOutboxEvents`; `.AssertWithValue(destination, subject)` for value-carrying events (Create/Update), `.AssertMetadata(destination, subject, key)` for no-value events (Delete)
- **Reference-data JSON name**: non-`Code` suffix (`gender`, not `genderCode`) in `.res.json`/`.req.json`/inline bodies
- **HTTP client mocking**: `MockHttpClientFactory` + `MockHttpClientRequest` fields configured in `OneTimeSetUp`, per-test `.Respond.With(...)`/`.Respond.WithJsonResource(...)`, always `.Verify()`
- Name tests `{Entity}_{Action}_{Outcome}` (e.g. `Product_Create_Success`)

For full workflow, decision trees, and code patterns see [`references/workflow.md`](references/workflow.md).

## Key References

- [`/.github/instructions/coreex-tests.instructions.md`](/.github/instructions/coreex-tests.instructions.md) — full, authoritative test conventions (auto-injected on any `*.Test*/**/*.cs` edit); this skill is a practical workflow layered on top of it, not a replacement
- Related skills: [`coreex-api`](../coreex-api/SKILL.md) (the controller/endpoint implementation these tests exercise), [`coreex-app-service`](../coreex-app-service/SKILL.md) (Application logic behind the endpoint), [`coreex-test-subscribe`](../coreex-test-subscribe/SKILL.md) (Subscribe host tests — shares this skill's setup foundations), [`coreex-test-relay`](../coreex-test-relay/SKILL.md) (Outbox Relay host tests)
- [Testing deep-dive](/.github/docs/coreex/testing.md) — test architecture, data seeding, schema isolation, E2E runner; optional (after `/coreex-docs-sync`)
- Illustrative examples (CoreEx sample — not present in your project):
  - [Contoso.Products.Test.Api](https://github.com/Avanade/CoreEx/tree/main/samples/tests/Contoso.Products.Test.Api) — PostgreSQL domain example (outbox + HTTP mock adapter)
  - [Contoso.Shopping.Test.Api](https://github.com/Avanade/CoreEx/tree/main/samples/tests/Contoso.Shopping.Test.Api) — SQL Server domain example (outbox + ETag/concurrency)
