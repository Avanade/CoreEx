# External Integrations

## Core Sections (Required)

### 1) Integration Inventory

| System | Type (API/DB/Queue/etc) | Purpose | Auth model | Criticality | Evidence |
|--------|---------------------------|---------|------------|-------------|----------|
| SQL Server | DB | Primary persistence for sample domains, outbox tables, and migration utilities | Connection string-based | High | docker-compose.yml; samples/src/Contoso.Products.Database/Program.cs; samples/src/Contoso.Products.Api/Program.cs |
| Redis | Cache/backplane | L2 distributed cache and FusionCache backplane | Connection configured through Aspire/registered ConfigurationOptions | Medium | docker-compose.yml; samples/src/Contoso.Products.Api/Program.cs; samples/src/Contoso.Shopping.Api/Program.cs |
| Azure Service Bus | Queue/topic broker | Async event publishing, relay, and subscriber processing | Connection configured through Aspire/host config; emulator config committed for local use | High | servicebus/Config.json; samples/src/Contoso.Products.Outbox.Relay/Program.cs; samples/src/Contoso.Products.Subscribe/Program.cs |
| Products API from Shopping | Internal HTTP API | Real-time inventory reservation during checkout | [TODO] no explicit auth configuration was found in the inspected Shopping client code | High | samples/src/Contoso.Shopping.Infrastructure/Clients/ProductsHttpClient.cs; samples/src/Contoso.Shopping.Infrastructure/Adapters/ProductAdapter.cs |
| OTLP / Aspire dashboard | Observability endpoint | Export traces from sample hosts and inspect them locally | No auth found in local compose config; dashboard is configured for anonymous local access | Medium | docker-compose.yml; samples/src/Contoso.Products.Api/Program.cs; samples/src/Contoso.Products.Outbox.Relay/Program.cs |
| Durable Task Scheduler | Workflow runtime | Order workflow worker orchestration sample | Connection string assembled from endpoint/task hub and auth mode | Medium | samples/src/Contoso.Order.Workflow.Worker/Program.cs |

### 2) Data Stores

| Store | Role | Access layer | Key risk | Evidence |
|-------|------|--------------|----------|----------|
| SQL Server | Transactional domain storage, outbox, and migration target | EF-backed repositories and DbEx console utilities | Sample connection strings and passwords are committed in local/dev artifacts | samples/src/Contoso.Shopping.Infrastructure/Contoso.Shopping.Infrastructure.csproj; samples/src/Contoso.Products.Database/Program.cs; samples/tests/Contoso.E2E.Runner/appsettings.json |
| Redis | Hybrid cache and backplane | FusionCache + AddRedisDistributedCache + CoreEx hybrid cache abstractions | Cache invalidation/consistency depends on host parity across repeated startup code | samples/src/Contoso.Products.Api/Program.cs; samples/src/Contoso.Shopping.Api/Program.cs |

### 3) Secrets and Credentials Handling

- Credential sources: docker-compose environment variables, appsettings.json for samples/E2E runner, UserSecretsId in the Aspire host, and runtime configuration/environment variables in the workflow worker.
- Hardcoding checks: committed sample credentials and connection strings are present in docker-compose.yml, samples/src/Contoso.Products.Database/Program.cs, and samples/tests/Contoso.E2E.Runner/appsettings.json.
- Rotation or lifecycle notes: [TODO] no secret-rotation guidance or secret-manager policy file was found.

### 4) Reliability and Failure Behavior

- Retry/backoff behavior: transactional outbox relays are implemented for Products and Shopping; [TODO] no explicit HTTP retry/backoff policy configuration was found in the inspected host files.
- Timeout policy: [TODO] no explicit timeout configuration was found in the inspected host or client files.
- Circuit-breaker or fallback behavior: Shopping checkout falls back to direct broker publication for reservation cancellation if the transactional path fails; Service Bus subscriber sessions set MaxConcurrentSessions and emulator MaxDeliveryCount is configured.

### 5) Observability for Integrations

- Logging around external calls: yes, host-level logging is configured via appsettings and the checkout failure path logs an error before sending a direct cancellation command.
- Metrics/tracing coverage: yes, sample APIs, relays, subscribers, and the workflow worker all add OpenTelemetry tracing and OTLP export.
- Missing visibility gaps: [TODO] no committed alerting, dashboard provisioning, or SLO configuration was found beyond the local Aspire dashboard container.

### 6) Evidence

- docker-compose.yml
- servicebus/Config.json
- samples/aspire/Contoso.Aspire/Contoso.Aspire.csproj
- samples/src/Contoso.Products.Api/Program.cs
- samples/src/Contoso.Shopping.Api/Program.cs
- samples/src/Contoso.Products.Outbox.Relay/Program.cs
- samples/src/Contoso.Products.Subscribe/Program.cs
- samples/src/Contoso.Shopping.Infrastructure/Clients/ProductsHttpClient.cs
- samples/src/Contoso.Shopping.Infrastructure/Adapters/ProductAdapter.cs
- samples/src/Contoso.Order.Workflow.Worker/Program.cs
- samples/src/Contoso.Products.Database/Program.cs
- samples/tests/Contoso.E2E.Runner/appsettings.json
