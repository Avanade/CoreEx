namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a numeric validation to check whether negatives are allowed (defaults to <see langword="true"/>, i.e. allowed).
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="Type"/>.</typeparam>
/// <param name="allowNegatives">Indicates whether to allow negative values.</param>
public sealed class NumericRule<TEntity, TProperty>(Func<PropertyContext<TEntity, TProperty>, bool>? allowNegatives = null) : PropertyRuleBase<TEntity, TProperty> where TEntity : class where TProperty : INumber<TProperty>
{
    private readonly Func<PropertyContext<TEntity, TProperty>, bool>? _allowNegatives = allowNegatives;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        if (_allowNegatives is not null)
        {
            var allowNegatives = _allowNegatives(context);
            if (!allowNegatives && TProperty.IsNegative(context.Value))
                context.AddError(ErrorText ?? ValidatorStrings.AllowNegativesFormat);
        }

        return Task.CompletedTask;
    }
}