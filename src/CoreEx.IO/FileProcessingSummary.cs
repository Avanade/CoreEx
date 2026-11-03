namespace CoreEx.IO;

/// <summary>Describes completed processing or an explicit business execution failure without diagnostic history.</summary>
public sealed class FileProcessingSummary
{
    /// <summary>Initializes an outcome snapshot and rejects contradictory success/failure states.</summary>
    /// <param name="context">The file context supplying immutable identity and processing mode.</param>
    /// <param name="outcome">The returned outcome; operational faults and cancellation are not summaries.</param>
    /// <param name="progress">The final validation and execution counts.</param>
    /// <param name="validationCompleted">Whether validation reached EOF and completed its applicable checks, rather than stopping early.</param>
    /// <param name="executionResult">The original failed execution Result for ExecutionFailed; success otherwise.</param>
    public FileProcessingSummary(FileProcessingContext context, FileProcessingOutcome outcome, FileProcessingProgress progress, bool validationCompleted, Result executionResult = default)
    {
        context.ThrowIfNull();
        progress.ThrowIfNull();
        if (!Enum.IsDefined(outcome))
            throw new ArgumentOutOfRangeException(nameof(outcome));

        if (outcome != FileProcessingOutcome.Rejected && (!validationCompleted || progress.ErrorCount != 0 || progress.InvalidRecords != 0))
            throw new ArgumentException("Acceptance requires completed validation with no content errors.", nameof(outcome));

        if (outcome == FileProcessingOutcome.Rejected && (progress.ErrorCount == 0 || progress.UnitsAttempted != 0))
            throw new ArgumentException("Rejection requires a content error and zero execution.", nameof(outcome));

        if ((outcome == FileProcessingOutcome.Validated && context.Mode != FileProcessingMode.ValidateOnly)
            || (outcome is FileProcessingOutcome.Executed or FileProcessingOutcome.ExecutionFailed && context.Mode != FileProcessingMode.ValidateThenExecute))
            throw new ArgumentException("The outcome must match the requested processing mode.", nameof(outcome));

        if (outcome == FileProcessingOutcome.Validated && progress.UnitsAttempted != 0)
            throw new ArgumentException("Validation-only processing cannot execute units.", nameof(progress));

        if (outcome == FileProcessingOutcome.Executed && progress.UnitsExecuted != progress.UnitsValidated)
            throw new ArgumentException("Successful execution must complete every accepted unit.", nameof(progress));

        if ((outcome == FileProcessingOutcome.ExecutionFailed) != executionResult.IsFailure)
            throw new ArgumentException("Only execution failure carries a failed execution Result.", nameof(executionResult));

        if (outcome == FileProcessingOutcome.ExecutionFailed && progress.UnitsAttempted - progress.UnitsExecuted != 1)
            throw new ArgumentException("Execution must stop at the first failed unit.", nameof(progress));

        Identity = context.Identity;
        Mode = context.Mode;
        Outcome = outcome;
        Progress = progress;
        ValidationCompleted = validationCompleted;
        ExecutionResult = executionResult;
    }

    /// <summary>Gets the immutable processing identity, not the mutable context.</summary>
    public FileProcessingIdentity Identity { get; }

    /// <summary>Gets the requested processing mode.</summary>
    public FileProcessingMode Mode { get; }

    /// <summary>Gets the explicit processing outcome.</summary>
    public FileProcessingOutcome Outcome { get; }

    /// <summary>Gets the final progress snapshot.</summary>
    public FileProcessingProgress Progress { get; }

    /// <summary>Gets whether validation completed through EOF and its applicable checks.</summary>
    public bool ValidationCompleted { get; }

    /// <summary>Gets whether the requested processing mode completed successfully.</summary>
    public bool IsSuccess => Outcome is FileProcessingOutcome.Validated or FileProcessingOutcome.Executed;

    /// <summary>Gets the original execution Result; failure is available without throwing or losing progress.</summary>
    public Result ExecutionResult { get; }
}
