namespace CoreEx.Hosting;

/// <summary>
/// Provides an <see cref="IHealthCheck"/> for a <see cref="HostedServiceBase"/> reporting its <see cref="HostedServiceBase.Status"/>.
/// </summary>
/// <remarks>The <see cref="Data"/> indicates current state; assumes healthy where reported.</remarks>
public sealed class HostedServiceHealthCheck : IHealthCheck
{
    private volatile Dictionary<string, object>? _data;

    /// <summary>
    /// Gets or sets the last updated health check data.
    /// </summary>
    /// <remarks>An initial <c>null</c> value indicates unhealthy as no status has been reported.</remarks>
    public Dictionary<string, object>? Data { get => _data; set => _data = value.ThrowIfNull(nameof(Data)); }

    /// <summary>
    /// Gets or sets the latest <see cref="HealthCheckResult"/> to report.
    /// </summary>
    /// <remarks>Defaults to <see cref="HealthCheckResult.Unhealthy"/>; no health check reported.</remarks>
    public HealthCheckResult Result { get; set; } = HealthCheckResult.Unhealthy("No health check reported.");

    /// <inheritdoc/>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) => Task.FromResult(Result);
}