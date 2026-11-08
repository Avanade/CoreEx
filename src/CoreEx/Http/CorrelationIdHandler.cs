namespace CoreEx.Http;

/// <summary>Propagates optional ambient correlation on each outgoing HTTP request.</summary>
/// <remarks>Explicit request headers win. Factory-pooled handlers never capture a scoped execution context.</remarks>
public sealed class CorrelationIdHandler : DelegatingHandler
{
    /// <summary>Gets the option which suppresses correlation for a single request.</summary>
    public static HttpRequestOptionsKey<bool> SuppressCorrelationId { get; } = new("CoreEx.SuppressCorrelationId");

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Options.TryGetValue(SuppressCorrelationId, out var suppress) && suppress)
            request.Headers.Remove(HttpNames.CorrelationIdHeaderName);
        else if (request.Headers.TryGetValues(HttpNames.CorrelationIdHeaderName, out var values))
        {
            var identifiers = values.ToArray();
            if (identifiers.Length != 1 || identifiers[0] is null || !Correlation.IsValid(identifiers[0]))
                throw new ArgumentException("The outgoing correlation header must contain one valid identifier.", nameof(request));
        }
        else if (ExecutionContext.TryGetCurrent(out var ec) && ec.CorrelationId is not null)
            request.Headers.Add(HttpNames.CorrelationIdHeaderName, ec.CorrelationId);

        return base.SendAsync(request, cancellationToken);
    }
}
