namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides base comparison validation capability.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
/// <param name="compareToText">The compare-to value text formatter (used in error message); otherwise, uses the resulting compare to value.</param>
/// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
public abstract class CompareRuleBase<TEntity, TProperty>(CompareOperator compareOperator, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IComparable<TProperty>
{
    /// <summary>
    /// Gets the <see cref="CompareOperator"/>.
    /// </summary>
    protected CompareOperator Operator { get; } = compareOperator;

    /// <summary>
    /// Gets or sets the comparer.
    /// </summary>
    protected IComparer<TProperty> Comparer { get; } = comparer ?? Comparer<TProperty>.Default;

    /// <summary>
    /// Gets the comparison text formatter (used in error message); otherwise, uses the resulting compare to value.
    /// </summary>
    protected Func<TProperty, LText?>? CompareToText { get; } = compareToText;

    /// <summary>
    /// Compare two values using the <see cref="Comparer"/>.
    /// </summary>
    /// <param name="lValue">The left value.</param>
    /// <param name="rValue">The right value.</param>
    /// <returns><see langword="true"/> where valid; otherwise, <see langword="false"/>.</returns>
    protected bool Compare(TProperty lValue, TProperty rValue) => Operator switch
    {
        CompareOperator.Equal => Comparer.Compare(lValue, rValue) == 0,
        CompareOperator.NotEqual => Comparer.Compare(lValue, rValue) != 0,
        CompareOperator.LessThan => Comparer.Compare(lValue, rValue) < 0,
        CompareOperator.LessThanOrEqualTo => Comparer.Compare(lValue, rValue) <= 0,
        CompareOperator.GreaterThan => Comparer.Compare(lValue, rValue) > 0,
        CompareOperator.GreaterThanOrEqualTo => Comparer.Compare(lValue, rValue) >= 0,
        _ => throw new InvalidOperationException("An invalid Operator value was encountered.")
    };

    /// <summary>
    /// Creates the error message passing the <see cref="CompareToText"/> or <paramref name="compareToValue"/> as the third format parameter (i.e. String.Format("{2}")).
    /// </summary>
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="compareToValue">The compare-to value.</param>
    protected void CreateErrorMessage(PropertyContext<TEntity, TProperty> context, TProperty compareToValue)
    {
        context.ThrowIfNull();
        var compareToText = CompareToText?.Invoke(compareToValue) ?? context.FormatValue(compareToValue);

        switch (Operator)
        {
            case CompareOperator.Equal: context.AddError(ErrorText ?? ValidatorStrings.CompareEqualFormat, compareToText); break;
            case CompareOperator.NotEqual: context.AddError(ErrorText ?? ValidatorStrings.CompareNotEqualFormat, compareToText); break;
            case CompareOperator.LessThan: context.AddError(ErrorText ?? ValidatorStrings.CompareLessThanFormat, compareToText); break;
            case CompareOperator.LessThanOrEqualTo: context.AddError(ErrorText ?? ValidatorStrings.CompareLessThanEqualFormat, compareToText); break;
            case CompareOperator.GreaterThan: context.AddError(ErrorText ?? ValidatorStrings.CompareGreaterThanFormat, compareToText); break;
            case CompareOperator.GreaterThanOrEqualTo: context.AddError(ErrorText ?? ValidatorStrings.CompareGreaterThanEqualFormat, compareToText); break;
        }
    }
}