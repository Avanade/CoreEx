namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a <paramref name="values"/> comparison (<see cref="CompareValuesRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="values">The compare-to value(s).</param>
    /// <param name="comparer">The optional <see cref="IEqualityComparer{T}"/>.</param>
    /// <param name="overrideValueWhereMatched">Indicates whether to override the underlying property value with the corresponding matched value.</param>
    public static IPropertyRule<TEntity, TProperty> CompareValues<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, IEnumerable<TProperty> values, IEqualityComparer<TProperty>? comparer = null, bool overrideValueWhereMatched = false) where TEntity : class where TProperty : IEquatable<TProperty>
        => Chain(rule, new CompareValuesRule<TEntity, TProperty>(_ => values, comparer, overrideValueWhereMatched));

    /// <summary>
    /// Chains a <paramref name="values"/> comparison (<see cref="CompareValuesRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="values">The function to get the compare-to value(s).</param>
    /// <param name="comparer">The optional <see cref="IEqualityComparer{T}"/>.</param>
    /// <param name="overrideValueWhereMatched">Indicates whether to override the underlying property value with the corresponding matched value.</param>
    public static IPropertyRule<TEntity, TProperty> CompareValues<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, IEnumerable<TProperty>> values, IEqualityComparer<TProperty>? comparer = null, bool overrideValueWhereMatched = false) where TEntity : class where TProperty : IEquatable<TProperty>
        => Chain(rule, new CompareValuesRule<TEntity, TProperty>(c => values.ThrowIfNull()(c), comparer, overrideValueWhereMatched));

    /* Nullable/Struct */

    /// <summary>
    /// Chains a <paramref name="values"/> comparison (<see cref="CompareValuesRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="values">The compare-to value(s).</param>
    /// <param name="comparer">The optional <see cref="IEqualityComparer{T}"/>.</param>
    /// <param name="overrideValueWhereMatched">Indicates whether to override the underlying property value with the corresponding matched value.</param>
    public static IPropertyRule<TEntity, TProperty> CompareValues<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, IEnumerable<TProperty> values, IEqualityComparer<TProperty>? comparer = null, bool overrideValueWhereMatched = false) where TEntity : class where TProperty : struct, IEquatable<TProperty>
        => Chain(rule, new CompareValuesRule<TEntity, TProperty>(_ => values, comparer, overrideValueWhereMatched));

    /// <summary>
    /// Chains a <paramref name="values"/> comparison (<see cref="CompareValuesRule{TEntity, TProperty}"/>) validation.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="values">The function to get the compare-to value(s).</param>
    /// <param name="comparer">The optional <see cref="IEqualityComparer{T}"/>.</param>
    /// <param name="overrideValueWhereMatched">Indicates whether to override the underlying property value with the corresponding matched value.</param>
    public static IPropertyRule<TEntity, TProperty> CompareValues<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, IEnumerable<TProperty>> values, IEqualityComparer<TProperty>? comparer = null, bool overrideValueWhereMatched = false) where TEntity : class where TProperty : struct, IEquatable<TProperty>
        => Chain(rule, new CompareValuesRule<TEntity, TProperty>(c => values.ThrowIfNull()(c), comparer, overrideValueWhereMatched));
}