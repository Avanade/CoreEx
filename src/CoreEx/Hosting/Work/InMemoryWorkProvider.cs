namespace CoreEx.Hosting.Work;

/// <summary>
/// Provides a process-local <see cref="IWorkProvider"/> with atomic create and optimistic-concurrency update semantics.
/// </summary>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>; defaults to <see cref="JsonDefaults.SerializerOptions"/>.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/> used for record expiry; defaults to <see cref="TimeProvider.System"/>.</param>
/// <remarks>The state is <b>only</b> shared within the current process (register as a singleton); this is intended for development, testing and single-instance hosts. Use a durable shared provider (for example Redis) where multiple instances must observe the same work.</remarks>
public sealed class InMemoryWorkProvider(JsonSerializerOptions? jsonSerializerOptions = null, TimeProvider? timeProvider = null) : IWorkProvider
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<string, Entry> _entries = [];
#if NET8_0
    private readonly object _lock = new();
#else
    private readonly Lock _lock = new();
#endif

    /// <inheritdoc/>
    public Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(TryGet(id.ThrowIfNullOrEmpty(), out var entry) ? Deserialize(entry) : null);
        }
    }

    /// <inheritdoc/>
    public Task<bool> CreateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = state.ThrowIfNull().Id.ThrowIfNullOrEmpty(nameof(state.Id));
        var json = Serialize(state);
        var expires = Expires(timeToLive);

        lock (_lock)
        {
            if (TryGet(id, out _))
                return Task.FromResult(false);

            var entry = new Entry { State = json, Incarnation = Guid.NewGuid().ToString("N"), Version = 1, Expires = expires };
            _entries[id] = entry;
            state.ETag = ETag(entry);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> UpdateAsync(WorkState state, BinaryData? data, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = state.ThrowIfNull().Id.ThrowIfNullOrEmpty(nameof(state.Id));
        var json = Serialize(state);
        var expires = Expires(timeToLive);
        var bytes = data?.ToArray();

        lock (_lock)
        {
            if (!TryGet(id, out var entry) || !IsMatch(entry, state.ETag))
                return Task.FromResult(false);

            entry.State = json;
            entry.Version++;
            entry.Expires = expires;
            if (bytes is not null)
                entry.Data = bytes;

            state.ETag = ETag(entry);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(WorkState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var id = state.ThrowIfNull().Id.ThrowIfNullOrEmpty(nameof(state.Id));

        lock (_lock)
        {
            if (!TryGet(id, out var entry))
                return Task.FromResult(true);

            if (!IsMatch(entry, state.ETag))
                return Task.FromResult(false);

            _entries.Remove(id);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_lock)
        {
            return Task.FromResult(TryGet(id.ThrowIfNullOrEmpty(), out var entry) && entry.Data is not null ? new BinaryData(entry.Data.ToArray()) : null);
        }
    }

    /// <summary>
    /// Gets the unexpired entry, removing it where expired; must be called within the lock.
    /// </summary>
    private bool TryGet(string id, [NotNullWhen(true)] out Entry? entry)
    {
        if (_entries.TryGetValue(id, out entry))
        {
            if (entry.Expires > _timeProvider.GetUtcNow())
                return true;

            _entries.Remove(id);
            entry = null;
        }

        return false;
    }

    /// <summary>
    /// Indicates whether the <paramref name="etag"/> matches the <paramref name="entry"/> version.
    /// </summary>
    private static bool IsMatch(Entry entry, string? etag) => etag is not null && etag == ETag(entry);

    /// <summary>
    /// Combines the record incarnation with its current version.
    /// </summary>
    private static string ETag(Entry entry) => $"{entry.Incarnation}:{entry.Version.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Calculates the absolute expiry from the <paramref name="timeToLive"/>.
    /// </summary>
    private DateTimeOffset Expires(TimeSpan timeToLive) => timeToLive > TimeSpan.Zero ? _timeProvider.GetUtcNow().Add(timeToLive) : throw new ArgumentOutOfRangeException(nameof(timeToLive), "The time-to-live must be greater than zero.");

    /// <summary>
    /// Serializes the <paramref name="state"/> to an independent copy.
    /// </summary>
    private byte[] Serialize(WorkState state) => JsonSerializer.SerializeToUtf8Bytes(state, _jsonSerializerOptions);

    /// <summary>
    /// Deserializes an independent copy of the <paramref name="entry"/> state.
    /// </summary>
    private WorkState Deserialize(Entry entry)
    {
        var state = JsonSerializer.Deserialize<WorkState>(entry.State, _jsonSerializerOptions) ?? throw new InvalidOperationException("The persisted work state could not be deserialized.");
        state.ETag = ETag(entry);
        return state;
    }

    /// <summary>
    /// Represents one persisted work record.
    /// </summary>
    private sealed class Entry
    {
        /// <summary>Gets or sets the serialized state.</summary>
        public required byte[] State { get; set; }

        /// <summary>Gets the unique record incarnation.</summary>
        public required string Incarnation { get; init; }

        /// <summary>Gets or sets the version.</summary>
        public long Version { get; set; }

        /// <summary>Gets or sets the result data.</summary>
        public byte[]? Data { get; set; }

        /// <summary>Gets or sets the record expiry.</summary>
        public DateTimeOffset Expires { get; set; }
    }
}
