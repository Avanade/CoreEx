namespace CoreEx.Data.Querying;

/// <summary>
/// Provides the base <see cref="QueryFilterParser"/> field configuration.
/// </summary>
public abstract class QueryFilterFieldConfigBase : IQueryFilterFieldConfig
{
    private readonly QueryFilterParser _parser;
    private readonly Type _type;
    private readonly string _field;
    private readonly string? _model;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFilterFieldConfigBase{TSelf}"/> class.
    /// </summary>
    /// <param name="parser">The owning <see cref="QueryFilterParser"/>.</param>
    /// <param name="type">The field type.</param>
    /// <param name="field">The field name.</param>
    /// <param name="model">The model name (defaults to <paramref name="field"/>).</param>
    /// <remarks>The <see cref="Operators"/> defaults:
    /// <list type="bullet">
    /// <item>Where <see cref="FieldType"/> is <see cref="QueryFilterFieldType.Boolean"/> then defaults to <see cref="QueryFilterOperator.BooleanEqualityOperators"/>.</item>
    /// <item>Where <see cref="FieldType"/> is <see cref="QueryFilterFieldType.Enum"/> then defaults to <see cref="QueryFilterOperator.EqualityOperators"/>.</item>
    /// <item>Where <see cref="FieldType"/> is <see cref="QueryFilterFieldType.String"/> then defaults to <see cref="QueryFilterOperator.ComparisonOperators"/>.</item>
    /// <item>Otherwise, <see cref="FieldType"/> is <see cref="QueryFilterFieldType.Other"/> then defaults to <see cref="QueryFilterOperator.ComparisonOperators"/>.</item>
    /// </list></remarks>
    public QueryFilterFieldConfigBase(QueryFilterParser parser, Type type, string field, string? model)
    {
        _parser = parser.ThrowIfNull();
        _type = type.ThrowIfNull();
        _field = field.ThrowIfNullOrEmpty();
        _model = model.ThrowIfEmpty();
        ModelPrefix = parser.DefaultModelPrefix;

        if (_type.IsEnum)
        {
            FieldType = QueryFilterFieldType.Enum;
            Operators = QueryFilterOperator.Equal | QueryFilterOperator.NotEqual | QueryFilterOperator.In;
        }
        else if (_type == typeof(string))
        {
            FieldType = QueryFilterFieldType.String;
            Operators = QueryFilterOperator.ComparisonOperators;
        }
        else if (_type == typeof(bool))
        {
            FieldType = QueryFilterFieldType.Boolean;
            Operators = QueryFilterOperator.Equal | QueryFilterOperator.NotEqual;
            SchemaType = QueryFilterSchemaType.Boolean;
        }
        else
        {
            FieldType = QueryFilterFieldType.Other;
            Operators = QueryFilterOperator.ComparisonOperators;

            if (_type == typeof(DateTime) || _type == typeof(DateTimeOffset))
            {
                SchemaType = QueryFilterSchemaType.String;
                SchemaFormat = "date-time";
            }
            else if (_type == typeof(DateOnly))
            {
                SchemaType = QueryFilterSchemaType.String;
                SchemaFormat = "date";
            }
            else if (IsIntegerType(_type))
            {
                SchemaType = QueryFilterSchemaType.Integer;
                SchemaFormat = _type.Name.ToLowerInvariant();
            }
            else if (IsNumberType(_type))
            {
                SchemaType = QueryFilterSchemaType.Number;
                SchemaFormat = _type.Name.ToLowerInvariant();
            }
        }
    }

    /// <summary>
    /// Indicates whether the specified <paramref name="type"/> is an integer type.
    /// </summary>
    private static bool IsIntegerType(Type type) => type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte) ||
                                                    type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) ||
                                                    type == typeof(int?) || type == typeof(long?) || type == typeof(short?) || type == typeof(byte?) ||
                                                    type == typeof(uint?) || type == typeof(ulong?) || type == typeof(ushort?);

    /// <summary>
    /// Indicates whether the specified <paramref name="type"/> is a number type.
    /// </summary>
    private static bool IsNumberType(Type type) => type == typeof(double) || type == typeof(decimal) || type == typeof(float) ||
                                                   type == typeof(double?) || type == typeof(decimal?) || type == typeof(float?);

    /// <inheritdoc/>
    QueryFilterParser IQueryFilterFieldConfig.Parser => _parser;

    /// <inheritdoc/>
    Type IQueryFilterFieldConfig.Type => Type;

    /// <summary>
    /// Gets the field type.
    /// </summary>
    protected Type Type => _type;

    /// <inheritdoc/>
    QueryFilterFieldType IQueryFilterFieldConfig.FieldType => FieldType;

    /// <summary>
    /// Gets the <see cref="QueryFilterFieldType"/>.
    /// </summary>
    protected QueryFilterFieldType FieldType { get; set; }

    /// <inheritdoc/>
    string IQueryFilterFieldConfig.Field => Field;

    /// <summary>
    /// Gets the field name.
    /// </summary>
    protected string Field => _field;

    /// <inheritdoc/>
    string IQueryFilterFieldConfig.Model => Model;

    /// <summary>
    /// Gets the model name to be used for the dynamic LINQ expression.
    /// </summary>
    protected string Model => _model ?? _field;

    /// <inheritdoc/>
    public string? ModelPrefix { get; protected set; }

    /// <inheritdoc/>
    public string FullyQualifiedModelName => ModelPrefix is null ? Model : $"{ModelPrefix}.{Model}";

    /// <inheritdoc/>
    QueryFilterOperator IQueryFilterFieldConfig.Operators => Operators;

    /// <summary>
    /// Gets the supported <see cref="QueryFilterOperator"/>(s).
    /// </summary>
    protected QueryFilterOperator Operators { get; set; }

    /// <inheritdoc/>
    bool? IQueryFilterFieldConfig.IsToUpper => IsToUpper;

    /// <summary>
    /// Indicates whether the comparison should ignore case or not; will use <see cref="string.ToUpper()"/> (where <see langword="true"/>) or <see cref="string.ToLower()"/> (where <see langword="false"/>) when selected for comparisons.
    /// </summary>
    protected bool? IsToUpper { get; set; }

    /// <inheritdoc/>
    bool IQueryFilterFieldConfig.IsNullable => IsNullable;

    /// <summary>
    /// Indicates whether the field can be <see langword="null"/> or not.
    /// </summary>
    protected bool IsNullable { get; set; } = false;

    /// <inheritdoc/>
    bool IQueryFilterFieldConfig.IsCheckForNotNull => IsCheckForNotNull;

    /// <summary>
    /// Indicates whether a not-<see langword="null"/> check should also be performed before the comparion occurs (defaults to <see langword="false"/>).
    /// </summary>
    protected bool IsCheckForNotNull { get; set; } = false;

    /// <inheritdoc/>
    Func<QueryStatement>? IQueryFilterFieldConfig.DefaultStatement => DefaultStatement;

    /// <summary>
    /// Gets or sets the default LINQ <see cref="QueryStatement"/> function to be used where no filtering is specified.
    /// </summary>
    protected Func<QueryStatement>? DefaultStatement { get; set; }

    /// <inheritdoc/>
    QueryFilterFieldResultWriter? IQueryFilterFieldConfig.ResultWriter => ResultWriter;

    /// <summary>
    /// Gets or sets the <see cref="QueryFilterFieldResultWriter"/>.
    /// </summary>
    protected QueryFilterFieldResultWriter? ResultWriter { get; set; }

    /// <inheritdoc/>
    QueryFilterSchemaType IQueryFilterFieldConfig.SchemaType => SchemaType;

    /// <summary>
    /// Gets or sets the <see cref="QueryFilterSchemaType"/>.
    /// </summary>
    protected QueryFilterSchemaType SchemaType { get; set; } = QueryFilterSchemaType.String;

    /// <inheritdoc/>
    string? IQueryFilterFieldConfig.SchemaFormat => SchemaFormat;

    /// <summary>
    /// Gets or sets the corresponding format for the <see cref="SchemaType"/> (where applicable).
    /// </summary>
    protected string? SchemaFormat { get; set; }

    /// <inheritdoc/>
    string? IQueryFilterFieldConfig.HelpText => HelpText;

    /// <summary>
    /// Gets or sets the additional help text.
    /// </summary>
    protected string? HelpText { get; set; }

    /// <inheritdoc/>
    object? IQueryFilterFieldConfig.ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter) => ConvertToValue(operation, field, filter);

    /// <summary>
    /// Converts <paramref name="field"/> to the underlying type.
    /// </summary>
    /// <param name="operation">The operation <see cref="QueryFilterToken"/> being performed on the <paramref name="operation"/>.</param>
    /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
    /// <param name="filter">The query filter.</param>
    /// <returns>The converted value.</returns>
    /// <remarks>Note: A converted value of <see langword="null"/> is considered invalid and will result in an <see cref="InvalidOperationException"/>.</remarks>
    protected abstract object ConvertToValue(QueryFilterToken operation, QueryFilterToken field, string filter);

    /// <summary>
    /// Validate the <paramref name="constant"/> token against the field configuration.
    /// </summary>
    /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
    /// <param name="constant">The constant <see cref="QueryFilterToken"/>.</param>
    /// <param name="filter">The query filter.</param>
    void IQueryFilterFieldConfig.ValidateConstant(QueryFilterToken field, QueryFilterToken constant, string filter)
    {
        if (!QueryFilterTokenKind.Constant.HasFlag(constant.Kind))
            throw new QueryFilterParserException($"Field '{field.GetRawToken(filter)}' constant '{constant.GetValueToken(filter)}' is not considered valid.");

        if (constant.Kind == QueryFilterTokenKind.Null && !IsNullable)
            throw new QueryFilterParserException($"Field '{field.GetRawToken(filter)}' constant '{constant.GetValueToken(filter)}' is not supported.");

        if (FieldType == QueryFilterFieldType.String || FieldType == QueryFilterFieldType.Enum)
        {
            if (!(constant.Kind == QueryFilterTokenKind.Literal || constant.Kind == QueryFilterTokenKind.Null))
                throw new QueryFilterParserException($"Field '{field.GetRawToken(filter)}' constant '{constant.GetValueToken(filter)}' must be specified as a {QueryFilterTokenKind.Literal} where the underlying type is a string.");
        }
        else if (FieldType == QueryFilterFieldType.Boolean)
        {
            if (!(constant.Kind == QueryFilterTokenKind.True || constant.Kind == QueryFilterTokenKind.False || constant.Kind == QueryFilterTokenKind.Null))
                throw new QueryFilterParserException($"Field '{field.GetRawToken(filter)}' constant '{constant.GetValueToken(filter)}' is not considered a valid boolean.");
        }
        else
        {
            if (!(constant.Kind == QueryFilterTokenKind.Value || constant.Kind == QueryFilterTokenKind.Null))
                throw new QueryFilterParserException($"Field '{field.GetRawToken(filter)}' constant '{constant.GetValueToken(filter)}' must not be specified as a {QueryFilterTokenKind.Literal} where the underlying type is not a string.");
        }
    }

    /// <inheritdoc/>
    public override string ToString() => AppendToString(new StringBuilder()).ToString();

    /// <summary>
    /// Appends the field configuration to the <paramref name="stringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/>.</param>
    /// <returns>The <paramref name="stringBuilder"/>.</returns>
    public virtual StringBuilder AppendToString(StringBuilder stringBuilder)
    {
        stringBuilder.Append(_field);
        stringBuilder.Append(" (Type: ").Append(_type.Name);
        stringBuilder.Append(", Null: ").Append(IsNullable ? "true" : "false");
        stringBuilder.Append(", Operators: ");

        AppendOperatorsToString(stringBuilder);

        stringBuilder.Append(')');
        if (!string.IsNullOrEmpty(HelpText))
            stringBuilder.Append(" - ").Append(HelpText);

        return stringBuilder;
    }

    /// <summary>
    /// Appends the <see cref="Operators"/> to the <paramref name="stringBuilder"/>.
    /// </summary>
    /// <param name="stringBuilder">The <see cref="StringBuilder"/>.</param>
    /// <returns>The <paramref name="stringBuilder"/>.</returns>
    protected StringBuilder AppendOperatorsToString(StringBuilder stringBuilder) 
        => stringBuilder.Append(String.Join(", ", Enum.GetValues<QueryFilterOperator>().Where(x => Operators.HasFlag(x)).Select(x => GetODataOperator(x)).Where(x => x is not null)));

    /// <summary>
    /// Gets the ODATA operator for the specified <paramref name="operator"/>
    /// </summary>
    /// <param name="operator">The <see cref="QueryFilterOperator"/>.</param>
    protected static string? GetODataOperator(QueryFilterOperator @operator) => @operator switch
    {
        QueryFilterOperator.Equal => "EQ",
        QueryFilterOperator.NotEqual => "NE",
        QueryFilterOperator.GreaterThan => "GT",
        QueryFilterOperator.GreaterThanOrEqual => "GE",
        QueryFilterOperator.LessThan => "LT",
        QueryFilterOperator.LessThanOrEqual => "LE",
        QueryFilterOperator.In => "IN",
        QueryFilterOperator.StartsWith => nameof(QueryFilterOperator.StartsWith),
        QueryFilterOperator.EndsWith => nameof(QueryFilterOperator.EndsWith),
        QueryFilterOperator.Contains => nameof(QueryFilterOperator.Contains),
        _ => null
    };

    /// <inheritdoc/>
    public virtual IDictionary<string, object?> ToSchemaDictionary()
    {
        var dict = new Dictionary<string, object?>
        {
            { "type", SchemaType.ToString().ToLowerInvariant() },
        };

        if (SchemaFormat is not null)
            dict.Add("format", SchemaFormat);

        if (IsNullable)
            dict.Add("nullable", true);

        dict.Add("operators", Enum.GetValues<QueryFilterOperator>().Where(x => Operators.HasFlag(x)).Select(x => GetODataOperator(x)?.ToLowerInvariant()).Where(x => x is not null).ToArray());

        if (HelpText is not null)
            dict.Add("description", HelpText);

        return dict;
    } 
}