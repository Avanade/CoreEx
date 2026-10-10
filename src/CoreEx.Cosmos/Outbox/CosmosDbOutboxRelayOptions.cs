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
    /// <remarks>The container must already exist (partitioned on <c>/id</c>); the relay never creates it. See <c>CosmosDbProvisionArgs.OutboxLeaseContainer</c>.</remarks>
    public required string LeaseContainerId { get; init; }

    /// <summary>
    /// The default lease <see cref="Container"/> identifier (<c>$outbox-leases</c>); a single container shared by all outbox relays (processors are uniquely named per monitored container).
    /// </summary>
    public const string DefaultLeaseContainerId = "$outbox-leases";

    /// <summary>
    /// Gets or sets the Change Feed Processor instance name; must be distinct per concurrently-running instance for the same <see cref="ContainerId"/>/<see cref="LeaseContainerId"/> pair.
    /// </summary>
    public required string InstanceName { get; init; }

    /// <summary>
    /// Gets or sets the poll interval; where not specified, the Change Feed Processor default applies.
    /// </summary>
    public TimeSpan? PollInterval { get; set; }

    /// <summary>
    /// Gets or sets whether instrumentation is enabled for change-feed polling and lease maintenance.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/> to suppress frequent background SDK and HTTP spans, matching the relational outbox relays. Applied when the processor starts or resumes.</remarks>
    public bool IsInstrumentationEnabledForPolling { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of items returned per batch; where not specified, the Change Feed Processor default applies.
    /// </summary>
    /// <remarks>Named to match the equivalent SQL Server/Postgres outbox relay hosted service configuration (<c>DatabaseOutboxRelayHostedServiceBase.BatchSize</c>) rather than the underlying Change Feed
    /// Processor SDK's own <c>WithMaxItems</c> terminology - this property still maps directly onto it.</remarks>
    public int? BatchSize { get; set; }

    /// <summary>
    /// Gets or sets the start time; where not specified, the relay explicitly reads from the beginning for a brand-new lease with no prior checkpoint so a first-ever startup or container-reset recovery
    /// does not skip a pre-existing outbox backlog. Existing lease checkpoints take precedence; an explicit start time intentionally excludes earlier events on a fresh lease.
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Gets or sets the delay before (and between) attempts to recover the Change Feed Processor where the monitored and/or lease container was deleted and recreated whilst the relay was running (e.g. a
    /// provisioning reset). Defaults to 5 seconds.
    /// </summary>
    public TimeSpan RecoveryDelay { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the <see cref="ResiliencePipeline{T}"/> used to protect batch execution with a self-pausing/self-resuming circuit breaker; where not specified, defaults to
    /// <see cref="CosmosDbOutboxRelayResiliency.CreateRelayCircuitBreakerResiliency(int, TimeSpan?, TimeSpan?, TimeSpan?, double)"/>'s own defaults.
    /// </summary>
    public ResiliencePipeline<Result>? Resiliency { get; set; }
}
