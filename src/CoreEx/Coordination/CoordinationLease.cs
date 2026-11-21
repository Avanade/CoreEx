namespace CoreEx.Coordination;

/// <summary>
/// Maintains a conservatively timed lease with irreversible ownership loss.
/// </summary>
public sealed class CoordinationLease : ISynchronizationLease
{
    private readonly CoordinationOptions _options;
    private readonly Func<CancellationToken, Task<bool>> _renew;
    private readonly Func<CancellationToken, Task> _release;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _lost = new();
    private readonly CancellationTokenSource _stop = new();
    private readonly CancellationToken _ownershipLost;
    private readonly SemaphoreSlim _transition = new(1, 1);
    private readonly Task _loop;
    private int _disposed;

    /// <summary>
    /// Creates a lease after an acknowledged acquisition.
    /// </summary>
    /// <param name="options">The validated options.</param>
    /// <param name="acquisitionStarted">The monotonic acquisition start timestamp.</param>
    /// <param name="renew">The owner-checked renewal.</param>
    /// <param name="release">The owner-checked release or retained-outcome cleanup.</param>
    /// <param name="logger">The diagnostic logger.</param>
    public CoordinationLease(CoordinationOptions options, long acquisitionStarted, Func<CancellationToken, Task<bool>> renew, Func<CancellationToken, Task> release, ILogger logger)
    {
        options.Validate();
        _options = options;
        _renew = renew;
        _release = release;
        _logger = logger;
        _ownershipLost = _lost.Token;
        ArmDeadline(acquisitionStarted);
        _loop = RunAsync();
    }

    /// <inheritdoc/>
    public CancellationToken OwnershipLost => _ownershipLost;

    /// <summary>
    /// Throws if this handle has lost ownership or has been disposed.
    /// </summary>
    public void EnsureOwned()
    {
        if (_lost.IsCancellationRequested || Volatile.Read(ref _disposed) != 0)
            throw new InvalidOperationException("Coordination ownership has been lost or disposed.");
    }

    /// <summary>
    /// Arms a conservative local deadline without resurrecting lost ownership.
    /// </summary>
    private void ArmDeadline(long started)
    {
        var remaining = _options.LeaseDuration - Stopwatch.GetElapsedTime(started) - TimeSpan.FromMilliseconds(10);
        if (remaining <= TimeSpan.Zero)
            _lost.Cancel();
        else if (!_lost.IsCancellationRequested)
            _lost.CancelAfter(remaining);
    }

    /// <summary>
    /// Runs renewal; failure conservatively loses ownership and is logged.
    /// </summary>
    private async Task RunAsync()
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_stop.Token, _lost.Token);
        try
        {
            while (true)
            {
                await Task.Delay(_options.RenewalInterval, linked.Token).ConfigureAwait(false);
                await _transition.WaitAsync(linked.Token).ConfigureAwait(false);
                try
                {
                    linked.Token.ThrowIfCancellationRequested();
                    var started = Stopwatch.GetTimestamp();

                    using var operation = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
                    operation.CancelAfter(_options.OperationTimeout);

                    if (!await _renew(operation.Token).WaitAsync(operation.Token).ConfigureAwait(false))
                    {
                        _logger.LogWarning("Coordination renewal was refused; ownership is lost.");
                        _lost.Cancel();
                        return;
                    }

                    ArmDeadline(started);
                }
                finally
                {
                    _transition.Release();
                }
            }
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested)
        {
            if (!_stop.IsCancellationRequested)
                _logger.LogWarning("Coordination validity expired before renewal was acknowledged; ownership is lost.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Coordination renewal failed; ownership is lost.");
            _lost.Cancel();
        }
    }

    /// <summary>
    /// Serializes a terminal transition with renewal and stops the heartbeat once acknowledged.
    /// </summary>
    /// <param name="complete">The owner-checked terminal mutation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task CompleteAsync(Func<CancellationToken, Task<bool>> complete, CancellationToken cancellationToken)
        => TransitionAsync(complete, "The idempotency attempt no longer owns processing; its outcome requires reconciliation.", cancellationToken);

    /// <summary>
    /// Serializes an owner-checked release with renewal and stops the heartbeat once acknowledged.
    /// </summary>
    /// <param name="release">The owner-checked release mutation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public Task ReleaseForRetryAsync(Func<CancellationToken, Task<bool>> release, CancellationToken cancellationToken)
        => TransitionAsync(release, "The idempotency attempt could not be released because ownership was lost.", cancellationToken);

    /// <summary>
    /// Serializes an owned terminal transition against lease renewal.
    /// </summary>
    private async Task TransitionAsync(Func<CancellationToken, Task<bool>> transition, string failureMessage, CancellationToken cancellationToken)
    {
        await _transition.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            EnsureOwned();
            using var operation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lost.Token);
            operation.CancelAfter(_options.OperationTimeout);

            if (!await transition(operation.Token).WaitAsync(operation.Token).ConfigureAwait(false))
                throw new InvalidOperationException(failureMessage);

            _stop.Cancel();
            _lost.CancelAfter(Timeout.InfiniteTimeSpan);
        }
        finally
        {
            _transition.Release();
        }

        await _loop.ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        _stop.Cancel();
        await _loop.ConfigureAwait(false);
        await _transition.WaitAsync().ConfigureAwait(false);

        using var cleanup = new CancellationTokenSource(_options.OperationTimeout);
        try
        {
            await _release(cleanup.Token).WaitAsync(cleanup.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Coordination cleanup failed; backend expiry or reconciliation is required.");
            throw;
        }
        finally
        {
            try
            {
                _lost.Cancel();
            }
            finally
            {
                _transition.Release();
                _lost.Dispose();
                _stop.Dispose();
            }
        }
    }
}
