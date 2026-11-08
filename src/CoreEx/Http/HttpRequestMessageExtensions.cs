namespace CoreEx.Http;

/// <summary>Configures per-request correlation independently of the ambient execution.</summary>
public static class HttpRequestMessageExtensions
{
    /// <summary>Overrides correlation, or explicitly suppresses it when null.</summary>
    /// <param name="request">The request.</param>
    /// <param name="correlationId">The valid identifier or null to suppress.</param>
    /// <returns>The request.</returns>
    public static HttpRequestMessage WithCorrelationId(this HttpRequestMessage request, string? correlationId)
    {
        request.ThrowIfNull();
        Correlation.Validate(correlationId);
        request.Headers.Remove(HttpNames.CorrelationIdHeaderName);
        request.Options.Set(CorrelationIdHandler.SuppressCorrelationId, correlationId is null);
        if (correlationId is not null)
            request.Headers.Add(HttpNames.CorrelationIdHeaderName, correlationId);

        return request;
    }
}
