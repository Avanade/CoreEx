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
    /// Counts the files and lines of code for child directories distinguising between generated and non-generated.
    /// </summary>
    Count
}