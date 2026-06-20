namespace CoreEx.Validation.Clauses;

/// <summary>
/// Represents a <i>when</i> test clause; in that the condition must be <see langword="true"/> to continue.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="whenAsync">The when-based <see cref="PredicateAsync{TEntity, TProperty}"/>.</param>
public sealed class WhenClause<TEntity, TProperty>(PredicateAsync<TEntity, TProperty> whenAsync) : IPropertyClause<TEntity, TProperty> where TEntity : class
{
    private readonly PredicateAsync<TEntity, TProperty> _whenAsync = whenAsync.ThrowIfNull();

    /// <inheritdoc/>
    public Task<bool> CheckAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken) => _whenAsync(context, cancellationToken);
}