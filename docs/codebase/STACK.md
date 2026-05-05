# Technology Stack

## Core Sections (Required)

### 1) Runtime Summary

| Area | Value | Evidence |
|------|-------|----------|
| Primary language | C# | src/Directory.Build.props; CoreEx.sln |
| Runtime + version | Reusable libraries target net8.0, net9.0, and net10.0; sample hosts use net10.0; generator targets netstandard2.0 | src/Directory.Build.props; samples/Directory.Build.props; samples/aspire/Contoso.Aspire/Contoso.Aspire.csproj; gen/CoreEx.Gen/CoreEx.Gen.csproj |
| Package manager | NuGet with Central Package Management | Directory.Packages.props |
| Module/build system | MSBuild project/solution build with SDK-style .csproj files | CoreEx.sln; src/CoreEx/CoreEx.csproj |

### 2) Production Frameworks and Dependencies

| Dependency | Version | Role in system | Evidence |
|------------|---------|----------------|----------|
| ASP.NET Core | 8.0.24 / 9.0.13 / 10.0.3 | Web API hosting, controllers, OpenAPI support | Directory.Packages.props; src/CoreEx.AspNetCore/CoreEx.AspNetCore.csproj |
| Entity Framework Core + SQL Server | 8.0.22-10.0.0 | Data access for sample infrastructure and CoreEx EF integration | Directory.Packages.props; samples/src/Contoso.Shopping.Infrastructure/Contoso.Shopping.Infrastructure.csproj |
| Microsoft.Data.SqlClient + Aspire SQL client | 6.1.4 / 13.2.2 | SQL Server connectivity | Directory.Packages.props; src/CoreEx.Database.SqlServer/CoreEx.Database.SqlServer.csproj |
| NSwag.AspNetCore | 14.6.2 | OpenAPI generation for sample APIs | Directory.Packages.props; samples/src/Contoso.Products.Api/Program.cs |
| OpenTelemetry | 1.15.0 family | Tracing and OTLP export across APIs, relays, subscribers, and workflow worker | Directory.Packages.props; samples/src/Contoso.Products.Api/Program.cs; samples/src/Contoso.Products.Outbox.Relay/Program.cs; samples/src/Contoso.Order.Workflow.Worker/Program.cs |
| Azure Service Bus Aspire integration | 13.2.2 | Messaging publisher/subscriber wiring | Directory.Packages.props; src/CoreEx.Azure.Messaging.ServiceBus/CoreEx.Azure.Messaging.ServiceBus.csproj; samples/src/Contoso.Products.Subscribe/Program.cs |
| FusionCache + Redis backplane | 2.5.0 | Hybrid cache and idempotency/caching support | Directory.Packages.props; src/CoreEx.Caching.FusionCache/CoreEx.Caching.FusionCache.csproj; samples/src/Contoso.Shopping.Api/Program.cs |
| CloudNative.CloudEvents.SystemTextJson | 2.8.0 | CloudEvent interoperability | Directory.Packages.props |
| Microsoft.DurableTask.* | 1.17.1 | Order workflow sample orchestration and worker runtime | Directory.Packages.props; samples/src/Contoso.Order.Workflow.Worker/Program.cs; samples/src/Contoso.Order.Workflow.Workflow/Contoso.Order.Workflow.Workflow.csproj |
| DbEx.SqlServer | 3.0.0-preview-2 | Database migration/data console utilities in samples/tests | Directory.Packages.props; samples/src/Contoso.Products.Database/Program.cs |

### 3) Development Toolchain

| Tool | Purpose | Evidence |
|------|---------|----------|
| NUnit | Test framework | Directory.Packages.props; tests/CoreEx.Test.Unit/CoreEx.Test.Unit.csproj |
| AwesomeAssertions | Assertions | Directory.Packages.props; tests/CoreEx.Test.Unit/CoreEx.Test.Unit.csproj |
| coverlet.collector | Test coverage collection | Directory.Packages.props; tests/CoreEx.Test.Unit/CoreEx.Test.Unit.csproj |
| UnitTestEx / UnitTestEx.NUnit | API and integration-style test helpers | Directory.Packages.props; samples/tests/Contoso.Products.Test.Api/Contoso.Products.Test.Api.csproj |
| CoreEx.Gen | Roslyn analyzer/source generator packaged as an analyzer | gen/CoreEx.Gen/CoreEx.Gen.csproj; tests/CoreEx.Test.Unit/CoreEx.Test.Unit.csproj |
| .editorconfig | Formatting baseline | .editorconfig |

### 4) Key Commands

```bash
dotnet build CoreEx.sln
dotnet test CoreEx.sln
dotnet run --project samples/aspire/Contoso.Aspire
docker compose up -d db-sql-server redis-cache servicebus-emulator aspire-dashboard dts-emulator
```

### 5) Environment and Config

- Config sources: appsettings.json and appsettings.Development.json in sample hosts, docker-compose.yml, servicebus/Config.json, central MSBuild props, and a UserSecretsId in the Aspire host.
- Required env vars: dts-endpoint, TASKHUB, [TODO] additional deployment/runtime variables are not summarized in a committed env template.
- Deployment/runtime constraints: local sample execution expects SQL Server, Redis, Azure Service Bus emulator, and Aspire dashboard infrastructure. Current concrete implementation/scaffolding in inspected code is SQL Server-primary, with broader backend support discussed in docs as a capability direction.

### 6) Evidence

- Directory.Packages.props
- src/Directory.Build.props
- CoreEx.sln
- src/CoreEx/CoreEx.csproj
- src/CoreEx.Database.SqlServer/CoreEx.Database.SqlServer.csproj
- src/CoreEx.Azure.Messaging.ServiceBus/CoreEx.Azure.Messaging.ServiceBus.csproj
- src/CoreEx.Caching.FusionCache/CoreEx.Caching.FusionCache.csproj
- samples/aspire/Contoso.Aspire/Contoso.Aspire.csproj
- samples/src/Contoso.Products.Api/Program.cs
- samples/src/Contoso.Shopping.Api/Program.cs
- samples/src/Contoso.Order.Workflow.Worker/Program.cs
- docker-compose.yml
