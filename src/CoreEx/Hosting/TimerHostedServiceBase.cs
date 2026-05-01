namespace CoreEx.Hosting;

/// <summary>
/// Represents a base class for an <see cref="IHostedService"/> based on an <see cref="Interval"/> to <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> work.
/// </summary>
/// <remarks>Each timer-based invocation of the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI)
/// <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see>.
/// <para>A <see cref="OneOffIntervalAdjust(TimeSpan)"/> is provided to enable a one-off change to the timer where required.</para></remarks>
public abstract class TimerHostedServiceBase : HostedServiceBase
{
    private readonly SemaphoreSlim _signal = new(0);
    private Task? _backgroundTask;
    private CancellationTokenSource? _backgroundCts;
    private CancellationTokenSource? _delayCts;
    private TimeSpan? _oneOffInterval;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimerHostedServiceBase"/> class.
    /// </summary>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public TimerHostedServiceBase(IServiceProvider serviceProvider, ILogger logger) : base(serviceProvider, logger) => ArePauseAndResumeSupported = true;

    /// <summary>
    /// Gets or sets the action to configure the <see cref="ExecutionContext"/> prior to executing the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> method.
    /// </summary>
    public Action<ExecutionContext>? ExecutionContextConfigure { get; set; }

    /// <summary>
    /// Gets or sets the <i>first</i> timer start interval. 
    /// </summary>
    /// <remarks>Defaults to <see cref="Interval"/>. This is used as a maximum, in that the actual start is determined using a random value up to this value to ensure staggering of execution where multiple hosts are triggered at the same time.</remarks>
    public TimeSpan FirstInterval { get => field; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <summary>
    /// Gets or sets the timer interval <see cref="TimeSpan"/>.
    /// </summary>
    /// <remarks>Defaults to 500 milliseconds.</remarks>
    public TimeSpan Interval { get => field; set => field = SetValueWhenStatusIsInitializedOnly(value); } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Gets or sets the timer start interval after an unhandled <see cref="Exception"/> that occurs during the execution where <see cref="PauseOnUnhandledException"/> is <see langword="false"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="Interval"/>.</remarks>
    public TimeSpan OnUnhandledInterval { get => field; set => field = SetValueWhenStatusIsInitializedOnly(value); }

    /// <summary>
    /// Indicates whether to automatically halt the service on an unhandled <see cref="Exception"/> that occurs during the execution of the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> method.
    /// </summary>
    /// <returns><see langword="true"/> indicates that the service should be <see cref="ServiceStatus.Paused"/>; otherwise, <see langword="false"/> indicates to continue executing after the next interval.</returns>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool PauseOnUnhandledException { get => field; set => field = SetValueWhenStatusIsInitializedOnly(value); } = true;

    /// <summary>
    /// Gets or sets the maximum number of consecutive immediate executions (i.e. without an interval) before forcing a sleep interval.
    /// </summary>
    /// <remarks>Defaults to 100. This is a safety mechanism to prevent runaway execution where the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> method continually returns <see langword="true"/> indicating to execute immediately without an interval.</remarks>
    public int MaxConsecutiveExecutions { get => field; set => field = SetValueWhenStatusIsInitializedOnly(value.ThrowIfLessThanOrEqualToZero()); } = 100;

    /// <summary>
    /// Gets the last execution <see cref="DateTimeOffset"/>.
    /// </summary>
    public DateTimeOffset LastExecuted { get; protected set; } = DateTimeOffset.MinValue;

    /// <summary>
    /// Gets the last execution <see cref="Exception"/>; <see langword="null"/> indicates success.
    /// </summary>
    public Exception? LastException { get; protected set; }

    /// <summary>
    /// Provides an opportunity to explicitly handle an unhandled <see cref="Exception"/> that occurs during the execution of the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> method.
    /// </summary>
    /// <param name="exception">The unhandled <see cref="Exception"/>.</param>
    /// <returns><see langword="true"/> indicates that the service should be <see cref="ServiceStatus.Paused"/>; otherwise, <see langword="false"/> indicates to continue executing after the next interval.</returns>
    /// <remarks>The <paramref name="exception"/> is automatically logged prior to invoking this method.
    /// <para>The default result is the <see cref="PauseOnUnhandledException"/>.</para>
    /// <para>Also, consider using the <see cref="OneOffTrigger(TimeSpan?)"/> to adjust the next interval where applicable.</para></remarks>
    protected virtual bool OnUnhandledException(Exception exception) => PauseOnUnhandledException;

    /// <inheritdoc/>
    protected async override Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        // Execute the base initialization to ensure potential dependencies are available.
        await base.OnInitializeAsync(cancellationToken).ConfigureAwait(false);

        // Helper to get the default-value where the provided value is not valid (i.e. less than or equal to zero); otherwise, returns the provided value.
        static TimeSpan GetDefault(TimeSpan value, TimeSpan defaultValue) => value <= TimeSpan.Zero ? defaultValue : value;

        // Get the configuration-based settings.
        Interval = Internal.GetConfigurationValueWithFallback<TimeSpan>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:Interval", "CoreEx:Host:Services:Interval", Interval, Configuration).ThrowWhen(interval => interval <= TimeSpan.Zero, nameof(Interval), "Interval must be positive.");
        FirstInterval = Internal.GetConfigurationValueWithFallback<TimeSpan>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:FirstInterval", "CoreEx:Host:Services:FirstInterval", GetDefault(FirstInterval, Interval), Configuration).ThrowWhen(firstInterval => firstInterval <= TimeSpan.Zero, nameof(FirstInterval), "FirstInterval must be positive.");
        OnUnhandledInterval = Internal.GetConfigurationValueWithFallback<TimeSpan>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:OnUnhandledInterval", "CoreEx:Host:Services:OnUnhandledInterval", GetDefault(OnUnhandledInterval, Interval), Configuration).ThrowWhen(onUnhandledInterval => onUnhandledInterval <= TimeSpan.Zero, nameof(OnUnhandledInterval), "OnUnhandledInterval must be positive.");
        PauseOnUnhandledException = Internal.GetConfigurationValueWithFallback<bool>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:PauseOnUnhandledException", "CoreEx:Host:Services:PauseOnUnhandledException", PauseOnUnhandledException, Configuration);
        MaxConsecutiveExecutions = Internal.GetConfigurationValueWithFallback<int>($"CoreEx:Host:Services:{ServiceConfigurationSectionName}:MaxConsecutiveExecutions", "CoreEx:Host:Services:MaxConsecutiveExecutions", MaxConsecutiveExecutions, Configuration).ThrowWhen(max => max <= 0, nameof(MaxConsecutiveExecutions), "MaxConsecutiveExecutions must be positive.");

        if (Logger.IsEnabled(LogLevel.Information))
            Logger.LogInformation("{ServiceName} settings: Interval={Interval}, FirstInterval={FirstInterval}, OnUnhandledInterval={OnUnhandledInterval}, PauseOnUnhandledException={PauseOnUnhandledException}, MaxConsecutiveExecutions={MaxConsecutiveExecutions}",
                ServiceName, Interval, FirstInterval, OnUnhandledInterval, PauseOnUnhandledException, MaxConsecutiveExecutions);
    }

    /// <inheritdoc/>
    protected sealed override async Task<ServiceStatus> OnStartAsync(CancellationToken cancellationToken)
    {
        if (Logger.IsEnabled(LogLevel.Debug))
            Logger.LogDebug("{ServiceName} starting. Timer first/interval {FirstInterval}/{Interval}.", ServiceName, FirstInterval, Interval);

        _backgroundCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await OnStartingAsync(_backgroundCts.Token).ConfigureAwait(false);

        // Start the background loop.
        _backgroundTask = Task.Run(async () =>
        {
            try
            {
                await RunPeriodicAsync(_backgroundCts.Token).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCanceled())
            {
                if (Logger.IsEnabled(LogLevel.Critical))
                    Logger.LogCritical(ex, "{ServiceName} background task failed unexpectedly.", ServiceName);
            }
        }, _backgroundCts.Token);

        // And, ... sleep.
        return ServiceStatus.Sleeping;
    }

    /// <summary>
    /// Triggered when the <see cref="TimerHostedServiceBase"/> is starting (prior to initiating the timer).
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected virtual Task OnStartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Performs the asynchronous background loop.
    /// </summary>
    private async Task RunPeriodicAsync(CancellationToken cancellationToken)
    {
        // Initial staggered (pseudo jitter) delay.
        var maxMs = Math.Max(1, (int)Math.Min(FirstInterval.TotalMilliseconds, (double)int.MaxValue));
        var nextInterval = TimeSpan.FromMilliseconds(Random.Shared.Next(1, maxMs + 1));

        // Loop-de-loop until cancellation is requested.
        while (!cancellationToken.IsCancellationRequested)
        {
            // Wait for either the interval to expire or an explicit signal.
            try
            {
                if (nextInterval == Timeout.InfiniteTimeSpan)
                    await _signal.WaitAsync(cancellationToken).ConfigureAwait(false);
                else
                {
                    _delayCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    var delayTask = Task.Delay(nextInterval, _delayCts.Token);
                    var signalTask = _signal.WaitAsync(cancellationToken);
                    await Task.WhenAny(delayTask, signalTask).ConfigureAwait(false);
                    _delayCts?.Dispose();
                    _delayCts = null;
                }
            }
            catch (Exception ex) when (ex.IsCanceled()) { break; }

            lock (SyncLock)
            {
                // Where not currently sleeping, then we need to wait for a status change.
                if (!Status.IsAsleep)
                {
                    nextInterval = Timeout.InfiniteTimeSpan;
                    continue;
                }

                LastException = null;
                Status = ServiceStatus.Running;

                if (Logger.IsEnabled(LogLevel.Debug))
                    Logger.LogDebug("{ServiceName} execution triggered.", ServiceName);
            }

            // Do the actual work!
            try
            {
                await ScopedExecuteAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex.IsCanceled())
                    break;

                ExceptionHandling(ex);
            }

            // Confirm the status and determine next interval.
            if (!ManageStatusAndGetNextInterval(out nextInterval))
                break;
        }
    }

    /// <summary>
    /// Orchestrates the scoped execution.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Task"/> that represents the long running operation.</returns>
    private async Task ScopedExecuteAsync(CancellationToken cancellationToken)
    {
        var immediate = false;
        int consecutiveExecutionCount = 0;

        do
        {
            // Where the cancellation token is requested, exit the loop and stop processing.
            if (cancellationToken.IsCancellationRequested)
                break;

            // Manage the status and confirm can keep running.
            if (!ManageStatusAndGetNextInterval(out _, checkCanKeepRunningStatus: true))
                break;

            // Create a scope in which to perform the execution.
            await using var scope = ServiceProvider.CreateAsyncScope();

            // Instantiate and configure the execution context.
            var ec = scope.ServiceProvider.GetRequiredService<ExecutionContext>();
            ExecutionContextConfigure?.Invoke(ec);

            await HostedServiceInvoker.InvokeAsync(this, async (_, cancellationToken) =>
            {
                // Enable logging scope data.
                var sd = new Dictionary<string, object?>();
                AddLoggingScope(sd);

                using (Logger.BeginScope(sd))
                {
                    // Execute the work!
                    try
                    {
                        immediate = await OnExecuteAsync(ec, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (ex.IsCanceled())
                            return;

                        immediate = false;
                        ExceptionHandling(ex);
                    }
                }
            }, cancellationToken).ConfigureAwait(false);

            if (immediate && ++consecutiveExecutionCount > MaxConsecutiveExecutions)
            {
                immediate = false;
                if (Logger.IsEnabled(LogLevel.Warning))
                    Logger.LogWarning("{ServiceName} exceeded {Max} consecutive executions, forcing sleep.", ServiceName, MaxConsecutiveExecutions);
            }
        }
        while (immediate);
    }

    /// <summary>
    /// Provides the common/standardized exception handling.
    /// </summary>
    private void ExceptionHandling(Exception ex)
    {
        lock (SyncLock)
        {
            LastException = ex;

            // Stop where requested; otherwise, swallow and continue.
            if (OnUnhandledException(ex))
            {
                OneOffIntervalAdjustInternal(Timeout.InfiniteTimeSpan);
                if (Logger.IsEnabled(LogLevel.Critical))
                    Logger.LogCritical(ex, "{ServiceName} pausing due to failure: {Error}", ServiceName, ex.Message);
            }
            else
            {
                OneOffIntervalAdjustInternal(OnUnhandledInterval);
                if (Logger.IsEnabled(LogLevel.Error))
                    Logger.LogError(ex, "{ServiceName} failure; continuation in {NextInterval}: {Error}", ServiceName, _oneOffInterval ?? Interval, ex.Message);
            }
        }
    }

    /// <summary>
    /// Manage the status and report accordingly and advise whether can continue.
    /// </summary>
    private bool ManageStatusAndGetNextInterval(out TimeSpan nextInterval, bool checkCanKeepRunningStatus = false)
    {
        lock (SyncLock)
        {
            if (Status.IsStop)
            {
                nextInterval = Timeout.InfiniteTimeSpan;
                return false;
            }

            var interval = _oneOffInterval ?? Interval;
            _oneOffInterval = null;

            if (interval == Timeout.InfiniteTimeSpan)
            {
                if (Status != ServiceStatus.Paused)
                {
                    Status = ServiceStatus.Paused;
                    if (LastException is null && Logger.IsEnabled(LogLevel.Warning))
                        Logger.LogWarning("{ServiceName} execution completed. Paused with no scheduled continuation.", ServiceName);
                }

                nextInterval = Timeout.InfiniteTimeSpan;
            }
            else
            {
                if (!Status.IsAsleep)
                {
                    LastExecuted = DateTimeOffset.UtcNow;

                    if (Status.IsRunning && checkCanKeepRunningStatus)
                    {
                        Status = ServiceStatus.Running; // Forces report of health.
                        nextInterval = interval;
                        return true;
                    }

                    Status = ServiceStatus.Sleeping;
                    if (Logger.IsEnabled(LogLevel.Debug))
                        Logger.LogDebug("{ServiceName} execution completed. Sleeping with continuation in {Interval}.", ServiceName, interval);

                    nextInterval = interval;
                }
                else
                {
                    nextInterval = interval;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// Triggered to perform the work within a <i>scoped</i> <see cref="ExecutionContext"/>.
    /// </summary>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> indicates that the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> should be re-executed immediately (without an interval); otherwise, 
    /// <see langword="false"/> to re-execute after the configured <see cref="Interval"/>.</returns>
    /// <remarks>Each timer-based invocation of the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI)
    /// <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see> and corresponding <see cref="ExecutionContext"/>.</remarks>
    protected abstract Task<bool> OnExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken);

    /// <inheritdoc/>
    protected override Task OnPauseAsync(CancellationToken cancellationToken)
    {
        lock (SyncLock)
        {
            _oneOffInterval = Timeout.InfiniteTimeSpan;
            SignalWakeUp();
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnResumeAsync(CancellationToken cancellationToken)
    {
        lock (SyncLock)
        {
            if (Status == ServiceStatus.Resuming)
            {
                _oneOffInterval = Interval;
                Status = ServiceStatus.Sleeping;
                LastException = null;

                if (Logger.IsEnabled(LogLevel.Information))
                    Logger.LogInformation("{ServiceName} resumed. Status reset to Sleeping.", ServiceName);

                SignalWakeUp();
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Triggered when the application host is performing a graceful shutdown.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
    protected sealed override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        try
        {
            _delayCts?.Cancel();
            _backgroundCts?.Cancel();
        }
        finally
        {
            try
            {
                if (_backgroundTask != null)
                    await _backgroundTask.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex.IsCanceled())
            {
                // Graceful shutdown timeout exceeded - this is acceptable.
            }
        }

        await OnStoppingAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Triggered when the <see cref="TimerHostedServiceBase"/> is stopping (after <see cref="OnExecuteAsync"/> has stopped).
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected virtual Task OnStoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    protected override HealthCheckResult OnReportHealthStatus(Dictionary<string, object> data)
    {
        data.Add("interval", Interval.ToString());
        data.Add("firstInterval", FirstInterval.ToString());
        data.Add("onUnhandledInterval", OnUnhandledInterval.ToString());
        data.Add("pauseOnUnhandledException", PauseOnUnhandledException);
        data.Add("maxConsecutiveExecutions", MaxConsecutiveExecutions);
        data.Add("lastExecuted", LastExecuted);

        return Status.IsPause
            ? HealthCheckResult.Degraded("Service is in a paused state.", null, data)
            : (LastException is null ? HealthCheckResult.Healthy(null, data) : HealthCheckResult.Degraded(null, LastException, data));
    }

    /// <summary>
    /// Provides an opportunity to add additional logging scope data for the <see cref="ILogger"/> during the execution of the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> method.
    /// </summary>
    /// <param name="data">The <see cref="IDictionary{TKey, TValue}"/> to add scope data to.</param>
    protected virtual void AddLoggingScope(IDictionary<string, object?> data) { }

    /// <summary>
    /// Provides an opportunity to make a one-off change to the underlying timer to trigger using the specified <paramref name="oneOffInterval"/>.
    /// </summary>
    /// <param name="oneOffInterval">The one-off interval.</param>
    /// <remarks>A negative <paramref name="oneOffInterval"/> will result in <see cref="Timeout.InfiniteTimeSpan"/> leading to a status change to paused.</remarks>
    protected void OneOffIntervalAdjust(TimeSpan oneOffInterval)
    {
        lock (SyncLock)
        {
            OneOffIntervalAdjustInternal(oneOffInterval);
        }
    }

    /// <summary>
    /// Adjusts the interval for the next scheduled execution to a one-off value, overriding the regular interval for a single occurrence.
    /// </summary>
    private void OneOffIntervalAdjustInternal(TimeSpan oneOffInterval)
    {
        if (Status.IsStop)
            return;

        if (oneOffInterval < TimeSpan.Zero)
            oneOffInterval = Timeout.InfiniteTimeSpan;

        _oneOffInterval = oneOffInterval;
        SignalWakeUp();
    }

    /// <summary>
    /// Provides an opportunity to explicitly trigger the service execution versus waiting for the next scheduled interval.
    /// </summary>
    /// <param name="oneOffInterval">The one-off interval before triggering; defaults to <c>null</c> which represents an immediate trigger.</param>
    /// <remarks>A negative <paramref name="oneOffInterval"/> will result in <see cref="Timeout.InfiniteTimeSpan"/> leading to a status change to paused.
    /// <para>Invokes the <see cref="OneOffIntervalAdjust(TimeSpan)"/> internally to perform.</para></remarks>
    public void OneOffTrigger(TimeSpan? oneOffInterval = null) => OneOffIntervalAdjust(oneOffInterval ?? TimeSpan.Zero);

    /// <summary>
    /// Signals the background loop to wake up immediately by canceling any active delay and releasing the semaphore.
    /// </summary>
    private void SignalWakeUp()
    {
        _delayCts?.Cancel();
        if (_signal.CurrentCount == 0)
            _signal.Release();
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try { _backgroundCts?.Cancel(); } catch { }
            try { _delayCts?.Cancel(); } catch { }

            try
            {
                _signal?.Dispose();
                _delayCts?.Dispose();
                _backgroundCts?.Dispose();
            }
            catch { }
            finally
            {
                _delayCts = null;
                _backgroundTask = null;
                _backgroundCts = null;
            }
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Performs a one-off <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> of the service work outside of the timer-based schedule.
    /// </summary>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> indicates that the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> should be re-executed immediately (without an interval); otherwise, 
    /// <see langword="false"/> to re-execute after the configured <see cref="Interval"/>.</returns>
    /// <remarks><b>Warning:</b> this is intended for advanced scenarios, such as testing, and improper usage may result in unexpected behavior.</remarks>
    public async Task<bool> OneOffExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken) => await OnExecuteAsync(executionContext, cancellationToken).ConfigureAwait(false);
}