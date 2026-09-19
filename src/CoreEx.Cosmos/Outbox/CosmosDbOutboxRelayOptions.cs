namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Provides the configuration options for a <see cref="CosmosDbOutboxRelay"/>.
/// </summary>
public sealed class CosmosDbOutboxRelayOptions
{
    /// <summary>
    /// Gets or sets the monitored <see cref="Container"/> identifier.
    /// </summary>
    public required string ContainerId { get; init; }

    /// <summary>
    /// Gets or sets the lease <see cref="Container"/> identifier.
    /// </summary>
    public required string LeaseContainerId { get; init; }

    /// <summary>
    /// Gets or sets the Change Feed Processor instance name; must be distinct per concurrently-running instance for the same <see cref="ContainerId"/>/<see cref="LeaseContainerId"/> pair.
    /// </summary>
    public required string InstanceName { get; init; }

    /// <summary>
    /// Gets or sets the poll interval; where not specified, the Change Feed Processor default applies.
    /// </summary>
    public TimeSpan? PollInterval { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items returned per batch; where not specified, the Change Feed Processor default applies.
    /// </summary>
    /// <remarks>Named to match the equivalent SQL Server/Postgres outbox relay hosted service configuration (<c>DatabaseOutboxRelayHostedServiceBase.BatchSize</c>) rather than the underlying Change Feed
    /// Processor SDK's own <c>WithMaxItems</c> terminology - this property still maps directly onto it.</remarks>
    public int? BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the start time; where not specified, the Change Feed Processor's own default applies (confirmed empirically to mean "from the beginning" for a brand-new lease with no prior checkpoint, so
    /// a first-ever relay startup does not silently skip a pre-existing outbox backlog).
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="ResiliencePipeline{T}"/> used to protect batch execution with a self-pausing/self-resuming circuit breaker; where not specified, defaults to
    /// <see cref="CosmosDbOutboxRelayResiliency.CreateRelayCircuitBreakerResiliency(int, TimeSpan?, TimeSpan?, TimeSpan?, double)"/>'s own defaults.
    /// </summary>
    public ResiliencePipeline<Result>? Resiliency { get; set; }
}
