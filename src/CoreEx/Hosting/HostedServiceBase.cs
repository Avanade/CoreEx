namespace CoreEx.Hosting;

/// <summary>
/// Represents a base class for an <see cref="IHostedService"/>.
/// </summary>
/// <remarks>Provides common properties and methods for hosted services including status reporting and health check integration.
/// <para>The <see cref="OnInitializeAsync(CancellationToken)"/> should be used to initialize the service, such as reading configuration settings, etc. versus via the constructor.</para></remarks>
public abstract class HostedServiceBase : IHostedService, IDisposable
{
    private string? _serviceConfigurationSectionName;
    private string? _serviceName;
    private HostedServiceHealthCheck? _healthCheck;
    private ServiceStatus _status = ServiceStatus.Initializing;
    private int _disposed;

    /// <summary>
    /// Gets the argument for specifying that the <see cref="HostedServiceBase"/> should execute as a no-op (i.e. no operation); i.e. do nothing.
    /// </summary>
    /// <remarks>This is to support the likes of testing where the underlying hosted services should not execute.</remarks>
    public const string NoOpArgument = "--no-op-hosted-services";

    /// <summary>
    /// Gets the <see cref="HostedServiceInvoker"/> to be used for all hosted services.
    /// </summary>
    protected static HostedServiceInvoker HostedServiceInvoker { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="HostedServiceBase"/> class.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public HostedServiceBase(IServiceProvider serviceProvider, ILogger logger)
    {
        ServiceProvider = serviceProvider.ThrowIfNull();
        Logger = logger.ThrowIfNull();
        Configuration = ServiceProvider.GetService<IConfiguration>() ?? throw new InvalidOperationException($"No {nameof(IConfiguration)} available in the service provider.");
    }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/>.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the <see cref="IConfiguration"/>.
    /// </summary>
    protected IConfiguration Configuration { get; }

    /// <summary>
    /// Gets the <see cref="ILogger"/>.
    /// </summary>
    protected ILogger Logger { get; }

    /// <summary>
    /// Gets ot sets the service configuration section name (used for configuration lookup).
    /// </summary>
    /// <remarks>Defaults to the <see cref="Type"/> <see cref="MemberInfo.Name"/>.</remarks>
    public string ServiceConfigurationSectionName
    {
        get => _serviceConfigurationSectionName ??= GetType().Name;
        set => _serviceConfigurationSectionName = SetValueWhenStatusIsInitializedOnly(value.ThrowIfEmpty());
    }

    /// <summary>
    /// Gets or sets the service name (used for logging).
    /// </summary>
    /// <remarks>Defaults to the <see cref="Type"/> <see cref="MemberInfo.Name"/>.</remarks>
    public string ServiceName
    {
        get => _serviceName ??= GetType().Name;
        set => _serviceName = SetValueWhenStatusIsInitializedOnly(value.ThrowIfEmpty());
    }

    /// <summary>
    /// Gets or sets the optional <see cref="HostedServiceHealthCheck"/> to report health; where set the health status will be reported on each <see cref="Status"/> change.
    /// </summary>
    public HostedServiceHealthCheck? HealthCheck { get => _healthCheck; set => _healthCheck = SetValueWhenStatusIsInitializedOnly(value); }

    /// <summary>
    /// Get or sets an optional tag that can be used to store additional information about the service instance; e.g. assigning a tenant identifier where applicable.
    /// </summary>
    public object? Tag { get; set; }

    /// <summary>
    /// Gets the synchronization lock.
    /// </summary>
#if NET9_0_OR_GREATER
    protected Lock SyncLock { get; } = new();
#else
    protected object SyncLock { get; } = new();
#endif

    /// <summary>
    /// Indicates whether <see cref="PauseAsync(CancellationToken)"/> and <see cref="ResumeAsync(CancellationToken)"/> are supported.
    /// </summary>
    public bool ArePauseAndResumeSupported { get; protected set; } = false;

    /// <summary>
    /// Gets the current <see cref="ServiceStatus"/>.
    /// </summary>
    /// <remarks>The <see cref="Status"/> should always be updated within a <see cref="SyncLock"/> to ensure thread safety.</remarks>
    public ServiceStatus Status
    {
        get => _status;
        protected set => _status = ReportHealthStatus(value);
    }

    /// <summary>
    /// Reports the <see cref="HealthCheckResult"/> on status change.
    /// </summary>
    /// <param name="status">The <see cref="ServiceStatus"/>.</param>
    /// <returns>The <paramref name="status"/>.</returns>
    private ServiceStatus ReportHealthStatus(ServiceStatus status)
    {
        if (_healthCheck is not null)
        {
            var data = new Dictionary<string, object>
                {
                    { "service", ServiceName },
                    { "status", status.ToString() }
                };

            _healthCheck.Result = OnReportHealthStatus(data);
        }

        return status;
    }

    /// <summary>
    /// Sets the value when the <see cref="Status"/> is <see cref="ServiceStatus.Initializing"/> only; otherwise, throws an <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>The value to support fluent-style method-chaining.</returns>
    protected T SetValueWhenStatusIsInitializedOnly<T>(T value)
    {
        if (!Status.IsInitializing)
            throw new InvalidOperationException($"Cannot set value when status is {Status}.");

        return value;
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Abstractions.Internal.TryGetConfigurationValue<bool?>(NoOpArgument, out var nop, Configuration) && (nop is null || nop.Value))
        {
            lock (SyncLock)
            {
                Status = ServiceStatus.NoOp;
                if (Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("{ServiceName} no-op.", ServiceName);
            }

            return;
        }

        lock (SyncLock)
        {
            Status = ServiceStatus.Initializing;
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("{ServiceName} initializing.", ServiceName);
        }

        await OnInitializeAsync(cancellationToken).ConfigureAwait(false);

        lock (SyncLock)
        {
            Status = ServiceStatus.Starting;
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("{ServiceName} starting.", ServiceName);
        }

        var status = await OnStartAsync(cancellationToken).ConfigureAwait(false);

        lock (SyncLock)
        {
            if (Status == ServiceStatus.Starting)
                Status = status;

            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("{ServiceName} started.", ServiceName);
        }
    }

    /// <summary>
    /// The hosted service is being initialized.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>This is called prior to the <see cref="OnStartAsync(CancellationToken)"/> and provides an opportunity to perform any initialization logic.
    /// <para><b>Note:</b> where overriding invoke the base <see cref="OnInitializeAsync(CancellationToken)"/> first to ensure initialization occurs in the correct sequence. Failing to invoke the base
    /// will likely result in unintended side-effects/errors.</para></remarks>
    protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// A hosted service start has been requested.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="ServiceStatus"/> as a result of the start; typically either <see cref="ServiceStatus.Running"/> or <see cref="ServiceStatus.Sleeping"/>.</returns>
    protected abstract Task<ServiceStatus> OnStartAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Pauses the hosted service.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Pausing will only be performed where the current <see cref="Status"/> is either <see cref="ServiceStatus.Running"/> or <see cref="ServiceStatus.Sleeping"/>.</remarks>
    public async Task PauseAsync(CancellationToken cancellationToken)
    {
        if (!ArePauseAndResumeSupported)
            throw new NotSupportedException($"{ServiceName} does not support pausing and resuming.");

        lock (SyncLock)
        {
            if (!Status.CanPause)
                return;

            Status = ServiceStatus.Pausing;
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("{ServiceName} pausing.", ServiceName);
        }

        await OnPauseAsync(cancellationToken).ConfigureAwait(false);

        lock (SyncLock)
        {
            if (Status == ServiceStatus.Pausing)
            {
                Status = ServiceStatus.Paused;
                if (Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("{ServiceName} paused.", ServiceName);
            }
        }
    }

    /// <summary>
    /// A hosted service pause has been requested.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>The <see cref="Status"/> will automatically be set to <see cref="ServiceStatus.Pausing"/> prior to execution. It will then be automatically set to <see cref="ServiceStatus.Paused"/> post execution
    /// where the <see cref="Status"/> is still <see cref="ServiceStatus.Pausing"/>.</remarks>
    protected virtual Task OnPauseAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"Where pause and resume are supported then the {nameof(OnPauseAsync)} method must be implemented.");

    /// <summary>
    /// Initiates a <see cref="PauseAsync(CancellationToken)"/> without waiting for completion (fire-and-forget).
    /// </summary>
    /// <remarks>Use this method only when calling from likes of API endpoints or in loops where you need an immediate return.</remarks>
    public void Pause() => _ = Task.Run(async () =>
    {
        try
        {
            await PauseAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(ex, "{ServiceName} pause (fire-and-forget) failed: {Error}", ServiceName, ex.Message);
        }
    });

    /// <summary>
    /// Resumes the hosted service.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Resuming will only be performed where the current <see cref="Status"/> is <see cref="ServiceStatus.Paused"/>.
    /// <para>The <see cref="OnResumeAsync(CancellationToken)"/> is responsible for updating the <see cref="Status"/>.</para></remarks>
    public async Task ResumeAsync(CancellationToken cancellationToken)
    {
        if (!ArePauseAndResumeSupported)
            throw new NotSupportedException($"{ServiceName} does not support pausing and resuming.");

        lock (SyncLock)
        { 
            if (!Status.CanResume)
                return;

            Status = ServiceStatus.Resuming;
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("{ServiceName} resuming.", ServiceName);
        }

        await OnResumeAsync(cancellationToken).ConfigureAwait(false);

        lock (SyncLock)
        {
            if (Status == ServiceStatus.Resuming)
            {
                Status = ServiceStatus.Running;

                if (Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("{ServiceName} resumed.", ServiceName);
            }
        }
    }

    /// <summary>
    /// A hosted service resume has been requested.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>The <see cref="Status"/> will automatically be set to <see cref="ServiceStatus.Resuming"/> prior to execution. It will then be automatically set to <see cref="ServiceStatus.Running"/> post execution
    /// where the <see cref="Status"/> is still <see cref="ServiceStatus.Resuming"/>.</remarks>
    protected virtual Task OnResumeAsync(CancellationToken cancellationToken) => throw new NotImplementedException($"Where pause and resume are supported then the {nameof(OnResumeAsync)} method must be implemented.");

    /// <summary>
    /// Initiates a <see cref="ResumeAsync(CancellationToken)"/> without waiting for completion (fire-and-forget).
    /// </summary>
    /// <remarks>Use this method only when calling from likes of API endpoints or in loops where you need an immediate return.</remarks>
    public void Resume() => _ = Task.Run(async () =>
    {
        try
        {
            await ResumeAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (Logger.IsEnabled(LogLevel.Error))
                Logger.LogError(ex, "{ServiceName} resume (fire-and-forget) failed: {Error}", ServiceName, ex.Message);
        }
    });

    /// <inheritdoc/>
    /// <remarks>A stop will always be executed regardless of current <see cref="Status"/>.</remarks>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (SyncLock)
        {
            if (Status.IsStop)

            Status = ServiceStatus.Stopping;
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("{ServiceName} stop requested.", ServiceName);
        }

        await OnStopAsync(cancellationToken);

        lock (SyncLock)
        {
            Status = ServiceStatus.Stopped;
            if (Logger.IsEnabled(LogLevel.Information))
                Logger.LogInformation("{ServiceName} stopped.", ServiceName);
        }
    }

    /// <summary>
    /// A hosted service stop has been requested.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>The <see cref="Status"/> will automatically be set to <see cref="ServiceStatus.Stopping"/> prior to execution. It will then be automatically set to <see cref="ServiceStatus.Stopped"/> post execution.</remarks>
    protected abstract Task OnStopAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Provides an opportunity to override the health status reporting.
    /// </summary>
    /// <param name="data">The status data.</param>
    /// <returns>The <see cref="HealthCheckResult"/>.</returns>
    protected abstract HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data);

    /// <summary>
    /// Dispose of resources.
    /// </summary>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            lock (SyncLock)
            {
                Status = ServiceStatus.Stopped;
            }

            Dispose(true);
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="TimerHostedServiceBase"/> and optionally releases the managed resources.
    /// </summary>
    /// <param name="disposing"><see langword="true"/> to release both managed and unmanaged resources; <see langword="false"/> to release only unmanaged resources.</param>
    protected virtual void Dispose(bool disposing) { }
}