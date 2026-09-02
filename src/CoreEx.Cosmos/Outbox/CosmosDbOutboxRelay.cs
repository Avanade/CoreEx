namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Owns the underlying Cosmos DB <see cref="ChangeFeedProcessor"/> for a single monitored container, providing start/pause/resume/stop lifecycle management and circuit-breaker-protected batch processing via
/// <see cref="CosmosDbOutboxRelayProcessor"/>.
/// </summary>
/// <remarks>The Cosmos DB analogue of Azure Service Bus's <c>ServiceBusReceiverBase</c> - a push/callback-driven, SDK-managed processor, not a poll-on-a-timer loop. The semaphore-guarded start/pause/resume/stop
/// state machine below deliberately mirrors <c>ServiceBusReceiverBase</c>'s (which cannot be shared directly - it lives in <c>CoreEx.Azure.Messaging.ServiceBus</c>, a package this one must not depend on).
/// <para>Constructed with the raw SDK <see cref="Microsoft.Azure.Cosmos.Database"/> rather than <see cref="ICosmosDb"/> deliberately - <see cref="ICosmosDb"/> is registered scoped, and an instance of this class
/// is built once and lives for the process lifetime, so capturing a scoped service here would be a captive-dependency bug. The <see cref="Microsoft.Azure.Cosmos.Database"/> proxy, like a <see cref="Container"/>
/// or <see cref="CosmosClient"/>, is stable and safe to hold long-term.</para></remarks>
public sealed class CosmosDbOutboxRelay : IAsyncDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _syncLock = new();
#else
    private readonly object _syncLock = new();
#endif
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ChangeFeedProcessor _processor;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CosmosDbOutboxRelay"/> class.
    /// </summary>
    /// <param name="database">The <see cref="Microsoft.Azure.Cosmos.Database"/>.</param>
    /// <param name="options">The <see cref="CosmosDbOutboxRelayOptions"/>.</param>
    /// <param name="processor">The <see cref="CosmosDbOutboxRelayProcessor"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public CosmosDbOutboxRelay(Database database, CosmosDbOutboxRelayOptions options, CosmosDbOutboxRelayProcessor processor, ILogger<CosmosDbOutboxRelay> logger)
    {
        Options = options.ThrowIfNull();
        Processor = processor.ThrowIfNull();
        Logger = logger.ThrowIfNull();
        Resiliency = options.Resiliency ?? CosmosDbOutboxRelayResiliency.CreateRelayCircuitBreakerResiliency();

        database.ThrowIfNull();
        var container = database.GetContainer(options.ContainerId);
        var leaseContainer = database.GetContainer(options.LeaseContainerId);

        var builder = container.GetChangeFeedProcessorBuilder<CosmosDbOutboxEvent>($"outbox-relay-{options.ContainerId}", OnChangesAsync)
            .WithInstanceName(options.InstanceName)
            .WithLeaseContainer(leaseContainer)
            .WithErrorNotification(OnErrorNotificationAsync);

        if (options.PollInterval is not null)
            builder = builder.WithPollInterval(options.PollInterval.Value);

        if (options.BatchSize is not null)
            builder = builder.WithMaxItems(options.BatchSize.Value);

        if (options.StartTime is not null)
            builder = builder.WithStartTime(options.StartTime.Value);

        _processor = builder.Build();
    }

    /// <summary>
    /// Gets the <see cref="CosmosDbOutboxRelayOptions"/>.
    /// </summary>
    public CosmosDbOutboxRelayOptions Options { get; }

    /// <summary>
    /// Gets the <see cref="CosmosDbOutboxRelayProcessor"/>.
    /// </summary>
    public CosmosDbOutboxRelayProcessor Processor { get; }

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    public ILogger Logger { get; }

    /// <summary>
    /// Gets the <see cref="ResiliencePipeline{T}"/> used to protect <see cref="Processor"/> batch execution with a self-pausing/self-resuming circuit breaker.
    /// </summary>
    public ResiliencePipeline<Result> Resiliency { get; }

    /// <summary>
    /// Gets the <see cref="ServiceStatus"/>.
    /// </summary>
    public ServiceStatus Status { get; private set; }

    /// <summary>
    /// Gets or sets the reason for the current <see cref="Status"/> (where applicable, e.g. a pause).
    /// </summary>
    public string? StatusReason { get; set; }

    /// <summary>
    /// Starts the underlying <see cref="ChangeFeedProcessor"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!Status.CanStart)
                return;

            LogStatusChange(Status = ServiceStatus.Starting);
            await _processor.StartAsync().ConfigureAwait(false);
            LogStatusChange(Status = ServiceStatus.Running);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Pauses the underlying <see cref="ChangeFeedProcessor"/> (via <see cref="ChangeFeedProcessor.StopAsync"/> - there is no dedicated pause API; validated empirically that stopping then later starting the
    /// same processor instance resumes correctly from its last checkpoint).
    /// </summary>
    /// <param name="reason">The reason for the pause.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task PauseAsync(string reason, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!Status.CanPause)
                return;

            StatusReason = reason;
            LogStatusChange(Status = ServiceStatus.Pausing);
            await _processor.StopAsync().ConfigureAwait(false);
            LogStatusChange(Status = ServiceStatus.Paused);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Resumes the underlying <see cref="ChangeFeedProcessor"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!Status.CanResume)
                return;

            LogStatusChange(Status = ServiceStatus.Resuming);
            await _processor.StartAsync().ConfigureAwait(false);
            LogStatusChange(Status = ServiceStatus.Running);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Stops the underlying <see cref="ChangeFeedProcessor"/>.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var wasInitializing = Status.IsInitializing;
            LogStatusChange(Status = ServiceStatus.Stopping);

            if (!wasInitializing)
                await _processor.StopAsync().ConfigureAwait(false);

            LogStatusChange(Status = ServiceStatus.Stopped);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Handles a batch of changes delivered by the <see cref="ChangeFeedProcessor"/>, executing <see cref="Processor"/> through <see cref="Resiliency"/> and rethrowing on failure so the Change Feed
    /// Processor's own native redelivery/backoff continues to apply on top of whatever the circuit breaker decides.
    /// </summary>
    private async Task OnChangesAsync(IReadOnlyCollection<CosmosDbOutboxEvent> changes, CancellationToken cancellationToken)
    {
        var ctx = ResilienceContextPool.Shared.Get(cancellationToken);
        try
        {
            ctx.Properties.Set(ResilienceOwner<CosmosDbOutboxRelay>.PropertyKey, this);

            var result = await Resiliency.ExecuteAsync(static async (rc, state) =>
            {
                try
                {
                    await state.relay.Processor.ProcessBatchAsync(state.changes, rc.CancellationToken).ConfigureAwait(false);
                    return Result.Success;
                }
                catch (Exception ex)
                {
                    return Result.Fail(ex);
                }
            }, ctx, (relay: this, changes)).ConfigureAwait(false);

            result.ThrowOnError();
        }
        finally
        {
            ResilienceContextPool.Shared.Return(ctx);
        }
    }

    /// <summary>
    /// Handles a Change Feed Processor infrastructure-level error notification (e.g. lease acquisition issues) - distinct from a <see cref="Processor"/> batch exception, which is handled by <see cref="OnChangesAsync"/>.
    /// </summary>
    private Task OnErrorNotificationAsync(string leaseToken, Exception error)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
            Logger.LogWarning(error, "Cosmos DB change feed processor error for container '{ContainerId}', lease '{LeaseToken}': {Error}", Options.ContainerId, leaseToken, error.Message);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Logs the status change.
    /// </summary>
    private void LogStatusChange(ServiceStatus status)
    {
        lock (_syncLock)
        {
            if (!status.IsPause)
                StatusReason = null;
        }

        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Cosmos DB outbox relay for container '{ContainerId}': {Status}.", Options.ContainerId, status);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _semaphore.Dispose();
        await Task.CompletedTask.ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
