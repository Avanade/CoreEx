namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Adds a entity predicate (<see cref="WhenClause{TEntity, TProperty}"/>) clause to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="predicate">A predicate to determine whether the current rule is to executed.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> WhenEntity<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Predicate<TEntity> predicate) where TEntity : class
        => AddClause(rule, new WhenClause<TEntity, TProperty>((c, _) => Task.FromResult(predicate.ThrowIfNull().Invoke(c.Entity))));

    /// <summary>
    /// Adds a property predicate (<see cref="WhenClause{TEntity, TProperty}"/>) clause to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="predicate">A predicate to determine whether the current rule is to executed.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> WhenValue<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Predicate<TProperty> predicate) where TEntity : class
        => AddClause(rule, new WhenClause<TEntity, TProperty>((c, _) => Task.FromResult(predicate.ThrowIfNull().Invoke(c.Value))));

    /// <summary>
    /// Adds a when <see cref="bool"/> (<see cref="WhenClause{TEntity, TProperty}"/>) clause to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="when">A <see cref="bool"/> to determine whether the current rule is to executed.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> When<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, bool when) where TEntity : class
        => AddClause(rule, new WhenClause<TEntity, TProperty>((_, _) => Task.FromResult(when)));

    /// <summary>
    /// Adds a <see cref="bool"/> function (<see cref="WhenClause{TEntity, TProperty}"/>) clause to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="when">A <see cref="bool"/> function to determine whether the current rule is to executed.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> When<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<bool> when) where TEntity : class
        => AddClause(rule, new WhenClause<TEntity, TProperty>((c, _) => Task.FromResult(when.ThrowIfNull().Invoke())));

    /// <summary>
    /// Adds a <see cref="PropertyContext{TEntity, TProperty}"/> <see cref="bool"/> function (<see cref="WhenClause{TEntity, TProperty}"/>) clause to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="when">A <see cref="PropertyContext{TEntity, TProperty}"/> <see cref="bool"/> function to determine whether the current rule is to executed.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> When<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, bool> when) where TEntity : class
        => AddClause(rule, new WhenClause<TEntity, TProperty>((c, _) => Task.FromResult(when.ThrowIfNull().Invoke(c))));

    /// <summary>
    /// Adds an <see langword="async"/> function (<see cref="WhenClause{TEntity, TProperty}"/>) clause to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="whenAsync">A <see cref="PredicateAsync{TEntity, TProperty}"/> to determine whether the current rule is to executed.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> When<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, PredicateAsync<TEntity, TProperty> whenAsync) where TEntity : class
        => AddClause(rule, new WhenClause<TEntity, TProperty>(whenAsync));

    /// <summary>
    /// Adds a must have non-<see langword="default"/> value (<see cref="WhenClause{TEntity, TProperty}"/>) clause to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <returns>The <paramref name="rule"/> to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, TProperty> WhenHasValue<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule) where TEntity : class
        => AddClause(rule, new WhenClause<TEntity, TProperty>((c, _) => Task.FromResult(Comparer<TProperty>.Default.Compare(c.Value, default!) != 0)));
}