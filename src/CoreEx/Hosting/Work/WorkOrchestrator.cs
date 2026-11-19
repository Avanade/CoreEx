namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents the long-running work (see <see cref="WorkState"/>) tracking orchestrator.
/// </summary>
/// <param name="provider">The <see cref="IWorkProvider"/>.</param>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
/// <remarks>Each state transition is applied as an optimistic-concurrency (compare-and-set) loop against the <see cref="IWorkProvider"/>: the current state is read, auto-expiry is applied, the transition is validated, and the
/// update only succeeds where no other writer has changed the state in the interim. As such, concurrent transitions are linearized; for example, exactly one concurrent <see cref="StartAsync"/> succeeds, and a stale
/// transition can never overwrite a <see cref="WorkStatus.Finished"/> state. Invalid transitions return a <see cref="Result.ConflictError"/>; exhausted retries return a <see cref="Result.ConcurrencyError"/>.</remarks>
public class WorkOrchestrator(IWorkProvider provider, JsonSerializerOptions? jsonSerializerOptions = null)
{
    private const int MaxAttempts = 10;
    private const string ExpiredReason = "The work has not finished within the expiry timeframe and is assumed to have expired.";
    private readonly WorkOrchestratorInvoker _invoker = WorkOrchestratorInvoker.Default;
    private TimeSpan? _expiryTimeSpan;
    private TimeSpan? _retentionTimeSpan;

    /// <summary>
    /// Gets the default <see cref="ExpiryTimeSpan"/>.
    /// </summary>
    internal static TimeSpan DefaultExpiryTimeSpan => Internal.GetConfigurationValue<TimeSpan>("CoreEx:Hosting:Work:Expiry", TimeSpan.FromDays(1));

    /// <summary>
    /// Gets the default <see cref="RetentionTimeSpan"/>.
    /// </summary>
    internal static TimeSpan DefaultRetentionTimeSpan => Internal.GetConfigurationValue<TimeSpan>("CoreEx:Hosting:Work:Retention", TimeSpan.FromDays(1));

    /// <summary>
    /// Gets the <see cref="IWorkProvider"/>.
    /// </summary>
    public IWorkProvider Provider { get; } = provider.ThrowIfNull(nameof(provider));

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="JsonDefaults.SerializerOptions"/>.</remarks>
    public JsonSerializerOptions JsonSerializerOptions { get; } = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;

    /// <summary>
    /// Gets or sets the work expiry <see cref="TimeSpan"/>; being the maximum duration that work may remain unfinished before it is automatically <see cref="WorkStatus.Expired"/>.
    /// </summary>
    /// <remarks>Defaults to configuration setting '<c>CoreEx:Hosting:Work:Expiry</c>'; otherwise, one (1) day.</remarks>
    public TimeSpan ExpiryTimeSpan
    {
        get => _expiryTimeSpan ??= DefaultExpiryTimeSpan;
        set => _expiryTimeSpan = value;
    }

    /// <summary>
    /// Gets or sets the retention <see cref="TimeSpan"/>; being the minimum duration that the work is retained after it has finished (or would have expired).
    /// </summary>
    /// <remarks>Defaults to configuration setting '<c>CoreEx:Hosting:Work:Retention</c>'; otherwise, one (1) day. The record time-to-live is the remaining expiry plus the retention whilst executing, and the retention once finished.</remarks>
    public TimeSpan RetentionTimeSpan
    {
        get => _retentionTimeSpan ??= DefaultRetentionTimeSpan;
        set => _retentionTimeSpan = value > TimeSpan.Zero ? value : throw new ArgumentOutOfRangeException(nameof(value), "The retention must be greater than zero.");
    }

    /// <summary>
    /// Indicates whether to check the <see cref="WorkState.User"/> where not <see langword="null"/> where performing a <see cref="GetWithTypeAsync(string, string, CancellationToken)"/> or <see cref="GetWithTypeAsync{T}(string, CancellationToken)"/>.
    /// </summary>
    public bool CheckUser { get; set; } = true;

    /// <summary>
    /// Gets the <see cref="WorkState"/> for the specified <paramref name="id"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="WorkState"/> where found; otherwise, <see langword="null"/>.</returns>
    /// <remarks>Will automatically (and atomically) set the <see cref="WorkState.Status"/> to <see cref="WorkStatus.Expired"/> when the work is <i>not</i> <see cref="WorkStatus.Finished"/> and has expired (see <see cref="WorkState.Expiry"/>).</remarks>
    public Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken = default) => _invoker.InvokeAsync(this, async (_, cancellationToken) =>
    {
        var result = await TransitionAsync(id, (_, _) => false, null, cancellationToken).ConfigureAwait(false);
        return result.IsFailureOfType<NotFoundException>() ? null : result.Value;
    }, cancellationToken);

    /// <summary>
    /// Atomically creates and persists a <b>new</b> <see cref="WorkState"/> with a <see cref="WorkStatus.Created"/> status.
    /// </summary>
    /// <param name="args">The <see cref="WorkArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.ConflictError"/> is returned where the <see cref="WorkArgs.Id"/> already exists.</remarks>
    public Task<Result<WorkState>> CreateAsync(WorkArgs args, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result<WorkState>>(this, async (_, cancellationToken) =>
    {
        args.ThrowIfNull();
        var now = Runtime.UtcNow;
        var traceParent = args.TraceParent;
        var traceState = args.TraceState;
        if (string.IsNullOrEmpty(traceParent) && Activity.Current is not null)
        {
            traceParent = Activity.Current.Id;
            traceState = Activity.Current.TraceStateString;
        }

        var ws = new WorkState()
        {
            Id = args.Id,
            TypeName = args.TypeName.ThrowIfNullOrEmpty(),
            Status = WorkStatus.Created,
            Created = now,
            Expiry = now.Add(args.Expiry ?? ExpiryTimeSpan),
            User = args.User ?? (ExecutionContext.TryGetCurrent(out var ec) ? ec.User : null),
            TraceParent = traceParent,
            TraceState = traceState
        };

        if (await Provider.CreateAsync(ws, GetTimeToLive(ws, now), cancellationToken).ConfigureAwait(false))
            return ws;

        return Result.ConflictError($"Work '{args.Id}' already exists.");
    }, cancellationToken);

    /// <summary>
    /// Atomically starts (claims) a previously <see cref="WorkStatus.Created"/> <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may be returned. A <see cref="Result.ConflictError"/> is returned where already started; i.e. exactly one concurrent caller can start the work. Starting work that has already
    /// <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>; the caller should verify the returned <see cref="WorkState.Status"/>.</remarks>
    public Task<Result<WorkState>> StartAsync(string id, CancellationToken cancellationToken = default) => _invoker.InvokeAsync(this, (_, cancellationToken) => TransitionAsync(id, (ws, now) =>
    {
        if (IsFinished(ws.Status))
            return false;

        if (ws.Status != WorkStatus.Created)
            return Result.ConflictError($"Work '{id}' cannot be started due to current status of '{ws.Status}'.");

        ws.Status = WorkStatus.Started;
        ws.Started = now;
        return true;
    }, null, cancellationToken), cancellationToken);

    /// <summary>
    /// Sets a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/> to <see cref="WorkStatus.Indeterminate"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="reason">The indeterminate reason.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> (not in progress) may be returned. Setting work to indeterminate that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> IndeterminateAsync(string id, string reason, CancellationToken cancellationToken = default)
    {
        reason.ThrowIfNullOrEmpty(nameof(reason));
        return _invoker.InvokeAsync(this, (_, cancellationToken) => TransitionAsync(id, (ws, now) =>
        {
            if (IsFinished(ws.Status))
                return false;

            if (!WorkStatus.InProgress.HasFlag(ws.Status))
                return Result.ConflictError($"Work '{id}' cannot be set to indeterminate due to current status of '{ws.Status}'.");

            ws.Status = WorkStatus.Indeterminate;
            ws.Indeterminate = now;
            ws.Reason = reason;
            return true;
        }, null, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Completes a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> (not in progress) may be returned. Completing work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>;
    /// the caller should verify the returned <see cref="WorkState.Status"/>.</remarks>
    public Task<Result<WorkState>> CompleteAsync(string id, CancellationToken cancellationToken = default) => CompleteAsync(id, null, cancellationToken);

    /// <summary>
    /// Completes a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/> whilst atomically setting the result <paramref name="data"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="data">The result data; <see langword="null"/> leaves any existing data unchanged.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> (not in progress) may be returned. Completing work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>
    /// or data; the caller should verify the returned <see cref="WorkState.Status"/>.</remarks>
    public Task<Result<WorkState>> CompleteAsync(string id, BinaryData? data, CancellationToken cancellationToken = default) => _invoker.InvokeAsync(this, (_, cancellationToken) => TransitionAsync(id, (ws, now) =>
    {
        if (IsFinished(ws.Status))
            return false;

        if (!WorkStatus.InProgress.HasFlag(ws.Status))
            return Result.ConflictError($"Work '{id}' cannot be completed due to current status of '{ws.Status}'.");

        ws.Status = WorkStatus.Completed;
        ws.Finished = now;
        ws.Reason = null;
        return true;
    }, data, cancellationToken), cancellationToken);

    /// <summary>
    /// Fails a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="reason">The failure reason.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> (not in progress) may be returned. Failing work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> FailAsync(string id, string reason, CancellationToken cancellationToken = default)
    {
        reason.ThrowIfNullOrEmpty(nameof(reason));
        return _invoker.InvokeAsync(this, (_, cancellationToken) => TransitionAsync(id, (ws, now) =>
        {
            if (IsFinished(ws.Status))
                return false;

            if (!WorkStatus.InProgress.HasFlag(ws.Status))
                return Result.ConflictError($"Work '{id}' cannot be failed due to current status of '{ws.Status}'.");

            ws.Status = WorkStatus.Failed;
            ws.Finished = now;
            ws.Reason = reason;
            return true;
        }, null, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Fails a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/> due to an unhandled <paramref name="exception"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="exception">The unhandled <see cref="Exception"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>The <see cref="WorkState.Reason"/> is intentionally generic as it is visible to the initiating user; the <paramref name="exception"/> details are not persisted and should be logged by the caller.
    /// <para>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> (not in progress) may be returned. Failing work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</para></remarks>
    public Task<Result<WorkState>> FailAsync(string id, Exception exception, CancellationToken cancellationToken = default)
    {
        exception.ThrowIfNull(nameof(exception));
        return FailAsync(id, "Work failed due to an unexpected error.", cancellationToken);
    }

    /// <summary>
    /// Expires an executing <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="reason">The expiry reason.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may be returned. Expiring work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> ExpireAsync(string id, string reason, CancellationToken cancellationToken = default) => TerminateAsync(id, WorkStatus.Expired, reason, cancellationToken);

    /// <summary>
    /// Cancels an executing <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may be returned. Cancelling work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> CancelAsync(string id, string reason, CancellationToken cancellationToken = default) => TerminateAsync(id, WorkStatus.Canceled, reason, cancellationToken);

    /// <summary>
    /// Terminates an executing <see cref="WorkState"/> with the specified <paramref name="status"/>.
    /// </summary>
    private Task<Result<WorkState>> TerminateAsync(string id, WorkStatus status, string reason, CancellationToken cancellationToken)
    {
        reason.ThrowIfNullOrEmpty(nameof(reason));
        return _invoker.InvokeAsync(this, (_, cancellationToken) => TransitionAsync(id, (ws, now) =>
        {
            if (IsFinished(ws.Status))
                return false;

            ws.Status = status;
            ws.Finished = now;
            ws.Reason = reason;
            return true;
        }, null, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Deletes a <see cref="WorkState"/> (and its result data).
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>A <see cref="Result.ConflictError"/> will be returned where attempting to delete work that is not <see cref="WorkStatus.Finished"/>. Deleting work that does not exist is considered successful.</remarks>
    public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result>(this, async (_, cancellationToken) =>
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var result = await TransitionAsync(id, (_, _) => false, null, cancellationToken).ConfigureAwait(false);
            if (result.IsFailureOfType<NotFoundException>())
                return Result.Success;

            if (result.IsFailure)
                return result.Error;

            var ws = result.Value;
            if (!IsFinished(ws.Status))
                return Result.ConflictError($"Work '{id}' can not be deleted due to current status of '{ws.Status}'; must be considered 'Finished'.");

            if (await Provider.DeleteAsync(ws, cancellationToken).ConfigureAwait(false))
                return Result.Success;
        }

        return Result.ConcurrencyError($"Work '{id}' could not be deleted due to concurrent updates.");
    }, cancellationToken);

    /// <summary>
    /// Gets the result data as <see cref="BinaryData"/> and then JSON deserializes to the specified <typeparamref name="TValue"/>.
    /// </summary>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The deserialized value where found; otherwise, <see langword="default"/>.</returns>
    public async Task<TValue?> GetDataValueAsync<TValue>(string id, CancellationToken cancellationToken = default)
    {
        var data = await GetDataAsync(id, cancellationToken).ConfigureAwait(false);
        if (data is null)
            return default;

        return JsonSerializer.Deserialize<TValue>(data, JsonSerializerOptions);
    }

    /// <summary>
    /// Gets the result data as a <see cref="BinaryData"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="BinaryData"/> where found; otherwise, <see langword="null"/>.</returns>
    public Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken = default)
        => _invoker.InvokeAsync(this, (_, cancellationToken) => Provider.GetDataAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken), cancellationToken);

    /// <summary>
    /// Sets the result data as the specified <paramref name="value"/> serialized as JSON.
    /// </summary>
    /// <typeparam name="TValue">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="id">The work identifier.</param>
    /// <param name="value">The value to JSON serialize as the result data.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> (already finished) may be returned.</remarks>
    public Task<Result> SetDataValueAsync<TValue>(string id, TValue value, CancellationToken cancellationToken = default)
        => SetDataAsync(id, BinaryData.FromObjectAsJson(value, JsonSerializerOptions), cancellationToken);

    /// <summary>
    /// Atomically sets the result data as the specified <see cref="BinaryData"/> whilst the work is executing (not <see cref="WorkStatus.Finished"/>).
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="data">The <see cref="BinaryData"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Result"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> or <see cref="Result.ConflictError"/> (already finished) may be returned. Use <see cref="CompleteAsync(string, BinaryData?, CancellationToken)"/> to set the data and complete atomically.</remarks>
    public Task<Result> SetDataAsync(string id, BinaryData data, CancellationToken cancellationToken = default)
    {
        data.ThrowIfNull(nameof(data));
        return _invoker.InvokeAsync<Result>(this, async (_, cancellationToken) =>
        {
            var result = await TransitionAsync(id, (ws, _) => IsFinished(ws.Status) ? Result.ConflictError($"Work '{id}' data cannot be set due to current status of '{ws.Status}'.") : true, data, cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? Result.Success : result.Error;
        }, cancellationToken);
    }

    /// <summary>
    /// Performs the optimistic-concurrency transition loop: read, auto-expire, apply and conditionally update.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="apply">The transition; returns <see langword="true"/> where the state was changed, <see langword="false"/> for no change, or a failure.</param>
    /// <param name="data">The optional result data to persist where changed.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The resulting <see cref="WorkState"/>.</returns>
    private async Task<Result<WorkState>> TransitionAsync(string id, Func<WorkState, DateTimeOffset, Result<bool>> apply, BinaryData? data, CancellationToken cancellationToken)
    {
        id.ThrowIfNullOrEmpty(nameof(id));
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var ws = await Provider.GetAsync(id, cancellationToken).ConfigureAwait(false);
            if (ws is null)
                return Result.NotFoundError($"Work '{id}' was not found.");

            var now = Runtime.UtcNow;
            var expired = false;
            if (!IsFinished(ws.Status) && now >= ws.Expiry)
            {
                ws.Status = WorkStatus.Expired;
                ws.Finished = now;
                ws.Reason = ExpiredReason;
                expired = true;
            }

            var applied = apply(ws, now);
            if (applied.IsFailure)
                return applied.Error;

            if (!expired && !applied.Value)
                return ws;

            if (await Provider.UpdateAsync(ws, applied.Value ? data : null, GetTimeToLive(ws, now), cancellationToken).ConfigureAwait(false))
                return ws;
        }

        return Result.ConcurrencyError($"Work '{id}' could not be updated due to concurrent updates.");
    }

    /// <summary>
    /// Indicates whether the <paramref name="status"/> is <see cref="WorkStatus.Finished"/>.
    /// </summary>
    private static bool IsFinished(WorkStatus status) => WorkStatus.Finished.HasFlag(status);

    /// <summary>
    /// Gets the record time-to-live: the remaining expiry plus retention whilst executing; otherwise, the retention.
    /// </summary>
    private TimeSpan GetTimeToLive(WorkState ws, DateTimeOffset now) => IsFinished(ws.Status) || ws.Expiry <= now ? RetentionTimeSpan : ws.Expiry - now + RetentionTimeSpan;

    /// <summary>
    /// Indicates whether the <paramref name="owner"/> and <paramref name="current"/> users represent the same identity (type and identifier; otherwise, user name).
    /// </summary>
    private static bool IsSameUser(AuthenticationUser owner, AuthenticationUser current)
        => owner.Type == current.Type && (owner.Id is not null || current.Id is not null ? string.Equals(owner.Id, current.Id, StringComparison.Ordinal) : string.Equals(owner.UserName, current.UserName, StringComparison.Ordinal));

    #region WithType

    /// <summary>
    /// Gets the <see cref="WorkState"/> for the specified <paramref name="typeName"/> and <paramref name="id"/>.
    /// </summary>
    /// <param name="typeName">The <see cref="WorkState.TypeName"/>.</param>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="WorkState"/> where found; otherwise, <see langword="null"/>.</returns>
    /// <remarks>Will automatically set the <see cref="WorkState.Status"/> to <see cref="WorkStatus.Expired"/> when the work is <i>not</i> <see cref="WorkStatus.Finished"/> and has expired (see <see cref="WorkState.Expiry"/>).
    /// <para>Additionally, the <paramref name="typeName"/> must equal the <see cref="WorkState.TypeName"/>; and, if <see cref="CheckUser"/> is <see langword="true"/> then the <see cref="WorkState.User"/> must represent the same identity
    /// (<see cref="AuthenticationUser.Type"/> and <see cref="AuthenticationUser.Id"/>; otherwise, <see cref="AuthenticationUser.UserName"/>) as the <see cref="ExecutionContext.User"/> ensuring that the initiating user can only interact
    /// with their <see cref="WorkState"/>. Where the aforementioned does not equal then a <see langword="null"/> will be returned. Where there is no current <see cref="ExecutionContext.User"/> (i.e. a system or background
    /// context) the user check is not performed.</para></remarks>
    public async Task<WorkState?> GetWithTypeAsync(string typeName, string id, CancellationToken cancellationToken = default)
    {
        var ws = await GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (ws is null || ws.TypeName != typeName)
            return null;

        return CheckUser && ws.User is not null && ExecutionContext.TryGetCurrent(out var ec) && ec.User is not null && !IsSameUser(ws.User, ec.User) ? null : ws;
    }

    /// <summary>
    /// Gets the <see cref="WorkState"/> for the specified <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to infer the <see cref="WorkState.TypeName"/> enabling state separation.</typeparam>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="WorkState"/> where found; otherwise, <see langword="null"/>.</returns>
    /// <remarks>See <see cref="GetWithTypeAsync(string, string, CancellationToken)"/> for the type and user checks performed.</remarks>
    public Task<WorkState?> GetWithTypeAsync<T>(string id, CancellationToken cancellationToken = default) => GetWithTypeAsync(WorkArgs.GetTypeName<T>(), id, cancellationToken);

    #endregion
}
