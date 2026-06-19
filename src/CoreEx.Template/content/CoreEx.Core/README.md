# app-name

A CoreEx microservice for the `domain-name` domain.

## This solution

<!-- #if implement-sqlserver -->
- **Data provider:** SQL Server (`CoreEx.Database.SqlServer`, `CoreEx.EntityFrameworkCore`)
<!-- #endif -->
<!-- #if implement-postgres -->
- **Data provider:** PostgreSQL (`CoreEx.Database.Postgres`, `CoreEx.EntityFrameworkCore`)
<!-- #endif -->
<!-- #if implement-none-data -->
- **Data provider:** None — facade over an external system (no local database)
<!-- #endif -->
<!-- #if (refdata-enabled && !implement-none-data) -->
- **Reference data:** Enabled — `tools/app-name.CodeGen/` + `src/app-name.Application/ReferenceDataService.cs`
<!-- #endif -->
<!-- #if domain-driven-enabled -->
- **Domain layer:** Enabled — `src/app-name.Domain/` (aggregates, value objects)
<!-- #endif -->
<!-- #if rop-enabled -->
- **Railway-oriented programming:** Enabled — services return `Result`/`Result<T>`
<!-- #endif -->
<!-- #if (outbox-enabled && !implement-none-data) -->
- **Transactional outbox:** Enabled — events committed atomically with data
<!-- #endif -->
<!-- #if implement-servicebus -->
- **Messaging:** Azure Service Bus (`CoreEx.Azure.Messaging.ServiceBus`)
<!-- #endif -->

---

## Prerequisites

| Requirement | Notes |
|---|---|
| **.NET SDK** | `net10.0` — see `Directory.Build.props` |
| **Podman** (preferred) or **Docker** | Required to run the infrastructure containers |

### Git configuration (Windows)

```bash
git config --global core.autocrlf input
```

Run this once. Git for Windows defaults to `core.autocrlf=true`, which overrides the `.gitattributes` LF enforcement and causes noisy diffs. `input` mode stores LF on commit without expanding to CRLF on checkout.

---

## Solution structure

```
app-name/
├── src/
│   ├── app-name.Contracts/        # Public contracts, DTOs, event schemas
│   ├── app-name.Application/      # Services, validators, repository interfaces
<!-- #if domain-driven-enabled -->
│   ├── app-name.Domain/           # Aggregates, value objects, domain events
<!-- #endif -->
│   └── app-name.Infrastructure/   # EF Core repositories, outbox, external adapters
<!-- #if !implement-none-data -->
├── tools/
<!-- #if (refdata-enabled && !implement-none-data) -->
│   ├── app-name.CodeGen/          # Reference data C# generation (reads ref-data.yaml)
<!-- #endif -->
│   └── app-name.Database/         # Database migrations and seeding (DbEx)
<!-- #endif -->
└── tests/
    ├── app-name.Test.Common/      # Shared test infrastructure and seed data
    └── app-name.Test.Unit/        # Fast isolated unit tests (no I/O)
```

Host projects (`app-name.Api`, `app-name.Relay`, `app-name.Subscribe`) are added separately with `dotnet new coreex-api`, `coreex-relay`, and `coreex-subscribe`. See [Adding hosts](#adding-hosts) below.

---

## Infrastructure

<!-- #if !implement-none-data -->
Start the containers before running the solution or any integration tests:

```bash
podman compose up -d   # Podman (preferred)
docker compose up -d   # Docker
```

| Container | Port(s) | Purpose |
|---|---|---|
<!-- #if implement-sqlserver -->
| `db-sql-server` | 1433 | SQL Server — domain schema and data |
<!-- #endif -->
<!-- #if implement-postgres -->
| `db-postgres` | 5432 | PostgreSQL — domain schema and data |
<!-- #endif -->
| `redis-cache` | 6379 | Redis — FusionCache distributed backplane |
<!-- #if implement-servicebus -->
| `servicebus-emulator` | 5672 (AMQP), 5300 (mgmt) | Azure Service Bus emulator |
<!-- #endif -->
| `aspire-dashboard` | 18888 (UI), 4317 (OTLP) | OpenTelemetry traces and logs |

Stop and remove containers:

```bash
podman compose down
```

<!-- #else -->
This solution is a facade (no local database). Start Redis for caching and the dashboard for telemetry:

```bash
podman compose up -d
```

<!-- #endif -->
<!-- #if implement-sqlserver -->
### Connection strings

Connection strings are in each host's `appsettings.Development.json` under the `Aspire:` key hierarchy. The `Database` default:

```json
"Aspire": {
  "Microsoft": {
    "Data": {
      "SqlClient": {
        "ConnectionString": "Data Source=127.0.0.1,1433;Initial Catalog=domain-name;User id=sa;Password=yourStrong(!)Password;TrustServerCertificate=true"
      }
    }
  }
}
```

<!-- #endif -->
<!-- #if implement-postgres -->
### Connection strings

Connection strings are in each host's `appsettings.Development.json` under the `Aspire:` key hierarchy. The `Database` default:

```json
"Aspire": {
  "Npgsql": {
    "ConnectionString": "Server=127.0.0.1;Database=domain-name-lower;Username=postgres;Password=yourStrong#!Password"
  }
}
```

<!-- #endif -->
---

<!-- #if !implement-none-data -->
## Database

Migrate and seed — required once on first run and after any schema change:

```bash
dotnet run --project tools/app-name.Database -- all
```

| Command | Effect |
|---|---|
| `-- all` | Create schema, run migrations, seed reference data |
| `-- drop` | Drop and recreate the database |
| `-- reset` | Reset seed data only (schema stays) |
| `-- migrate` | Apply migrations without seeding |

See `tools/app-name.Database/Migrations/` for migration scripts and `tools/app-name.Database/Data/` for seed data.

<!-- #endif -->
<!-- #if (refdata-enabled && !implement-none-data) -->
---

## Reference data code generation

After editing `tools/app-name.CodeGen/ref-data.yaml`, regenerate the C# reference data layer:

```bash
dotnet run --project tools/app-name.CodeGen
```

Commit the generated `*.g.cs` files alongside the `ref-data.yaml` changes. **Never edit generated files by hand** — they are overwritten on the next run.

<!-- #endif -->
---

## Build and test

```bash
dotnet build
dotnet test
```

<!-- #if !implement-none-data -->
Unit tests in `tests/app-name.Test.Unit` are fast and isolated — no infrastructure required. Integration tests (in host test projects) require the containers to be running and the database migrated.

<!-- #else -->
Unit tests in `tests/app-name.Test.Unit` are fast and isolated — no infrastructure required.

<!-- #endif -->
---

## Adding hosts

Use the CoreEx templates to add host projects into this solution:

```bash
dotnet new coreex-api       -n app-name.Api              -o .   # HTTP API + test project
dotnet new coreex-relay     -n app-name.Relay     -o .   # Outbox relay + test project
dotnet new coreex-subscribe -n app-name.Subscribe        -o .   # Event subscriber + test project
```

Each template adds the host project, its test project, and wires both into the solution file.

---

## Coding conventions

| Rule | Detail |
|---|---|
| **Line endings** | LF everywhere (enforced by `.gitattributes` + `.editorconfig`) |
| **Indentation** | 4 spaces (C#) · 2 spaces (JSON, YAML, XML, `*.csproj`) |
| **Nullable** | Enabled — nullable warnings are treated as errors; never suppress with `!` without justification |
| **`using` statements** | One `GlobalUsing.cs` per project — never in individual source files |
| **Namespaces** | File-scoped only: `namespace Foo.Bar;` |
| **Private fields** | Prefixed `_camelCase` |
| **Generated files** | Never edit `*.g.cs`, `*.g.sql`, `*.g.pgsql` — re-run the owning generator |
| **ConfigureAwait** | Always `.ConfigureAwait(false)` in service and repository code |

Full rules are in `.editorconfig`. AI-oriented guidance is in `AGENTS.md`.
