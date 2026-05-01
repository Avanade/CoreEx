namespace CoreEx.Data.Querying.Expressions;

/// <summary>
/// Represents a query filter <see cref="QueryFilterTokenKind.StringFunctions"/> expression.
/// </summary>
/// <param name="parser">The <see cref="QueryFilterParser"/>.</param>
/// <param name="filter">The originating query filter.</param>
/// <param name="function">The function <see cref="QueryFilterOperatorExpression"/></param>
public sealed class QueryFilterStringFunctionExpression(QueryFilterParser parser, string filter, QueryFilterToken function) : QueryFilterExpressionBase(parser, filter, function), IQueryFilterFieldStatementExpression
{
    private IQueryFilterFieldConfig? _fieldConfig;
    private bool _isComplete;

    /// <summary>
    /// Gets the function <see cref="QueryFilterToken"/>.
    /// </summary>
    public QueryFilterToken Function { get; private set; }

    /// <summary>
    /// Gets the <see cref="IQueryFilterFieldConfig"/>.
    /// </summary>
    public IQueryFilterFieldConfig FieldConfig => _fieldConfig ?? throw new InvalidOperationException($"{nameof(FieldConfig)} must be set before it can be accessed.");

    /// <summary>
    /// Gets the field <see cref="QueryFilterToken"/>.
    /// </summary>
    public QueryFilterToken Field { get; private set; }

    /// <summary>
    /// Gets the constant <see cref="QueryFilterToken"/>.
    /// </summary>
    public QueryFilterToken Constant { get; private set; }

    /// <inheritdoc/>
    public override bool IsComplete => _isComplete;

    /// <inheritdoc/>
    public override bool CanAddToken(QueryFilterToken token) => !_isComplete;

    /// <inheritdoc/>
    protected override void AddToken(int index, QueryFilterToken token)
    {
        switch (index)
        {
            case 0:
                Function = token;
                break;

            case 1:
                if (token.Kind != QueryFilterTokenKind.OpenParenthesis)
                    throw new QueryFilterParserException($"A '{Function.GetRawToken(Filter)}' function expects an opening '(' not a '{token.GetValueToken(Filter)}'.");

                break;

            case 2:
                Field = token;
                _fieldConfig = Parser.GetFieldConfig(Field, Filter);

                var op = (QueryFilterOperator)(int)Function.Kind;
                if (!FieldConfig.Operators.HasFlag(op))
                    throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter)}' does not support the '{Function.GetRawToken(Filter)}' function.");

                break;

            case 3:
                if (token.Kind != QueryFilterTokenKind.Comma)
                    throw new QueryFilterParserException($"A '{Function.GetRawToken(Filter)}' function expects a ',' separator between the field and its constant.");

                break;

            case 4:
                if (token.Kind == QueryFilterTokenKind.Null)
                    throw new QueryFilterParserException($"A '{Function.GetRawToken(Filter)}' function references a null constant which is not supported.");

                FieldConfig.ValidateConstant(Field, token, Filter);
                Constant = token;
                break;

            case 5:
                if (token.Kind != QueryFilterTokenKind.CloseParenthesis)
                    throw new QueryFilterParserException($"A '{Function.GetRawToken(Filter)}' function expects a closing ')' not a '{token.GetValueToken(Filter)}'.");

                _isComplete = true;
                break;
        }
    }

    /// <summary>
    /// Gets the converted <see cref="Constant"/> value.
    /// </summary>
    /// <returns>The converted value.</returns>
    public object GetConstantValue() => Constant!.GetConvertedValue(Function, Field, FieldConfig, Filter);

    /// <inheritdoc/>
    public override void WriteAsDynamicLinq(QueryFilterParserWriter writer)
    {
        if (FieldConfig.IsCheckForNotNull)
        {
            writer.AppendWithSpacing('(');
            writer.Append(FieldConfig.FullyQualifiedModelName);
            writer.Append(" != null &&");
        }

        writer.AppendWithSpacing(FieldConfig!.FullyQualifiedModelName);
        if (FieldConfig.FieldType == QueryFilterFieldType.String && FieldConfig.IsToUpper.HasValue)
        {
            if (FieldConfig.IsToUpper.Value)
                writer.Append(".ToUpper()");
            else
                writer.Append(".ToLower()");
        }

        writer.Append('.');
        writer.Append(Function.ToLinq(Filter));
        writer.Append('(');
        writer.AppendValue(Constant.GetConvertedValue(Function, Field, FieldConfig, Filter));
        writer.Append(')');

        if (FieldConfig.IsCheckForNotNull)
            writer.Append(')');
    }
}