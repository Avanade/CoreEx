// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data.Querying.Expressions
{
    /// <summary>
    /// Provides a query filter expression.
    /// </summary>
    public abstract class QueryFilterExpressionBase
    {
        /// <summary>
        /// Initlializes a new instance of the <see cref="QueryFilterExpressionBase"/>.
        /// </summary>
        /// <param name="parser">The <see cref="QueryFilterParser"/>.</param>
        /// <param name="filter">The originating query filter.</param>
        /// <param name="first">The first <see cref="QueryFilterToken"/> to be added.</param>
        public QueryFilterExpressionBase(QueryFilterParser parser, string filter, QueryFilterToken first)
        {
            Parser = parser.ThrowIfNull(nameof(parser));
            Filter = filter.ThrowIfNull(nameof(filter));
            AddToken(first);
        }

        /// <summary>
        /// Gets the owning <see cref="QueryFilterParser"/>.
        /// </summary>
        public QueryFilterParser Parser { get; }

        /// <summary>
        /// Gets the originating query filter.
        /// </summary>
        public string Filter { get; }

        /// <summary>
        /// Gets the count of tokens added.
        /// </summary>
        public int TokenCount { get; private set; }

        /// <summary>
        /// Indicates whether the expression is considered in a complete and valid state.
        /// </summary>
        public virtual bool IsComplete => true;

        /// <summary>
        /// Indicates whether the <paramref name="token"/> can be added to the expression.
        /// </summary>
        /// <param name="token">The <see cref="QueryFilterToken"/>.</param>
        /// <returns><see langword="true"/> indicates that the <paramref name="token"/> can and should be added; otherwise, <see langword="false"/> signifies that the <paramref name="token"/> is for the next expression.</returns>
        /// <remarks>Used to determine whether the next <paramref name="token"/> can be added; allows an expression to support multiple complete states.</remarks>
        public virtual bool CanAddToken(QueryFilterToken token) => !IsComplete;

        /// <summary>
        /// Adds the <paramref name="token"/> to the expression.
        /// </summary>
        /// <param name="token">The <see cref="QueryFilterToken"/>.</param>
        public void AddToken(QueryFilterToken token)
        {
            AddToken(TokenCount, token);
            TokenCount++;
        }

        /// <summary>
        /// Adds the <paramref name="token"/> to the expression.
        /// </summary>
        /// <param name="index">The <paramref name="token"/> index.</param>
        /// <param name="token">The <see cref="QueryFilterToken"/>.</param>
        protected abstract void AddToken(int index, QueryFilterToken token);

        /// <summary>
        /// Converts the query filter expression into the corresponding dynamic LINQ appending to the <paramref name="result"/>.
        /// </summary>
        /// <param name="result">The <see cref="QueryFilterParserResult"/>.</param>
        public abstract void WriteToResult(QueryFilterParserResult result);
    }
}