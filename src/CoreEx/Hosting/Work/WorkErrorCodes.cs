namespace CoreEx.Hosting.Work;

/// <summary>
/// Defines standard work error codes.
/// </summary>
public static class WorkErrorCodes
{
    /// <summary>
    /// The work did not start before its start deadline.
    /// </summary>
    public const string NotStarted = "not-started";

    /// <summary>
    /// The operation ended without a known outcome.
    /// </summary>
    public const string OutcomeUnknown = "outcome-unknown";

    /// <summary>
    /// The work was canceled.
    /// </summary>
    public const string Canceled = "canceled";

    /// <summary>
    /// The work failed during execution.
    /// </summary>
    public const string ExecutionFailed = "execution-failed";

    /// <summary>
    /// The maximum execution-attempt count was reached.
    /// </summary>
    public const string MaxAttempts = "max-attempts";
}
