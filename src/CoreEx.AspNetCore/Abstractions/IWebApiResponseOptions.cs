namespace CoreEx.AspNetCore.Abstractions;

/// <summary>
/// Enables the <see cref="WebApi{TResult}"/> response options.
/// </summary>
public interface IWebApiResponseOptions
{
    /// <summary>
    /// Creates the <see cref="Uri"/> representing the location of the resource.
    /// </summary>
    Uri? CreateLocationUri(object? value);
}