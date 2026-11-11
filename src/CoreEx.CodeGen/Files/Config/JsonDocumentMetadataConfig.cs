namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines one JSON document metadata selector.</summary>
[CodeGenClass("JsonDocumentMetadata", Title = "JSON document metadata selector.")]
[CodeGenCategory("Primary", Title = "JSON Pointer selector.")]
public class JsonDocumentMetadataConfig : ConfigBase<CodeGenConfig, MetadataConfig>
{
    /// <summary>Gets or sets the JSON Pointer selecting the metadata object.</summary>
    [JsonPropertyName("path")]
    [CodeGenProperty("Primary", Title = "JSON Pointer.", IsMandatory = true)]
    public string? Path { get; set; }

    /// <summary>Gets the configured JSON Pointer as a safe C# string literal.</summary>
    public string PathLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Path!);

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        Path = RequireText(nameof(Path), Path);
        return Task.CompletedTask;
    }
}
