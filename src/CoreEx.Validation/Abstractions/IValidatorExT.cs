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
    /// Validate using the <paramref name="context"/>.
    /// </summary>
    /// <param name="context">The <see cref="IValidationContext{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IValidationContext{T}"/>.</returns>
    /// <remarks>This is generally intended for internal use only.</remarks>
    Task ValidateAsync(IValidationContext<T> context, CancellationToken cancellationToken);
}