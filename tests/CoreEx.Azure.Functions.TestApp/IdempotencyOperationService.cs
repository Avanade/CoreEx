namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Provides observable test-host operations for response replay and deterministic overlap.
/// </summary>
public sealed class IdempotencyOperationService
{
    private readonly ConcurrentDictionary<string, Operation> _operations = new();

    /// <summary>
    /// Gets the number of application executions for an operation.
    /// </summary>
    public int GetCount(string id) => _operations.TryGetValue(id, out var operation) ? Volatile.Read(ref operation.Count) : 0;

    /// <summary>
    /// Releases an explicitly blocked test operation.
    /// </summary>
    public void Release(string id) => _operations.GetOrAdd(id, _ => new()).Release.TrySetResult();

    /// <summary>
    /// Executes a small operation with observable application work and optional controlled failure or overlap.
    /// </summary>
    public async Task<IdempotencyResponse> ExecuteAsync(string id, string name, CancellationToken cancellationToken = default)
    {
        var operation = _operations.GetOrAdd(id, _ => new());
        var count = Interlocked.Increment(ref operation.Count);
        if (name == "wait")
            await operation.Release.Task.WaitAsync(cancellationToken).ConfigureAwait(false);

        if (name == "fail-once" && count == 1)
            throw new InvalidOperationException("Internal idempotency diagnostic.");

        CoreEx.ExecutionContext.Current.AddInfoMessage("Idempotent operation processed.");
        return new(id, name, count);
    }

    /// <summary>
    /// Stores test-host execution state.
    /// </summary>
    private sealed class Operation
    {
        public int Count;
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }
}

/// <summary>
/// Describes an observable application execution.
/// </summary>
/// <param name="Id">The operation identifier.</param>
/// <param name="Name">The request value.</param>
/// <param name="Executions">The application execution count.</param>
public sealed record IdempotencyResponse(string Id, string Name, int Executions);
