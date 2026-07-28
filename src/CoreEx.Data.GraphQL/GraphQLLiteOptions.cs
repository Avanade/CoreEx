namespace CoreEx.Data.GraphQL;

/// <summary>
/// Provides the explicit, DI-driven registration of GraphQL-lite query roots.
/// </summary>
/// <remarks>Registered via <see cref="GraphQLExtensions.AddCoreExGraphQLLite(IServiceCollection, Action{GraphQLLiteOptions, IServiceProvider})"/>. Each root binds a GraphQL root field name to an
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
    /// Gets or sets the maximum number of root fields (including repeated/aliased occurrences) permitted in a single document's selection set.
    /// </summary>
    /// <remarks>Defaults to <see langword="null"/> (unlimited), matching the engine's existing behavior. Each root field can independently drive a backend query, so a document with many
    /// aliased occurrences of the same (or different) root fields can fan out to many backend calls from one inbound request; setting a cap here bounds that fan-out. See
    /// <see cref="GraphQLEngine.ExecuteAsync(string, string?, IReadOnlyDictionary{string, object?}?, CancellationToken)"/>, which rejects a document exceeding this limit with a
    /// <c>TOO_MANY_ROOT_FIELDS</c> error before any backend work is performed.</remarks>
    public int? MaxRootFields { get; set; }

    /// <summary>
    /// Gets or sets whether the <c>__schema</c>/<c>__type</c> introspection meta-fields are answerable over the query endpoint.
    /// </summary>
    /// <remarks>Defaults to <see langword="false"/> (secure-by-default): a request for either meta-field produces an <c>INTROSPECTION_DISABLED</c> error rather than being executed. This only
    /// gates the client-facing <c>__schema</c>/<c>__type</c> query fields - <see cref="IGraphQLEngine.GetSchemaAsync(CancellationToken)"/> (the direct API, e.g. for internal tooling or
    /// documentation generation) is unaffected and always available. Enable this where client tooling (e.g. GraphiQL, Postman, Apollo/Relay codegen) needs to introspect the schema.</remarks>
    public bool EnableIntrospection { get; set; }

    /// <summary>
    /// Registers a list query root field (e.g. <c>products</c>) bound to an existing <see cref="QueryArgsConfig"/> and <c>QueryAsync</c>-shaped delegate.
    /// </summary>
    /// <typeparam name="TItem">The projected item <see cref="Type"/>.</typeparam>
    /// <param name="name">The GraphQL root field name.</param>
    /// <param name="queryArgsConfig">The existing <see cref="QueryArgsConfig"/> used to validate/parse the <c>filter</c>/<c>orderby</c> arguments (e.g. <c>ProductQueryArgsConfig.Default</c>).</param>
    /// <param name="resolver">The existing query delegate (e.g. <c>(qa, pa, ct) =&gt; service.QueryAsync(qa, pa, ct)</c>).</param>
    /// <returns>The <see cref="GraphQLLiteOptions"/> to support fluent-style method-chaining.</returns>
    /// <exception cref="ArgumentException">Thrown where <paramref name="name"/> is <c>__</c>-prefixed (reserved for GraphQL introspection) or already registered as a query or item root.</exception>
    public GraphQLLiteOptions AddQuery<TItem>(string name, QueryArgsConfig queryArgsConfig, Func<QueryArgs?, PagingArgs?, CancellationToken, Task<IItemsResult<TItem>>> resolver)
    {
        name.ThrowIfNullOrEmpty();
        queryArgsConfig.ThrowIfNull();
        resolver.ThrowIfNull();
        ThrowIfReservedOrDuplicate(name);

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
    /// <exception cref="ArgumentException">Thrown where <paramref name="name"/> is <c>__</c>-prefixed (reserved for GraphQL introspection) or already registered as a query or item root.</exception>
    public GraphQLLiteOptions AddGet<TItem>(string name, Func<GraphQLLiteArgs, CancellationToken, Task<TItem?>> resolver) where TItem : CoreEx.Entities.Abstractions.IReadOnlyIdentifier
    {
        name.ThrowIfNullOrEmpty();
        resolver.ThrowIfNull();
        ThrowIfReservedOrDuplicate(name);

        _itemRoots[name] = new GraphQLItemRoot(name, typeof(TItem), async (args, ct) => await resolver(args, ct).ConfigureAwait(false));
        return this;
    }

    /// <summary>
    /// Validates that a root field name conforms to the GraphQL <c>Name</c> grammar, is not <c>__</c>-prefixed (reserved for GraphQL introspection), and has not already been
    /// registered as a query or item root.
    /// </summary>
    /// <param name="name">The GraphQL root field name.</param>
    /// <exception cref="ArgumentException">Thrown where <paramref name="name"/> is not a valid GraphQL name, is reserved, or already registered.</exception>
    private void ThrowIfReservedOrDuplicate(string name)
    {
        if (name.StartsWith("__", StringComparison.Ordinal))
            throw new ArgumentException($"Root field name '{name}' is reserved for GraphQL introspection; names starting with '__' are not permitted.", nameof(name));

        if (!GraphQLNameValidator.IsValidName(name))
            throw new ArgumentException($"Root field name '{name}' is not a valid GraphQL name.", nameof(name));

        if (_queryRoots.ContainsKey(name) || _itemRoots.ContainsKey(name))
            throw new ArgumentException($"A root field named '{name}' is already registered.", nameof(name));
    }
}
