namespace CoreEx.Redis;

/// <summary>
/// Provides a Redis-backed <see cref="IWorkProvider"/> with atomic create and optimistic-concurrency (compare-and-set) update semantics.
/// </summary>
/// <remarks>Each work record is a single Redis hash (state, version and optional data) written only via embedded Lua scripts so that the state and data are always updated atomically. All operations target the primary
/// with a bounded acknowledgement wait. Redis must be configured so that these keys are not evicted (for example <c>maxmemory-policy noeviction</c>); asynchronous replication means a failover may lose a recent write.</remarks>
public sealed class RedisWorkProvider : IWorkProvider
{
    private static readonly string _create = LoadScript("WorkCreate.lua");
    private static readonly string _update = LoadScript("WorkUpdate.lua");
    private static readonly string _delete = LoadScript("WorkDelete.lua");
    private readonly IConnectionMultiplexer _connection;
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly string _keyPrefix;
    private readonly TimeSpan _operationTimeout;
    private readonly int? _maxDataSize;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisWorkProvider"/> class.
    /// </summary>
    /// <param name="connection">The host-owned <see cref="IConnectionMultiplexer"/>.</param>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>; defaults to <see cref="JsonDefaults.SerializerOptions"/>.</param>
    /// <param name="keyPrefix">The key prefix enabling separation within a shared Redis; defaults to <c>CoreEx:work:v1:</c>.</param>
    /// <param name="operationTimeout">The maximum duration to wait for each Redis operation; defaults to five seconds.</param>
    /// <param name="maxDataSize">The maximum result data size in bytes. Defaults to 512 KiB; use <see langword="null"/> for no configured limit.</param>
    public RedisWorkProvider(IConnectionMultiplexer connection, JsonSerializerOptions? jsonSerializerOptions = null, string? keyPrefix = null, TimeSpan? operationTimeout = null, int? maxDataSize = 512 * 1024)
    {
        _connection = connection.ThrowIfNull();
        _jsonSerializerOptions = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;
        _keyPrefix = keyPrefix ?? "CoreEx:work:v1:";
        _operationTimeout = operationTimeout ?? TimeSpan.FromSeconds(5);
        if (_operationTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(operationTimeout));

        if (maxDataSize is <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxDataSize));

        _maxDataSize = maxDataSize;
    }

    /// <summary>
    /// Loads an embedded Lua script from the <c>Scripts</c> folder.
    /// </summary>
    private static string LoadScript(string name)
    {
        using var reader = new StreamReader(Resource.GetStream<RedisWorkProvider>($"Scripts.{name}"));
        return reader.ReadToEnd();
    }

    /// <summary>
    /// Gets the bounded-length physical key for the work identifier.
    /// </summary>
    private RedisKey Key(string id) => $"{_keyPrefix}{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(id.ThrowIfNullOrEmpty(nameof(id)))))}";

    /// <summary>
    /// Converts the <paramref name="timeToLive"/> to positive whole milliseconds.
    /// </summary>
    private static long Milliseconds(TimeSpan timeToLive) => timeToLive > TimeSpan.Zero ? Math.Max(1, (long)Math.Ceiling(timeToLive.TotalMilliseconds)) : throw new ArgumentOutOfRangeException(nameof(timeToLive), "The time-to-live must be greater than zero.");

    /// <summary>
    /// Executes one primary-side operation with a bounded acknowledgement wait.
    /// </summary>
    private async Task<T> ExecuteAsync<T>(Func<IDatabase, Task<T>> operation, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(_operationTimeout);
        return await operation(_connection.GetDatabase()).WaitAsync(cts.Token).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var key = Key(id);
        var values = await ExecuteAsync(db => db.HashGetAsync(key, ["s", "v"], CommandFlags.DemandMaster), cancellationToken).ConfigureAwait(false);
        if (values[0].IsNull || values[1].IsNull)
            return null;

        var state = JsonSerializer.Deserialize<WorkState>((byte[])values[0]!, _jsonSerializerOptions) ?? throw new InvalidOperationException("The persisted work state could not be deserialized.");
        state.ETag = values[1].ToString();
        return state;
    }

    /// <inheritdoc/>
    public async Task<bool> CreateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        var key = Key(state.ThrowIfNull().Id!);
        RedisValue[] args = [JsonSerializer.SerializeToUtf8Bytes(state, _jsonSerializerOptions), Milliseconds(timeToLive)];
        if ((int)await ExecuteAsync(db => db.ScriptEvaluateAsync(_create, [key], args, CommandFlags.DemandMaster), cancellationToken).ConfigureAwait(false) != 1)
            return false;

        state.ETag = "1";
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(WorkState state, BinaryData? data, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        var key = Key(state.ThrowIfNull().Id!);
        if (data is not null && _maxDataSize.HasValue && data.ToMemory().Length > _maxDataSize.Value)
            throw new ArgumentException($"The work result data size of {data.ToMemory().Length} bytes exceeds the maximum of {_maxDataSize.Value} bytes.", nameof(data));

        RedisValue[] args = [state.ETag ?? string.Empty, JsonSerializer.SerializeToUtf8Bytes(state, _jsonSerializerOptions), Milliseconds(timeToLive), data is null ? "keep" : "set", data?.ToArray() ?? []];
        var version = (long)await ExecuteAsync(db => db.ScriptEvaluateAsync(_update, [key], args, CommandFlags.DemandMaster), cancellationToken).ConfigureAwait(false);
        if (version <= 0)
            return false;

        state.ETag = version.ToString(CultureInfo.InvariantCulture);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(WorkState state, CancellationToken cancellationToken)
    {
        var key = Key(state.ThrowIfNull().Id!);
        RedisValue[] args = [state.ETag ?? string.Empty];
        return (int)await ExecuteAsync(db => db.ScriptEvaluateAsync(_delete, [key], args, CommandFlags.DemandMaster), cancellationToken).ConfigureAwait(false) == 1;
    }

    /// <inheritdoc/>
    public async Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken)
    {
        var key = Key(id);
        var value = await ExecuteAsync(db => db.HashGetAsync(key, "d", CommandFlags.DemandMaster), cancellationToken).ConfigureAwait(false);
        return value.IsNull ? null : new BinaryData((byte[])value!);
    }
}
