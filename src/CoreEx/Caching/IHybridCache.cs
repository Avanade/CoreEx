namespace CoreEx.Caching;

/// <summary>
/// Enables a hybrid cache (i.e. local and/or distributed) as defined by the <see cref="CacheStrategy"/>.
/// </summary>
/// <remarks>The <see cref="HybridCacheEntryOptions"/> is required for each method to specify the underlying cache behavior; therefore, it is important that the same options are reused
/// when accessing the same key as this may result in inconsistent/unexpected behavior.</remarks>
public interface IHybridCache
{
    /// <summary>
    /// Gets the <see cref="ICacheKeyProvider"/>.
    /// </summary>
    ICacheKeyProvider KeyProvider { get; }

    /// <summary>
    /// Tries to get the cached value for the specified key. 
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A tuple with a <see langword="bool"/> indicating whether the entry exists and the associated value where found (otherwise, <see langword="default"/>).</returns>
    Task<(bool Exists, T? Value)> TryGetByKeyAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the cached value for the specified key.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The cached value or default.</returns>
    Task<T?> GetOrDefaultByKeyAsync<T>(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets or overwrites the cache value for the specified key.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cache value.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task SetByKeyAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the cached value for the specified key using the <paramref name="factory"/> to create and set where not found.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The function used to create the cache value.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The cached value.</returns>
    Task<T> GetOrCreateByKeyAsync<T>(string key, Func<CancellationToken, Task<T>> factory, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes the cached value for the specified key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task RemoveByKeyAsync(string key, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove all cached values that have the specified tag.
    /// </summary>
    /// <param name="tag">The cache tag.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task RemoveByTagAsync(string tag, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove all cached values that have the specified tags.
    /// </summary>
    /// <param name="tags">The cache tags.</param>
    /// <param name="options">The optional <see cref="HybridCacheEntryOptions"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task RemoveByTagAsync(IEnumerable<string> tags, HybridCacheEntryOptions? options = null, CancellationToken cancellationToken = default);
}