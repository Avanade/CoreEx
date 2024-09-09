// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Data.Querying.Expressions
{
    /// <summary>
    /// Represents a query filter <see cref="QueryFilterTokenKind.Operator"/> expression.
    /// </summary>
    /// <param name="parser">The <see cref="QueryFilterParser"/>.</param>
    /// <param name="filter">The originating query filter.</param>
    /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
    public sealed class QueryFilterOperatorExpression(QueryFilterParser parser, string filter, QueryFilterToken field) : QueryFilterExpressionBase(parser, filter, field)
    {
        private bool _isComplete;

        /// <summary>
        /// Gets the field <see cref="IQueryFilterFieldConfig"/>.
        /// </summary>
        public IQueryFilterFieldConfig? FieldConfig { get; private set; }

        /// <summary>
        /// Gets the field <see cref="QueryFilterToken"/>.
        /// </summary>
        public QueryFilterToken Field { get; private set; }

        /// <summary>
        /// Gets the operator <see cref="QueryFilterToken"/>.
        /// </summary>
        public QueryFilterToken Operator { get; private set; }

        /// <summary>
        /// Gets the constant <see cref="QueryFilterToken"/> list.
        /// </summary>
        public List<QueryFilterToken> Constants { get; } = [];

        /// <inheritdoc/>
        public override bool IsComplete => _isComplete;

        /// <inheritdoc/>
        public override bool CanAddToken(QueryFilterToken token) => !_isComplete || TokenCount == 1 && QueryFilterTokenKind.Operator.HasFlag(token.Kind);

        /// <inheritdoc/>
        protected override void AddToken(int index, QueryFilterToken token)
        {
            switch (index)
            {
                case 0:
                    Field = token;
                    FieldConfig = Parser.GetFieldConfig(Field, Filter);
                    _isComplete = FieldConfig.IsTypeBoolean;
                    break;

                case 1:
                    if (!QueryFilterTokenKind.AllStringOperators.HasFlag(token.Kind))
                        throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' does not support '{token.GetRawToken(Filter).ToString()}' as an operator.");

                    if (!FieldConfig!.SupportedKinds.HasFlag(token.Kind))
                        throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' does not support the '{token.GetRawToken(Filter).ToString()}' operator.");

                    _isComplete = false;
                    Operator = token;
                    break;

                case 2:
                    if (Operator.Kind == QueryFilterTokenKind.In)
                    {
                        if (token.Kind != QueryFilterTokenKind.OpenParenthesis)
                            throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' must specify an opening '(' for the '{Operator.GetRawToken(Filter).ToString()}' operator.");

                        break;
                    }

                    if (token.Kind == QueryFilterTokenKind.Null && !QueryFilterTokenKind.EqualityOperator.HasFlag(Operator.Kind))
                        throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' constant must not be null for an '{Operator.GetRawToken(Filter).ToString()}' operator.");

                    FieldConfig!.ValidateConstant(Field, token, Filter);
                    Constants.Add(token);
                    _isComplete = true;
                    break;

                default:
                    if (index % 2 != 0)
                    {
                        if (token.Kind == QueryFilterTokenKind.CloseParenthesis)
                            throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' constant must be specified before the closing ')' for the '{Operator.GetRawToken(Filter).ToString()}' operator.");

                        if (token.Kind == QueryFilterTokenKind.OpenParenthesis)
                            throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' must close ')' the '{Operator.GetRawToken(Filter).ToString()}' operator before specifying a further open '('.");

                        if (token.Kind == QueryFilterTokenKind.Null)
                            throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' constant must not be null for an '{Operator.GetRawToken(Filter).ToString()}' operator.");

                        FieldConfig!.ValidateConstant(Field, token, Filter);
                        Constants.Add(token);
                    }
                    else
                    {
                        if (token.Kind == QueryFilterTokenKind.CloseParenthesis)
                        {
                            if (Constants.Count == 0)
                                throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' expects at least one constant value for an '{Operator.GetRawToken(Filter).ToString()}' operator.");

                            _isComplete = true;
                            break;
                        }

                        if (token.Kind != QueryFilterTokenKind.Comma)
                            throw new QueryFilterParserException($"Field '{Field.GetRawToken(Filter).ToString()}' expects a ',' separator between constant values for an '{Operator.GetRawToken(Filter).ToString()}' operator.");
                    }

                    break;
            }
        }

        /// <inheritdoc/>
        public override void WriteToResult(QueryFilterParserResult result)
        {
            result.Fields.Add(FieldConfig!.Field);

            if (Operator.Kind != QueryFilterTokenKind.In && (Constants.Count == 0 || Constants[0].Kind != QueryFilterTokenKind.Null) && FieldConfig!.IsCheckForNotNull)
            {
                result.Append("(");
                result.FilterBuilder.Append(FieldConfig.Model);
                result.FilterBuilder.Append(" != null && ");
            }

            result.Append(FieldConfig!.Model);

            if (Constants.Count > 0)
            {
                if (FieldConfig.IsTypeString && FieldConfig.IsToUpper)
                    result.FilterBuilder.Append(".ToUpper()");

                result.FilterBuilder.Append(' ');
                result.FilterBuilder.Append(Operator.ToLinq(Filter));
                result.FilterBuilder.Append(' ');

                if (Operator.Kind == QueryFilterTokenKind.In)
                {
                    result.FilterBuilder.Append('(');
                    for (int i = 0; i < Constants.Count; i++)
                    {
                        if (i > 0)
                            result.FilterBuilder.Append(", ");

                        result.AppendValue(Constants[i].GetConvertedValue(Operator, Field, FieldConfig, Filter));
                    }

                    result.FilterBuilder.Append(')');
                }
                else
                {
                    if (Constants[0].Kind == QueryFilterTokenKind.Value || Constants[0].Kind == QueryFilterTokenKind.Literal)
                        result.AppendValue(Constants[0].GetConvertedValue(Operator, Field, FieldConfig, Filter));
                    else
                        result.FilterBuilder.Append(Constants[0].ToLinq(Filter));
                }
            }

            if (Operator.Kind != QueryFilterTokenKind.In && (Constants.Count == 0 || Constants[0].Kind != QueryFilterTokenKind.Null) && FieldConfig!.IsCheckForNotNull)
                result.FilterBuilder.Append(')');
        }

        /// <inheritdoc/>
        protected override IQueryFilterFieldConfig? GetFieldConfig() => FieldConfig;
    }
}