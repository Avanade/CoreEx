# Codebase Concerns

## Core Sections (Required)

### 1) Top Risks (Prioritized)

| Severity | Concern | Evidence | Impact | Suggested action |
|----------|---------|----------|--------|------------------|
| high | Sample credentials and connection strings are committed in local/dev artifacts. | docker-compose.yml; samples/src/Contoso.Products.Database/Program.cs; samples/tests/Contoso.E2E.Runner/appsettings.json | Increases the chance that local-only credentials are reused or copied into non-local environments. | Move sample secrets to user-secrets or env-template files and keep checked-in values obviously non-reusable. |
| medium | Multi-backend intent exists, but the current concrete implementation and scaffolding are SQL Server-centric; this can be misread as either SQL-only or equally mature multi-provider support. | README.md; docs/capabilities.md; src/CoreEx.Database.SqlServer/CoreEx.Database.SqlServer.csproj; docker-compose.yml | Onboarding and design decisions may assume the wrong provider maturity level. | Document provider strategy explicitly as SQL Server-primary with other backends added when needed. |
| medium | Sample host startup is duplicated across multiple API, relay, and subscriber Program.cs files. | samples/src/Contoso.Products.Api/Program.cs; samples/src/Contoso.Shopping.Api/Program.cs; samples/src/Contoso.Orders.Api/Program.cs; samples/src/Contoso.Products.Outbox.Relay/Program.cs; samples/src/Contoso.Products.Subscribe/Program.cs | Repeated bootstrap code can drift across domains and host types. | Extract common host-registration extensions or add tests/assertions around expected startup composition. |
| medium | Shopping checkout uses both transactional outbox publication and a direct broker fallback path. | samples/src/Contoso.Shopping.Application/BasketService.cs; samples/src/Contoso.Shopping.Infrastructure/Adapters/ProductAdapter.cs | Two message publication paths must remain semantically aligned during failure handling. | Add focused tests and documentation around compensation/fallback behavior. |

### 2) Technical Debt

| Debt item | Why it exists | Where | Risk if ignored | Suggested fix |
|-----------|---------------|-------|-----------------|---------------|
| Repeated host wiring | Each sample host configures overlapping CoreEx, cache, SQL, OpenTelemetry, and health-check setup inline | samples/src/Contoso.Products.Api/Program.cs; samples/src/Contoso.Shopping.Api/Program.cs; samples/src/Contoso.Orders.Api/Program.cs; samples/src/Contoso.Products.Subscribe/Program.cs | Behavioral drift between services and hosts becomes harder to spot | Introduce shared extension methods for common host composition |
| Sample status visibility | README.md describes two complete reference solutions, while the solution also contains Orders and order-workflow sample projects that are still in progress | README.md; CoreEx.sln | New contributors may misclassify in-progress samples as production-ready references | Label in-progress sample status in top-level docs and sample READMEs |
| Secret handling in examples | Sample-local secrets are embedded directly in repo files | docker-compose.yml; samples/src/Contoso.Products.Database/Program.cs; samples/tests/Contoso.E2E.Runner/appsettings.json | Normalizes insecure copy/paste patterns | Replace checked-in secrets with placeholders and env-driven overrides |

### 3) Security Concerns

| Risk | OWASP category (if applicable) | Evidence | Current mitigation | Gap |
|------|--------------------------------|----------|--------------------|-----|
| Checked-in passwords/connection strings in sample assets | A02 Cryptographic Failures / Secrets Management | docker-compose.yml; samples/src/Contoso.Products.Database/Program.cs; samples/tests/Contoso.E2E.Runner/appsettings.json | These appear scoped to local/dev usage only | The repo does not provide a committed env template or explicit secret-handling guardrail for these values |
| Local Aspire dashboard allows anonymous access | A01 Broken Access Control | docker-compose.yml | This is clearly configured for local development only | No environment-specific guard in the committed compose file other than the setting name itself |
| Internal Products API client shows no explicit auth configuration in inspected code | A01 Broken Access Control | samples/src/Contoso.Shopping.Infrastructure/Clients/ProductsHttpClient.cs; samples/src/Contoso.Shopping.Api/Program.cs | [TODO] auth may be applied elsewhere, but it was not visible in inspected files | The inspected client/host files do not show authentication or authorization for the inter-service call |

### 4) Performance and Scaling Concerns

| Concern | Evidence | Current symptom | Scaling risk | Suggested improvement |
|---------|----------|-----------------|-------------|-----------------------|
| Repeated cache/telemetry/bootstrap configuration per host | samples/src/Contoso.Products.Api/Program.cs; samples/src/Contoso.Shopping.Api/Program.cs; samples/src/Contoso.Products.Subscribe/Program.cs | Configuration parity depends on copy/paste discipline | One host can lag behind others in cache or telemetry behavior | Centralize shared startup composition |
| Checkout performs a synchronous cross-service reservation call before finalizing the transaction | samples/src/Contoso.Shopping.Application/BasketService.cs; samples/src/Contoso.Shopping.Infrastructure/Adapters/ProductAdapter.cs | Checkout latency includes remote API availability and response time | Higher latency or transient failures in Products directly affect Shopping checkout | Add explicit resilience policy configuration and document timeout/retry expectations |
| No explicit timeout/retry settings were found in inspected HTTP client wiring | samples/src/Contoso.Shopping.Api/Program.cs; samples/src/Contoso.Shopping.Infrastructure/Clients/ProductsHttpClient.cs | Runtime behavior depends on defaults or hidden configuration | Failure recovery characteristics are hard to reason about | Add explicit resilience/timeout configuration in host setup |

### 5) Fragile/High-Churn Areas

| Area | Why fragile | Churn signal | Safe change strategy |
|------|-------------|-------------|----------------------|
| samples/src/*/Program.cs host bootstraps | Several hosts repeat nearly the same registration pattern with small variations | Structural duplication is visible in inspected Program.cs files; 90-day and 365-day git queries over src and samples/src produced a flat result with no clear hotspot above 1 touched commit per listed file | Change one host pattern, then compare every sibling host and run the corresponding sample tests |
| src/CoreEx.Validation/* | Validation files are the most visible source family in the one-year churn sample, but the signal is still flat at 1 touched commit per listed file | Terminal git log --since='365 days ago' sample returned multiple CoreEx.Validation files, each with count 1 | Keep changes small and run the related unit test projects immediately |

### 6) [ASK USER] Questions

1. No open [ASK USER] items remain for this pass.

### 7) Evidence

- README.md
- docs/capabilities.md
- CoreEx.sln
- docker-compose.yml
- src/CoreEx.Database.SqlServer/CoreEx.Database.SqlServer.csproj
- samples/src/Contoso.Products.Api/Program.cs
- samples/src/Contoso.Shopping.Api/Program.cs
- samples/src/Contoso.Orders.Api/Program.cs
- samples/src/Contoso.Products.Subscribe/Program.cs
- samples/src/Contoso.Shopping.Application/BasketService.cs
- samples/src/Contoso.Shopping.Infrastructure/Adapters/ProductAdapter.cs
- samples/src/Contoso.Products.Database/Program.cs
- samples/tests/Contoso.E2E.Runner/appsettings.json
