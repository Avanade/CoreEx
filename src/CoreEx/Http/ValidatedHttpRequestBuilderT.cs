namespace CoreEx.Http;

/// <summary>
/// Enables opt-in validation when sending an HTTP request and consuming a deserialized response value.
/// </summary>
/// <typeparam name="T">The response value type.</typeparam>
/// <param name="builder">The <see cref="HttpRequestBuilder"/>.</param>
/// <param name="validator">The response value validator.</param>
public sealed class ValidatedHttpRequestBuilder<T>(HttpRequestBuilder builder, Validation.IValidator<T> validator)
{
    private readonly HttpRequestBuilder _builder = builder.ThrowIfNull();
    private readonly Validation.IValidator<T> _validator = validator.ThrowIfNull();

    /// <summary>
    /// Gets the <see cref="HttpRequestBuilder"/>.
    /// </summary>
    public HttpRequestBuilder Builder => _builder;

    /// <summary>
    /// Gets the <see cref="Validation.IValidator{T}"/>.
    /// </summary>
    public Validation.IValidator<T> Validator => _validator;

    /// <summary>
    /// Configures a <see cref="Result"/> to return when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The <see cref="Result"/> to return.</param>
    /// <returns>The <see cref="ValidatedHttpRequestBuilder{T}"/> instance to support fluent chaining.</returns>
    public ValidatedHttpRequestBuilder<T> WithStatusResult(HttpStatusCode statusCode, Result result)
    {
        _builder.WithStatusResult(statusCode, result);
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The function returning the <see cref="Result"/>.</param>
    /// <returns>The <see cref="ValidatedHttpRequestBuilder{T}"/> instance to support fluent chaining.</returns>
    public ValidatedHttpRequestBuilder<T> WithStatusResult(HttpStatusCode statusCode, Func<Result> result)
    {
        _builder.WithStatusResult(statusCode, result);
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The function returning the <see cref="Result"/> for the response.</param>
    /// <returns>The <see cref="ValidatedHttpRequestBuilder{T}"/> instance to support fluent chaining.</returns>
    public ValidatedHttpRequestBuilder<T> WithStatusResult(HttpStatusCode statusCode, Func<HttpResponseMessage, Result> result)
    {
        _builder.WithStatusResult(statusCode, result);
        return this;
    }

    /// <summary>
    /// Configures an asynchronous <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The asynchronous function returning the <see cref="Result"/> for the response.</param>
    /// <returns>The <see cref="ValidatedHttpRequestBuilder{T}"/> instance to support fluent chaining.</returns>
    public ValidatedHttpRequestBuilder<T> WithStatusResult(HttpStatusCode statusCode, Func<HttpResponseMessage, CancellationToken, Task<Result>> result)
    {
        _builder.WithStatusResult(statusCode, result);
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> to return when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Result)"/>.
    /// </summary>
    public ValidatedHttpRequestBuilder<T> WithErrorStatus(HttpStatusCode statusCode, Result result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{Result})"/>.
    /// </summary>
    public ValidatedHttpRequestBuilder<T> WithErrorStatus(HttpStatusCode statusCode, Func<Result> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{HttpResponseMessage, Result})"/>.
    /// </summary>
    public ValidatedHttpRequestBuilder<T> WithErrorStatus(HttpStatusCode statusCode, Func<HttpResponseMessage, Result> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures an asynchronous <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{HttpResponseMessage, CancellationToken, Task{Result}})"/>.
    /// </summary>
    public ValidatedHttpRequestBuilder<T> WithErrorStatus(HttpStatusCode statusCode, Func<HttpResponseMessage, CancellationToken, Task<Result>> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Sends the HTTP request and gets a result containing the validated value; missing content or a null validated value is a failure. Disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated result.</returns>
    public Task<Result<T>> ToResultAsync(CancellationToken cancellationToken = default) => ToResultAsync(null, cancellationToken);

    /// <summary>
    /// Sends the HTTP request and gets a result containing the validated value; missing content or a null validated value is a failure. Disposes the response upon completion.
    /// </summary>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated result.</returns>
    public async Task<Result<T>> ToResultAsync(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        using var response = await _builder.GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await _builder.TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
            return customResult.Bind<T>();

        return await response.WithValidator(_validator).ToResultAsync(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and gets a result containing the validated value, allowing null; absent response values are not validated. Disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated result.</returns>
    public Task<Result<T?>> ToResultOrDefaultAsync(CancellationToken cancellationToken = default) => ToResultOrDefaultAsync(null, cancellationToken);

    /// <summary>
    /// Sends the HTTP request and gets a result containing the validated value, allowing null; absent response values are not validated. Disposes the response upon completion.
    /// </summary>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated result.</returns>
    public async Task<Result<T?>> ToResultOrDefaultAsync(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        using var response = await _builder.GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await _builder.TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
            return customResult.Bind<T?>();

        return await response.WithValidator(_validator).ToResultOrDefaultAsync(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and gets the validated value, throwing on failure, including missing content or a null validated value. Disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value.</returns>
    public Task<T> GetValueAsync(CancellationToken cancellationToken = default) => GetValueAsync(null, cancellationToken);

    /// <summary>
    /// Sends the HTTP request and gets the validated value, throwing on failure, including missing content or a null validated value. Disposes the response upon completion.
    /// </summary>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value.</returns>
    public async Task<T> GetValueAsync(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        using var response = await _builder.GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await _builder.TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
        {
            customResult.ThrowOnError();
            return customResult.Bind<T>().Value;
        }

        return await response.WithValidator(_validator).GetValueAsync(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and gets the validated value, allowing null and throwing on failure; absent response values are not validated. Disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value or null.</returns>
    public Task<T?> GetValueOrDefaultAsync(CancellationToken cancellationToken = default) => GetValueOrDefaultAsync(null, cancellationToken);

    /// <summary>
    /// Sends the HTTP request and gets the validated value, allowing null and throwing on failure; absent response values are not validated. Disposes the response upon completion.
    /// </summary>
    /// <param name="jsonSerializerOptions">The optional JSON serializer options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The validated value or null.</returns>
    public async Task<T?> GetValueOrDefaultAsync(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        using var response = await _builder.GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await _builder.TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
        {
            customResult.ThrowOnError();
            return customResult.Bind<T?>().Value;
        }

        return await response.WithValidator(_validator).GetValueOrDefaultAsync(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
    }
}
