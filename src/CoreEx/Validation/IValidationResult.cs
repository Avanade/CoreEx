namespace CoreEx.Validation;

/// <summary>
/// Enables <see cref="Value"/> validation results.
/// </summary>
public interface IValidationResult : IToResult
{
    /// <summary>
    /// Gets the value being validated.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Indicates whether there has been one or more validation errors.
    /// </summary>
    bool HasErrors { get; }

    /// <summary>
    /// Gets a <see cref="MessageItemCollection"/> where <see cref="HasErrors"/> and individual errors have been recorded or other <see cref="MessageType"/> has been recorded; otherwise, <see langword="null"/>.
    /// </summary>
    MessageItemCollection? Messages { get; }

    /// <summary>
    /// Converts the <see cref="IValidationResult"/> into a corresponding <see cref="Exception"/>.
    /// </summary>
    /// <returns>The corresponding <see cref="Exception"/> (typically a <see cref="ValidationException"/>) where <see cref="HasErrors"/>; otherwise, <see langword="null"/>.</returns>
    Exception? ToException();

    /// <summary>
    /// Throws an <see cref="Exception"/> (typically a <see cref="ValidationException"/>) where <see cref="HasErrors"/>.
    /// </summary>
    /// <returns>The <see cref="IValidationResult"/> to support fluent-style method-chaining.</returns>
    IValidationResult ThrowOnError();
}