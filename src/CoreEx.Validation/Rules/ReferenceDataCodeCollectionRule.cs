namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides an <see cref="IReferenceDataCodeCollection"/> validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
/// <param name="allowInactive">Indicates whether to allow an <see cref="IReferenceData"/> value where <see cref="IReferenceData.IsActive"/> is set to <see langword="false"/>.</param>
public class ReferenceDataCodeCollectionRule<TEntity, TProperty>(bool allowInactive) : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : IReferenceDataCodeCollection
{
    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        if (context.Value.HasInvalidItems || (!allowInactive && context.Value.HasInactiveItems))
            context.AddError(ErrorText ?? ValidatorStrings.InvalidItemsFormat);

        return Task.CompletedTask;
    }
}