namespace CoreEx.Data.Querying;

/// <summary>
/// Provides query-oriented extension methods.
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Adds a dynamic query <see cref="QueryArgs.Filter"/> (basic dynamic <i>OData-like</i> <c>$filter</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="queryConfig">The <see cref="QueryArgsConfig"/>.</param>
    /// <param name="queryArgs">The <see cref="QueryArgs"/>.</param>
    /// <returns>The query.</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, QueryArgsConfig queryConfig, QueryArgs? queryArgs = null) => Where(source, queryConfig, queryArgs?.Filter);

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
    /// Adds a dynamic query filter (basic dynamic <i>OData-like</i> <c>$filter</c> statement).
    /// </summary>
    /// <typeparam name="TSource">The <paramref name="source"/> element <see cref="Type"/>.</typeparam>
    /// <param name="source">The sequence of elements.</param>
    /// <param name="result">The <see cref="QueryArgsParseResult"/> that contains the parsed dynamic <i>OData-like</i> <c>$filter</c> statement.</param>
    /// <returns>The query.</returns>
    public static IQueryable<TSource> Where<TSource>(this IQueryable<TSource> source, QueryArgsParseResult result)
    {
        result.ThrowIfNull().ThrowOnError();
        if (result.FilterResult is null)
            return source;

        var linq = result.FilterResult.ToLinqString(out var args);
        return string.IsNullOrEmpty(linq) ? source : source.Where(result.Config.ParsingConfig, linq, [.. args]);
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