namespace CoreEx.AspNetCore;

/// <summary>
/// Represents the <see cref="WebApi{TResult}"/> request and response options.
/// </summary>
/// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
/// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
public sealed class WebApiRequestResponseOptions<TRequest, TResponse> : WebApiOptionsBase, IWebApiRequestOptions<TRequest>, IWebApiResponseOptions<TResponse>
{
    private Func<TResponse, Uri>? _locationUri;

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiRequestResponseOptions{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
    /// <param name="value">The deserialized request value.</param>
    public WebApiRequestResponseOptions(HttpRequest httpRequest, TRequest? value) : base(httpRequest) 
    {
        ValueOrDefault = value;

        // Override the ETag where specified as a request IF-MATCH header.
        if (value is not null && ETag is not null && value is IETag etag)
            etag.ETag = ETag;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebApiRequestResponseOptions{TRequest, TResponse}"/> class from an existing instance.
    /// </summary>
    /// <param name="options">The <see cref="WebApiOptionsBase"/>.</param>
    /// <param name="value">The deserialized request value.</param>
    public WebApiRequestResponseOptions(WebApiOptionsBase options, TRequest? value) : base(options) 
    {
        ValueOrDefault = value;

        // Override the ETag where specified as a request IF-MATCH header.
        if (value is not null && ETag is not null && value is IETag etag)
            etag.ETag = ETag;

        // Override the location function;
        if (options is IWebApiResponseOptions<TResponse> ro)
            _locationUri = ro.LocationUri;
    }

    /// <inheritdoc/>
    public TRequest? ValueOrDefault { get; }

    /// <inheritdoc/>
    [NotNull]
    public TRequest Value => ValueOrDefault.Required();

    /// <inheritdoc/>
    Func<TResponse, Uri>? IWebApiResponseOptions<TResponse>.LocationUri => _locationUri;

    /// <summary>
    /// Sets (overrides) the response <see cref="IWebApiResponseOptions{TResponse}.LocationUri"/> to use the <paramref name="locationUri"/> function.
    /// </summary>
    /// <param name="locationUri">The function to return the <see cref="Uri"/> representing the location of the resource.</param>
    /// <returns>The <see cref="WebApiRequestResponseOptions{TRequest, TResponse}"/> to support fluent-style method-chaining.</returns>
    public WebApiRequestResponseOptions<TRequest, TResponse> WithLocationUri(Func<TResponse, Uri>? locationUri)
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

    /// <inheritdoc/>
    protected internal override Result Verify() => WebApiRequestOptions<TRequest>.VerifyRequest(this, ValueOrDefault).Then(() => base.Verify());
}