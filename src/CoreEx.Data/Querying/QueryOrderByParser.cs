namespace CoreEx.Data.Querying;

/// <summary>
/// Represents a basic query sort order by parser and LINQ translator with explicitly defined field support.
/// </summary>
/// <remarks>This is <b>not</b> intended to be a replacement for OData, GraphQL, etc. but to provide a limited, explicitly supported, dynamic capability to sort an underlying query.</remarks>
/// <param name="owner">The owning <see cref="QueryArgsConfig"/>.</param>
public sealed class QueryOrderByParser(QueryArgsConfig owner)
{
    private readonly List<QueryOrderByFieldConfig> _fields = [];
    private Action<string[]>? _validator;
    private string? _helpText;
    private string? _defaultOrderBy;
    private string? _defaultOrderByStatement;

    /// <summary>
    /// Gets the owning <see cref="QueryArgsConfig"/>.
    /// </summary>
    public QueryArgsConfig Owner => owner.ThrowIfNull();

    /// <summary>
    /// Indicates that at least a single field has been configured.
    /// </summary>
    public bool HasFields => _fields.Count > 0;

    /// <summary>
    /// Gets the default <i>OData-like</i> <c>$orderby</c> statement.
    /// </summary>
    public string? DefaultOrderBy => _defaultOrderBy ??= string.Join(", ", _fields.Where(f => f.DefaultDirection is not null).Select(f => f.Field.ToLowerInvariant() + (f.DefaultDirection == QueryOrderByDirection.Ascending ? " asc" : "desc")));

    /// <summary>
    /// Gets the default model prefix (if any).
    /// </summary>
    /// <remarks>This will be automatically applied to all subsequent field additions; for example <see cref="AddField(string, string?, Action{QueryOrderByFieldConfig}?)"/>.</remarks>
    public string? DefaultModelPrefix { get; private set; }

    /// <summary>
    /// Sets (overrides) the <see cref="DefaultModelPrefix"/> (if any).
    /// </summary>
    /// <param name="modelPrefix"></param>
    /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
    public QueryOrderByParser WithDefaultModelPrefix(string? modelPrefix = null)
    {
        DefaultModelPrefix = modelPrefix;
        return this;
    }

    /// <summary>
    /// Adds a <see cref="QueryOrderByFieldConfig"/> to the parser for the specified <paramref name="field"/> as-is.
    /// </summary>
    /// <param name="field">The field name used in the order by specified with the correct casing.</param>
    /// <param name="configure">The optional action enabling further field configuration.</param>
    /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
    /// <remarks>To avoid unnecessary parsing this should be the valid dynamic LINQ statement.</remarks>
    public QueryOrderByParser AddField(string field, Action<QueryOrderByFieldConfig>? configure = null) => AddField(field, null, configure);

    /// <summary>
    /// Adds a <see cref="QueryOrderByFieldConfig"/> to the parser using the specified <paramref name="field"/> and <paramref name="model"/>.
    /// </summary>
    /// <param name="field">The field name used in the query filter.</param>
    /// <param name="model">The model name (defaults to <paramref name="field"/>.</param>
    /// <param name="configure">The optional action enabling further field configuration.</param>
    /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
    public QueryOrderByParser AddField(string field, string? model, Action<QueryOrderByFieldConfig>? configure = null)
    {
        var config = new QueryOrderByFieldConfig(this, field, model);

        field.ThrowWhen(field => _fields.Any(f => f.Field.Equals(field, StringComparison.OrdinalIgnoreCase)), $"The order-by field '{field}' has already been added and must be unique.");

        configure?.Invoke(config);
        _fields.Add(config);

        _defaultOrderBy = null;
        _defaultOrderByStatement = null;
        return this;
    }

    /// <summary>
    /// Sets (override) the additional help text.
    /// </summary>
    /// <param name="text">The additional help text.</param>
    /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
    public QueryOrderByParser WithHelpText(string text)
    {
        _helpText = text;
        return this;
    }

    /// <summary>
    /// Sets (overrides) a <paramref name="validator"/> that can be used to further validate the fields specified in the order by.
    /// </summary>
    /// <param name="validator">The validator action.</param>
    /// <returns>The <see cref="QueryOrderByParser"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Throw a <see cref="QueryOrderByParserException"/> to have the validation message formatted correctly and consistently.
    /// <para>The <c>string[]</c> passed into the validator will contain the parsed fields (names) in the order in which they were specified.</para></remarks>
    public QueryOrderByParser WithValidator(Action<string[]>? validator)
    {
        _validator = validator;
        return this;
    }

    /// <summary>
    /// Parses and converts the <paramref name="orderBy"/> to dynamic LINQ.
    /// </summary>
    /// <param name="orderBy">The query order-by.</param>
    /// <returns>The <see cref="QueryOrderByParserResult"/>.</returns>
    public QueryOrderByParserResult Parse(string? orderBy)
    {
        var usingDefault = false;
        if (string.IsNullOrEmpty(orderBy))
        {
            if (_defaultOrderByStatement is not null)
                return new QueryOrderByParserResult(_defaultOrderByStatement);

            usingDefault = true;
            orderBy = DefaultOrderBy;
            if (orderBy is null)
                return new QueryOrderByParserResult(orderBy);
        }

        var fields = new List<string>();
        var sb = new StringBuilder();

        try
        {
            foreach (var sort in orderBy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var parts = sort.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts.Length == 0)
                    continue;
                else if (parts.Length > 2)
                    throw new QueryOrderByParserException("Statement is syntactically incorrect.");

                var field = parts[0];
                var config = _fields.FirstOrDefault(f => f.Field.Equals(field, StringComparison.OrdinalIgnoreCase)) ?? throw new QueryOrderByParserException($"Field '{field}' is not supported.");

                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(config.FullyQualifiedModelName);

                var dir = parts.Length == 2 ? parts[1].Trim() : null;
                if (dir is not null)
                {
                    var direction = QueryOrderByDirection.Ascending;
                    if (dir.Length > 3 && nameof(QueryOrderByDirection.Descending).StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                    {
                        sb.Append(" desc");
                        direction = QueryOrderByDirection.Descending;
                    }
                    else if (!(dir.Length > 2 && nameof(QueryOrderByDirection.Ascending).StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
                        throw new QueryOrderByParserException($"Field '{field}' direction '{dir}' is invalid; must be either 'asc' (ascending) or 'desc' (descending).");

                    if (!config.Direction.HasFlag(direction))
                        throw new QueryOrderByParserException($"Field '{field}' direction '{dir}' is invalid; not supported.");
                }

                if (fields.Contains(config.Field))
                    throw new QueryOrderByParserException($"Field '{field}' must not be specified more than once.");

                fields.Add(config.Field);
            }

            foreach (var config in _fields.Where(x => x.IsAlwaysInclude))
            {
                if (fields.Contains(config.Field))
                    continue;

                if (sb.Length > 0)
                    sb.Append(", ");

                sb.Append(config.FullyQualifiedModelName);
                if (config.AlwaysIncludeDirection == QueryOrderByDirection.Descending)
                    sb.Append(" desc");
            }

            _validator?.Invoke([.. fields]);
        }
        catch (QueryOrderByParserException qobpex)
        {
            qobpex.WithExtension("schema", owner.ToJsonSchema());
            return new QueryOrderByParserResult(qobpex);
        }

        if (usingDefault)
        {
            _defaultOrderByStatement = sb.ToString();
            return new QueryOrderByParserResult(_defaultOrderByStatement);
        }
        else
            return new QueryOrderByParserResult(sb.ToString());
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var sb = new StringBuilder();

        if (HasFields)
        {
            sb.Append("Order-by field(s) are as follows:");
            foreach (var field in _fields)
            {
                sb.AppendLine().Append(field.Field.ToLowerInvariant()).Append(" (Direction: ");
                if (field.Direction == QueryOrderByDirection.Ascending)
                    sb.Append("asc");
                else if (field.Direction == QueryOrderByDirection.Descending)
                    sb.Append("desc");
                else
                    sb.Append("asc or desc");

                sb.Append(')');
            }
        }
        else
            sb.Append("Order-By statement is not currently supported.");

        if (!string.IsNullOrEmpty(DefaultOrderBy))
            sb.AppendLine().AppendLine("---").Append("Default: ").Append(DefaultOrderBy);

        if (!string.IsNullOrEmpty(_helpText))
            sb.AppendLine().AppendLine("---").Append("Note: ").Append(_helpText);

        return sb.ToString();
    }

    /// <summary>
    /// Produces the JSON schema for the configured fields.
    /// </summary>
    /// <returns>The <see cref="JsonElement"/>.</returns>
    public JsonElement ToJsonSchema()
    {
        var dict = new Dictionary<string, object?>();
        foreach (var field in _fields)
        {
            string[] directions = field.Direction switch
            {

                QueryOrderByDirection.Ascending => ["asc"],
                QueryOrderByDirection.Descending => ["desc"],
                _ => ["asc", "desc"]
            };

            dict.Add(field.Field.ToLowerInvariant(), new { direction = directions });
        }

        var root = new Dictionary<string, object?>
        {
            { "fields", HasFields ? dict : null }
        };

        if (!string.IsNullOrEmpty(DefaultOrderBy))
            root["default"] = DefaultOrderBy;

        if (!string.IsNullOrEmpty(_helpText))
            root["description"] = _helpText;

        return JsonSerializer.SerializeToElement(root);
    }
}