namespace CoreEx;

public static partial class Extensions
{
    /// <summary>
    /// Creates a single-use <see cref="HttpRequestBuilder"/> for the specified method and URI; no request is sent until a terminal method is called.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>The request builder.</returns>
    public static HttpRequestBuilder Send(this HttpClient client, HttpMethod method, string? requestUri)
        => Send(client, method, requestUri, null);

    /// <summary>
    /// Creates a single-use <see cref="HttpRequestBuilder"/> with optional low-level request configuration.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="configureRequest">An optional action to configure the request before sending.</param>
    /// <returns>The request builder.</returns>
    public static HttpRequestBuilder Send(this HttpClient client, HttpMethod method, string? requestUri, Action<HttpRequestMessage>? configureRequest)
        => new(client, method, requestUri, configureRequest);

    /// <summary>Creates a single-use GET request builder without sending the request.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>The request builder.</returns>
    public static HttpRequestBuilder Get(this HttpClient client, string? requestUri) => client.Send(HttpMethod.Get, requestUri);

    /// <summary>Creates a single-use POST request builder without sending the request.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>The request builder.</returns>
    public static HttpRequestBuilder Post(this HttpClient client, string? requestUri) => client.Send(HttpMethod.Post, requestUri);

    /// <summary>Creates a single-use PUT request builder without sending the request.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>The request builder.</returns>
    public static HttpRequestBuilder Put(this HttpClient client, string? requestUri) => client.Send(HttpMethod.Put, requestUri);

    /// <summary>Creates a single-use PATCH request builder without choosing a body format or sending the request.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>The request builder.</returns>
    public static HttpRequestBuilder Patch(this HttpClient client, string? requestUri) => client.Send(HttpMethod.Patch, requestUri);

    /// <summary>Creates a single-use DELETE request builder without sending the request.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <returns>The request builder.</returns>
    public static HttpRequestBuilder Delete(this HttpClient client, string? requestUri) => client.Send(HttpMethod.Delete, requestUri);

    /// <summary>Sends a request and returns the response, which the caller must dispose.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string? requestUri, CancellationToken cancellationToken = default)
        => SendAsync(client, method, requestUri, null, cancellationToken);

    /// <summary>Sends a request with optional configuration and returns the response, which the caller must dispose.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="method">The HTTP method.</param>
    /// <param name="requestUri">The request URI.</param>
    /// <param name="configureRequest">An optional action to configure the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The response.</returns>
    public static Task<HttpResponseMessage> SendAsync(this HttpClient client, HttpMethod method, string? requestUri, Action<HttpRequestMessage>? configureRequest, CancellationToken cancellationToken = default)
        => Send(client, method, requestUri, configureRequest).GetResponseAsync(cancellationToken);
}
