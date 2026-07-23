namespace CoreEx.Data.GraphQL.Internal;

/// <summary>
/// Maps the fixed, documented GraphQL-lite argument convention (<c>filter</c>, <c>orderby</c>, <c>skip</c>, <c>take</c>, <c>count</c>, <c>includeText</c>, <c>includeInactive</c>) onto
/// <see cref="QueryArgs"/>/<see cref="PagingArgs"/> — identical semantics to the REST <c>$filter</c>/<c>$orderby</c>/<c>$skip</c>/<c>$take</c>/<c>$count</c> query strings.
/// </summary>
internal static class GraphQLArgsMapper
{
    /// <summary>
    /// Builds the <see cref="QueryArgs"/> from the resolved GraphQL field arguments.
    /// </summary>
    public static QueryArgs BuildQueryArgs(IReadOnlyDictionary<string, object?> args)
    {
        var queryArgs = new QueryArgs { Filter = args.GetString("filter"), OrderBy = args.GetString("orderby") };
        if (args.GetBool("includeText") is true)
            queryArgs.IncludeText();

        if (args.GetBool("includeInactive") is true)
            queryArgs.IncludeInactive();

        return queryArgs;
    }

    /// <summary>
    /// Builds the <see cref="PagingArgs"/> from the resolved GraphQL field arguments.
    /// </summary>
    public static PagingArgs BuildPagingArgs(IReadOnlyDictionary<string, object?> args) => new(args.GetInt("skip") ?? 0, args.GetInt("take"), args.GetBool("count") ?? false);
}
