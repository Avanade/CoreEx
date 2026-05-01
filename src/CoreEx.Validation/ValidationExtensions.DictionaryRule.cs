namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a dictionary (<see cref="DictionaryRule{TEntity, TProperty, TKey, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="with">Extends configuration <see cref="DictionaryRule{TEntity, TProperty, TKey, TValue}.With"/>.</param>
    public static DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue> Dictionary<TEntity, TKey, TValue>(this IPropertyRule<TEntity, IDictionary<TKey, TValue>> rule, Func<DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With, DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With> with) where TEntity : class where TKey : notnull where TValue : notnull
        => (DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>)Chain(rule, new DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>(null, null, with));

    /// <summary>
    /// Chains a collection (<see cref="DictionaryRule{TEntity, TProperty, TKey, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="DictionaryRule{TEntity, TProperty, TKey, TValue}.With"/>.</param>
    public static DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue> Dictionary<TEntity, TKey, TValue>(this IPropertyRule<TEntity, IDictionary<TKey, TValue>> rule, int maxCount, Func<DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With, DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With>? with = null) where TEntity : class where TKey : notnull where TValue : notnull
        => (DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>)Chain(rule, new DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>(null, _ => maxCount, with));

    /// <summary>
    /// Chains a collection (<see cref="DictionaryRule{TEntity, TProperty, TKey, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="DictionaryRule{TEntity, TProperty, TKey, TValue}.With"/>.</param>
    public static DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue> Dictionary<TEntity, TKey, TValue>(this IPropertyRule<TEntity, IDictionary<TKey, TValue>> rule, Func<PropertyContext<TEntity, IDictionary<TKey, TValue>>, int?>? maxCount, Func<DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With, DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With>? with = null) where TEntity : class where TKey : notnull where TValue : notnull
        => (DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>)Chain(rule, new DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>(null, maxCount, with));

    /// <summary>
    /// Chains a collection (<see cref="DictionaryRule{TEntity, TProperty, TKey, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="minCount">The minimum count.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="DictionaryRule{TEntity, TProperty, TKey, TValue}.With"/>.</param>
    public static DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue> Dictionary<TEntity, TKey, TValue>(this IPropertyRule<TEntity, IDictionary<TKey, TValue>> rule, int minCount, int? maxCount, Func<DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With, DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With>? with = null) where TEntity : class where TKey : notnull where TValue : notnull
        => (DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>)Chain(rule, new DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>(_ => minCount, _ => maxCount, with));

    /// <summary>
    /// Chains a collection (<see cref="DictionaryRule{TEntity, TProperty, TKey, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="minCount">The minimum count.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="DictionaryRule{TEntity, TProperty, TKey, TValue}.With"/>.</param>
    public static DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue> Dictionary<TEntity, TKey, TValue>(this IPropertyRule<TEntity, IDictionary<TKey, TValue>> rule, Func<PropertyContext<TEntity, IDictionary<TKey, TValue>>, int>? minCount, Func<PropertyContext<TEntity, IDictionary<TKey, TValue>>, int?>? maxCount, Func<DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With, DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>.With>? with = null) where TEntity : class where TKey : notnull where TValue : notnull
        => (DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>)Chain(rule, new DictionaryRule<TEntity, IDictionary<TKey, TValue>, TKey, TValue>(minCount, maxCount, with));
}