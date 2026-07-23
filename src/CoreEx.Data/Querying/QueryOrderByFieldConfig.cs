namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the <see cref="QueryOrderByParser"/> field configuration.
/// </summary>
/// <param name="parser">The owning <see cref="QueryOrderByParser"/>.</param>
/// <param name="field">The field name.</param>
/// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
public sealed class QueryOrderByFieldConfig(QueryOrderByParser parser, string field, string? model)
{
    private readonly string? _model = model;

    /// <summary>
    /// Gets the owning <see cref="QueryFilterParser"/>.
    /// </summary>
    public QueryOrderByParser Parser { get; internal set; } = parser.ThrowIfNull();

    /// <summary>
    /// Gets the field name.
    /// </summary>
    public string Field { get; } = field.ThrowIfNullOrEmpty(nameof(field));

    /// <summary>
    /// Gets or sets model name to be used for the dynamic LINQ expression.
    /// </summary>
    /// <remarks>Defaults to the <see cref="Field"/> name.</remarks>
    public string? Model => _model ?? Field;

    /// <summary>
    /// Gets the optional prefix to be used where referencing the underlying <see cref="IQueryable{T}"/> model.
    /// </summary>
    /// <remarks>This will default from <see cref="QueryOrderByParser.DefaultModelPrefix"/> when instantiated.</remarks>
    public string? ModelPrefix { get; private set; } = parser.DefaultModelPrefix;

    /// <summary>
    /// Gets the fully-qualified <see cref="Model"/> name (including any <see cref="ModelPrefix"/> where specified).
    /// </summary>
    public string FullyQualifiedModelName => (ModelPrefix is null ? string.Empty : ModelPrefix + ".") + Model;

    /// <summary>
    /// Gets the supported <see cref="QueryOrderByDirection"/>.
    /// </summary>
    /// <remarks>Defaults to <see cref="QueryOrderByDirection.Both"/>.</remarks>
    public QueryOrderByDirection Direction { get; private set; } = QueryOrderByDirection.Both;

    /// <summary>
    /// Gets the default <see cref="QueryOrderByDirection"/>.
    /// </summary>
    public QueryOrderByDirection? DefaultDirection { get; private set; }

    /// <summary>
    /// Indicates whether the field is to always be included in the query ordering.
    /// </summary>
    /// <remarks>Where not explicitly specified in the order by statement, this field will be included as the last order-by field.</remarks>
    public bool IsAlwaysInclude => AlwaysIncludeDirection.HasValue;

    /// <summary>
    /// Gets the <see cref="QueryOrderByDirection"/> to be always included in the query ordering.
    /// </summary>
    /// <remarks>Where not explicitly specified in the order by statement, this field will be included as the last order-by field.</remarks>
    public QueryOrderByDirection? AlwaysIncludeDirection { get; private set; }

    /// <summary>
    /// Gets the additional help text.
    /// </summary>
    public string? HelpText { get; private set; }

    /// <summary>
    /// Sets (overrides) the <see cref="Direction"/>.
    /// </summary>
    /// <param name="supportedDirection">The <see cref="QueryOrderByDirection"/>.</param>
    /// <returns>The <see cref="QueryOrderByFieldConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The default is <see cref="QueryOrderByDirection.Both"/>.</remarks>
    public QueryOrderByFieldConfig WithDirection(QueryOrderByDirection supportedDirection)
    {
        Direction = supportedDirection;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the optional <see cref="ModelPrefix"/> to be used where referencing the underlying <see cref="IQueryable{T}"/> model.
    /// </summary>
    /// <param name="modelPrefix">The model prefix.</param>
    /// <returns>The <see cref="QueryOrderByFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QueryOrderByFieldConfig WithModelPrefix(string modelPrefix)
    {
        ModelPrefix = modelPrefix.ThrowIfNullOrEmpty();
        return this;
    }

    /// <summary>
    /// Clears (overrides) the optional <see cref="ModelPrefix"/> to be used where referencing the underlying <see cref="IQueryable{T}"/> model.
    /// </summary>
    /// <returns>The <see cref="QueryOrderByFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QueryOrderByFieldConfig WithNoModelPrefix()
    {
        ModelPrefix = null;
        return this;
    }

    /// <summary>
    /// Sets (overrides) the default order-by and its <see cref="QueryOrderByDirection"/>.
    /// </summary>
    /// <param name="defaultDirection">The default <see cref="QueryOrderByDirection"/>.</param>
    /// <returns>The <see cref="QueryOrderByFieldConfig"/> to support fluent-style method-chaining.</returns>
    /// <remarks>This is used to define the overall default query ordering when none is specified; each field must also be defined in the order in which they are applied. By not specifying a field then this denotes
    /// that it is not to be included in the default query ordering.</remarks>
    public QueryOrderByFieldConfig WithDefault(QueryOrderByDirection defaultDirection = QueryOrderByDirection.Ascending)
    {
        DefaultDirection = defaultDirection.ThrowWhen(defaultDirection => defaultDirection == QueryOrderByDirection.Both, $"Default direction cannot be '{QueryOrderByDirection.Both}'.");
        return this;
    }

    /// <summary>
    /// Sets (overrides) the always included in the query ordering and its <see cref="QueryOrderByDirection"/>.
    /// </summary>
    /// <returns>The <see cref="QueryOrderByFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QueryOrderByFieldConfig WithAlwaysInclude(QueryOrderByDirection alwaysDirection = QueryOrderByDirection.Ascending)
    {
        AlwaysIncludeDirection = alwaysDirection.ThrowWhen(alwaysDirection => alwaysDirection == QueryOrderByDirection.Both, $"Always include direction cannot be '{QueryOrderByDirection.Both}'.");
        return this;
    }

    /// <summary>
    /// Sets (overrides) the additional help text.
    /// </summary>
    /// <param name="text">The additional help text.</param>
    /// <returns>The <see cref="QueryOrderByFieldConfig"/> to support fluent-style method-chaining.</returns>
    public QueryOrderByFieldConfig WithHelpText(string text)
    {
        HelpText = text.ThrowIfNullOrEmpty(nameof(text));
        return this;
    }
}