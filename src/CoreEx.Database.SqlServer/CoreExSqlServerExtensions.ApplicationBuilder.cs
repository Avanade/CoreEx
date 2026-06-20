#pragma warning disable IDE0130 // Namespace does not match folder structure; by design.
namespace Microsoft.Extensions.Hosting;
#pragma warning restore IDE0130 // Namespace does not match folder structure

/// <summary>
/// Provides <see cref="SqlServerDatabase"/> and related extensions.
/// </summary>
public static class CoreExSqlServerExtensions
{
    /// <summary>
    /// Adds <b>singleton</b> <see cref="SqlServerOutboxRelayHostedService"/> keyed service(s) (as per <paramref name="servicesCount"/>) that will be executed as a hosted service (in the background).
    /// </summary>
    /// <param name="builder">The <see cref="IHostApplicationBuilder"/>.</param>
    /// <param name="servicesCount">The number of hosted services to start to enable concurrency of processing across partitions.</param>
    /// <param name="serviceKeyPrefix">The keyed singleton and health check key prefix; defaults to '<c>sqlserver-outbox-relay-</c>'.</param>
    /// <param name="configure">An optional action to configure each <see cref="SqlServerOutboxRelayHostedService"/> instance.</param>
    /// <returns>The <see cref="IHostApplicationBuilder"/> for fluent-style method-chaining.</returns>
    /// <remarks>Where the <paramref name="servicesCount"/> is not specified it will, attempt to get the value from configuration using '<c>CoreEx:Host:Services:OutboxRelay:ServicesCount</c>' as the key; otherwise,
    /// defaults to '<c>4</c>'.
    /// <para>Uses the <see cref="Microsoft.Extensions.DependencyInjection.CoreExExtensions.AddHostedService{THostedService}(IServiceCollection, string, Action{IServiceProvider, THostedService}?)"/> to enable.</para></remarks>
    public static IHostApplicationBuilder AddSqlServerOutboxRelayHostedService(this IHostApplicationBuilder builder, int? servicesCount = null, string serviceKeyPrefix = "sqlserver-outbox-relay-", Action<IServiceProvider, SqlServerOutboxRelayHostedService>? configure = null)
    {
        // Determine the number of services to add.
        builder.ThrowIfNull();
        servicesCount ??= CoreEx.Abstractions.Internal.GetConfigurationValue<int>($"CoreEx:Host:Services:OutboxRelay:ServicesCount", 4, builder.Configuration);
        servicesCount.ThrowWhen(servicesCount => servicesCount <= 0 || servicesCount > 32);

        // Add the services as per count.
        for (int i = 0; i < servicesCount; i++)
            builder.Services.ThrowIfNull().AddHostedService<SqlServerOutboxRelayHostedService>($"{serviceKeyPrefix}{i:00}", configure);

        return builder;
    }
}