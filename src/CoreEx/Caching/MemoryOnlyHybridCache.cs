namespace CoreEx.Caching;

/// <summary>
/// Provides an <see cref="IHybridCache"/> implementation that uses in-memory caching only regardless of the specified <see cref="HybridCacheEntryOptions.Strategy"/>.
/// </summary>
/// <param name="cacheKeyProvider">The optional <see cref="ICacheKeyProvider"/>.</param>
/// <param name="memoryCache">The optional <see cref="IMemoryCache"/>.</param>
public sealed class MemoryOnlyHybridCache(ICacheKeyProvider? cacheKeyProvider = null, IMemoryCache? memoryCache = null) : IHybridCache
{
    private readonly IMemoryCache _memoryCache = memoryCache ?? new MemoryCache(new MemoryCacheOptions());

    /// <inheritdoc/>
    public ICacheKeyProvider KeyProvider { get; } = cacheKeyProvider ?? new DefaultCacheKeyProvider();

    /// <inheritdoc/>
    public async Task<(bool Exists, T? Value)> TryGetByKeyAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue<T>(KeyProvider.GetFullyQualifiedCacheKey(key), out T? value))
            return (true, value);

        return (false, default);
    }

    /// <inheritdoc/>
    public async Task<T> GetOrCreateByKeyAsync<T>(string key, Func<CancellationToken, Task<T>> factory, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue<T>(KeyProvider.GetFullyQualifiedCacheKey(key), out T? value))
            return value!;

        var result = await factory(cancellationToken).ConfigureAwait(false)!;
        _memoryCache.Set(KeyProvider.GetFullyQualifiedCacheKey(key), result, options?.LocalExpiration ?? HybridCacheEntryOptions.DefaultLocalExpiration);
        return result;
    }
        
    /// <inheritdoc/>
    public Task<T?> GetOrDefaultByKeyAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue<T>(KeyProvider.GetFullyQualifiedCacheKey(key), out T? value))
            return Task.FromResult<T?>(value);

        return Task.FromResult<T?>(default);
    }

    /// <inheritdoc/>
    public Task SetByKeyAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        _memoryCache.Set(KeyProvider.GetFullyQualifiedCacheKey(key), value, options?.LocalExpiration ?? HybridCacheEntryOptions.DefaultLocalExpiration);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task RemoveByKeyAsync(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(KeyProvider.GetFullyQualifiedCacheKey(key));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    /// <remarks>Throws a <see cref="NotSupportedException"/>.</remarks>
    public Task RemoveByTagAsync(string tag, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    /// <inheritdoc/>
    /// <remarks>Throws a <see cref="NotSupportedException"/>.</remarks>
    public Task RemoveByTagAsync(IEnumerable<string> tags, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}