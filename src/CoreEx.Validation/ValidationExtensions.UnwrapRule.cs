namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Validates the wrapped value, skipping the inner chain when an optional wrapper is unspecified.
    /// </summary>
    /// <typeparam name="TEntity">The owning entity type.</typeparam>
    /// <typeparam name="TValue">The wrapped value type.</typeparam>
    /// <param name="rule">The wrapped property rule.</param>
    /// <returns>A rule for the wrapped value.</returns>
    public static IPropertyRule<TEntity, TValue> Unwrap<TEntity, TValue>(this IPropertyRule<TEntity, IWrappedValue<TValue>> rule) where TEntity : class
        => Unwrap<TEntity, IWrappedValue<TValue>, TValue>(rule);

    /// <summary>
    /// Validates the wrapped value for an explicitly specified wrapper and value type.
    /// </summary>
    /// <typeparam name="TEntity">The owning entity type.</typeparam>
    /// <typeparam name="TWrapper">The wrapper type.</typeparam>
    /// <typeparam name="TValue">The wrapped value type.</typeparam>
    /// <param name="rule">The wrapped property rule.</param>
    /// <returns>A rule for the wrapped value.</returns>
    /// <remarks>Call this static helper with explicit type arguments for custom struct wrappers because generic variance cannot infer their wrapped value type.</remarks>
    public static IPropertyRule<TEntity, TValue> Unwrap<TEntity, TWrapper, TValue>(IPropertyRule<TEntity, TWrapper> rule) where TEntity : class where TWrapper : IWrappedValue<TValue>
    {
        var unwrapRule = new UnwrapRule<TEntity, TWrapper, TValue>();
        rule.ThrowIfNull().Chain(unwrapRule);
        return unwrapRule;
    }
}
