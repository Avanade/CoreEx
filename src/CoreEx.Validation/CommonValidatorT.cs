namespace CoreEx.Validation;

/// <summary>
/// Provides a common validator enabling standardized configuration and validation behavior to be shared/reused.
/// </summary>
/// <typeparam name="TValue">The value <see cref="Type"/>.</typeparam>
/// <remarks>General guidance is to <b>not</b> use <see cref="Nullable{T}"/> (struct) or nullable class (i.e. <c>string?</c>) <typeparamref name="TValue"/>  as this will limit reusability with <i>no</i> functional benefits.</remarks>
/// <param name="configure">The action to configure the <see cref="CommonValidator{T}"/>.</param>
public class CommonValidator<TValue>(Action<InlineValidator<TValue>.Validator>? configure) : InlineValidator<TValue>(configure)
{
    private List<Func<PropertyContext<ValidationValue<TValue>, TValue>, CancellationToken, Task>>? _additionalAsync;

    /// <summary>
    /// Validate the common value (post all configured <see cref="PropertyRuleBase{TEntity, TProperty}"/> chained rules) enabling multiple additional validation functions to be added.
    /// </summary>
    /// <param name="additionalAsync">The additional validation function.</param>
    /// <returns>The <see cref="CommonValidator{T}"/>.</returns>
    public CommonValidator<TValue> AdditionalAsync(Func<PropertyContext<ValidationValue<TValue>, TValue>, CancellationToken, Task> additionalAsync)
    {
        (_additionalAsync ??= []).Add(additionalAsync.ThrowIfNull());
        return this;
    }

    /// <summary>
    /// Validate the common property value.
    /// </summary>
    /// <param name="context">The <see cref="PropertyContext{TEntity, TProperty}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected async override Task OnValidateAsync(PropertyContext<ValidationValue<TValue>, TValue> context, CancellationToken cancellationToken)
    {
        if (_additionalAsync is not null)
        {
            foreach (var validator in _additionalAsync)
            {
                await validator(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}