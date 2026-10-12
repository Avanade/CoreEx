namespace CoreEx.Cosmos.Outbox;

/// <summary>
/// Owns the underlying Cosmos DB <see cref="ChangeFeedProcessor"/> for a single monitored container, providing start/pause/resume/stop lifecycle management and circuit-breaker-protected batch processing via
/// <see cref="CosmosDbOutboxRelayProcessor"/>.
/// </summary>
/// <remarks>The Cosmos DB analogue of Azure Service Bus's <c>ServiceBusReceiverBase</c> - a push/callback-driven, SDK-managed processor, not a poll-on-a-timer loop. The semaphore-guarded start/pause/resume/stop
/// state machine below deliberately mirrors <c>ServiceBusReceiverBase</c>'s (which cannot be shared directly - it lives in <c>CoreEx.Azure.Messaging.ServiceBus</c>, a package this one must not depend on).
/// <para>Constructed with the raw SDK <see cref="Microsoft.Azure.Cosmos.Database"/> rather than <see cref="ICosmosDb"/> deliberately - <see cref="ICosmosDb"/> is registered scoped, and an instance of this class
/// is built once and lives for the process lifetime, so capturing a scoped service here would be a captive-dependency bug. The <see cref="Microsoft.Azure.Cosmos.Database"/> proxy, like a <see cref="Container"/>
/// or <see cref="CosmosClient"/>, is stable and safe to hold long-term.</para>
/// <para>The lease container (<see cref="CosmosDbOutboxRelayOptions.LeaseContainerId"/>, partitioned on <c>/id</c> - the Change Feed Processor's own lease-document convention) is <b>never</b> created by the relay: creating
/// a container is a control-plane operation that a production host identity (e.g. Entra ID data-plane RBAC) is not expected to be permitted. It must be provisioned up front (see <c>CosmosDbProvisionArgs.OutboxLeaseContainer</c>,
/// or infrastructure-as-code); <see cref="StartAsync(CancellationToken)"/> fails fast with an <see cref="InvalidOperationException"/> where it does not exist (reading its metadata is permitted by data-plane roles).
/// Concurrent hosted-service instances (see <see cref="Microsoft.Extensions.Hosting.CoreExCosmosOutboxExtensions.AddCosmosDbOutboxRelayHostedService"/>'s <c>servicesCount</c>) share the same lease container for coordination.</para></remarks>
public sealed class CosmosDbOutboxRelay : IAsyncDisposable
{
#if NET9_0_OR_GREATER
    private readonly Lock _syncLock = new();
#else
    private readonly object _syncLock = new();
#endif
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly CancellationTokenSource _recoveryCts = new();
    private readonly Database _database;
    private ChangeFeedProcessor _processor;
    private int _recovering;
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

        _database = database.ThrowIfNull();
        _processor = BuildProcessor();
    }

    /// <summary>
    /// Builds a new <see cref="ChangeFeedProcessor"/> from the <see cref="Options"/>.
    /// </summary>
    private ChangeFeedProcessor BuildProcessor()
    {
        var builder = _database.GetContainer(Options.ContainerId).GetChangeFeedProcessorBuilder<CosmosDbOutboxEvent>($"outbox-relay-{Options.ContainerId}", OnChangesAsync)
            .WithInstanceName(Options.InstanceName)
            .WithLeaseContainer(_database.GetContainer(Options.LeaseContainerId))
            .WithErrorNotification(OnErrorNotificationAsync);

        if (Options.PollInterval is not null)
            builder = builder.WithPollInterval(Options.PollInterval.Value);

        if (Options.BatchSize is not null)
            builder = builder.WithMaxItems(Options.BatchSize.Value);

        // The SDK defaults fresh leases to "now", which would skip an existing outbox backlog.
        builder = builder.WithStartTime(Options.StartTime ?? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc));

        return builder.Build();
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
    /// <exception cref="InvalidOperationException">The <see cref="CosmosDbOutboxRelayOptions.LeaseContainerId"/> container does not exist - see this type's own remarks.</exception>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!Status.CanStart)
                return;

            // SDK background tasks inherit this scope; publishing explicitly restores instrumentation in the batch processor.
            using var instrumentation = SuppressInstrumentationScope.Begin(!Options.IsInstrumentationEnabledForPolling);
            await EnsureLeaseContainerExistsAsync(cancellationToken).ConfigureAwait(false);

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
    /// Verifies that the lease container exists (a metadata read; the relay never creates it).
    /// </summary>
    private async Task EnsureLeaseContainerExistsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _database.GetContainer(Options.LeaseContainerId).ReadContainerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"The outbox relay lease container '{Options.LeaseContainerId}' does not exist in database '{_database.Id}'; the relay never creates it. " +
                $"Provision it up front (partition key '/id') - e.g. 'CosmosDbProvisionArgs.OutboxLeaseContainer()' in the Database project, or infrastructure-as-code.", cex);
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

            using var instrumentation = SuppressInstrumentationScope.Begin(!Options.IsInstrumentationEnabledForPolling);
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

            using var instrumentation = SuppressInstrumentationScope.Begin(!Options.IsInstrumentationEnabledForPolling);
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
            using var instrumentation = SuppressInstrumentationScope.Begin(!Options.IsInstrumentationEnabledForPolling);
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
    /// <remarks>Where the error indicates the monitored and/or lease container no longer exists (a <see cref="HttpStatusCode.NotFound"/> anywhere in the exception chain - e.g. a provisioning reset deleted and
    /// recreated the containers underneath the running relay), the processor is bound to the deleted container resource identifiers and would never recover on its own; a background recovery is therefore
    /// triggered (see <see cref="RecoverAsync"/>).</remarks>
    private Task OnErrorNotificationAsync(string leaseToken, Exception error)
    {
        if (Logger.IsEnabled(LogLevel.Warning))
            Logger.LogWarning(error, "Cosmos DB change feed processor error for container '{ContainerId}', lease '{LeaseToken}': {Error}", Options.ContainerId, leaseToken, error.Message);

        // Never stop/rebuild the processor from within its own callback; recover on a separate task (at most one at a time).
        if (IsContainerNotFound(error) && !_recoveryCts.IsCancellationRequested && Interlocked.CompareExchange(ref _recovering, 1, 0) == 0)
            _ = Task.Run(RecoverAsync);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines whether the <paramref name="error"/> chain contains a <see cref="CosmosException"/> with a <see cref="HttpStatusCode.NotFound"/> status code.
    /// </summary>
    private static bool IsContainerNotFound(Exception? error)
    {
        while (error is not null)
        {
            if (error is CosmosException { StatusCode: HttpStatusCode.NotFound })
                return true;

            if (error is AggregateException aex && aex.InnerExceptions.Any(IsContainerNotFound))
                return true;

            error = error.InnerException;
        }

        return false;
    }

    /// <summary>
    /// Recovers from the monitored and/or lease container having been deleted and recreated by stopping the existing (stale) <see cref="ChangeFeedProcessor"/> and starting a newly built one; retried (after
    /// <see cref="CosmosDbOutboxRelayOptions.RecoveryDelay"/>) until successful, or until the relay is no longer running or is stopped.
    /// </summary>
    /// <remarks>The new processor starts against a new (empty) lease set and as such (unless <see cref="CosmosDbOutboxRelayOptions.StartTime"/> is specified) reads the change feed from the beginning; this is
    /// safe as the latest-version change feed only returns items that still exist, and relayed outbox documents are deleted (at-least-once delivery semantics are unaffected).</remarks>
    private async Task RecoverAsync()
    {
        var cancellationToken = _recoveryCts.Token;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(Options.RecoveryDelay, cancellationToken).ConfigureAwait(false);

                // Acquire per attempt (never across the delay) so a concurrent stop/dispose is not blocked.
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    if (Status != ServiceStatus.Running)
                        return;

                    using var instrumentation = SuppressInstrumentationScope.Begin(!Options.IsInstrumentationEnabledForPolling);

                    try
                    {
                        await _processor.StopAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (!ex.IsCanceledBy(cancellationToken))
                    {
                        if (Logger.IsEnabled(LogLevel.Debug))
                            Logger.LogDebug(ex, "Cosmos DB outbox relay for container '{ContainerId}': stopping the stale change feed processor failed (ignored): {Error}", Options.ContainerId, ex.Message);
                    }

                    try
                    {
                        await EnsureLeaseContainerExistsAsync(cancellationToken).ConfigureAwait(false);
                        await _database.GetContainer(Options.ContainerId).ReadContainerAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

                        var processor = BuildProcessor();
                        await processor.StartAsync().ConfigureAwait(false);
                        _processor = processor;

                        if (Logger.IsEnabled(LogLevel.Warning))
                            Logger.LogWarning("Cosmos DB outbox relay for container '{ContainerId}' recovered: the monitored and/or lease container was recreated whilst running; the change feed processor has been restarted.", Options.ContainerId);

                        return;
                    }
                    catch (Exception ex) when (!ex.IsCanceledBy(cancellationToken))
                    {
                        if (Logger.IsEnabled(LogLevel.Warning))
                            Logger.LogWarning(ex, "Cosmos DB outbox relay for container '{ContainerId}' recovery failed; will retry in {RecoveryDelay}: {Error}", Options.ContainerId, Options.RecoveryDelay, ex.Message);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }
        catch (Exception ex) when (ex.IsCanceledBy(cancellationToken)) { }
        catch (ObjectDisposedException) { }
        finally
        {
            Interlocked.Exchange(ref _recovering, 0);
        }
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
    /// <remarks>Stops the underlying <see cref="ChangeFeedProcessor"/> (via <see cref="StopAsync(CancellationToken)"/>) before releasing synchronization resources - disposing a started relay without an
    /// explicit preceding <see cref="StopAsync(CancellationToken)"/> would otherwise leave the Change Feed Processor running, with its callbacks racing against the disposed semaphore. Idempotent -
    /// safe to call more than once (only the first call performs any work), and safe to call even where the relay was never started (<see cref="StopAsync(CancellationToken)"/> itself tolerates that,
    /// skipping the processor call while still transitioning <see cref="Status"/>).</remarks>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _recoveryCts.Cancel();
        await StopAsync().ConfigureAwait(false);
        _semaphore.Dispose();
        _recoveryCts.Dispose();
        GC.SuppressFinalize(this);
    }
}
