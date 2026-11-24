#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class CoreExAspNetCoreExtensions
{
    /// <summary>
    /// Registers the generic operations resource configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">The optional operations configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCoreExOperations(this IServiceCollection services, Action<OperationsOptions>? configure = null)
    {
        services.ThrowIfNull();
        var options = services.AddOptions<OperationsOptions>();
        if (configure is not null)
            options.Configure(configure);

        return services;
    }

    /// <summary>
    /// Registers a message type that can be tracked and queried through the generic operations resource.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="operationName">The stable application-owned operation name.</param>
    /// <param name="configure">The type-specific configuration.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCoreExOperation<TMessage>(this IServiceCollection services, string operationName, Action<OperationTypeOptions<TMessage>>? configure = null)
    {
        services.ThrowIfNull();
        services.AddOptions<OperationsOptions>().Configure(options => options.AddOperation(operationName, configure));
        return services;
    }

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
    /// Adds a <b>scoped</b> <see cref="IdempotencyProvider"/> service as the <see cref="IIdempotencyProvider"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">The action to configure the <see cref="IdempotencyProvider"/> instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    /// <remarks>Will also try to add the <b>scoped</b> <see cref="IdempotencyKeyMiddleware"/>.</remarks>
    public static IServiceCollection AddIdempotencyProvider(this IServiceCollection services, Action<IServiceProvider, IdempotencyProvider>? configure = null)
    {
        services.ThrowIfNull().AddScoped<IIdempotencyProvider>(sp =>
        {
            var provider = new IdempotencyProvider(sp.GetRequiredService<CoreEx.Idempotency.IIdempotencyStore>(), sp.GetService<ILogger<IdempotencyProvider>>());
            configure?.Invoke(sp, provider);
            return provider;
        });

        services.TryAddScoped<IdempotencyKeyMiddleware>();
        return services;
    }

    /// <summary>
    /// Adds a <b>scoped</b> <see cref="IdempotencyKeyMiddleware"/> service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
    public static IServiceCollection AddIdempotencyKeyMiddleware(this IServiceCollection services)
        => services.ThrowIfNull().AddScoped<IdempotencyKeyMiddleware>();
}
