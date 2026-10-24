namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Requires the optional property to be specified.
    /// </summary>
    /// <typeparam name="TEntity">The owning entity type.</typeparam>
    /// <typeparam name="TValue">The wrapped value type.</typeparam>
    /// <param name="rule">The optional property rule.</param>
    /// <returns>The presence rule to support fluent-style method-chaining.</returns>
    public static IPropertyRule<TEntity, Optional<TValue>> Specified<TEntity, TValue>(this IPropertyRule<TEntity, Optional<TValue>> rule) where TEntity : class
        => Specified<TEntity, Optional<TValue>, TValue>(rule);

    /// <summary>
    /// Validates the wrapped value when the optional property is specified.
    /// </summary>
    /// <typeparam name="TEntity">The owning entity type.</typeparam>
    /// <typeparam name="TValue">The wrapped value type.</typeparam>
    /// <param name="rule">The optional property rule.</param>
    /// <returns>A rule for the wrapped value.</returns>
    public static IPropertyRule<TEntity, TValue> Unwrap<TEntity, TValue>(this IPropertyRule<TEntity, Optional<TValue>> rule) where TEntity : class
    {
        var unwrapRule = new UnwrapRule<TEntity, Optional<TValue>, TValue>(Optional<TValue>.Specified);
        rule.ThrowIfNull().Chain(unwrapRule);
        return unwrapRule;
    }
}
