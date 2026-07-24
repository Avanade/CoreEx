namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Maps the GraphQL-native <c>where</c>/<c>orderBy</c>/<c>first</c>/<c>after</c>/<c>includeText</c>/<c>includeInactive</c> root field arguments onto <see cref="QueryArgs"/>/
/// <see cref="PagingArgs"/>, translating the structured <c>where</c>/<c>orderBy</c> input objects to the equivalent OData-esque <c>filter</c>/<c>orderby</c> strings consumed by
/// the underlying <see cref="QueryArgsConfig"/> (see <see cref="GraphQLFilterTranslator"/>/<see cref="GraphQLOrderByTranslator"/>).
/// </summary>
internal static class GraphQLArgsMapper
{
    /// <summary>
    /// Builds the <see cref="QueryArgs"/> from the resolved GraphQL field arguments, translating <c>where</c>/<c>orderBy</c> to their OData-esque equivalents.
    /// </summary>
    /// <param name="args">The resolved GraphQL field arguments.</param>
    /// <returns>The <see cref="QueryArgs"/>.</returns>
    public static QueryArgs BuildQueryArgs(IReadOnlyDictionary<string, object?> args)
    {
        var queryArgs = new QueryArgs
        {
            Filter = GraphQLFilterTranslator.Translate(args.TryGetValue("where", out var where) ? where : null),
            OrderBy = GraphQLOrderByTranslator.Translate(args.TryGetValue("orderBy", out var orderBy) ? orderBy : null)
        };

        if (args.GetBool("includeText") is true)
            queryArgs.IncludeText();

        if (args.GetBool("includeInactive") is true)
            queryArgs.IncludeInactive();

        return queryArgs;
    }

    /// <summary>
    /// Builds the underlying <see cref="PagingArgs"/> for a Relay Cursor Connections query root, from the resolved <c>first</c>/<c>after</c> GraphQL field arguments.
    /// </summary>
    /// <param name="args">The resolved GraphQL field arguments.</param>
    /// <param name="isCountRequested">Indicates whether the client's selection set requested <c>totalCount</c>, in which case the underlying query is asked to compute the total
    /// count; otherwise, the (potentially expensive) count query is skipped.</param>
    /// <returns>The <see cref="PagingArgs"/> to invoke the underlying query with (its <see cref="PagingArgs.Take"/> is deliberately one greater than the returned client-requested
    /// page size — an over-fetch used to derive <c>hasNextPage</c> without a second query) and that client-requested <c>first</c> page size.</returns>
    /// <exception cref="GraphQLArgumentTranslationException">Thrown where <c>last</c>/<c>before</c> (backward pagination) are specified, <c>first</c> is not greater than zero,
    /// or <c>after</c> is not a valid cursor.</exception>
    public static (PagingArgs PagingArgs, int First) BuildConnectionPagingArgs(IReadOnlyDictionary<string, object?> args, bool isCountRequested)
    {
        if (args.ContainsKey("last") || args.ContainsKey("before"))
            throw new GraphQLArgumentTranslationException("Backward pagination ('last'/'before') is not supported; use 'first'/'after'.");

        var first = args.GetInt("first") ?? PagingArgs.DefaultTake;
        if (first <= 0)
            throw new GraphQLArgumentTranslationException("'first' must be greater than zero.");

        var maxFirst = PagingArgs.MaximumTake > 1 ? PagingArgs.MaximumTake - 1 : 1;
        first = Math.Min(first, maxFirst);

        var skip = 0;
        var after = args.GetString("after");
        if (!string.IsNullOrEmpty(after))
        {
            if (!GraphQLCursor.TryDecode(after, out var offset))
                throw new GraphQLArgumentTranslationException("'after' is not a valid cursor.");

            skip = offset + 1;
        }

        return (new PagingArgs(skip, first + 1, isCountRequested), first);
    }
}
