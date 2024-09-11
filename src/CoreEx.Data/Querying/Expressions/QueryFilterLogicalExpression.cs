// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data.Querying.Expressions
{
    /// <summary>
    /// Represents a query filter <see cref="QueryFilterTokenKind.Logical"/> expression.
    /// </summary>
    /// <param name="parser">The <see cref="QueryFilterParser"/>.</param>
    /// <param name="filter">The originating query filter.</param>
    /// <param name="logical">The logical <see cref="QueryFilterOperatorExpression"/></param>
    public class QueryFilterLogicalExpression(QueryFilterParser parser, string filter, QueryFilterToken logical) : QueryFilterExpressionBase(parser, filter, logical)
    {
        private QueryFilterToken _logical = QueryFilterToken.Unspecified;
        private QueryFilterToken _not = QueryFilterToken.Unspecified;
        private bool _isComplete = true;

        /// <inheritdoc/>
        public override bool IsComplete => _isComplete;

        /// <inheritdoc/>
        public override bool CanAddToken(QueryFilterToken token)
        {
            if (TokenCount == 1)
                return token.Kind == QueryFilterTokenKind.Not;

            _isComplete = token.Kind == QueryFilterTokenKind.OpenParenthesis;
            return _isComplete
                ? false
                : throw new QueryFilterParserException($"A '{_not.GetRawToken(Filter).ToString()}' expects an opening '(' to start an expression versus a syntactically incorrect '{token.GetValueToken(Filter)}' token.");
        }

        /// <inheritdoc/>
        protected override void AddToken(int index, QueryFilterToken token)
        {
            if (index == 0 && token.Kind != QueryFilterTokenKind.Not)
                _logical = token;
            else
            {
                _not = token;
                _isComplete = false;
            }
        }

        /// <inheritdoc/>
        public override void WriteToResult(QueryFilterParserResult result)
        {
            if (_logical.Kind != QueryFilterTokenKind.Unspecified)
                result.Append(_logical.ToLinq(Filter));

            if (_not.Kind != QueryFilterTokenKind.Unspecified)
                result.Append(_not.ToLinq(Filter));
        }
    }
}