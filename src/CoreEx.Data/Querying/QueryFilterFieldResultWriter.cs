namespace CoreEx.Data.Querying;

/// <summary>
/// The <see cref="QueryFilterParserWriter"/> function that will be used to write the <paramref name="expression"/> as dynamic LINQ.
/// </summary>
/// <param name="expression">The <see cref="IQueryFilterFieldStatementExpression"/> expression.</param>
/// <param name="writer">The <see cref="QueryFilterParserResult"/>.</param>
/// <returns><see langword="true"/> indicates that a LINQ statement has been written and that the standard writer should not be invoked; otherwise, <see langword="false"/> indicates that the standard writer is to be invoked.</returns>
public delegate bool QueryFilterFieldResultWriter(IQueryFilterFieldStatementExpression expression, QueryFilterParserWriter writer);