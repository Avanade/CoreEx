namespace CoreEx.Validation.Abstractions;

public abstract partial class ValidatorBase<TEntity, TSelf>
{
    /// <summary>
    /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the validator <see cref="Rules"/> for the specified <paramref name="propertyExpression"/>.
    /// </summary>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="propertyExpression">The property expression.</param>
    /// <returns>The <see cref="IPropertyRule{TEntity, TProperty}"/>.</returns>
    /// <remarks>This is a synonym for the <see cref="Property{TProperty}(Expression{Func{TEntity, TProperty?}})"/> to enable <see href="https://docs.fluentvalidation.net/en/latest/">FluentValidation</see>-like syntax.</remarks>
    protected IRootPropertyRule<TEntity, TProperty> RuleFor<TProperty>(Expression<Func<TEntity, TProperty?>> propertyExpression)
        => PropertyInternal<TProperty>(RuntimeMetadata.GetForExpression(propertyExpression.ThrowIfNull()), null, null);

    /// <summary>
    /// Adds a <see cref="IPropertyRule{TEntity, TProperty}"/> to the validator <see cref="Rules"/> for the specified <paramref name="propertyExpression"/>.
    /// </summary>
    /// <param name="propertyExpression">The property <see cref="Expression{TDelegate}"/>.</param>
    /// <remarks>This is a synonym for the <see cref="Property{TProperty}(Expression{Func{TEntity, TProperty?}})"/> to enable <see href="https://docs.fluentvalidation.net/en/latest/">FluentValidation</see>-like syntax.</remarks>
    protected IRootPropertyRule<TEntity, TProperty?> RuleFor<TProperty>(Expression<Func<TEntity, TProperty?>> propertyExpression) where TProperty : struct
    {
        var metadata = RuntimeMetadata.GetForExpression(propertyExpression.ThrowIfNull());
        var rule = new RootPropertyRule<TEntity, TProperty?>(metadata,
            e => metadata.GetValue<TProperty?>(e).GetValueOrDefault(),
            e => Comparer<TProperty>.Default.Compare(metadata.GetValue<TProperty?>(e).GetValueOrDefault(), default) == 0);

        Rules.Add(rule);
        return rule;
    }
}