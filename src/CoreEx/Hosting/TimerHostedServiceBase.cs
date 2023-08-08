// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Represents a base class for an <see cref="IHostedService"/> based on an <see cref="Interval"/> to <see cref="Execute(object?)"/> work.
    /// </summary>
    /// <remarks>Each timer-based invocation of the <see cref="ExecuteAsync(IServiceProvider, CancellationToken)"/> will be managed witin the context of a new Dependency Injection (DI)
    /// <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see>.
    /// <para>A <see cref="OneOffIntervalAdjust(TimeSpan, bool)"/> is provided to enable a one-off change to the timer where required.</para></remarks>
    public abstract class TimerHostedServiceBase : IHostedService, IDisposable
    {
        private static readonly Random _random = new();

        private readonly object _lock = new();
        private string? _name;
        private CancellationTokenSource? _cts;
        private Timer? _timer;
        private DateTime _lastExecuted = DateTime.MinValue;
        private TimeSpan? _oneOffInterval;
        private Task? _executeTask;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerHostedServiceBase"/> class.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>; defaults to instance from the <paramref name="serviceProvider"/> where not specified.</param>
        public TimerHostedServiceBase(IServiceProvider serviceProvider, ILogger logger, SettingsBase? settings = null)
        {
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Settings = settings ?? ServiceProvider.GetService<SettingsBase>() ?? new DefaultSettings(ServiceProvider.GetRequiredService<IConfiguration>());
        }

        /// <summary>
        /// Gets the <see cref="IServiceProvider"/>.
        /// </summary>
        protected IServiceProvider ServiceProvider;

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        protected SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the service name (used for the likes of configuration and logging).
        /// </summary>
        /// <remarks>Defaults to the <see cref="Type"/> <see cref="MemberInfo.Name"/>.</remarks>
        public virtual string ServiceName => _name ??= GetType().Name;

        /// <summary>
        /// Gets or sets the <i>first</i> timer start interval. 
        /// </summary>
        /// <remarks>Defaults to <see cref="Interval"/>. This is used as a maximum, in that the actual start is determined using a random value up to this value to ensure staggering of execution where multiple hosts are triggered at the same time.</remarks>
        public virtual TimeSpan? FirstInterval { get; set; }

        /// <summary>
        /// Gets or sets the timer interval <see cref="TimeSpan"/>.
        /// </summary>
        /// <remarks>Defaults to one hour.</remarks>
        public virtual TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(60);

        /// <summary>
        /// Provides an opportunity to make a one-off change to the underlying timer to trigger using the specified <paramref name="oneOffInterval"/>.
        /// </summary>
        /// <param name="oneOffInterval">The one-off interval.</param>
        /// <param name="leaveWhereTimeRemainingIsLess">Indicates whether to <i>not</i> adjust the time where the time remaining is less than the one-off interval specified.</param>
        protected void OneOffIntervalAdjust(TimeSpan oneOffInterval, bool leaveWhereTimeRemainingIsLess = false)
        {
            lock (_lock)
            {
                if (_disposed)
                    return;

                // Where already executing save the one-off value and use when ready.
                if (_executeTask != null)
                    _oneOffInterval = oneOffInterval;
                else
                {
                    _oneOffInterval = null;

                    // Where less time remaining than specified and requested to leave then do nothing.
                    if (leaveWhereTimeRemainingIsLess && (DateTime.UtcNow - _lastExecuted) < oneOffInterval)
                        return;

                    _timer?.Change(oneOffInterval, oneOffInterval);
                }
            }
        }

        /// <summary>
        /// Triggered when the application host is ready to start the service.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        async Task IHostedService.StartAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{ServiceName} started. Timer first/interval {FirstInterval}/{Interval}.", ServiceName, FirstInterval ?? Interval, Interval);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            await StartingAsync(_cts.Token).ConfigureAwait(false);
            _timer = new Timer(Execute, null, TimeSpan.FromMilliseconds(_random.Next(1, (int)(FirstInterval ?? Interval).TotalMilliseconds)), Interval);
        }

        /// <summary>
        /// Triggered when the <see cref="TimerHostedServiceBase"/> is starting (prior to initiating the timer).
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected virtual Task StartingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Performs the internal execution.
        /// </summary>
        private void Execute(object? state)
        {
            // Stop the timer as no more work should be initiated until after complete.
            lock (_lock)
            {
                _timer!.Change(Timeout.Infinite, Timeout.Infinite);
                Logger.LogDebug("{ServiceName} execution triggered by timer.", ServiceName);

                _executeTask = Task.Run(async () => await ScopedExecuteAsync(_cts!.Token).ConfigureAwait(false));
            }

            _executeTask.Wait();

            // Restart the timer.
            lock (_lock)
            {
                _executeTask = null;
                _lastExecuted = DateTime.UtcNow;

                if (_cts!.IsCancellationRequested)
                    return;

                var interval = _oneOffInterval ?? Interval;
                _oneOffInterval = null;

                Logger.LogDebug("{ServiceName} execution completed. Retry in {interval}.", ServiceName, Interval);
                _timer?.Change(interval, interval);
            }
        }

        /// <summary>
        /// Executes the data orchestration for the next outbox and/or incomplete outbox.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="Task"/> that represents the long running operations.</returns>
        private Task ScopedExecuteAsync(CancellationToken cancellationToken) => ServiceInvoker.Current.InvokeAsync(this, async (_, cancellationToken) =>
        {
            // Create a scope in which to perform the execution.
            using var scope = ServiceProvider.CreateScope();
            ExecutionContext.Reset();

            try
            {
                await ExecuteAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (ex is TaskCanceledException || (ex is AggregateException aex && aex.InnerException is TaskCanceledException))
                    return;

                Logger.LogCritical(ex, "{ServiceName} failure as a result of an unexpected exception: {Error}", ServiceName, ex.Message);
                throw;
            }
        }, cancellationToken);

        /// <summary>
        /// Triggered to perform the work as a result of the <see cref="Interval"/>.
        /// </summary>
        /// <param name="scopedServiceProvider">The scoped <see cref="IServiceProvider"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each timer-based invocation of the <see cref="ExecuteAsync(IServiceProvider, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI)
        /// <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see> that is passed for direct usage.</remarks>
        protected abstract Task ExecuteAsync(IServiceProvider scopedServiceProvider, CancellationToken cancellationToken);

        /// <summary>
        /// Triggered when the application host is performing a graceful shutdown.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        async Task IHostedService.StopAsync(CancellationToken cancellationToken)
        {
            Logger.LogInformation("{ServiceName} stop requested.", ServiceName);
            _timer!.Change(Timeout.Infinite, Timeout.Infinite);

            try
            {
                _cts!.Cancel();
            }
            finally
            {
                await Task.WhenAny(_executeTask ?? Task.CompletedTask, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
            }

            await StoppingAsync(cancellationToken).ConfigureAwait(false);
            Logger.LogInformation("{ServiceName} stopped.", ServiceName);
        }

        /// <summary>
        /// Triggered when the <see cref="TimerHostedServiceBase"/> is stopping (after <see cref="ExecuteAsync"/> has stopped).
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        protected virtual Task StoppingAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    _disposed = true;
                    _timer?.Dispose();
                    _cts?.Cancel();
                    _cts?.Dispose();
                }
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the <see cref="TimerHostedServiceBase"/> and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) { }
    }
}