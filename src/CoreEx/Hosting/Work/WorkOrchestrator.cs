namespace CoreEx.Hosting.Work;

/// <summary>
/// Orchestrates durable, optimistic-concurrency work-state transitions.
/// </summary>
/// <param name="provider">The underlying work provider.</param>
/// <param name="options">The optional work configuration.</param>
/// <remarks>Each state transition is applied with provider ETag compare-and-set. A lease identifies the current worker epoch, but does not prevent an expired worker from continuing to execute; handlers requiring side-effect fencing must enforce the lease epoch in their own store.</remarks>
public class WorkOrchestrator(IWorkProvider provider, WorkOrchestratorOptions? options = null)
{
    private const int MaxConcurrencyAttempts = 10;
    private const string ExpiredMessage = "The work did not finish before its expiry deadline.";
    private readonly WorkOrchestratorInvoker _invoker = WorkOrchestratorInvoker.Default;
    private readonly WorkOrchestratorOptions _options = options ?? new WorkOrchestratorOptions();

    /// <summary>
    /// Gets the underlying provider.
    /// </summary>
    public IWorkProvider Provider { get; } = provider.ThrowIfNull(nameof(provider));

    /// <summary>
    /// Gets or sets the maximum time work may remain unfinished.
    /// </summary>
    public TimeSpan ExpiryTimeSpan
    {
        get => _options.Expiry;
        set
        {
            _options.Expiry = value;
            _options.Validate();
        }
    }

    /// <summary>
    /// Gets or sets the minimum duration finished work is retained.
    /// </summary>
    public TimeSpan RetentionTimeSpan
    {
        get => _options.Retention;
        set
        {
            _options.Retention = value;
            _options.Validate();
        }
    }

    /// <summary>
    /// Gets the latest state for an operation identity, applying automatic expiry when necessary.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state where found; otherwise, <see langword="null"/>.</returns>
    /// <remarks>This method does not check the owner and is intended for trusted system and worker code. Use <see cref="GetWithTypeAsync(string, OperationKey, CancellationToken)"/> for caller-facing access.</remarks>
    public Task<WorkState?> GetAsync(OperationKey operationKey, CancellationToken cancellationToken = default)
        => _invoker.InvokeAsync(this, async (_, ct) => (await TransitionAsync(operationKey, (_, _) => null, ct).ConfigureAwait(false)).State, cancellationToken);

    /// <summary>
    /// Gets the latest state for an operation identity and the provider time observed during the read, applying automatic expiry when necessary.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state where found and the provider time observed during the read.</returns>
    /// <remarks>This method does not check the owner and is intended for trusted system and worker code. Use <see cref="GetWithTypeAsync(string, OperationKey, CancellationToken)"/> for caller-facing access.</remarks>
    public Task<WorkSnapshot> GetSnapshotAsync(OperationKey operationKey, CancellationToken cancellationToken = default)
        => _invoker.InvokeAsync(this, async (_, ct) =>
        {
            var result = await TransitionAsync(operationKey, (_, _) => null, ct).ConfigureAwait(false);
            return new WorkSnapshot(result.State, result.ObservedOn);
        }, cancellationToken);

    /// <summary>
    /// Atomically creates a work item in the <see cref="WorkStatus.Created"/> state.
    /// </summary>
    /// <param name="args">The work creation arguments.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The new state, or a conflict when the operation identity is already in use.</returns>
    /// <remarks>The owner defaults to the current execution user, or <see cref="AuthenticationUser.EnvironmentUser"/> without an execution context. An unauthenticated owner creates unowned work.</remarks>
    public Task<Result<WorkState>> CreateAsync(WorkArgs args, CancellationToken cancellationToken = default)
        => _invoker.InvokeAsync<Result<WorkState>>(this, async (_, ct) =>
        {
            args.ThrowIfNull();
            var read = await Provider.GetAsync(args.OperationKey, ct).ConfigureAwait(false);
            ValidateStateIdentity(read.State, args.OperationKey);
            if (read.State is not null)
                return Result.ConflictError($"Work '{args.OperationKey.InstanceId}' already exists.");

            var expiry = args.Expiry ?? ExpiryTimeSpan;
            if (expiry <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(args.Expiry), "The work expiry must be greater than zero.");

            var startTimeout = args.StartTimeout ?? _options.StartTimeout;
            if (startTimeout is { } timeout && (timeout <= TimeSpan.Zero || timeout > expiry))
                throw new ArgumentOutOfRangeException(nameof(args.StartTimeout), "The start timeout must be greater than zero and no greater than the work expiry.");

            var traceParent = args.TraceParent;
            var traceState = args.TraceState;
            if (string.IsNullOrEmpty(traceParent) && Activity.Current is not null)
            {
                traceParent = Activity.Current.Id;
                traceState = Activity.Current.TraceStateString;
            }

            var user = args.User ?? (ExecutionContext.TryGetCurrent(out var ec) ? ec.User : AuthenticationUser.EnvironmentUser);
            if (user?.Type == AuthenticationType.Unauthenticated)
                user = null;

            var state = new WorkState
            {
                Scope = args.OperationKey.Scope,
                DomainName = args.OperationKey.DomainName,
                TenantId = args.OperationKey.TenantId,
                OperationName = args.OperationKey.OperationName,
                Id = args.Id,
                TypeName = args.TypeName.ThrowIfNullOrEmpty(),
                Status = WorkStatus.Created,
                CreatedOn = read.Now,
                UpdatedOn = read.Now,
                ExpiryOn = read.Now.Add(expiry),
                StartBy = startTimeout.HasValue ? read.Now.Add(startTimeout.Value) : null,
                User = user,
                TraceParent = traceParent,
                TraceState = traceState,
                Revision = Guid.NewGuid().ToString("N")
            };

            try
            {
                if (await Provider.CreateAsync(state, GetTimeToLive(state, read.Now), ct).ConfigureAwait(false))
                    return state;
            }
            catch (WorkOutcomeUnknownException)
            {
                var latest = await Provider.GetAsync(args.OperationKey, ct).ConfigureAwait(false);
                ValidateStateIdentity(latest.State, args.OperationKey);
                if (latest.State?.Revision == state.Revision)
                    return latest.State;

                throw;
            }

            return Result.ConflictError($"Work '{args.OperationKey.InstanceId}' already exists.");
        }, cancellationToken);

    /// <summary>
    /// Starts a created work item or takes over an expired worker lease.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="leaseOwner">The opaque lease owner token.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The explicit transition outcome and, when applied, the worker lease.</returns>
    public Task<WorkTransitionResult> StartAsync(OperationKey operationKey, string leaseOwner, CancellationToken cancellationToken = default)
    {
        leaseOwner.ThrowIfNullOrEmpty(nameof(leaseOwner));
        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(operationKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (state.Status == WorkStatus.Created)
            {
                state.Status = WorkStatus.Started;
                state.StartedOn = now;
                state.Attempt = 1;
            }
            else if (IsInProgress(state.Status))
            {
                if (state.LeaseExpiresOn is { } expires && expires > now)
                    return new(WorkTransitionOutcome.LeaseHeld, false);

                if (_options.MaxExecutionAttempts is { } maxAttempts && state.Attempt >= maxAttempts)
                {
                    Finish(state, WorkStatus.Failed, now, new WorkError(WorkErrorCodes.MaxAttempts, "The maximum number of execution attempts was reached."));
                    return new(WorkTransitionOutcome.Conflict);
                }

                state.Status = WorkStatus.Indeterminate;
                state.IndeterminateOn = now;
                state.Attempt++;
            }
            else
                return new(WorkTransitionOutcome.Conflict, false);

            state.LeaseOwner = leaseOwner;
            state.LeaseExpiresOn = LeaseExpiry(now, state.ExpiryOn);
            return new(WorkTransitionOutcome.Applied, true, IssueLease: true, IsTakeover: state.Attempt > 1);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Renews a worker lease if its owner and epoch are still current.
    /// </summary>
    /// <param name="lease">The lease to renew.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome and renewed lease.</returns>
    public Task<WorkTransitionResult> RenewLeaseAsync(WorkLease lease, CancellationToken cancellationToken = default)
    {
        lease.ThrowIfNull();
        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(lease.WorkKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (!IsInProgress(state.Status))
                return new(WorkTransitionOutcome.Conflict, false);

            if (!Owns(state, lease))
                return new(WorkTransitionOutcome.LeaseLost, false);

            state.LeaseExpiresOn = LeaseExpiry(now, state.ExpiryOn);
            return new(WorkTransitionOutcome.Applied, true, IssueLease: true);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Releases a worker lease without changing the operation status.
    /// </summary>
    /// <param name="lease">The lease to release.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> ReleaseLeaseAsync(WorkLease lease, CancellationToken cancellationToken = default)
    {
        lease.ThrowIfNull();
        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(lease.WorkKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (!IsInProgress(state.Status))
                return new(WorkTransitionOutcome.Conflict, false);

            if (!Owns(state, lease))
                return new(WorkTransitionOutcome.LeaseLost, false);

            state.LeaseExpiresOn = now;
            return new(WorkTransitionOutcome.Applied);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Persists progress for the current lease.
    /// </summary>
    /// <param name="lease">The current lease.</param>
    /// <param name="progress">The progress value.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> ReportProgressAsync(WorkLease lease, WorkProgress progress, CancellationToken cancellationToken = default)
    {
        lease.ThrowIfNull();
        progress.ThrowIfNull();
        if (progress.PercentComplete is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(progress), "Progress percentage must be between 0 and 100.");

        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(lease.WorkKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (!IsInProgress(state.Status))
                return new(WorkTransitionOutcome.Conflict, false);

            if (!Owns(state, lease))
                return new(WorkTransitionOutcome.LeaseLost, false);

            state.Progress = progress with { LastUpdated = now };
            return new(WorkTransitionOutcome.Applied);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Completes work owned by the supplied current lease.
    /// </summary>
    /// <param name="lease">The current lease.</param>
    /// <param name="resourceLocation">The app-relative URI of the resource produced by the work.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> CompleteAsync(WorkLease lease, string? resourceLocation = null, CancellationToken cancellationToken = default)
    {
        lease.ThrowIfNull();
        ValidateResourceLocation(resourceLocation);
        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(lease.WorkKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (!IsInProgress(state.Status))
                return new(WorkTransitionOutcome.Conflict, false);

            if (!Owns(state, lease))
                return new(WorkTransitionOutcome.LeaseLost, false);

            Finish(state, WorkStatus.Completed, now);
            state.ResourceLocation = resourceLocation;
            return new(WorkTransitionOutcome.Applied);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Fails work owned by the supplied current lease.
    /// </summary>
    /// <param name="lease">The current lease.</param>
    /// <param name="error">The safe failure details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> FailAsync(WorkLease lease, WorkError error, CancellationToken cancellationToken = default)
    {
        lease.ThrowIfNull();
        ValidateError(error);
        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(lease.WorkKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (!IsInProgress(state.Status))
                return new(WorkTransitionOutcome.Conflict, false);

            if (!Owns(state, lease))
                return new(WorkTransitionOutcome.LeaseLost, false);

            Finish(state, WorkStatus.Failed, now, error);
            return new(WorkTransitionOutcome.Applied);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Fails work when no live lease owns it, such as a disposition-driven failure.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="error">The safe failure details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> FailUnownedAsync(OperationKey operationKey, WorkError error, CancellationToken cancellationToken = default)
    {
        ValidateError(error);
        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(operationKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (IsInProgress(state.Status) && state.LeaseExpiresOn is { } expires && expires > now)
                return new(WorkTransitionOutcome.LeaseHeld, false);

            if (state.Status != WorkStatus.Created && !IsInProgress(state.Status))
                return new(WorkTransitionOutcome.Conflict, false);

            Finish(state, WorkStatus.Failed, now, error);
            return new(WorkTransitionOutcome.Applied);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Fences work that was created but never started.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> FenceAsync(OperationKey operationKey, CancellationToken cancellationToken = default)
        => FenceAsync(operationKey, new WorkError(WorkErrorCodes.NotStarted, "The work was not started before its deadline."), cancellationToken);

    /// <summary>
    /// Fences work that was created but never started using the specified safe error.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="error">The safe failure details.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> FenceAsync(OperationKey operationKey, WorkError error, CancellationToken cancellationToken = default)
    {
        ValidateError(error);
        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(operationKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (state.Status != WorkStatus.Created)
                return new(WorkTransitionOutcome.Conflict, false);

            Finish(state, WorkStatus.Failed, now, error);
            return new(WorkTransitionOutcome.Applied);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Requests cancellation of a work item.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="message">The optional user-safe cancellation message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> CancelAsync(OperationKey operationKey, string? message = null, CancellationToken cancellationToken = default)
        => _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(operationKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            var error = new WorkError(WorkErrorCodes.Canceled, message ?? "The work was canceled.");
            if (state.Status == WorkStatus.Created)
            {
                Finish(state, WorkStatus.Canceled, now, error);
                return new(WorkTransitionOutcome.Applied);
            }

            if (!IsInProgress(state.Status))
                return new(WorkTransitionOutcome.Conflict, false);

            if (state.CancelRequested)
                return new(WorkTransitionOutcome.Applied, false);

            state.CancelRequested = true;
            state.Error = error;
            return new(WorkTransitionOutcome.Applied);
        }, ct), cancellationToken);

    /// <summary>
    /// Acknowledges a cancellation request for the current worker lease.
    /// </summary>
    /// <param name="lease">The current lease.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transition outcome.</returns>
    public Task<WorkTransitionResult> AcknowledgeCancelAsync(WorkLease lease, CancellationToken cancellationToken = default)
    {
        lease.ThrowIfNull();
        return _invoker.InvokeAsync(this, (_, ct) => TransitionAsync(lease.WorkKey, (state, now) =>
        {
            if (IsFinished(state.Status))
                return new(WorkTransitionOutcome.AlreadyFinished, false);

            if (!IsInProgress(state.Status))
                return new(WorkTransitionOutcome.Conflict, false);

            if (!Owns(state, lease))
                return new(WorkTransitionOutcome.LeaseLost, false);

            if (!state.CancelRequested)
                return new(WorkTransitionOutcome.Conflict, false);

            Finish(state, WorkStatus.Canceled, now, state.Error ?? new WorkError(WorkErrorCodes.Canceled, "The work was canceled."));
            return new(WorkTransitionOutcome.Applied);
        }, ct), cancellationToken);
    }

    /// <summary>
    /// Deletes a finished work item.
    /// </summary>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when deleted or already absent; otherwise, an error.</returns>
    public Task<Result> DeleteAsync(OperationKey operationKey, CancellationToken cancellationToken = default)
        => _invoker.InvokeAsync<Result>(this, async (_, ct) =>
        {
            for (var attempt = 0; attempt < MaxConcurrencyAttempts; attempt++)
            {
                var state = await GetAsync(operationKey, ct).ConfigureAwait(false);
                if (state is null)
                    return Result.Success;

                if (!IsFinished(state.Status))
                    return Result.ConflictError($"Work '{operationKey.InstanceId}' cannot be deleted while its status is '{state.Status}'.");

                if (await Provider.DeleteAsync(state, ct).ConfigureAwait(false))
                    return Result.Success;
            }

            return Result.ConcurrencyError($"Work '{operationKey.InstanceId}' could not be deleted due to concurrent updates.");
        }, cancellationToken);

    /// <summary>
    /// Gets the state when both its type and owner match the current caller.
    /// </summary>
    /// <param name="typeName">The expected operation type.</param>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state only when type and owner checks pass; otherwise, <see langword="null"/>.</returns>
    public async Task<WorkState?> GetWithTypeAsync(string typeName, OperationKey operationKey, CancellationToken cancellationToken = default)
    {
        var state = await GetAsync(operationKey, cancellationToken).ConfigureAwait(false);
        if (state is null || state.TypeName != typeName)
            return null;

        if (state.User is null)
            return state;

        if (!ExecutionContext.TryGetCurrent(out var ec) || ec.User is null || ec.User.Type == AuthenticationType.Unauthenticated)
            return null;

        return state.User.Equals(ec.User) ? state : null;
    }

    /// <summary>
    /// Gets the state when its type and owner match the current caller.
    /// </summary>
    /// <typeparam name="T">The type used to derive the operation type name.</typeparam>
    /// <param name="operationKey">The complete operation identity.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The state only when type and owner checks pass; otherwise, <see langword="null"/>.</returns>
    public Task<WorkState?> GetWithTypeAsync<T>(OperationKey operationKey, CancellationToken cancellationToken = default) => GetWithTypeAsync(WorkArgs.GetTypeName<T>(), operationKey, cancellationToken);

    /// <summary>Applies a transition to the latest state with automatic expiry and optimistic-concurrency retries.</summary>
    private async Task<WorkTransitionResult> TransitionAsync(OperationKey operationKey, Func<WorkState, DateTimeOffset, TransitionDecision?> apply, CancellationToken cancellationToken)
    {
        operationKey.ThrowIfNull(nameof(operationKey));
        _options.Validate();

        for (var attempt = 0; attempt < MaxConcurrencyAttempts; attempt++)
        {
            var read = await Provider.GetAsync(operationKey, cancellationToken).ConfigureAwait(false);
            var state = read.State;
            ValidateStateIdentity(state, operationKey);
            var now = read.Now;
            if (state is null)
                return CreateResult(WorkTransitionOutcome.NotFound, null, observedOn: now);

            if (!IsFinished(state.Status) && now >= state.ExpiryOn)
            {
                Finish(state, WorkStatus.Expired, now, new WorkError(state.StartedOn.HasValue ? WorkErrorCodes.OutcomeUnknown : WorkErrorCodes.NotStarted, ExpiredMessage));
                state.UpdatedOn = now;
                var expired = await SaveAsync(state, now, "expire", cancellationToken).ConfigureAwait(false);
                if (expired is not null)
                    return CreateResult(WorkTransitionOutcome.Expired, expired, observedOn: now);

                continue;
            }

            if (state.Status == WorkStatus.Created && state.StartBy is { } startBy && now >= startBy)
            {
                Finish(state, WorkStatus.Expired, now, new WorkError(WorkErrorCodes.NotStarted, "The work was not started before its start deadline."));
                state.UpdatedOn = now;
                var expired = await SaveAsync(state, now, "expire", cancellationToken).ConfigureAwait(false);
                if (expired is not null)
                    return CreateResult(WorkTransitionOutcome.Expired, expired, observedOn: now);

                continue;
            }

            var decision = apply(state, now);
            if (decision is null)
                return CreateResult(WorkTransitionOutcome.Applied, state, observedOn: now);

            if (!decision.Write)
                return CreateResult(decision.Outcome, state, observedOn: now);

            if (decision.Outcome != WorkTransitionOutcome.Applied && decision.Outcome != WorkTransitionOutcome.Conflict)
                return CreateResult(decision.Outcome, state, observedOn: now);

            state.UpdatedOn = now;
            var saved = await SaveAsync(state, now, decision.Outcome.ToString(), cancellationToken).ConfigureAwait(false);
            if (saved is null)
                continue;

            var lease = decision.IssueLease ? new WorkLease(saved.OperationKey, saved.LeaseOwner!, saved.Attempt, saved.LeaseExpiresOn!.Value) : null;
            return CreateResult(decision.Outcome, saved, lease, decision.IsTakeover, now);
        }

        var latest = await Provider.GetAsync(operationKey, cancellationToken).ConfigureAwait(false);
        ValidateStateIdentity(latest.State, operationKey);
        return CreateResult(WorkTransitionOutcome.ConcurrencyExhausted, latest.State, observedOn: latest.Now);
    }

    /// <summary>Creates a transition result and records its outcome on the current activity.</summary>
    private static WorkTransitionResult CreateResult(WorkTransitionOutcome outcome, WorkState? state, WorkLease? lease = null, bool isTakeover = false, DateTimeOffset observedOn = default)
    {
        Activity.Current?.SetTag("work.outcome", outcome.ToString());
        return new WorkTransitionResult(outcome, state, lease, isTakeover) { ObservedOn = observedOn };
    }

    /// <summary>Saves a new revision and resolves a timed-out update only when that revision is observed.</summary>
    private async Task<WorkState?> SaveAsync(WorkState state, DateTimeOffset now, string operation, CancellationToken cancellationToken)
    {
        var revision = Guid.NewGuid().ToString("N");
        state.Revision = revision;

        try
        {
            return await Provider.UpdateAsync(state, GetTimeToLive(state, now), cancellationToken).ConfigureAwait(false) ? state : null;
        }
        catch (WorkOutcomeUnknownException)
        {
            var current = await Provider.GetAsync(state.OperationKey, cancellationToken).ConfigureAwait(false);
            ValidateStateIdentity(current.State, state.OperationKey);
            if (current.State?.Revision == revision)
                return current.State;

            throw;
        }
    }

    /// <summary>Calculates the record retention from its status and the provider time.</summary>
    private TimeSpan GetTimeToLive(WorkState state, DateTimeOffset now)
    {
        if (IsFinished(state.Status))
        {
            var untilStartBy = state.StartBy is { } startBy ? startBy - now : TimeSpan.Zero;
            return Max(_options.Retention, untilStartBy + _options.Retention);
        }

        var expiry = state.LeaseExpiresOn is { } leaseExpiry && leaseExpiry > state.ExpiryOn ? leaseExpiry : state.ExpiryOn;
        return Max(TimeSpan.FromMilliseconds(1), expiry - now + _options.Retention);
    }

    /// <summary>Calculates a lease deadline capped at the work expiry.</summary>
    private DateTimeOffset LeaseExpiry(DateTimeOffset now, DateTimeOffset expiry) => now.Add(_options.LeaseDuration) < expiry ? now.Add(_options.LeaseDuration) : expiry;

    /// <summary>Returns the greater of two durations.</summary>
    private static TimeSpan Max(TimeSpan first, TimeSpan second) => first >= second ? first : second;

    /// <summary>Marks a work item finished and clears its lease.</summary>
    private static void Finish(WorkState state, WorkStatus status, DateTimeOffset now, WorkError? error = null)
    {
        state.Status = status;
        state.FinishedOn = now;
        state.Error = error;
        state.LeaseOwner = null;
        state.LeaseExpiresOn = null;
    }

    /// <summary>Indicates whether the status is terminal.</summary>
    private static bool IsFinished(WorkStatus status) => WorkStatus.Finished.HasFlag(status);

    /// <summary>Indicates whether the status represents work in progress.</summary>
    private static bool IsInProgress(WorkStatus status) => (WorkStatus.InProgress & status) != 0;

    /// <summary>Validates that a provider returned the requested complete identity.</summary>
    private static void ValidateStateIdentity(WorkState? state, OperationKey operationKey)
    {
        if (state is not null && (state.OperationKey != operationKey || state.Id != operationKey.InstanceId))
            throw new InvalidDataException("The work provider returned a state with an identity that does not match the requested operation key.");
    }

    /// <summary>Indicates whether the lease owner and epoch match the current state.</summary>
    private static bool Owns(WorkState state, WorkLease lease) => state.Attempt == lease.Epoch && string.Equals(state.LeaseOwner, lease.Owner, StringComparison.Ordinal);

    /// <summary>Validates that the work error has a code and message.</summary>
    private static void ValidateError(WorkError error)
    {
        error.ThrowIfNull();
        error.Code.ThrowIfNullOrEmpty(nameof(error.Code));
        error.Message.ThrowIfNullOrEmpty(nameof(error.Message));
    }

    /// <summary>Validates an optional app-relative resource URI.</summary>
    private static void ValidateResourceLocation(string? value)
    {
        if (value is null)
            return;

        if (value.Length > 2048 || value.Length == 0 || value[0] != '/' || (value.Length > 1 && value[1] == '/') || value.Contains('\\') || value.Any(char.IsControl) || !Uri.TryCreate(value, UriKind.Relative, out var uri) || uri.IsAbsoluteUri)
            throw new ArgumentException("The resource location must be an app-relative URI beginning with a single slash.", nameof(value));
    }

    /// <summary>Describes a transition outcome and whether it must be persisted or issue a lease.</summary>
    private sealed record TransitionDecision(WorkTransitionOutcome Outcome, bool Write = true, bool IssueLease = false, bool IsTakeover = false);
}
