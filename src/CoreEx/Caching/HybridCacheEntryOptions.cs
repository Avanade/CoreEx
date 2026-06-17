namespace CoreEx.Caching;

/// <summary>
/// Provides the <see cref="IHybridCache"/> entry options, including strategy, expiration and tags.
/// </summary>
public record class HybridCacheEntryOptions
{
    private static CacheStrategy? _defaultStrategy;
    private static TimeSpan? _defaultLocalExpiration;
    private static TimeSpan? _defaultDistributedExpiration;

    /// <summary>
    /// Gets or sets the default <see cref="Strategy"/>.
    /// </summary>
    /// <remarks>Defaults to settings '<c>CoreEx:Caching:DefaultStrategy</c>'; otherwise, <see cref="CacheStrategy.Hybrid"/>.</remarks>
    public static CacheStrategy DefaultStrategy
    {
        get => _defaultStrategy ??= Internal.GetConfigurationValue("CoreEx:Caching:DefaultStrategy", CacheStrategy.Hybrid);
        set => _defaultStrategy = value;
    }

    /// <summary>
    /// Gets or sets the default <see cref="LocalExpiration"/>.
    /// </summary>
    /// <remarks>Defaults to settings '<c>CoreEx:Caching:DefaultLocalExpiration</c>'; otherwise, five (<c>5</c>) minutes.</remarks>
    public static TimeSpan DefaultLocalExpiration
    {
        get => _defaultLocalExpiration ??= Internal.GetConfigurationValue("CoreEx:Caching:DefaultLocalExpiration", TimeSpan.FromMinutes(5));
        set => _defaultLocalExpiration = value;
    }

    /// <summary>
    /// Gets or sets the default <see cref="LocalExpiration"/>.
    /// </summary>
    /// <remarks>Defaults to settings '<c>CoreEx:Caching:DistributedExpiration</c>'; otherwise, five (<c>5</c>) minutes.</remarks>
    public static TimeSpan DefaultDistributedExpiration
    {
        get => _defaultDistributedExpiration ??= Internal.GetConfigurationValue("CoreEx:Caching:DefaultDistributedExpiration", TimeSpan.FromMinutes(5));
        set => _defaultDistributedExpiration = value;
    }

    /// <summary>
    /// Creates a new <see cref="HybridCacheEntryOptions"/> using the defaults.
    /// </summary>
    /// <returns>The <see cref="HybridCacheEntryOptions"/>.</returns>
    public static HybridCacheEntryOptions CreateDefault() => new()
    {
        Strategy = DefaultStrategy,
        LocalExpiration = DefaultLocalExpiration,
        DistributedExpiration = DefaultDistributedExpiration
    };

    /// <summary>
    /// Creates a new <see cref="HybridCacheEntryOptions"/> using the specified configuration <paramref name="name"/> to retrieve the underlying configuration settings.
    /// </summary>
    /// <param name="name">The configuration name..</param>
    /// <param name="localExpiration">The default local expiration <see cref="TimeSpan"/> used where not configured.</param>
    /// <param name="distributedExpiration">The default distributed expiration <see cref="TimeSpan"/> used where not configured.</param>
    /// <param name="strategy">The default <see cref="CacheStrategy"/> used where not configured.</param>
    /// <returns>The <see cref="HybridCacheEntryOptions"/>.</returns>
    public static HybridCacheEntryOptions CreateForName(string name, TimeSpan? localExpiration = null, TimeSpan? distributedExpiration = null, CacheStrategy? strategy = null)
    {
        var config = ExecutionContext.GetService<IConfiguration>() ?? Internal.EmptyConfiguration; // Avoids passing null.

        // Default from configuration.
        return new HybridCacheEntryOptions
        {
            LocalExpiration = Internal.GetConfigurationValue($"CoreEx:Caching:{name}:LocalExpiration", localExpiration ?? DefaultLocalExpiration, config),
            DistributedExpiration = Internal.GetConfigurationValue($"CoreEx:Caching:{name}:DistributedExpiration", distributedExpiration ?? DefaultDistributedExpiration, config),
            Strategy = Internal.GetConfigurationValue<CacheStrategy>($"CoreEx:Caching:{name}:Strategy", strategy ?? DefaultStrategy, config)
        };
    }

    /// <summary>
    /// Creates a new <see cref="HybridCacheEntryOptions"/> using the specified <typeparamref name="T"/> name to retrieve the underlying configuration settings.
    /// </summary>
    /// <typeparam name="T">The cache <see cref="Type"/>.</typeparam>
    /// <param name="localExpiration">The default local expiration <see cref="TimeSpan"/> used where not configured.</param>
    /// <param name="distributedExpiration">The default distributed expiration <see cref="TimeSpan"/> used where not configured.</param>
    /// <param name="strategy">The default <see cref="CacheStrategy"/> used where not configured.</param>
    /// <returns>A <see cref="HybridCacheEntryOptions"/> instance associated with the specified type.</returns>
    /// <remarks>The <typeparamref name="T"/> <see cref="MemberInfo.Name"/> is used as the name; see <see cref="CreateForName(string, TimeSpan?, TimeSpan?, CacheStrategy?)"/>.</remarks>
    public static HybridCacheEntryOptions CreateFor<T>(TimeSpan? localExpiration = null, TimeSpan? distributedExpiration = null, CacheStrategy? strategy = null)
        => CreateForName(nameof(T), localExpiration, distributedExpiration, strategy);

    /// <summary>
    /// Gets or sets the <see cref="CacheStrategy"/>.
    /// </summary>
    public CacheStrategy Strategy { get; set; } = CacheStrategy.Hybrid;

    /// <summary>
    /// Gets or sets the local cache expiration <see cref="TimeSpan"/> where applicable.
    /// </summary>
    /// <remarks>Where <see langword="null"/> then the <see cref="IHybridCache"/> implementation default local expiration is applied.</remarks>
    public TimeSpan? LocalExpiration { get; set; }

    /// <summary>
    /// Gets or sets the distributed cache expiration <see cref="TimeSpan"/> where applicable.
    /// </summary>
    /// <remarks>Where <see langword="null"/> then the <see cref="IHybridCache"/> implementation default distributed expiration is applied.</remarks>
    public TimeSpan? DistributedExpiration { get; set; }

    /// <summary>
    /// Gets or sets the associated tags used when setting the underlying cache entry.
    /// </summary>
    /// <remarks>Tags enable grouped cache entry management where supported by the underlying cache implementation(s).</remarks>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Adds the specified tags to the cache entry options.
    /// </summary>
    /// <param name="tags">The tags.</param>
    /// <returns>The <see cref="HybridCacheEntryOptions"/> to support fluent-style method-chaining.</returns>
    public HybridCacheEntryOptions WithTags(params IEnumerable<string> tags)
    {
        if (tags.Any())
            Tags = Tags is null ? [.. tags] : [.. Tags.Concat(tags).Distinct()];

        return this;
    }
}