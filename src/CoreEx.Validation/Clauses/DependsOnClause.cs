namespace CoreEx.Validation.Clauses;

/// <summary>
/// Represents a depends on clause; in that specified property (<paramref name="dependsOnExpression"/>) of the entity must have a non-default value, and not have a validation error, to continue.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <typeparam name="TDependsOnProperty">The depends on property <see cref="Type"/>.</typeparam>
/// <param name="dependsOnExpression">The <see cref="Expression"/> to reference the depends on entity property.</param>
public sealed class DependsOnClause<TEntity, TProperty, TDependsOnProperty>(Expression<Func<TEntity, TDependsOnProperty>> dependsOnExpression) : IPropertyClause<TEntity, TProperty> where TEntity : class
{
    private readonly IPropertyRuntimeMetadata _dependsOn = RuntimeMetadata.GetForExpression(dependsOnExpression.ThrowIfNull());

    /// <inheritdoc/>
    public Task<bool> CheckAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        // Make sure not the same property.
        if (_dependsOn.Name == context.Name)
            throw new InvalidOperationException($"The depends on property '{_dependsOn.Name}' cannot be the same as the property being validated.");

        // Do not continue where the depends on property is in error.
        if (context.HasError(context.CreateFullyQualifiedPropertyName(_dependsOn.Name)))
            return Task.FromResult(false);

        // Check depends on value to continue.
        return Task.FromResult(!_dependsOn.IsDefault(context.Entity));
    }
}