namespace CoreEx.Hosting.Synchronization;

/// <summary>
/// Provides an <see cref="IHybridCache"/>-based <see cref="ISynchronizer"/>.
/// </summary>
/// <param name="cache">The <see cref="IHybridCache"/>.</param>
public sealed class HybridCacheSynchronizer(IHybridCache cache) : ISynchronizer
{
    private readonly IHybridCache _cache = cache.ThrowIfNull();
    private readonly ConcurrentDictionary<string, (HybridCacheEntryOptions Options, string UId)> _options = new();

    /// <summary>
    /// Gets or sets the optional (default) <see cref="HybridCacheEntryOptions"/>.
    /// </summary>
    public HybridCacheEntryOptions? Options { get; set; }

    /// <summary>
    /// Indicates whether instrumentation is enabled.
    /// </summary>
    /// <remarks><para>Default is <see langword="false"/>.</para>
    /// A synchronizer is likely to be used frequently and as such cause instrumentation noise; therefore, is disabled by default.</remarks>
    public bool IsInstrumentationEnabled { get; set; } = false;

    /// <inheritdoc/>
    public async Task<bool> EnterAsync<T>(string? name = null, CancellationToken cancellationToken = default)
    {
        var key = GetFullName<T>(name);
        var uid = Runtime.NewId();
        var opt = Options ?? HybridCacheEntryOptions.CreateFor<T>();

        // Copy the options and add a tag with the unique identifier.
        opt = (opt with { }).WithTags(uid);

        using (SuppressInstrumentationScope.Begin(!IsInstrumentationEnabled))
        {
            // Attempt to add the value; if it already exists, then another process has entered.
            var cuid = await _cache.GetOrCreateByKeyAsync(key, _ =>
            {
                _options.TryAdd(key, (opt, uid));
                return Task.FromResult(uid);
            }, opt, cancellationToken);

            return cuid == uid;
        }
    }

    /// <inheritdoc/>
    public async Task ExitAsync<T>(string? name = null, CancellationToken cancellationToken = default)
    {
        var key = GetFullName<T>(name);
        if (_options.TryRemove(key, out var val))
        {
            using (SuppressInstrumentationScope.Begin(!IsInstrumentationEnabled))
            {
                // Remove the cache entry by the unique tag. If another process has entered or entry has expired, there is nothing we can do about it at this point.
                await _cache.RemoveByTagAsync(val.UId, val.Options, cancellationToken);
            }

            return;
        }

        throw new InvalidOperationException($"The synchronizer for '{key}' has not been entered by this process.");
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Clean-up any remaining owned locks using the uid; there should be none where all have been properly exited.
        foreach (var entry in _options)
        {
            await _cache.RemoveByTagAsync(entry.Value.UId, entry.Value.Options);
        }

        _options.Clear();
    }

    /// <summary>
    /// Gets the full name.
    /// </summary>
    private static string GetFullName<T>(string? name) => $"{typeof(T).Name}{(name == null ? "" : $":{name}")}";
}