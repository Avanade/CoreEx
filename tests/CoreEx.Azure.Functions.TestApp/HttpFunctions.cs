namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Demonstrates HTTP entry points using the existing CoreEx WebApi.
/// </summary>
public sealed class HttpFunctions(WebApi webApi, InvocationService service)
{
    private readonly WebApi _webApi = webApi;
    private readonly InvocationService _service = service;

    /// <summary>
    /// Reads and removes a test-only Service Bus runtime trace observation.
    /// </summary>
    [Function(nameof(ReadTraceObservation))]
    public Task<IActionResult> ReadTraceObservation([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "trace/{messageId}")] HttpRequest request, string messageId, CancellationToken cancellationToken)
        => _webApi.GetAsync(request, (_, _) => Task.FromResult(ServiceBusTraceProbe.Take(messageId)), cancellationToken: cancellationToken);

    /// <summary>
    /// Reads invocation-scoped application state.
    /// </summary>
    [Function(nameof(ReadContext))]
    public Task<IActionResult> ReadContext([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "context")] HttpRequest request, CancellationToken cancellationToken)
        => _webApi.GetAsync(request, (_, ct) => _service.ReadAsync(ct), cancellationToken: cancellationToken);

    /// <summary>
    /// Demonstrates request parsing and a created response.
    /// </summary>
    [Function(nameof(Create))]
    public Task<IActionResult> Create([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "values")] HttpRequest request, CancellationToken cancellationToken)
        => _webApi.PostAsync<string, string>(request, (ro, ct) =>
        {
            ct.ThrowIfCancellationRequested();
            ro.WithLocationUri(value => new Uri($"/api/values/{Uri.EscapeDataString(value)}", UriKind.Relative));
            return Task.FromResult(ro.Value);
        }, cancellationToken: cancellationToken);

    /// <summary>
    /// Demonstrates known and unexpected application failures.
    /// </summary>
    [Function(nameof(Failure))]
    public Task<IActionResult> Failure([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "failure/{kind}")] HttpRequest request, string kind, CancellationToken cancellationToken)
        => _webApi.GetAsync<string>(request, (_, _) => throw kind switch
        {
            "known" => new NotFoundException("The requested value does not exist."),
            _ => new InvalidOperationException("Internal diagnostic must not be exposed.")
        }, cancellationToken: cancellationToken);

    /// <summary>
    /// Demonstrates informational response headers.
    /// </summary>
    [Function(nameof(Messages))]
    public Task<IActionResult> Messages([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "messages")] HttpRequest request, CancellationToken cancellationToken)
        => _webApi.GetAsync(request, (_, _) =>
        {
            CoreEx.ExecutionContext.Current.AddInfoMessage("Processed.");
            return Task.FromResult("done");
        }, cancellationToken: cancellationToken);
}
