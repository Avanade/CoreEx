namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a <paramref name="compareTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Compare<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, CompareOperator compareOperator, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(compareOperator, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <paramref name="compareTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Compare<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, CompareOperator compareOperator, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(compareOperator, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.Equal"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Equal<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.Equal, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.Equal"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Equal<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.Equal, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.NotEqual"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> NotEqual<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.NotEqual, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.NotEqual"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> NotEqual<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.NotEqual, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.LessThan"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> LessThan<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.LessThan, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.LessThan"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> LessThan<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.LessThan, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.LessThanOrEqualTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> LessThanOrEqualTo<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.LessThanOrEqualTo, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.LessThanOrEqualTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> LessThanOrEqualTo<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.LessThanOrEqualTo, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.GreaterThanOrEqualTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> GreaterThanOrEqualTo<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.GreaterThanOrEqualTo, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.GreaterThanOrEqualTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> GreaterThanOrEqualTo<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.GreaterThanOrEqualTo, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.GreaterThan"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> GreaterThan<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.GreaterThan, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.GreaterThan"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> GreaterThan<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.GreaterThan, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /* Nullable/Struct */

    /// <summary>
    /// Chains a <paramref name="compareTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Compare<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, CompareOperator compareOperator, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(compareOperator, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <paramref name="compareTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareOperator">The <see cref="CompareOperator"/>.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Compare<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, CompareOperator compareOperator, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(compareOperator, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.Equal"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Equal<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.Equal, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.Equal"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Equal<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.Equal, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.NotEqual"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> NotEqual<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.NotEqual, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.NotEqual"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> NotEqual<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.NotEqual, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.LessThan"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> LessThan<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.LessThan, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.LessThan"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> LessThan<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.LessThan, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.LessThanOrEqualTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> LessThanOrEqualTo<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.LessThanOrEqualTo, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.LessThanOrEqualTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> LessThanOrEqualTo<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.LessThanOrEqualTo, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.GreaterThanOrEqualTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> GreaterThanOrEqualTo<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.GreaterThanOrEqualTo, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.GreaterThanOrEqualTo"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> GreaterThanOrEqualTo<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.GreaterThanOrEqualTo, c => compareTo.ThrowIfNull()(c), compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.GreaterThan"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> GreaterThan<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, TProperty compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.GreaterThan, _ => compareTo, compareToText, comparer));

    /// <summary>
    /// Chains a <see cref="CompareOperator.GreaterThan"/> comparison (<see cref="CompareValueRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="compareTo">The function to get the compare-to value.</param>
    /// <param name="compareToText">The value text formatter (used in the error message); otherwise, uses the resulting <paramref name="compareTo"/> value.</param>
    /// <param name="comparer">The optional <see cref="IComparer{T}"/>.</param>
    public static IPropertyRule<TEntity, TProperty> GreaterThan<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty> compareTo, Func<TProperty, LText?>? compareToText = null, IComparer<TProperty>? comparer = null) where TEntity : class where TProperty : struct, IComparable<TProperty>
        => Chain(rule, new CompareValueRule<TEntity, TProperty>(CompareOperator.GreaterThan, c => compareTo.ThrowIfNull()(c), compareToText, comparer));
}