namespace CoreEx.Validation.Rules;

/// <summary>
/// Adapts a wrapped property's value to the existing typed property-rule chain.
/// </summary>
/// <typeparam name="TEntity">The owning entity type.</typeparam>
/// <typeparam name="TWrapper">The wrapper type.</typeparam>
/// <typeparam name="TValue">The wrapped value type.</typeparam>
/// <param name="wrapValue">An optional factory to write overridden values back into the wrapper.</param>
internal sealed class UnwrapRule<TEntity, TWrapper, TValue>(Func<TValue, TWrapper>? wrapValue = null) : PropertyRuleBase<TEntity, TValue>, IPropertyRule<TEntity> where TEntity : class where TWrapper : IWrappedValue<TValue>
{
    /// <inheritdoc/>
    async Task IPropertyRule<TEntity>.ValidateAsync(IPropertyContext<TEntity> context, CancellationToken cancellationToken)
    {
        if (context.Value is not TWrapper wrapper)
            throw new InvalidOperationException($"The property value must be {typeof(TWrapper)}.");

        if (wrapper is IOptionalValue<TValue> optional && !optional.IsSpecified)
            return;

        var outerMetadata = context.Metadata;
        var metadata = new PropertyRuntimeMetadata<TEntity, TValue>(outerMetadata.Name,
            entity => outerMetadata.GetValue<TWrapper>(entity).Value,
            wrapValue is not null && !outerMetadata.IsReadOnly ? (entity, value) => outerMetadata.SetValue(entity, wrapValue(value)) : null,
            text: () => outerMetadata.Text, jsonName: outerMetadata.JsonName, format: outerMetadata.Format);

        var value = wrapper.Value;
        var propertyContext = new PropertyContext<TEntity, TValue>(context, value, value is null, metadata);
        await ((IPropertyRuleEx<TEntity, TValue>)this).ValidateAsync(propertyContext, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TValue> context, CancellationToken cancellationToken) => Task.CompletedTask;
}
