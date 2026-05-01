namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minText">The minimum text (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="exclusiveBetween">Indicates whether the between comparison is exclusive or inclusive (default).</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Between<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty min, TProperty max, LText? minText = null, LText? maxText = null, bool exclusiveBetween = false, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(_ => min, _ => max, _ => minText, _ => maxText, exclusiveBetween, comparer));

    /// <summary>
    /// Chains a between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The function to get the minimum value.</param>
    /// <param name="max">The function to get the maximum value.</param>
    /// <param name="minText">The minimum text formatter (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text formatter (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="exclusiveBetween">Indicates whether the between comparison is exclusive or inclusive (default).</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Between<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> min, Func<PropertyContext<TEntity, TProperty>, TProperty> max, Func<TProperty, LText?>? minText = null, Func<TProperty, LText?>? maxText = null, bool exclusiveBetween = false, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(c => min.ThrowIfNull()(c), c => max.ThrowIfNull()(c), minText, maxText, exclusiveBetween, comparer));

    /// <summary>
    /// Chains an inclusive between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minText">The minimum text (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> InclusiveBetween<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty min, TProperty max, LText? minText = null, LText? maxText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
    => Chain(rule, new BetweenRule<TEntity, TProperty>(_ => min, _ => max, _ => minText, _ => maxText, false, comparer));

    /// <summary>
    /// Chains an inclusive between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The function to get the minimum value.</param>
    /// <param name="max">The function to get the maximum value.</param>
    /// <param name="minText">The minimum text formatter (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text formatter (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> InclusiveBetween<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> min, Func<PropertyContext<TEntity, TProperty>, TProperty> max, Func<TProperty, LText?>? minText = null, Func<TProperty, LText?>? maxText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(c => min.ThrowIfNull()(c), c => max.ThrowIfNull()(c), minText, maxText, false, comparer));

    /// <summary>
    /// Chains an exclusive between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minText">The minimum text (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> ExclusiveBetween<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty min, TProperty max, LText? minText = null, LText? maxText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(_ => min, _ => max, _ => minText, _ => maxText, true, comparer));

    /// <summary>
    /// Chains an exclusive between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The function to get the minimum value.</param>
    /// <param name="max">The function to get the maximum value.</param>
    /// <param name="minText">The minimum text formatter (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text formatter (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> ExclusiveBetween<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> min, Func<PropertyContext<TEntity, TProperty>, TProperty> max, Func<TProperty, LText?>? minText = null, Func<TProperty, LText?>? maxText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(c => min.ThrowIfNull()(c), c => max.ThrowIfNull()(c), minText, maxText, true, comparer));

    /* Nullable/Struct */

    /// <summary>
    /// Chains a between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minText">The minimum text (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="exclusiveBetween">Indicates whether the between comparison is exclusive or inclusive (default).</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Between<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty min, TProperty max, LText? minText = null, LText? maxText = null, bool exclusiveBetween = false, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(_ => min, _ => max, _ => minText, _ => maxText, exclusiveBetween, comparer));

    /// <summary>
    /// Chains a between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The function to get the minimum value.</param>
    /// <param name="max">The function to get the maximum value.</param>
    /// <param name="minText">The minimum text formatter (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text formatter (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="exclusiveBetween">Indicates whether the between comparison is exclusive or inclusive (default).</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Between<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> min, Func<PropertyContext<TEntity, TProperty>, TProperty> max, Func<TProperty, LText?>? minText = null, Func<TProperty, LText?>? maxText = null, bool exclusiveBetween = false, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(c => min.ThrowIfNull()(c), c => max.ThrowIfNull()(c), minText, maxText, exclusiveBetween, comparer));

    /// <summary>
    /// Chains an inclusive between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minText">The minimum text (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> InclusiveBetween<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty min, TProperty max, LText? minText = null, LText? maxText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
    => Chain(rule, new BetweenRule<TEntity, TProperty>(_ => min, _ => max, _ => minText, _ => maxText, false, comparer));

    /// <summary>
    /// Chains an inclusive between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The function to get the minimum value.</param>
    /// <param name="max">The function to get the maximum value.</param>
    /// <param name="minText">The minimum text formatter (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text formatter (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> InclusiveBetween<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> min, Func<PropertyContext<TEntity, TProperty>, TProperty> max, Func<TProperty, LText?>? minText = null, Func<TProperty, LText?>? maxText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(c => min.ThrowIfNull()(c), c => max.ThrowIfNull()(c), minText, maxText, false, comparer));

    /// <summary>
    /// Chains an exclusive between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The minimum value.</param>
    /// <param name="max">The maximum value.</param>
    /// <param name="minText">The minimum text (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> ExclusiveBetween<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty min, TProperty max, LText? minText = null, LText? maxText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(_ => min, _ => max, _ => minText, _ => maxText, true, comparer));

    /// <summary>
    /// Chains an exclusive between comparison (<see cref="BetweenRule{TEntity, TProperty}"/>) validation of a <paramref name="min"/> and <paramref name="max"/> value.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="min">The function to get the minimum value.</param>
    /// <param name="max">The function to get the maximum value.</param>
    /// <param name="minText">The minimum text formatter (used in the error message); otherwise, uses the resulting <paramref name="min"/> value.</param>
    /// <param name="maxText">The maximum text formatter (used in the error message); otherwise, uses the resulting <paramref name="max"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> ExclusiveBetween<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> min, Func<PropertyContext<TEntity, TProperty>, TProperty> max, Func<TProperty, LText?>? minText = null, Func<TProperty, LText?>? maxText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new BetweenRule<TEntity, TProperty>(c => min.ThrowIfNull()(c), c => max.ThrowIfNull()(c), minText, maxText, true, comparer));
}