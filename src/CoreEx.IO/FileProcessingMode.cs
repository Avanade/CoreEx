namespace CoreEx.IO;

/// <summary>Defines whether accepted input is replayed for business execution.</summary>
public enum FileProcessingMode
{
    /// <summary>Validates input without executing business units.</summary>
    ValidateOnly,

    /// <summary>Validates the entire input before replaying its immutable contents for execution.</summary>
    ValidateThenExecute
}
