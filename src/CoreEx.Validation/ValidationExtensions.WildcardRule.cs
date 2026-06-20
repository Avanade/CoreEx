namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains a <see cref="Wildcards.Wildcard"/> <see langword="string"/> (<see cref="WildcardRule{TEntity}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="wildcard">The optional <see cref="Wildcards.Wildcard"/>; defaults to <see cref="Wildcard.Default"/>.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> Wildcard<TEntity>(this IPropertyRule<TEntity, string> rule, Wildcard? wildcard = null) where TEntity : class
        => Chain(rule, new WildcardRule<TEntity>(_ => wildcard));

    /// <summary>
    /// Chains a <see cref="Wildcards.Wildcard"/> <see langword="string"/> (<see cref="WildcardRule{TEntity}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="wildcard">The optional <see cref="Wildcards.Wildcard"/>; defaults to <see cref="Wildcard.Default"/>.</param>
    /// <returns>The <see cref="StringRule{TEntity}"/>.</returns>
    public static IPropertyRule<TEntity, string> Wildcard<TEntity>(this IPropertyRule<TEntity, string> rule, Func<PropertyContext<TEntity, string>, Wildcard?> wildcard) where TEntity : class
        => Chain(rule, new WildcardRule<TEntity>(wildcard));

}