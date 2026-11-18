namespace CoreEx.IO.Json;

/// <summary>Streams diagnostics for one mapping without retaining diagnostic history.</summary>
/// <param name="options">The configured JSON mapping policies.</param>
/// <param name="line">The physical source line number.</param>
/// <param name="ordinal">The business-unit ordinal, or null for metadata.</param>
/// <param name="recordType">The logical type associated with this mapping.</param>
/// <param name="report">The callback that records a diagnostic.</param>
/// <param name="stopped">The callback that reports whether processing has stopped.</param>
internal sealed class JsonMappingContext(JsonFileOptions options, long line, long? ordinal, string recordType, Func<FileDiagnostic, CancellationToken, Task> report, Func<bool> stopped)
{
    /// <summary>Gets the mapping policy.</summary>
    public JsonFileOptions Options { get; } = options;

    /// <summary>Gets whether any mapped property failed.</summary>
    public bool Invalid { get; private set; }

    /// <summary>Gets whether the fail-fast policy stopped mapping.</summary>
    public bool Stopped => stopped();

    /// <summary>Reports a pure metadata validator message with its original text and severity.</summary>
    /// <param name="message">The validator message text.</param>
    /// <param name="severity">The validator message severity.</param>
    /// <param name="property">The corresponding declared model property, when present.</param>
    /// <param name="source">The corresponding source JSON Pointer.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous diagnostic-reporting operation.</returns>
    public Task ReportValidationAsync(LText message, MessageType severity, string? property, string source, CancellationToken cancellationToken)
    {
        Invalid |= severity == MessageType.Error;
        return report(new(FileDiagnosticCategory.Validation, "file.validation", message, severity,
            new(line, propertyPath: property, sourcePath: source), recordType, ordinal), cancellationToken);
    }

    /// <summary>Reports a declared property or parent-object error without including input values.</summary>
    /// <param name="code">The stable diagnostic code.</param>
    /// <param name="propertyPath">The declared model path.</param>
    /// <param name="sourcePath">The source JSON Pointer.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The asynchronous diagnostic-reporting operation.</returns>
    public Task FailAsync(string code, string propertyPath, string sourcePath, CancellationToken cancellationToken)
    {
        Invalid = true;
        return report(new(code == "file.structure.cardinality" ? FileDiagnosticCategory.Structure : FileDiagnosticCategory.Mapping,
            code, code == "file.structure.cardinality" ? "The collection does not satisfy its occurrence bounds." : "The JSON value does not match the declared contract.",
            location: new(line, propertyPath: string.IsNullOrEmpty(propertyPath) ? null : propertyPath, sourcePath: sourcePath), recordType: recordType, unitOrdinal: ordinal), cancellationToken);
    }

    /// <summary>Appends an escaped direct source member to a JSON Pointer.</summary>
    /// <param name="parent">The parent JSON Pointer.</param>
    /// <param name="name">The source member name to escape and append.</param>
    /// <returns>The escaped child JSON Pointer.</returns>
    public static string Source(string parent, string name) => parent + "/" + name.Replace("~", "~0", StringComparison.Ordinal).Replace("/", "~1", StringComparison.Ordinal);

    /// <summary>Appends a declared property to its model path.</summary>
    /// <param name="parent">The parent model path.</param>
    /// <param name="name">The declared property name to append.</param>
    /// <returns>The child model path.</returns>
    public static string Property(string parent, string name) => parent.Length == 0 ? name : parent + "." + name;

    /// <summary>Checks cardinality independently from presence and conversion.</summary>
    /// <param name="cardinality">The required occurrence cardinality.</param>
    /// <param name="count">The observed number of occurrences.</param>
    /// <returns>Whether the count satisfies the cardinality.</returns>
    public static bool Allows(FileCardinality cardinality, long count) => cardinality switch
    {
        FileCardinality.Many => true,
        FileCardinality.One => count == 1,
        FileCardinality.Optional => count <= 1,
        FileCardinality.OneOrMore => count > 0,
        _ => throw new ArgumentOutOfRangeException(nameof(cardinality))
    };
}
