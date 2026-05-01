namespace CoreEx.Entities;

/// <summary>
/// Enables a typed <see cref="Value"/> result wrapper (non-error) that contains additional context.
/// </summary>
/// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
/// <remarks>This is not intended for error scenarios, as the likes of an <see cref="Exception"/>, <see cref="Result"/> or <see cref="Result{T}"/> enable accordingly.</remarks>
internal interface IValueResult<T> : IValueResult
{
    /// <inheritdoc/>
    object? IValueResult.Value => Value;

    /// <summary>
    /// Gets the value.
    /// </summary>
    new T Value { get; }
}