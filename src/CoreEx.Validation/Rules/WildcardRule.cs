namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a <see cref="Wildcard"/> <see langword="string"/> validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <param name="wildcard">The <see cref="Wildcard"/>.</param>
public class WildcardRule<TEntity>(Func<PropertyContext<TEntity, string>, Wildcard?> wildcard) : PropertyRuleBase<TEntity, string> where TEntity : class
{
    private readonly Func<PropertyContext<TEntity, string>, Wildcard?> _wildcard = wildcard.ThrowIfNull();

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, string> context, CancellationToken cancellationToken)
    {
        var wildcard = _wildcard(context) ?? Wildcard.Default ?? Wildcard.MultiBasic;

        if (wildcard.Parse(context.Value).HasError)
            context.AddError(ErrorText ?? ValidatorStrings.WildcardFormat);

        return Task.CompletedTask;
    }
}