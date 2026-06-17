namespace CoreEx.RefData.HealthChecks;

/// <summary>
/// Provides a <see cref="ReferenceDataOrchestrator"/> <see cref="IHealthCheck"/>.
/// </summary>
/// <param name="orchestrator">The <see cref="ReferenceDataOrchestrator"/>.</param>
public class ReferenceDataOrchestratorHealthCheck(ReferenceDataOrchestrator orchestrator) : IHealthCheck
{
    private readonly ReferenceDataOrchestrator _orchestrator = orchestrator.ThrowIfNull();

    /// <inheritdoc/>
    /// <remarks>Will always return <see cref="HealthCheckResult.Healthy"/>.</remarks>
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var data = new Dictionary<string, object>
        {
            { "types", _orchestrator.GetAllTypes().Select(x => x.Name).ToArray() }
        };

        return Task.FromResult(HealthCheckResult.Healthy(null, data));
    }
}