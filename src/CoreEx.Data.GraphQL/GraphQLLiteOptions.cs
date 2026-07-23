namespace CoreEx.Data.GraphQL;

/// <summary>
/// Provides the explicit, DI-driven registration of GraphQL-lite query roots.
/// </summary>
/// <remarks>Registered via <see cref="GraphQLServiceCollectionExtensions.AddCoreExGraphQLLite(IServiceCollection, Action{GraphQLLiteOptions, IServiceProvider})"/>. Each root binds a GraphQL root field name to an
/// existing <see cref="QueryArgsConfig"/> (list roots) or a single-item resolver (get roots) — no attribute-based auto-discovery.</remarks>
public sealed class GraphQLLiteOptions
{
    private readonly Dictionary<string, GraphQLQueryRoot> _queryRoots = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, GraphQLItemRoot> _itemRoots = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the registered list query roots.
    /// </summary>
    public IReadOnlyDictionary<string, GraphQLQueryRoot> QueryRoots => _queryRoots;

    /// <summary>
    /// Gets the registered single-item roots.
    /// </summary>
    public IReadOnlyDictionary<string, GraphQLItemRoot> ItemRoots => _itemRoots;

    /// <summary>
    /// Registers a list query root field (e.g. <c>products</c>) bound to an existing <see cref="QueryArgsConfig"/> and <c>QueryAsync</c>-shaped delegate.
    /// </summary>
    /// <typeparam name="TItem">The projected item <see cref="Type"/>.</typeparam>
    /// <param name="name">The GraphQL root field name.</param>
    /// <param name="queryArgsConfig">The existing <see cref="QueryArgsConfig"/> used to validate/parse the <c>filter</c>/<c>orderby</c> arguments (e.g. <c>ProductQueryArgsConfig.Default</c>).</param>
    /// <param name="resolver">The existing query delegate (e.g. <c>(qa, pa, ct) =&gt; service.QueryAsync(qa, pa, ct)</c>).</param>
    /// <returns>The <see cref="GraphQLLiteOptions"/> to support fluent-style method-chaining.</returns>
    public GraphQLLiteOptions AddQuery<TItem>(string name, QueryArgsConfig queryArgsConfig, Func<QueryArgs?, PagingArgs?, CancellationToken, Task<IItemsResult<TItem>>> resolver)
    {
        name.ThrowIfNull();
        queryArgsConfig.ThrowIfNull();
        resolver.ThrowIfNull();

        _queryRoots[name] = new GraphQLQueryRoot(name, typeof(TItem), queryArgsConfig, async (qa, pa, ct) => await resolver(qa, pa, ct).ConfigureAwait(false));
        return this;
    }

    /// <summary>
    /// Registers a single-item root field (e.g. <c>product</c>) bound to an existing single-item <c>GetAsync</c>-shaped delegate.
    /// </summary>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="name">The GraphQL root field name.</param>
    /// <param name="resolver">The resolver delegate, receiving the resolved GraphQL field arguments (e.g. <c>id</c>).</param>
    /// <returns>The <see cref="GraphQLLiteOptions"/> to support fluent-style method-chaining.</returns>
    public GraphQLLiteOptions AddGet<TItem>(string name, Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<TItem?>> resolver)
    {
        name.ThrowIfNull();
        resolver.ThrowIfNull();

        _itemRoots[name] = new GraphQLItemRoot(name, typeof(TItem), async (args, ct) => await resolver(args, ct).ConfigureAwait(false));
        return this;
    }
}
