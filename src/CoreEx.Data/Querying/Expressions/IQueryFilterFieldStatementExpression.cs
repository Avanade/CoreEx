namespace CoreEx.Data.Querying.Expressions;

/// <summary>
/// Identifies a query filter statement expression.
/// </summary>
public interface IQueryFilterFieldStatementExpression 
{
    /// <summary>
    /// Gets the field <see cref="IQueryFilterFieldConfig"/>.
    /// </summary>
    IQueryFilterFieldConfig FieldConfig { get; }

    /// <summary>
    /// Gets the field <see cref="QueryFilterToken"/>.
    /// </summary>
    QueryFilterToken Field { get; }
}