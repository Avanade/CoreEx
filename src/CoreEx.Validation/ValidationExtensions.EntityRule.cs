namespace CoreEx.Validation;

public static partial class ValidationExtensions
{
    /// <summary>
    /// Chains an entity (<see cref="EntityRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="with">Extends configuration <see cref="EntityRule{TEntity, TProperty}.With"/>.</param>
    public static IPropertyRule<TEntity, TProperty> Entity<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, Action<EntityRule<TEntity, TProperty>.With> with) where TEntity : class where TProperty : class?
        => Chain(rule, new EntityRule<TEntity, TProperty>(with));

    /// <summary>
    /// Chains an entity (<see cref="EntityRule{TEntity, TProperty}"/>) validation to the existing <paramref name="rule"/>.
    /// </summary>
    /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
    /// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
    /// <param name="rule">The <see cref="IPropertyRule{TEntity, TProperty}"/> being extended.</param>
    /// <param name="validator">The <see cref="IValidatorEx{T}"/>.</param>
    /// <remarks>Consider using the <see cref="Entity{TEntity, TProperty}(IPropertyRule{TEntity, TProperty}, Action{EntityRule{TEntity, TProperty}.With})"/> as this provides additional validator options (where applicable).</remarks>
    public static IPropertyRule<TEntity, TProperty> Entity<TEntity, TProperty>(this IPropertyRule<TEntity, TProperty> rule, IValidatorEx<TProperty> validator) where TEntity : class where TProperty : class?
        => Entity(rule, w => w.WithValidator(validator));
}