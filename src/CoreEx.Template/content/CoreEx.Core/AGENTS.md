# app-name -- CoreEx Application Services

This represents the **CoreEx domain-based application services** for the `domain-name` domain.

**AI assistance:** To install CoreEx AI workflow assets (instructions, prompts, agents) for this solution, run `dotnet new coreex-ai` at the **repo root**:

```bash
# Single-repo (most common):
dotnet new coreex-ai

# Monorepo (CoreEx under a subfolder):
dotnet new coreex-ai --app-folder <relative-path-from-root>
```

Once installed, run `/coreex-scaffold` to add missing hosts or `/coreex-expert` for architecture guidance.

> Re-run `dotnet new coreex-ai` (or `/coreex-docs-sync`) after bumping the CoreEx NuGet version in `Directory.Packages.props`.

---

## Project Structure

```
app-name/
+-- src/
|   +-- app-name.Contracts/        # Public contracts: entities, DTOs, event schemas
|   +-- app-name.Application/      # Business logic: services, validators, repository interfaces
|   +-- app-name.Domain/           # (domain-driven-enabled) Aggregates, value objects, domain events
|   +-- app-name.Infrastructure/   # EF Core repositories, outbox, external integrations
+-- tools/
|   +-- app-name.Database/         # (data-provider != None) Database migrations (DbEx)
|   +-- app-name.CodeGen/          # (refdata-enabled && data-provider != None) Ref-data code gen
+-- tests/
|   +-- app-name.Test.Common/      # Shared test infrastructure: TestData marker, embedded seed data
|   +-- app-name.Test.Unit/        # Fast isolated unit tests (validators, services, no I/O)
+-- Directory.Packages.props       # Central NuGet version management (no versions in .csproj)
```

---

## Feature Configuration

<!-- #if implement-sqlserver -->
- **Data provider:** SQL Server (`CoreEx.Database.SqlServer`, `CoreEx.EntityFrameworkCore`)
<!-- #elif implement-postgres -->
- **Data provider:** PostgreSQL (`CoreEx.Database.Postgres`, `CoreEx.EntityFrameworkCore`)
<!-- #else -->
- **Data provider:** None -- facade solution (e.g. over Dynamics 365 via HttpClient)
<!-- #endif -->
<!-- #if (refdata-enabled && !implement-none-data) -->
- **Reference data:** Enabled -- `src/app-name.Application/ReferenceDataService.cs` and `tools/app-name.CodeGen/`
<!-- #else -->
- **Reference data:** Disabled
<!-- #endif -->
<!-- #if domain-driven-enabled -->
- **Domain project:** Enabled -- `src/app-name.Domain/` (aggregates, value objects)
<!-- #else -->
- **Domain project:** Disabled -- domain logic lives in Application
<!-- #endif -->
<!-- #if rop-enabled -->
- **Railway-Oriented Programming:** Enabled -- service methods return `Result`/`Result<T>`
<!-- #else -->
- **Railway-Oriented Programming:** Disabled -- standard exception-based error handling
<!-- #endif -->
<!-- #if (outbox-enabled && !implement-none-data) -->
- **Transactional outbox:** Enabled -- events committed atomically with data via the outbox table
<!-- #else -->
- **Transactional outbox:** Disabled
<!-- #endif -->
<!-- #if implement-servicebus -->
- **Messaging:** Azure Service Bus (`CoreEx.Azure.Messaging.ServiceBus`)
<!-- #else -->
- **Messaging:** None configured
<!-- #endif -->

---

## Relevant Docs

After running `dotnet new coreex-ai` at the repo root, the following are available:

- `.github/docs/coreex/layers.md` -- full layered architecture and dependency rules
- `.github/docs/coreex/patterns.md` -- CoreEx request/response and event patterns
- `.github/docs/coreex/application-scaffolding-guide.md` -- choosing the smallest safe CoreEx solution shape before adding code
- `.github/docs/coreex/contracts-layer.md` -- entities, DTOs, event schemas
- `.github/docs/coreex/application-layer.md` -- services, validators, repository interfaces
- `.github/docs/coreex/infrastructure-layer.md` -- EF Core, outbox, external integrations
- `.github/docs/coreex/testing.md` -- test project setup, `WithGenericTester`, `WithApiTester`
- `.github/docs/coreex/local-dev.md` -- running locally with .NET Aspire
- `.github/docs/coreex/tooling.md` -- Database and CodeGen tool projects

