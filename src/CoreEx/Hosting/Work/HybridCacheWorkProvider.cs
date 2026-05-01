namespace CoreEx.Hosting.Work;

/// <summary>
/// Provides the <see cref="IWorkProvider"/> implementation using an underlying <see cref="IHybridCache"/>.
/// </summary>
/// <param name="hybridCache">The <see cref="IHybridCache"/>.</param>
public class HybridCacheWorkProvider(IHybridCache hybridCache) : IWorkProvider
{
    /// <summary>
    /// Gets the underlying <see cref="IHybridCache"/>.
    /// </summary>
    public IHybridCache HybridCache { get; } = hybridCache.ThrowIfNull();

    /// <summary>
    /// Gets or sets the <see cref="HybridCacheEntryOptions"/>.
    /// </summary>
    public HybridCacheEntryOptions? CacheEntryOptions { get; set; }

    /// <summary>
    /// Gets or sets the maximum <see cref="SetDataAsync(string, BinaryData, CancellationToken)"/> cached data size in bytes.
    /// </summary>
    /// <remarks>The default is 512 * 1024 (512 KB).</remarks>
    public int MaxCachedDataSize { get; set => field = value.ThrowWhen(value => value <= 0); } = 512 * 1024;

    /// <summary>
    /// Gets the <see cref="CacheEntryOptions"/> and defaults where not specified.
    /// </summary>
    private HybridCacheEntryOptions GetCacheEntryOptions()
    {
        if (CacheEntryOptions is not null)
            return CacheEntryOptions;

        // Create default with double the WorkState expiry time span.
        var expiry = WorkOrchestrator.DefaultExpiryTimeSpan * 2;
        return HybridCacheEntryOptions.CreateFor<WorkState>(expiry, expiry, CacheStrategy.Hybrid).WithTags(nameof(WorkState));
    }

    /// <inheritdoc/>
    public async Task<WorkState?> GetAsync(string id, CancellationToken cancellationToken)
    {
        var (Exists, Value) = await HybridCache.TryGetAsync<WorkStateCacheItem>(id, GetCacheEntryOptions(), cancellationToken);
        return Exists ? Value!.State : null;
    }

    /// <inheritdoc/>
    public async Task CreateAsync(WorkState state, CancellationToken cancellationToken)
    {
        var exists = true;
        await HybridCache.GetOrCreateAsync(state.ThrowIfNull().Id.ThrowIfNullOrEmpty(), _ =>
        {
            exists = false;
            return Task.FromResult(new WorkStateCacheItem { Id = state.Id!, State = state });
        }, GetCacheEntryOptions(), cancellationToken);

        if (exists)
            throw new InvalidOperationException($"A work state with the identifier '{state.Id}' already exists.");
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(WorkState state, CancellationToken cancellationToken)
    {
        var (Exists, Value) = await HybridCache.TryGetAsync<WorkStateCacheItem>(state.ThrowIfNull().Id.ThrowIfNullOrEmpty(), GetCacheEntryOptions(), cancellationToken).ConfigureAwait(false);
        if (!Exists || Value is null)
            throw new NotFoundException();

        Value.State = state;
        await HybridCache.SetAsync(Value, GetCacheEntryOptions(), cancellationToken);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string id, CancellationToken cancellationToken) => HybridCache.RemoveAsync<WorkStateCacheItem>(id, cancellationToken);

    /// <inheritdoc/>
    public async Task<BinaryData?> GetDataAsync(string id, CancellationToken cancellationToken)
    {
        var (Exists, Value) = await HybridCache.TryGetAsync<WorkStateCacheItem>(id.ThrowIfNullOrEmpty(), GetCacheEntryOptions(), cancellationToken).ConfigureAwait(false);
        if (!Exists || Value is null)
            return null;

        return Value.Data;
    }

    /// <inheritdoc/>
    public async Task SetDataAsync(string id, BinaryData data, CancellationToken cancellationToken)
    {
        var (Exists, Value) = await HybridCache.TryGetAsync<WorkStateCacheItem>(id.ThrowIfNullOrEmpty(), GetCacheEntryOptions(), cancellationToken).ConfigureAwait(false);
        if (!Exists || Value is null)
            throw new NotFoundException();

        if (data.Length > MaxCachedDataSize)
            throw new InvalidOperationException($"The data size of {data.Length} bytes exceeds the maximum allowed size of {MaxCachedDataSize} bytes.");

        Value.Data = data;
        await HybridCache.SetAsync(Value, GetCacheEntryOptions(), cancellationToken);
    }

    /// <summary>
    /// Provides the underlying work state cache item.
    /// </summary>
    private record class WorkStateCacheItem : IReadOnlyIdentifier<string>
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        public required string Id { get; init; }

        /// <summary>
        /// Gets or sets the <see cref="WorkState"/>.
        /// </summary>
        public required WorkState State { get; set; }

        /// <summary>
        /// Gets or sets the associated data.
        /// </summary>
        public BinaryData? Data { get; set; }
    }
}