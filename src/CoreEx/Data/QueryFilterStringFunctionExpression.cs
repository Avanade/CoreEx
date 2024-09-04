// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data
{
    /// <summary>
    /// Represents a query filter <see cref="QueryFilterTokenKind.StringFunction"/> expression.
    /// </summary>
    /// <param name="parser">The <see cref="QueryFilterParser"/>.</param>
    /// <param name="filter">The originating query filter.</param>
    /// <param name="function">The function <see cref="QueryFilterOperatorExpression"/></param>
    public class QueryFilterStringFunctionExpression(QueryFilterParser parser, string filter, QueryFilterToken function) : QueryFilterExpressionBase(parser, filter, function)
    {
        private bool _isComplete;
        private QueryFilterToken _function;
        private IQueryFilterFieldConfig? _fieldConfig;
        private QueryFilterToken _field;
        private QueryFilterToken _constant;

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
                    _function = token;
                    break;

                case 1:
                    if (token.Kind != QueryFilterTokenKind.OpenParenthesis)
                        throw new QueryFilterParserException($"Filter is invalid: A '{_function.GetRawToken(Filter).ToString()}' function expects an opening '(' not a '{token.GetValueToken(Filter)}'.");

                    break;

                case 2:
                    _field = token;
                    _fieldConfig = Parser.GetFieldConfig(_field, Filter);

                    if (!_fieldConfig!.SupportedKinds.HasFlag(_function.Kind))
                        throw new QueryFilterParserException($"Filter is invalid: Field '{_field.GetRawToken(Filter).ToString()}' does not support the '{_function.GetRawToken(Filter).ToString()}' function.");

                    break;

                case 3:
                    if (token.Kind != QueryFilterTokenKind.Comma)
                        throw new QueryFilterParserException($"Filter is invalid: A '{_function.GetRawToken(Filter).ToString()}' function expects a ',' separator between the field and its constant.");

                    break;

                case 4:
                    if (token.Kind == QueryFilterTokenKind.Null)
                        throw new QueryFilterParserException($"Filter is invalid: A '{_function.GetRawToken(Filter).ToString()}' function references a null constant which is not supported.");

                    _fieldConfig!.ValidateConstant(_field, token, Filter);
                    _constant = token;
                    break;

                case 5:
                    if (token.Kind != QueryFilterTokenKind.CloseParenthesis)
                        throw new QueryFilterParserException($"Filter is invalid: A '{_function.GetRawToken(Filter).ToString()}' function expects a closing ')' not a '{token.GetValueToken(Filter)}'.");

                    _isComplete = true;
                    break;
            }
        }

        /// <inheritdoc/>
        public override void WriteToResult(QueryFilterParserResult result)
        {
            if (_fieldConfig!.IsCheckForNotNull)
            {
                result.Append('(');
                result.FilterBuilder.Append(_fieldConfig.LinqName);
                result.FilterBuilder.Append(" != null &&");
            }

            result.Append(_fieldConfig!.LinqName);
            if (_fieldConfig.IsTypeString && _fieldConfig.IsIgnoreCase)
                result.FilterBuilder.Append(".ToUpper()");

            result.FilterBuilder.Append('.');
            result.FilterBuilder.Append(_function.ToLinq(Filter));
            result.FilterBuilder.Append('(');
            result.AppendValue(_constant.GetConvertedValue(_field, _fieldConfig, Filter));
            result.FilterBuilder.Append(')');

            if (_fieldConfig!.IsCheckForNotNull)
                result.FilterBuilder.Append(')');
        }
    }
}