namespace CoreEx.IO;

/// <summary>Describes optional physical and logical locations without requiring a line-oriented format.</summary>
public sealed class FileLocation
{
    /// <summary>Initializes a location; logical paths are opaque and are never interpreted as physical coordinates.</summary>
    /// <param name="lineNumber">The one-based physical starting line, when known.</param>
    /// <param name="linePosition">The one-based character position within that line, when known.</param>
    /// <param name="fieldIndex">The zero-based field index, when applicable.</param>
    /// <param name="fieldName">The declared field name, not its input value.</param>
    /// <param name="propertyPath">The logical property, JSON, or XML path, when available.</param>
    /// <param name="sourcePath">The format-native source path, when available.</param>
    public FileLocation(long? lineNumber = null, int? linePosition = null, int? fieldIndex = null, string? fieldName = null, string? propertyPath = null, string? sourcePath = null)
    {
        if (lineNumber.HasValue)
            lineNumber.Value.ThrowIfLessThanOrEqualToZero(paramName: nameof(lineNumber));

        if (linePosition.HasValue)
            linePosition.Value.ThrowIfLessThanOrEqualToZero(paramName: nameof(linePosition));

        if (linePosition.HasValue && !lineNumber.HasValue)
            throw new ArgumentOutOfRangeException(nameof(linePosition), "A positive line position requires a physical line number.");

        if (fieldIndex.HasValue)
            fieldIndex.Value.ThrowIfLessThanZero(paramName: nameof(fieldIndex));

        if (fieldName is not null)
            fieldName.ThrowIfNullOrWhiteSpace();

        if (propertyPath is not null)
            propertyPath.ThrowIfNullOrWhiteSpace();

        if (sourcePath is not null && sourcePath.Length > 0 && string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("A source path cannot contain only whitespace.", nameof(sourcePath));

        LineNumber = lineNumber;
        LinePosition = linePosition;
        FieldIndex = fieldIndex;
        FieldName = fieldName;
        PropertyPath = propertyPath;
        SourcePath = sourcePath;
    }

    /// <summary>Gets the one-based physical starting line, or null when unavailable.</summary>
    public long? LineNumber { get; }

    /// <summary>Gets the one-based physical character position, not a field index.</summary>
    public int? LinePosition { get; }

    /// <summary>Gets the zero-based physical field index, when applicable.</summary>
    public int? FieldIndex { get; }

    /// <summary>Gets the declared field name, when applicable.</summary>
    public string? FieldName { get; }

    /// <summary>Gets the opaque logical path; it does not imply a physical location.</summary>
    public string? PropertyPath { get; }

    /// <summary>Gets the opaque format-native source path; an empty path identifies a document root.</summary>
    public string? SourcePath { get; }
}
