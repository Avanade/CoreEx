namespace CoreEx.IO;

/// <summary>Provides an immutable progress snapshot without retaining input models or diagnostics.</summary>
public sealed class FileProcessingProgress
{
    /// <summary>Initializes nonnegative processing counts; replay does not duplicate validation counters.</summary>
    /// <param name="bytesRead">The bytes consumed during the validation pass, including syntax and envelope records.</param>
    /// <param name="recordsRead">The physical data/envelope records returned during validation, including malformed records; excludes column-name headers and skipped blank/comment lines.</param>
    /// <param name="invalidRecords">The distinct physical records known to fail mapping, field, or structural checks; group/file diagnostics do not invent failed records.</param>
    /// <param name="unitsValidated">The accepted flat rows or root groups that passed unit validation; later file errors may still reject all execution.</param>
    /// <param name="unitsAttempted">The execution hooks entered, including a hook that failed; excludes replay reads before a hook.</param>
    /// <param name="unitsExecuted">The execution hooks that returned success, not a guarantee of transaction commits or exactly-once effects.</param>
    /// <param name="errorCount">The error diagnostics emitted during validation.</param>
    /// <param name="warningCount">The warning diagnostics emitted during validation.</param>
    /// <param name="informationCount">The informational diagnostics emitted during validation.</param>
    public FileProcessingProgress(long bytesRead = 0, long recordsRead = 0, long invalidRecords = 0, long unitsValidated = 0, long unitsAttempted = 0, long unitsExecuted = 0, long errorCount = 0, long warningCount = 0, long informationCount = 0)
    {
        bytesRead.ThrowIfLessThanZero();
        recordsRead.ThrowIfLessThanZero();
        invalidRecords.ThrowIfLessThanZero();
        unitsValidated.ThrowIfLessThanZero();
        unitsAttempted.ThrowIfLessThanZero();
        unitsExecuted.ThrowIfLessThanZero();
        errorCount.ThrowIfLessThanZero();
        warningCount.ThrowIfLessThanZero();
        informationCount.ThrowIfLessThanZero();
        if (invalidRecords > recordsRead)
            throw new ArgumentException("Invalid records cannot exceed records read.", nameof(invalidRecords));

        if (unitsValidated > recordsRead - invalidRecords)
            throw new ArgumentException("Validated units require distinct valid input records.", nameof(unitsValidated));

        if (unitsAttempted > unitsValidated)
            throw new ArgumentException("Attempted units cannot exceed validated units.", nameof(unitsAttempted));

        if (unitsExecuted > unitsAttempted)
            throw new ArgumentException("Executed units cannot exceed attempted units.", nameof(unitsExecuted));

        if (invalidRecords > 0 && errorCount == 0)
            throw new ArgumentException("Invalid records require a content error.", nameof(errorCount));

        BytesRead = bytesRead;
        RecordsRead = recordsRead;
        InvalidRecords = invalidRecords;
        UnitsValidated = unitsValidated;
        UnitsAttempted = unitsAttempted;
        UnitsExecuted = unitsExecuted;
        ErrorCount = errorCount;
        WarningCount = warningCount;
        InformationCount = informationCount;
    }

    /// <summary>Gets the validation-pass bytes consumed; excludes replay reads.</summary>
    public long BytesRead { get; }

    /// <summary>Gets the physical data/envelope records returned during validation.</summary>
    public long RecordsRead { get; }

    /// <summary>Gets the distinct invalid physical records, not the number of diagnostics.</summary>
    public long InvalidRecords { get; }

    /// <summary>Gets the flat rows or root groups that passed unit validation.</summary>
    public long UnitsValidated { get; }

    /// <summary>Gets the execution hooks entered, including a hook that failed.</summary>
    public long UnitsAttempted { get; }

    /// <summary>Gets the execution hooks that returned success.</summary>
    public long UnitsExecuted { get; }

    /// <summary>Gets the emitted validation error count.</summary>
    public long ErrorCount { get; }

    /// <summary>Gets the emitted validation warning count.</summary>
    public long WarningCount { get; }

    /// <summary>Gets the emitted validation informational count.</summary>
    public long InformationCount { get; }
}
