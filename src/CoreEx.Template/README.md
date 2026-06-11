# CoreEx.Template

> Provides the `dotnet new` template pack for scaffolding CoreEx-based domain microservice solutions -- five composable templates, one `dotnet new install`.

## Overview

`CoreEx.Template` is a `PackageType=Template` NuGet package that installs five `dotnet new` templates as a single unit. Together they cover both the AI-ready bootstrap entry point and the full project topology for a CoreEx domain-based microservice: the shared solution core (contracts, application, infrastructure, database tooling) and the independently deployable host processes (API, Outbox Relay, Subscriber).

| Short name | Template | Emits |
|---|---|---|
| `coreex-bootstrap` | CoreEx AI-ready bootstrap repository | Minimal repository shell + packaged AI workflow assets for `/coreex-scaffold` |
| `coreex` | CoreEx domain-based microservice application | Solution scaffold: `src/` libraries + `tools/` projects + `tests/` (Test.Common + Test.Unit) |
| `coreex-api` | CoreEx API host | `src/[name].Api/` host project + `tests/[solution].Test.Api/` integration test project |
| `coreex-relay` | CoreEx Outbox Relay host | `src/[name].Relay/` host project + `tests/[solution].Test.Outbox.Relay/` integration test project |
| `coreex-subscriber` | CoreEx Subscriber host | `src/[name].Subscriber/` host project + `tests/[solution].Test.Subscribe/` integration test project |

Parameters are consistent across templates -- the same `--data-provider`, `--messaging-provider`, and feature flags appear in every template that needs them, ensuring the generated code is coherent regardless of which templates you use.

## Installation

```sh
dotnet new install CoreEx.Template
```

To verify:

```sh
dotnet new list --tag CoreEx
```

To update after a new release:

```sh
dotnet new update
```

To uninstall:

```sh
dotnet new uninstall CoreEx.Template
```

## AI-Guided Scaffolding

Every generated solution includes a small bootstrap AI workflow set under `.github/`, and the dedicated `coreex-bootstrap` template emits only that shell:

- `/coreex-scaffold` -- interviews for project needs, recommends the smallest safe `dotnet new coreex*` command set, and can run the commands.
- `.github/skills/solution-scaffolder/` -- the packaged workflow guidance for greenfield scaffolding.
- `.github/docs/coreex/application-scaffolding-guide.md` -- the decision-oriented guide behind the workflow.

The packaged workflow assets are sourced from the repository's canonical `.github/` set, while the generated solution still receives the consumer-specific `copilot-instructions.md` variant from `consumer-instructions/`.

Typical bootstrap flow:

```sh
dotnet new coreex-bootstrap -n Avanade.Erp.Sales
```

Then run `/coreex-scaffold` and answer the business-shape questions. The workflow derives the required `coreex`, `coreex-api`, `coreex-relay`, and `coreex-subscriber` commands, using `--force` only when replacing the bootstrap placeholders.

---

## Naming Convention

The templates use dot-delimited names that encode the solution hierarchy. Understanding this is essential -- the name you supply drives all file names, namespace declarations, project cross-references, and configuration values automatically.

**Recommended format:** `[Company].[Product].[Domain]`

| Segment | Example | Role |
|---|---|---|
| Company | `Avanade` | Organisation |
| Product | `Erp` | Product or system |
| Domain | `Sales` | The bounded context being scaffolded |

For the **solution template** (`coreex`) the name is the three-part solution root, e.g. `Avanade.Erp.Sales`. For each **host template** (`coreex-api`, `coreex-relay`, `coreex-subscriber`) the name appends the host suffix, e.g. `Avanade.Erp.Sales.Api`.

### Derived values (example: `Avanade.Erp.Sales.Api`)

| Derived symbol | Value | Used for |
|---|---|---|
| `domain-name` | `Sales` | Class names, namespaces, DB identifiers (mixed case) |
| `domain-name-lower` | `sales` | Service Bus topic/subscription names, Postgres database name |
| `solution-name` | `Avanade.Erp.Sales` | Cross-project references in host `.csproj` files |
| `solution-parent-name` | `Avanade.Erp` | `CoreEx:Host:SolutionName` in `appsettings.json` |

---

## Template 1 -- `coreex-bootstrap` (AI-ready bootstrap)

Creates an intentionally minimal repository shell for teams that want to start with the agent workflow before committing to a CoreEx project shape. It emits root guidance and the packaged `.github/` and `.claude/` AI assets needed for `/coreex-scaffold`.

Run this when you want the repository to stay effectively empty until the scaffold interview derives the right host and capability mix.

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `-n` / `--name` | string | _(required)_ | Solution base name, e.g. `Avanade.Erp.Sales`. Drives the root guidance text. |

### Output

```
AGENTS.md
README.md
.github/
.claude/
```

### Example

```sh
dotnet new coreex-bootstrap -n Avanade.Erp.Sales
```

---

## Template 2 -- `coreex` (Solution scaffold)

Scaffolds the shared solution core: the `.slnx` solution file, solution-wide configuration files (`.editorconfig`, `Directory.Build.props`, `Directory.Packages.props`), the `Contracts`, `Application`, `Infrastructure` (and optionally `Domain`) class-library projects, and the `Database` and `CodeGen` tooling projects.

Run this **once per domain** from the solution root directory.

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `-n` / `--name` | string | _(required)_ | Solution base name, e.g. `Avanade.Erp.Sales`. Drives all file names and namespaces. Format: `[Company].[Product].[Domain]`. |
| `--refdata-enabled` | bool | `true` | Includes the reference-data pattern: `IReferenceDataRepository` (Application), `ReferenceDataService` (Application), `ReferenceDataRepository` (Infrastructure), and the `CodeGen` tool project. |
| `--rop-enabled` | bool | `false` | Enables Railway-Oriented Programming -- `Result`/`Result<T>` return types throughout the solution. |
| `--domain-driven-enabled` | bool | `false` | Adds the `[Domain].Domain` project for DDD aggregates, domain events, and value objects. |
| `--data-provider` | `SqlServer` \| `Postgres` \| `None` | `SqlServer` | The data persistence technology. `None` is for facade scenarios (e.g. over Dynamics 365) where there is no local database -- the `Database` tool project, EF Core packages, DbContext, and EfDb are all omitted. |
| `--outbox-enabled` | bool | `true` | Includes transactional-outbox wiring in the Infrastructure project. Has no effect when `--data-provider None`. |
| `--messaging-provider` | `ServiceBus` \| `None` | `ServiceBus` | The messaging technology. `None` omits all messaging configuration. |

### Output

```
[name].slnx
.editorconfig
.gitignore
.filenesting.json
Directory.Build.props
Directory.Packages.props
src/
  [name].Contracts/
    [name].Contracts.csproj
    GlobalUsing.cs
  [name].Application/
    [name].Application.csproj
    GlobalUsing.cs
    ReferenceDataService.cs              (refdata-enabled only)
    Repositories/
      IReferenceDataRepository.cs        (refdata-enabled only)
  [name].Domain/                         (domain-driven-enabled only)
    [name].Domain.csproj
    GlobalUsing.cs
  [name].Infrastructure/
    [name].Infrastructure.csproj
    GlobalUsing.cs
    Repositories/
      [Domain]DbContext.cs               (data-provider != None)
      [Domain]EfDb.cs                    (data-provider != None)
      ReferenceDataRepository.cs         (refdata-enabled && data-provider != None)
tools/
  [name].Database/                       (data-provider != None)
    [name].Database.csproj
    Program.cs
  [name].CodeGen/                        (refdata-enabled && data-provider != None)
    [name].CodeGen.csproj
    Program.cs
    ref-data.yaml
tests/
  [name].Test.Common/
    [name].Test.Common.csproj
    TestData.cs
    Data/                                (data-provider != None -- embedded seed data for DbEx)
  [name].Test.Unit/
    [name].Test.Unit.csproj
```

### Examples

Default (SQL Server, refdata, Service Bus outbox):
```sh
dotnet new coreex -n Avanade.Erp.Sales
```

PostgreSQL with no refdata:
```sh
dotnet new coreex -n Avanade.Erp.Sales --data-provider Postgres --refdata-enabled false
```

Facade over an external system (no local database or messaging):
```sh
dotnet new coreex -n Avanade.Erp.Sales --data-provider None --messaging-provider None
```

Domain-driven design with ROP, Postgres, no outbox:
```sh
dotnet new coreex -n Avanade.Erp.Sales --domain-driven-enabled true --rop-enabled true --data-provider Postgres --outbox-enabled false
```

---

## Template 3 -- `coreex-api` (API host)

Scaffolds an ASP.NET Core Web API host project. Wires up CoreEx execution context, optional reference-data orchestration, FusionCache L1/L2 caching with Redis backplane, the selected database provider and EF Core, outbox publishing, NSwag OpenAPI, and OpenTelemetry.

Run this from the **solution root** (the directory created by `coreex`). The template emits into both `src/` and `tests/` so it must be run at the root level.

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `-n` / `--name` | string | _(required)_ | Full project name, e.g. `Avanade.Erp.Sales.Api`. Must match the solution name with `.Api` appended. |
| `--refdata-enabled` | bool | `true` | Wires up `ReferenceDataOrchestrator` and dynamic service registration for reference-data caching. Match the value used with `coreex`. |
| `--data-provider` | `SqlServer` \| `Postgres` \| `None` | `SqlServer` | Database technology. Selects the Aspire connection, CoreEx database, unit-of-work, and outbox publisher registration. |
| `--outbox-enabled` | bool | `true` | Registers the outbox publisher (`SqlServerOutboxPublisher` or `PostgresOutboxPublisher`) as the `IEventPublisher`. Has no effect when `--data-provider None`. |

### Output

```
src/
  [name].Api/
    [name].Api.csproj
    Program.cs
    GlobalUsing.cs
    appsettings.json
    appsettings.Development.json
    AGENTS.md
tests/
  [solution-name].Test.Api/
    [solution-name].Test.Api.csproj
```

**`appsettings.json`** (illustrative -- values are replaced at generation time):

```jsonc
{
  "CoreEx": {
    "Host": {
      "SolutionName": "Avanade.Erp",    // Company.Product (solution-parent-name)
      "DomainName": "Sales"              // Domain (domain-name)
    },
    "Events": {
      "Destination": "sales"             // Topic/queue name (domain-name-lower)
    }
  },
  "Logging": { ... }
}
```

**`appsettings.Development.json`** (connection strings vary by `--data-provider`):

```jsonc
{
  "Aspire": {
    // SQL Server (implement-sqlserver):
    "Microsoft": { "Data": { "SqlClient": { "ConnectionString": "Data Source=127.0.0.1,1433;Initial Catalog=Sales;..." } } },
    // OR PostgreSQL (implement-postgres):
    "Npgsql": { "ConnectionString": "Server=127.0.0.1;Database=sales;..." },
    "StackExchange": { "Redis": { "ConnectionString": "localhost:6379" } }
  }
}
```

### Examples

Default (SQL Server, refdata, outbox):
```sh
dotnet new coreex-api -n Avanade.Erp.Sales.Api
```

PostgreSQL, no refdata:
```sh
dotnet new coreex-api -n Avanade.Erp.Sales.Api --data-provider Postgres --refdata-enabled false
```

Facade (no database):
```sh
dotnet new coreex-api -n Avanade.Erp.Sales.Api --data-provider None
```

---

## Template 4 -- `coreex-relay` (Outbox Relay host)

Scaffolds an ASP.NET Core background-service host that reads committed events from the transactional outbox table and forwards them to the messaging provider. Does not reference the solution's shared projects -- it is a standalone host that requires only a database connection and a messaging connection.

Run this from the **solution root** (the directory created by `coreex`). The template emits into both `src/` and `tests/` so it must be run at the root level.

> **Note:** When `--data-provider None` is selected the outbox relay has nothing to relay. Scaffold this host only when your solution uses the outbox pattern (i.e. `--data-provider` is `SqlServer` or `Postgres` and `--outbox-enabled true`).

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `-n` / `--name` | string | _(required)_ | Full project name, e.g. `Avanade.Erp.Sales.Relay`. |
| `--data-provider` | `SqlServer` \| `Postgres` \| `None` | `SqlServer` | Database technology used for reading the outbox. |
| `--messaging-provider` | `ServiceBus` \| `None` | `ServiceBus` | Messaging provider to publish forwarded events to. |

### Output

```
src/
  [name].Relay/
    [name].Relay.csproj
    Program.cs
    appsettings.json
    appsettings.Development.json
    AGENTS.md
tests/
  [solution-name].Test.Outbox.Relay/
    [solution-name].Test.Outbox.Relay.csproj
```

**`appsettings.json`** (illustrative):

```jsonc
{
  "CoreEx": {
    "Host": {
      "SolutionName": "Avanade.Erp",
      "DomainName": "Sales",
      "Services": {
        "Interval": "00:00:00.500",
        "OutboxRelay": {
          "BatchSize": 10,
          "PerWorkerPartitionCount": 2,
          "LeaseDuration": "00:00:05",
          "BackoffDuration": "00:00:05",
          "ServicesCount": 4
        }
      }
    }
  }
}
```

**`appsettings.Development.json`** (connection strings vary by provider):

```jsonc
{
  "Aspire": {
    // SQL Server or PostgreSQL connection (same pattern as coreex-api)
    "Azure": {
      // implement-servicebus only:
      "Messaging": { "ServiceBus": { "ConnectionString": "Endpoint=sb://localhost;..." } }
    }
  }
}
```

### Examples

SQL Server outbox -> Azure Service Bus:
```sh
dotnet new coreex-relay -n Avanade.Erp.Sales.Relay
```

PostgreSQL outbox -> Azure Service Bus:
```sh
dotnet new coreex-relay -n Avanade.Erp.Sales.Relay --data-provider Postgres
```

---

## Template 4 -- `coreex-subscriber` (Subscriber host)

Scaffolds an ASP.NET Core background-service host that receives events from a messaging provider, dispatches them through the CoreEx `SubscribedManager`, and processes them via subscriber implementations. Optionally includes reference-data caching and a database connection for subscriber logic that needs persistence.

Run this from the **solution root** (the directory created by `coreex`). The template emits into both `src/` and `tests/` so it must be run at the root level.

### Parameters

| Parameter | Type | Default | Description |
|---|---|---|---|
| `-n` / `--name` | string | _(required)_ | Full project name, e.g. `Avanade.Erp.Sales.Subscriber`. |
| `--refdata-enabled` | bool | `true` | Wires up `ReferenceDataOrchestrator` and dynamic service registration for reference-data caching. Match the value used with `coreex`. |
| `--data-provider` | `SqlServer` \| `Postgres` \| `None` | `SqlServer` | Database technology. When not `None`, EF Core and outbox publisher are wired so subscriber logic can persist state and emit its own events. |
| `--messaging-provider` | `ServiceBus` \| `None` | `ServiceBus` | Messaging provider to receive events from. When `None`, no receiver is configured. |

### Output

```
src/
  [name].Subscriber/
    [name].Subscriber.csproj
    Program.cs
    GlobalUsing.cs
    appsettings.json
    appsettings.Development.json
    AGENTS.md
tests/
  [solution-name].Test.Subscribe/
    [solution-name].Test.Subscribe.csproj
```

**`appsettings.json`** (illustrative):

```jsonc
{
  "CoreEx": {
    "Host": {
      "SolutionName": "Avanade.Erp",
      "DomainName": "Sales",
      "Services": { "Interval": "00:00:00.500" }
    },
    "Events": {
      "Destination": "sales"             // Outbound event topic/queue (domain-name-lower)
    }
  }
}
```

**`appsettings.Development.json`** (connection strings vary by provider):

```jsonc
{
  "Aspire": {
    // SQL Server or PostgreSQL connection (same pattern as coreex-api)
    "Azure": {
      // implement-servicebus only:
      "Messaging": {
        "ServiceBus": {
          "ConnectionString": "Endpoint=sb://localhost;...",
          "QueueOrTopicName": "avanade.erp",   // solution-name-lower
          "SubscriptionName": "sales"           // domain-name-lower
        }
      }
    },
    "StackExchange": { "Redis": { "ConnectionString": "localhost:6379" } }
  }
}
```

### Examples

Default (SQL Server, refdata, Service Bus):
```sh
dotnet new coreex-subscriber -n Avanade.Erp.Sales.Subscriber
```

PostgreSQL, no refdata:
```sh
dotnet new coreex-subscriber -n Avanade.Erp.Sales.Subscriber --data-provider Postgres --refdata-enabled false
```

Messaging only (no local database):
```sh
dotnet new coreex-subscriber -n Avanade.Erp.Sales.Subscriber --data-provider None
```

---

## Typical Workflow

All four templates are independent. Use only the ones your solution needs. The following shows a full event-driven microservice topology:

### Step 1 -- Create the solution root

```sh
mkdir Avanade.Erp.Sales
cd Avanade.Erp.Sales
dotnet new coreex -n Avanade.Erp.Sales
```

Produces the `.slnx`, solution configuration files, `src/` class libraries, `tools/` projects, and `tests/` (Test.Common + Test.Unit). The current directory becomes the solution root.

### Step 2 -- Scaffold host projects

Run from the **solution root**. Each template emits into both `src/` and `tests/`:

```sh
dotnet new coreex-api        -n Avanade.Erp.Sales.Api
dotnet new coreex-relay      -n Avanade.Erp.Sales.Relay
dotnet new coreex-subscriber -n Avanade.Erp.Sales.Subscriber
```

### Step 3 -- Add hosts and their test projects to the solution

```sh
dotnet sln Avanade.Erp.Sales.slnx add src/Avanade.Erp.Sales.Api
dotnet sln Avanade.Erp.Sales.slnx add tests/Avanade.Erp.Sales.Test.Api

dotnet sln Avanade.Erp.Sales.slnx add src/Avanade.Erp.Sales.Relay
dotnet sln Avanade.Erp.Sales.slnx add tests/Avanade.Erp.Sales.Test.Outbox.Relay

dotnet sln Avanade.Erp.Sales.slnx add src/Avanade.Erp.Sales.Subscriber
dotnet sln Avanade.Erp.Sales.slnx add tests/Avanade.Erp.Sales.Test.Subscribe
```

### Resulting directory structure

```
Avanade.Erp.Sales/
  Avanade.Erp.Sales.slnx
  .editorconfig  |  .gitignore  |  Directory.Build.props  |  Directory.Packages.props
  src/
    Avanade.Erp.Sales.Contracts/
    Avanade.Erp.Sales.Application/
    Avanade.Erp.Sales.Infrastructure/
    Avanade.Erp.Sales.Api/
    Avanade.Erp.Sales.Relay/
    Avanade.Erp.Sales.Subscriber/
  tools/
    Avanade.Erp.Sales.Database/
    Avanade.Erp.Sales.CodeGen/
  tests/
    Avanade.Erp.Sales.Test.Common/
    Avanade.Erp.Sales.Test.Unit/
    Avanade.Erp.Sales.Test.Api/
    Avanade.Erp.Sales.Test.Outbox.Relay/
    Avanade.Erp.Sales.Test.Subscribe/
```

---

## Scenario Examples

### Full event-driven (SQL Server, Service Bus, refdata)

All defaults -- no flags required:

```sh
dotnet new coreex            -n Avanade.Erp.Sales
dotnet new coreex-api        -n Avanade.Erp.Sales.Api
dotnet new coreex-relay      -n Avanade.Erp.Sales.Relay
dotnet new coreex-subscriber -n Avanade.Erp.Sales.Subscriber
```

### PostgreSQL variant

```sh
dotnet new coreex            -n Avanade.Erp.Sales --data-provider Postgres
dotnet new coreex-api        -n Avanade.Erp.Sales.Api        --data-provider Postgres
dotnet new coreex-relay      -n Avanade.Erp.Sales.Relay      --data-provider Postgres
dotnet new coreex-subscriber -n Avanade.Erp.Sales.Subscriber --data-provider Postgres
```

### Facade over an external system (e.g. Dynamics 365)

No local database, no outbox, no messaging infrastructure -- the API is a pure HTTP facade:

```sh
dotnet new coreex     -n Avanade.Erp.Sales --data-provider None --messaging-provider None
dotnet new coreex-api -n Avanade.Erp.Sales.Api --data-provider None
```

No relay or subscriber needed -- omit those templates entirely.

### API + Subscriber only (no outbox relay)

When using an at-least-once messaging pattern without transactional outbox:

```sh
dotnet new coreex            -n Avanade.Erp.Sales --outbox-enabled false
dotnet new coreex-api        -n Avanade.Erp.Sales.Api        --outbox-enabled false
dotnet new coreex-subscriber -n Avanade.Erp.Sales.Subscriber
```

---

## Package Design Notes

**Single install, four templates.** All four templates ship inside one NuGet package (`PackageType=Template`). A single `dotnet new install CoreEx.Template` makes all four short names available: `coreex`, `coreex-api`, `coreex-relay`, and `coreex-subscriber`.

**Version stamping.** Each `template.json` carries `COREEX_VERSION` as a placeholder. During `dotnet pack`, an inline MSBuild `ReplaceTextInFile` task stamps the actual `$(Version)` into generated copies of the four `template.json` files before they are packed. The source copies in `content/` retain the placeholder and remain editable.

**Central Package Management.** All `PackageReference` entries in generated projects carry no `Version` attribute -- versions are resolved from the `Directory.Packages.props` emitted by the `coreex` solution template. The host templates therefore require that the `coreex` solution template has been applied first (since `Directory.Packages.props` lives at the solution root).

**Conditional file exclusion.** The template engine's `sources.modifiers` with glob patterns handle all conditional file output -- no empty placeholder files are emitted. The `.slnx` solution file uses `specialCustomOperations` to enable `<!--#if-->` XML-style conditionals (the template engine does not recognise `.slnx` as XML by default).

**`preferNameDirectory`.** All four templates set `preferNameDirectory: false` so they generate into the current directory rather than creating an extra named subdirectory. The solution template generates its content directly into the current directory. Host templates generate their content into `src/[ProjectName]/` and `tests/[solution-name].Test.X/` subdirectories, which are created by the template itself as part of the source layout -- not by the `preferNameDirectory` mechanism.

**`solution-name` file renaming.** The `solution-name` derived symbol (everything before the last dot-segment of the `-n` value) carries `fileRename: "solution-name"` in all host templates. This causes directory names like `solution-name.Test.Api` to be substituted at generation time, producing correctly-named test project folders (e.g. `Avanade.Erp.Sales.Test.Api`) that match the CoreEx sample naming convention.

## Additional Resources

- [CoreEx](https://github.com/Avanade/CoreEx) -- The framework these templates scaffold for.
- [CoreEx.CodeGen](../CoreEx.CodeGen/README.md) -- The reference-data code-generation tool project scaffolded by the `coreex` template when `--refdata-enabled true`.
- [dotnet templating wiki](https://github.com/dotnet/templating/wiki) -- Full reference for `template.json` configuration.
