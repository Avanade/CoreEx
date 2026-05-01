namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a common (<see cref="CommonRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="commonValidator">The <see cref="CommonValidator{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Common<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, InlineValidator<TProperty> commonValidator) where TEntity : class
        => Chain(rule, new CommonRule<TEntity, TProperty>(commonValidator.ThrowIfNull()));

    /// <summary>
    /// Chains a common (<see cref="CommonRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="commonValidator">The <see cref="CommonValidator{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Common<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, InlineValidator<TProperty> commonValidator) where TEntity : class where TProperty : struct
        => Chain(rule, new CommonRule<TEntity, TProperty>(commonValidator.ThrowIfNull()));
}