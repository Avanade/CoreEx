namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a must be <see langword="null"/> (<see cref="NullNoneEmptyRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public static IPropertyRule<TEntity, TProperty> Null<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class where TProperty : class?
        => Chain(rule, new NullNoneEmptyRule<TEntity, TProperty>(mustBeNull: _ => true, mustBeDefault: _ => false, mustBeEmpty: _ => false));

    /// <summary>
    /// Chains a must be <see langword="default"/> (<see cref="NullNoneEmptyRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public static IPropertyRule<TEntity, TProperty> None<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class
        => Chain(rule, new NullNoneEmptyRule<TEntity, TProperty>(mustBeNull: _ => false, mustBeDefault: _ => true, mustBeEmpty: _ => false));

    /// <summary>
    /// Chains a must be empty (<see cref="NullNoneEmptyRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public static IPropertyRule<TEntity, TProperty> Empty<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class where TProperty : class?
        => Chain(rule, new NullNoneEmptyRule<TEntity, TProperty>(mustBeNull: _ => false, mustBeDefault: _ => false, mustBeEmpty: _ => true));

    /// <summary>
    /// Chains a must be null (<see cref="NullNoneEmptyRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public static IPropertyRule<TEntity, TProperty?> Null<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule) where TEntity : class where TProperty : struct
        => Chain(rule, new NullNoneEmptyRule<TEntity, TProperty?>(mustBeNull: _ => true, mustBeDefault: _ => false, mustBeEmpty: _ => false));

    /// <summary>
    /// Chains a must be <see langword="default"/> (<see cref="NullNoneEmptyRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public static IPropertyRule<TEntity, TProperty> None<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule) where TEntity : class where TProperty : struct
        => Chain(rule, new NullNoneEmptyRule<TEntity, TProperty>(mustBeNull: _ => false, mustBeDefault: _ => true, mustBeEmpty: _ => false));

    /// <summary>
    /// Chains a must be empty (<see cref="NullNoneEmptyRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    public static IPropertyRule<TEntity, TProperty> Empty<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule) where TEntity : class where TProperty : struct
        => Chain(rule, new NullNoneEmptyRule<TEntity, TProperty>(mustBeNull: _ => false, mustBeDefault: _ => false, mustBeEmpty: _ => true));
}