# Codebase Structure

## Core Sections (Required)

### 1) Top-Level Map

List only meaningful top-level directories and files.

| Path | Purpose | Evidence |
|------|---------|----------|
| src/ | CoreEx reusable library packages | CoreEx.sln; README.md |
| tests/ | Unit and API-style tests for CoreEx libraries | CoreEx.sln; tests/CoreEx.Test.Unit/CoreEx.Test.Unit.csproj |
| samples/src/ | Contoso sample applications and hosts by domain/layer, including in-progress Orders and Order.Workflow sample areas | CoreEx.sln; samples/README.md |
| samples/tests/ | Sample API, relay, subscriber, unit, and E2E test projects | CoreEx.sln; samples/README.md |
| samples/aspire/ | Aspire AppHost that references runnable sample services | samples/aspire/Contoso.Aspire/Contoso.Aspire.csproj |
| gen/ | Roslyn source generator/analyzer project | CoreEx.sln; gen/CoreEx.Generator/CoreEx.Generator.csproj |
| docs/ | Technical notes and generated codebase knowledge docs | README.md; docs/capabilities.md |
| ref/ | Separate reference solution content (NDCOrderOrchestration.sln) | list_dir output; ref/NDCOrderOrchestration.sln |
| servicebus/ | Emulator configuration for local Azure Service Bus topics/subscriptions | CoreEx.sln; servicebus/Config.json |
| tools/ | Repo utility area | list_dir output |

### 2) Entry Points

- Main runtime entry: there is no single root application entry; runnable entry points are sample host Program.cs files under samples/src/ plus the Aspire AppHost in samples/aspire/Contoso.Aspire.
- Secondary entry points (worker/cli/jobs): database console utilities in samples/src/*Database/Program.cs, outbox relays in samples/src/*.Outbox.Relay/Program.cs, subscriber hosts in samples/src/*.Subscribe/Program.cs, and the order workflow worker in samples/src/Contoso.Order.Workflow.Worker/Program.cs.
- How entry is selected (script/config): projects are selected explicitly via dotnet run --project ..., as shown in README.md and samples/README.md.

### 3) Module Boundaries

| Boundary | What belongs here | What must not be here |
|----------|-------------------|------------------------|
| src/CoreEx.* | Reusable framework primitives, ASP.NET integration, data, validation, events, caching, and unit-testing helpers | Sample-specific domain logic |
| samples/src/Contoso.*.Api | HTTP host bootstrap and controllers | Direct persistence details beyond injected services/repositories |
| samples/src/Contoso.*.Application | Use-case orchestration, validation, repository interfaces, adapters, and service contracts | ASP.NET host wiring and low-level EF mappings |
| samples/src/Contoso.Shopping.Domain | Aggregate/entity/value-object behavior | HTTP transport or EF DbContext concerns |
| samples/src/Contoso.*.Infrastructure | EF repositories, mappers, typed clients, adapters, outbox publishers | Web host startup |
| samples/tests/ and tests/ | Automated tests, test data, and test-only resources | Production host/runtime code |

### 4) Naming and Organization Rules

- File naming pattern: PascalCase .cs filenames such as ProductService.cs, BasketRepository.cs, ProductController.cs, and ExceptionTests.cs.
- Directory organization pattern: mostly layer-first under samples (Api, Application, Infrastructure, Domain, Database, Subscribe, Outbox.Relay) and package-first under src (CoreEx.*, CoreEx.AspNetCore.*, CoreEx.Database.*).
- Import aliasing or path conventions: standard C# project references and global using files are used; no TypeScript-style path alias system exists in the inspected files.

### 5) Evidence

- CoreEx.sln
- README.md
- samples/README.md
- samples/aspire/Contoso.Aspire/Contoso.Aspire.csproj
- samples/src/Contoso.Products.Api/Program.cs
- samples/src/Contoso.Products.Database/Program.cs
- samples/src/Contoso.Products.Outbox.Relay/Program.cs
- samples/src/Contoso.Products.Subscribe/Program.cs
- samples/src/Contoso.Shopping.Domain/Basket.cs
- gen/CoreEx.Generator/CoreEx.Generator.csproj
