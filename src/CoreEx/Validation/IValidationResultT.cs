namespace CoreEx.Validation;

/// <summary>
/// Enables typed <see cref="Value"/> validation results.
/// </summary>
/// <typeparam name="T">The <see cref="Value"/> <see cref="Type"/>.</typeparam>
public interface IValidationResult<T> : IValidationResult
{
    /// <inheritdoc/>
    object? IValidationResult.Value => Value;

    /// <summary>
    /// Gets the value being validated.
    /// </summary>
    new T? Value { get; }
}