namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines JSONL metadata placement among all non-skipped lines.</summary>
[CodeGenClass("JsonlMetadata", Title = "JSONL metadata placement.")]
[CodeGenCategory("Primary", Title = "Metadata placement.")]
public class JsonLinesMetadataConfig : ConfigBase<CodeGenConfig, MetadataConfig>
{
    /// <summary>Gets or sets the source-order restriction.</summary>
    [JsonPropertyName("position")]
    [CodeGenProperty("Primary", Title = "Metadata position.", Options = ["Any", "First", "Last"], Description = "Defaults to Any; malformed/unroutable non-skipped lines participate.")]
    public string? Position { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        Position ??= "Any";
        if (Position is not ("Any" or "First" or "Last"))
            throw new CodeGenException(this, nameof(Position), "Require Any, First, or Last.");

        return Task.CompletedTask;
    }
}
