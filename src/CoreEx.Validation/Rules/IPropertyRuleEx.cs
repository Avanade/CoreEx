namespace CoreEx.Validation.Rules;

/// <summary>
/// Enables an extended property rule for an entity and property.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
public interface IPropertyRuleEx<TEntity, TProperty> : IPropertyRule<TEntity, TProperty> where TEntity : class
{
    /// <summary>
    /// Validates the value.
    /// </summary>
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task ValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken);
}