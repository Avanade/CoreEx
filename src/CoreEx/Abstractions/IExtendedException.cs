namespace CoreEx.Abstractions;

/// <summary>
/// Enables the extended <see cref="Exception"/> capabilities.
/// </summary>
public interface IExtendedException
{
    /// <summary>
    /// Gets the exception message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the exception detail.
    /// </summary>
    string? Detail { get; }

    /// <summary>
    /// Gets the error type/category.
    /// </summary>
    string? ErrorType { get; }

    /// <summary>
    /// Gets the error code.
    /// </summary>
    string? ErrorCode { get; }

    /// <summary>
    /// Gets the corresponding <see cref="HttpStatusCode"/>.
    /// </summary>
    HttpStatusCode StatusCode { get; }

    /// <summary>
    /// Indicates whether this exception exception should be treated as a known/supported error; versus, being an unexpected outcome and handled accordingly. 
    /// </summary>
    bool IsError { get; }

    /// <summary>
    /// Indicates whether exception is transient; i.e. is a candidate for a retry.
    /// </summary>
    bool IsTransient { get; }

    /// <summary>
    /// Gets the <see cref="IsTransient"/> retry after interval.
    /// </summary>
    TimeSpan? RetryAfter { get; }

    /// <summary>
    /// Indicates whether the <see cref="IExtendedException"/> should be logged.
    /// </summary>
    bool ShouldBeLogged { get; }

    /// <summary>
    /// Gets an <see cref="IDictionary{TKey, TValue}"/> of extension values.
    /// </summary>
    /// <remarks>This enables additional values to be captured against the exception for later use/inspection.</remarks>
    IDictionary<string, object?> Extensions { get; }

    /// <summary>
    /// Indicates whether there are any <see cref="Extensions"/>.
    /// </summary>
    bool HasExtensions { get; }

    /// <summary>
    /// Converts the <see cref="IExtendedException"/> to a <see cref="Result"/>.
    /// </summary>
    /// <returns>The <see cref="Result"/>.</returns>
    Result ToResult();
}