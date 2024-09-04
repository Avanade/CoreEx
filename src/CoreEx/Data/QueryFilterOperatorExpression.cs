// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Data
{
    /// <summary>
    /// Represents a query filter <see cref="QueryFilterTokenKind.Operator"/> expression.
    /// </summary>
    /// <param name="parser">The <see cref="QueryFilterParser"/>.</param>
    /// <param name="filter">The originating query filter.</param>
    /// <param name="field">The field <see cref="QueryFilterToken"/>.</param>
    public class QueryFilterOperatorExpression(QueryFilterParser parser, string filter, QueryFilterToken field) : QueryFilterExpressionBase(parser, filter, field)
    {
        private IQueryFilterFieldConfig? _fieldConfig;
        private QueryFilterToken _field;
        private QueryFilterToken _operator;
        private readonly List<QueryFilterToken> _constants = [];
        private bool _isComplete;

        /// <inheritdoc/>
        public override bool IsComplete => _isComplete;

        /// <inheritdoc/>
        public override bool CanAddToken(QueryFilterToken token) => !_isComplete || (TokenCount == 1 && QueryFilterTokenKind.Operator.HasFlag(token.Kind));

        /// <inheritdoc/>
        protected override void AddToken(int index, QueryFilterToken token)
        {
            switch (index)
            {
                case 0:
                    _field = token;
                    _fieldConfig = Parser.GetFieldConfig(_field, Filter);
                    _isComplete = _fieldConfig.IsTypeBoolean;
                    break;

                case 1:
                    if (!QueryFilterTokenKind.AllStringOperators.HasFlag(token.Kind))
                        throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' does not support '{token.GetRawToken(Filter).ToString()}' as an operator.");

                    if (!_fieldConfig!.SupportedKinds.HasFlag(token.Kind))
                        throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' does not support the '{token.GetRawToken(Filter).ToString()}' operator.");

                    _isComplete = false;
                    _operator = token;
                    break;

                case 2:
                    if (_operator.Kind == QueryFilterTokenKind.In)
                    {
                        if (token.Kind != QueryFilterTokenKind.OpenParenthesis)
                            throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' must specify an opening '(' for the '{_operator.GetRawToken(Filter).ToString()}' operator.");

                        break;
                    }

                    if (token.Kind == QueryFilterTokenKind.Null && !QueryFilterTokenKind.EqualityOperator.HasFlag(_operator.Kind))
                        throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' constant must not be null for an '{_operator.GetRawToken(Filter).ToString()}' operator.");

                    _fieldConfig!.ValidateConstant(_field, token, Filter);
                    _constants.Add(token);
                    _isComplete = true;
                    break;

                default:
                    if (index % 2 != 0)
                    {
                        if (token.Kind == QueryFilterTokenKind.CloseParenthesis)
                            throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' constant must be specified before the closing ')' for the '{_operator.GetRawToken(Filter).ToString()}' operator.");

                        if (token.Kind == QueryFilterTokenKind.OpenParenthesis)
                            throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' must close ')' the '{_operator.GetRawToken(Filter).ToString()}' operator before specifying a further open '('.");

                        if (token.Kind == QueryFilterTokenKind.Null)
                            throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' constant must not be null for an '{_operator.GetRawToken(Filter).ToString()}' operator.");

                        _fieldConfig!.ValidateConstant(_field, token, Filter);
                        _constants.Add(token);
                    }
                    else
                    {
                        if (token.Kind == QueryFilterTokenKind.CloseParenthesis)
                        {
                            if (_constants.Count == 0)
                                throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' expects at least one constant value for an '{_operator.GetRawToken(Filter).ToString()}' operator.");

                            _isComplete = true;
                            break;
                        }

                        if (token.Kind != QueryFilterTokenKind.Comma)
                            throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' expects a ',' separator between constant values for an '{_operator.GetRawToken(Filter).ToString()}' operator.");
                    }

                    break;
            }
        }

        /// <inheritdoc/>
        public override void WriteToResult(QueryFilterParserResult result)
        {
            if (_operator.Kind != QueryFilterTokenKind.In && (_constants.Count == 0 || _constants[0].Kind != QueryFilterTokenKind.Null) && _fieldConfig!.IsCheckForNotNull)
            {
                result.Append("(");
                result.FilterBuilder.Append(_fieldConfig.LinqName);
                result.FilterBuilder.Append(" != null && ");
            }

            result.Append(_fieldConfig!.LinqName);

            if (_constants.Count > 0)
            {
                if (_fieldConfig.IsTypeString && _fieldConfig.IsIgnoreCase)
                    result.FilterBuilder.Append(".ToUpper()");

                result.FilterBuilder.Append(' ');
                result.FilterBuilder.Append(_operator.ToLinq(Filter));
                result.FilterBuilder.Append(' ');

                if (_operator.Kind == QueryFilterTokenKind.In)
                {
                    result.FilterBuilder.Append('(');
                    for (int i = 0; i < _constants.Count; i++)
                    {
                        if (i > 0)
                            result.FilterBuilder.Append(", ");

                        result.AppendValue(_constants[i].GetConvertedValue(_field, _fieldConfig, Filter));
                    }

                    result.FilterBuilder.Append(')');
                }
                else
                {
                    if (_constants[0].Kind == QueryFilterTokenKind.Value || _constants[0].Kind == QueryFilterTokenKind.Literal)
                        result.AppendValue(_constants[0].GetConvertedValue(_field, _fieldConfig, Filter));
                    else
                        result.FilterBuilder.Append(_constants[0].ToLinq(Filter));
                }
            }

            if (_operator.Kind != QueryFilterTokenKind.In && (_constants.Count == 0 || _constants[0].Kind != QueryFilterTokenKind.Null) && _fieldConfig!.IsCheckForNotNull)
                result.FilterBuilder.Append(')');
        }
    }
}