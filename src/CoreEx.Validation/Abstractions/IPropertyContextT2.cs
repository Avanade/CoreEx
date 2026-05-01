namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Enables a validation context for a property.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
public interface IPropertyContext<TEntity, TProperty> : IPropertyContext<TEntity> where TEntity : class 
{
    /// <inheritdoc/>
    object? IPropertyContext.Value => Value;

    /// <summary>
    /// Gets the property value.
    /// </summary>
    /// <returns>The property value.</returns>
    new TProperty Value { get; }

    /// <summary>
    /// Overrides (sets) the property value (where not read-only).
    /// </summary>
    /// <param name="value">The override value.</param>
    void Override(TProperty value);
}