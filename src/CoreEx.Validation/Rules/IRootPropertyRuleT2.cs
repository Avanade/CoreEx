namespace CoreEx.Validation.Rules;

/// <summary>
/// Enables root property rule capabilities.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
public interface IRootPropertyRule<TEntity, TProperty> : IRootPropertyRule<TEntity>, IPropertyRule<TEntity, TProperty> where TEntity : class { }