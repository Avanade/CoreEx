namespace CoreEx.IO.Json;

/// <summary>Declares one object selected as non-executable JSON document metadata.</summary>
public abstract class JsonFileMetadata
{
    private protected JsonFileMetadata(string name, string recordType, string path, bool required)
    {
        Name = name.ThrowIfNullOrWhiteSpace();
        RecordType = recordType.ThrowIfNullOrWhiteSpace();
        Path = path.ThrowIfNull();
        Segments = JsonPointer.Parse(path);
        Required = required;
    }

    /// <summary>Gets the logical context accessor key.</summary>
    public string Name { get; }

    /// <summary>Gets the logical metadata record type.</summary>
    public string RecordType { get; }

    /// <summary>Gets the JSON Pointer selecting this metadata object.</summary>
    public string Path { get; }

    /// <summary>Gets whether exactly one occurrence is required.</summary>
    public bool Required { get; }

    /// <summary>Gets the decoded selector segments used for overlap checks.</summary>
    internal string[] Segments { get; }

    /// <summary>Maps and validates the selected metadata object.</summary>
    /// <param name="cursor">The cursor positioned at the selected JSON object.</param>
    /// <param name="context">The pass-local mapping and diagnostic context.</param>
    /// <param name="validate">Whether pure metadata validation should run.</param>
    /// <param name="cancellationToken">The processing cancellation token.</param>
    /// <returns>The typed metadata value.</returns>
    internal abstract Task<object?> MapAsync(JsonDocumentCursor cursor, JsonMappingContext context, bool validate, CancellationToken cancellationToken);
}
