namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Represents a result from a Web API operation.
/// </summary>
/// <typeparam name="TResult">The ASP.NET Core result <see cref="Type"/>.</typeparam>
/// <param name="httpResponse">The <see cref="HttpResponse"/>.</param>
internal readonly record struct WebApiResult<TResult>(HttpResponse httpResponse)
{
    /// <summary>
    /// Gets the <see cref="HttpResponse"/>.
    /// </summary>
    public HttpResponse HttpResponse { get; } = httpResponse.ThrowIfNull();

    /// <summary>
    /// Gets the content.
    /// </summary>
    public string? Content { get; init; }

    /// <summary>
    /// Gets the content type.
    /// </summary>
    public string? ContentType { get; init; }

    /// <summary>
    /// Gets the <see cref="HttpStatusCode"/>.
    /// </summary>
    public HttpStatusCode StatusCode { get; init; }

    /// <summary>
    /// Gets the <see cref="Exception"/>.
    /// </summary>
    public Exception? Exception { get; init; }

    /// <summary>
    /// Indicates whether to bypass exception logging for the <see cref="Exception"/>. 
    /// </summary>
    /// <remarks>This is typically used when the exception is already handled and logged, and you want to prevent duplicate logging.</remarks>
    public bool BypassExceptionLogging { get; init; }

    /// <summary>
    /// Gets the result where already pre-formed.
    /// </summary>
    public TResult? Result { get; init; }

    /// <summary>
    /// Gets the <see cref="WebApiHeader"/>.
    /// </summary>
    public WebApiHeader? Headers { get; init; }
}