namespace CoreEx.IO;

/// <summary>Provides an immutable progress snapshot without retaining input models or diagnostics.</summary>
public sealed class FileProcessingProgress
{
    /// <summary>Initializes nonnegative record, unit, diagnostic, and execution counts; replay does not duplicate validation counters.</summary>
    /// <param name="bytesRead">The bytes consumed during the validation pass, including syntax and envelope records.</param>
    /// <param name="recordsRead">The format-defined source records returned during validation, including malformed records.</param>
    /// <param name="invalidRecords">The distinct source records known to fail parsing, mapping, field, or structural checks.</param>
    /// <param name="unitsRead">The recognized candidate business-unit boundaries; defaults to source records for flat processors.</param>
    /// <param name="invalidUnits">The distinct recognized candidates rejected by mapping, structure, or validation.</param>
    /// <param name="unitsValidated">The accepted flat rows or root groups that passed unit validation.</param>
    /// <param name="unitsAttempted">The execution hooks entered, including a hook that failed; excludes replay reads before a hook.</param>
    /// <param name="unitsExecuted">The execution hooks that returned success, not a guarantee of transaction commits or exactly-once effects.</param>
    /// <param name="errorCount">The error diagnostics emitted during validation.</param>
    /// <param name="warningCount">The warning diagnostics emitted during validation.</param>
    /// <param name="informationCount">The informational diagnostics emitted during validation.</param>
    internal FileProcessingProgress(long bytesRead = 0, long recordsRead = 0, long invalidRecords = 0, long? unitsRead = null, long invalidUnits = 0,
        long unitsValidated = 0, long unitsAttempted = 0, long unitsExecuted = 0, long errorCount = 0, long warningCount = 0, long informationCount = 0)
    {
        bytesRead.ThrowIfLessThanZero();
        recordsRead.ThrowIfLessThanZero();
        invalidRecords.ThrowIfLessThanZero();
        unitsRead?.ThrowIfLessThanZero();
        invalidUnits.ThrowIfLessThanZero();
        unitsValidated.ThrowIfLessThanZero();
        unitsAttempted.ThrowIfLessThanZero();
        unitsExecuted.ThrowIfLessThanZero();
        errorCount.ThrowIfLessThanZero();
        warningCount.ThrowIfLessThanZero();
        informationCount.ThrowIfLessThanZero();
        var actualUnitsRead = unitsRead ?? recordsRead;

        if (invalidRecords > recordsRead)
            throw new ArgumentException("Invalid records cannot exceed records read.", nameof(invalidRecords));

        if (invalidUnits > actualUnitsRead)
            throw new ArgumentException("Invalid units cannot exceed units read.", nameof(invalidUnits));

        if (unitsValidated > actualUnitsRead - invalidUnits)
            throw new ArgumentException("Validated units require distinct valid input units.", nameof(unitsValidated));

        if (unitsAttempted > unitsValidated)
            throw new ArgumentException("Attempted units cannot exceed validated units.", nameof(unitsAttempted));

        if (unitsExecuted > unitsAttempted)
            throw new ArgumentException("Executed units cannot exceed attempted units.", nameof(unitsExecuted));

        if (invalidUnits > 0 && errorCount == 0)
            throw new ArgumentException("Invalid units require a content error.", nameof(errorCount));

        if (invalidRecords > 0 && errorCount == 0)
            throw new ArgumentException("Invalid records require a content error.", nameof(errorCount));

        BytesRead = bytesRead;
        RecordsRead = recordsRead;
        InvalidRecords = invalidRecords;
        UnitsRead = actualUnitsRead;
        InvalidUnits = invalidUnits;
        UnitsValidated = unitsValidated;
        UnitsAttempted = unitsAttempted;
        UnitsExecuted = unitsExecuted;
        ErrorCount = errorCount;
        WarningCount = warningCount;
        InformationCount = informationCount;
    }

    /// <summary>Gets the validation-pass bytes consumed; excludes replay reads.</summary>
    public long BytesRead { get; }

    /// <summary>Gets the format-defined source records returned during validation.</summary>
    public long RecordsRead { get; }

    /// <summary>Gets the distinct invalid source records, not the number of diagnostics.</summary>
    public long InvalidRecords { get; }

    /// <summary>Gets the candidate business-unit boundaries recognized during validation.</summary>
    public long UnitsRead { get; }

    /// <summary>Gets the distinct candidate business units rejected by mapping, structure, or validation.</summary>
    public long InvalidUnits { get; }

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
