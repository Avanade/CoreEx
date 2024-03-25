// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Hosting.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Hosting
{
    /// <summary>
    /// Extends the <see cref="TimerHostedServiceBase"/> and adds <see cref="IServiceSynchronizer"/> to the <see cref="ExecuteAsync(IServiceProvider, CancellationToken)"/> to manage concurrency of execution.
    /// </summary>
    /// <typeparam name="TSync">The <see cref="Type"/> in which to perform the <see cref="Synchronizer"/> <see cref="IServiceSynchronizer.Enter{T}(string?)"/> for.</typeparam>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    /// <param name="settings">The <see cref="SettingsBase"/>; defaults to instance from the <paramref name="serviceProvider"/> where not specified.</param>
    /// <param name="synchronizer">The <see cref="IServiceSynchronizer"/>; defaults to <see cref="ConcurrentSynchronizer"/> where not specified.</param>
    /// <param name="healthCheck">The optional <see cref="TimerHostedServiceHealthCheck"/> to report health.</param>
    public abstract class SynchronizedTimerHostedServiceBase<TSync>(IServiceProvider serviceProvider, ILogger logger, SettingsBase? settings = null, IServiceSynchronizer? synchronizer = null, TimerHostedServiceHealthCheck? healthCheck = null) 
        : TimerHostedServiceBase(serviceProvider, logger, settings, healthCheck)
    {
        /// <summary>
        /// Gets the <see cref="IServiceSynchronizer"/>.
        /// </summary>
        protected IServiceSynchronizer Synchronizer { get; } = synchronizer ?? new ConcurrentSynchronizer();

        /// <summary>
        /// Gets or sets the optional synchronization name (used by <see cref="IServiceSynchronizer.Enter{T}(string?)"/> and <see cref="IServiceSynchronizer.Exit{T}(string?)"/>).
        /// </summary>
        protected string? SynchronizationName { get; set; }

        /// <summary>
        /// Triggered to perform the work as a result of the <see cref="TimerHostedServiceBase.Interval"/> is a synchronized manner.
        /// </summary>
        /// <param name="scopedServiceProvider">The scoped <see cref="IServiceProvider"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks><b>Note:</b> do <b>not</b> override this method as this implements the sychronization management; use <see cref="SynchronizedExecuteAsync(IServiceProvider, CancellationToken)"/> to implement desired functionality.
        /// <para>Each timer-based invocation of the <see cref="ExecuteAsync(IServiceProvider, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI)
        /// <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see> that is passed for direct usage.</para></remarks>
        protected async override Task ExecuteAsync(IServiceProvider scopedServiceProvider, CancellationToken cancellationToken)
        {
            // Ensure we have synchronized control; if not exit immediately.
            if (!Synchronizer.Enter<TSync>(SynchronizationName))
                return;

            try
            {
                await SynchronizedExecuteAsync(scopedServiceProvider, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Synchronizer.Exit<TSync>(SynchronizationName);
            }
        }

        /// <summary>
        /// Triggered to perform the work as a result of the <see cref="TimerHostedServiceBase.Interval"/> with the context of a <see cref="IServiceSynchronizer.Enter{T}(string?)"/> and <see cref="IServiceSynchronizer.Exit{T}(string?)"/>.
        /// </summary>
        /// <param name="scopedServiceProvider">The scoped <see cref="IServiceProvider"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <remarks>Each timer-based invocation of the <see cref="ExecuteAsync(IServiceProvider, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI)
        /// <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see> that is passed for direct usage.</remarks>
        protected abstract Task SynchronizedExecuteAsync(IServiceProvider scopedServiceProvider, CancellationToken cancellationToken);
    }
}