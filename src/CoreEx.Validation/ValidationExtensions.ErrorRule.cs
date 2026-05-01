namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a specified <paramref name="error"/> (<see cref="ErrorRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="error">The error text.</param>
    /// <remarks>Use a succeeding conditional clause to control whether the <paramref name="error"/> is emitted.</remarks>
    public static IPropertyRule<TEntity, TProperty> Error<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, LText error) where TEntity : class
        => Chain(rule, new ErrorRule<TEntity, TProperty>(error));

    /// <summary>
    /// Chains a duplicate (<see cref="ErrorRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <remarks>Use a succeeding conditional clause to control whether the error (<see cref="ValidatorStrings.DuplicateFormat"/>) is emitted.</remarks>
    public static IPropertyRule<TEntity, TProperty> Duplicate<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class => Error(rule, ValidatorStrings.DuplicateFormat);

    /// <summary>
    /// Chains a not-found (<see cref="ErrorRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <remarks>Use a succeeding conditional clause to control whether the error (<see cref="ValidatorStrings.NotFoundFormat"/>) is emitted.</remarks>
    public static IPropertyRule<TEntity, TProperty> NotFound<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class => Error(rule, ValidatorStrings.NotFoundFormat);

    /// <summary>
    /// Chains an invalid (<see cref="ErrorRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <remarks>Use a succeeding conditional clause to control whether the error (<see cref="ValidatorStrings.InvalidFormat"/>) is emitted.</remarks>
    public static IPropertyRule<TEntity, TProperty> Invalid<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class => Error(rule, ValidatorStrings.InvalidFormat);

    /// <summary>
    /// Chains an invalid (<see cref="ErrorRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <remarks>Use a succeeding conditional clause to control whether the error (<see cref="ValidatorStrings.ImmutableFormat"/>) is emitted.</remarks>
    public static IPropertyRule<TEntity, TProperty> Immutable<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class => Error(rule, ValidatorStrings.ImmutableFormat);
}