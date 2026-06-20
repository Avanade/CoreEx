namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides <see langword="string"/> validation including minimum and maximum length, and regular expression.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <param name="minLength">The minimum length.</param>
/// <param name="maxLength">The maximum length.</param>
/// <param name="regex">The regular expression.</param>
public sealed class StringRule<TEntity>(Func<PropertyContext<TEntity, string>, int>? minLength = null, Func<PropertyContext<TEntity, string>, int?>? maxLength = null, Func<PropertyContext<TEntity, string>, Regex?>? regex = null) : PropertyRuleBase<TEntity, string> where TEntity : class
{
    private readonly Func<PropertyContext<TEntity, string>, int>? _minLength = minLength;
    private readonly Func<PropertyContext<TEntity, string>, int?>? _maxLength = maxLength;
    private readonly Func<PropertyContext<TEntity, string>, Regex?>? _regex = regex;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, string> context, CancellationToken cancellationToken)
    {
        var minLength = _minLength?.Invoke(context) ?? 0;
        var maxLength = _maxLength?.Invoke(context);
        var regex = _regex?.Invoke(context);

        if (minLength < 0)
            throw new InvalidOperationException($"Minimum length must be zero or greater.");

        if (maxLength is not null && maxLength.Value <= 0)
            throw new InvalidOperationException($"Maximum length must be greater than zero.");

        if (minLength > 0 && maxLength.HasValue && minLength == maxLength!.Value && context.Value.Length != minLength)
        {
            context.AddError(ErrorText ?? ValidatorStrings.ExactLengthFormat, minLength);
            return Task.CompletedTask;
        }

        if (context.Value.Length < minLength)
        {
            context.AddError(ErrorText ?? ValidatorStrings.MinLengthFormat, minLength);
            return Task.CompletedTask;
        }

        if (maxLength.HasValue && context.Value.Length > maxLength.Value)
        {
            context.AddError(ErrorText ?? ValidatorStrings.MaxLengthFormat, maxLength);
            return Task.CompletedTask;
        }

        if (regex is not null && !regex.IsMatch(context.Value))
            context.AddError(ErrorText ?? ValidatorStrings.RegexFormat);

        return Task.CompletedTask;
    }
}