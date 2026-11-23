namespace CoreEx.Hosting.Work;

/// <summary>
/// Provides a process-local <see cref="IWorkProvider"/> with atomic create and optimistic-concurrency update semantics.
/// </summary>
/// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>; defaults to <see cref="JsonDefaults.SerializerOptions"/>.</param>
/// <param name="timeProvider">The optional <see cref="TimeProvider"/> used for store time and record expiry; defaults to <see cref="TimeProvider.System"/>.</param>
/// <remarks>The state is <b>only</b> shared within the current process (register as a singleton); this is intended for development, testing and single-instance hosts. Use a durable shared provider (for example Redis) where multiple instances must observe the same work.</remarks>
public sealed class InMemoryWorkProvider(JsonSerializerOptions? jsonSerializerOptions = null, TimeProvider? timeProvider = null) : IWorkProvider
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = jsonSerializerOptions ?? JsonDefaults.SerializerOptions;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly Dictionary<CompositeKey, Entry> _entries = [];
#if NET8_0
    private readonly object _lock = new();
#else
    private readonly Lock _lock = new();
#endif

    /// <inheritdoc/>
    public Task<WorkSnapshot> GetAsync(string domainName, string? tenantId, string operationName, string id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        tenantId = string.IsNullOrEmpty(tenantId) ? null : tenantId;
        var identity = CompositeKey.Create(domainName.ThrowIfNullOrWhiteSpace(), tenantId, operationName.ThrowIfNullOrWhiteSpace(), id.ThrowIfNullOrWhiteSpace());
        lock (_lock)
        {
            var now = _timeProvider.GetUtcNow();
            if (!TryGet(identity, now, out var entry))
                return Task.FromResult(new WorkSnapshot(null, now));

            var state = Deserialize(entry);
            ValidateStateIdentity(state, identity);
            return Task.FromResult(new WorkSnapshot(state, now));
        }
    }

    /// <inheritdoc/>
    public Task<bool> CreateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var identity = GetIdentity(state);
        var json = Serialize(state);
        var expires = Expires(timeToLive);

        lock (_lock)
        {
            if (TryGet(identity, _timeProvider.GetUtcNow(), out _))
                return Task.FromResult(false);

            var entry = new Entry { State = json, Incarnation = Guid.NewGuid().ToString("N"), Version = 1, Expires = expires };
            _entries[identity] = entry;
            state.ETag = ETag(entry);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> UpdateAsync(WorkState state, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var identity = GetIdentity(state);
        var json = Serialize(state);
        var expires = Expires(timeToLive);

        lock (_lock)
        {
            if (!TryGet(identity, _timeProvider.GetUtcNow(), out var entry) || !IsMatch(entry, state.ETag))
                return Task.FromResult(false);

            entry.State = json;
            entry.Version++;
            entry.Expires = expires;
            state.ETag = ETag(entry);
            return Task.FromResult(true);
        }
    }

    /// <inheritdoc/>
    public Task<bool> DeleteAsync(WorkState state, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var identity = GetIdentity(state);

        lock (_lock)
        {
            if (!TryGet(identity, _timeProvider.GetUtcNow(), out var entry))
                return Task.FromResult(true);

            if (!IsMatch(entry, state.ETag))
                return Task.FromResult(false);

            _entries.Remove(identity);
            return Task.FromResult(true);
        }
    }

    /// <summary>
    /// Gets and validates the complete identity for the <paramref name="state"/>.
    /// </summary>
    private static CompositeKey GetIdentity(WorkState state)
    {
        state.ThrowIfNull();
        return CompositeKey.Create(state.DomainName.ThrowIfNullOrWhiteSpace(nameof(state.DomainName)), state.TenantId, state.OperationName.ThrowIfNullOrWhiteSpace(nameof(state.OperationName)), state.Id.ThrowIfNullOrWhiteSpace(nameof(state.Id)));
    }

    /// <summary>
    /// Validates that a state stores the requested complete identity.
    /// </summary>
    private static void ValidateStateIdentity(WorkState state, CompositeKey identity)
    {
        if (GetIdentity(state) != identity)
            throw new InvalidOperationException("The work state identity must match the requested identity.");
    }

    /// <summary>
    /// Gets the unexpired entry, removing it where expired; must be called within the lock.
    /// </summary>
    private bool TryGet(CompositeKey identity, DateTimeOffset now, [NotNullWhen(true)] out Entry? entry)
    {
        if (_entries.TryGetValue(identity, out entry))
        {
            if (entry.Expires > now)
                return true;

            _entries.Remove(identity);
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

        /// <summary>Gets or sets the record expiry.</summary>
        public DateTimeOffset Expires { get; set; }
    }
}
