namespace CoreEx;

public static partial class Extensions
{
    /// <summary>
    /// Creates an <see cref="HttpRequestBuilder"/> for the specified <paramref name="method"/> and <paramref name="requestUri"/>.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>An <see cref="HttpRequestBuilder"/>.</returns>
    public static HttpRequestBuilder Send(this HttpClient client, HttpMethod method, string? requestUri)
        => Send(client, method, requestUri, null);

    /// <summary>
    /// Creates an <see cref="HttpRequestBuilder"/> for the specified <paramref name="method"/> and <paramref name="requestUri"/>.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
    /// <returns>An <see cref="HttpRequestBuilder"/>.</returns>
    public static HttpRequestBuilder Send(this HttpClient client, HttpMethod method, string? requestUri, Action<HttpRequestMessage>? configureRequest)
        => new(client, method, requestUri, configureRequest);

    /// <summary>
    /// Creates an <see cref="HttpRequestBuilder"/> for the specified <paramref name="method"/> and <paramref name="requestUri"/>, serializing the specified <paramref name="value"/> to JSON.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <returns>An <see cref="HttpRequestBuilder"/>.</returns>
    public static HttpRequestBuilder SendValue<T>(this HttpClient client, HttpMethod method, string? requestUri, T value)
        => SendValue(client, method, requestUri, value, null, null, null);

    /// <summary>
    /// Creates an <see cref="HttpRequestBuilder"/> for the specified <paramref name="method"/> and <paramref name="requestUri"/>, serializing the specified <paramref name="value"/> to JSON.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
    /// <returns>An <see cref="HttpRequestBuilder"/>.</returns>
    public static HttpRequestBuilder SendValue<T>(this HttpClient client, HttpMethod method, string? requestUri, T value, Action<HttpRequestMessage>? configureRequest)
        => SendValue(client, method, requestUri, value, null, configureRequest, null);

    /// <summary>
    /// Creates an <see cref="HttpRequestBuilder"/> for the specified <paramref name="method"/> and <paramref name="requestUri"/>, serializing the specified <paramref name="value"/> to JSON.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
    /// <returns>An <see cref="HttpRequestBuilder"/>.</returns>
    public static HttpRequestBuilder SendValue<T>(this HttpClient client, HttpMethod method, string? requestUri, T value, string? mediaType, Action<HttpRequestMessage>? configureRequest)
        => SendValue(client, method, requestUri, value, mediaType, configureRequest, null);

    /// <summary>
    /// Creates an <see cref="HttpRequestBuilder"/> for the specified <paramref name="method"/> and <paramref name="requestUri"/>, serializing the specified <paramref name="value"/> to JSON.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
    /// <param name="jsonSerializerOptions">An optional <see cref="JsonSerializerOptions"/> to use when serializing the value to JSON; defaults to <see cref="JsonDefaults.SerializerOptions"/> where not specified.</param>
    /// <returns>An <see cref="HttpRequestBuilder"/>.</returns>
    public static HttpRequestBuilder SendValue<T>(this HttpClient client, HttpMethod method, string? requestUri, T value, string? mediaType = null, Action<HttpRequestMessage>? configureRequest = null, JsonSerializerOptions? jsonSerializerOptions = null)
        => Send(client, method, requestUri, request =>
        {
            mediaType ??= MediaTypeNames.Application.Json;
            request.Content = JsonContent.Create(value, new MediaTypeHeaderValue(mediaType), options: jsonSerializerOptions ?? JsonDefaults.SerializerOptions);
            configureRequest?.Invoke(request);
        });

    /// <summary>
    /// Sends an HTTP request with the specified <paramref name="method"/> and <paramref name="requestUri"/> and returns the response.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string? requestUri, CancellationToken cancellationToken = default)
        => SendAsync(client, method, requestUri, null, cancellationToken);

    /// <summary>
    /// Sends an HTTP request with the specified <paramref name="method"/> and <paramref name="requestUri"/> and returns the response.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string? requestUri, Action<HttpRequestMessage>? configureRequest, CancellationToken cancellationToken = default)
        => Send(client, method, requestUri, configureRequest).GetResponseAsync(cancellationToken);

    /// <summary>
    /// Sends an HTTP request with the specified <paramref name="method"/> and <paramref name="requestUri"/> and returns the response, serializing the specified <paramref name="value"/> to JSON.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public static Task<HttpResponseMessage> SendValueAsync<T>(this HttpClient client, HttpMethod method, string? requestUri, T value, CancellationToken cancellationToken = default)
        => SendValueAsync(client, method, requestUri, value, null, null, null, cancellationToken);

    /// <summary>
    /// Sends an HTTP request with the specified <paramref name="method"/> and <paramref name="requestUri"/> and returns the response, serializing the specified <paramref name="value"/> to JSON.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public static Task<HttpResponseMessage> SendValueAsync<T>(this HttpClient client, HttpMethod method, string? requestUri, T value, Action<HttpRequestMessage>? configureRequest, CancellationToken cancellationToken = default)
        => SendValueAsync(client, method, requestUri, value, null, configureRequest, null, cancellationToken);

    /// <summary>
    /// Sends an HTTP request with the specified <paramref name="method"/> and <paramref name="requestUri"/> and returns the response, serializing the specified <paramref name="value"/> to JSON.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public static Task<HttpResponseMessage> SendValueAsync<T>(this HttpClient client, HttpMethod method, string? requestUri, T value, string? mediaType, Action<HttpRequestMessage>? configureRequest, CancellationToken cancellationToken = default)
        => SendValueAsync(client, method, requestUri, value, mediaType, configureRequest, null, cancellationToken);

    /// <summary>
    /// Sends an HTTP request with the specified <paramref name="method"/> and <paramref name="requestUri"/> and returns the response, serializing the specified <paramref name="value"/> to JSON.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    /// <param name="client">The <see cref="HttpClient"/>.</param>
    /// <param name="method">The <see cref="HttpMethod"/>.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="value">The value to serialize to JSON.</param>
    /// <param name="mediaType">The media type.</param>
    /// <param name="configureRequest">An optional action to configure the <see cref="HttpRequestMessage"/>.</param>
    /// <param name="jsonSerializerOptions">An optional <see cref="JsonSerializerOptions"/> to use when serializing the value to JSON; defaults to <see cref="JsonDefaults.SerializerOptions"/> where not specified.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
    public static Task<HttpResponseMessage> SendValueAsync<T>(this HttpClient client, HttpMethod method, string? requestUri, T value, string? mediaType, Action<HttpRequestMessage>? configureRequest = null, JsonSerializerOptions? jsonSerializerOptions = null, CancellationToken cancellationToken = default)
        => SendValue(client, method, requestUri, value, mediaType, configureRequest, jsonSerializerOptions).GetResponseAsync(cancellationToken);
}
