namespace CoreEx.Coordination;

/// <summary>
/// Provides atomic in-memory coordination across DI scopes sharing the same backing state, never across independent hosts or replicas.
/// </summary>
/// <remarks>No Redis connection is used. Normal registration shares one backing state per root service provider; independently built service providers do not coordinate, even within the same .NET process.</remarks>
/// <param name="state">The singleton backing state.</param>
/// <param name="settings">The <see cref="IHostSettings"/> providing the domain name.</param>
/// <param name="logger">The logger.</param>
/// <param name="options">The ownership options.</param>
public sealed class InMemoryCoordination(InMemoryCoordinationState state, IHostSettings settings, ILogger<InMemoryCoordination> logger, CoordinationOptions options) : ISynchronizer, CoreEx.Idempotency.IIdempotencyStore
{
    /// <summary>
    /// Gets the clock independently of request-frozen application time.
    /// </summary>
    private static DateTimeOffset Now => TimeProvider.System.GetUtcNow();

    /// <summary>
    /// Namespaces the identity qualified by the host domain and ambient tenant.
    /// </summary>
    private (string Purpose, CompositeKey Key) Key(string purpose, string name, CompositeKey key)
    {
        var tenantId = ExecutionContext.TryGetCurrent(out var ec) && !string.IsNullOrEmpty(ec.TenantId) ? ec.TenantId : null;
        return (purpose, CompositeKey.Create(settings.ThrowIfNull().DomainName.ThrowIfNullOrWhiteSpace(nameof(IHostSettings.DomainName)), tenantId, name.ThrowIfNullOrWhiteSpace(), key.ToCanonicalString().ThrowIfNullOrWhiteSpace(nameof(key))));
    }

    /// <summary>
    /// Removes expired records before accessing the shared store.
    /// </summary>
    private void Prune()
    {
        foreach (var key in state.Entries.Where(x => x.Value.Expiry <= Now).Select(x => x.Key).ToArray())
            state.Entries.Remove(key);
    }

    /// <inheritdoc/>
    public async Task<ISynchronizationLease?> TryAcquireAsync(string name, CompositeKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        options.Validate();
        var physical = Key("lease", name, key);
        var started = Stopwatch.GetTimestamp();
        var owner = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        lock (state.SyncRoot)
        {
            Prune();
            if (state.Entries.ContainsKey(physical))
                return null;

            state.Entries.Add(physical, new() { Owner = owner, Deadline = Now + options.LeaseDuration, Expiry = Now + options.LeaseDuration });
        }

        var lease = new CoordinationLease(options, started, ct => Renew(physical, owner, false, ct), ct => RetainOrRelease(physical, owner, false, ct), logger);
        if (lease.OwnershipLost.IsCancellationRequested)
        {
            await lease.DisposeAsync().ConfigureAwait(false);
            throw new TimeoutException("The acquired in-memory lease has no remaining conservative validity.");
        }

        return lease;
    }

    /// <inheritdoc/>
    public async Task<CoreEx.Idempotency.IdempotencyBeginResult> TryBeginAsync(string name, CompositeKey key, string fingerprint, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        options.Validate();
        fingerprint.ThrowIfNullOrEmpty();
        var physical = Key("idempotency", name, key);
        var started = Stopwatch.GetTimestamp();
        var owner = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

        lock (state.SyncRoot)
        {
            Prune();
            if (state.Entries.TryGetValue(physical, out var entry))
            {
                var outcome = entry.Fingerprint != fingerprint ? CoreEx.Idempotency.IdempotencyOutcome.DifferentRequest
                    : entry.State == CoreEx.Idempotency.IdempotencyOutcome.InProgress && entry.Deadline <= Now ? CoreEx.Idempotency.IdempotencyOutcome.Indeterminate : entry.State;

                return new(outcome, Response: outcome == CoreEx.Idempotency.IdempotencyOutcome.Completed && entry.Response is not null ? new BinaryData(entry.Response.ToArray()) : null, Revision: entry.Owner);
            }

            state.Entries.Add(physical, new() { Owner = owner, Fingerprint = fingerprint, State = CoreEx.Idempotency.IdempotencyOutcome.InProgress, Deadline = Now + options.LeaseDuration, Expiry = Now + options.LeaseDuration + options.Retention });
        }

        var attempt = new CoreEx.Idempotency.IdempotencyAttempt(options, started, ct => Renew(physical, owner, true, ct), ct => RetainOrRelease(physical, owner, true, ct), (response, ct) => Complete(physical, owner, response, ct), ct => ReleaseForRetry(physical, owner, ct), logger);
        if (attempt.OwnershipLost.IsCancellationRequested)
        {
            await attempt.DisposeAsync().ConfigureAwait(false);
            throw new TimeoutException("The acquired in-memory idempotency attempt has no remaining conservative validity.");
        }

        return new(CoreEx.Idempotency.IdempotencyOutcome.Acquired, attempt, Revision: owner);
    }

    /// <summary>
    /// Renews an owned, unexpired processing record.
    /// </summary>
    private Task<bool> Renew((string Purpose, CompositeKey Key) key, string owner, bool retained, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (state.SyncRoot)
        {
            if (!state.Entries.TryGetValue(key, out var entry) || entry.Owner != owner || entry.Deadline <= Now || (retained && entry.State != CoreEx.Idempotency.IdempotencyOutcome.InProgress))
                return Task.FromResult(false);

            entry.Deadline = Now + options.LeaseDuration;
            entry.Expiry = entry.Deadline + (retained ? options.Retention : TimeSpan.Zero);
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Releases a synchronization lease or retains uncertain idempotency work.
    /// </summary>
    private Task RetainOrRelease((string Purpose, CompositeKey Key) key, string owner, bool retained, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (state.SyncRoot)
        {
            if (state.Entries.TryGetValue(key, out var entry) && entry.Owner == owner)
            {
                if (!retained)
                    state.Entries.Remove(key);
                else if (entry.State == CoreEx.Idempotency.IdempotencyOutcome.InProgress)
                    entry.State = CoreEx.Idempotency.IdempotencyOutcome.Indeterminate;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Commits an owned terminal response without overwriting another attempt.
    /// </summary>
    private Task<bool> Complete((string Purpose, CompositeKey Key) key, string owner, BinaryData? response, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (state.SyncRoot)
        {
            if (!state.Entries.TryGetValue(key, out var entry) || entry.Owner != owner || entry.Expiry <= Now)
                return Task.FromResult(false);

            if (entry.State is CoreEx.Idempotency.IdempotencyOutcome.Completed or CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable)
                return Task.FromResult(entry.Response is null ? response is null : response is not null && entry.Response.ToMemory().Span.SequenceEqual(response.ToMemory().Span));

            if (entry.State != CoreEx.Idempotency.IdempotencyOutcome.InProgress || entry.Deadline <= Now)
                return Task.FromResult(false);

            entry.Response = response is null ? null : new BinaryData(response.ToArray());
            entry.State = response is null ? CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable : CoreEx.Idempotency.IdempotencyOutcome.Completed;
            entry.Expiry = Now + options.Retention;
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Deletes the same active owner attempt before its lease expires.
    /// </summary>
    private Task<bool> ReleaseForRetry((string Purpose, CompositeKey Key) key, string owner, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (state.SyncRoot)
        {
            if (!state.Entries.TryGetValue(key, out var entry) || entry.Owner != owner || entry.State != CoreEx.Idempotency.IdempotencyOutcome.InProgress || entry.Deadline <= Now)
                return Task.FromResult(false);

            state.Entries.Remove(key);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> TryResolveAsync(string name, CompositeKey key, string revision, BinaryData? response, bool allowRetry = false, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        options.Validate();
        revision.ThrowIfNullOrEmpty();
        if (!allowRetry)
            CoreEx.Idempotency.IdempotencyAttempt.ThrowIfEmptyResponse(response);

        var physical = Key("idempotency", name, key);
        lock (state.SyncRoot)
        {
            Prune();
            if (!state.Entries.TryGetValue(physical, out var entry) || entry.Owner != revision
                || (entry.State != CoreEx.Idempotency.IdempotencyOutcome.Indeterminate && (entry.State != CoreEx.Idempotency.IdempotencyOutcome.InProgress || entry.Deadline > Now)))
                return Task.FromResult(false);

            if (allowRetry)
                state.Entries.Remove(physical);
            else
            {
                entry.Response = response is null ? null : new BinaryData(response.ToArray());
                entry.State = response is null ? CoreEx.Idempotency.IdempotencyOutcome.ResponseUnavailable : CoreEx.Idempotency.IdempotencyOutcome.Completed;
                entry.Expiry = Now + options.Retention;
            }

            return Task.FromResult(true);
        }
    }
}
