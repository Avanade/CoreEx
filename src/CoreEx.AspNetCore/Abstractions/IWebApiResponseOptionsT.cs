namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Enables the <see cref="WebApi{TResult}"/> response options.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public interface IWebApiResponseOptions<TResponse> : IWebApiResponseOptions
{
    /// <summary>
    /// Gets the function that will return the <see cref="Uri"/> representing the location of the resource.
    /// </summary>
    Func<TResponse, Uri>? LocationUri { get; }
}