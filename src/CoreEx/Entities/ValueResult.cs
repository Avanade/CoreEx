namespace CoreEx.Entities;

/// <summary>
/// Provides a typed <see cref="Value"/> result wrapper (non-error) that contains additional context.
/// </summary>
/// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
/// <remarks>This is not intended for error scenarios, as the likes of an <see cref="Exception"/>, <see cref="Result"/> or <see cref="Result{T}"/> enable accordingly.</remarks>
/// <param name="value">The value.</param>
/// <param name="statusCode">The resulting <see cref="HttpStatusCode"/>.</param>
public class ValueResult<T>(T value = default!, HttpStatusCode? statusCode = null) : IValueResult<T>
{
    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public T Value { get; set; } = value;

    /// <summary>
    /// Gets or sets the resulting <see cref="HttpStatusCode"/>.
    /// </summary>
    /// <remarks>This does not imply that the result has to be used in an HTTP context; just that this represents a number of well-known statuses.</remarks>
    public HttpStatusCode? StatusCode { get; set; } = statusCode;
}