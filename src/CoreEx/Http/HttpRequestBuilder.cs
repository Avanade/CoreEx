/*
 * NOTE: When adding, updating, or removing request configuration methods in this file,
 * ensure the matching extensions in Extensions.HttpRequestMessage (CoreEx.Extensions)
 * are kept in sync, and vice versa, to preserve consistency across both HTTP request APIs.
 */

namespace CoreEx.Http;

/// <summary>
/// Builds and executes an HTTP request, providing fluent terminal methods that handle response consumption and disposal.
/// </summary>
/// <param name="client">The <see cref="HttpClient"/>.</param>
/// <param name="method">The <see cref="HttpMethod"/>.</param>
/// <param name="requestUri">The request URI.</param>
/// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
public sealed class HttpRequestBuilder(HttpClient client, HttpMethod method, string? requestUri, Action<HttpRequestMessage>? configureRequest = null)
{
    private readonly HttpClient _client = client.ThrowIfNull();
    private readonly HttpMethod _method = method.ThrowIfNull();
    private readonly string? _requestUri = requestUri;
    private Action<HttpRequestMessage>? _configureRequest = configureRequest;
    private Dictionary<HttpStatusCode, Func<HttpResponseMessage, CancellationToken, Task<Result>>>? _statusResults;

    /// <summary>
    /// Gets the <see cref="HttpClient"/>.
    /// </summary>
    public HttpClient Client => _client;

    /// <summary>
    /// Gets the <see cref="HttpMethod"/>.
    /// </summary>
    public HttpMethod Method => _method;

    /// <summary>
    /// Gets the request URI.
    /// </summary>
    public string? RequestUri => _requestUri;

    /// <summary>
    /// Adds an action to configure the <see cref="HttpRequestMessage"/> before it is sent.
    /// </summary>
    /// <param name="configureRequest">The action to configure the request.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder Configure(Action<HttpRequestMessage> configureRequest)
    {
        configureRequest.ThrowIfNull();
        var existing = _configureRequest;
        _configureRequest = existing is null ? configureRequest : (r => { existing(r); configureRequest(r); });
        return this;
    }

    /// <summary>
    /// Adds the <see cref="HttpRequestHeaders.IfMatch"/> header with the specified <paramref name="etag"/> (where not <see langword="null"/>).
    /// </summary>
    /// <param name="etag">The entity tag value.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithIfMatch(string? etag) => Configure(r => r.WithIfMatch(etag));

    /// <summary>
    /// Adds the <see cref="HttpRequestHeaders.IfNoneMatch"/> header with the specified <paramref name="etag"/> (where not <see langword="null"/>).
    /// </summary>
    /// <param name="etag">The entity tag value.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithIfNoneMatch(string? etag) => Configure(r => r.WithIfNoneMatch(etag));

    /// <summary>
    /// Sets the request content type to <see cref="HttpNames.MergePatchJsonMediaTypeName"/>.
    /// </summary>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithMergePatchJsonContentType() => Configure(r => r.WithMergePatchJsonContentType());

    /// <summary>
    /// Adds the <see cref="HttpNames.IdempotencyKeyHeaderName"/> header with the specified <paramref name="idempotencyKey"/> or generates a new one.
    /// </summary>
    /// <param name="idempotencyKey">The optional idempotency key; where not specified, a new <see cref="Guid"/> is generated and used.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithIdempotencyKey(string? idempotencyKey = null) => Configure(r => r.WithIdempotencyKey(idempotencyKey));

    /// <summary>
    /// Adds <see cref="PagingArgs"/> to the request URI query string.
    /// </summary>
    /// <param name="paging">The <see cref="PagingArgs"/>.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithPaging(PagingArgs? paging) => Configure(r => r.WithPaging(paging));

    /// <summary>
    /// Adds paging query parameters to the request URI query string.
    /// </summary>
    /// <param name="skip">The specified number of elements in a sequence to bypass.</param>
    /// <param name="take">The specified number of contiguous elements from the start of a sequence.</param>
    /// <param name="count">Indicates whether to get the total count when performing the underlying query.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithPaging(long? skip = null, long? take = null, bool count = false) => Configure(r => r.WithPaging(skip, take, count));

    /// <summary>
    /// Adds <see cref="QueryArgs"/> to the request URI query string.
    /// </summary>
    /// <param name="query">The <see cref="QueryArgs"/>.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithQuery(QueryArgs? query) => Configure(r => r.WithQuery(query));

    /// <summary>
    /// Adds query parameters to the request URI query string.
    /// </summary>
    /// <param name="filter">The basic dynamic <i>OData-like</i> <c>$filter</c> statement.</param>
    /// <param name="orderBy">The basic dynamic <i>OData-like</i> <c>$orderby</c> statement.</param>
    /// <param name="include">The list of <b>included</b> fields.</param>
    /// <param name="exclude">The list of <b>excluded</b> fields.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithQuery(string? filter = null, string? orderBy = null, IEnumerable<string>? include = null, IEnumerable<string>? exclude = null) => Configure(r => r.WithQuery(filter, orderBy, include, exclude));

    /// <summary>
    /// Adds <see cref="QueryArgs.IsIncludeText"/> to the request URI query string.
    /// </summary>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithIncludeText() => Configure(r => r.WithIncludeText());

    /// <summary>
    /// Adds <see cref="QueryArgs.IsIncludeInactive"/> to the request URI query string.
    /// </summary>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithIncludeInactive() => Configure(r => r.WithIncludeInactive());

    /// <summary>
    /// Adds <see cref="QueryArgs.IncludeFields"/> to the request URI query string.
    /// </summary>
    /// <param name="fields">The list of included fields.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithFields(params IEnumerable<string> fields) => Configure(r => r.WithFields(fields));

    /// <summary>
    /// Adds <see cref="QueryArgs.ExcludeFields"/> to the request URI query string.
    /// </summary>
    /// <param name="fields">The list of excluded fields.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithoutFields(params IEnumerable<string> fields) => Configure(r => r.WithoutFields(fields));

    /// <summary>
    /// Configures a <see cref="Result"/> to return when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The <see cref="Result"/> to return.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithStatusResult(HttpStatusCode statusCode, Result result)
    {
        _statusResults ??= [];
        _statusResults[statusCode] = (_, _) => Task.FromResult(result);
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The function returning the <see cref="Result"/>.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithStatusResult(HttpStatusCode statusCode, Func<Result> result)
    {
        result.ThrowIfNull();
        _statusResults ??= [];
        _statusResults[statusCode] = (_, _) => Task.FromResult(result());
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The function returning the <see cref="Result"/> for the response.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithStatusResult(HttpStatusCode statusCode, Func<HttpResponseMessage, Result> result)
    {
        result.ThrowIfNull();
        _statusResults ??= [];
        _statusResults[statusCode] = (response, _) => Task.FromResult(result(response));
        return this;
    }

    /// <summary>
    /// Configures an asynchronous <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>.
    /// </summary>
    /// <param name="statusCode">The HTTP status code to match.</param>
    /// <param name="result">The asynchronous function returning the <see cref="Result"/> for the response.</param>
    /// <returns>The <see cref="HttpRequestBuilder"/> instance to support fluent chaining.</returns>
    public HttpRequestBuilder WithStatusResult(HttpStatusCode statusCode, Func<HttpResponseMessage, CancellationToken, Task<Result>> result)
    {
        result.ThrowIfNull();
        _statusResults ??= [];
        _statusResults[statusCode] = result;
        return this;
    }

    /// <summary>
    /// Configures a <see cref="Result"/> to return when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Result)"/>.
    /// </summary>
    public HttpRequestBuilder WithErrorStatus(HttpStatusCode statusCode, Result result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{Result})"/>.
    /// </summary>
    public HttpRequestBuilder WithErrorStatus(HttpStatusCode statusCode, Func<Result> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures a <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{HttpResponseMessage, Result})"/>.
    /// </summary>
    public HttpRequestBuilder WithErrorStatus(HttpStatusCode statusCode, Func<HttpResponseMessage, Result> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Configures an asynchronous <see cref="Result"/> factory to invoke with the <see cref="HttpResponseMessage"/> when the response status matches the specified <paramref name="statusCode"/>. Alias for <see cref="WithStatusResult(HttpStatusCode, Func{HttpResponseMessage, CancellationToken, Task{Result}})"/>.
    /// </summary>
    public HttpRequestBuilder WithErrorStatus(HttpStatusCode statusCode, Func<HttpResponseMessage, CancellationToken, Task<Result>> result) => WithStatusResult(statusCode, result);

    /// <summary>
    /// Attempts to get a registered custom <see cref="Result"/> for the response's status code.
    /// </summary>
    internal async Task<Result?> TryGetStatusResultAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (_statusResults is not null && _statusResults.TryGetValue(response.StatusCode, out var handler))
            return await handler(response, cancellationToken).ConfigureAwait(false);

        return null;
    }

    /// <summary>
    /// Sends the HTTP request and returns the <see cref="HttpResponseMessage"/>. The caller is responsible for disposing the returned response.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public async Task<HttpResponseMessage> GetResponseAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(_method, _requestUri);
        if (_client.BaseAddress is not null && request.RequestUri?.IsAbsoluteUri != true)
            request.RequestUri = new Uri(_client.BaseAddress, _requestUri ?? string.Empty);

        _configureRequest?.Invoke(request);
        return await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and returns the <see cref="HttpResponseMessage"/>. The caller is responsible for disposing the returned response.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public Task<HttpResponseMessage> SendAsync(CancellationToken cancellationToken = default) => GetResponseAsync(cancellationToken);

    /// <summary>
    /// Sends the HTTP request and throws an exception where the response is not successful; disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The completed <see cref="Task"/>.</returns>
    public async Task ThrowOnErrorAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
        {
            customResult.ThrowOnError();
            return;
        }

        await response.ThrowOnErrorAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and converts the response into a <see cref="ProblemDetailsException"/> where not successful and problem+json; disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corresponding <see cref="ProblemDetailsException"/> or <see langword="null"/>.</returns>
    public async Task<ProblemDetailsException?> ToProblemDetailsAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
        {
            if (customResult.IsFailure && customResult.Error is ProblemDetailsException pde)
                return pde;

            return null;
        }

        return await response.ToProblemDetailsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and throws where considered a business exception; disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corresponding <see cref="ProblemDetailsException"/> or <see langword="null"/>.</returns>
    public async Task<ProblemDetailsException?> ThrowOnBusinessExceptionAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
        {
            if (customResult.IsFailure && customResult.Error is BusinessException)
                customResult.ThrowOnError();

            return null;
        }

        return await response.ThrowOnBusinessExceptionAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and converts the response into a <see cref="Result"/>; disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corresponding <see cref="Result"/>.</returns>
    public async Task<Result> ToResultAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
            return customResult;

        return await response.ToResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and converts the response into a <see cref="Result{T}"/> including the deserialized JSON response value; disposes the response upon completion.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    public async Task<Result<T>> ToResultAsync<T>(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
            return customResult.Bind<T>();

        return await response.ToResultAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and converts the response into a <see cref="Result{T}"/> including the deserialized JSON response value; disposes the response upon completion.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    public Task<Result<T>> ToResultAsync<T>(CancellationToken cancellationToken = default) => ToResultAsync<T>(null, cancellationToken);

    /// <summary>
    /// Sends the HTTP request and converts the response into a <see cref="Result{T}"/> including the deserialized JSON response value or <see langword="null"/>; disposes the response upon completion.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    public async Task<Result<T?>> ToResultOrDefaultAsync<T>(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
            return customResult.Bind<T?>();

        return await response.ToResultOrDefaultAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and converts the response into a <see cref="Result{T}"/> including the deserialized JSON response value or <see langword="null"/>; disposes the response upon completion.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    public Task<Result<T?>> ToResultOrDefaultAsync<T>(CancellationToken cancellationToken = default) => ToResultOrDefaultAsync<T>(null, cancellationToken);

    /// <summary>
    /// Sends the HTTP request and converts the response into a <see cref="Result{T}"/> including the response content as a string; disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The corresponding <see cref="Result{T}"/>.</returns>
    public async Task<Result<string?>> ToStringResultAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
            return customResult.Bind<string?>();

        return await response.ToStringResultAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and gets the deserialized JSON response value where successful; disposes the response upon completion.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response value.</returns>
    public async Task<T> GetValueAsync<T>(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
        {
            customResult.ThrowOnError();
            return customResult.Bind<T>().Value;
        }

        return await response.GetValueAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and gets the deserialized JSON response value where successful; disposes the response upon completion.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response value.</returns>
    public Task<T> GetValueAsync<T>(CancellationToken cancellationToken = default) => GetValueAsync<T>(null, cancellationToken);

    /// <summary>
    /// Sends the HTTP request and gets the deserialized JSON response value or <see langword="null"/> where successful; disposes the response upon completion.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="jsonSerializerOptions">The optional <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response value or <see langword="null"/>.</returns>
    public async Task<T?> GetValueOrDefaultAsync<T>(JsonSerializerOptions? jsonSerializerOptions, CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
        {
            customResult.ThrowOnError();
            return customResult.Bind<T?>().Value;
        }

        return await response.GetValueOrDefaultAsync<T>(jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends the HTTP request and gets the deserialized JSON response value or <see langword="null"/> where successful; disposes the response upon completion.
    /// </summary>
    /// <typeparam name="T">The response value <see cref="Type"/>.</typeparam>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The deserialized response value or <see langword="null"/>.</returns>
    public Task<T?> GetValueOrDefaultAsync<T>(CancellationToken cancellationToken = default) => GetValueOrDefaultAsync<T>(null, cancellationToken);

    /// <summary>
    /// Sends the HTTP request and gets the response content as a string where successful; disposes the response upon completion.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response content as a string or <see langword="null"/>.</returns>
    public async Task<string?> GetStringAsync(CancellationToken cancellationToken = default)
    {
        using var response = await GetResponseAsync(cancellationToken).ConfigureAwait(false);
        if (await TryGetStatusResultAsync(response, cancellationToken).ConfigureAwait(false) is { } customResult)
        {
            customResult.ThrowOnError();
            return customResult.Bind<string?>().Value;
        }

        return await response.GetStringAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Configures validation of a successful, non-null deserialized HTTP response value.
    /// </summary>
    /// <typeparam name="T">The response value type.</typeparam>
    /// <param name="validator">The response value validator.</param>
    /// <returns>A typed wrapper that validates when a value is consumed.</returns>
    public ValidatedHttpRequestBuilder<T> WithValidator<T>(Validation.IValidator<T> validator) => new(this, validator);
}
