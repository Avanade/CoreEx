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

    /// <summary>
    /// Throws the corresponding exception, configuring it only when it is a <see cref="ValidationException"/>.
    /// </summary>
    /// <param name="configure">The non-null configuration action, invoked once only for a validation exception.</param>
    /// <returns>The same <see cref="IValidationResult"/> when no exception exists, supporting fluent-style method-chaining.</returns>
    /// <remarks>Other exception types are propagated unchanged. Configuration exceptions are not intercepted.</remarks>
    IValidationResult ThrowOnError(Action<ValidationException> configure)
    {
        configure.ThrowIfNull();
        var exception = ToException();
        if (exception is ValidationException validationException)
            configure(validationException);

        if (exception is not null)
            new Result(exception).ThrowOnError();

        return this;
    }
}
