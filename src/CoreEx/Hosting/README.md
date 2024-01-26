# CoreEx.Hosting

The `CoreEx.Hosting` namespace provides additional [hosted service (worker)](https://learn.microsoft.com/en-us/dotnet/core/extensions/workers) runtime capabilities.

<br/>

## Motivation

To enable improved hosted service consistency and testability, plus additional [`IHostedService`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice) runtime capabilities.

<br/>

## Host startup

To improve consistency and testability the [`IHostStartup`](./IHostStartup.cs) and [`HostStartup`](./HostStartup) implementations are provided. By seperating out the key [Dependency Injection (DI)](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection) configuration from the underlying host configuration enables the DI configuration to be tested in isolation against a _test-host_ where applicable.

The following is an example of a `HostStartup` implementation.

```csharp
public class Startup : HostStartup
{
    public override void ConfigureAppConfiguration(HostBuilderContext context, IConfigurationBuilder config)
    {
        config.AddEnvironmentVariables("Prefix_");
    }

    /// <inheritdoc/>
    public override void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSettings()
            .AddExecutionContext()
            .AddJsonSerializer();
	}
}
```

The following is an example of a `Program` implementation that initiates a host and uses the [`ConfigureHostStartup`](HostStartupExtensions.cs) extension method to integrate the `Startup` functionality. This has an added advantage of being able to add specific startup capabilities directly to a host that should not be available to the _test-host_ (as demonstrated by `ConfigureFunctionsWorkerDefaults`).

```csharp
new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureHostStartup<Startup>()
    .Build().Run();
```

<br/>

## Hosted services

The following additional [`IHostedService`](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.hosting.ihostedservice) implementations are provided.

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