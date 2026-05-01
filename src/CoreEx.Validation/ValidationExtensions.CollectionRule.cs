namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a collection (<see cref="CollectionRule{TEntity, TProperty, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The <see cref="IEnumerable{T}"/> item <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> to chain to.</param>
    /// <param name="with">Extends configuration <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    public static CollectionRule<TEntity, IEnumerable<TItem>, TItem> Collection<TEntity, TItem>(this IPropertyRule<TEntity, IEnumerable<TItem?>> rule, Func<CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With, CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With> with) where TEntity : class
        => (CollectionRule<TEntity, IEnumerable<TItem>, TItem>)Chain(rule, new CollectionRule<TEntity, IEnumerable<TItem>, TItem>(null, null, with));

    /// <summary>
    /// Chains a collection (<see cref="CollectionRule{TEntity, TProperty, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The <see cref="IEnumerable{T}"/> item <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> to chain to.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    public static CollectionRule<TEntity, IEnumerable<TItem>, TItem> Collection<TEntity, TItem>(this IPropertyRule<TEntity, IEnumerable<TItem?>> rule, int maxCount, Func<CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With, CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With>? with = null) where TEntity : class
        => (CollectionRule<TEntity, IEnumerable<TItem>, TItem>)Chain(rule, new CollectionRule<TEntity, IEnumerable<TItem>, TItem>(null, _ => maxCount, with));

    /// <summary>
    /// Chains a collection (<see cref="CollectionRule{TEntity, TProperty, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The <see cref="IEnumerable{T}"/> item <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> to chain to.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    public static CollectionRule<TEntity, IEnumerable<TItem>, TItem> Collection<TEntity, TItem>(this IPropertyRule<TEntity, IEnumerable<TItem?>> rule, Func<PropertyContext<TEntity, IEnumerable<TItem>>, int?>? maxCount, Func<CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With, CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With>? with = null) where TEntity : class
        => (CollectionRule<TEntity, IEnumerable<TItem>, TItem>)Chain(rule, new CollectionRule<TEntity, IEnumerable<TItem>, TItem>(null, maxCount, with));

    /// <summary>
    /// Chains a collection (<see cref="CollectionRule{TEntity, TProperty, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The <see cref="IEnumerable{T}"/> item <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> to chain to.</param>
    /// <param name="minCount">The minimum count.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    public static CollectionRule<TEntity, IEnumerable<TItem>, TItem> Collection<TEntity, TItem>(this IPropertyRule<TEntity, IEnumerable<TItem?>> rule, int minCount, int? maxCount, Func<CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With, CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With>? with = null) where TEntity : class
        => (CollectionRule<TEntity, IEnumerable<TItem>, TItem>)Chain(rule, new CollectionRule<TEntity, IEnumerable<TItem>, TItem>(_ => minCount, _ => maxCount, with));

    /// <summary>
    /// Chains a collection (<see cref="CollectionRule{TEntity, TProperty, TItem}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The <see cref="IEnumerable{T}"/> item <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> to chain to.</param>
    /// <param name="minCount">The minimum count.</param>
    /// <param name="maxCount">The maximum count.</param>
    /// <param name="with">Extends configuration <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    public static CollectionRule<TEntity, IEnumerable<TItem>, TItem> Collection<TEntity, TItem>(this IPropertyRule<TEntity, IEnumerable<TItem?>> rule, Func<PropertyContext<TEntity, IEnumerable<TItem>>, int>? minCount, Func<PropertyContext<TEntity, IEnumerable<TItem>>, int?>? maxCount, Func<CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With, CollectionRule<TEntity, IEnumerable<TItem>, TItem>.With>? with = null) where TEntity : class
        => (CollectionRule<TEntity, IEnumerable<TItem>, TItem>)Chain(rule, new CollectionRule<TEntity, IEnumerable<TItem>, TItem>(minCount, maxCount, with));

    /* WithDuplicateCheck-extensions */

    /// <summary>
    /// Sets the duplicate check based on the <see cref="IIdentifierCore.Id"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="with">The <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    /// <param name="duplicateText">The duplicate <see cref="LText"/> to be used in the error message.</param>
    /// <returns>The <paramref name="with"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="duplicateText"/> defaults to <see cref="ValidatorStrings.IdentifierText"/>.</remarks>
    public static CollectionRule<TEntity, TProperty, TItem>.With WithDuplicateIdCheck<TEntity, TProperty, TItem>(this CollectionRule<TEntity, TProperty, TItem>.With with, LText? duplicateText = null) where TEntity : class where TProperty : IEnumerable<TItem> where TItem : IIdentifierCore
        => with.ThrowIfNull().WithDuplicateCheckingInternal(item => item.EntityKey, CompositeKeyComparer.Default, () => duplicateText ?? ValidatorStrings.IdentifierText);

    /// <summary>
    /// Sets the duplicate check based on the <see cref="IEntityKey.EntityKey"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="with">The <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    /// <param name="duplicateText">The duplicate <see cref="LText"/> to be used in the error message.</param>
    /// <returns>The <paramref name="with"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="duplicateText"/> defaults to <see cref="ValidatorStrings.KeyText"/>.</remarks>
    public static CollectionRule<TEntity, TProperty, TItem>.With WithDuplicateKeyCheck<TEntity, TProperty, TItem>(this CollectionRule<TEntity, TProperty, TItem>.With with, LText? duplicateText = null) where TEntity : class where TProperty : IEnumerable<TItem> where TItem : IEntityKey
        => with.ThrowIfNull().WithDuplicateCheckingInternal(item => item.EntityKey, CompositeKeyComparer.Default, () => duplicateText ?? ValidatorStrings.KeyText);

    /// <summary>
    /// Sets the duplicate check based on the <paramref name="propertyExpression"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItemProperty">The item property <see cref="Type"/>.</typeparam>
    /// <param name="with">The <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    /// <param name="propertyExpression">The <see cref="Expression"/> to reference the entity property.</param>
    /// <param name="comparer">The <see cref="IEqualityComparer{T}"/>.</param>
    /// <param name="duplicateText">The duplicate <see cref="LText"/> to be used in the error message.</param>
    /// <returns>The <paramref name="with"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="duplicateText"/> defaults to the resulting <paramref name="propertyExpression"/> <see cref="IPropertyRuntimeMetadata.Text"/>.</remarks>
    public static CollectionRule<TEntity, TProperty, TItem>.With WithDuplicatePropertyCheck<TEntity, TProperty, TItem, TItemProperty>(this CollectionRule<TEntity, TProperty, TItem>.With with, Expression<Func<TItem, TItemProperty>> propertyExpression, IEqualityComparer<TItemProperty>? comparer = null, LText? duplicateText = null) where TEntity : class where TProperty : IEnumerable<TItem> where TItem : class
    {
        var dcp = RuntimeMetadata.GetForExpression(propertyExpression.ThrowIfNull());
        return with.ThrowIfNull().WithDuplicateCheckingInternal(dcp.GetValue<TItemProperty>, comparer, () => duplicateText ??= dcp.Text);
    }

    /// <summary>
    /// Sets the duplicate check based on the item value (<see cref="IEquatable{T}"/>).
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <param name="with">The <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    /// <param name="duplicateText">The duplicate <see cref="LText"/> to be used in the error message.</param>
    /// <param name="comparer">The equality comparer.</param>
    /// <returns>The <paramref name="with"/> to support fluent-style method-chaining.</returns>
    /// <remarks>The <paramref name="duplicateText"/> defaults to <see cref="ValidatorStrings.KeyText"/>.</remarks>
    public static CollectionRule<TEntity, TProperty, TItem>.With WithDuplicateCheck<TEntity, TProperty, TItem>(this CollectionRule<TEntity, TProperty, TItem>.With with, IEqualityComparer<TItem>? comparer = null, LText? duplicateText = null) where TEntity : class where TProperty : IEnumerable<TItem> where TItem : IEquatable<TItem>
        => with.ThrowIfNull().WithDuplicateCheckingInternal(item => item, comparer, () => duplicateText ?? ValidatorStrings.ItemText);

    /// <summary>
    /// Sets the duplicate check based on the <paramref name="keySelector"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TKey">The key <see cref="Type"/>.</typeparam>
    /// <param name="with">The <see cref="CollectionRule{TEntity, TProperty, TItem}.With"/>.</param>
    /// <param name="keySelector">The key selector.</param>
    /// <param name="comparer">The equality comparer.</param>
    /// <param name="duplicateText">The duplicate <see cref="LText"/> to be used in the error message.</param>
    /// <returns>The <paramref name="with"/> to support fluent-style method-chaining.</returns>
    public static CollectionRule<TEntity, TProperty, TItem>.With WithDuplicateCheck<TEntity, TProperty, TItem, TKey>(this CollectionRule<TEntity, TProperty, TItem>.With with, Func<TItem, TKey> keySelector, IEqualityComparer<TKey>? comparer = null, LText? duplicateText = null) where TEntity : class where TProperty : IEnumerable<TItem>
        => with.ThrowIfNull().WithDuplicateCheckingInternal(keySelector, comparer, () => duplicateText ?? ValidatorStrings.ItemText);
}