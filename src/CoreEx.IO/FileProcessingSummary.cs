namespace CoreEx.IO;

/// <summary>Describes the mode-independent result of processing without retaining diagnostic history.</summary>
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

        var executionMode = context.Mode is FileProcessingMode.ValidateThenExecute or FileProcessingMode.ValidateAndExecute;
        var singlePassExecution = context.Mode == FileProcessingMode.ValidateAndExecute;
        var executionFailure = outcome == FileProcessingOutcome.ExecutionFailed;

        if (outcome == FileProcessingOutcome.Succeeded
            && (!validationCompleted || progress.ErrorCount != 0 || progress.InvalidRecords != 0 || progress.InvalidUnits != 0 || progress.UnitsValidated != progress.UnitsRead))
            throw new ArgumentException("Success requires completed validation with no content errors.", nameof(outcome));

        if (outcome == FileProcessingOutcome.ContentFailed && (progress.ErrorCount == 0 || executionResult.IsFailure))
            throw new ArgumentException("Content failure requires a content error and cannot carry a failed execution Result.", nameof(outcome));

        if (outcome == FileProcessingOutcome.ContentFailed && !singlePassExecution && progress.UnitsAttempted != 0)
            throw new ArgumentException("Content failure cannot include execution in validation-only or two-pass mode.", nameof(progress));

        if (outcome == FileProcessingOutcome.ExecutionFailed && (!executionMode || !executionResult.IsFailure))
            throw new ArgumentException("Execution failure requires an execution mode and a failed execution Result.", nameof(outcome));

        if (executionFailure && progress.UnitsAttempted - progress.UnitsExecuted != 1)
            throw new ArgumentException("Execution must stop at the first failed unit.", nameof(progress));

        if (executionFailure && !singlePassExecution && (!validationCompleted || progress.ErrorCount != 0 || progress.InvalidRecords != 0 || progress.InvalidUnits != 0))
            throw new ArgumentException("Two-pass execution failure requires completed, error-free validation.", nameof(outcome));

        if (!executionMode && progress.UnitsAttempted != 0)
            throw new ArgumentException("Only execution modes can attempt business units.", nameof(progress));

        if (outcome == FileProcessingOutcome.Succeeded && executionMode && progress.UnitsExecuted != progress.UnitsValidated)
            throw new ArgumentException("Successful execution must complete every accepted unit.", nameof(progress));

        if (executionFailure != executionResult.IsFailure)
            throw new ArgumentException("Only execution failure carries a failed execution Result.", nameof(executionResult));

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

    /// <summary>Gets whether processing completed without content or business execution errors.</summary>
    public bool IsSuccess => Outcome == FileProcessingOutcome.Succeeded;

    /// <summary>Gets the original execution Result; failure is available without throwing or losing progress.</summary>
    public Result ExecutionResult { get; }
}
