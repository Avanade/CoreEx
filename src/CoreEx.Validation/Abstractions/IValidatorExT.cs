namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Extends the <see cref="IValidator{T}"/>
/// </summary>
/// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
public interface IValidatorEx<T> : IValidatorEx, IValidator<T>
{
    /// <inheritdoc/>
    async Task<IValidationContext> IValidatorEx.ValidateAsync(object? value, ValidationArgs? args, CancellationToken cancellationToken)
        => await ValidateAsync((T)value!, args, cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Validate the value with optional <see cref="ValidationArgs"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IValidationContext{T}"/>.</returns>
    Task<IValidationContext<T>> ValidateAsync(T value, ValidationArgs? args, CancellationToken cancellationToken);

    /// <summary>
    /// Validate the value with optional <see cref="ValidationArgs"/> and automatically throw a <see cref="ValidationException"/> where <see cref="IValidationResult.HasErrors"/>.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task ValidateAndThrowAsync(T value, ValidationArgs? args, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the value and configures the <see cref="ValidationException"/> before throwing when validation fails.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
    /// <param name="configure">The optional configuration action, invoked once only when validation fails.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    async Task ValidateAndThrowAsync(T value, ValidationArgs? args, Action<ValidationException>? configure, CancellationToken cancellationToken)
    {
        var context = await ValidateAsync(value, args, cancellationToken).ConfigureAwait(false);
        if (context.HasErrors)
            Result.ValidationError(context.Messages, configure).ThrowOnError();
    }

    /// <summary>
    /// Validates the value and configures the exception before throwing when validation fails.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="configure">The configuration action, invoked once only when validation fails.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    Task ValidateAndThrowAsync(T value, Action<ValidationException> configure, CancellationToken cancellationToken)
        => ValidateAndThrowAsync(value, null, configure.ThrowIfNull(), cancellationToken);

    /// <summary>
    /// Validate using the <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="IValidationContext{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IValidationContext{T}"/>.</returns>
    /// <remarks>This is generally intended for internal use only.</remarks>
    Task ValidateAsync(IValidationContext<T> context, CancellationToken cancellationToken);
}
