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
    internal QueryArgsParseResult(QueryArgsConfig config, QueryFilterParserResult? filterResult = null, QueryOrderByParserResult? orderByResult = null)
    {
        Config = config;
        FilterResult = filterResult;
        OrderByResult = orderByResult;
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