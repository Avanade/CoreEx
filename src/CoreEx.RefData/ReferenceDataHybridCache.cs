namespace CoreEx.RefData;

/// <summary>
/// Provides <see cref="IReferenceDataCache"/> implementation using an <see cref="IHybridCache"/>.
/// </summary>
/// <param name="cache">The underlying <see cref="IHybridCache"/>.</param>
public partial class ReferenceDataHybridCache(IHybridCache cache) : IReferenceDataCache
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _semaphores = new();
    private readonly ConcurrentDictionary<Type, HybridCacheEntryOptions> _entryOptions = new();
#if NET8_0
    private readonly object _lock = new();
#else
    private readonly Lock _lock = new();
#endif

    /// <summary>
    /// Gets the underlying <see cref="IHybridCache"/>.
    /// </summary>
    public IHybridCache Cache { get; set; } = cache.ThrowIfNull();

    /// <summary>
    /// Gets or creates the <see cref="HybridCacheEntryOptions"/> for the specified <paramref name="type"/>. 
    /// </summary>
    private HybridCacheEntryOptions GetOrCreateEntryOptions(Type type) => _entryOptions.GetOrAdd(type, _ =>
    {
        var options = HybridCacheEntryOptions.CreateForName(type.Name);
        OnCreateCacheEntry(type, options);
        return options;
    });


    /// <summary>
    /// Provides an opportunity to further configure the <see cref="HybridCacheEntryOptions"/>.
    /// </summary>
    /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
    /// <param name="entry">The <see cref="HybridCacheEntryOptions"/>.</param>
    protected virtual void OnCreateCacheEntry(Type type, HybridCacheEntryOptions entry) { }

    /// <inheritdoc/>
    public async Task<IReferenceDataCollection> GetOrCreateAsync(Type type, Func<Type, CancellationToken, Task<IReferenceDataCollection>> factory, CancellationToken cancellationToken = default)
    {
        var key = $"RefData:{(Internal.GetNamespaceFormattedName(type))}";
        var options = GetOrCreateEntryOptions(type);

        // Use invoker to ensure properly typed (needed for the likes of deserialization, where/if used).
        var invoker = GetInvokerForType(type);

        // Try and get as most likely already in the cache; where exists then exit fast.
        var (Exists, Value) = await invoker(Cache, key, options, cancellationToken).ConfigureAwait(false);
        if (Exists)
            return (IReferenceDataCollection)Value!;

        // A lock is also needed to absolutely ensure only a single semaphore is _ever_ created per type/key.
        SemaphoreSlim semaphore;
        lock (_lock)
        {
            // Get or add a new semaphore for the cache key so we can manage single concurrency for *this* key only.
            semaphore = _semaphores.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        }

        // Use the semaphore to manage a single thread to perform the "expensive" get operation.
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Does a get or create as it may have been added as we went to lock; use the typed invoker (as opposed to relying on C# generic type inference) to ensure the concrete type,
            // rather than the IReferenceDataCollection interface, is used as the underlying cache generic type argument (see GetOrCreateInvokerForType remarks).
            var getOrCreateInvoker = GetOrCreateInvokerForType(type);
            return await getOrCreateInvoker(Cache, key, factory, type, options, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    /// <inheritdoc/>
    public Task RemoveAsync(Type type, CancellationToken cancellationToken = default)
        => Cache.RemoveByKeyAsync($"RefData:{(Internal.GetNamespaceFormattedName(type))}", GetOrCreateEntryOptions(type), cancellationToken);

    /// <summary>
    /// Registers the <see cref="HybridCacheEntryOptions"/> for the specified <typeparamref name="TRefColl"/>.
    /// </summary>
    /// <typeparam name="TRefColl">The <see cref="IReferenceData"/> <see cref="Type"/>.</typeparam>
    /// <param name="options">The <see cref="HybridCacheEntryOptions"/>.</param>
    /// <returns>The <see cref="ReferenceDataHybridCache"/> to support fluent-style method-chaining.</returns>
    public ReferenceDataHybridCache RegisterCacheEntryOptions<TRefColl>(HybridCacheEntryOptions options) where TRefColl : IReferenceDataCollection => RegisterCacheEntryOptions(typeof(TRefColl), options);

    /// <summary>
    /// Registers the <see cref="HybridCacheEntryOptions"/> for the specified <paramref name="type"/> (should be a <see cref="IReferenceDataCollection"/> <see cref="Type"/>).
    /// </summary>
    /// <param name="type">The <see cref="Type"/>.</param>
    /// <param name="options">The <see cref="HybridCacheEntryOptions"/>.</param>
    /// <returns>The <see cref="ReferenceDataHybridCache"/> to support fluent-style method-chaining.</returns>
    public ReferenceDataHybridCache RegisterCacheEntryOptions(Type type, HybridCacheEntryOptions options)
    {
        if (type.ThrowIfNull().GetInterface(nameof(IReferenceDataCollection)) == null)
            throw new ArgumentException($"The specified '{type.Name}' is not a valid {nameof(IReferenceDataCollection)} type.", nameof(type));

        options.ThrowIfNull();
        _entryOptions[type] = options;
        return this;
    }
}
