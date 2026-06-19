namespace CoreEx.Validation.Clauses;

/// <summary>
/// Enables a property clause for an entity.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
public interface IPropertyClause<TEntity> where TEntity : class 
{
    /// <summary>
    /// Checks the clause.
    /// </summary>
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where validation is to continue; otherwise, <see langword="false"/> to stop.</returns>
    Task<bool> CheckAsync(IPropertyContext<TEntity> context, CancellationToken cancellationToken);
}