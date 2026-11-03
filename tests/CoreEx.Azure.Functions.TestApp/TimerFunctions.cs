namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Demonstrates invocation context outside HTTP without starting another receiver.
/// </summary>
public sealed class TimerFunctions(InvocationService service)
{
    private readonly InvocationService _service = service;

    /// <summary>
    /// Reads application state from a scheduled invocation.
    /// </summary>
    [Function(nameof(Scheduled))]
    public async Task Scheduled([TimerTrigger("0 0 0 1 1 *")] TimerInfo timer, CancellationToken cancellationToken)
    {
        _ = timer;
        await _service.ReadAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Leaves non-HTTP failures visible to the Functions runtime.
    /// </summary>
    [Function(nameof(FailingScheduled))]
    public Task FailingScheduled([TimerTrigger("0 0 0 1 1 *")] TimerInfo timer, CancellationToken cancellationToken)
        => throw new InvalidOperationException("Non-HTTP failure must propagate.");
}
