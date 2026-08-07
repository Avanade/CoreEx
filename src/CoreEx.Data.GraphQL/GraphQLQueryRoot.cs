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
    /// <param name="options">The owning <see cref="GraphQLLiteOptions"/> - consulted live (not snapshotted) for <see cref="GraphQLLiteOptions.EnableSensitiveDataLogging"/> on every invocation.</param>
    internal GraphQLQueryRoot(string name, Type itemType, QueryArgsConfig queryArgsConfig, Func<QueryArgs?, PagingArgs?, CancellationToken, Task<IItemsResult>> resolver, GraphQLLiteOptions options)
    {
        Name = name.ThrowIfNull();
        ItemType = itemType.ThrowIfNull();
        QueryArgsConfig = queryArgsConfig.ThrowIfNull();
        _resolver = resolver.ThrowIfNull();
        _options = options.ThrowIfNull();
    }

    private readonly GraphQLLiteOptions _options;

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
    public async Task<IItemsResult> InvokeAsync(QueryArgs? queryArgs, PagingArgs? pagingArgs, CancellationToken cancellationToken)
    {
        if (ExecutionContext.HasCurrent)
        {
            // Where debug logging is enabled, log the invocation of the GraphQL query root. By default this is structural information only (whether a filter/order-by was
            // specified, and the paging window) - never the literal QueryArgs.Filter/OrderBy text, which embeds client-supplied filter values (e.g. "name eq 'Jane Doe'")
            // verbatim - unless EnableSensitiveDataLogging has been explicitly opted into (mirroring EF Core's option of the same name).
            var logger = ExecutionContext.GetService<ILogger<GraphQLQueryRoot>>();
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                if (_options.EnableSensitiveDataLogging)
                    logger.LogDebug("Invoking GraphQL query root '{Name}' with:\n  QueryArgs: [{QueryArgs}]\n  PagingArgs: [{PagingArgs}].", Name, queryArgs, pagingArgs);
                else
                    logger.LogDebug("Invoking GraphQL query root '{Name}' with: HasFilter={HasFilter}, HasOrderBy={HasOrderBy}, PagingArgs: [{PagingArgs}].",
                        Name, queryArgs?.Filter is not null, queryArgs?.OrderBy is not null, pagingArgs);
            }
        }

        return await _resolver(queryArgs, pagingArgs, cancellationToken).ConfigureAwait(false);
    }
}
