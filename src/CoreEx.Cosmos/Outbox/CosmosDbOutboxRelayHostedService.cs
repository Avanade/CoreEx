namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Provides the <see cref="CosmosDbOutboxRelay"/> execution leveraging an underlying <see cref="HostedServiceBase"/>.
/// </summary>
/// <remarks>A thin wrapper only - all the actual Change Feed Processor/circuit-breaker logic lives in <see cref="CosmosDbOutboxRelay"/>, exactly mirroring how <c>ServiceBusReceiverHostedService{TReceiver}</c>
/// delegates to <c>ServiceBusReceiverBase</c>.</remarks>
public sealed class CosmosDbOutboxRelayHostedService : HostedServiceBase
{
    private readonly CosmosDbOutboxRelay _relay;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbOutboxRelayHostedService"/> class.
    /// </summary>
    /// <param name="relay">The <see cref="CosmosDbOutboxRelay"/>.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public CosmosDbOutboxRelayHostedService(CosmosDbOutboxRelay relay, IServiceProvider serviceProvider, ILogger<CosmosDbOutboxRelayHostedService> logger) : base(serviceProvider, logger)
    {
        _relay = relay.ThrowIfNull();
        ArePauseAndResumeSupported = true;
    }

    /// <inheritdoc/>
    protected override async Task<ServiceStatus> OnStartAsync(CancellationToken cancellationToken)
    {
        await _relay.StartAsync(cancellationToken).ConfigureAwait(false);
        return ServiceStatus.Running;
    }

    /// <inheritdoc/>
    protected override Task OnPauseAsync(CancellationToken cancellationToken) => _relay.PauseAsync("Hosted service externally paused.", cancellationToken);

    /// <inheritdoc/>
    protected override Task OnResumeAsync(CancellationToken cancellationToken) => _relay.ResumeAsync(cancellationToken);

    /// <inheritdoc/>
    protected override Task OnStopAsync(CancellationToken cancellationToken) => _relay.StopAsync(cancellationToken);

    /// <inheritdoc/>
    protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data)
    {
        if (_relay.StatusReason is not null)
            data.Add("statusReason", _relay.StatusReason);

        return Status.IsPause ? HealthCheckResult.Degraded("Service is in a paused state.", null, data) : HealthCheckResult.Healthy(null, data);
    }
}
