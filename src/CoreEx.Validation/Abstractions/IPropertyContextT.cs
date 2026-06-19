namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Enables a validation context for a property.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
public interface IPropertyContext<TEntity> : IPropertyContext where TEntity : class 
{
    /// <summary>
    /// Gets the <see cref="RootPropertyRule"/>.
    /// </summary>
    IRootPropertyRule<TEntity> RootPropertyRule { get; }

    /// <inheritdoc/>
    IValidationContext IPropertyContext.Owner => Owner;

    /// <summary>
    /// Gets the owning entity <see cref="IValidationContext{TEntity}"/>.
    /// </summary>
    new IValidationContext<TEntity> Owner { get; }

    /// <summary>
    /// Gets the owning entity value.
    /// </summary>
    TEntity? Entity { get; }
}