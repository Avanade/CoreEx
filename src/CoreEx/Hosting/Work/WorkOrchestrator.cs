namespace CoreEx.Hosting.Work;

/// <summary>
/// Represents the long-running work (see <see cref="WorkState"/>) tracking orchestrator.
/// </summary>
/// <param name="provider">The <see cref="IWorkProvider"/>.</param>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
/// <remarks>There are some basic consistency checks that occur for the methods to ensure largely correct usage (sequence of execution). The onus is primarily on the developer to ensure correct usage.</remarks>
public class WorkOrchestrator(IWorkProvider provider, JsonSerializerOptions? jsonSerializerOptions = null)
{
    private readonly WorkOrchestratorInvoker _invoker = WorkOrchestratorInvoker.Default;
    private TimeSpan? _expiryTimeSpan;

    /// <summary>
    /// Gets the default <see cref="ExpiryTimeSpan"/>.
    /// </summary>
    internal static TimeSpan DefaultExpiryTimeSpan => Internal.GetConfigurationValue<TimeSpan>("CoreEx:Hosting:Work:Expiry", TimeSpan.FromDays(1));

    /// <summary>
    /// Gets the <see cref="IWorkProvider"/>.
    /// </summary>
    public IWorkProvider Provider = provider.ThrowIfNull(nameof(provider));

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="JsonDefaults.SerializerOptions"/>.</remarks>
    public JsonSerializerOptions JsonSerializerOptions = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;

    /// <summary>
    /// Gets or sets the work expiry <see cref="TimeSpan"/>.
    /// </summary>
    /// <remarks>Defaults to configuration setting '<c>CoreEx:Hosting:Work:Expiry</c>'; otherwise, one (1) day.</remarks>
    public TimeSpan ExpiryTimeSpan
    {
        get => _expiryTimeSpan ??= DefaultExpiryTimeSpan;
        set => _expiryTimeSpan = value;
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
    /// <remarks>Will automatically set the <see cref="WorkState.Status"/> to <see cref="WorkStatus.Expired"/> when the work is <i>not</i> <see cref="WorkStatus.Finished"/> and has expired (see <see cref="WorkState.Expiry"/>).</remarks>
    public Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken = default) => _invoker.InvokeAsync(this, async (_, cancellationToken) =>
    {
        var ws = await Provider.GetAsync(id.ThrowIfNullOrEmpty(nameof(id)), cancellationToken).ConfigureAwait(false);
        if (ws is null)
            return null;

        // Automatically expire where it has been hanging around unfinished for too long.
        if (!WorkStatus.Finished.HasFlag(ws.Status) && Runtime.UtcNow >= ws.Expiry)
        {
            var wsr = await ExpireAsync(id, "The work has not finished within the expiry timeframe and is assumed to have expired.", cancellationToken).ConfigureAwait(false);
            return wsr.Value;
        }

        return ws;
    }, cancellationToken);

    /// <summary>
    /// Creates and persists a <b>new</b> <see cref="WorkState"/> with a <see cref="WorkStatus.Created"/> status.
    /// </summary>
    /// <param name="args">The <see cref="WorkArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="WorkState"/>.</returns>
    public Task<WorkState> CreateAsync(WorkArgs args, CancellationToken cancellationToken = default) => _invoker.InvokeAsync(this, async (_, cancellationToken) =>
    {
        args.ThrowIfNull();
        var now = Runtime.UtcNow;

        var ws = new WorkState()
        {
            Id = args.Id,
            TypeName = args.TypeName.ThrowIfNullOrEmpty(),
            Status = WorkStatus.Created,
            Created = now,
            Expiry = now.Add(args.Expiry ?? ExpiryTimeSpan),
            User = args.User,
            TraceParent = args.TraceParent,
            TraceState = args.TraceState
        };

        if (ws.User is null && ExecutionContext.TryGetCurrent(out var ec))
            ws.User = ec.User;

        if (string.IsNullOrEmpty(args.TraceParent) && Activity.Current is not null)
        {
            args.TraceParent = Activity.Current.Id;
            args.TraceState = Activity.Current.TraceStateString;
        }

        await Provider.CreateAsync(ws, cancellationToken).ConfigureAwait(false);

        return ws;
    }, cancellationToken);

    /// <summary>
    /// Starts a previously <see cref="WorkStatus.Created"/> <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Starting work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> StartAsync(string id, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result<WorkState>>(this, async (_, cancellationToken) =>
    {
        var ws = await Provider.GetAsync(id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);
        if (ws is null)
            return Result.NotFoundError();

        if (WorkStatus.Finished.HasFlag(ws.Status))
            return ws;

        if (ws.Status != WorkStatus.Created)
            throw new InvalidOperationException($"Work '{id}' cannot be started due to current status of '{ws.Status}'.");

        ws.Status = WorkStatus.Started;
        ws.Started = Runtime.UtcNow;

        await Provider.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
        return ws;
    }, cancellationToken);

    /// <summary>
    /// Sets a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/> to <see cref="WorkStatus.Indeterminate"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="reason">The indeterminate reason.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Setting work to indeterminate that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> IndeterminateAsync(string id, string reason, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result<WorkState>>(this, async (_, cancellationToken) =>
    {
        var ws = await GetAsync(id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);
        if (ws is null)
            return Result.NotFoundError();

        if (WorkStatus.Finished.HasFlag(ws.Status))
            return ws;

        if (!WorkStatus.InProgress.HasFlag(ws.Status))
            throw new InvalidOperationException($"Work '{id}' cannot be set to indeterminate due to current status of '{ws.Status}'.");

        ws.Status = WorkStatus.Indeterminate;
        ws.Indeterminate = Runtime.UtcNow;
        ws.Reason = reason.ThrowIfNullOrEmpty();

        await Provider.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
        return ws;
    }, cancellationToken);

    /// <summary>
    /// Completes a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Completing work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> CompleteAsync(string id, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result<WorkState>>(this, async (_, cancellationToken) =>
    {
        var ws = await GetAsync(id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);
        if (ws is null)
            return Result.NotFoundError();

        if (WorkStatus.Finished.HasFlag(ws.Status))
            return ws;

        if (!WorkStatus.InProgress.HasFlag(ws.Status))
            throw new InvalidOperationException($"Work '{id}' cannot be completed due to current status of '{ws.Status}'.");

        ws.Status = WorkStatus.Completed;
        ws.Finished = Runtime.UtcNow;
        ws.Reason = null;

        await Provider.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
        return ws;
    }, cancellationToken);

    /// <summary>
    /// Fails a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="reason">The failure reason.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Failing work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> FailAsync(string id, string reason, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result<WorkState>>(this, async (_, cancellationToken) =>
    {
        var ws = await GetAsync(id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);
        if (ws is null)
            return Result.NotFoundError();

        if (WorkStatus.Finished.HasFlag(ws.Status))
            return ws;

        if (!WorkStatus.InProgress.HasFlag(ws.Status))
            throw new InvalidOperationException($"Work '{id}' cannot be failed due to current status of '{ws.Status}'.");

        ws.Status = WorkStatus.Failed;
        ws.Finished = Runtime.UtcNow;
        ws.Reason = reason.ThrowIfNullOrEmpty();

        await Provider.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
        return ws;
    }, cancellationToken);

    /// <summary>
    /// Fails a previously <see cref="WorkStatus.Started"/> <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="exception">The unhandled <see cref="Exception"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Failing work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> FailAsync(string id, Exception exception, CancellationToken cancellationToken = default)
        => FailAsync(id, $"Work failed due to an unexpected error: {exception.ThrowIfNull(nameof(exception)).Message}", cancellationToken);

    /// <summary>
    /// Expires a <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Expiring work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> ExpireAsync(string id, string reason, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result<WorkState>>(this, async (_, cancellationToken) =>
    {
        var ws = await Provider.GetAsync(id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);
        if (ws is null)
            return Result.NotFoundError();

        if (WorkStatus.Finished.HasFlag(ws.Status))
            return ws;

        ws.Status = WorkStatus.Expired;
        ws.Finished = Runtime.UtcNow;
        ws.Reason = reason.ThrowIfNullOrEmpty();

        await Provider.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
        return ws;
    }, cancellationToken);

    /// <summary>
    /// Cancels a <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="reason">The cancellation reason.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The updated <see cref="WorkState"/>.</returns>
    /// <remarks>A <see cref="Result.NotFoundError"/> may also be returned. Cancelling work that has already <see cref="WorkStatus.Finished"/> will not update the <see cref="WorkStatus"/>.</remarks>
    public Task<Result<WorkState>> CancelAsync(string id, string reason, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result<WorkState>>(this, async (_, cancellationToken) =>
    {
        var ws = await Provider.GetAsync(id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);
        if (ws is null)
            return Result.NotFoundError();

        if (WorkStatus.Finished.HasFlag(ws.Status))
            return ws;

        ws.Status = WorkStatus.Canceled;
        ws.Finished = Runtime.UtcNow;
        ws.Reason = reason.ThrowIfNullOrEmpty(nameof(reason));

        await Provider.UpdateAsync(ws, cancellationToken).ConfigureAwait(false);
        return ws;
    }, cancellationToken);

    /// <summary>
    /// Deletes a <see cref="WorkState"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>A <see cref="Result.ConflictError"/> will be returned where attempting to delete work that is not <see cref="WorkStatus.Finished"/>.</remarks>
    public Task<Result> DeleteAsync(string id, CancellationToken cancellationToken = default) => _invoker.InvokeAsync<Result>(this, async (_, cancellationToken) =>
    {
        var ws = await GetAsync(id.ThrowIfNullOrEmpty(), cancellationToken).ConfigureAwait(false);
        if (ws is null)
            return Result.Success;

        if (!WorkStatus.Finished.HasFlag(ws.Status))
            return Result.ConflictError($"Work '{id}' can not be deleted due to current status of '{ws.Status}'; must be considered 'Finished'.");

        await Provider.DeleteAsync(id, cancellationToken).ConfigureAwait(false);
        return Result.Success;
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
    /// <returns>The <see cref="BinaryData"/> where found; otherwise, <c>null</c>.</returns>
    public Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken = default) => _invoker.InvokeAsync(this, (_, cancellationToken) =>
    {
        return Provider.GetDataAsync(id.ThrowIfNullOrEmpty(), cancellationToken);
    });

    /// <summary>
    /// Sets the result data as the specified <paramref name="value"/> serialized as JSON.
    /// </summary>
    /// <typeparam name="TValue">The <paramref name="value"/> <see cref="Type"/>.</typeparam>
    /// <param name="id">The work identifier.</param>
    /// <param name="value">The value to JSON serialize as the result data.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task SetDataValueAsync<TValue>(string id, TValue value, CancellationToken cancellationToken = default)
    {
        var bd = BinaryData.FromObjectAsJson<TValue>(value, JsonSerializerOptions);
        await SetDataAsync(id, bd, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the result data as the specified <see cref="BinaryData"/>.
    /// </summary>
    /// <param name="id">The work identifier.</param>
    /// <param name="data">The <see cref="BinaryData"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public async Task SetDataAsync(string id, BinaryData data, CancellationToken cancellationToken = default)
    {
        if (await GetAsync(id, cancellationToken).ConfigureAwait(false) is null)
            throw new ArgumentException($"Work '{id}' does not exist.", nameof(id));

        await _invoker.InvokeAsync(this, async (_, cancellationToken) =>
        {
            await Provider.SetDataAsync(id.ThrowIfNullOrEmpty(), data.ThrowIfNull(), cancellationToken).ConfigureAwait(false);
        }, cancellationToken).ConfigureAwait(false);
    }

    #region WithType

    /// <summary>
    /// Gets the <see cref="WorkState"/> for the specified <paramref name="typeName"/> and <paramref name="id"/>.
    /// </summary>
    /// <param name="typeName">The <see cref="WorkState.TypeName"/>.</param>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="WorkState"/> where found; otherwise, <c>null</c>.</returns>
    /// <remarks>Will automatically set the <see cref="WorkState.Status"/> to <see cref="WorkStatus.Expired"/> when the work is <i>not</i> <see cref="WorkStatus.Finished"/> and has expired (see <see cref="WorkState.Expiry"/>).
    /// <para>Additionally, the <paramref name="typeName"/> must equal the <see cref="WorkState.TypeName"/>; and, if <see cref="CheckUser"/> is <see langword="true"/> then the <see cref="WorkState.User"/> must equal the <see cref="ExecutionContext.User"/>
    /// ensuring that the initiating user can only interact with their <see cref="WorkState"/>. Where the aforementioned does not equal then a <see langword="null"/> will be returned.</para></remarks>
    public async Task<WorkState?> GetWithTypeAsync(string typeName, string id, CancellationToken cancellationToken = default)
    {
        var ws = await GetAsync(id, cancellationToken).ConfigureAwait(false);
        if (ws is null || ws.TypeName != typeName)
            return null;

        return CheckUser && ws.User is not null && ExecutionContext.TryGetCurrent(out var ec) && ec.User is not null && ws.User != ec.User ? null : ws;
    }

    /// <summary>
    /// Gets the <see cref="WorkState"/> for the specified <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type"/> to infer the <see cref="WorkState.TypeName"/> enabling state separation.</typeparam>
    /// <param name="id">The work identifier.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="WorkState"/> where found; otherwise, <c>null</c>.</returns>
    /// <remarks>Will automatically set the <see cref="WorkState.Status"/> to <see cref="WorkStatus.Expired"/> when the work is <i>not</i> <see cref="WorkStatus.Finished"/> and has expired (see <see cref="WorkState.Expiry"/>).
    /// <para>Additionally, the <typeparamref name="T"/> must equal the <see cref="WorkState.TypeName"/>; and, if <see cref="CheckUser"/> is <see langword="true"/> then the <see cref="WorkState.User"/> must equal the <see cref="ExecutionContext.User"/>
    /// ensuring that the initiating user can only interact with their <see cref="WorkState"/>. Where the aforementioned does not equal then a <see langword="null"/> will be returned.</para></remarks>
    public Task<WorkState?> GetWithTypeAsync<T>(string id, CancellationToken cancellationToken = default) => GetWithTypeAsync(WorkArgs.GetTypeName<T>(), id, cancellationToken);

    #endregion
}