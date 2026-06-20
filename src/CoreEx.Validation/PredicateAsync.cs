namespace CoreEx.Validation;

/// <summary>
/// Represents the method signature for an <see cref="IPropertyRule{TEntity, TProperty}"/> context-based asynchronous predicate.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
/// <returns><see langword="true"/> where meets the criteria defined within the method represented by this delegate; otherwise, <see langword="false"/>.</returns>
public delegate Task<bool> PredicateAsync<TEntity, TProperty>(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken) where TEntity : class;