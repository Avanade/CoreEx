# CoreEx Application Scaffolding Guide

This guide helps a new team decide **what to scaffold first**, **which hosts to include**, and **which CoreEx capabilities to add now versus later**. It is intentionally decision-oriented: `docs/capabilities.md` explains what the framework can do, while this guide explains how to turn that into an application shape that fits your use case. If you want help learning how to ask the agent the right questions while making these decisions, see the [Agent Interaction Guide](agent-interaction-guide.md) and [Agent Prompt Recipes](agent-prompt-recipes.md).

## Understand the Defaults vs the Abstractions

In this repository, **SQL Server** and **Azure Service Bus** are the default scaffolding targets because they are the most complete sample implementations and the primary host wiring demonstrated in the starter and Contoso samples.

That should not be read as "CoreEx only works with SQL Server and Service Bus." A better mental model is:

- CoreEx provides **application and integration patterns** first.
- This repo currently provides **default implementation paths** for those patterns using SQL Server and Azure Service Bus.
- Alternative databases or brokers should be introduced when the **use case requires them**, not because teams want to abstract everything up front.

Examples:

- If SQL Server fits the operational and data requirements, use the standard SQL Server projects and migrations because that path is the most proven in this repo.
- If a domain truly requires a different database backend, CoreEx patterns such as contracts, services, validation, unit-of-work boundaries, and event workflows still matter; only the implementation plumbing changes.
- If Azure Service Bus fits the messaging needs, use the repo's default publisher/subscriber/relay wiring.
- If a use case requires another broker, the `EventData` abstraction and event-oriented architecture remain relevant even when the transport changes.

## Start with the Smallest Useful Shape

The starter and sample architecture support a modular domain layout built from:

- `Contracts`
- `Application`
- `Infrastructure`
- `Api`
- `Database`
- optionally `Subscribe`
- optionally `Relay`
- optionally a separate worker or orchestration host

The sample host shapes also include **OpenTelemetry-compatible telemetry wiring** via the standard CoreEx/OpenTelemetry setup shown in the sample `Program.cs` files, so observability can be added as part of the normal host composition rather than as a separate architecture track.

For most teams, the right question is not "Which CoreEx packages exist?" but "Which responsibilities does this application need on day one?"

Use this progression:

1. Start with a **single API domain** when the service owns its own data and synchronous CRUD-style operations are the primary need.
2. Add an **outbox relay** when the API must publish integration events reliably after database commits.
3. Add a **subscriber host** when the service must react to events or commands from other services.
4. Add a **worker or orchestration host** when the business process is long-running, stateful, batch-oriented, externally coordinated, or compensation-heavy.

## Which Scaffolding Path to Use

| Need | Best starting point | Why |
|---|---|---|
| New implementation solution with one or more domains and standard hosts. | `CoreEx.Template` via `dotnet new coreex*` | This is the deterministic scaffolding path for layered solution structure, package choices, standard `Program.cs` wiring, tests, and host projects. |
| Existing domain needs new messaging or integration capability added incrementally. | Inspect first, then make a manual CoreEx-aligned change | Capability retrofits are now normal implementation work guided by the samples and instruction files rather than a dedicated scaffolding command. |
| New domain with a conventional entity shape. | `dotnet new coreex` plus only the required `coreex-api`, `coreex-relay`, or `coreex-subscribe` hosts | Fastest path when you want deterministic output and only need to fill in the domain-specific logic. |
| New domain with richer rules or more design uncertainty. | Ask `CoreEx Expert` to choose the shape, then scaffold with `dotnet new coreex*` | Best when you want sample-aligned advice before committing to a host set or capability mix. |

## Recommended Application Shapes

### 1. API-Only Domain

Choose this when:

- The service mainly exposes synchronous HTTP operations.
- It owns its own schema and data lifecycle.
- Cross-service integration is limited or can be added later.

Scaffold:

- `Contracts`
- `Application`
- `Infrastructure`
- `Api`
- `Database`
- matching API and common test projects

Pull in early:

- `CoreEx`
- `CoreEx.AspNetCore`
- `CoreEx.AspNetCore.NSwag`
- `CoreEx.Database.SqlServer`
- `CoreEx.EntityFrameworkCore`

Default implementation note:

- PostgreSQL is the default data provider for the templates; SQL Server and `None` (facade, no local database) remain fully supported and are selectable via `--data-provider`.
- Treat that as the recommended initial implementation, not as a rule that every CoreEx application must use that provider forever — choose the one that fits the solution.

Usually add immediately:

- `ETag` and change-log support for mutable entities.
- `ProblemDetails`/CoreEx exception handling.
- OpenAPI and health checks.

Good fit:

- Product master data.
- Reference-data-backed CRUD domains.
- Internal line-of-business APIs that do not yet need async integration.

### 2. API + Outbox Relay

Choose this when:

- The API updates business data and must publish integration events reliably.
- You need to avoid dual writes to database plus broker.
- Other services depend on ordered or guaranteed event publication.

Scaffold:

- API-only domain shape, plus `Relay`

Pull in early:

- `CoreEx.Events`
- `CoreEx.Database.SqlServer`
- `CoreEx.Azure.Messaging.ServiceBus`

Default implementation note:

- SQL Server plus Azure Service Bus is the standard initial combination for reliable integration-event publication in this repo.
- Choose a different database or broker only when the business, platform, compliance, latency, throughput, tenancy, or deployment constraints justify that divergence.

Usually add immediately:

- Unit-of-work with outbox.
- Event formatter.
- Outbox tables and stored procedures in the database project.
- Relay host telemetry and health checks.

Good fit:

- Product, catalog, pricing, customer, or order domains that publish state-change events.
- Any service where "database committed but event not published" is unacceptable.

### 3. API + Subscribe + Outbox Relay

Choose this when:

- The service both publishes its own events and consumes events or commands from other services.
- You are building a distributed service, not just a standalone API.
- You need asynchronous integration boundaries with explicit host separation.

Scaffold:

- `Contracts`
- `Application`
- `Infrastructure`
- `Api`
- `Database`
- `Subscribe`
- `Relay`

Pull in early:

- `CoreEx.Events`
- `CoreEx.Azure.Messaging.ServiceBus`
- `CoreEx.Caching.FusionCache` when the service caches reference or replica data

Default implementation note:

- The sample architecture uses Azure Service Bus because the repo already demonstrates subscriber and relay hosts around it.
- The broader architectural pattern is still publish/subscribe with `EventData`; the broker choice is an implementation decision driven by the integration use case.

Usually add immediately:

- Subscriber classes per message subject.
- Shared error-handling strategy for known recoverable subscriber failures.
- Reference data orchestration if incoming messages rely on code tables or shared reference sets.

Good fit:

- Inventory availability projections.
- Shopping or basket domains that react to product or reservation events.
- Services that maintain local replicas of upstream data.

### 4. API + Worker / Orchestration

Choose this when:

- The core business process spans multiple steps, services, or time boundaries.
- You need retries, timers, external-event waits, fan-out/fan-in, batching, or compensation.
- A request/response API plus pub/sub is not enough to model the process safely.

Scaffold:

- Core domain projects
- API host if the workflow is externally started or queried over HTTP
- separate worker/orchestration host
- supporting infrastructure for the workflow backend

Pull in when needed:

- Durable Task SDK + DTS patterns described in `docs/orchestration.md`
- CoreEx telemetry and health checks in the worker host

Usually add immediately:

- Explicit orchestration contracts.
- Activity boundaries around external calls.
- Client endpoint or service to start/query workflow instances.

Good fit:

- Order submission and approval flows.
- Long-running fulfilment or settlement processes.
- Human approval, callback-driven, or scheduled business operations.

## Capability-by-Capability Guidance

The biggest mistake new teams make is enabling every framework feature up front. Prefer enabling capabilities because the **use case demands them**, not because the package exists.

| Capability | Add it when | Skip or defer when |
|---|---|---|
| **Validation** | The API accepts business input that must be checked consistently before persistence or orchestration. | The host is read-only or input shape is trivial and temporary. |
| **ETag / optimistic concurrency** | Multiple users or systems can update the same resource and lost updates matter. | Data is append-only or single-writer. |
| **Idempotency key** | Clients may retry POST requests, especially across unstable networks or user-driven retries. | The endpoint is naturally idempotent already or not externally retried. |
| **FusionCache + Redis** | Reads are hot, repeated, cross-instance, or expensive; you need hybrid L1/L2 caching and graceful degraded reads. | Data changes too frequently to benefit, or the service is small and latency/load do not justify cache complexity yet. |
| **Reference data orchestration** | The domain uses shared code tables, statuses, categories, units of measure, or other read-heavy lookup sets. | The values are local-only, short-lived, or not managed as reference data. |
| **Unit-of-work + outbox** | Data writes and integration-event publication must succeed together from a business perspective. | The service has no async integration boundary yet. |
| **Azure Service Bus integration** | You publish or consume messages across service boundaries and want the repo's standard broker pattern. | The application is strictly synchronous or local-only. |
| **Subscriber host** | The service reacts to upstream events/commands independently of user HTTP traffic. | Integration is outbound only. |
| **Outbox relay** | The service publishes integration events from committed business transactions. | The service consumes only and does not publish. |
| **Result<T> pipelines** | You are modeling expected business failures or domain flows compositionally, especially around aggregates/workflows. | Exception-style services are clearer and the flow is simple CRUD. |
| **DomainDriven aggregate patterns** | The domain has invariants across child entities or rich mutation rules. | The service is mostly thin CRUD over simple records. |
| **Dynamic query / paging / filtering** | List endpoints need flexible API-side filtering, ordering, and projection. | Consumers only need a few fixed queries. |
| **Workflow orchestration** | A business process is long-running, resumable, externally coordinated, or compensation-heavy. | Simple CRUD plus event publication already covers the need. |

## Choosing Defaults vs Diverging from Them

Start with the repo defaults unless there is a concrete reason not to:

- **Use SQL Server by default** because the database projects, migration tooling, outbox procedures, and sample hosts are already shaped around it.
- **Use Azure Service Bus by default** because the relay and subscriber patterns in this repo are already demonstrated around it.

Diverge when the use case clearly demands it, for example:

- Existing enterprise platform standards require another database or broker.
- Required operational characteristics are a poor fit for the default choice.
- A managed service, deployment target, or regulatory boundary constrains the technology selection.
- A specific integration landscape already centers on another messaging platform.

When you do diverge, preserve the CoreEx patterns first:

- keep the layered project shape
- keep `EventData` and integration-event conventions
- keep unit-of-work and outbox thinking where reliable publication still matters
- keep validation, execution context, HTTP semantics, and contract patterns

In other words, the **use case should drive the backend**, not the other way around.

## A Practical "What Should I Scaffold?" Checklist

### If your application is mostly CRUD over owned data

Scaffold a standard API domain first. Add:

- contracts with `IIdentifier`, `IETag`, and `IChangeLog` where appropriate
- application service plus validator
- infrastructure repository and SQL Server database project
- API controllers with CoreEx `WebApi` helper style

Do **not** start with orchestration or subscriber hosts unless there is an immediate business requirement.

### If your application must notify other services after changes

Start with the standard API domain, but include outbox and relay from the beginning. That gives you:

- reliable post-commit event publication
- a clean boundary between request handling and broker delivery
- room to add subscribers later without redesigning the write path

### If your application depends heavily on upstream domain data

Scaffold a subscriber host early. This is a strong signal that the service is part of an event-driven landscape and should not rely only on synchronous API calls to other domains.

Typical example:

- Shopping depends on product and inventory-related events.
- The service keeps local state aligned through subscribers while still serving its own API.

### If your application has approvals, callbacks, batches, or days-long flows

Scaffold orchestration intentionally rather than forcing that logic into controllers, subscribers, or background timers. A plain background service can run repeated work, but it is not a substitute for durable workflow state, replay, timers, external events, or compensation logic.

## How to Think About Layering

CoreEx is most effective when you keep the responsibilities sharp:

| Layer | Put this here | Do not put this here |
|---|---|---|
| Contracts | DTOs, identifiers, ETags, change logs, reference-data code properties. | Domain rules, persistence logic, service calls. |
| Application | Validation, unit-of-work orchestration, business use cases, event creation, adapters as interfaces. | HTTP plumbing, EF details, transport-specific code. |
| Infrastructure | Repositories, EF mapping, query config, typed HTTP clients, adapter implementations, outbox publisher plumbing. | Controller concerns and user-facing endpoint logic. |
| API | Routing, request/response behavior, `WebApi` helper usage, OpenAPI metadata, idempotency and HTTP semantics. | Rich business rules or database composition. |
| Subscribe / Worker / Relay | Message consumption, hosted background processing, relay mechanics, orchestration workers. | User-driven request/response logic. |

If a capability changes the transport or execution model, it usually belongs in a host project. If it changes business rules or persistence orchestration, it usually belongs in Application or Infrastructure.

## Opinionated Defaults That Usually Pay Off

For a new greenfield CoreEx service, these defaults are usually worth keeping:

- Use the layered domain shape instead of collapsing everything into the API.
- Use SQL Server first unless you have a strong reason to diverge.
- Use validators rather than ad hoc controller checks.
- Use ETags on mutable resources.
- Use OpenAPI, ProblemDetails, execution context, and health endpoints from the start.
- Use outbox if you already know the service will publish integration events.
- Use reference data orchestration if statuses, categories, or codes are central to the model.

## Things to Avoid Scaffolding Too Early

- A subscriber host when the domain does not actually consume messages yet.
- Orchestration for simple CRUD plus single-event publication.
- Rich aggregate patterns for record-centric admin data with no real invariants.
- Redis/FusionCache before there is either shared-cache need or measurable read pressure.
- CQRS split for every service when read and write concerns are still simple.

## Suggested First Questions for a New Domain

Before scaffolding, answer these:

1. Does this service own its own database schema?
2. Will it publish integration events after writes?
3. Will it consume events or commands from other services?
4. Does it need shared reference data?
5. Are updates concurrent enough to require ETags?
6. Are POST retries likely enough to require idempotency?
7. Is the core business flow synchronous, eventually consistent, or orchestrated over time?
8. Does the model behave like true aggregates with invariants, or mostly like CRUD records?

Those answers usually determine the host set, package set, and scaffold depth more reliably than entity field lists alone.

## Suggested Starter Combinations

| Use case | Scaffold | CoreEx capabilities to prioritize |
|---|---|---|
| Internal admin CRUD API. | API domain + database. | Validation, ETag, change log, OpenAPI, paging/filtering. |
| Master-data service that other domains depend on. | API + database + outbox relay. | Validation, reference data, outbox, Service Bus publisher, idempotency. |
| Event-driven domain maintaining local replicas or reacting to commands. | API + subscribe + outbox relay. | Service Bus subscriber/publisher, outbox, reference data, cache, health/telemetry. |
| Long-running business process or approval workflow. | API + worker/orchestration host, optionally plus outbox/subscribers. | Durable orchestration, telemetry, external-event waits, retries, compensation. |
| Rich aggregate domain with nested rules. | `dotnet new coreex` then `dotnet new coreex-domain` plus only the required hosts. | DomainDriven patterns, validators, Result pipelines where appropriate, explicit mapping. |
| Straightforward conventional entity. | `dotnet new coreex` plus only the required hosts. | Standard contracts/application/infrastructure/API/database shape with minimal custom reasoning. |

## Where to Go Next

- Read `coreex-starter/README.md` for starter/bootstrap expectations.
- Read `docs/capabilities.md` for the underlying framework features and patterns.
- Read `docs/orchestration.md` before adding a workflow worker.
- Read `src/CoreEx.Template/README.md` before scaffolding a new solution or host.
- Use the Product and Shopping samples as the concrete reference architecture for API, subscribe, and outbox relay hosts.

## Evidence

- `coreex-starter/README.md`
- `src/CoreEx.Template/README.md`
- `docs/capabilities.md`
- `docs/orchestration.md`
- `samples/src/Contoso.Products.Api/Program.cs`
- `samples/src/Contoso.Products.Subscribe/Program.cs`
- `samples/src/Contoso.Products.Relay/Program.cs`
