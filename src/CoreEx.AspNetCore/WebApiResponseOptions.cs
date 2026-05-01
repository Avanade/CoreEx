namespace CoreEx.AspNetCore;

/// <summary>
/// Represents the <see cref="WebApi{TResult}"/> response options.
/// </summary>
/// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
/// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
public sealed class WebApiResponseOptions<TResponse>(HttpRequest httpRequest) : WebApiOptionsBase(httpRequest), IWebApiResponseOptions<TResponse>
{
    private Func<TResponse, Uri>? _locationUri;

    /// <inheritdoc/>
    Func<TResponse, Uri>? IWebApiResponseOptions<TResponse>.LocationUri => _locationUri;

    /// <summary>
    /// Sets (overrides) the response <see cref="IWebApiResponseOptions{TResponse}.LocationUri"/> to use the <paramref name="locationUri"/> function.
    /// </summary>
    /// <param name="locationUri">The function to return the <see cref="Uri"/> representing the location of the resource.</param>
    /// <returns>The <see cref="WebApiResponseOptions{TResponse}"/> to support fluent-style method-chaining.</returns>
    public WebApiResponseOptions<TResponse> WithLocationUri(Func<TResponse, Uri>? locationUri)
    {
        _locationUri = locationUri;
        return this;
    }

    /// <inheritdoc/>
    Uri? IWebApiResponseOptions.CreateLocationUri(object? value)
    {
        // Check if there is a valued-version and invoke.
        if (_locationUri is not null)
        {
            if (value is null)
                throw new InvalidOperationException($"The resulting value cannot be null when {nameof(LocationUri)} is specified.");

            var val = (TResponse)value;
            return _locationUri(val);
        }

        // Invoke the default parameterless version.
        return LocationUri?.Invoke();
    }
}