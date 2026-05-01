namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a <see langword="string"/> (<see cref="StringRule{TEntity}"/>) <paramref name="maxLength"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="maxLength">The maximum string length.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> String<TEntity>(this IPropertyRule<TEntity, string> rule, int maxLength) where TEntity : class
        => Chain(rule, new StringRule<TEntity>(maxLength: _ => maxLength));

    /// <summary>
    /// Chains a <see langword="string"/> (<see cref="StringRule{TEntity}"/>) <paramref name="minLength"/> and <paramref name="maxLength"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="minLength">The minimum string length.</param>
    /// <param name="maxLength">The maximum string length.</param>
    /// <param name="regex">The <see cref="Regex"/>.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> String<TEntity>(this IPropertyRule<TEntity, string> rule, int minLength, int? maxLength, Regex? regex = null) where TEntity : class
        => Chain(rule, new StringRule<TEntity>(minLength: _ => minLength, maxLength: _ => maxLength, regex: _ => regex));

    /// <summary>
    /// Chains a <see langword="string"/> (<see cref="StringRule{TEntity}"/>) <paramref name="minLength"/> and <paramref name="maxLength"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="minLength">The minimum string length.</param>
    /// <param name="maxLength">The maximum string length.</param>
    /// <param name="regex">The <see cref="Regex"/>.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> String<TEntity>(this IPropertyRule<TEntity, string> rule, Func<PropertyContext<TEntity, string>, int>? minLength, Func<PropertyContext<TEntity, string>, int?>? maxLength, Func<PropertyContext<TEntity, string>, Regex?>? regex = null) where TEntity : class
        => Chain(rule, new StringRule<TEntity>(minLength: minLength, maxLength: maxLength, regex: regex));

    /// <summary>
    /// Chains a <see langword="string"/> (<see cref="StringRule{TEntity}"/>) <paramref name="regex"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="regex">The <see cref="Regex"/>.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> String<TEntity>(this IPropertyRule<TEntity, string> rule, Regex regex) where TEntity : class
        => Chain(rule, new StringRule<TEntity>(regex: _ => regex));

    /// <summary>
    /// Chains a <see langword="string"/> (<see cref="StringRule{TEntity}"/>) <paramref name="regex"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="regex">The <see cref="Regex"/>.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> Matches<TEntity>(this IPropertyRule<TEntity, string> rule, Regex regex) where TEntity : class
        => Chain(rule, new StringRule<TEntity>(regex: _ => regex));

    /// <summary>
    /// Chains a <see langword="string"/> (<see cref="StringRule{TEntity}"/>) <paramref name="exactLength"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="exactLength">The exact string length.</param>
    /// <param name="regex">The optional <see cref="Regex"/>.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> Length<TEntity>(this IPropertyRule<TEntity, string> rule, int exactLength, Regex? regex = null) where TEntity : class
        => Chain(rule, new StringRule<TEntity>(minLength: _ => exactLength, maxLength: _ => exactLength, regex: _ => regex));

    /// <summary>
    /// Chains a <see langword="string"/> (<see cref="StringRule{TEntity}"/>) <paramref name="minLength"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="minLength">The minimum string length.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> MinimumLength<TEntity>(this IPropertyRule<TEntity, string> rule, int minLength) where TEntity : class
        => Chain(rule, new StringRule<TEntity>(minLength: _ => minLength));

    /// <summary>
    /// Chains a <see langword="string"/> (<see cref="StringRule{TEntity}"/>) <paramref name="maxLength"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="maxLength">The maximum string length.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> MaximumLength<TEntity>(this IPropertyRule<TEntity, string> rule, int maxLength) where TEntity : class
        => Chain(rule, new StringRule<TEntity>(maxLength: _ => maxLength));
}