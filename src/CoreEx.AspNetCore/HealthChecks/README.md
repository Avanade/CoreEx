# CoreEx.AspNetCore.HealthChecks

> Provides `HealthCheckOptions` and endpoint-registration extensions for wiring CoreEx-standard live, startup, ready, and detailed health check endpoints into an ASP.NET Core application.

## Overview

`CoreEx.AspNetCore.HealthChecks` bridges the `CoreEx.HealthChecks` tag conventions and the `CoreEx.Hosting` `HostedServiceHealthCheck` registrations into fully configured ASP.NET Core endpoint middleware. `HealthCheckOptions` controls which of the standard endpoint paths are enabled, what path each uses, and whether detailed JSON output is served.

The detail endpoint writes a JSON `HealthReport` body (status, duration, each entry's status/duration/exception/data), making it easy for operations dashboards to consume without custom serialization. Each endpoint uses tag-based filtering (`live`, `ready`) aligned with `HealthCheckTags` from `CoreEx.HealthChecks`.

## Key capabilities

- ♥ **Standard endpoints**: Live (`/health/live`), startup (`/health/startup`), ready (`/health/ready`), and related detail (e.g. `/health/ready/detail`) are configurable via `HealthCheckOptions` properties.
- 📋 **JSON detail report**: The detail endpoint writes a structured JSON `HealthReport` with per-entry status, description, duration, exception message, and custom data dictionary.
- ⚙️ **Per-endpoint enable/disable**: Each endpoint can be disabled independently (`IsLiveEndpointEnabled`, `IsReadyEndpointEnabled`, `IsStartupEndpointEnabled`, `AreDetailedEndpointsEnabled`) for environments where not all probes are needed.
- 🔌 **Custom detail writer**: `OnWriteDetailedHealthCheckAsync(HttpContext, HealthReport)` is a virtual method on `HealthCheckOptions` for full control over the detail response format.
- 🔒 **Securable detail endpoints**: `MapHealthChecks(options, detailedGroupConfigure)` accepts an optional `Action<IEndpointConventionBuilder>` applied only to the `/detailed` endpoints (e.g. `g => g.RequireAuthorization()`), so live/startup/ready probes stay anonymous while the detailed diagnostic payload is protected.

## Key types

| Type | Description |
|------|-------------|
| **[`HealthCheckOptions`](./HealthCheckOptions.cs)** | Configuration for live/startup/ready/detail endpoint paths, enable flags, and the virtual `OnWriteDetailedHealthCheckAsync` detail writer. |

## Related Namespaces

- **[`CoreEx.HealthChecks`](../../CoreEx/HealthChecks/README.md)** - Defines `HealthCheckTags.Live` and `HealthCheckTags.Ready` constants consumed by the endpoint tag filters.
- **[`CoreEx.Hosting`](../../CoreEx/Hosting/README.md)** - `HostedServiceHealthCheck` and `HostedServiceManager` register health checks that appear in the ready endpoint aggregation.
- **[`CoreEx.AspNetCore`](../README.md)** - `MapCoreExHealthChecks(HealthCheckOptions)` extension method in the root package wires all four endpoints into the application's endpoint routing.