namespace CoreEx.Caching.FusionCache;

/// <summary>
/// Provides the <see cref="IHybridCache"/> implementation based on <i><see href="https://github.com/ZiggyCreatures/FusionCache">FusionCache</see></i>.
/// </summary>
/// <param name="fusionCache">The underlying <see cref="IFusionCache"/>.</param>
/// <param name="cacheKeyProvider">The <see cref="ICacheKeyProvider"/>.</param>
public class FusionHybridCache(IFusionCache fusionCache, ICacheKeyProvider cacheKeyProvider) : IHybridCache
{
    private readonly IFusionCache _fusionCache = fusionCache.ThrowIfNull();
    private Action<FusionCacheEntryOptions>? _configure;

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
        => await _fusionCache.GetOrSetAsync(KeyProvider.GetFullyQualifiedCacheKey(key), async ct => await factory(ct).ConfigureAwait(false), ConfigureEntryOptions(options), options?.Tags, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task SetByKeyAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.SetAsync(KeyProvider.GetFullyQualifiedCacheKey(key), value, ConfigureEntryOptions(options), options?.Tags, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task RemoveByKeyAsync(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.RemoveAsync(KeyProvider.GetFullyQualifiedCacheKey(key), ConfigureEntryOptions(options), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task RemoveByTagAsync(string tag, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.RemoveByTagAsync(tag, ConfigureEntryOptions(options), cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task RemoveByTagAsync(IEnumerable<string> tags, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
        => await _fusionCache.RemoveByTagAsync(tags, ConfigureEntryOptions(options), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Convert and configure.
    /// </summary>
    private FusionCacheEntryOptions ConfigureEntryOptions(HybridCacheEntryOptions? options)
    {
        var fco = (options ?? HybridCacheEntryOptions.CreateDefault()).ToFusionCacheEntryOptions();
        _configure?.Invoke(fco);
        return fco;
    }

    /// <summary>
    /// Provides an opportunity to further <paramref name="configure"/> the <see cref="FusionCacheEntryOptions"/> directly before use.
    /// </summary>
    /// <param name="configure">The action to configure the <see cref="FusionCacheEntryOptions"/>.</param>
    /// <returns>The <see cref="FusionHybridCache"/> to support fluent-style method-chaining.</returns>
    public FusionHybridCache ConfigureEntryOptions(Action<FusionCacheEntryOptions>? configure)
    {
        _configure = configure;
        return this;
    }
}