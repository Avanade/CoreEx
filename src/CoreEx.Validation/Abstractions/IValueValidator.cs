namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Enables the <see cref="ValueValidator{T}"/> validation.
/// </summary>
/// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
public interface IValueValidator<T>
{
    /// <summary>
    /// Validate the value.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IValidationResult{T}"/>.</returns>
    Task<IValidationResult<ValidationValue<T>>> ValidateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate the value with optional <see cref="ValidationArgs"/>.
    /// </summary>
    /// <param name="args">An optional <see cref="ValidationArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IValidationResult{T}"/>.</returns>
    Task<IValidationResult<ValidationValue<T>>> ValidateAsync(ValidationArgs? args, CancellationToken cancellationToken = default);
}