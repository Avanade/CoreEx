# Architecture

## Core Sections (Required)

### 1) Architectural Style

- Primary style: modular layered architecture for the reusable framework, plus event-driven microservice samples.
- Why this classification: sample domains are split into Api, Application, Infrastructure, Domain, Database, Relay, and Subscribe projects, and the runtime flow combines synchronous HTTP with outbox-driven Service Bus messaging.
- Primary constraints: multi-target library packaging across net8/net9/net10; SQL Server-backed unit-of-work and outbox flows in sample hosts; CoreEx-centric patterns such as Result-based orchestration, ETag/idempotency, and dynamic service registration.

### 2) System Flow

```text
HTTP controller -> application service -> domain aggregate or repository -> SQL Server / HTTP adapter -> outbox or direct publisher -> subscriber/consumer -> response
```

Describe the flow in 4-6 steps using file-backed evidence.

1. A controller receives an HTTP request and delegates through CoreEx.WebApi helpers, for example ProductController.PostAsync and PatchAsync.
2. The application service validates input, loads current state when needed, and coordinates a unit-of-work, for example ProductService and BasketService.
3. For Shopping mutations, domain behavior is applied on Basket and BasketItem before persistence.
4. Infrastructure repositories translate between domain/contracts and EF-backed persistence models, for example BasketRepository and ProductRepository.
5. Cross-service behavior happens through a typed HTTP client and adapter for real-time reservation, plus outbox messages or direct Service Bus publishing for async commands.
6. Relay and subscriber hosts move outbox records to Azure Service Bus and consume messages back into application services.

### 3) Layer/Module Responsibilities

| Layer or module | Owns | Must not own | Evidence |
|-----------------|------|--------------|----------|
| API controllers | HTTP routes, request/response semantics, idempotency attributes, WebApi delegation | Domain rules and persistence queries | samples/src/Contoso.Products.Api/Controllers/ProductController.cs |
| Application services | Validation, orchestration, Result pipelines, unit-of-work/event creation | ASP.NET startup and EF entity tracking details | samples/src/Contoso.Products.Application/ProductService.cs; samples/src/Contoso.Shopping.Application/BasketService.cs |
| Domain | Aggregate/entity invariants and mutation rules | Transport and infrastructure concerns | samples/src/Contoso.Shopping.Domain/Basket.cs; samples/src/Contoso.Shopping.Domain/BasketItem.cs |
| Infrastructure | EF DbContext access, query config, mapping, typed clients, adapters | Public HTTP endpoint definitions | samples/src/Contoso.Products.Infrastructure/Repositories/ProductRepository.cs; samples/src/Contoso.Shopping.Infrastructure/Repositories/BasketRepository.cs; samples/src/Contoso.Shopping.Infrastructure/Adapters/ProductAdapter.cs |
| Relay/subscriber hosts | Background message movement and consumption | Business orchestration for user-facing HTTP requests | samples/src/Contoso.Products.Relay/Program.cs; samples/src/Contoso.Products.Subscribe/Program.cs |

### 4) Reused Patterns

| Pattern | Where found | Why it exists |
|---------|-------------|---------------|
| Unit of Work + Outbox | ProductService, BasketService, Products/Shopping relay hosts | Persist state and enqueue events/commands atomically before broker delivery |
| Repository | ProductRepository, BasketRepository | Separate data access/mapping from application services |
| Aggregate / Entity / Value Object | Basket, BasketItem, ItemPricing | Keep mutation rules and consistency checks in the domain model |
| Anti-corruption / adapter | ProductAdapter, ProductsHttpClient | Isolate Shopping from Products API and message semantics |
| Dynamic service registration | AddDynamicServicesUsing in API and subscriber hosts | Reduce explicit DI wiring across layered sample projects |
| Roslyn source generation | gen/CoreEx.Generator, generated .g.cs persistence files | Generate boilerplate and analyzer-time artifacts |

### 5) Known Architectural Risks

- Sample host bootstrapping is repeated across Products, Shopping, and Orders hosts; the repeated AddExecutionContext/AddMvcWebApi/cache/SQL/OpenTelemetry wiring increases configuration-drift risk.
- Shopping checkout intentionally mixes a transactional outbox path with a direct broker fallback on failure; that keeps reservations from being stranded, but it also creates two publication paths that must stay behaviorally aligned.

### 6) Evidence

- samples/src/Contoso.Products.Api/Controllers/ProductController.cs
- samples/src/Contoso.Products.Api/Program.cs
- samples/src/Contoso.Shopping.Api/Program.cs
- samples/src/Contoso.Products.Application/ProductService.cs
- samples/src/Contoso.Shopping.Application/BasketService.cs
- samples/src/Contoso.Shopping.Domain/Basket.cs
- samples/src/Contoso.Products.Infrastructure/Repositories/ProductRepository.cs
- samples/src/Contoso.Shopping.Infrastructure/Repositories/BasketRepository.cs
- samples/src/Contoso.Shopping.Infrastructure/Adapters/ProductAdapter.cs
- samples/src/Contoso.Products.Relay/Program.cs
- samples/src/Contoso.Products.Subscribe/Program.cs
- gen/CoreEx.Generator.csproj
