# Layers

CoreEx promotes a clean, layered architecture for building enterprise APIs and distributed services. The sample domains — **Products** (Postgres) and **Shopping** (SQL Server) — demonstrate these patterns in a polyglot setting, showing that the same architectural approach scales across different persistence technologies. Each domain is decomposed into business layers (**Contracts**, **Domain** (optional), **Application**, **Infrastructure**) and one or more host layers (**API**, **Outbox Relay**, **Subscribe**). The business layers enforce a strict inward dependency rule — `Domain` references only `Contracts`; `Application` references `Contracts` and `Domain`; `Infrastructure` references `Application` (and transitively `Domain`) — never the reverse; the host layers act as composition roots that wire everything together and delegate to Application logic.

> **Polyglot data**: Products is backed by PostgreSQL via [`CoreEx.Database.Postgres`](../../src/CoreEx.Database.Postgres) and [`CoreEx.EntityFrameworkCore`](../../src/CoreEx.EntityFrameworkCore). Shopping uses SQL Server via [`CoreEx.Database.SqlServer`](../../src/CoreEx.Database.SqlServer) and the same EF Core integration. The layers above the Infrastructure boundary are completely unaware of the underlying database technology.

```
                   caller / message broker
                           │
          ┌────────────────┼────────────────┐
          ▼                ▼                ▼
  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
  │  API Host    │ │Outbox Relay  │ │Subscribe Host│  ← Host layers (composition roots)
  └──────┬───────┘ └──────────────┘ └──────┬───────┘
         │                                 │
         ▼                                 ▼
  ┌────────────────────────────────────────────────┐   ╔══════════════════╗
  │                  Application                   │   ║  *.CodeGen       ║ ← generates .g.cs
  ├────────────────────────────────────────────────┤   ║  (design-time)   ║   into all layers
  │             Domain  (optional)                 │   ╚══════════════════╝
  ├────────────────────────────────────────────────┤   ╔══════════════════╗
  │                Infrastructure                  │   ║  *.Database      ║ ← manages schema,
  ├────────────────────────────────────────────────┤   ║  (design-time)   ║   seeds data &
  │                  Contracts                     │   ║                  ║   generates .g.cs
  └────────────────────────────────────────────────┘   ╚══════════════════╝
```

## Business layers

| Layer | Description | Detail |
|---|---|---|
| 📋 **Contracts** | Public API and messaging surface — entity contracts, reference data, and source generation. | [contracts-layer.md](contracts-layer.md) |
| 🧩 **Domain** | Optional aggregate and value-object layer, introduced when rich domain rules require a dedicated consistency boundary. | [domain-layer.md](domain-layer.md) |
| ⚙️ **Application** | Business logic orchestration — services, validators, repository interfaces, adapters, mapping, and policies. | [application-layer.md](application-layer.md) |
| 🗄️**Infrastructure** | Concrete persistence, mapping, HTTP clients, and adapter implementations wired in via DI. | [infrastructure-layer.md](infrastructure-layer.md) |

## Host layers

Host layers are the deployable processes. Each is a composition root that wires the business layers together via DI and exposes a runtime surface (HTTP endpoints, background workers, health checks). They delegate all business operations to the Application layer and contain no business logic themselves.

| Host | Description | Detail |
|---|---|---|
| 🌐 **API** | Exposes the domain's capabilities as HTTP endpoints via MVC controllers or minimal APIs. | [hosts-layer.md](hosts-layer.md#api-host) |
| 📤 **Outbox Relay** | Polls the transactional outbox and forwards committed events to the message broker. | [hosts-layer.md](hosts-layer.md#outbox-relay-host) |
| 📥 **Subscribe** | Receives inbound events from the message broker and routes them to Application-layer logic via Subscriber classes. | [hosts-layer.md](hosts-layer.md#subscribe-host) |

## Developer Tooling

These are design-time console projects that feed the layer stack but have no runtime presence. They are run locally by developers to generate code and manage the database schema.

| Tool | Description | Detail |
|---|---|---|
| 🛠️ **CodeGen** | Scaffolds the full reference-data implementation (contract, controller, service, repository interface, repository, mapper) across all target layers from a single `ref-data.yaml`. | [tooling.md](tooling.md#code-generation-codegen) |
| 🗃️ **Database** | Full database lifecycle management: creates and migrates the schema, provisions transactional outbox infrastructure (stored procedures / functions), seeds reference and master data, and generates Infrastructure-layer persistence models (`Persistence/*.g.cs`) and the EF Core `DbContext` partial (`Repositories/*DbContext.g.cs`). This essentially becomes the primary deployment tool that should be used across all environments. | [tooling.md](tooling.md#database-management-database) |

## Testing

Tests are organised by host boundary — each `*.Test.*` project tests one deployable unit in isolation. Intra-domain dependencies (database, cache, outbox) are real; inter-domain calls (HTTP to other domains, direct broker publishes) are mocked. See [testing.md](testing.md) for the full testing guide.

For cross-domain end-to-end validation and load simulation, all hosts are run together under [Aspire](aspire.md), and the [E2E Runner](aspire.md#e2e-runner) drives real workloads across the full system.

