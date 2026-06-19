namespace CoreEx.EntityFrameworkCore;

/// <summary>
/// Provides options for the <see cref="IEfDb"/>.
/// </summary>
public class EfDbOptions
{
    private readonly Lazy<ConcurrentDictionary<Type, object>> _models = new();

    /// <summary>
    /// Gets the default <see cref="EfDbArgs"/>.
    /// </summary>
    public EfDbArgs Args { get; private set; } = new();

    /// <summary>
    /// Sets (overrides) the default <see cref="Args"/>.
    /// </summary>
    /// <param name="args">The <see cref="EfDbArgs"/>.</param>
    /// <returns>The <see cref="EfDbOptions"/> to support fluent-style method-chaining.</returns>
    public EfDbOptions WithArgs(EfDbArgs args)
    {
        Args = args with { };
        return this;
    }

    /// <summary>
    /// Adds (or updates) the <see cref="EfDbModelOptions{TModel}"/> for the specified <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="configureOptions">The action to configure the <see cref="EfDbModelOptions{TModel}"/>.</param>
    /// <returns>The <see cref="EfDbOptions"/> to support fluent-style method-chaining.</returns>
    public EfDbOptions WithModel<TModel>(Action<EfDbModelOptions<TModel>>? configureOptions = null) where TModel : class
    {
        if (configureOptions is null)
            return this;

        var mo = GetOrAddModelOptions<TModel>();
        configureOptions?.Invoke(mo);
        return this;
    }

    /// <summary>
    /// Gets or adds the <see cref="EfDbModelOptions{TModel}"/> for the specified <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <returns>he <see cref="EfDbModelOptions{TModel}"/>.</returns>
    public EfDbModelOptions<TModel> GetOrAddModelOptions<TModel>() where TModel : class => (EfDbModelOptions<TModel>)_models.Value.GetOrAdd(typeof(TModel), _ => new EfDbModelOptions<TModel>());
    
    /// <summary>
    /// Tries to get <see cref="EfDbModelOptions{TModel}"/> for the specified <typeparamref name="TModel"/>.
    /// </summary>
    /// <typeparam name="TModel">The model <see cref="Type"/>.</typeparam>
    /// <param name="modelOptions">The <see cref="EfDbModelOptions{TModel}"/> where found.</param>
    /// <returns><see langword="true"/> where the <see cref="EfDbModelOptions{TModel}"/> was found; otherwise, <see langword="false"/>.</returns>
    public bool TryGetModelOptions<TModel>([NotNullWhen(true)] out EfDbModelOptions<TModel>? modelOptions) where TModel : class
    {
        if (_models.Value.TryGetValue(typeof(TModel), out var mo))
        {
            modelOptions = (EfDbModelOptions<TModel>)mo;
            return true;
        }

        modelOptions = null;
        return false;
    }
}