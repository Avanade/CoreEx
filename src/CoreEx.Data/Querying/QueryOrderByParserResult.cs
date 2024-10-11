// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// Represents the result of <see cref="QueryOrderByParser.Parse(string?)"/>.
    /// </summary>
    public sealed class QueryOrderByParserResult
    {
        private readonly string? _orderByStatement;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryOrderByParserResult"/> class.
        /// </summary>
        /// <param name="orderByStatement">The resulting dynamic LINQ order by statement.</param>
        internal QueryOrderByParserResult(string? orderByStatement) => _orderByStatement = orderByStatement;

        /// <summary>
        /// Provides the resulting dynamic LINQ order by.
        /// </summary>
        public string? ToLinqString() => _orderByStatement;
    }
}