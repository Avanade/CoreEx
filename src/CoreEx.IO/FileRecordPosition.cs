namespace CoreEx.IO;

/// <summary>Defines an optional file-boundary restriction for a business record or file-level metadata.</summary>
/// <remarks>Each format determines which source records participate, excluding its skipped content and column-name headers.</remarks>
public enum FileRecordPosition
{
    /// <summary>Allows any participating source-record position.</summary>
    Any,

    /// <summary>Requires the first participating source record.</summary>
    First,

    /// <summary>Requires the last participating source record; no participating record may follow it.</summary>
    Last
}
