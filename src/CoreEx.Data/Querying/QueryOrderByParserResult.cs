namespace CoreEx.Data.Querying;

/// <summary>
/// Represents the result of <see cref="QueryOrderByParser.Parse(string?)"/>.
/// </summary>
public sealed class QueryOrderByParserResult : IQueryParseError
{
    private readonly string? _orderByStatement;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryOrderByParserResult"/> class.
    /// </summary>
    /// <param name="orderByStatement">The resulting dynamic LINQ order by statement.</param>
    internal QueryOrderByParserResult(string? orderByStatement) => _orderByStatement = orderByStatement;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryOrderByParserResult"/> class with an error.
    /// </summary>
    /// <param name="error">The error <see cref="QueryOrderByParserException"/>.</param>
    internal QueryOrderByParserResult(QueryOrderByParserException error) => Error = error.ThrowIfNull();

    /// <inheritdoc/>
    public bool HasError => Error is not null;

    /// <inheritdoc/>
    ExtendedException? IQueryParseError.Error => Error;

    /// <summary>
    /// Gets the error represented as an <see cref="QueryOrderByParserException"/> that occurred during parsing, if any.
    /// </summary>
    public QueryOrderByParserException? Error { get; internal set; }

    /// <summary>
    /// Throws the <see cref="Error"/> where <see cref="HasError"/>; otherwise, does nothing.
    /// </summary>
    public QueryOrderByParserResult ThrowOnError() => HasError ? throw Error!: this;

    /// <summary>
    /// Provides the resulting dynamic LINQ order by.
    /// </summary>
    public string? ToLinqString() => _orderByStatement;
}