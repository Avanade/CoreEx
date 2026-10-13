namespace CoreEx.Data.Querying;

/// <summary>
/// Provides query-oriented extension methods.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Adds a dynamic query <see cref="QueryArgs.Filter"/> (basic dynamic <i>OData-like</i> <c>$filter</c> statement) and <see cref="QuerySearchMode.Keyword"/> <see cref="QueryArgs.Search"/> (free-text <c>$search</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
    /// <param name="queryArgs">The <see cref="QueryArgs"/>.</param>
    /// <returns>The query.</returns>
    /// <remarks>A <see cref="QuerySearchParser.IsRanked">ranked</see> search is <b>not</b> applied; this is the responsibility of the repository.</remarks>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, QueryArgsConfig queryConfig, QueryArgs? queryArgs = null) => WhereSearch(Where(source, queryConfig, queryArgs?.Filter), queryConfig, queryArgs?.Search);

    /// <summary>
    /// Adds a dynamic query <see cref="QuerySearchMode.Keyword"/> <paramref name="search"/> (free-text <c>$search</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
    /// <param name="search">The free-text <c>$search</c> statement.</param>
    /// <returns>The query.</returns>
    /// <remarks>A <see cref="QuerySearchParser.IsRanked">ranked</see> search is <b>not</b> applied; this is the responsibility of the repository.</remarks>
    public static IQueryable<TSource> WhereSearch<TSource>(this IQueryable<TSource> source, QueryArgsConfig queryConfig, string? search)
    {
        queryConfig.ThrowIfNull();
        if (string.IsNullOrWhiteSpace(search))
            return source;

        if (!queryConfig.HasSearchParser)
            throw new QuerySearchParserException("Search statement is not currently supported.");

        return WhereSearch(source, queryConfig, queryConfig.SearchParser.Parse(search).ThrowOnError());
    }

    /// <summary>
    /// Applies the <see cref="QuerySearchMode.Keyword"/> search predicate (where applicable).
    /// </summary>
    private static IQueryable<TSource> WhereSearch<TSource>(IQueryable<TSource> source, QueryArgsConfig queryConfig, QuerySearchParserResult? result)
    {
        if (result is null)
            return source;

        var linq = result.ToLinqString(out var args);
        return string.IsNullOrEmpty(linq) ? source : source.Where(queryConfig.ParsingConfig, linq, [.. args]);
    }

    /// <summary>
    /// Adds a dynamic query <paramref name="filter"/> (basic dynamic <i>OData-like</i> <c>$filter</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
    /// <param name="filter">The basic dynamic <i>OData-like</i> <c>$filter</c> statement.</param>
    /// <returns>The query.</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, QueryArgsConfig queryConfig, string? filter)
    {
        queryConfig.ThrowIfNull();
        if (!queryConfig.HasFilterParser && !string.IsNullOrEmpty(filter))
            throw new QueryFilterParserException("Query filter statement is not currently supported.");

        if (!queryConfig.HasFilterParser)
            return source;

        var result = queryConfig.FilterParser.Parse(filter).ThrowOnError();
        var linq = result.ToLinqString(out var args);
        return string.IsNullOrEmpty(linq) ? source : source.Where(result.Config.ParsingConfig, linq, [.. args]);
    }

    /// <summary>
    /// Adds a dynamic query filter (basic dynamic <i>OData-like</i> <c>$filter</c> statement) and <see cref="QuerySearchMode.Keyword"/> search (free-text <c>$search</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="result">The <see cref="QueryArgsParseResult"/> that contains the parsed dynamic <i>OData-like</i> <c>$filter</c> and <c>$search</c> statements.</param>
    /// <returns>The query.</returns>
    /// <remarks>A <see cref="QueryArgsParseResult.IsRankedSearch">ranked</see> search is <b>not</b> applied; this is the responsibility of the repository.</remarks>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, QueryArgsParseResult result)
    {
        result.ThrowIfNull().ThrowOnError();
        if (result.FilterResult is not null)
        {
            var linq = result.FilterResult.ToLinqString(out var args);
            if (!string.IsNullOrEmpty(linq))
                source = source.Where(result.Config.ParsingConfig, linq, [.. args]);
        }

        return WhereSearch(source, result.Config, result.SearchResult);
    }

    /// <summary>
    /// Adds a dynamic query <see cref="QueryArgs.OrderBy"/> (basic dynamic <i>OData-like</i> <c>$orderby</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
    /// <param name="queryArgs">The <see cref="QueryArgs"/>.</param>
    /// <returns>The query.</returns>
    public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, QueryArgsConfig queryConfig, QueryArgs? queryArgs = null) => OrderBy(source, queryConfig, queryArgs?.OrderBy);

    /// <summary>
    /// Adds a dynamic query <paramref name="orderby"/> (basic dynamic <i>OData-like</i> <c>$orderby</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
    /// <param name="orderby">The basic dynamic <i>OData-like</i> <c>$orderby</c> statement.</param>
    /// <returns>The query.</returns>
    public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, QueryArgsConfig queryConfig, string? orderby)
    {
        queryConfig.ThrowIfNull();
        if (!queryConfig.HasOrderByParser && !string.IsNullOrEmpty(orderby))
            throw new QueryOrderByParserException("OrderBy filter is not currently supported.");

        if (!queryConfig.HasOrderByParser)
            return source;

        var result = queryConfig.OrderByParser.Parse(orderby).ThrowOnError();
        var linq = result.ToLinqString();
        return string.IsNullOrEmpty(linq) ? source : source.OrderBy(linq);
    }

    /// <summary>
    /// Adds a dynamic query order-by (basic dynamic <i>OData-like</i> <c>$orderby</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="result">The <see cref="QueryArgsParseResult"/> that contains the parsed dynamic <i>OData-like</i> <c>$orderby</c> statement.</param>
    /// <returns>The query.</returns>
    public static IQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> source, QueryArgsParseResult result)
    {
        result.ThrowIfNull().ThrowOnError();
        if (result.OrderByResult is null)
            return source;

        var linq = result.OrderByResult.ToLinqString();
        return string.IsNullOrEmpty(linq) ? source : source.OrderBy(linq);
    }
}