namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Demonstrates scoped application behavior without a persistence dependency.
/// </summary>
[ScopedService]
public sealed class InvocationService(CoreEx.ExecutionContext executionContext)
{
    private readonly CoreEx.ExecutionContext _executionContext = executionContext;

    /// <summary>
    /// Reads the invocation context across an asynchronous boundary.
    /// </summary>
    public async Task<object> ReadAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        return new
        {
            InvocationId = _executionContext.Attributes["invocation-id"],
            SameContext = ReferenceEquals(_executionContext, CoreEx.ExecutionContext.Current),
            SameService = ReferenceEquals(this, CoreEx.ExecutionContext.GetRequiredService<InvocationService>())
        };
    }
}
