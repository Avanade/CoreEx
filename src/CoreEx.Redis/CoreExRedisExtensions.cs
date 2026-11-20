#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Registers Redis coordination independently of ordinary caching.
/// </summary>
public static class CoreExRedisExtensions
{
    /// <summary>
    /// Adds scoped Redis providers over the host-owned connection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="options">The optional ownership options.</param>
    /// <param name="maxIdempotencyPayloadSize">The maximum serialized terminal response payload size in bytes. Defaults to 1 MiB; use <see langword="null"/> for no configured limit.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRedisCoordination(this IServiceCollection services, CoordinationOptions? options = null, int? maxIdempotencyPayloadSize = 1024 * 1024)
    {
        options ??= new();
        options.Validate();
        if (maxIdempotencyPayloadSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxIdempotencyPayloadSize));

        services.AddScoped(sp => new CoreEx.Redis.RedisCoordination(sp.GetRequiredService<IConnectionMultiplexer>(), sp.GetRequiredService<ILogger<CoreEx.Redis.RedisCoordination>>(), options, maxIdempotencyPayloadSize));
        services.AddScoped<ISynchronizer>(sp => sp.GetRequiredService<CoreEx.Redis.RedisCoordination>());
        services.AddScoped<IIdempotencyStore>(sp => sp.GetRequiredService<CoreEx.Redis.RedisCoordination>());
        return services;
    }

    /// <summary>
    /// Adds the singleton <see cref="CoreEx.Redis.RedisWorkProvider"/> as the <see cref="CoreEx.Hosting.Work.IWorkProvider"/> over the host-owned connection, and a scoped <see cref="CoreEx.Hosting.Work.WorkOrchestrator"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The optional Redis work-provider configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRedisWorkProvider(this IServiceCollection services, Action<CoreEx.Redis.RedisWorkProviderOptions>? configure = null)
    {
        services.ThrowIfNull();
        var options = new CoreEx.Redis.RedisWorkProviderOptions();
        configure?.Invoke(options);
        options.Validate();

        services.TryAddSingleton<CoreEx.Hosting.Work.IWorkProvider>(sp => new CoreEx.Redis.RedisWorkProvider(sp.GetRequiredService<IConnectionMultiplexer>(), options));
        services.TryAddScoped(sp =>
        {
            var workOptions = new CoreEx.Hosting.Work.WorkOrchestratorOptions();
            workOptions.Validate();
            if (options.OperationTimeout > TimeSpan.FromTicks(workOptions.LeaseDuration.Ticks / 3))
                throw new InvalidOperationException("The Redis operation timeout multiplied by three must not exceed the work lease duration.");

            return new CoreEx.Hosting.Work.WorkOrchestrator(sp.GetRequiredService<CoreEx.Hosting.Work.IWorkProvider>(), workOptions);
        });

        return services;
    }
}
