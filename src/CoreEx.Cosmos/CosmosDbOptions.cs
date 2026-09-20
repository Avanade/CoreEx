namespace CoreEx.Cosmos;

/// <summary>
/// Provides options for the <see cref="ICosmosDb"/>.
/// </summary>
public class CosmosDbOptions
{
    // Keyed by (containerId, TModel) - not containerId alone - since a container is legitimately shared by multiple distinct model types (see CosmosDbModelOptions<TModel>.WithTypeDiscriminator);
    // keying by containerId alone would let the first TModel registered for a given containerId "win" the cache slot for the lifetime of this (typically singleton) instance, with every other type
    // sharing that containerId throwing InvalidCastException when it tries to cast the cached entry back to its own CosmosDbModelOptions<TModel>.
    private readonly ConcurrentDictionary<(string ContainerId, Type ModelType), object> _models = new();

    /// <summary>
    /// Gets the default <see cref="CosmosDbArgs"/>.
    /// </summary>
    public CosmosDbArgs Args { get; private set; } = new();

    /// <summary>
    /// Sets (overrides) the default <see cref="Args"/>.
    /// </summary>
    /// <param name="args">The <see cref="CosmosDbArgs"/>.</param>
    /// <returns>The <see cref="CosmosDbOptions"/> to support fluent-style method-chaining.</returns>
    public CosmosDbOptions WithArgs(CosmosDbArgs args)
    {
        Args = args with { };
        return this;
    }

    /// <summary>
    /// Gets or adds the <see cref="CosmosDbModelOptions{TModel}"/> for the specified container <paramref name="containerId"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="configure">The optional action to configure a <b>newly created</b> <see cref="CosmosDbModelOptions{TModel}"/>; see remarks.</param>
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/>.</returns>
    /// <remarks><paramref name="configure"/> is invoked <b>only</b> the first time a <see cref="CosmosDbModelOptions{TModel}"/> is created for this <paramref name="containerId"/>/<typeparamref name="TModel"/>
    /// combination - deliberately, since this <see cref="CosmosDbOptions"/> is typically a long-lived singleton shared across every <see cref="CosmosDb"/> instance (e.g. one per request/scope) that calls
    /// <see cref="CosmosDb.Container{TModel}(string, Action{CosmosDbModelOptions{TModel}}?)"/> for the same <paramref name="containerId"/>. Were <paramref name="configure"/> instead re-invoked against an
    /// already-configured (and potentially already in-use) shared instance by every new scope, a callback appending state (e.g. <see cref="CosmosDbModelOptions{TModel}.WithFilter"/>) would keep
    /// accumulating duplicate registrations for as long as the process runs, and concurrent first-callers could race on mutating the same shared instance. <see cref="ConcurrentDictionary{TKey, TValue}.GetOrAdd(TKey, Func{TKey, TValue})"/>'s
    /// factory may itself run more than once under concurrent first-time access, but only ever against its own freshly-constructed (not-yet-published/not-yet-shared) candidate instance - exactly one of
    /// which is ever actually stored and returned - so this remains safe without any additional locking.</remarks>
    public CosmosDbModelOptions<TModel> GetOrAddModelOptions<TModel>(string containerId, Action<CosmosDbModelOptions<TModel>>? configure = null) where TModel : class, IEntityKey, new()
        => (CosmosDbModelOptions<TModel>)_models.GetOrAdd((containerId.ThrowIfNull(), typeof(TModel)), _ =>
        {
            var options = new CosmosDbModelOptions<TModel>();
            configure?.Invoke(options);
            return options;
        });

    /// <summary>
    /// Tries to get the <see cref="CosmosDbModelOptions{TModel}"/> for the specified container <paramref name="containerId"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="modelOptions">The <see cref="CosmosDbModelOptions{TModel}"/> where found.</param>
    /// <returns><see langword="true"/> where the <see cref="CosmosDbModelOptions{TModel}"/> was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetModelOptions<TModel>(string containerId, [NotNullWhen(true)] out CosmosDbModelOptions<TModel>? modelOptions) where TModel : class, IEntityKey, new()
    {
        if (_models.TryGetValue((containerId.ThrowIfNull(), typeof(TModel)), out var mo))
        {
            modelOptions = (CosmosDbModelOptions<TModel>)mo;
            return true;
        }

        modelOptions = null;
        return false;
    }
}
