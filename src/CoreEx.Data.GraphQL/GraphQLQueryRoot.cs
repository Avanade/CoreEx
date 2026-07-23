namespace CoreEx.Data.GraphQL;

/// <summary>
/// Represents a registered GraphQL-lite query root field binding a GraphQL root field name to an underlying <see cref="QueryArgsConfig"/>-driven list query.
/// </summary>
/// <remarks>Bound to an existing <c>QueryAsync(QueryArgs?, PagingArgs?, CancellationToken)</c>-shaped delegate; see <see cref="GraphQLLiteOptions.AddQuery{TItem}(string, QueryArgsConfig, Func{QueryArgs?, PagingArgs?, CancellationToken, Task{IItemsResult{TItem}}})"/>.</remarks>
public sealed class GraphQLQueryRoot
{
    private readonly Func<QueryArgs?, PagingArgs?, CancellationToken, Task<IItemsResult>> _resolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="GraphQLQueryRoot"/> class.
    /// </summary>
    /// <param name="name">The GraphQL root field name.</param>
    /// <param name="itemType">The underlying item <see cref="Type"/> returned per row.</param>
    /// <param name="queryArgsConfig">The <see cref="QueryArgsConfig"/> used to validate/parse the <c>filter</c>/<c>orderby</c> arguments.</param>
    /// <param name="resolver">The underlying query resolver delegate.</param>
    internal GraphQLQueryRoot(string name, Type itemType, QueryArgsConfig queryArgsConfig, Func<QueryArgs?, PagingArgs?, CancellationToken, Task<IItemsResult>> resolver)
    {
        Name = name.ThrowIfNull();
        ItemType = itemType.ThrowIfNull();
        QueryArgsConfig = queryArgsConfig.ThrowIfNull();
        _resolver = resolver.ThrowIfNull();
    }

    /// <summary>
    /// Gets the GraphQL root field name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the underlying item <see cref="Type"/> returned per row.
    /// </summary>
    public Type ItemType { get; }

    /// <summary>
    /// Gets the <see cref="QueryArgsConfig"/> used to validate/parse the <c>filter</c>/<c>orderby</c> arguments.
    /// </summary>
    public QueryArgsConfig QueryArgsConfig { get; }

    /// <summary>
    /// Invokes the underlying query resolver.
    /// </summary>
    /// <param name="queryArgs">The <see cref="QueryArgs"/>.</param>
    /// <param name="pagingArgs">The <see cref="PagingArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IItemsResult"/>.</returns>
    public Task<IItemsResult> InvokeAsync(QueryArgs? queryArgs, PagingArgs? pagingArgs, CancellationToken cancellationToken) => _resolver(queryArgs, pagingArgs, cancellationToken);
}
