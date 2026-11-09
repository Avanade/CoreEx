namespace CoreEx.Http;

/// <summary>Propagates optional ambient correlation on each outgoing HTTP request.</summary>
/// <param name="configuration">The optional host configuration; otherwise, the current execution's configuration is used.</param>
/// <remarks>Explicit request headers win. Factory-pooled handlers never capture a scoped execution context. Automatic handling is skipped when <c>CoreEx:Correlation:Enabled</c> is false.</remarks>
public sealed class CorrelationIdHandler(IConfiguration? configuration = null) : DelegatingHandler
{
    /// <summary>Gets the option which suppresses correlation for a single request.</summary>
    public static HttpRequestOptionsKey<bool> SuppressCorrelationId { get; } = new("CoreEx.SuppressCorrelationId");

    /// <inheritdoc/>
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!Correlation.IsEnabled(configuration))
            return base.SendAsync(request, cancellationToken);

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
