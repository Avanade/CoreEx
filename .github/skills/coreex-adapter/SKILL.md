---
name: coreex-adapter
description: "Create or modify a CoreEx Infrastructure-layer adapter (anti-corruption layer). USE FOR: new adapter interface in Application/Adapters/{ExternalDomain}/, new adapter implementation in Infrastructure/Adapters/{ExternalDomain}/, new typed HTTP client in Infrastructure/Clients/{ExternalDomain}/, event-driven sync/replication adapter (IXxxSyncAdapter), unit tests for HTTP clients with MockHttpClientFactory. DO NOT USE FOR: repositories within the same domain (use coreex-repository), application services that call adapters (use coreex-app-service), event subscriber hosts that drive sync adapters (see coreex-event-subscribers.instructions.md)."
argument-hint: "Optional: external domain name, operations needed (get/reserve/cancel/sync), synchronous vs replication role, HTTP or EF-only"
tags: ["adapter", "anti-corruption", "http-client", "infrastructure", "integration", "coreex"]
---

# CoreEx: Adapter (Anti-Corruption Layer)

Guides you through adding or modifying an adapter — the boundary that isolates your domain from an external system. Covers the Application-layer interface, Infrastructure-layer implementation, typed HTTP client, sub-folder conventions, registration, and unit testing.

## When to Use

- Add an `IXxxAdapter` interface for a new external-domain dependency
- Add a synchronous real-time adapter (HTTP call + optional local EF read/event publish)
- Add a replication adapter (`IXxxSyncAdapter`) for event-driven data sync into your local store
- Add or extend a typed HTTP client (`XxxHttpClient`) in `Infrastructure/Clients/{ExternalDomain}/`
- Unit-test HTTP client response-code handling with `MockHttpClientFactory`

## When Not to Use

- EF repositories within the same domain — use `coreex-repository`
- Application services that consume the adapter — use `coreex-app-service`
- Event subscriber hosts that drive `IXxxSyncAdapter` — use `coreex-subscriber`

> **Resolve project-wide choices from state before asking.** Read the solution-root `AGENTS.md`
> **Feature Configuration** for `messaging-provider` (does this domain publish/consume events?) and `data-provider`
> (EF-only replication vs HTTP). Only prompt for what is unrecorded; re-state resolved values for confirmation.

## Quick Reference

- **Always use a sub-folder** per external domain: `Application/Adapters/{ExternalDomain}/`, `Infrastructure/Adapters/{ExternalDomain}/`, `Infrastructure/Clients/{ExternalDomain}/`
- Interface surface must be **domain-idiomatic** — never a mirror of the remote API's shape
- Returns `Result` or `Result<T>` — not plain values or thrown exceptions
- `[ScopedService<IXxxAdapter>]` on the implementation — auto-discovered via `AddDynamicServicesUsing<T>()`
- Typed HTTP client registered via `builder.AddTypedHttpClient<XxxHttpClient>("ServiceName")` in `Program.cs`
- Never call `HttpClient` directly in adapter methods — always delegate to the typed client class
- `response.ToResultAsync()` maps HTTP status to `Result` (2xx → `Success`, error + ProblemDetails → `BusinessException`/`ProblemDetailsException`, plain error → `HttpRequestException`)
- Two distinct adapter roles in the samples — **synchronous** (real-time HTTP + events) and **replication** (event-driven local EF write); separate interfaces, separate implementations

For full workflow and code examples see [`references/workflow.md`](references/workflow.md).

## Key References

- [`/.github/instructions/coreex-repositories.instructions.md`](/.github/instructions/coreex-repositories.instructions.md) — Infrastructure conventions (adapters, clients, mapping)
- [`/.github/instructions/coreex-application-services.instructions.md`](/.github/instructions/coreex-application-services.instructions.md) — Application conventions (adapter interfaces, anti-corruption layer)
- Related skills: [`coreex-repository`](../coreex-repository/SKILL.md) (same-domain persistence), [`coreex-app-service`](../coreex-app-service/SKILL.md) (consumes the adapter), [`coreex-subscriber`](../coreex-subscriber/SKILL.md) (drives `IXxxSyncAdapter`)
- [Infrastructure layer deep-dive](/.github/docs/coreex/infrastructure-layer.md) — optional (after `/coreex-docs-sync`)
- Illustrative examples (CoreEx sample — not present in your project):
  - [synchronous + replication adapter interfaces](https://github.com/Avanade/CoreEx/tree/main/samples/src/Contoso.Shopping.Application/Adapters/Products) — `IProductAdapter`, `IProductSyncAdapter`
  - [adapter + client implementations](https://github.com/Avanade/CoreEx/tree/main/samples/src/Contoso.Shopping.Infrastructure/Adapters/Products) and the [typed HTTP client](https://github.com/Avanade/CoreEx/tree/main/samples/src/Contoso.Shopping.Infrastructure/Clients/Products)
