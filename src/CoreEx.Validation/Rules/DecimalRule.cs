namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a decimal validation to check <paramref name="precision"/>, <paramref name="scale"/> and whether negatives are allowed (defaults to <see langword="true"/>, i.e. allowed).
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
/// <param name="scale">The maximum number of decimal places.</param>
/// <param name="allowNegatives">Indicates whether to allow negative values.</param>
/// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.
/// <para>Internally converts to the property value to a <see cref="decimal"/>. Other floating-point types (<see cref="float"/> and <see cref="double"/>) are generally not supported as precision might be lost during conversion.</para></remarks>
public sealed class DecimalRule<TEntity, TProperty>(Func<PropertyContext<TEntity, TProperty>,int?>? precision, Func<PropertyContext<TEntity, TProperty>, int?>? scale = null, Func<PropertyContext<TEntity, TProperty>, bool>? allowNegatives = null) : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IFloatingPoint<TProperty>
{
    private readonly Func<PropertyContext<TEntity, TProperty>, int?>? _precision = precision;
    private readonly Func<PropertyContext<TEntity, TProperty>, int?>? _scale = scale;
    private readonly Func<PropertyContext<TEntity, TProperty>, bool> _allowNegativesFunc = allowNegatives ?? (_ => true);

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        var precision = _precision?.Invoke(context);
        var scale = _scale?.Invoke(context);
        var allowNegatives = _allowNegativesFunc(context);

        if (precision.HasValue && precision.Value < 1)
            throw new InvalidOperationException("Precision minimum value (where specified) is 1.");

        if (scale.HasValue && scale.Value < 0)
            throw new InvalidOperationException("Scale minimum value (where specified) is 0.");

        // Validate the scale and/or precision where specified.
        if (precision is not null || scale is not null)
        {
            // Convert numeric to a decimal value.
            var value = decimal.CreateChecked(context.Value);
            var integralLength = precision.HasValue ? DecimalRuleHelper.CalcIntegralPartLength(value) : 0;
            var fractionalLength = precision.HasValue || scale.HasValue ? DecimalRuleHelper.CalcFractionalPartLength(value) : 0;

            // Check the precision.
            if (precision.HasValue && !DecimalRuleHelper.CheckPrecision(precision.Value, scale, integralLength, fractionalLength))
            {
                context.AddError(ErrorText ?? ValidatorStrings.MaxDigitsFormat, precision);
                return Task.CompletedTask;
            }

            // Check the scale.
            if (scale.HasValue && !DecimalRuleHelper.CheckScale(scale.Value, fractionalLength))
            {
                context.AddError(ErrorText ?? ValidatorStrings.DecimalPlacesFormat, scale);
                return Task.CompletedTask;
            }
        }

        // Finally, check for negatives.
        if (!allowNegatives && TProperty.IsNegative(context.Value))
            context.AddError(ErrorText ?? ValidatorStrings.AllowNegativesFormat);

        return Task.CompletedTask;
    }
}