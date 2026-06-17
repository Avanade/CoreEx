#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides standard extensions.
/// </summary>
public static partial class CoreExReferenceDataExtensions
{
    /// <summary>
    /// Adds the <see cref="ReferenceDataOrchestrator"/> created by the <paramref name="orchestratorFactory"/> as a singleton service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="orchestratorFactory">The function to create the <see cref="ReferenceDataOrchestrator"/>.</param>
    /// <param name="healthCheck">Indicates whether a corresponding <see cref="ReferenceDataOrchestratorHealthCheck"/> should be configured.</param>
    /// <param name="healthCheckName">The health check name; defaults to '<c>reference-data-orchestrator</c>'.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    /// <remarks>Also, registers the <see cref="ReferenceDataHybridCache"/> as the required <see cref="IReferenceDataCache"/> scoped service.</remarks>
    public static IServiceCollection AddReferenceDataOrchestrator(this IServiceCollection services, Func<IServiceProvider, ReferenceDataOrchestrator> orchestratorFactory, bool healthCheck = true, string? healthCheckName = "reference-data-orchestrator")
    {
        services.ThrowIfNull().AddSingleton(sp => orchestratorFactory(sp));
        if (healthCheck)
            services.AddHealthChecks().AddTypeActivatedCheck<ReferenceDataOrchestratorHealthCheck>(healthCheckName ?? "reference-data-orchestrator", null, tags: HealthCheckTags.StartUpAndReadyOnly);

        services.TryAddScoped<IReferenceDataCache>(sp => new ReferenceDataHybridCache(sp.GetService<IHybridCache>() ?? ActivatorUtilities.GetServiceOrCreateInstance<MemoryOnlyHybridCache>(sp)));
        return services;
    }

    /// <summary>
    /// Adds the <see cref="ReferenceDataOrchestrator"/> using an <see cref="IReferenceDataCache"/> as a singleton service automatically registering the <see cref="IReferenceDataProvider"/> (see <see cref="ReferenceDataOrchestrator.Register"/>).
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="healthCheck">Indicates whether a corresponding <see cref="ReferenceDataOrchestratorHealthCheck"/> should be configured.</param>
    /// <param name="healthCheckName">The health check name; defaults to '<c>reference-data-orchestrator</c>'.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    /// <remarks>Where an <see cref="IReferenceDataCache"/> has not been registered then the <see cref="ReferenceDataHybridCache"/> will be used by default.</remarks>
    public static IServiceCollection AddReferenceDataOrchestrator(this IServiceCollection services, bool healthCheck = true, string? healthCheckName = "reference-data-orchestrator")
        => AddReferenceDataOrchestrator(services, sp => new ReferenceDataOrchestrator(sp, sp.GetRequiredService<ILogger<ReferenceDataOrchestrator>>()).Register(), healthCheck, healthCheckName);

    /// <summary>
    /// Adds the <see cref="ReferenceDataOrchestrator"/> using an <see cref="IReferenceDataCache"/> as a singleton service automatically registering the specified <typeparamref name="TProvider"/> (see <see cref="ReferenceDataOrchestrator.Register"/>).
    /// </summary>
    /// <typeparam name="TProvider">The <see cref="IReferenceDataProvider"/> to register.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="healthCheck">Indicates whether a corresponding <see cref="ReferenceDataOrchestratorHealthCheck"/> should be configured.</param>
    /// <param name="healthCheckName">The health check name; defaults to '<c>reference-data-orchestrator</c>'.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    /// <remarks>Where an <see cref="IReferenceDataCache"/> has not been registered then the <see cref="ReferenceDataHybridCache"/> will be used by default.</remarks>
    public static IServiceCollection AddReferenceDataOrchestrator<TProvider>(this IServiceCollection services, bool healthCheck = true, string? healthCheckName = "reference-data-orchestrator") where TProvider : IReferenceDataProvider
        => AddReferenceDataOrchestrator(services, sp => new ReferenceDataOrchestrator(sp, sp.GetRequiredService<ILogger<ReferenceDataOrchestrator>>()).Register<TProvider>(), healthCheck, healthCheckName);
}