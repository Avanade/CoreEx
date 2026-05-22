# CoreEx.HealthChecks

> Provides `HealthCheckTags` constants and extension methods for standardizing health check registration with CoreEx-defined tag conventions across all CoreEx host types.

## Overview

`CoreEx.HealthChecks` is a small, focused namespace that bridges CoreEx with the .NET `Microsoft.Extensions.Diagnostics.HealthChecks` infrastructure. It defines the standard tag names used when registering health checks (`live`, ``, `ready`) and provides extension methods that apply those tags consistently so that liveness and readiness probes can be separated at the `IHealthChecksBuilder` level without each host re-defining the tag strings.

## Key capabilities

- 🏷️ **Standard tags**: `HealthCheckTags.Live` (`"live"`), `HealthCheckTags.Startup` (`"startup"`) and `HealthCheckTags.Ready` (`"ready"`) are the canonical tag names used by CoreEx hosts to distinguish liveness from readiness checks.
- 🔧 **Registration extensions**: `Extensions.AddCoreExHealthChecks(IHealthChecksBuilder)` registers the standard CoreEx health checks (e.g. `HostedServiceManager`) with the correct tags and names.
- 🎯 **Filtering integration**: Tags align with the standard ASP.NET Core health-check filtering convention, allowing `/health/live`, `/health/startup` and `/health/ready` endpoints to filter by tag without custom predicates.

## Key types

| Type | Description |
|------|-------------|
| **[`HealthCheckTags`](./HealthCheckTags.cs)** | Static constants: `Live` (`"live"`), `Startup` (`"startup"`), `Ready` (`"ready"`), and `All` array for use in `IHealthChecksBuilder.AddCheck(..., tags: HealthCheckTags.All)`. |
| **[`Extensions`](./Extensions.cs)** | `IHealthChecksBuilder` extension methods applying CoreEx health check registrations with standard tags. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - Root package; `Program.cs` hosts call `PostConfigureAllHealthChecks()` which uses these tags.
- **[`CoreEx.Hosting`](../Hosting/README.md)** - `HostedServiceHealthCheck` registers itself under the `HealthCheckTags.Ready` tag.