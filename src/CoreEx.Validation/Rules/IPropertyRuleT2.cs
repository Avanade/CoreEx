namespace CoreEx.Validation.Rules;

/// <summary>
/// Enables a property rule for an entity and property.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
public interface IPropertyRule<TEntity, out TProperty> : IPropertyRule<TEntity> where TEntity : class
{
    /// <summary>
    /// Overrides the error message for the rule.
    /// </summary>
    /// <param name="errorText">The error <see cref="LText"/>.</param>
    /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/> to support fluent-style method-chaining.</returns>
    IPropertyRule<TEntity, TProperty> WithMessage(LText errorText);
}