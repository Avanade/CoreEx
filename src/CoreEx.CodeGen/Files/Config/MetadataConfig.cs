namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares one file-level metadata record by its logical name and wire-record type.</summary>
[CodeGenClass("Metadata", Title = "File metadata.")]
[CodeGenCategory("Primary", Title = "Metadata identity and occurrence.")]
public class MetadataConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the logical metadata name.</summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "Logical metadata name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>Gets or sets the exact declared wire-record name.</summary>
    [JsonPropertyName("record")]
    [CodeGenProperty("Primary", Title = "Wire record.", IsMandatory = true)]
    public string? Record { get; set; }

    /// <summary>Gets or sets whether exactly one occurrence is required.</summary>
    [JsonPropertyName("required")]
    [CodeGenProperty("Primary", Title = "Required metadata.", Description = "Defaults to false; duplicate occurrences are always errors.")]
    public bool? Required { get; set; }

    /// <summary>Gets or sets CSV-specific metadata routing.</summary>
    [JsonPropertyName("csv")]
    [CodeGenProperty("Primary", Title = "CSV metadata routing.")]
    public CsvMetadataConfig? Csv { get; set; }

    /// <summary>Gets or sets JSONL metadata placement.</summary>
    [JsonPropertyName("jsonl")]
    [CodeGenProperty("Primary", Title = "JSONL metadata placement.")]
    public JsonLinesMetadataConfig? Jsonl { get; set; }

    /// <summary>Gets the matching record config.</summary>
    public RecordConfig RecordConfig => Root!.Records!.Single(x => x.Name == Record);

    /// <summary>Gets the configured name as a safe C# string literal.</summary>
    public string NameLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Name!);

    /// <summary>Gets the configured record key as a safe C# string literal.</summary>
    public string RecordLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Record!);

    /// <summary>Gets the requiredness as a C# literal.</summary>
    public string RequiredLiteral => Required == true ? "true" : "false";

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        RejectUnknownProperties();
        Name = RequireText(nameof(Name), Name);
        Record = RequireText(nameof(Record), Record);
        Required = DefaultWhereNull(Required, () => false);
        if (Root!.IsJsonl ? Csv is not null : Jsonl is not null)
            throw new CodeGenException(this, nameof(Csv), "Metadata settings do not apply to this format.");

        if (Root.IsJsonl)
        {
            Jsonl ??= new();
            await Jsonl.PrepareAsync(Root, this).ConfigureAwait(false);
        }
        else
        {
            Csv ??= new();
            await Csv.PrepareAsync(Root, this).ConfigureAwait(false);
        }
    }
}
