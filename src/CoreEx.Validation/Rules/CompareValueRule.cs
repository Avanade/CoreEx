using CoreEx.Validation.Abstractions;

namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a comparison validation using the specified <paramref name="compareOperator"/> and <paramref name="compareToValue"/>.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
/// <param name="compareToValue">The function to get the compare-to value.</param>
/// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareToValue"/> value.</param>
/// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
public sealed class CompareValueRule<TEntity, TProperty>(CompareOperator compareOperator, Func<PropertyContext<TEntity, TProperty>, TProperty> compareToValue, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) 
    : CompareRuleBase<TEntity, TProperty>(compareOperator, compareToText, comparer) where TEntity : class where TProperty : IComparable<TProperty>
{
    private readonly Func<PropertyContext<TEntity, TProperty>, TProperty> _compareToValue = compareToValue.ThrowIfNull();

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        // Get the compare to value; where is null then simply skip the comparison.
        var compareTo = _compareToValue(context);
        if (compareTo is null)
            return Task.CompletedTask;

        // Perform the comparison.
        if (!Compare(context.Value, compareTo))
            CreateErrorMessage(context, compareTo);

        return Task.CompletedTask;
    }
}