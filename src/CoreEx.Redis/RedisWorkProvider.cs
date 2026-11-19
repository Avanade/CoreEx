namespace CoreEx.Redis;

/// <summary>
/// Provides a Redis-backed <see cref="IWorkProvider"/> with atomic create and optimistic-concurrency updates.
/// </summary>
/// <remarks>Each work record is a Redis hash containing serialized state, incarnation, and version. All writes use embedded Lua scripts and target the primary. Redis replicates asynchronously, so failover can lose recently acknowledged writes; this provider does not attempt to narrow that window.</remarks>
public sealed class RedisWorkProvider : IWorkProvider
{
    private static readonly string _create = LoadScript("WorkCreate.lua");
    private static readonly string _get = LoadScript("WorkGet.lua");
    private static readonly string _update = LoadScript("WorkUpdate.lua");
    private static readonly string _delete = LoadScript("WorkDelete.lua");
    private static readonly DateTimeOffset _unixEpoch = DateTimeOffset.UnixEpoch;
    private readonly IConnectionMultiplexer _connection;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly RedisWorkProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisWorkProvider"/> class.
    /// </summary>
    /// <param name="connection">The host-owned Redis connection.</param>
    /// <param name="options">The provider options.</param>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    public RedisWorkProvider(IConnectionMultiplexer connection, RedisWorkProviderOptions? options = null, JsonSerializerOptions? jsonSerializerOptions = null)
    {
        _connection = connection.ThrowIfNull();
        _options = options ?? new RedisWorkProviderOptions();
        _options.Validate();
        _jsonSerializerOptions = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;
    }

    /// <summary>Loads an embedded Redis script.</summary>
    private static string LoadScript(string name)
    {
        using var reader = new StreamReader(Resource.GetStream<RedisWorkProvider>($"Scripts.{name}"));
        return reader.ReadToEnd();
    }

    /// <summary>Creates the Redis key for a work identifier.</summary>
    private RedisKey Key(string id) => $"{_options.KeyPrefix}{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ThrowIfNullOrEmpty(nameof(id)))))}";

    /// <summary>Converts a positive time-to-live to Redis milliseconds.</summary>
    private static long Milliseconds(TimeSpan timeToLive)
        => timeToLive > TimeSpan.Zero ? Math.Max(1, (long)Math.Ceiling(timeToLive.TotalMilliseconds)) : throw new ArgumentOutOfRangeException(nameof(timeToLive), "The time-to-live must be greater than zero.");

    /// <summary>Executes a bounded Redis operation and identifies ambiguous write timeouts.</summary>
    private async Task<T> ExecuteAsync<T>(Func<IDatabase, Task<T>> operation, CancellationToken cancellationToken, bool isWrite = false, string? id = null, string? operationName = null)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_options.OperationTimeout);
        try
        {
            return await operation(_connection.GetDatabase()).WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException exception) when (!cancellationToken.IsCancellationRequested && cts.IsCancellationRequested)
        {
            if (isWrite)
                throw new WorkOutcomeUnknownException(id ?? string.Empty, operationName ?? "write", exception);

            throw new TimeoutException("The Redis work-provider read exceeded its operation timeout.", exception);
        }
        catch (RedisTimeoutException exception)
        {
            if (isWrite)
                throw new WorkOutcomeUnknownException(id ?? string.Empty, operationName ?? "write", exception);

            throw new TimeoutException("The Redis work-provider read exceeded its operation timeout.", exception);
        }
    }

    /// <summary>Executes a bounded write whose timeout is reported as an unknown outcome.</summary>
    private Task<RedisResult> ExecuteWriteAsync(string id, string operationName, Func<IDatabase, Task<RedisResult>> operation, CancellationToken cancellationToken)
        => ExecuteAsync(operation, cancellationToken, isWrite: true, id, operationName);

    /// <inheritdoc/>
    public async Task<WorkSnapshot> GetAsync(string id, CancellationToken cancellationToken)
    {
        var key = Key(id);
        var useLocalClock = _options.TimeProvider is not null;
        RedisValue[] args = [useLocalClock ? 1 : 0];
        var values = (RedisResult[]?)await ExecuteAsync(
            db => db.ScriptEvaluateAsync(_get, [key], args, CommandFlags.DemandMaster),
            cancellationToken).ConfigureAwait(false) ?? throw new InvalidDataException("Redis returned no work snapshot.");

        var now = useLocalClock
            ? _options.TimeProvider!.GetUtcNow()
            : _unixEpoch.AddSeconds((long)values[3]).AddTicks((long)values[4] * 10);
        if (values[0].IsNull && values[1].IsNull && values[2].IsNull)
            return new WorkSnapshot(null, now);
        if (values[0].IsNull || values[1].IsNull || values[2].IsNull)
            throw new InvalidDataException("The persisted work record is missing its state, incarnation, or version.");

        var state = JsonSerializer.Deserialize<WorkState>((byte[])values[0]!, _jsonSerializerOptions)
            ?? throw new InvalidOperationException("The persisted work state could not be deserialized.");
        state.ETag = $"{(string?)values[1]}:{(long)values[2]}";
        return new WorkSnapshot(state, now);
    }

    /// <inheritdoc/>
    public async Task<bool> CreateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        var id = state.ThrowIfNull().Id.ThrowIfNullOrEmpty(nameof(state.Id));
        var key = Key(id);
        var incarnation = Guid.NewGuid().ToString("N");
        RedisValue[] args = [JsonSerializer.SerializeToUtf8Bytes(state, _jsonSerializerOptions), Milliseconds(timeToLive), incarnation];
        if ((int)await ExecuteWriteAsync(id, "create", db => db.ScriptEvaluateAsync(_create, [key], args, CommandFlags.DemandMaster), cancellationToken).ConfigureAwait(false) != 1)
            return false;

        state.ETag = $"{incarnation}:1";
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        var id = state.ThrowIfNull().Id.ThrowIfNullOrEmpty(nameof(state.Id));
        var key = Key(id);
        var expectedETag = state.ETag ?? string.Empty;
        RedisValue[] args = [expectedETag, JsonSerializer.SerializeToUtf8Bytes(state, _jsonSerializerOptions), Milliseconds(timeToLive)];
        var version = (long)await ExecuteWriteAsync(id, "update", db => db.ScriptEvaluateAsync(_update, [key], args, CommandFlags.DemandMaster), cancellationToken).ConfigureAwait(false);
        if (version <= 0)
            return false;

        var separator = expectedETag.LastIndexOf(':');
        if (separator <= 0)
            throw new InvalidDataException("The work ETag is invalid.");
        state.ETag = $"{expectedETag[..separator]}:{version.ToString(CultureInfo.InvariantCulture)}";
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(WorkState state, CancellationToken cancellationToken)
    {
        var id = state.ThrowIfNull().Id.ThrowIfNullOrEmpty(nameof(state.Id));
        var key = Key(id);
        RedisValue[] args = [state.ETag ?? string.Empty];
        return (int)await ExecuteWriteAsync(id, "delete", db => db.ScriptEvaluateAsync(_delete, [key], args, CommandFlags.DemandMaster), cancellationToken).ConfigureAwait(false) == 1;
    }
}
