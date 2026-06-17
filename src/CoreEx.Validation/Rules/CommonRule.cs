namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides <paramref name="common"/> validation integration.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="common">The <see cref="InlineValidator{T}"/>.</param>
/// <remarks>Although named <paramref name="common"/> (primary use), the underlying base <see cref="InlineValidator{TValue}"/> is supported to enable additional types where applicable.</remarks>
public sealed class CommonRule<TEntity, TProperty>(InlineValidator<TProperty> common) : PropertyRuleBase<TEntity, TProperty> where TEntity : class
{
    private readonly InlineValidator<TProperty> _commonValidator = common.ThrowIfNull();

    /// <inheritdoc/>
    protected override bool ValidateWhenNull => true;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken) => _commonValidator.ValidateAsync(context, cancellationToken);
}