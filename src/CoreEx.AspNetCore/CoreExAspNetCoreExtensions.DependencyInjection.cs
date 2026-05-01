#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class CoreExAspNetCoreExtensions
{
    /// <summary>
    /// Adds a scoped service to instantiate a new <see cref="CoreEx.AspNetCore.Mvc.WebApi"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="CoreEx.AspNetCore.Mvc.WebApi"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddMvcWebApi(this IServiceCollection services, Action<IServiceProvider, CoreEx.AspNetCore.Mvc.WebApi>? configure = null) => services.ThrowIfNull().AddScoped(sp =>
    {
        var webApi = new CoreEx.AspNetCore.Mvc.WebApi(sp.GetService<JsonSerializerOptions>(), sp.GetService<ILogger<CoreEx.AspNetCore.Mvc.WebApi>>(), sp.GetService<ExecutionContext>());
        configure?.Invoke(sp, webApi);
        return webApi;
    });

    /// <summary>
    /// Adds a scoped service to instantiate a new <see cref="CoreEx.AspNetCore.Http.WebApi"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="CoreEx.AspNetCore.Http.WebApi"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddHttpWebApi(this IServiceCollection services, Action<IServiceProvider, CoreEx.AspNetCore.Http.WebApi>? configure = null) => services.ThrowIfNull().AddScoped(sp =>
    {
        var webApi = new CoreEx.AspNetCore.Http.WebApi(sp.GetService<JsonSerializerOptions>(), sp.GetService<ILogger<CoreEx.AspNetCore.Http.WebApi>>(), sp.GetService<ExecutionContext>());
        configure?.Invoke(sp, webApi);
        return webApi;
    });


    /// <summary>
    /// Adds a <b>scoped</b> <see cref="HybridCacheIdempotencyProvider"/> service as the <see cref="IIdempotencyProvider"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="HybridCacheIdempotencyProvider"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>Will also try to add the <b>scoped</b> <see cref="IdempotencyKeyMiddleware"/>.</remarks>
    public static IServiceCollection AddHybridCacheIdempotencyProvider(this IServiceCollection services, Action<IServiceProvider, HybridCacheIdempotencyProvider>? configure = null)
    {
        services.ThrowIfNull().AddScoped<IIdempotencyProvider>(sp =>
        {
            var provider = new HybridCacheIdempotencyProvider(sp.GetRequiredService<IHybridCache>());
            configure?.Invoke(sp, provider);
            return provider;
        });

        services.TryAddScoped<IdempotencyKeyMiddleware>();
        return services;
    }

    /// <summary>
    /// Adds a <b>scoped</b> a scoped <see cref="IdempotencyKeyMiddleware"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddIdempotencyKeyMiddleware(this IServiceCollection services)
        => services.ThrowIfNull().AddScoped<IdempotencyKeyMiddleware>();
}