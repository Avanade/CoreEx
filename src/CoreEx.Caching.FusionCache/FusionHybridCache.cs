namespace CoreEx.Caching.FusionCache;

/// <summary>
/// Provides the <see cref="IHybridCache"/> implementation based on <i><see href="https://github.com/ZiggyCreatures/FusionCache">FusionCache</see></i>.
/// </summary>
/// <param name="fusionCache">The underlying <see cref="IFusionCache"/>.</param>
/// <param name="cacheKeyProvider">The <see cref="ICacheKeyProvider"/>.</param>
public class FusionHybridCache(IFusionCache fusionCache, ICacheKeyProvider cacheKeyProvider) : IHybridCache
{
    private readonly IFusionCache _fusionCache = fusionCache.ThrowIfNull();
    private Action<HybridCacheEntryOptions, FusionCacheEntryOptions>? _configure;

    /// <inheritdoc/>
    public ICacheKeyProvider KeyProvider { get; } = cacheKeyProvider.ThrowIfNull();

    /// <inheritdoc/>
    public async Task<(bool Exists, T? Value)> TryGetByKeyAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var fr = await _fusionCache.TryGetAsync<T>(KeyProvider.GetFullyQualifiedCacheKey(key), ConfigureEntryOptions(options), cancellationToken).ConfigureAwait(false);
        return (fr.HasValue, fr.GetValueOrDefault());
    }

    /// <inheritdoc/>
    public async Task<T?> GetOrDefaultByKeyAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.GetOrDefaultAsync<T>(KeyProvider.GetFullyQualifiedCacheKey(key), default, ConfigureEntryOptions(options), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task<T> GetOrCreateByKeyAsync<T>(string key, Func<CancellationToken, Task<T>> factory, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.GetOrSetAsync(KeyProvider.GetFullyQualifiedCacheKey(key), async ct => await factory(ct).ConfigureAwait(false), ConfigureEntryOptions(options), QualifyTags(options?.Tags), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    /// <remarks>Concurrent misses for the same key invoke the <paramref name="factory"/> one at a time (per node by default, courtesy of FusionCache's memory locker; cross-node coalescing requires configuring a FusionCache distributed locker), with later callers served from the cache where the first succeeded.
    /// A failure is not cached by skipping the memory and distributed cache writes via the adaptive factory context (rather than throwing, which would be logged as a factory error).</remarks>
    public async Task<Result<T>> GetOrCreateByKeyWithResultAsync<T>(string key, Func<CancellationToken, Task<Result<T>>> factory, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        factory.ThrowIfNull();

        Result<T>? failure = null;
        var value = await _fusionCache.GetOrSetAsync<T>(KeyProvider.GetFullyQualifiedCacheKey(key), async (ctx, ct) =>
        {
            var result = await factory(ct).ConfigureAwait(false);
            if (result.IsSuccess)
                return result.Value;

            failure = result;
            ctx.Options.SkipMemoryCacheWrite = true;
            ctx.Options.SkipDistributedCacheWrite = true;
            return default!;
        }, default, ConfigureEntryOptions(options), QualifyTags(options?.Tags), cancellationToken).ConfigureAwait(false);

        return failure ?? value;
    }

    /// <inheritdoc/>
    public async Task SetByKeyAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.SetAsync(KeyProvider.GetFullyQualifiedCacheKey(key), value, ConfigureEntryOptions(options), QualifyTags(options?.Tags), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task RemoveByKeyAsync(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.RemoveAsync(KeyProvider.GetFullyQualifiedCacheKey(key), ConfigureEntryOptions(options), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task RemoveByTagAsync(string tag, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.RemoveByTagAsync(KeyProvider.GetFullyQualifiedCacheKey(tag), ConfigureEntryOptions(options), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task RemoveByTagAsync(IEnumerable<string> tags, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.RemoveByTagAsync(QualifyTags(tags)!, ConfigureEntryOptions(options), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Qualifies each of the specified <paramref name="tags"/> via the <see cref="KeyProvider"/>, the same way every cache key is qualified.
    /// </summary>
    /// <remarks>Without this, tags would be the one part of the cache surface left unpartitioned - e.g. two tenants/domains sharing the same underlying <see cref="IFusionCache"/>/backplane and using the same tag
    /// name (which several CoreEx-provided <see cref="IHybridCache"/> consumers do, using fixed tag names) would silently invalidate each other's entries via <see cref="RemoveByTagAsync(string, HybridCacheEntryOptions?, CancellationToken)"/>.</remarks>
    private string[]? QualifyTags(IEnumerable<string>? tags) => tags?.Select(KeyProvider.GetFullyQualifiedCacheKey).ToArray();

    /// <summary>
    /// Convert and configure.
    /// </summary>
    private FusionCacheEntryOptions ConfigureEntryOptions(HybridCacheEntryOptions? options)
    {
        var hco = options ?? HybridCacheEntryOptions.CreateDefault();
        var fco = hco.ToFusionCacheEntryOptions();
        _configure?.Invoke(hco, fco);
        return fco;
    }

    /// <summary>
    /// Provides an opportunity to further <paramref name="configure"/> the <see cref="FusionCacheEntryOptions"/> directly before use.
    /// </summary>
    /// <param name="configure">The action to configure the resulting <see cref="FusionCacheEntryOptions"/>, given the source <see cref="HybridCacheEntryOptions"/> (e.g. to key behavior off <see cref="HybridCacheEntryOptions.Tags"/>
    /// or <see cref="HybridCacheEntryOptions.Strategy"/>) for context.</param>
    /// <returns>The <see cref="FusionHybridCache"/> to support fluent-style method-chaining.</returns>
    public FusionHybridCache ConfigureEntryOptions(Action<HybridCacheEntryOptions, FusionCacheEntryOptions>? configure)
    {
        _configure = configure;
        return this;
    }
}
