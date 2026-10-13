namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the <see cref="QuerySearchParser"/> <see cref="QuerySearchMode.Keyword"/> field configuration.
/// </summary>
/// <param name="parser">The owning <see cref="QuerySearchParser"/>.</param>
/// <param name="model">The model name to be used for the dynamic LINQ expression.</param>
public sealed class QuerySearchFieldConfig(QuerySearchParser parser, string model)
{
    /// <summary>
    /// Gets the owning <see cref="QuerySearchParser"/>.
    /// </summary>
    public QuerySearchParser Parser { get; } = parser.ThrowIfNull();

    /// <summary>
    /// Gets the model name to be used for the dynamic LINQ expression.
    /// </summary>
    /// <remarks>The model must be of <see cref="Type"/> <see cref="string"/>.</remarks>
    public string Model { get; } = model.ThrowIfNullOrEmpty(nameof(model));

    /// <summary>
    /// Gets the optional prefix to be used where referencing the underlying <see cref="IQueryable{T}"/> model.
    /// </summary>
    /// <remarks>This will default from <see cref="QuerySearchParser.DefaultModelPrefix"/> when instantiated.</remarks>
    public string? ModelPrefix { get; private set; } = parser.DefaultModelPrefix;

    /// <summary>
    /// Gets the fully-qualified <see cref="Model"/> name (including any <see cref="ModelPrefix"/> where specified).
    /// </summary>
    public string FullyQualifiedModelName => (ModelPrefix is null ? string.Empty : ModelPrefix + ".") + Model;

    /// <summary>
    /// Gets the <see cref="QueryFilterOperator"/> used to match a search term against the field.
    /// </summary>
    /// <remarks>Defaults to <see cref="QueryFilterOperator.Contains"/>.</remarks>
    public QueryFilterOperator Operator { get; private set; } = QueryFilterOperator.Contains;

    /// <summary>
    /// Indicates whether the field and search term are to be converted to upper case before comparison.
    /// </summary>
    public bool IsToUpper { get; private set; }

    /// <summary>
    /// Indicates whether the field and search term are to be converted to lower case before comparison.
    /// </summary>
    public bool IsToLower { get; private set; }

    /// <summary>
    /// Indicates whether a <c>!= null</c> check is to be included before the comparison.
    /// </summary>
    /// <remarks>Defaults to <see langword="true"/>.</remarks>
    public bool IsCheckNotNull { get; private set; } = true;

    /// <summary>
    /// Sets (overrides) the <see cref="Operator"/>.
    /// </summary>
    /// <param name="operator">The <see cref="QueryFilterOperator"/>; must be one of <see cref="QueryFilterOperator.Contains"/>, <see cref="QueryFilterOperator.StartsWith"/>, <see cref="QueryFilterOperator.EndsWith"/> or <see cref="QueryFilterOperator.Equal"/>.</param>
    /// <returns>The <see cref="QuerySearchFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QuerySearchFieldConfig WithOperator(QueryFilterOperator @operator)
    {
        Operator = @operator.ThrowWhen(o => o is not (QueryFilterOperator.Contains or QueryFilterOperator.StartsWith or QueryFilterOperator.EndsWith or QueryFilterOperator.Equal),
            $"Operator must be one of {QueryFilterOperator.Contains}, {QueryFilterOperator.StartsWith}, {QueryFilterOperator.EndsWith} or {QueryFilterOperator.Equal}.");

        return this;
    }

    /// <summary>
    /// Indicates that the field and search term are to be converted to upper case before comparison (see <see cref="IsToUpper"/>).
    /// </summary>
    /// <returns>The <see cref="QuerySearchFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QuerySearchFieldConfig AsUpperCase()
    {
        IsToUpper = true;
        IsToLower = false;
        return this;
    }

    /// <summary>
    /// Indicates that the field and search term are to be converted to lower case before comparison (see <see cref="IsToLower"/>).
    /// </summary>
    /// <returns>The <see cref="QuerySearchFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QuerySearchFieldConfig AsLowerCase()
    {
        IsToLower = true;
        IsToUpper = false;
        return this;
    }

    /// <summary>
    /// Indicates that the <c>!= null</c> check is to be excluded (see <see cref="IsCheckNotNull"/>).
    /// </summary>
    /// <returns>The <see cref="QuerySearchFieldConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Only use where the underlying field is guaranteed to be non-<see langword="null"/>, or the data source handles <see langword="null"/> comparisons natively.</remarks>
    public QuerySearchFieldConfig WithNoNullCheck()
    {
        IsCheckNotNull = false;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the optional <see cref="ModelPrefix"/> to be used where referencing the underlying <see cref="IQueryable{T}"/> model.
    /// </summary>
    /// <param name="modelPrefix">The model prefix.</param>
    /// <returns>The <see cref="QuerySearchFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QuerySearchFieldConfig WithModelPrefix(string modelPrefix)
    {
        ModelPrefix = modelPrefix.ThrowIfNullOrEmpty();
        return this;
    }

    /// <summary>
    /// Clears (overrides) the optional <see cref="ModelPrefix"/> to be used where referencing the underlying <see cref="IQueryable{T}"/> model.
    /// </summary>
    /// <returns>The <see cref="QuerySearchFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QuerySearchFieldConfig WithNoModelPrefix()
    {
        ModelPrefix = null;
        return this;
    }

    /// <summary>
    /// Writes the dynamic LINQ comparison for the specified <paramref name="term"/>.
    /// </summary>
    internal void WriteAsDynamicLinq(StringBuilder sb, List<object?> args, string term)
    {
        var field = FullyQualifiedModelName + (IsToUpper ? ".ToUpper()" : IsToLower ? ".ToLower()" : string.Empty);
        var placeholder = $"@{args.Count}";
        args.Add(IsToUpper ? term.ToUpperInvariant() : IsToLower ? term.ToLowerInvariant() : term);

        if (IsCheckNotNull)
            sb.Append('(').Append(FullyQualifiedModelName).Append(" != null && ");

        sb.Append(Operator switch
        {
            QueryFilterOperator.StartsWith => $"{field}.StartsWith({placeholder})",
            QueryFilterOperator.EndsWith => $"{field}.EndsWith({placeholder})",
            QueryFilterOperator.Equal => $"{field} == {placeholder}",
            _ => $"{field}.Contains({placeholder})"
        });

        if (IsCheckNotNull)
            sb.Append(')');
    }
}
