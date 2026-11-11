namespace CoreEx.IO;

/// <summary>Identifies whether processing succeeded, found invalid content, or encountered a returned business execution failure.</summary>
public enum FileProcessingOutcome
{
    /// <summary>Validation and all requested processing completed without content or business execution errors.</summary>
    Succeeded,

    /// <summary>Content validation failed; progress may include reports or writes completed before the failure.</summary>
    ContentFailed,

    /// <summary>A business execution returned a failed Result; earlier units may have committed.</summary>
    ExecutionFailed
}
