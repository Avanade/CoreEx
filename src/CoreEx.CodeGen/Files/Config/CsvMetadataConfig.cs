namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines CSV-specific placement for one file-level metadata record.</summary>
[CodeGenClass("CsvMetadata", Title = "CSV metadata settings.")]
[CodeGenCategory("Primary", Title = "Metadata placement.")]
public class CsvMetadataConfig : ConfigBase<CodeGenConfig, MetadataConfig>
{
    /// <summary>Gets or sets the optional first/last boundary restriction.</summary>
    [JsonPropertyName("position")]
    [CodeGenProperty("Primary", Title = "Metadata position.", Options = ["Any", "First", "Last"], Description = "Defaults to Any; blank/comment records and the column-name header do not count.")]
    public string? Position { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        Position = DefaultWhereNull(Position, () => "Any");
        if (Position is not ("Any" or "First" or "Last"))
            throw new CodeGenException(this, nameof(Position), "Metadata position must be Any, First, or Last.");

        return Task.CompletedTask;
    }
}
