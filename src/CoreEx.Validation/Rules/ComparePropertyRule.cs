namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a comparison validation against another property within the same entity; also confirms other property has no errors prior to comparison.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <typeparam name="TCompareProperty">The comparison property <see cref="Type"/>.</typeparam>
/// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
/// <param name="compareToPropertyExpression">>The <see cref="Expression"/> to reference the compare-to entity property.</param>
/// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting compare-to value.</param>
/// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
public sealed class ComparePropertyRule<TEntity, TProperty, TCompareProperty>(CompareOperator compareOperator, Expression<Func<TEntity, TCompareProperty>> compareToPropertyExpression, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null)
    : CompareRuleBase<TEntity, TProperty>(compareOperator, compareToText, comparer) where TEntity : class where TProperty : IComparable<TProperty>
{
    private readonly IPropertyRuntimeMetadata _compareToProperty = RuntimeMetadata.GetForExpression(compareToPropertyExpression.ThrowIfNull());

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        // Make sure not the same property.
        if (_compareToProperty.Name == context.Name)
            throw new InvalidOperationException($"The compare-to property '{_compareToProperty.Name}' cannot be the same as the property being validated.");

        // Do not continue where the compare-to property is in error.
        if (context.HasError(context.CreateFullyQualifiedPropertyName(_compareToProperty.Name)))
            return Task.CompletedTask;

        // Get the compare to value; where is null then simply skip the comparison.
        var compareTo = _compareToProperty.GetValue<TCompareProperty>(context.Entity);
        if (compareTo is null)
            return Task.CompletedTask;

        // Where compare to is the same type then _fast-path_ the comparison.
        if (compareTo is TProperty casted)
        {
            if (!Compare(context.Value, casted))
                CreateErrorMessage(context, casted);

            return Task.CompletedTask;
        }

        // Convert (slow-path) the compare-to to the property type and perform the (apples-to-apples) compare.
        try
        {
            var changed = (TProperty)Convert.ChangeType(compareTo, typeof(TProperty));
            if (!Compare(context.Value, changed))
                CreateErrorMessage(context, changed);
        }
        catch (Exception ex) when (ex is InvalidCastException || ex is FormatException)
        {
            throw new InvalidCastException($"Property '{_compareToProperty.Name}' and '{context.Name}' are incompatible for a comparison: {ex.Message}", ex);
        }

        return Task.CompletedTask;
    }
}