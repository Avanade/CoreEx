namespace CoreEx.Data.Querying;

/// <summary>
/// Represents the result of a <see cref="QueryFilterParser.Parse(string?)"/>.
/// </summary>
public sealed class QueryFilterParserResult : IQueryParseError
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFilterParserResult"/> class.
    /// </summary>
    internal QueryFilterParserResult(QueryArgsConfig config)
    {
        Config = config.ThrowIfNull();
        Writer = new(Config);
    }

    /// <summary>
    /// Gets the owning <see cref="QueryArgsConfig"/>.
    /// </summary>
    public QueryArgsConfig Config { get; }

    /// <inheritdoc/>
    public bool HasError => Error is not null;

    /// <summary>
    /// Throws the <see cref="Error"/> where <see cref="HasError"/>; otherwise, does nothing.
    /// </summary>
    public QueryFilterParserResult ThrowOnError() => HasError ? throw Error! : this;

    /// <inheritdoc/>
    ExtendedException? IQueryParseError.Error => Error;

    /// <summary>
    /// Gets the error represented as an <see cref="QueryFilterParserException"/> that occurred during parsing, if any.
    /// </summary>
    public QueryFilterParserException? Error { get; internal set; }

    /// <summary>
    /// Gets the field names referenced within the resulting LINQ query.
    /// </summary>
    public HashSet<string> Fields { get; } = [];

    /// <summary>
    /// Gets the <see cref="QueryFilterParserWriter"/>.
    /// </summary>
    public QueryFilterParserWriter Writer { get; }

    /// <summary>
    /// Provides the resulting dynamic LINQ filter and corresponding <paramref name="args"/>.
    /// </summary>
    public string? ToLinqString(out object?[] args)
    {
        args = [.. Writer.Args];
        return Writer.ToString();
    }

    /// <summary>
    /// Defaults the dynamic LINQ (see <see cref="ToLinqString(out object?[])"/>) with the specified <paramref name="statement"/> where not already set.
    /// </summary>
    /// <param name="statement">The <see cref="QueryStatement"/>.</param>
    public void UseDefault(QueryStatement? statement) => UseDefault(statement is null ? null : () => statement);

    /// <summary>
    /// Defaults the dynamic LINQ (see <see cref="ToLinqString(out object?[])"/>) with the specified <paramref name="statement"/> function where not already set.
    /// </summary>
    /// <param name="statement">The <see cref="QueryStatement"/> function.</param>
    public void UseDefault(Func<QueryStatement>? statement)
    {
        if (Writer.FilterBuilder.Length > 0)
            return;

        var stmt = statement?.Invoke();
        if (stmt is not null)
        {
            Writer.Append(stmt.Statement);
            Writer.Args.AddRange(stmt.Args);
        }
    }
}