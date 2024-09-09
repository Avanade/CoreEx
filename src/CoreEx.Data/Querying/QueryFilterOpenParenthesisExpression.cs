﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents a query filter <see cref="QueryFilterTokenKind.OpenParenthesis"/> expression.
    /// </summary>
    /// <param name="parser">The <see cref="QueryFilterParser"/>.</param>
    /// <param name="filter">The originating query filter.</param>
    /// <param name="syntax">The syntax <see cref="QueryFilterToken"/>.</param>
    public sealed class QueryFilterOpenParenthesisExpression(QueryFilterParser parser, string filter, QueryFilterToken syntax) : QueryFilterExpressionBase(parser, filter, syntax)
    {
        private QueryFilterToken _syntax;

        /// <inheritdoc/>
        protected override void AddToken(int index, QueryFilterToken token) => _syntax = token;

        /// <inheritdoc/>
        public override void WriteToResult(QueryFilterParserResult result) => result.Append(_syntax.ToLinq(Filter));

        /// <inheritdoc/>
        protected override IQueryFilterFieldConfig? GetFieldConfig() => null;
    }
}