namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a <paramref name="precision"/>, <paramref name="scale"/> and <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    /// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.</remarks>
    public static IPropertyRule<TEntity, decimal> Decimal<TEntity>(this IPropertyRule<TEntity, decimal> rule, int? precision, int? scale = null, bool allowNegatives = true) where TEntity : class
        => Chain(rule, new DecimalRule<TEntity, decimal>(_ => precision, _ => scale, _ => allowNegatives));

    /// <summary>
    /// Chains a <paramref name="precision"/>, <paramref name="scale"/> and <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    /// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.</remarks>
    public static IPropertyRule<TEntity, decimal> Decimal<TEntity>(this IPropertyRule<TEntity, decimal> rule, Func<PropertyContext<TEntity, decimal>, int?>? precision, Func<PropertyContext<TEntity, decimal>, int?>? scale = null, Func<PropertyContext<TEntity, decimal>, bool>? allowNegatives = null) where TEntity : class
        => Chain(rule, new DecimalRule<TEntity, decimal>(precision, scale, allowNegatives));

    /// <summary>
    /// Chains a <paramref name="precision"/>, <paramref name="scale"/> and <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    /// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.</remarks>
    public static IPropertyRule<TEntity, TProperty> PrecisionScale<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, int? precision, int? scale = null, bool allowNegatives = true) where TEntity : class where TProperty : IFloatingPoint<TProperty>
        => Chain(rule, new DecimalRule<TEntity, TProperty>(_ => precision, _ => scale, _ => allowNegatives));

    /// <summary>
    /// Chains a <paramref name="precision"/>, <paramref name="scale"/> and <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    /// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.</remarks>
    public static IPropertyRule<TEntity, TProperty> PrecisionScale<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, int?>? precision, Func<PropertyContext<TEntity, TProperty>, int?>? scale = null, Func<PropertyContext<TEntity, TProperty>, bool>? allowNegatives = null) where TEntity : class where TProperty : IFloatingPoint<TProperty>
        => Chain(rule, new DecimalRule<TEntity, TProperty>(precision, scale, allowNegatives));

    /* Nullable/Struct */

    /// <summary>
    /// Chains a <paramref name="precision"/>, <paramref name="scale"/> and <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    /// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.</remarks>
    public static IPropertyRule<TEntity, decimal> Decimal<TEntity>(this IPropertyRule<TEntity, decimal?> rule, int? precision, int? scale = null, bool allowNegatives = true) where TEntity : class
        => Chain(rule, new DecimalRule<TEntity, decimal>(_ => precision, _ => scale, _ => allowNegatives));

    /// <summary>
    /// Chains a <paramref name="precision"/>, <paramref name="scale"/> and <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    /// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.</remarks>
    public static IPropertyRule<TEntity, decimal> Decimal<TEntity>(this IPropertyRule<TEntity, decimal?> rule, Func<PropertyContext<TEntity, decimal>, int?>? precision, Func<PropertyContext<TEntity, decimal>, int?>? scale = null, Func<PropertyContext<TEntity, decimal>, bool>? allowNegatives = null) where TEntity : class
        => Chain(rule, new DecimalRule<TEntity, decimal>(precision, scale, allowNegatives));

    /// <summary>
    /// Chains a <paramref name="precision"/>, <paramref name="scale"/> and <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    /// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.</remarks>
    public static IPropertyRule<TEntity, TProperty> PrecisionScale<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, int? precision, int? scale = null, bool allowNegatives = true) where TEntity : class where TProperty : struct, IFloatingPoint<TProperty>
        => Chain(rule, new DecimalRule<TEntity, TProperty>(_ => precision, _ => scale, _ => allowNegatives));

    /// <summary>
    /// Chains a <paramref name="precision"/>, <paramref name="scale"/> and <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="precision">The maximum number of significant digits (including <paramref name="scale"/>).</param>
    /// <param name="scale">The maximum number of decimal places.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    /// <remarks>For example, to validate a number with the pattern '999.99', then <paramref name="precision"/> would be 5 and <paramref name="scale"/> would be 2.</remarks>
    public static IPropertyRule<TEntity, TProperty> PrecisionScale<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, int?>? precision, Func<PropertyContext<TEntity, TProperty>, int?>? scale = null, Func<PropertyContext<TEntity, TProperty>, bool>? allowNegatives = null) where TEntity : class where TProperty : struct, IFloatingPoint<TProperty>
        => Chain(rule, new DecimalRule<TEntity, TProperty>(precision, scale, allowNegatives));
}