namespace CoreEx.Caching;

/// <summary>
/// Enables fully-qualified key formatting of a cache key where partitioning or namespacing is required.
/// </summary>
/// <remarks>This is used to ensure that cache keys are unique across an application or service; for example, by prefixing with the domain and/or tenant names. See <see cref="DefaultCacheKeyProvider"/> which implements as described.</remarks>
public interface ICacheKeyProvider
{
    /// <summary>
    /// Gets the fully-qualified cache key.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The fully-qualified cache key.</returns>
    string GetFullyQualifiedCacheKey(string key);

    /// <summary>
    /// Gets the non-qualified cache key using the <see cref="IEntityKey"/> <see cref="Type"/> of <typeparamref name="T"/> and <paramref name="key"/> value.
    /// </summary>
    /// <typeparam name="T">The cache value <see cref="Type"/>.</typeparam>
    /// <param name="key">The <see cref="CompositeKey"/>.</param>
    /// <returns>The cache key.</returns>
    /// <remarks>The result of this should still be passed through the <see cref="GetFullyQualifiedCacheKey(string)"/> to get the final fully-qualified cache key.</remarks>
    string GetEntityCacheKey<T>(CompositeKey key) where T : IEntityKey;
}