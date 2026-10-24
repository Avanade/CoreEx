namespace CoreEx.Data.Querying;

/// <summary>
/// Represents the result of <see cref="QuerySearchParser.Parse(string?)"/>.
/// </summary>
public sealed class QuerySearchParserResult : IQueryParseError
{
    private readonly QuerySearchParser? _parser;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuerySearchParserResult"/> class.
    /// </summary>
    /// <param name="parser">The owning <see cref="QuerySearchParser"/>.</param>
    /// <param name="text">The normalized search text.</param>
    /// <param name="terms">The distinct search terms.</param>
    internal QuerySearchParserResult(QuerySearchParser parser, string? text, string[] terms)
    {
        _parser = parser.ThrowIfNull();
        Mode = parser.Mode;
        Text = text;
        Terms = terms;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QuerySearchParserResult"/> class with an error.
    /// </summary>
    /// <param name="error">The error <see cref="QuerySearchParserException"/>.</param>
    internal QuerySearchParserResult(QuerySearchParserException error) => Error = error.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="QuerySearchMode"/>.
    /// </summary>
    public QuerySearchMode Mode { get; }

    /// <summary>
    /// Indicates whether the search is ranked (i.e. <see cref="Mode"/> is not <see cref="QuerySearchMode.Keyword"/>) and is to be executed by the repository with results ordered by relevance.
    /// </summary>
    public bool IsRanked => Mode != QuerySearchMode.Keyword;

    /// <summary>
    /// Gets the normalized search text (trimmed with internal white space collapsed); <see langword="null"/> where not specified.
    /// </summary>
    /// <remarks>This is the value that should be passed to a ranked search capability (e.g. used to generate an embedding for a semantic search).</remarks>
    public string? Text { get; }

    /// <summary>
    /// Indicates whether there is search <see cref="Text"/>.
    /// </summary>
    public bool HasText => !string.IsNullOrEmpty(Text);

    /// <summary>
    /// Gets the distinct (case-insensitive) white-space separated search terms.
    /// </summary>
    public string[] Terms { get; } = [];

    /// <inheritdoc/>
    public bool HasError => Error is not null;

    /// <inheritdoc/>
    ExtendedException? IQueryParseError.Error => Error;

    /// <summary>
    /// Gets the error represented as an <see cref="QuerySearchParserException"/> that occurred during parsing, if any.
    /// </summary>
    public QuerySearchParserException? Error { get; }

    /// <summary>
    /// Throws the <see cref="Error"/> where <see cref="HasError"/>; otherwise, does nothing.
    /// </summary>
    public QuerySearchParserResult ThrowOnError() => HasError ? throw Error! : this;

    /// <summary>
    /// Provides the resulting <see cref="QuerySearchMode.Keyword"/> dynamic LINQ predicate.
    /// </summary>
    /// <param name="args">The resulting dynamic LINQ arguments.</param>
    /// <returns>The dynamic LINQ predicate; <see langword="null"/> where there is no <see cref="Text"/> or the search <see cref="IsRanked"/> (as must be executed by the repository).</returns>
    /// <remarks>Each <see cref="Terms">term</see> must match (AND) at least one of the configured fields (OR).</remarks>
    public string? ToLinqString(out object?[] args)
    {
        ThrowOnError();
        args = [];
        if (_parser is null || IsRanked || Terms.Length == 0)
            return null;

        var sb = new StringBuilder();
        var list = new List<object?>();
        foreach (var term in Terms)
        {
            if (sb.Length > 0)
                sb.Append(" && ");

            sb.Append('(');
            var first = true;
            foreach (var field in _parser.KeywordFields)
            {
                if (!first)
                    sb.Append(" || ");

                field.WriteAsDynamicLinq(sb, list, term);
                first = false;
            }

            sb.Append(')');
        }

        args = [.. list];
        return sb.ToString();
    }

    /// <inheritdoc/>
    public override string ToString() => Text ?? string.Empty;
}
