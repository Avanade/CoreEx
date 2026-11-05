namespace CoreEx.Validation;

/// <summary>
/// Provides entity validation.
/// </summary>
/// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
/// <remarks>See also <see cref="Validator.Create{TEntity}"/>.</remarks>
public class Validator<TEntity> : ValidatorBase<TEntity, Validator<TEntity>> where TEntity : class
{
    private List<Func<ValidationContext<TEntity>, CancellationToken, Task>>? _additionalAsync;

    /// <inheritdoc/>
    public async sealed override Task<IValidationContext<TEntity>> ValidateAsync(TEntity value, ValidationArgs? args = null, CancellationToken cancellationToken = default)
    {
        var context = new ValidationContext<TEntity>(value, args ?? new ValidationArgs { FullyQualifiedEntityName = Validation.ValueName, FullyQualifiedJsonEntityName = null });
        await ValidateInternalAsync(context, cancellationToken).ConfigureAwait(false);
        return context;
    }

    /// <summary>
    /// Validate the value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IValidationContext{T}"/>.</returns>
    public Task<IValidationContext<TEntity>> ValidateAsync(TEntity value, CancellationToken cancellationToken)
        => ValidateAsync(value, null, cancellationToken);

    /// <inheritdoc/>
    public sealed override Task ValidateAndThrowAsync(TEntity value, ValidationArgs? args = null, CancellationToken cancellationToken = default)
        => ValidateAndThrowAsync(value, args, null, cancellationToken);

    /// <inheritdoc/>
    public async sealed override Task ValidateAndThrowAsync(TEntity value, ValidationArgs? args, Action<ValidationException>? configure, CancellationToken cancellationToken)
        => (await ValidateWithResultAsync(value, args, configure, cancellationToken).ConfigureAwait(false)).ThrowOnError();

    /// <summary>
    /// Validate the value and automatically throw a <see cref="ValidationException"/> where <see cref="IValidationResult.HasErrors"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    public Task ValidateAndThrowAsync(TEntity value, CancellationToken cancellationToken)
        => ValidateAndThrowAsync(value, args: null, cancellationToken);

    /// <inheritdoc/>
    internal override Task ValidateAsync(IValidationContext<TEntity> context, CancellationToken cancellationToken)
        => ValidateInternalAsync((ValidationContext<TEntity>)context, cancellationToken);

    /// <summary>
    /// Orchestrates the validation of the entity value
    /// </summary>
    private async Task ValidateInternalAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken)
    {
        if (context.Value is null)
        {
            context.AddError(Validation.MandatoryFormat, Validation.ValueText);
            return;
        }

        foreach (var rule in Rules)
        {
            await rule.ValidateAsync(context, cancellationToken).ConfigureAwait(false);
        }

        await OnValidateAsync(context, cancellationToken).ConfigureAwait(false);

        if (_additionalAsync is not null)
        {
            foreach (var item in _additionalAsync)
            {
                await item(context, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Validate the entity value (post all configured <see cref="ValidatorBase{TEntity, TSelf}"/> chained rules) enabling multiple additional validation functions to be added.
    /// </summary>
    /// <param name="additionalAsync">The additional validation function.</param>
    /// <returns>The <see cref="Validator{TEntity}"/>.</returns>
    public Validator<TEntity> AdditionalAsync(Func<ValidationContext<TEntity>, CancellationToken, Task> additionalAsync)
    {
        (_additionalAsync ??= []).Add(additionalAsync.ThrowIfNull());
        return this;
    }

    /// <summary>
    /// Validates the entity (post all configured <see cref="ValidatorBase{TEntity, TSelf}"/> chained rules) enabling additional validation logic to be added by the inheriting class.
    /// </summary>
    /// <param name="context">The <see cref="ValidationContext{TEntity}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    protected virtual Task OnValidateAsync(ValidationContext<TEntity> context, CancellationToken cancellationToken) => Task.CompletedTask;

    /// <summary>
    /// Validate the value with optional <see cref="ValidationArgs"/> with a corresponding <see cref="Result{T}"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <remarks>The <see cref="Result{T}"/>.</remarks>
    public Task<Result<TEntity>> ValidateWithResultAsync(TEntity value, ValidationArgs? args = null, CancellationToken cancellationToken = default)
        => ValidateWithResultAsync(value, args, null, cancellationToken);

    /// <summary>
    /// Validates the value and returns a result with a configured <see cref="ValidationException"/> when validation fails.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="configure">The configuration action, invoked once only when validation fails.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The validation <see cref="Result{T}"/>, retaining the original value on success.</returns>
    public Task<Result<TEntity>> ValidateWithResultAsync(TEntity value, Action<ValidationException> configure, CancellationToken cancellationToken)
        => ValidateWithResultAsync(value, null, configure.ThrowIfNull(), cancellationToken);

    /// <summary>
    /// Validates the value and returns a result with an optionally configured <see cref="ValidationException"/> when validation fails.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
    /// <param name="configure">The optional configuration action, invoked once only when validation fails.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The validation <see cref="Result{T}"/>, retaining the original value on success.</returns>
    public async Task<Result<TEntity>> ValidateWithResultAsync(TEntity value, ValidationArgs? args, Action<ValidationException>? configure, CancellationToken cancellationToken)
    {
        var vc = await ValidateAsync(value, args, cancellationToken).ConfigureAwait(false);
        return vc.HasErrors ? Result.ValidationError(vc.Messages, configure) : value;
    }
}
