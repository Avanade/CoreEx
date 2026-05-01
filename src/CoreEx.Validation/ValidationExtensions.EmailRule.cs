namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains an e-mail <see langword="string"/> (<see cref="EmailRule{TEntity}"/>) with optional <paramref name="maxLength"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="maxLength">The maximum string length.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> Email<TEntity>(this IPropertyRule<TEntity, string> rule, int maxLength) where TEntity : class
        => Chain(rule, new EmailRule<TEntity>(_ => maxLength));

    /// <summary>
    /// Chains an e-mail <see langword="string"/> (<see cref="EmailRule{TEntity}"/>) with optional <paramref name="maxLength"/> validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="maxLength">The optional maximum string length.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> Email<TEntity>(this IPropertyRule<TEntity, string> rule, Func<PropertyContext<TEntity, string>, int?>? maxLength = null) where TEntity : class
        => Chain(rule, new EmailRule<TEntity>(maxLength));
}