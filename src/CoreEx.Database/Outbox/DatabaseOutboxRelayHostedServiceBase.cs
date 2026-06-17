namespace CoreEx.Database.Outbox;

/// <summary>
/// Provides the base <see cref="IDatabaseOutboxRelay.RelayAsync(CoreEx.Database.Outbox.DatabaseOutboxRelayArgs, CancellationToken)"/> execution leveraging a <see cref="TimerHostedServiceBase"/>.
/// </summary>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public abstract class DatabaseOutboxRelayHostedServiceBase(IServiceProvider serviceProvider, ILogger logger) : TimerHostedServiceBase(serviceProvider, logger)
{
    private PartitionPicker? _partitionPicker;

    /// <summary>
    /// Gets or sets the batch size.
    /// </summary>
    /// <remarks>Defaults to '<c>25</c>'.</remarks>
    public int BatchSize { get => field; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <summary>
    /// Gets or sets the lease duration used to lock when claiming a batch.
    /// </summary>
    /// <remarks>Defaults to '<c>5</c>' minutes.</remarks>
    public TimeSpan LeaseDuration { get; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <summary>
    /// Gets or sets the backoff duration used to push out availability when cancelling a batch.
    /// </summary>
    /// <remarks>Defaults to '<c>5</c>' seconds.</remarks>
    public TimeSpan BackOffDuration { get; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <summary>
    /// Gets or sets the partition size.
    /// </summary>
    /// <remarks>Defaults to <see cref="PartitionKey.DefaultPartitionSize"/>.</remarks>
    public int PartitionSize { get => field; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <summary>
    /// Gets or sets the per-worker partition count.
    /// </summary>
    /// <remarks>Defaults to '<c>6</c>'.
    /// <para>Represents the number of partitions that will be relayed per <see cref="TimerHostedServiceBase.OnExecuteAsync(ExecutionContext, CancellationToken)"/>.</para></remarks>
    public int PerWorkerPartitionCount { get; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <summary>
    /// Gets the <see cref="Data.PartitionPicker"/> used to determine partition selection during processing.
    /// </summary>
    /// <remarks>This is instantiated during <see cref="OnInitializeAsync(CancellationToken)"/> and leverages configuration to determine its settings.</remarks>
    protected PartitionPicker PartitionPicker => _partitionPicker ?? throw new InvalidOperationException("PartitionPicker has not yet been initialized; this should not be accessed before the OnInitializeAsync.");

    /// <inheritdoc/>
    protected async override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        await base.OnInitializeAsync(cancellationToken).ConfigureAwait(false);

        BatchSize = Internal.GetConfigurationValueWithFallback<int>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:OutboxRelay:BatchSize", "CoreEx:Host:Services:OutboxRelay:BatchSize", 25, Configuration);
        LeaseDuration = Internal.GetConfigurationValueWithFallback<TimeSpan>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:OutboxRelay:LeaseDuration", "CoreEx:Host:Services:OutboxRelay:LeaseDuration", TimeSpan.FromMinutes(5), Configuration);
        BackOffDuration = Internal.GetConfigurationValueWithFallback<TimeSpan>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:OutboxRelay:BackOffDuration", "CoreEx:Host:Services:OutboxRelay:BackOffDuration", TimeSpan.FromSeconds(5), Configuration);
        PartitionSize = Internal.GetConfigurationValueWithFallback<int>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:OutboxRelay:PartitionSize", "CoreEx:Host:Services:OutboxRelay:PartitionSize", PartitionKey.DefaultPartitionSize, Configuration);
        PerWorkerPartitionCount = Internal.GetConfigurationValueWithFallback<int>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:OutboxRelay:PerWorkerPartitionCount", "CoreEx:Host:Services:OutboxRelay:PerWorkerPartitionCount", 6, Configuration);

        _partitionPicker = new PartitionPicker(PartitionKey.DefaultPartitionSize, PerWorkerPartitionCount);

        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("{ServiceName} settings: BatchSize={BatchSize}, LeaseDuration={LeaseDuration}, BackOffDuration={BackOffDuration}, PartitionSize={PartitionSize}, PerWorkerPartitionCount={PerWorkerPartitionCount}",
                ServiceName, BatchSize, LeaseDuration, BackOffDuration, PartitionSize, PerWorkerPartitionCount);
    }

    /// <inheritdoc/>
    protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data)
    {
        var rd = new Dictionary<string, object>()
        {
            ["BatchSize"] = BatchSize,
            ["LeaseDuration"] = LeaseDuration,
            ["BackOffDuration"] = BackOffDuration,
            ["PartitionSize"] = PartitionSize,
            ["PerWorkerPartitionCount"] = PerWorkerPartitionCount
        };

        data.Add("OutboxRelay", rd);

        return base.OnReportHealthStatus(data);
    }
}