namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Requires an optional wrapper to be specified without rejecting a specified null value.
    /// </summary>
    /// <typeparam name="TEntity">The owning entity type.</typeparam>
    /// <typeparam name="TValue">The wrapped value type.</typeparam>
    /// <param name="rule">The optional property rule.</param>
    /// <returns>The presence rule to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, IOptionalValue<TValue>> Specified<TEntity, TValue>(this IPropertyRule<TEntity, IOptionalValue<TValue>> rule) where TEntity : class
        => Specified<TEntity, IOptionalValue<TValue>, TValue>(rule);

    /// <summary>
    /// Requires an optional wrapper of an explicitly specified type to be specified.
    /// </summary>
    /// <typeparam name="TEntity">The owning entity type.</typeparam>
    /// <typeparam name="TWrapper">The optional wrapper type.</typeparam>
    /// <typeparam name="TValue">The wrapped value type.</typeparam>
    /// <param name="rule">The optional property rule.</param>
    /// <returns>The presence rule to support fluent-style method-chaining.</returns>
    /// <remarks>Call this static helper with explicit type arguments for custom struct wrappers because generic variance cannot infer their wrapped value type.</remarks>
    public static IPropertyRule<TEntity, TWrapper> Specified<TEntity, TWrapper, TValue>(IPropertyRule<TEntity, TWrapper> rule) where TEntity : class where TWrapper : IOptionalValue<TValue>
        => Chain(rule, new SpecifiedRule<TEntity, TWrapper, TValue>());
}
