---
name: coreex-test-subscribe
description: "Write or update an integration test in a CoreEx *.Test.Subscribe project. USE FOR: SubscriberTests partial classes, simulating broker message receipt via ServiceBusSubscribedSubscriber, command/event-data-sync/event-business-process test scenarios, ErrorHandler outcome assertions, unsubscribed-subject tests. Shares DB/cache/outbox OneTimeSetUp foundations with coreex-test-api — see that skill for the shared setup mechanics. DO NOT USE FOR: API host tests (use coreex-test-api), Outbox Relay host tests (use coreex-test-relay), the subscriber implementation itself (use coreex-subscriber)."
argument-hint: "Subscriber name/scenario (command / event-sync / event-business-process), subject string, payload type, ErrorHandler outcomes to cover"
tags: ["testing", "integration-tests", "subscriber", "messaging", "unittestex", "coreex"]
---

# CoreEx: Subscribe Host Integration Test

Guides you through writing or extending `*.Test.Subscribe` integration tests — simulating broker
message receipt against the real Subscribe host, real DB, real cache, real outbox, over
`WithApiTester<{Domain}.Subscribe.Program>`. Subscribe hosts share the same DB/cache/outbox
`OneTimeSetUp` foundation as API hosts (see `coreex-test-api`) — the only real difference is *how* the
test triggers behavior: a simulated message receipt instead of an HTTP call.

## When to Use

- Adding a test for a new command subscriber, event-data-sync subscriber, or event-business-process subscriber
- Asserting an `ErrorHandler` outcome (e.g. `CompleteAsInformation` on a semantically-expected not-found)
- Asserting state/adapter changes after an event-sync subscriber processes a payload
- Asserting outbox events published as a *result* of processing a command/business-process message
- Called standalone or as the hand-off step from `coreex-subscriber` once a subscriber is implemented

## When Not to Use

- API host integration tests — use `coreex-test-api` (owns the shared DB/cache/outbox setup this skill builds on)
- Outbox Relay host tests — use `coreex-test-relay`
- Implementing the subscriber class itself — use `coreex-subscriber`

## Quick Reference

- **Base class**: `WithApiTester<{Domain}.Subscribe.Program>` — same DB/cache/outbox `[OneTimeSetUp]` shape as API tests (migrate + seed via named-file overload → `ClearFusionCacheAsync()` → provider-specific `UseExpected{Postgres|SqlServer}OutboxPublisher()`); Subscribe hosts **do** have FusionCache (reference data, idempotency)
- **Simulate receipt**: build an `EventData` → `Test.CreateCloudEventFrom(ed)` → `.ToServiceBusReceivedMessage()` → resolve `ServiceBusSubscribedSubscriber` from DI → `.ReceiveAsync(sbm)`
- **One partial file per subscriber scenario** — `SubscriberTests.{Scenario}.cs`
- **Match the test shape to the subscriber scenario**: command → assert outcome + outbox events published as a result; event-data-sync → assert local state/adapter reflects the payload; event-business-process → assert the downstream service ran and published its own events
- **`ErrorHandler` outcomes are assertable** — a handled exception surfaces as `EventSubscriberHandledException` with `.ErrorHandling` and `.InnerException` to check
- **Always include an "unsubscribed subject" test** — confirms unknown subjects are consumed silently (`ErrorHandling.CompleteAsSilent`), not dead-lettered
- **Seed file naming carries over unchanged** — `read-data.seed.yaml` / `mutate-data.seed.yaml` / a schema-only `no-data.seed.yaml` for health/plumbing-only tests

For full workflow and code patterns see [`references/workflow.md`](references/workflow.md). For the
shared DB/cache/outbox setup mechanics (seed data authoring, provider-specific outbox helpers), see
[`../coreex-test-api/references/workflow.md`](../coreex-test-api/references/workflow.md).

## Key References

- [`/.github/instructions/coreex-tests.instructions.md`](/.github/instructions/coreex-tests.instructions.md) — full, authoritative test conventions, "Subscribe Host Tests" section
- `coreex-test-api` — shared integration-test setup foundations (DB migrate/seed, cache, outbox)
- `coreex-subscriber` — the subscriber implementation this skill's tests exercise; its three scenarios (command / event-data-sync / event-business-process) map directly to this skill's test shapes
- `samples/tests/Contoso.Products.Test.Subscribe/SubscriberTests.ReservationConfirm.cs` — command subscriber test (outbox assertion + `ErrorHandler`)
- `samples/tests/Contoso.Shopping.Test.Subscribe/SubscriberTests.ProductModify.cs` — event-sync subscriber test (state assertion)
