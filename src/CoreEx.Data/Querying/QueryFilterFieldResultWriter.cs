// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Data.Querying.Expressions;

namespace CoreEx.Data.Querying
{
    /// <summary>
    /// The <see cref="QueryFilterParserResult"/> writing function that will be used to write the <paramref name="expression"/> to the <see cref="QueryFilterParserResult.FilterBuilder"/>.
    /// </summary>
    /// <param name="expression">The <see cref="IQueryFilterFieldStatementExpression"/> expression.</param>
    /// <param name="result">The <see cref="QueryFilterParserResult"/>.</param>
    /// <returns><see langword="true"/> indicates that a LINQ statement has been written to the <see cref="QueryFilterParserResult.FilterBuilder"/> and that the standard writer should not be invoked;
    /// otherwise, <see langword="false"/> indicates that the standard writer is to be invoked.</returns>
    public delegate bool QueryFilterFieldResultWriter(IQueryFilterFieldStatementExpression expression, QueryFilterParserResult result);
}