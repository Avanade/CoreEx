# CoreEx.Invokers

> Provides the `InvokerBase<TCaller>` infrastructure for wrapping method invocations with consistent OpenTelemetry tracing, structured logging, and error tagging — the mechanism through which all CoreEx operations emit spans and log entries uniformly.

## Overview

`CoreEx.Invokers` defines a lightweight, inheritance-based invocation pipeline. Any service or infrastructure component that wants consistent tracing and logging derives its invoker from `InvokerBase<TCaller>` (or the non-generic `InvokerBase`) and overrides `OnInvokeAsync` to add cross-cutting behavior — OpenTelemetry activity creation, ILogger entries, exception tagging, and result recording.

`InvokerTracer` is the runtime context passed into every invocation: it creates and manages the `Activity` span, tags it with operation result, error type, error code, error message, and tenant ID, and writes structured log entries at the appropriate severity. Tracing and logging can each be enabled or disabled per invoker type via `IConfiguration` (`CoreEx:Invokers:{TypeName}:TracingEnabled` / `LoggingEnabled`), making it easy to reduce telemetry noise in specific scenarios.

The built-in `HostedServiceInvoker` and `WorkOrchestratorInvoker` implementations apply this same pattern to hosted service and work-orchestrator executions, giving those operations first-class spans without any additional instrumentation work.

## Key capabilities

- 📡 **OpenTelemetry spans**: Each invocation opens an `Activity` with a structured name (`{invoker}.{caller}.{member}`), tags result, error type, error code, message, and tenant ID, then closes the span on completion — success or failure.
- 📝 **Structured logging**: `InvokerTracer` logs Start, Complete, Error, and Exception events at configurable log levels, with invoker, caller, and member name as structured properties.
- ⚙️ **Per-type configuration**: Tracing and logging can be switched on/off per invoker type name via `IConfiguration`, allowing hot control without code changes.
- 🔌 **Override-friendly pipeline**: `OnInvokeAsync` is a `virtual` method; derived invokers add behavior by overriding it and calling the base, following a decorator chain. The caller func is never invoked directly.
- 🔑 **Caller-typed base**: `InvokerBase<TCaller>` constrains the caller type at compile time, ensuring type-safe invoker/caller pairings in the override signature.
- 🏠 **Built-in hosted service and work invokers**: `HostedServiceInvoker` and `WorkOrchestratorInvoker` extend `InvokerBase<T>` to apply the standard tracing/logging pipeline to hosted service and work-orchestration executions.

## Key types

| Type | Description |
|------|-------------|
| _[`InvokerBase`](./InvokerBase.cs)_ | Non-generic abstract base; holds the `IServiceProvider` reference and the root `Invoke` / `InvokeAsync` entry-point methods used by calling code. |
| _[`InvokerBase<TCaller>`](./InvokerBaseT.cs)_ | Generic abstract base constraining the caller type; defines `OnInvokeAsync<TResult>` as the virtual override point for cross-cutting behavior. |
| **[`InvokerTracer`](./InvokerTracer.cs)** | Readonly struct managing the `Activity` span lifecycle; writes Start/Complete/Error/Exception log events; tags span with result, error type/code/message, and tenant ID. |
| **[`HostedServiceInvoker`](./HostedServiceInvoker.cs)** | Concrete invoker used by `HostedServiceBase` to wrap every `OnExecuteAsync` call with a span and log entries. |
| **[`WorkOrchestratorInvoker`](./WorkOrchestratorInvoker.cs)** | Concrete invoker used by `WorkOrchestrator` to wrap work-item lifecycle operations with spans and log entries. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - `ExecutionContext.Current` provides the `IServiceProvider` and tenant ID accessed inside `InvokerTracer` to tag spans.
- **[`CoreEx.Hosting`](../Hosting/README.md)** - `HostedServiceBase` and `WorkOrchestrator` each hold a static `HostedServiceInvoker` / `WorkOrchestratorInvoker` instance used for all their executions.
- **[`CoreEx.AspNetCore`](../../CoreEx.AspNetCore/README.md)** - `WebApiInvoker` is an `InvokerBase<WebApi>` implementation used by the `WebApi` helpers to trace every HTTP operation.