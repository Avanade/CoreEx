namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a property comparison (<see cref="ComparePropertyRule{TEntity, TProperty, TCompareProperty}"/>) validation against another property (<paramref name="compareToPropertyExpression"/>) within the same entity; also confirms other property has no errors prior to comparison.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TCompareProperty"></typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
    /// <param name="compareToPropertyExpression">>The <see cref="Expression"/> to reference the compare-to entity property.</param>
    /// <param name="compareToText">The compare-to value text formatter (used in the error message); otherwise, uses the resulting compare-to value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> CompareProperty<TEntity, TProperty, TCompareProperty>(this IPropertyRule<TEntity, TProperty> rule, CompareOperator compareOperator, Expression<Func<TEntity, TCompareProperty>> compareToPropertyExpression, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new ComparePropertyRule<TEntity, TProperty, TCompareProperty>(compareOperator, compareToPropertyExpression, compareToText, comparer));

    /// <summary>
    /// Chains a property comparison (<see cref="ComparePropertyRule{TEntity, TProperty, TCompareProperty}"/>) validation against another property (<paramref name="compareToPropertyExpression"/>) within the same entity; also confirms other property has no errors prior to comparison.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TCompareProperty"></typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
    /// <param name="compareToPropertyExpression">>The <see cref="Expression"/> to reference the compare-to entity property.</param>
    /// <param name="compareToText">The compare-to value text formatter (used in the error message); otherwise, uses the resulting compare-to value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> CompareProperty<TEntity, TProperty, TCompareProperty>(this IPropertyRule<TEntity, TProperty?> rule, CompareOperator compareOperator, Expression<Func<TEntity, TCompareProperty>> compareToPropertyExpression, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new ComparePropertyRule<TEntity, TProperty, TCompareProperty>(compareOperator, compareToPropertyExpression, compareToText, comparer));
}