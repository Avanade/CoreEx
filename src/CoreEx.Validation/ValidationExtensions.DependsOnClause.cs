namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Adds a depends on expression (<see cref="DependsOnClause{TEntity, TProperty, TDependsOnProperty}"/>) clause to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TDependsOnProperty">The depends on property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="dependsOnPropertyExpression">The depends on property expression.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    /// <remarks>Represents a depends on clause; in that specified property (<paramref name="dependsOnPropertyExpression"/>) of the entity must have a non-default value, and not have a validation error, to continue.</remarks>
    public static IPropertyRule<TEntity, TProperty> DependsOn<TEntity, TProperty, TDependsOnProperty>(this IPropertyRule<TEntity, TProperty> rule, Expression<Func<TEntity, TDependsOnProperty>> dependsOnPropertyExpression) where TEntity : class
        => AddClause(rule, new DependsOnClause<TEntity, TProperty, TDependsOnProperty>(dependsOnPropertyExpression.ThrowIfNull()));
}