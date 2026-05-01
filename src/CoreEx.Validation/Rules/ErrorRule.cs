namespace CoreEx.Validation.Rules;

/// <summary>
/// Provides a validation rule that will always emit an error; unless, a succeeding conditional clause prevents it.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="System.Type"/>.</typeparam>
/// <typeparam name="TProperty">The property <see cref="System.Type"/>.</typeparam>
/// <param name="errorText">The resulting default error <see cref="LText"/>.</param>
public class ErrorRule<TEntity, TProperty>(LText errorText) : PropertyRuleBase<TEntity, TProperty> where TEntity : class
{
    private readonly LText _errorText = errorText;

    /// <inheritdoc/>
    protected override Task OnValidateAsync(PropertyContext<TEntity, TProperty> context, CancellationToken cancellationToken)
    {
        context.AddError(ErrorText ?? _errorText);
        return Task.CompletedTask;
    }
}