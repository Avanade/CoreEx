# CoreEx.Database.Abstractions

> Provides `DatabaseArgs` per-operation configuration, `DatabaseInvoker` for OpenTelemetry-traced command execution, and `IDatabaseParameters<T>` for fluent parameter accumulation.

## Overview

`CoreEx.Database.Abstractions` holds the supporting types that every `DatabaseCommand` execution depends on but that don't belong in the core `DatabaseCommand` class itself.

`DatabaseArgs` carries per-operation settings — such as the transaction `IsolationLevel` — that flow from the repository method down to the command execution without polluting the `IDatabase` interface.

`DatabaseInvoker` is the `InvokerBase<IDatabase>` that wraps every command in an OpenTelemetry `Activity` span, writes Start/Complete/Error structured log entries, and tags the span with the SQL statement text, operation result, and error details.

`IDatabaseParameters<T>` is the fluent parameter-builder interface implemented by `DatabaseCommand` and `DatabaseParameterCollection`, allowing extension methods to add typed parameters in a chainable style.

## Key types

| Type | Description |
|------|--------------|
| **[`DatabaseArgs`](./DatabaseArgs.cs)** | Per-operation options: `IsolationLevel` applied during command execution. |
| _[`DatabaseArgsBase`](./DatabaseArgsBase.cs)_ | Abstract base for `DatabaseArgs`; holds defaults for `Refresh` and `TransformException` extended by `DatabaseArgs`, and used by the `DatabaseCommand`. |
| **[`DatabaseInvoker`](./DatabaseInvoker.cs)** | `InvokerBase<IDatabase>` — wraps every command execution with an `Activity` span tagged with the SQL text, result, and error details; writes structured Start/Complete/Error log entries. |
| [`IDatabaseParameters<T>`](./IDatabaseParameters.cs) | Fluent interface exposing `Parameters` (`DatabaseParameterCollection`) and `Database` (`IDatabase`); implemented by `DatabaseCommand` to enable chained `AddParameter` calls. |

## Related Namespaces

- **[`CoreEx.Database`](../README.md)** - `DatabaseCommand` and `Database` consume these types directly.
- **[`CoreEx.Invokers`](../../CoreEx/Invokers/README.md)** - `DatabaseInvoker` extends `InvokerBase<IDatabase>` using the standard tracing/logging pipeline.