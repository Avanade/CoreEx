# CoreEx.RefData.HealthChecks

> Provides ASP.NET Core health-check integration for the `ReferenceDataOrchestrator`, reporting the full list of registered reference data types as health-check data.

## Overview

`CoreEx.RefData.HealthChecks` exposes `ReferenceDataOrchestratorHealthCheck`, a single `IHealthCheck` implementation that returns a healthy result and attaches the names of every type registered with the ambient `ReferenceDataOrchestrator` as diagnostic data. This lets operations teams confirm which reference data types are available in a deployed service without requiring access to the application internals.

The check always returns `Healthy` — it is a diagnostic aid rather than a fault detector. When reference data types fail to load at runtime, the failure is surfaced through the orchestrator's own exception propagation, not through this health check.

## Key capabilities

- ✅ **Always-healthy orchestrator probe**: `ReferenceDataOrchestratorHealthCheck` reports `Healthy` and attaches all registered type names, giving a quick confirmation that the orchestrator is configured and populated.
- 📋 **Type enumeration data**: the health-check data dictionary contains one entry per registered reference data type; the keys and values are both the fully qualified type name, making the result easy to inspect in health-check dashboards.

## Key types

| Type | Description |
|------|-------------|
| **[`ReferenceDataOrchestratorHealthCheck`](./ReferenceDataOrchestratorHealthCheck.cs)** | `IHealthCheck` implementation; calls `ReferenceDataOrchestrator.GetAllTypes()` and emits the result as health-check data with a `Healthy` status. |

## Related namespaces

- **[`CoreEx.RefData`](../README.md)** - Root namespace containing the orchestrator and cache that this health check monitors.