namespace CoreEx.Validation.Rules;

/// <summary>
/// Enables a property rule for an entity and property.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
public interface IPropertyRule<TEntity> where TEntity : class
{
    /// <summary>
    /// Gets the error message format text (overrides the default) used for all property validation errors.
    /// </summary>
    LText? ErrorText { get; }

    /// <summary>
    /// Adds an <see cref="IPropertyClause{TEntity}"/>.
    /// </summary>
    /// <param name="clause">The <see cref="IPropertyClause{TEntity}"/>.</param>
    void AddClause(IPropertyClause<TEntity> clause);

    /// <summary>
    /// Chains an <see cref="IPropertyRuleEx{TEntity, TProperty}"/> extending the current configuration.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity}"/>.</param>
    /// <returns>The chained <paramref name="rule"/>.</returns>
    /// <remarks>Chains an additional rule that is executed where the <see cref="ValidateAsync(IPropertyContext{TEntity}, CancellationToken)"/> is successful.</remarks>
    void Chain(IPropertyRule<TEntity> rule);

    /// <summary>
    /// Validates the value.
    /// </summary>
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task ValidateAsync(IPropertyContext<TEntity> context, CancellationToken cancellationToken);
}