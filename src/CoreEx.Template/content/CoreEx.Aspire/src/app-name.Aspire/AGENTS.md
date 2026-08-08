# domain-name Aspire AppHost -- AI Agent Guide

This is the **.NET Aspire AppHost** for the `domain-name` domain, part of the `solution-name` microservice. It
orchestrates this solution's own runtime hosts for local development and exposes the Aspire dashboard.

> **Before answering any CoreEx question:** check whether `.github/docs/coreex/` is populated at the solution root.
> If empty, run `/coreex-docs-sync` first. `.github/docs/coreex/local-dev.md` and `.github/docs/coreex/aspire.md`
> are especially relevant to this project.

---

## What This Project Does

`AppHost.cs` calls `builder.AddProject<Projects.X>(...)` once per runtime host this solution has, then runs the
distributed application. It does **not** contain business logic, DI registrations for the hosts themselves, or
connection strings -- those live in each host's own `Program.cs`/`appsettings*.json`. This project only wires
hosts together and adds dashboard sugar (see `Extensions.cs`).

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

## Adding a Host Later

If a new `Api`, `Relay`, or `Subscribe` host is added to this solution *after* this AppHost was generated, do not
re-run `dotnet new coreex-aspire --force` -- it will overwrite any customisation already made here. Instead, add
the missing pieces by hand:

1. A `<ProjectReference>` to the new host's `.csproj` in `app-name.Aspire.csproj`.
2. A `builder.AddProject<Projects.X>("...")` call in `AppHost.cs`, following the pattern already used for the
   other hosts.

## `Extensions.cs`

Provides fluent dashboard sugar used from `AppHost.cs`:

- `AddEndpoints(...)` -- annotates dashboard-visible URLs (e.g. health-check deep links) on a resource.
- `AddCommand(...)` -- adds a dashboard button that invokes an HTTP verb against an endpoint.
- `AddHostedServiceSupport()` -- composes the above to add "Pause all services"/"Resume all services" dashboard
  buttons for hosts that run `CoreEx` hosted services (Relay and Subscribe hosts expose
  `/hosted-services/all/{status,pause,resume}` via `MapHostedServices()`; the Api host does not, so it is never
  called there).

## Running

```sh
dotnet run --project src/solution-name.Aspire
```

Consult `.github/docs/coreex/local-dev.md` and `.github/docs/coreex/aspire.md` for the full local-development
workflow, including the Aspire CLI (`aspire run`, `aspire logs`, etc.).
