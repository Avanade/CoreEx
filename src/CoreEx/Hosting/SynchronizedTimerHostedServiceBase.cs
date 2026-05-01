namespace CoreEx.Hosting;

/// <summary>
/// Extends the <see cref="TimerHostedServiceBase"/> and adds <see cref="ISynchronizer"/> support to the <see cref="TimerHostedServiceBase.OnExecuteAsync(CoreEx.ExecutionContext, CancellationToken)"/> to manage synchronized concurrency of execution.
/// </summary>
/// <typeparam name="TSynchronizer">The <see cref="ISynchronizer"/> <see cref="Type"/>.</typeparam>
/// <typeparam name="TSelf">The implementing (self) <see cref="Type"/>; also used for synchronization.</typeparam>
/// <param name="serviceProvider">The <see cref="IServiceProvider"/>.</param>
/// <param name="logger">The <see cref="ILogger"/>.</param>
/// <remarks>Each timer-based invocation of the <see cref="SynchronizedExecuteAsync(ExecutionContext, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI)
/// <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see> and <see cref="ISynchronizer"/>. As the <see cref="ISynchronizer"/> may be scoped, it will be automatically resolved from the <i>scoped</i> <paramref name="serviceProvider"/> before use.
/// <para>A <see cref="TimerHostedServiceBase.OneOffIntervalAdjust(TimeSpan)"/> is provided to enable a one-off change to the timer where required.</para></remarks>
public abstract class SynchronizedTimerHostedServiceBase<TSynchronizer, TSelf>(IServiceProvider serviceProvider, ILogger logger)
    : TimerHostedServiceBase(serviceProvider, logger) where TSynchronizer : class, ISynchronizer where TSelf : SynchronizedTimerHostedServiceBase<TSynchronizer, TSelf>
{
    /// <summary>
    /// Gets or sets the optional name to differentiate the synchronization lock.
    /// </summary>
    protected string? SynchronizerName { get; set; }

    /// <inheritdoc/>
    protected sealed override async Task<bool> OnExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken)
    {
        var synchronizer = ExecutionContext.GetRequiredService<TSynchronizer>();

        // Attempt to enter the synchronizer; if not successful, simply exit.
        var entered = await synchronizer.EnterAsync<TSelf>(SynchronizerName, cancellationToken).ConfigureAwait(false);
        if (!entered)
            return false;

        // Execute within the synchronizer; and exit once complete (regardless of outcome).
        try
        {
            return await SynchronizedExecuteAsync(executionContext, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await synchronizer.ExitAsync<TSelf>(SynchronizerName, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Triggered to perform the work as a result of the <see cref="TimerHostedServiceBase.Interval"/> within a <i>scoped</i> <see cref="ExecutionContext"/> and synchronized via the <see cref="ISynchronizer"/>.
    /// </summary>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> indicates that the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> should be re-executed immediately (without an interval); otherwise, 
    /// <see langword="false"/> to re-execute after the configured <see cref="TimerHostedServiceBase.Interval"/>.</returns>
    /// <remarks>Each timer-based invocation of the <see cref="OnExecuteAsync(ExecutionContext, CancellationToken)"/> will be managed within the context of a new Dependency Injection (DI)
    /// <see cref="ServiceProviderServiceExtensions.CreateScope">scope</see> and corresponding <see cref="ExecutionContext"/>.</remarks>
    protected abstract Task<bool> SynchronizedExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken);
}