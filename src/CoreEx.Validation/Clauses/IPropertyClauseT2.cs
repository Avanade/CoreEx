namespace CoreEx.Validation.Clauses;

/// <summary>
/// Enables a typed property clause for an entity.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
public interface IPropertyClause<TEntity, TProperty> : IPropertyClause<TEntity> where TEntity : class
{
    Task<bool> IPropertyClause<TEntity>.CheckAsync(IPropertyContext<TEntity> context, CancellationToken cancellationToken)
        => CheckAsync((PropertyContext<TEntity, TProperty>)context, cancellationToken);

    /// <summary>
    /// Checks the clause.
    /// </summary>
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns><see langword="true"/> where validation is to continue; otherwise, <see langword="false"/> to stop.</returns>
    Task<bool> CheckAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken);
}