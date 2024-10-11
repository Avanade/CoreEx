// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents the <see cref="QueryArgsConfig.Parse"/> result.
    /// </summary>
    public sealed class QueryArgsParseResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryArgsParseResult"/> class.
        /// </summary>
        /// <param name="filterResult">The <see cref="QueryFilterParserResult"/>.</param>
        /// <param name="orderByResult">The <see cref="QueryOrderByParserResult"/>.</param>
        internal QueryArgsParseResult(QueryFilterParserResult? filterResult = null, QueryOrderByParserResult? orderByResult = null)
        {
            FilterResult = filterResult;
            OrderByResult = orderByResult;
        }

        /// <summary>
        /// Gets the <see cref="QueryOrderByParserResult"/>.
        /// </summary>
        public QueryFilterParserResult? FilterResult { get; }

        /// <summary>
        /// Gets the <see cref="QueryOrderByParserResult"/>.
        /// </summary>
        public QueryOrderByParserResult? OrderByResult { get; }
    }
}