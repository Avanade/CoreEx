namespace CoreEx.Operations;

/// <summary>
/// Provides a process-local reference <see cref="IOperationStore"/>.
/// </summary>
/// <param name="logger">The <see cref="ILogger{TCategoryName}"/>; required so that every expired operation is reported.</param>
/// <param name="settings">The <see cref="IHostSettings"/> providing the domain name.</param>
/// <param name="options">The optional <see cref="OperationStoreOptions"/>.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/> used as store time; defaults to <see cref="TimeProvider.System"/>.</param>
/// <remarks>Each command is atomic within the process, but the store is <b>not</b> transactional: it never participates in a unit of work and cannot commit atomically with business writes or an outbox. Use it for development and tests only; register as a singleton.</remarks>
public sealed class InMemoryOperationStore(ILogger<InMemoryOperationStore> logger, IHostSettings settings, OperationStoreOptions? options = null, TimeProvider? timeProvider = null) : OperationStoreBase(logger, settings, options)
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<CompositeKey, OperationState> _operations = [];
    private readonly Dictionary<Guid, CompositeKey> _operationIds = [];
#if NET8_0
    private readonly object _lock = new();
#else
    private readonly Lock _lock = new();
#endif

    /// <inheritdoc/>
    protected override Task<OperationState?> OnGetByIdAsync(Guid operationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(TryGet(operationId, out var state) ? state : null);
        }
    }

    /// <inheritdoc/>
    protected override Task<OperationState?> OnGetByKeyAsync(string domainName, string? tenantId, string operationName, string instanceId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CompositeKey.Create(domainName, tenantId, operationName, instanceId);
        lock (_lock)
        {
            return Task.FromResult(_operations.TryGetValue(key, out var state) ? state : null);
        }
    }

    /// <inheritdoc/>
    protected override Task<OperationCreateResult> OnCreateAsync(string domainName, string? tenantId, string operationName, string instanceId, string? initiatorId, string? data, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var key = CompositeKey.Create(domainName, tenantId, operationName, instanceId);
        lock (_lock)
        {
            if (_operations.TryGetValue(key, out var existing))
                return Task.FromResult(new OperationCreateResult(false, existing));

            var now = _timeProvider.GetUtcNow();
            var state = new OperationState
            {
                DomainName = domainName,
                TenantId = tenantId,
                OperationName = operationName,
                InstanceId = instanceId,
                OperationId = Guid.NewGuid(),
                Status = OperationStatus.Accepted,
                InitiatorId = initiatorId,
                Data = data,
                CreatedOn = now,
                UpdatedOn = now,
                AbandonAfter = now.Add(Options.InactivityTimeout),
                ETag = NewETag()
            };

            _operations[key] = state;
            _operationIds[state.OperationId] = key;
            return Task.FromResult(new OperationCreateResult(true, state));
        }
    }

    /// <inheritdoc/>
    protected override Task<OperationClaimResult> OnClaimAsync(Guid operationId, string owner, TimeSpan duration, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!TryGet(operationId, out var state))
                return Task.FromResult(new OperationClaimResult(OperationClaimOutcome.NotFound, null, null));

            if (state.IsFinished)
                return Task.FromResult(new OperationClaimResult(OperationClaimOutcome.Finished, state, null));

            var now = _timeProvider.GetUtcNow();
            if (state.ClaimOwner is not null && state.ClaimExpiresOn > now)
                return Task.FromResult(new OperationClaimResult(OperationClaimOutcome.Busy, state, null));

            state = Save(state with
            {
                Status = OperationStatus.Running,
                Attempt = state.Attempt + 1,
                ClaimOwner = owner,
                ClaimExpiresOn = now.Add(duration),
                AbandonAfter = now.Add(duration).Add(Options.InactivityTimeout),
                StartedOn = state.StartedOn ?? now,
                UpdatedOn = now,
                ETag = NewETag()
            });

            return Task.FromResult(new OperationClaimResult(OperationClaimOutcome.Claimed, state, state.ToClaim()));
        }
    }

    /// <inheritdoc/>
    protected override Task<OperationClaim?> OnRenewAsync(OperationClaim claim, TimeSpan duration, string? progress, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var now = _timeProvider.GetUtcNow();
            if (!TryGetHeld(claim, now, out var state))
                return Task.FromResult<OperationClaim?>(null);

            state = Save(state with { ClaimExpiresOn = now.Add(duration), AbandonAfter = now.Add(duration).Add(Options.InactivityTimeout), Progress = progress ?? state.Progress, UpdatedOn = now, ETag = NewETag() });
            return Task.FromResult<OperationClaim?>(state.ToClaim());
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> OnReleaseAsync(OperationClaim claim, string? errorCode, string? errorMessage, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var now = _timeProvider.GetUtcNow();
            if (!TryGetHeld(claim, now, out var state))
                return Task.FromResult(false);

            Save(state with
            {
                Status = OperationStatus.Retrying,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                ClaimOwner = null,
                ClaimExpiresOn = null,
                AbandonAfter = now.Add(Options.InactivityTimeout),
                UpdatedOn = now,
                ETag = NewETag()
            });
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    protected override Task<OperationState?> OnCompleteAsync(OperationClaim claim, OperationCompletion completion, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var now = _timeProvider.GetUtcNow();
            if (!TryGetHeld(claim, now, out var state))
                return Task.FromResult<OperationState?>(null);

            return Task.FromResult<OperationState?>(Save(state with
            {
                Status = completion.Status,
                ResultLocation = completion.ResultLocation,
                ErrorCode = completion.ErrorCode,
                ErrorMessage = completion.ErrorMessage,
                ClaimOwner = null,
                ClaimExpiresOn = null,
                AbandonAfter = null,
                CompletedOn = now,
                PurgeAfter = now.Add(Options.TerminalRetention),
                UpdatedOn = now,
                ETag = NewETag()
            }));
        }
    }

    /// <inheritdoc/>
    protected override Task<OperationState?> OnRequestCancelAsync(Guid operationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            if (!TryGet(operationId, out var state))
                return Task.FromResult<OperationState?>(null);

            var now = _timeProvider.GetUtcNow();
            if (state.Status is OperationStatus.Accepted or OperationStatus.Retrying)
            {
                state = Save(state with
                {
                    Status = OperationStatus.Cancelled,
                    ErrorCode = null,
                    ErrorMessage = null,
                    CancelRequestedOn = now,
                    AbandonAfter = null,
                    CompletedOn = now,
                    PurgeAfter = now.Add(Options.TerminalRetention),
                    UpdatedOn = now,
                    ETag = NewETag()
                });
            }
            else if (state.Status == OperationStatus.Running && state.CancelRequestedOn is null)
                state = Save(state with { CancelRequestedOn = now, UpdatedOn = now, ETag = NewETag() });

            return Task.FromResult<OperationState?>(state);
        }
    }

    /// <inheritdoc/>
    protected override Task<OperationCleanupResult> OnCleanupAsync(int maxCount, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            var now = _timeProvider.GetUtcNow();
            var expired = 0;
            var purged = 0;

            foreach (var state in _operations.Values.Where(x => !x.IsFinished && x.AbandonAfter <= now).Take(maxCount).ToList())
            {
                var saved = Save(state with
                {
                    Status = OperationStatus.Expired,
                    ClaimOwner = null,
                    ClaimExpiresOn = null,
                    AbandonAfter = null,
                    CompletedOn = now,
                    PurgeAfter = now.Add(Options.TerminalRetention),
                    UpdatedOn = now,
                    ETag = NewETag()
                });

                LogExpired(saved, state.Status);
                expired++;
            }

            foreach (var state in _operations.Values.Where(x => x.IsFinished && x.PurgeAfter <= now).Take(maxCount).ToList())
            {
                _operations.Remove(Key(state));
                _operationIds.Remove(state.OperationId);
                purged++;
            }

            return Task.FromResult(new OperationCleanupResult(expired, purged));
        }
    }

    /// <summary>
    /// Gets the operation by identifier; must be called within the lock.
    /// </summary>
    private bool TryGet(Guid operationId, [NotNullWhen(true)] out OperationState? state)
    {
        state = null;
        return _operationIds.TryGetValue(operationId, out var key) && _operations.TryGetValue(key, out state);
    }

    /// <summary>
    /// Gets the operation where the <paramref name="claim"/> is still held and unexpired; must be called within the lock.
    /// </summary>
    private bool TryGetHeld(OperationClaim claim, DateTimeOffset now, [NotNullWhen(true)] out OperationState? state)
        => TryGet(claim.OperationId, out state)
            && state.Status == OperationStatus.Running
            && string.Equals(state.ClaimOwner, claim.Owner, StringComparison.Ordinal)
            && state.Attempt == claim.Attempt
            && state.ClaimExpiresOn > now;

    /// <summary>
    /// Replaces the stored operation; must be called within the lock.
    /// </summary>
    private OperationState Save(OperationState state) => _operations[Key(state)] = state;

    /// <summary>
    /// Creates the in-memory key from the stored identity components.
    /// </summary>
    private static CompositeKey Key(OperationState state) => CompositeKey.Create(state.DomainName, state.TenantId, state.OperationName, state.InstanceId);

    /// <summary>
    /// Creates a new globally unique concurrency token.
    /// </summary>
    private static string NewETag() => Guid.NewGuid().ToString("N");
}
