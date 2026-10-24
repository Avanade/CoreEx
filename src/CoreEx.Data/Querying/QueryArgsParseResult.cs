namespace CoreEx.Data.Querying;

/// <summary>
/// Represents the <see cref="QueryArgsConfig.Parse"/> result.
/// </summary>
public sealed class QueryArgsParseResult : IQueryParseError, IToResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryArgsParseResult"/> class.
    /// </summary>
    /// <param name="config">The owning <see cref="QueryArgsConfig"/>.</param>
    /// <param name="filterResult">The <see cref="QueryFilterParserResult"/>.</param>
    /// <param name="orderByResult">The <see cref="QueryOrderByParserResult"/>.</param>
    /// <param name="searchResult">The <see cref="QuerySearchParserResult"/>.</param>
    internal QueryArgsParseResult(QueryArgsConfig config, QueryFilterParserResult? filterResult = null, QueryOrderByParserResult? orderByResult = null, QuerySearchParserResult? searchResult = null)
    {
        Config = config;
        FilterResult = filterResult;
        OrderByResult = orderByResult;
        SearchResult = searchResult;
    }

    /// <summary>
    /// Gets the owning <see cref="QueryArgsConfig"/>.
    /// </summary>
    public QueryArgsConfig Config { get; }

    /// <summary>
    /// Gets the <see cref="QueryOrderByParserResult"/>.
    /// </summary>
    public QueryFilterParserResult? FilterResult { get; }

    /// <summary>
    /// Gets the <see cref="QueryOrderByParserResult"/>.
    /// </summary>
    public QueryOrderByParserResult? OrderByResult { get; }

    /// <summary>
    /// Gets the <see cref="QuerySearchParserResult"/>; <see langword="null"/> where no search text was specified.
    /// </summary>
    public QuerySearchParserResult? SearchResult { get; }

    /// <summary>
    /// Indicates whether a <see cref="QuerySearchParserResult.IsRanked">ranked</see> search was specified; as such, the repository is responsible for executing the search (using the <see cref="QuerySearchParserResult.Text"/>)
    /// and ordering the results by relevance.
    /// </summary>
    public bool IsRankedSearch => SearchResult is not null && SearchResult.IsRanked;

    /// <inheritdoc/>
    public bool HasError => Error is not null;

    /// <inheritdoc/>
    public ExtendedException? Error { get; internal set; }

    /// <summary>
    /// Throws the <see cref="Error"/> where <see cref="HasError"/>; otherwise, does nothing.
    /// </summary>
    public QueryArgsParseResult ThrowOnError()
    {
        if (HasError)
            throw Error!;

        return this;
    }

    /// <inheritdoc/>
    public Result ToResult() => HasError ? Result.Fail(Error!) : Result.Success;
}