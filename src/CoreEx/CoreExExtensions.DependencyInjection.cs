#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class CoreExExtensions
{
    /// <summary>
    /// Adds a <b>scoped</b> service to instantiate a new <see cref="ExecutionContext"/> instance using an <paramref name="executionContextFactory"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="executionContextFactory">The function to override the creation of the <see cref="ExecutionContext"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>Where the <paramref name="executionContextFactory"/> is <see langword="null"/>, then the <see cref="ExecutionContext.Create"/> is used to create.</remarks>
    public static IServiceCollection AddExecutionContext(this IServiceCollection services, Func<IServiceProvider, ExecutionContext>? executionContextFactory = null) => services.ThrowIfNull().AddScoped(sp =>
    {
        var ec = executionContextFactory?.Invoke(sp) ?? ExecutionContext.Create?.Invoke() ??
            throw new InvalidOperationException("Unable to create 'ExecutionContext' instance; either (in order) 'executionContextFactory' resulted in null, or 'ExecutionContext.Create' resulted in null.");

        ec.ServiceProvider = sp;

        ExecutionContext.Reset();
        ExecutionContext.SetCurrent(ec);

        return ec;
    });

    /// <summary>
    /// Adds a <b>scoped</b> service to instantiate and <paramref name="configure"/> a new <see cref="ExecutionContext"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="ExecutionContext"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddExecutionContext(this IServiceCollection services, Action<IServiceProvider, ExecutionContext>? configure) => services.ThrowIfNull().AddScoped(sp =>
    {
        var ec = ExecutionContext.Create?.Invoke() ??
            throw new InvalidOperationException("Unable to create 'ExecutionContext' instance; the 'ExecutionContext.Create' resulted in null.");

        ec.ServiceProvider = sp;

        configure?.Invoke(sp, ec);

        ExecutionContext.Reset();
        ExecutionContext.SetCurrent(ec);

        return ec;
    });

    /// <summary>
    /// Adds a <b>singleton</b> service to instantiate a new <see cref="PrecisionTimeProvider"/> instance with the specified <paramref name="decimalPlaces"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="decimalPlaces">The number of decimal places (0-7) for fractional seconds. Defaults to 6 (microseconds) for database compatibility.</param>
    /// <param name="innerProvider">The optional inner <see cref="TimeProvider"/> to use. Defaults to null.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddPrecisionTimeProvider(this IServiceCollection services, int decimalPlaces = 6, TimeProvider? innerProvider = null)
        => services.ThrowIfNull().AddSingleton<TimeProvider>(new PrecisionTimeProvider(decimalPlaces, innerProvider));

    /// <summary>
    /// Dynamically registers all types within the specified assembly that have a defined <see cref="ServiceLifetimeAttribute"/> as inferred by (using) the specified generic type.
    /// </summary>
    /// <typeparam name="TAssembly1">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddDynamicServicesUsing<TAssembly1>(this IServiceCollection services)
        => services.ThrowIfNull().AddDynamicServicesUsing(typeof(TAssembly1).Assembly);

    /// <summary>
    /// Dynamically registers all types within the specified assemblies that have a defined <see cref="ServiceLifetimeAttribute"/> as inferred by (using) the specified generic types.
    /// </summary>
    /// <typeparam name="TAssembly1">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <typeparam name="TAssembly2">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddDynamicServicesUsing<TAssembly1, TAssembly2>(this IServiceCollection services)
        => services.ThrowIfNull().AddDynamicServicesUsing(typeof(TAssembly1).Assembly, typeof(TAssembly2).Assembly);

    /// <summary>
    /// Dynamically registers all types within the specified assemblies that have a defined <see cref="ServiceLifetimeAttribute"/> as inferred by (using) the specified generic types.
    /// </summary>
    /// <typeparam name="TAssembly1">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <typeparam name="TAssembly2">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <typeparam name="TAssembly3">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddDynamicServicesUsing<TAssembly1, TAssembly2, TAssembly3>(this IServiceCollection services)
        => services.ThrowIfNull().AddDynamicServicesUsing(typeof(TAssembly1).Assembly, typeof(TAssembly2).Assembly, typeof(TAssembly3).Assembly);

    /// <summary>
    /// Dynamically registers all types within the specified assemblies that have a defined <see cref="ServiceLifetimeAttribute"/> as inferred by (using) the specified generic types.
    /// </summary>
    /// <typeparam name="TAssembly1">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <typeparam name="TAssembly2">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <typeparam name="TAssembly3">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <typeparam name="TAssembly4">The <see cref="Type"/> to infer the <see cref="Assembly"/> from.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddDynamicServicesUsing<TAssembly1, TAssembly2, TAssembly3, TAssembly4>(this IServiceCollection services)
        => services.ThrowIfNull().AddDynamicServicesUsing(typeof(TAssembly1).Assembly, typeof(TAssembly2).Assembly, typeof(TAssembly3).Assembly, typeof(TAssembly4).Assembly);

    /// <summary>
    /// Dynamically registers all types within the specified <paramref name="assemblies"/> that have a defined <see cref="ServiceLifetimeAttribute"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="assemblies">The assemblies to probe for all types.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddDynamicServicesUsing(this IServiceCollection services, params IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies.Distinct())
        {
            foreach (var match in from type in assembly.GetTypes()
                                  where !type.IsAbstract && !type.IsGenericTypeDefinition
                                  let sla = ServiceLifetimeAttribute.GetCustomAttribute(type)
                                  where sla is not null
                                  select new { type, sla })
            {
                match.sla?.AddService(services, match.type);
            }
        }

        return services;
    }

    /// <summary>
    /// Adds a <b>singleton</b> service for the internal <see cref="IMemoryCache"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>Where not explicitly registered then a static internal <see cref="IMemoryCache"/> will be used.</remarks>
    public static IServiceCollection AddInternalCache(this IServiceCollection services) => services.ThrowIfNull().AddKeyedSingleton<IMemoryCache, MemoryCache>(CoreEx.Abstractions.Internal.CacheServiceKey);

    /// <summary>
    /// Adds a <b>scoped</b> service for the <see cref="ICacheKeyProvider"/> using the <see cref="DefaultCacheKeyProvider"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddDefaultCacheKeyProvider(this IServiceCollection services) => services.ThrowIfNull().AddScoped<ICacheKeyProvider, DefaultCacheKeyProvider>();

    /// <summary>
    /// Adds a <b>scoped</b> service for the <see cref="IHybridCache"/> using the <see cref="MemoryOnlyHybridCache"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddMemoryOnlyHybridCache(this IServiceCollection services) => services.ThrowIfNull().AddScoped<IHybridCache, MemoryOnlyHybridCache>();

    /// <summary>
    /// Adds the <see cref="IdempotencyKeyHandler"/> to the HTTP client pipeline which automatically manages the addition of idempotency keys to requests.
    /// </summary>
    /// <param name="builder">The <see cref="IHttpClientBuilder"/>.</param>
    /// <param name="configure">The action to configure the <see cref="IdempotencyKeyHandler"/> instance.</param>
    /// <returns>The <paramref name="builder"/> to support fluent-style method-chaining.</returns>
    /// <remarks>See <see cref="IdempotencyKeyHandler"/> for further details.</remarks>
    public static IHttpClientBuilder AddIdempotencyKeyHandler(this IHttpClientBuilder builder, Action<IServiceProvider, IdempotencyKeyHandler>? configure = null) => builder.ThrowIfNull().AddHttpMessageHandler(sp =>
    {
        var handler = new IdempotencyKeyHandler();
        configure?.Invoke(sp, handler);
        return handler;
    });

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="HybridCacheSynchronizer"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="HybridCacheSynchronizer"/> instance.</param>
    /// <param name="addAsISynchronizer">Indicates whether to also register as the <see cref="ISynchronizer"/> service.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddHybridCacheSynchronizer(this IServiceCollection services, Action<IServiceProvider, HybridCacheSynchronizer>? configure = null, bool addAsISynchronizer = true)
    {
        services.ThrowIfNull().AddScoped(sp =>
        {
            var synchronizer = ActivatorUtilities.CreateInstance<HybridCacheSynchronizer>(sp);
            configure?.Invoke(sp, synchronizer);
            return synchronizer;
        });

        if (addAsISynchronizer)
            services.AddScoped<ISynchronizer, HybridCacheSynchronizer>(sp => sp.GetRequiredService<HybridCacheSynchronizer>());

        return services;
    }

    /// <summary>
    /// Registers a post-configuration for all health checks to further configure and add the <see cref="HealthCheckTags.Startup"/> and <see cref="HealthCheckTags.Ready"/> tags where not currently defined.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The function to configure <i>each</i> <see cref="HealthCheckRegistration"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="configure"/> function should return <see langword="true"/> to add the default tags, otherwise, <see langword="false"/> to skip.</remarks>
    public static IServiceCollection PostConfigureAllHealthChecks(this IServiceCollection services, Func<HealthCheckRegistration, bool>? configure = null)
    {
        return services.ThrowIfNull().PostConfigureAll<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                if (configure?.Invoke(registration) ?? true)
                {
                    registration.Tags.Add(nameof(HealthCheckTags.Startup));
                    registration.Tags.Add(nameof(HealthCheckTags.Ready));
                }
            }
        });
    }

    /// <summary>
    /// Adds a <b>singleton</b> <see cref="HostedServiceManager"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="HostedServiceManager"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddHostedServiceManager(this IServiceCollection services, Action<IServiceProvider, HostedServiceManager>? configure = null)
    {
        return services.ThrowIfNull().AddSingleton(sp =>
        {
            var hsm = ActivatorUtilities.CreateInstance<HostedServiceManager>(sp);
            configure?.Invoke(sp, hsm);
            return hsm;
        });
    }

    /// <summary>
    /// Adds a <b>singleton</b> <typeparamref name="THostedService"/> keyed service that will be executed as a hosted service (i.e. in the background).
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The keyed singleton and health check key.</param>
    /// <param name="configure">An optional action to configure the <typeparamref name="THostedService"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>Also automatically adds a health-check for the hosted service.</remarks>
    public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services, string serviceKey, Action<IServiceProvider, THostedService>? configure = null) where THostedService : HostedServiceBase
        => AddHostedService<THostedService>(services, serviceKey, sp => ActivatorUtilities.CreateInstance<THostedService>(sp, sp.GetRequiredService<ILoggerFactory>().CreateLogger<THostedService>()), configure);

    /// <summary>
    /// Adds a <b>singleton</b> <typeparamref name="THostedService"/> keyed service that will be executed as a hosted service (i.e. in the background).
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="serviceKey">The keyed singleton and health check key.</param>
    /// <param name="factory">The function to create the <typeparamref name="THostedService"/> instance.</param>
    /// <param name="configure">An optional action to configure the <typeparamref name="THostedService"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>Also automatically adds a health-check for the hosted service.</remarks>
    public static IServiceCollection AddHostedService<THostedService>(this IServiceCollection services, string serviceKey, Func<IServiceProvider, THostedService> factory, Action<IServiceProvider, THostedService>? configure = null) where THostedService : HostedServiceBase
    {
        factory.ThrowIfNull();

        // Register health check for the hosted service.
        var hc = new HostedServiceHealthCheck();
        services.ThrowIfNull().AddHealthChecks().AddCheck(serviceKey.ThrowIfNullOrEmpty(), hc, tags: HealthCheckTags.StartUpAndReadyOnly);

        // Register the hosted service - this allows access to the instance by the service key where required; need by the HostedServiceManager.
        services.AddKeyedSingleton<HostedServiceBase>(serviceKey, (sp, _) =>
        {
            var hs = factory(sp);
            hs.ServiceName = serviceKey;
            hs.HealthCheck = hc;
            configure?.Invoke(sp, hs);
            return hs;
        });

        // Does not use 'AddHostedService' as this does not allow multiple instances to be registered with the same type; see https://github.com/dotnet/runtime/issues/38751.
        return services.AddSingleton<IHostedService>(sp => sp.GetRequiredKeyedService<HostedServiceBase>(serviceKey));
    }
}