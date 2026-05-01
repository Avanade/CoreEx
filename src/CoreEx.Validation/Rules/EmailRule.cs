namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides an e-mail <see langword="string"/> validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <param name="maxLength">The optional maximum string length.</param>
public sealed class EmailRule<TEntity>(Func<PropertyContext<TEntity, string>, int?>? maxLength) : PropertyRuleBase<TEntity, string> where TEntity : class
{
    private readonly Func<PropertyContext<TEntity, string>, int?>? _maxLength = maxLength;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, string> context, CancellationToken cancellationToken)
    {
        if (!System.Net.Mail.MailAddress.TryCreate(context.Value, out _))
            context.AddError(ErrorText ?? ValidatorStrings.EmailFormat);
        else if (_maxLength is not null)
        {
            var maxLength = _maxLength(context);
            if (maxLength.HasValue && context.Value!.Length > maxLength.Value)
                context.AddError(ErrorText ?? ValidatorStrings.MaxLengthFormat, maxLength.Value);
        }

        return Task.CompletedTask;
    }
}