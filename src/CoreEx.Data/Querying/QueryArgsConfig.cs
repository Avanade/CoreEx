namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the ODATA-esque dynamic LINQ queries <see cref="QueryArgs"/> execution configuration.
/// </summary>
public class QueryArgsConfig
{
    private QueryFilterParser? _filterParser;
    private QueryOrderByParser? _orderByParser;

    /// <summary>
    /// Creates a new <see cref="QueryArgsConfig"/>.
    /// </summary>
    /// <returns>The <see cref="QueryArgsConfig"/>.</returns>
    public static QueryArgsConfig Create() => new();

    /// <summary>
    /// Gets the <see cref="System.Linq.Dynamic.Core.ParsingConfig"/>.
    /// </summary>
    public ParsingConfig ParsingConfig { get; protected set; } = new ParsingConfig();

    /// <summary>
    /// Gets the <see cref="QueryFilterParser"/>.
    /// </summary>
    public QueryFilterParser FilterParser => _filterParser ??= new QueryFilterParser(this);

    /// <summary>
    /// Indicates whether there is a <see cref="FilterParser"/>.
    /// </summary>
    public bool HasFilterParser => _filterParser is not null;

    /// <summary>
    /// Gets the <see cref="QueryOrderByParser"/>.
    /// </summary>
    public QueryOrderByParser OrderByParser => _orderByParser ??= new QueryOrderByParser(this);

    /// <summary>
    /// Indicates whether there is an <see cref="OrderByParser"/>.
    /// </summary>
    public bool HasOrderByParser => _orderByParser is not null;

    /// <summary>
    /// Enables fluent-style method-chaining configuration for the <see cref="FilterParser"/>.
    /// </summary>
    /// <param name="filter">The <see cref="FilterParser"/>.</param>
    /// <returns>The <see cref="QueryArgsConfig"/> instance to support fluent-style method-chaining.</returns>
    public QueryArgsConfig WithFilter(Action<QueryFilterParser> filter)
    {
        filter.ThrowIfNull()(FilterParser);
        return this;
    }

    /// <summary>
    /// Enables fluent-style method-chaining configuration for the <see cref="OrderByParser"/>.
    /// </summary>
    /// <param name="orderBy">The <see cref="OrderByParser"/>.</param>
    /// <returns>The <see cref="QueryArgsConfig"/> instance to support fluent-style method-chaining.</returns>
    public QueryArgsConfig WithOrderBy(Action<QueryOrderByParser> orderBy)
    {
        orderBy.ThrowIfNull()(OrderByParser);
        return this;
    }

    /// <summary>
    /// Parses and converts the <see cref="QueryArgs.Filter"/> and <see cref="QueryArgs.OrderBy"/> to dynamic LINQ.
    /// </summary>
    /// <param name="queryArgs">The <see cref="QueryArgs"/>.</param>
    /// <returns>The <see cref="QueryArgsParseResult"/>.</returns>
    public QueryArgsParseResult Parse(QueryArgs? queryArgs)
    {
        var result = new QueryArgsParseResult(this);
        if (queryArgs is null)
            return result;

        QueryFilterParserResult? filterParserResult = null;
        if (!string.IsNullOrEmpty(queryArgs.Filter))
        {
            if (HasFilterParser)
            {
                filterParserResult = FilterParser.Parse(queryArgs.Filter);
                if (filterParserResult.HasError)
                    return result.Adjust(x => x.Error = filterParserResult.Error);
            }
            else
                return result.Adjust(x => x.Error = new QueryFilterParserException("Filter statement is not currently supported."));
        }

        QueryOrderByParserResult? orderByParserResult = null;
        if (HasOrderByParser)
        {
            orderByParserResult = OrderByParser.Parse(queryArgs.OrderBy);
            if (orderByParserResult.HasError)
                return result.Adjust(x => x.Error = orderByParserResult.Error);
        }
        else if (!string.IsNullOrEmpty(queryArgs.OrderBy))
            return result.Adjust(x => x.Error = new QueryOrderByParserException("OrderBy statement is not currently supported."));
            
        return new QueryArgsParseResult(this, filterParserResult, orderByParserResult);
    }

    /// <summary>
    /// Executes the underlying <see cref="OnWriteNullFilterExpression(QueryFilterParserWriter, IQueryFilterFieldConfig, QueryFilterTokenKind)"/> to write the <see langword="null"/> dynamic LINQ equality expression.
    /// </summary>
    /// <param name="writer">The <see cref="QueryFilterParserWriter"/></param>
    /// <param name="fieldConfig">The <see cref="IQueryFilterFieldConfig"/>.</param>
    /// <param name="filterOperator">The <see cref="QueryFilterTokenKind"/> (either <see cref="QueryFilterTokenKind.Equal"/> or <see cref="QueryFilterTokenKind.NotEqual"/>).</param>
    internal void WriteNullFilterExpression(QueryFilterParserWriter writer, IQueryFilterFieldConfig fieldConfig, QueryFilterTokenKind filterOperator) => OnWriteNullFilterExpression(writer, fieldConfig, filterOperator);

    /// <summary>
    /// Provides an opportunity to override the <see langword="null"/> dynamic LINQ equality expression write. For some data sources, such as NoSQL, the existence of the field (has a value) and whether it is <see langword="null"/> 
    /// are two different operations and may be data source specific. This method allows for this logic to be overridden to write the filter expression result that is data source specific.
    /// </summary>
    /// <param name="writer">The <see cref="QueryFilterParserWriter"/></param>
    /// <param name="fieldConfig">The <see cref="IQueryFilterFieldConfig"/>.</param>
    /// <param name="filterOperator">The <see cref="QueryFilterOperator"/> (either <see cref="QueryFilterTokenKind.Equal"/> or <see cref="QueryFilterTokenKind.NotEqual"/>).</param>
    protected virtual void OnWriteNullFilterExpression(QueryFilterParserWriter writer, IQueryFilterFieldConfig fieldConfig, QueryFilterTokenKind filterOperator)
    {
        writer.Append(fieldConfig.FullyQualifiedModelName);
        writer.Append($" {(filterOperator == QueryFilterTokenKind.Equal ? "==" : "!=")} null");
    }

    /// <summary>
    /// Produces the JSON schema for the configuration.
    /// </summary>
    /// <returns>The <see cref="JsonElement"/>.</returns>
    public JsonElement ToJsonSchema()
    {
        var dict = new Dictionary<string, JsonElement>();
        if (HasFilterParser)
            dict["filter"] = FilterParser.ToJsonSchema();

        if (HasOrderByParser)
            dict["orderby"] = OrderByParser.ToJsonSchema();

        return JsonSerializer.SerializeToElement(dict);
    }
}