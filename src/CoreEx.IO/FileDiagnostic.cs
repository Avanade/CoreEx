namespace CoreEx.IO;

/// <summary>Represents one content diagnostic without an exception or retained raw input value.</summary>
public sealed class FileDiagnostic
{
    /// <summary>Initializes a structured diagnostic, preserving localization until the consumer renders it.</summary>
    /// <param name="category">The diagnostic category.</param>
    /// <param name="code">The stable machine-readable diagnostic code.</param>
    /// <param name="message">The localization key and/or explanatory text; do not include sensitive input values.</param>
    /// <param name="severity">The CoreEx severity; only errors reject input.</param>
    /// <param name="location">The optional physical/logical location.</param>
    /// <param name="recordType">The configured logical record type, when known.</param>
    public FileDiagnostic(FileDiagnosticCategory category, string code, LText message, MessageType severity = MessageType.Error, FileLocation? location = null, string? recordType = null)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(category));

        if (!Enum.IsDefined(severity))
            throw new ArgumentOutOfRangeException(nameof(severity));

        code.ThrowIfNullOrWhiteSpace();
        if (string.IsNullOrWhiteSpace(message.KeyAndOrText) && string.IsNullOrWhiteSpace(message.FallbackText))
            throw new ArgumentException("A diagnostic requires a localization key or nonblank text.", nameof(message));

        if (recordType is not null)
            recordType.ThrowIfNullOrWhiteSpace();

        Category = category;
        Code = code;
        Message = message;
        Severity = severity;
        Location = location;
        RecordType = recordType;
    }

    /// <summary>Gets the diagnostic category.</summary>
    public FileDiagnosticCategory Category { get; }

    /// <summary>Gets the stable machine-readable code.</summary>
    public string Code { get; }

    /// <summary>Gets the unrendered localization key and/or message.</summary>
    public LText Message { get; }

    /// <summary>Gets the CoreEx diagnostic severity.</summary>
    public MessageType Severity { get; }

    /// <summary>Gets whether this diagnostic rejects the input.</summary>
    public bool IsError => Severity == MessageType.Error;

    /// <summary>Gets the optional physical/logical location.</summary>
    public FileLocation? Location { get; }

    /// <summary>Gets the logical record type, or null when routing was not possible.</summary>
    public string? RecordType { get; }
}
