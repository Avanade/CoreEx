# CoreEx.DependencyInjection

> Provides service lifetime attributes (`[ScopedService]`, `[SingletonService]`, `[TransientService]`) that mark implementation types for automatic discovery and registration into the .NET DI container.

## Overview

`CoreEx.DependencyInjection` simplifies service registration by letting implementation classes declare their own DI lifetime via an attribute, rather than requiring manual `services.AddScoped<TService, TImpl>()` call-outs in `Program.cs` or startup code.

Types decorated with one of the lifetime attributes are discovered at startup by `AddDynamicServicesUsing<T1, T2, ...>()` (defined in `CoreExExtensions.DependencyInjection.cs`) and registered automatically. The attribute can specify the service type to register against, and/or an optional key for keyed-service scenarios.

This pattern is used consistently across CoreEx's own infrastructure implementations — repositories, application services, and invokers — to keep startup code thin and to make service dependencies self-describing on the implementation class itself.

## Key capabilities

- 🏷️ **Declarative lifetime**: Apply `[ScopedService]`, `[SingletonService]`, or `[TransientService]` to an implementation class to declare its DI lifetime alongside the code that implements it.
- 🔗 **Explicit service type**: Optionally specify the service interface type (`[ScopedService<IMyService>]`) to control what the type is registered as; defaults to itself where not provided.
- 🔑 **Keyed service support**: Set `Key` on any attribute to register the type as a keyed service for scenarios requiring multiple implementations of the same interface.
- 🔍 **Auto-discovery**: `AddDynamicServicesUsing<T>()` scans assemblies containing the specified marker types and registers all attributed types, removing the need for manual per-type registration.

## Key types

| Type | Description |
|------|-------------|
| **[`ScopedServiceAttribute`](./ScopedServiceAttribute.cs)** | Marks an implementation class for registration as `ServiceLifetime.Scoped`. |
| **[`ScopedServiceAttribute<TService>`](./ScopedServiceAttributeT.cs)** | Typed variant of `ScopedServiceAttribute` specifying the service interface type explicitly. |
| **[`SingletonServiceAttribute`](./SingletonServiceAttribute.cs)** | Marks an implementation class for registration as `ServiceLifetime.Singleton`. |
| **[`SingletonServiceAttribute<TService>`](./SingletonServiceAttributeT.cs)** | Typed variant of `SingletonServiceAttribute` specifying the service interface type explicitly. |
| **[`TransientServiceAttribute`](./TransientServiceAttribute.cs)** | Marks an implementation class for registration as `ServiceLifetime.Transient`. |
| **[`TransientServiceAttribute<TService>`](./TransientServiceAttributeT.cs)** | Typed variant of `TransientServiceAttribute` specifying the service interface type explicitly. |
| _[`ServiceLifetimeAttribute`](./ServiceLifetimeAttribute.cs)_ | Abstract base attribute providing `Lifetime`, `Key`, and the service-type resolution logic shared by all three lifetime attributes. |

## Related Namespaces

- **[`CoreEx`](../README.md)** - `CoreExExtensions.DependencyInjection.cs` contains `AddDynamicServicesUsing<T>()` and `AddExecutionContext()` — the DI registration entry points that consume these attributes.