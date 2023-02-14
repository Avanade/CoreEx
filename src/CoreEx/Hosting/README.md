# CoreEx.Hosting

The `CoreEx.Hosting` namespace provides additional [hosted service (worker)](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers) capabilities.

<br/>

## Motivation

To enable additional [`IHostedService`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice) capabilities.

<br/>

## Hosted services

The following additional `IHostedService` implementations are provided.

Class | Description
-|-
[`TimerHostedServiceBase`](./TimerHostedServiceBase.cs) | Provides an `IHostedService` implementation that performs _work_ at a specified `Interval`.
[`SynchronizedTimerHostedServiceBase`](./SynchronizedTimerHostedServiceBase.cs) | Extends `TimerHostedServiceBase` adding [concurrency synchronization](#Concurrency-synchronization) to ensure only a single host can perform _work_ at a time (synchronously).

<br/>

## Concurrency synchronization

To ensure only a single host can perform _work_ at a time concurrency implementation is required; this is enabled by implementing the `Enter` and `Exit` methods defined by the [`IServiceSynchronizer`](./IServiceSynchronizer.cs) interface. The following implementations are provided.

Class | Description
-|-
[`ConcurrentSynchronizer`](./ConcurrentSynchronizer.cs) | Performs _no_ synchronization in that `Enter` will always return `true` resulting in concurrent execution.
[`FileLockSynchronizer`](./FileLockSynchronizer.cs) | Performs synchronization by taking an exclusive lock on a file.
