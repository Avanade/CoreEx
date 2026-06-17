namespace CoreEx.Azure.Messaging.ServiceBus.Abstractions;

/// <summary>
/// Provides the base Azure Service Bus receiver functionality.
/// </summary>
/// <param name="serviceBusClient">The <see cref="ServiceBusClient"/>.</param>
/// <param name="options">The <see cref="ServiceBusReceiverOptionsBase"/>.</param>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
public abstract partial class ServiceBusReceiverBase(ServiceBusClient serviceBusClient, ServiceBusReceiverOptionsBase options, IServiceProvider serviceProvider, ILogger<ServiceBusReceiverBase> logger) : IAsyncDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Gets the <see cref="ServiceBusClient"/>.
    /// </summary>
    protected ServiceBusClient ServiceBusClient { get; } = serviceBusClient.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="ServiceBusReceiverOptionsBase"/>.
    /// </summary>
    public ServiceBusReceiverOptionsBase Options { get; } = options.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/>.
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = serviceProvider.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    public ILogger Logger { get; } = logger.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="ServiceBusReceiverInvoker"/>.
    /// </summary>
    protected ServiceBusReceiverInvoker Invoker { get; } = serviceProvider.ThrowIfNull().GetService<ServiceBusReceiverInvoker>() ?? new();

    /// <summary>
    /// Gets the <see cref="ServiceStatus"/>.
    /// </summary>
    public ServiceStatus Status { get; private set; }

    /// <summary>
    /// Gets the reason for the current <see cref="Status"/> (where applicable).
    /// </summary>
    public string? StatusReason { get; set; }

    /// <summary>
    /// Starts the underlying message processor.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (!Status.CanStart)
                return;

            LogStatusChange(Status = ServiceStatus.Starting);

            await OnStartAsync(cancellationToken).ConfigureAwait(false);

            LogStatusChange(Status = ServiceStatus.Running);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Starts the underlying message processor.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task OnStartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Pauses the underlying message processor.
    /// </summary>
    /// <param name="reason">The reason for the pause.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task PauseAsync(string reason, CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!Status.CanPause)
                return;

            StatusReason = reason;
            LogStatusChange(Status = ServiceStatus.Pausing);

            await OnPauseAsync(cancellationToken).ConfigureAwait(false);

            LogStatusChange(Status = ServiceStatus.Paused);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Pauses the underlying message processor.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task OnPauseAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Resumes the underlying message processor.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!Status.CanResume)
                return;

            LogStatusChange(Status = ServiceStatus.Resuming);

            await OnResumeAsync(cancellationToken).ConfigureAwait(false);

            LogStatusChange(Status = ServiceStatus.Running);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Resumes the underlying message processor.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task OnResumeAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Stops the underlying message processor.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            LogStatusChange(Status = ServiceStatus.Stopping);

            if (!Status.IsInitializing)
                await OnStopAsync(cancellationToken).ConfigureAwait(false);

            LogStatusChange(Status = ServiceStatus.Stopped);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    /// Stops the underlying message processor.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected abstract Task OnStopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Logs the status change.
    /// </summary>
    private void LogStatusChange(ServiceStatus status)
    {
        // Only a pause should have a reason, so clear the reason for any other status.
        if (!status.IsPause)
            StatusReason = null;

        // Log the status change.
        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("Azure Service Bus receiver: {Status}.", status);
    }

    /// <summary>
    /// Encapsulates the standardized processing of the <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
    /// <param name="actions">The <see cref="IServiceBusMessageActions"/> to perform message actions.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected Task ProcessMessageAsync(ServiceBusReceivedMessage message, IServiceBusMessageActions actions, CancellationToken cancellationToken)
        => Invoker.InvokeAsync(this, actions, async (_, _, cancellationToken) =>
        {
            var result = await OnProcessMessageAsync(message, actions, cancellationToken).ConfigureAwait(false);
            MessageProcessed?.Invoke(this, new(result));
            return result;
        }, cancellationToken);

    /// <summary>
    /// Provides the standardized processing of the <paramref name="message"/>.
    /// </summary>
    /// <param name="message">The <see cref="ServiceBusReceivedMessage"/>.</param>
    /// <param name="actions">The <see cref="IServiceBusMessageActions"/> to perform message actions.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/> of the message processing.</returns>
    protected abstract Task<Result> OnProcessMessageAsync(ServiceBusReceivedMessage message, IServiceBusMessageActions actions, CancellationToken cancellationToken);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _semaphore.Dispose();

        _disposed = true;
        await DisposeAsync(true).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// </summary>
    /// <param name="disposing">Indicates whether to dispose of managed resources.</param>
    protected virtual ValueTask DisposeAsync(bool disposing) => ValueTask.CompletedTask;

    /// <summary>
    /// Gets or sets the event that is raised when a message has been processed (either successfully or unsuccessfully).
    /// </summary>
    public event EventHandler<MessageProcessedEventArgs>? MessageProcessed;
}