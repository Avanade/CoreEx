namespace CoreEx.Entities;

/// <summary>
/// Enables a <see cref="Value"/> result wrapper (non-error) that contains additional context.
/// </summary>
/// <remarks>This is not intended for error scenarios, as the likes of an <see cref="Exception"/>, <see cref="Result"/> or <see cref="Result{T}"/> enable accordingly.</remarks>
internal interface IValueResult
{
    /// <summary>
    /// Gets the value.
    /// </summary>
    object? Value { get; }

    /// <summary>
    /// Gets the resulting <see cref="HttpStatusCode"/>.
    /// </summary>
    /// <remarks>This does not imply that the result has to be used in an HTTP context; just that this represents a number of well-known statuses.</remarks>
    HttpStatusCode? StatusCode { get; }
}