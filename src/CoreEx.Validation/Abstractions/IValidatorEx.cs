
namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Extends the <see cref="IValidator"/>.
/// </summary>
public interface IValidatorEx : IValidator
{
    /// <summary>
    /// Validate the <paramref name="value"/> with optional <paramref name="args"/>.
    /// </summary>
    /// <param name="value">The value to validate.</param>
    /// <param name="args">The optional <see cref="ValidationArgs"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="IValidationContext"/>.</returns>
    Task<IValidationContext> ValidateAsync(object? value, ValidationArgs? args, CancellationToken cancellationToken);
}