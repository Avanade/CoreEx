namespace CoreEx;

public static partial class Extensions
{
    /// <summary>
    /// Tries to get the cached value for the specified key. 
    /// </summary>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A tuple with a <see langword="bool"/> indicating whether the entry exists and the associated value where found (otherwise, <see langword="default"/>).</returns>
    public static async Task<(bool Exists, T? Value)> TryGetAsync<T>(this IHybridCache cache, CompositeKey key, CancellationToken cancellationToken = default) where T : IEntityKey
        => await TryGetAsync<T>(cache, key, null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Tries to get the cached value for the specified key. 
    /// </summary>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A tuple with a <see langword="bool"/> indicating whether the entry exists and the associated value where found (otherwise, <see langword="default"/>).</returns>
    public static async Task<(bool Exists, T? Value)> TryGetAsync<T>(this IHybridCache cache, CompositeKey key, HybridCacheEntryOptions? options, CancellationToken cancellationToken = default) where T : IEntityKey
        => await cache.TryGetByKeyAsync<T>(cache.KeyProvider.GetEntityCacheKey<T>(key), options ?? HybridCacheEntryOptions.CreateFor<T>(), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the cached value for the specified key or the default where not found.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The cached value or the default where not found.</returns>
    public static async Task<T?> GetOrDefaultAsync<T>(this IHybridCache cache, CompositeKey key, CancellationToken cancellationToken = default) where T : IEntityKey
        => await GetOrDefaultAsync<T>(cache, key, null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the cached value for the specified key or the default where not found.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The cached value or the default where not found.</returns>
    public static async Task<T?> GetOrDefaultAsync<T>(this IHybridCache cache, CompositeKey key, HybridCacheEntryOptions? options, CancellationToken cancellationToken = default) where T : IEntityKey
        => await cache.GetOrDefaultByKeyAsync<T>(cache.KeyProvider.GetEntityCacheKey<T>(key), options ?? HybridCacheEntryOptions.CreateFor<T>(), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Sets or overwrites the cache <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="value">The cache value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Uses the <see cref="IHybridCache.KeyProvider"/> <see cref="ICacheKeyProvider.GetEntityCacheKey{T}(CompositeKey)"/> to determine the cache key.</remarks>
    public static async Task SetAsync<T>(this IHybridCache cache, T value, CancellationToken cancellationToken = default) where T : IEntityKey
        => await SetAsync<T>(cache, value, null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Sets or overwrites the cache <paramref name="value"/>.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="value">The cache value.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Uses the <see cref="IHybridCache.KeyProvider"/> <see cref="ICacheKeyProvider.GetEntityCacheKey{T}(CompositeKey)"/> to determine the cache key.</remarks>
    public static async Task SetAsync<T>(this IHybridCache cache, T value, HybridCacheEntryOptions? options, CancellationToken cancellationToken = default) where T : IEntityKey
        => await cache.SetByKeyAsync<T>(cache.KeyProvider.GetEntityCacheKey<T>(value.EntityKey), value, options ?? HybridCacheEntryOptions.CreateFor<T>(), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the cached value for the specified key using the <paramref name="factory"/> to create and set where not found.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="factory">The function used to create the cache value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Uses the <see cref="IHybridCache.KeyProvider"/> <see cref="ICacheKeyProvider.GetEntityCacheKey{T}(CompositeKey)"/> to determine the cache key.</remarks>
    public static async Task<T> GetOrCreateAsync<T>(this IHybridCache cache, CompositeKey key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken = default) where T : IEntityKey
        => await cache.GetOrCreateByKeyAsync<T>(cache.KeyProvider.GetEntityCacheKey<T>(key), factory, null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Gets the cached value for the specified key using the <paramref name="factory"/> to create and set where not found.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="factory">The function used to create the cache value.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Uses the <see cref="IHybridCache.KeyProvider"/> <see cref="ICacheKeyProvider.GetEntityCacheKey{T}(CompositeKey)"/> to determine the cache key.</remarks>
    public static async Task<T> GetOrCreateAsync<T>(this IHybridCache cache, CompositeKey key, Func<CancellationToken, Task<T>> factory, HybridCacheEntryOptions? options, CancellationToken cancellationToken = default) where T : IEntityKey
        => await cache.GetOrCreateByKeyAsync<T>(cache.KeyProvider.GetEntityCacheKey<T>(key), factory, options ?? HybridCacheEntryOptions.CreateFor<T>(), cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Removes the cached value for the specified key.
    /// </summary>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Uses the <see cref="IHybridCache.KeyProvider"/> <see cref="ICacheKeyProvider.GetEntityCacheKey{T}(CompositeKey)"/> to determine the cache key.</remarks>
    public static async Task RemoveAsync<T>(this IHybridCache cache, CompositeKey key, CancellationToken cancellationToken = default) where T : IEntityKey
        => await cache.RemoveAsync<T>(key, null, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Removes the cached value for the specified key.
    /// </summary>
    /// <param name="cache">The <see cref="IHybridCache"/>.</param>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>Uses the <see cref="IHybridCache.KeyProvider"/> <see cref="ICacheKeyProvider.GetEntityCacheKey{T}(CompositeKey)"/> to determine the cache key.</remarks>
    public static async Task RemoveAsync<T>(this IHybridCache cache, CompositeKey key, HybridCacheEntryOptions? options, CancellationToken cancellationToken = default) where T : IEntityKey
        => await cache.RemoveByKeyAsync(cache.KeyProvider.GetEntityCacheKey<T>(key), options ?? HybridCacheEntryOptions.CreateFor<T>(), cancellationToken).ConfigureAwait(false);
}