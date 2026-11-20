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

    /// <summary>
    /// Gets or sets the explicit operation key for the synchronization lease.
    /// </summary>
    /// <remarks>When set, this takes precedence over <see cref="SynchronizerName"/>. Otherwise a domain-wide key is created from host settings and the service type and name. Supply the same stable key on every participating replica and invocation.</remarks>
    protected OperationKey? SynchronizerKey { get; set; }

    /// <inheritdoc/>
    protected sealed override async Task<bool> OnExecuteAsync(ExecutionContext executionContext, CancellationToken cancellationToken)
    {
        var synchronizer = ExecutionContext.GetRequiredService<TSynchronizer>();

        var lease = SynchronizerKey is { } key
            ? await synchronizer.TryAcquireAsync(key, cancellationToken).ConfigureAwait(false)
            : await synchronizer.TryAcquireAsync<TSelf>(ExecutionContext.GetRequiredService<IHostSettings>().DomainName, SynchronizerName, cancellationToken).ConfigureAwait(false);
        if (lease is null)
            return false;

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lease.OwnershipLost);
        Exception? failure = null;
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            var result = await SynchronizedExecuteAsync(executionContext, linked.Token).ConfigureAwait(false);

            lease.OwnershipLost.ThrowIfCancellationRequested();
            return result;
        }
        catch (Exception ex)
        {
            failure = ex;
            throw;
        }
        finally
        {
            try
            {
                await lease.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (failure is not null)
            {
                Logger.LogError(ex, "Synchronization cleanup failed after protected execution failed.");
            }
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
