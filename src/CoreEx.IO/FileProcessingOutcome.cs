namespace CoreEx.IO;

/// <summary>Identifies a returned processing outcome; operational faults and cancellation propagate instead.</summary>
public enum FileProcessingOutcome
{
    /// <summary>Validation completed successfully and execution was not requested.</summary>
    Validated,

    /// <summary>Validation and all requested business execution completed successfully.</summary>
    Executed,

    /// <summary>Content was rejected, with no business execution performed.</summary>
    Rejected,

    /// <summary>A business execution returned a failed Result; earlier units may have committed.</summary>
    ExecutionFailed
}
