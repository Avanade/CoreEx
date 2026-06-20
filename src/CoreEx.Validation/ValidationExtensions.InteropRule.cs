namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains an interop (<see cref="InteropRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="getValidator">The function to get the <see cref="IValidator"/>.</param>
    /// <param name="validateWhenNull">Indicates whether the validation will be performed where the property value is <see langword="null"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Interop<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<IValidator> getValidator, bool validateWhenNull = false) where TEntity : class
        => Chain(rule, new InteropRule<TEntity, TProperty>(getValidator, validateWhenNull));

    /// <summary>
    /// Chains an interop (<see cref="InteropRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="validator">The <see cref="IValidator"/>.</param>
    /// <param name="validateWhenNull">Indicates whether the validation will be performed where the property value is <see langword="null"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Interop<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, IValidator validator, bool validateWhenNull = false) where TEntity : class
        => Interop(rule, () => validator, validateWhenNull);
}