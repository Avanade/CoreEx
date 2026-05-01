namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides an <see cref="Enum"/> validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
/// <param name="allowed">An optional list of allowed values.</param>
public class EnumRule<TEntity, TProperty>(Func<PropertyContext<TEntity, TProperty>, TProperty[]?>? allowed) : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : struct, Enum
{
    private readonly Func<PropertyContext<TEntity, TProperty>, TProperty[]?>? _allowed = allowed;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(context.Value))
            context.AddError(ErrorText ?? ValidatorStrings.InvalidFormat);

        if (_allowed is not null)
        {
            var allowed = _allowed(context);
            if (allowed is not null && allowed.Length > 0 && !allowed.Contains(context.Value))
                context.AddError(ErrorText ?? ValidatorStrings.InvalidFormat);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Provides a corresponding <see cref="Nullable{T}"/> <see cref="Enum"/> validation..
    /// </summary>
    /// <param name="allowed">An optional list of allowed values.</param>
    public sealed class NullableRule(Func<PropertyContext<TEntity, TProperty>, TProperty[]?>? allowed) : EnumRule<TEntity, TProperty>(allowed) { }
}