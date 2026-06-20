namespace CoreEx.Azure.Messaging.ServiceBus;

/// <summary>
/// Provides the Azure Service Bus receiver hosted service functionality.
/// </summary>
/// <typeparam name="TReceiver">The <see cref="ServiceBusReceiverBase"/> <see cref="Type"/>.</typeparam>
public sealed class ServiceBusReceiverHostedService<TReceiver> : HostedServiceBase where TReceiver : ServiceBusReceiverBase
{
    private readonly TReceiver _receiver;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerHostedServiceBase"/> class.
    /// </summary>
    /// <param name="receiver">The <see cref="ServiceCollectionServiceExtensions"/>.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public ServiceBusReceiverHostedService(TReceiver receiver, IServiceProvider serviceProvider, ILogger<ServiceBusReceiverHostedService<TReceiver>> logger) : base(serviceProvider, logger)
    {
        _receiver = receiver.ThrowIfNull();
        ArePauseAndResumeSupported = true;
    }

    /// <inheritdoc/>
    protected async override Task<ServiceStatus> OnStartAsync(CancellationToken cancellationToken)
    {
        await _receiver.StartAsync(cancellationToken).ConfigureAwait(false);
        return ServiceStatus.Running;
    }

    /// <inheritdoc/>
    protected override Task OnPauseAsync(CancellationToken cancellationToken)
        => _receiver.PauseAsync($"Hosted service externally paused by '{(ExecutionContext.TryGetCurrent(out var ec) ? ec.User ?? AuthenticationUser.EnvironmentUser : AuthenticationUser.EnvironmentUser)}.", cancellationToken);

    /// <inheritdoc/>
    protected override Task OnResumeAsync(CancellationToken cancellationToken) => _receiver.ResumeAsync(cancellationToken);

    /// <inheritdoc/>
    protected override Task OnStopAsync(CancellationToken cancellationToken) => _receiver.StopAsync(cancellationToken);

    /// <inheritdoc/>
    protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data)
    {
        if (_receiver.StatusReason is not null)
            data.Add("statusReason", _receiver.StatusReason);

        return Status.IsPause
            ? HealthCheckResult.Degraded("Service is in a paused state.", null, data)
            : HealthCheckResult.Healthy(null, data);
    }
}