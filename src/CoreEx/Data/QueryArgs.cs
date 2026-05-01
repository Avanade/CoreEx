namespace CoreEx.Data;

/// <summary>
/// Represents basic dynamic query arguments.
/// </summary>
/// <remarks>This is <b>not</b> intended to be a replacement for OData, GraphQL, etc. but to provide a limited, explicitly supported, dynamic capability to filter and order an underlying query.</remarks>
public class QueryArgs
{
    /// <summary>
    /// Create a new <see cref="QueryArgs"/>.
    /// </summary>
    /// <param name="filter">The basic dynamic <i>OData-like</i> <c>$filter</c> statement.</param>
    /// <param name="orderBy">The basic dynamic <i>OData-like</i> <c>$orderby</c> statement.</param>
    public static QueryArgs Create(string? filter = null, string? orderBy = null) => new() { Filter = filter, OrderBy = orderBy };

    /// <summary>
    /// Gets or sets the basic dynamic <i>OData-like</i> <c>$filter</c> statement.
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Gets or sets the basic dynamic <i>OData-like</i> <c>$orderby</c> statement.
    /// </summary>
    public string? OrderBy { get; set; }

    /// <summary>
    /// Gets or sets the list of <b>included</b> fields.
    /// </summary>
    /// <remarks>The <see cref="IncludeFields"/> and <see cref="ExcludeFields"/> are mutually exclusive.</remarks>
    public List<string>? IncludeFields { get; set; }

    /// <summary>
    /// Gets or sets the list of <b>excluded</b> fields.
    /// </summary>
    /// <remarks>The <see cref="IncludeFields"/> and <see cref="ExcludeFields"/> are mutually exclusive.</remarks>
    public List<string>? ExcludeFields { get; set; }

    /// <summary>
    /// Indicates whether to include any related texts for the resulting item(s).
    /// </summary>
    public bool IsIncludeText { get; set; }

    /// <summary>
    /// Indicates whether to include inactive items for the resulting item(s).
    /// </summary>
    public bool IsIncludeInactive { get; set; }

    /// <summary>
    /// Adds the specified fields to the <see cref="IncludeFields"/> list.
    /// </summary>
    /// <param name="fields">The fields to include.</param>
    /// <returns>The <see cref="QueryArgs"/> to support fluent-style method-chaining.</returns>
    public QueryArgs WithFields(params IEnumerable<string> fields)
    {
        IncludeFields ??= [];
        IncludeFields.AddRange(fields);
        return this;
    }

    /// <summary>
    /// Adds the specified fields to the <see cref="ExcludeFields"/> list.`
    /// </summary>
    /// <param name="fields">The fields to exclude.</param>
    /// <returns>The <see cref="QueryArgs"/> to support fluent-style method-chaining.</returns>
    public QueryArgs WithoutFields(params IEnumerable<string> fields)
    {
        ExcludeFields ??= [];
        ExcludeFields.AddRange(fields);
        return this;
    }

    /// <summary>
    /// Indicates whether to include any related texts for the resulting item(s); see <see cref="IsIncludeText"/>.
    /// </summary>
    /// <returns>The <see cref="QueryArgs"/> to support fluent-style method-chaining.</returns>
    public QueryArgs IncludeText()
    {
        IsIncludeText = true;
        return this;
    }

    /// <summary>
    /// Indicates whether to include inactive items for the resulting item(s); see <see cref="IsIncludeInactive"/>.
    /// </summary>
    /// <returns>The <see cref="QueryArgs"/> to support fluent-style method-chaining.</returns>
    public QueryArgs IncludeInactive()
    {
        IsIncludeInactive = true;
        return this;
    }
}