namespace CoreEx.Http;

/// <summary>
/// Provides an <see cref="HttpClient"/> <see cref="DelegatingHandler"/> that adds an idempotency-key (see <see cref="HttpNames.IdempotencyKeyHeaderName"/>) to outgoing HTTP requests.
/// </summary>
/// <remarks>Only <see cref="HttpMethod.Post"/> requests are supported by default; see <see cref="SupportedMethods"/>.
/// <para>It is expected that the <see cref="HttpMethod.Put"/> and <see cref="HttpMethod.Patch"/> endpoints being consumed support ETag's (optimistic concurrency) and as such this functionality will ensure idempotency.
/// Otherwise, the <see cref="HttpMethod.Get"/> and <see cref="HttpMethod.Delete"/> are assumed to be idempotent and are excluded. Adjust the <see cref="SupportedMethods"/> to support the given use-case.</para>
/// <para>Where an <see cref="HttpRequestMessage"/> already has the idempotency-key (<see cref="HeaderName"/>) defined this will be respected (i.e. will not be overridden).</para></remarks>
public sealed class IdempotencyKeyHandler : DelegatingHandler
{
    /// <summary>
    /// Gets or sets the supported HTTP methods for adding an idempotency key.
    /// </summary>
    /// <remarks>Defaults to <see cref="HttpMethod.Post"/> only.</remarks>
    public HttpMethod[] SupportedMethods { get; set; } = [HttpMethod.Post];

    /// <summary>
    /// Gets or sets the header name to use for the idempotency key.
    /// </summary>
    /// <remarks>Defaults to <see cref="HttpNames.IdempotencyKeyHeaderName"/>.</remarks>
    public string HeaderName { get; set => field = value.ThrowIfNullOrEmpty(); } = HttpNames.IdempotencyKeyHeaderName;

    /// <summary>
    /// Gets or sets the key generator function to use for generating the idempotency key value.
    /// </summary>
    /// <remarks>Defaults to generating a new <see cref="Guid"/> string.</remarks>
    public Func<string> KeyGenerator { get; set => field = value.ThrowIfNull(); } = () => Guid.NewGuid().ToString();

    /// <inheritdoc/>
    protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Only applicable for supported methods.
        if (SupportedMethods.Contains(request.Method))
        {
            // Respect existing idempotency key value.
            if (!request.Headers.Contains(HeaderName))
                request.Headers.Add(HeaderName, KeyGenerator());
        }

        // Continue processing; send it baby!
        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }
}