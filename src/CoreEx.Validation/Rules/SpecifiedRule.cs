namespace CoreEx.Validation.Rules;

/// <summary>
/// Requires an optional wrapper to have been specified.
/// </summary>
/// <typeparam name="TEntity">The owning entity type.</typeparam>
/// <typeparam name="TWrapper">The optional wrapper type.</typeparam>
/// <typeparam name="TValue">The wrapped value type.</typeparam>
internal sealed class SpecifiedRule<TEntity, TWrapper, TValue> : PropertyRuleBase<TEntity, TWrapper> where TEntity : class where TWrapper : IOptionalValue<TValue>
{
    /// <inheritdoc/>
    protected override bool ValidateWhenNull => true;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TWrapper> context, CancellationToken cancellationToken)
    {
        if (context.Value is null || !context.Value.IsSpecified)
            context.AddError(ErrorText ?? ValidatorStrings.MandatoryFormat);

        return Task.CompletedTask;
    }
}
