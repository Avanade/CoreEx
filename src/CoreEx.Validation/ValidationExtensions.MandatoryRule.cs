namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a mandatory (<see cref="MandatoryRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="mustNotBeDefault">Indicates that a validation error should occur when the value is <see langword="default"/>.</param>
    /// <param name="mustNotBeEmpty">Indicates that a validation error should occur when the value is considered empty.</param>
    public static IPropertyRule<TEntity, TProperty> Mandatory<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool mustNotBeDefault = true, bool mustNotBeEmpty = true) where TEntity : class
        => Chain(rule, new MandatoryRule<TEntity, TProperty>(mustNotBeDefault: _ => mustNotBeDefault, mustNotBeEmpty: _ => mustNotBeEmpty));

    /// <summary>
    /// Chains a mandatory (<see cref="MandatoryRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="mustNotBeDefault">Indicates that a validation error should occur when the value is <see langword="default"/>.</param>
    /// <param name="mustNotBeEmpty">Indicates that a validation error should occur when the value is considered empty.</param>
    public static IPropertyRule<TEntity, TProperty> Mandatory<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, bool>? mustNotBeDefault, Func<PropertyContext<TEntity, TProperty>, bool>? mustNotBeEmpty) where TEntity : class
        => Chain(rule, new MandatoryRule<TEntity, TProperty>(mustNotBeDefault ?? (_ => true), mustNotBeEmpty ?? (_ => true)));

    /// <summary>
    /// Chains a null and not empty (<see cref="MandatoryRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    public static IPropertyRule<TEntity, TProperty> NotEmpty<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class
        => Chain(rule, new MandatoryRule<TEntity, TProperty>(mustNotBeDefault: _ => true, mustNotBeEmpty: _ => true));

    /// <summary>
    /// Chains a not null only (<see cref="MandatoryRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    public static IPropertyRule<TEntity, TProperty> NotNull<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class
        => Chain(rule, new MandatoryRule<TEntity, TProperty>(mustNotBeDefault: _ => false, mustNotBeEmpty: _ => false));
}