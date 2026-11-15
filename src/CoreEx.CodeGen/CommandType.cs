namespace CoreEx.CodeGen;

/// <summary>
/// Represents the command type.
/// </summary>
public enum CommandType
{
    /// <summary>
    /// Execute the reference-data code generation (default).
    /// </summary>
    RefData,

    /// <summary>
    /// Counts the files and lines of code distinguishing between generated and non-generated.
    /// </summary>
    Count,

    /// <summary>
    /// Deletes generated files from the solution directories.
    /// </summary>
    Clean,

    /// <summary>
    /// Generates typed CSV processing from selected top-level *-file.yaml definitions.
    /// </summary>
    File
}
