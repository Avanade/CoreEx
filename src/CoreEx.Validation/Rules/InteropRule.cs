namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides an <i>interop</i> validation rule; intended for non-<c>CoreEx.Validation</c>.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="getValidator">The function to get the <see cref="IValidator{T}"/>.</param>
/// <param name="validateWhenNull">Indicates whether the validation will be performed where the property value is <see langword="null"/>.</param>
public class InteropRule<TEntity, TProperty>(Func<IValidator> getValidator, bool validateWhenNull) : PropertyRuleBase<TEntity, TProperty> where TEntity : class
{
    private readonly Func<IValidator> _getValidator = getValidator.ThrowIfNull();

    /// <inheritdoc/>
    protected override bool ValidateWhenNull => validateWhenNull;

    /// <inheritdoc/>
    protected async override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        var validator = _getValidator() ?? throw new InvalidOperationException("The get validator function must not return null.");
        IValidationResult vr;

        if (validator is IValidatorEx<TProperty> vex)
            vr = await vex.ValidateAsync(context.Value, context.CreateValidationArgs(), cancellationToken).ConfigureAwait(false);
        else if (validator is IValidator<TProperty> vtx)
            vr = await vtx.ValidateAsync(context.Value, cancellationToken).ConfigureAwait(false);
        else
            vr = await validator.ValidateAsync(context.Value, cancellationToken).ConfigureAwait(false);

        context.MergeResult(vr);
    }
}