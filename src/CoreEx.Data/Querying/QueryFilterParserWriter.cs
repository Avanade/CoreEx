namespace CoreEx.Data.Querying;

/// <summary>
/// Represents the resulting dynamic LINQ filter writer.
/// </summary>
public partial class QueryFilterParserWriter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFilterParserWriter"/> class.
    /// </summary>
    /// <param name="config">The owning <see cref="QueryArgsConfig"/>.</param>
    internal QueryFilterParserWriter(QueryArgsConfig config) => Config = config.ThrowIfNull();

    /// <summary>
    /// Gets the owning <see cref="QueryArgsConfig"/>.
    /// </summary>
    public QueryArgsConfig Config { get; }

    /// <summary>
    /// Gets the resulting dynamic LINQ filter <see cref="StringBuilder"/>.
    /// </summary>
    internal StringBuilder FilterBuilder { get; } = new();

    /// <summary>
    /// Gets the resulting arguments referenced by the <see cref="FilterBuilder"/>.
    /// </summary>
    internal List<object?> Args { get; } = [];

    /// <summary>
    /// Appends a <paramref name="char"/> to the <see cref="FilterBuilder"/> prepended with a space if required.
    /// </summary>
    /// <param name="char">The chararater to append.</param>
    /// <remarks>Also appends a space if required.</remarks>
    internal void AppendWithSpacing(char @char)
    {
        if (FilterBuilder.Length > 0 && FilterBuilder[^1] != ' ' && FilterBuilder[^1] != '!' && FilterBuilder[^1] != '(')
        {
            if (!(@char == ')' && FilterBuilder[^1] == ')'))
                FilterBuilder.Append(' ');
        }

        FilterBuilder.Append(@char);
    }

    /// <summary>
    /// Appends a <paramref name="span"/> to the <see cref="FilterBuilder"/> prepended with a space if required.
    /// </summary>
    /// <param name="span">The span.</param>
    /// <remarks>Also appends a space if required.</remarks>
    internal void AppendWithSpacing(ReadOnlySpan<char> span)
    {
        if (FilterBuilder.Length > 0 && FilterBuilder[^1] != ' ' && FilterBuilder[^1] != '!' && FilterBuilder[^1] != '(')
            FilterBuilder.Append(' ');

        FilterBuilder.Append(span);
    }

    /// <summary>
    /// Appends a <paramref name="value"/> to the underlying dynamic LINQ statement as a placeholder, and captures the corresponding argument value.
    /// </summary>
    /// <param name="value">The value.</param>
    public void AppendValue(object? value)
    {
        Args.Add(value);
        FilterBuilder.Append($"@{Args.Count - 1}");
    }

    /// <summary>
    /// Appends the <paramref name="char"/> as-is to the underlying dynamic LINQ statement.
    /// </summary>
    /// <param name="char">The character.</param>
    public void Append(char @char) => FilterBuilder.Append(@char);

    /// <summary>
    /// Appends the <paramref name="span"/> as-is to the underlying dynamic LINQ statement.
    /// </summary>
    /// <param name="span">The span.</param>
    public void Append(ReadOnlySpan<char> span) => FilterBuilder.Append(span);

    /// <summary>
    /// Appends a <paramref name="statement"/> to the underlying dynamic LINQ statement.
    /// </summary>
    /// <param name="statement">The <see cref="QueryStatement"/>.</param>
    /// <remarks>Also appends an '<c> &amp;&amp; </c>' (and) prior to the <paramref name="statement"/> where neccessary.</remarks>
    public void AppendStatement(QueryStatement statement)
    {
        statement.ThrowIfNull();
        if (FilterBuilder.Length > 0)
            FilterBuilder.Append(" && ");

        // Renumber the statement's own '@n' placeholders in a single pass over the original (unmutated) text so that renumbered output can never be re-matched
        // by a later substitution - unlike a per-index StringBuilder.Replace loop, which can corrupt/misassign args once Args.Count already overlaps the
        // statement's own placeholder range, or once the statement has 10+ placeholders (e.g. "@1" matching the leading digits of "@10").
        var baseIndex = Args.Count;
        FilterBuilder.Append(PlaceholderRegex().Replace(statement.Statement, m => $"@{baseIndex + int.Parse(m.Groups[1].Value)}"));
        Args.AddRange(statement.Args);
    }

    [GeneratedRegex(@"@(\d+)")]
    private static partial Regex PlaceholderRegex();

    /// <inheritdoc/>
    public override string? ToString() => FilterBuilder.Length == 0 ? null : FilterBuilder.ToString();
}