namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains an <see cref="System.Enum"/> (<see cref="EnumRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowed">An optional list of allowed values.</param>
    public static IPropertyRule<TEntity, TProperty> Enum<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, params TProperty[] allowed) where TEntity : class where TProperty : struct, Enum
        => Chain(rule, new EnumRule<TEntity, TProperty>(_ => allowed));

    /// <summary>
    /// Chains an <see cref="System.Enum"/> (<see cref="EnumRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowed">An optional list of allowed values.</param>
    public static IPropertyRule<TEntity, TProperty> Enum<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, IEnumerable<TProperty> allowed) where TEntity : class where TProperty : struct, Enum
        => Chain(rule, new EnumRule<TEntity, TProperty>(_ => allowed?.ToArray()));

    /// <summary>
    /// Chains an <see cref="System.Enum"/> (<see cref="EnumRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowed">An optional list of allowed values.</param>
    public static IPropertyRule<TEntity, TProperty> Enum<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Func<PropertyContext<TEntity, TProperty>, TProperty[]?>? allowed) where TEntity : class where TProperty : struct, Enum
        => Chain(rule, new EnumRule<TEntity, TProperty>(allowed));

    /* Nullable-enum */

    /// <summary>
    /// Chains an <see cref="System.Enum"/> (<see cref="EnumRule{TEntity, TProperty}.NullableRule"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowed">An optional list of allowed values.</param>
    public static IPropertyRule<TEntity, TProperty> Enum<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, params TProperty[] allowed) where TEntity : class where TProperty : struct, Enum
        => Chain(rule, new EnumRule<TEntity, TProperty>.NullableRule(_ => allowed));

    /// <summary>
    /// Chains an <see cref="System.Enum"/> (<see cref="EnumRule{TEntity, TProperty}.NullableRule"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowed">An optional list of allowed values.</param>
    public static IPropertyRule<TEntity, TProperty> Enum<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, IEnumerable<TProperty> allowed) where TEntity : class where TProperty : struct, Enum
        => Chain(rule, new EnumRule<TEntity, TProperty>.NullableRule(_ => allowed?.ToArray()));

    /// <summary>
    /// Chains an <see cref="System.Enum"/> (<see cref="EnumRule{TEntity, TProperty}.NullableRule"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="allowed">An optional list of allowed values.</param>
    public static IPropertyRule<TEntity, TProperty> Enum<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty?> rule, Func<PropertyContext<TEntity, TProperty>, TProperty[]?>? allowed) where TEntity : class where TProperty : struct, Enum
        => Chain(rule, new EnumRule<TEntity, TProperty>.NullableRule(allowed));

    /* Enum-string */

    /// <summary>
    /// Chains a <see langword="string"/>-based <see cref="System.Enum"/> (<see cref="EnumStringRule{TEntity}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="with">The <see cref="EnumStringRule{TEntity}.EnumWith"/> configuration function.</param>
    public static IPropertyRule<TEntity, string> Enum<TEntity>(this IPropertyRule<TEntity, string> rule, Func<EnumStringRule<TEntity>.EnumWith, EnumStringRule<TEntity>.EnumWith> with) where TEntity : class
        => Chain(rule, new EnumStringRule<TEntity>(with));
}