namespace CoreEx.Cosmos;

/// <summary>
/// Provides options for the <see cref="ICosmosDb"/>.
/// </summary>
public class CosmosDbOptions
{
    private readonly ConcurrentDictionary<string, object> _models = new();

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
    /// <returns>The <see cref="CosmosDbModelOptions{TModel}"/>.</returns>
    public CosmosDbModelOptions<TModel> GetOrAddModelOptions<TModel>(string containerId) where TModel : class, IEntityKey, new()
        => (CosmosDbModelOptions<TModel>)_models.GetOrAdd(containerId.ThrowIfNull(), _ => new CosmosDbModelOptions<TModel>());

    /// <summary>
    /// Tries to get the <see cref="CosmosDbModelOptions{TModel}"/> for the specified container <paramref name="containerId"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="containerId">The <see cref="Container"/> identifier.</param>
    /// <param name="modelOptions">The <see cref="CosmosDbModelOptions{TModel}"/> where found.</param>
    /// <returns><see langword="true"/> where the <see cref="CosmosDbModelOptions{TModel}"/> was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetModelOptions<TModel>(string containerId, [NotNullWhen(true)] out CosmosDbModelOptions<TModel>? modelOptions) where TModel : class, IEntityKey, new()
    {
        if (_models.TryGetValue(containerId.ThrowIfNull(), out var mo))
        {
            modelOptions = (CosmosDbModelOptions<TModel>)mo;
            return true;
        }

        modelOptions = null;
        return false;
    }
}
