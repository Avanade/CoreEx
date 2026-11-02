namespace CoreEx.Azure.Functions.TestApp;

/// <summary>
/// Provides deterministic application outcomes for the executable settlement examples.
/// </summary>
[ScopedService]
public sealed class MessageService
{
    /// <summary>
    /// Processes the requested outcome across an asynchronous boundary.
    /// </summary>
    /// <param name="outcome">The example outcome.</param>
    /// <param name="cancellationToken">The invocation cancellation token.</param>
    /// <returns>The processing result.</returns>
    public async Task<Result> ProcessAsync(string outcome, CancellationToken cancellationToken = default)
    {
        await Task.Delay(1, cancellationToken).ConfigureAwait(false);
        return outcome switch
        {
            "success" => Result.Success,
            "handled" => Result.Fail(new BusinessException("The command is no longer applicable.")),
            "retry" => Result.Fail(new TimeoutException("The dependency is temporarily unavailable.")),
            "reject" => Result.Fail(new ValidationException("The command is invalid.")),
            "catastrophic" => Result.Fail(new NotSupportedException("Processing cannot continue.")),
            _ => throw new InvalidOperationException("Unhandled application failure.")
        };
    }
}
