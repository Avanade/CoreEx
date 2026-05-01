namespace CoreEx.Validation.Abstractions;

/// <summary>
/// Provides a validation value entity wrapper.
/// </summary>
/// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
public sealed class ValidationValue<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationValue{T}"/> class.
    /// </summary>
    /// <param name="value">The value.</param>
    internal ValidationValue(T value) => Value = value;

    /// <summary>
    /// Gets the value.
    /// </summary>
    public T? Value { get; }
}