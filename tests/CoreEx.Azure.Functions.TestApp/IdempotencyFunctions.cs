namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Demonstrates explicit idempotency around an ordinary WebApi operation.
/// </summary>
/// <param name="webApi">The HTTP operation helper.</param>
/// <param name="service">The observable application service.</param>
public sealed class IdempotencyFunctions(WebApi webApi, IdempotencyOperationService service)
{
    /// <summary>
    /// Creates an observable result with optional idempotency.
    /// </summary>
    [Function(nameof(CreateIdempotent))]
    public Task<IActionResult> CreateIdempotent([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "idempotency/{id}")] HttpRequest request, string id, CancellationToken cancellationToken)
        => CreateAsync(request, id, false, cancellationToken);

    /// <summary>
    /// Creates an observable result requiring an idempotency key.
    /// </summary>
    [Function(nameof(CreateIdempotentRequired))]
    public Task<IActionResult> CreateIdempotentRequired([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "idempotency-required/{id}")] HttpRequest request, string id, CancellationToken cancellationToken)
        => CreateAsync(request, id, true, cancellationToken);

    /// <summary>
    /// Reads test-host application execution counts.
    /// </summary>
    [Function(nameof(ReadIdempotencyCount))]
    public Task<IActionResult> ReadIdempotencyCount([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "idempotency-count/{id}")] HttpRequest request, string id, CancellationToken cancellationToken)
        => webApi.GetAsync(request, (_, _) => Task.FromResult(service.GetCount(id)), cancellationToken: cancellationToken);

    /// <summary>
    /// Releases a test-host application operation to complete the concurrency proof.
    /// </summary>
    [Function(nameof(ReleaseIdempotencyOperation))]
    public Task<IActionResult> ReleaseIdempotencyOperation([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "idempotency-release/{id}")] HttpRequest request, string id, CancellationToken cancellationToken)
        => webApi.PostAsync(request, (_, _) =>
        {
            service.Release(id);
            return Task.CompletedTask;
        }, cancellationToken: cancellationToken);

    /// <summary>
    /// Defers both request parsing and application execution until after idempotency checks.
    /// </summary>
    private Task<IActionResult> CreateAsync(HttpRequest request, string id, bool isRequired, CancellationToken cancellationToken)
        => webApi.FunctionWithIdempotencyAsync(request, ct => webApi.PostAsync<string, IdempotencyResponse>(request, (ro, token) =>
        {
            ro.WithLocationUri(value => new Uri($"/api/idempotency/{Uri.EscapeDataString(value.Id)}", UriKind.Relative));
            request.HttpContext.Response.Cookies.Append("test-idempotency", id);
            return service.ExecuteAsync(id, ro.Value.Required(), token);
        }, cancellationToken: ct), isRequired, cancellationToken);
}
