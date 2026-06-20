# CoreEx.Hosting

> Provides base classes for timer-driven and synchronized hosted services, `HostSettings` for standardized host configuration, and `WorkOrchestrator` for tracking long-running distributed work state with pluggable storage.

## Overview

`CoreEx.Hosting` addresses two distinct but related concerns: running background work reliably within an .NET hosted-service model, and tracking the state of long-running distributed operations that outlive a single request or host invocation.

The hosted service base classes (`TimerHostedServiceBase`, `SynchronizedTimerHostedServiceBase`) provide a consistent foundation for background polling workers — handling pause/resume, error-interval back-off, no-op mode for tests, DI-scoped execution, and health check integration via `HostedServiceHealthCheck`. `HostSettings` standardizes how a host, including ASP.NET Core, exposes its solution name, domain name, environment, and source URI to the rest of the application.

`WorkOrchestrator` tracks the lifecycle of explicitly managed long-running work items (think: async job processing, background import, orchestration hand-off). Each work item has a `WorkState` record persisted via a pluggable `IWorkProvider`, with a `WorkStatus` lifecycle (Created → Started → Indeterminate/Completed/Failed/Abandoned) and automatic expiry.

## Key capabilities

- ⏱️ **Timer hosted service**: `TimerHostedServiceBase` runs `OnExecuteAsync` on a configurable interval with randomized first-start staggering, error back-off interval, and pause-on-exception support.
- 🔒 **Synchronized timer hosted service**: `SynchronizedTimerHostedServiceBase` wraps `TimerHostedServiceBase` with an `ISynchronizer` lock, ensuring only one instance across a multi-host deployment executes at a time.
- ♥ **Health check integration**: `HostedServiceHealthCheck` reports `ServiceStatus` as a named health check entry; `HostedServiceManager` aggregates multiple service statuses for a single health endpoint.
- ⚙️ **Host settings**: `HostSettings` standardizes solution name, domain name, environment name, and source URI — read from `IConfiguration` or provided explicitly — for use by caching, event naming, and logging.
- 📋 **Work orchestration**: `WorkOrchestrator` manages the create → start → complete/fail lifecycle for long-running work items, with automatic expiry and pluggable `IWorkProvider` storage.
- 🗄️ **Cache-backed work provider**: `HybridCacheWorkProvider` implements `IWorkProvider` using `IHybridCache`, enabling work state persistence without requiring a dedicated database.
- 🔗 **Distributed synchronization**: `ISynchronizer` / `HybridCacheSynchronizer` provide a distributed advisory lock used by `SynchronizedTimerHostedServiceBase` to prevent concurrent execution across host replicas.

## Key types

| Type | Description |
|------|-------------|
| _[`HostedServiceBase`](./HostedServiceBase.cs)_ | Abstract base `IHostedService` providing `ServiceStatus` tracking, health check registration, no-op mode (via `--no-op-hosted-services` arg), and scoped DI execution. |
| _[`TimerHostedServiceBase`](./TimerHostedServiceBase.cs)_ | Abstract timer-driven hosted service with configurable `Interval`, `FirstInterval`, error back-off, and pause/resume support. |
| _[`SynchronizedTimerHostedServiceBase`](./SynchronizedTimerHostedServiceBase.cs)_ | Abstract timer hosted service that acquires an `ISynchronizer` lock before each execution to prevent concurrent runs across replicas. |
| **[`HostedServiceHealthCheck`](./HostedServiceHealthCheck.cs)** | `IHealthCheck` implementation that reports the `ServiceStatus` of a registered `HostedServiceBase`. |
| **[`HostedServiceManager`](./HostedServiceManager.cs)** | Manages registration of multiple hosted services and provides aggregated status and health-check access. |
| **[`HostSettings`](./HostSettings.cs)** | Standardized host configuration: `SolutionName`, `DomainName`, `EnvironmentName`, `Source` URI. Created via `HostSettings.Create(IConfiguration, ...)`. |
| **[`WorkOrchestrator`](./Work/WorkOrchestrator.cs)** | Tracks the lifecycle of long-running work items: create, start, mark indeterminate/complete/failed/abandoned, with automatic expiry. |
| **[`WorkState`](./Work/WorkState.cs)** | Persisted record for a single work item: `Id`, `TypeName`, `Key`, `WorkStatus`, `Result`, `Reason`, trace parent/state, and timestamps. |
| **[`HybridCacheWorkProvider`](./Work/HybridCacheWorkProvider.cs)** | `IWorkProvider` implementation backed by `IHybridCache`, storing `WorkState` entries with configurable expiry. |
| **[`HybridCacheSynchronizer`](./Synchronization/HybridCacheSynchronizer.cs)** | `ISynchronizer` implementation using `IHybridCache` as a distributed advisory lock store. |
| **[`ServiceStatus`](./ServiceStatus.cs)** | Enum representing hosted service lifecycle: `Initializing`, `NoOp`, `Starting`, `Sleeping`, `Running`, `Paused`, `Stopping`, `Stopped`. |
| **[`WorkStatus`](./Work/WorkStatus.cs)** | Enum representing work item lifecycle: `Created`, `Started`, `Indeterminate`, `Completed`, `Failed`, `Abandoned`. |
| [`IHostSettings`](./IHostSettings.cs) | Interface exposing `SolutionName`, `DomainName`, `EnvironmentName`, and `Source` from `HostSettings`. |
| [`IWorkProvider`](./Work/IWorkProvider.cs) | Pluggable storage interface for `WorkState` persistence: get, create, update. |
| [`ISynchronizer`](./Synchronization/ISynchronizer.cs) | Distributed advisory lock interface: `EnterAsync<T>` / `ExitAsync<T>` for type-and-name-scoped locking. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - `ExecutionContext` is created per hosted service invocation; `HostSettings` is consumed by `DefaultCacheKeyProvider` and event naming.
- **[`CoreEx.Caching`](../Caching/README.md)** - `IHybridCache` is the backing store for both `HybridCacheWorkProvider` and `HybridCacheSynchronizer`.
- **[`CoreEx.Invokers`](../Invokers/README.md)** - `HostedServiceInvoker` and `WorkOrchestratorInvoker` use the invoker tracing pipeline to emit OpenTelemetry spans for hosted service and work executions.