namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    public static IPropertyRule<TEntity, TProperty> Numeric<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool allowNegatives = true) where TEntity : class where TProperty : INumber<TProperty>
        => Chain(rule, new NumericRule<TEntity, TProperty>(_ => allowNegatives));

    /// <summary>
    /// Chains a <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    public static IPropertyRule<TEntity, TProperty> Numeric<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, bool> allowNegatives) where TEntity : class where TProperty : INumber<TProperty>
        => Chain(rule, new NumericRule<TEntity, TProperty>(allowNegatives));

    /// <summary>
    /// Chains a positive-only numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    public static IPropertyRule<TEntity, TProperty> Positive<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class where TProperty : INumber<TProperty>
        => Chain(rule, new NumericRule<TEntity, TProperty>(allowNegatives: _ => false));

    /* Nullable/Struct */

    /// <summary>
    /// Chains a <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    public static IPropertyRule<TEntity, TProperty> Numeric<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, bool allowNegatives = true) where TEntity : class where TProperty : struct, INumber<TProperty>
        => Chain(rule, new NumericRule<TEntity, TProperty>(_ => allowNegatives));

    /// <summary>
    /// Chains a <paramref name="allowNegatives"/> numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowNegatives">Indicates whether to allow negative values.</param>
    public static IPropertyRule<TEntity, TProperty> Numeric<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, bool> allowNegatives) where TEntity : class where TProperty : struct, INumber<TProperty>
        => Chain(rule, new NumericRule<TEntity, TProperty>(allowNegatives));

    /// <summary>
    /// Chains a positive-only numeric value (<see cref="NumericRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    public static IPropertyRule<TEntity, TProperty> Positive<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule) where TEntity : class where TProperty : struct, INumber<TProperty>
        => Chain(rule, new NumericRule<TEntity, TProperty>(allowNegatives: _ => false));
}