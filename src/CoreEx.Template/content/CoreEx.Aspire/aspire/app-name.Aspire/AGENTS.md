# domain-name Aspire AppHost -- AI Agent Guide

This is the **.NET Aspire AppHost** for the `domain-name` domain, part of the `solution-name` microservice. It
orchestrates this solution's own runtime hosts for local development and exposes the Aspire dashboard.

> **Before answering any CoreEx question:** check whether `.github/docs/coreex/` is populated at the solution root.
> If empty, run `/coreex-docs-sync` first. `.github/docs/coreex/local-dev.md` and `.github/docs/coreex/aspire.md`
> are especially relevant to this project.

---

## What This Project Does

`AppHost.cs` first declares shared connection-string resources (SQL Server/Postgres/Cosmos DB, Redis, Service Bus -- whichever
apply per `data-provider`/`messaging-provider`), matching the connection name each host passes to its own Aspire
client-integration package (e.g. `AddAzureNpgsqlDataSource("Postgres")`). It then calls
`builder.AddProject<Projects.X>(...)` once per runtime host this solution has, chaining `.WithReference(...)` for
only the resources that host actually needs (Api: data provider + Redis; Relay: data provider + Service Bus;
Subscribe: data provider + Redis + Service Bus), then runs the distributed application. It does **not** contain
business logic or DI registrations for the hosts themselves -- those live in each host's own
`Program.cs`/`appsettings*.json`. This project only wires hosts and their dependencies together and adds dashboard
sugar (`AddEndpoints`/`AddCommand`/`AddHostedServiceSupport`/`DisableHttpCertificateValidation`, provided by the
`CoreEx.UnitTesting` package referenced by this project -- see `<Using Include="UnitTestEx" />` in
`app-name.Aspire.csproj`).

This solution was generated with:

<!-- #if has-api -->
- **Api host** included.
<!-- #endif -->
<!-- #if has-relay -->
- **Relay host** included.
<!-- #endif -->
<!-- #if has-subscribe -->
- **Subscribe host** included.
<!-- #endif -->

## Sibling Projects

- `app-name.Aspire.MockHost` -- a WireMock.Net-based console host for stubbing out external dependencies during
  local runs/tests via `AddMockHostProject<Projects.solution-name-underscore_Aspire_MockHost>(...)` (`UnitTestEx.Aspire`).
  Currently referenced but not wired into `AppHost.cs` -- uncomment the `mockhost` line once it has actual
  request/response mappings configured.
- `solution-name.Test.Aspire` -- an NUnit project that spins up this AppHost via `WithAspireTester<Projects.solution-name-underscore_Aspire>`
  for smoke/integration testing across all orchestrated hosts together.

## Guidance

For AppHost wiring, service-bus topology, third-party HTTP (MockHost) versus in-AppHost domains (`WithReference`), and the `Test.Aspire` `OnBeforeStartAsync` / `OnAfterStartAsync` lifecycle, see the `coreex-aspire` skill (`.github/skills/coreex-aspire/SKILL.md`) and `.github/instructions/coreex-aspire.instructions.md` when installed via `dotnet new coreex-ai`.

## Adding a Host Later

If a new `Api`, `Relay`, or `Subscribe` host is added to this solution *after* this AppHost was generated, do not
re-run `dotnet new coreex-aspire --force` -- it will overwrite any customisation already made here. Instead, add
the missing pieces by hand:

1. A `<ProjectReference>` to the new host's `.csproj` in `app-name.Aspire.csproj`.
2. A `builder.AddProject<Projects.X>("...")` call in `AppHost.cs`, chaining `.WithReference(...)` for whichever
   resources it needs, following the pattern already used for the other hosts.

## Running

```sh
dotnet run --project aspire/solution-name.Aspire
```

Consult `.github/docs/coreex/local-dev.md` and `.github/docs/coreex/aspire.md` for the full local-development
workflow, including the Aspire CLI (`aspire run`, `aspire logs`, etc.).
