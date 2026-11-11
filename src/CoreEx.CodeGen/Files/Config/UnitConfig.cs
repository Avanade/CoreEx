namespace CoreEx.CodeGen.Files.Config;

/// <summary>Identifies the single JSONL business record and its file-wide occurrence requirement.</summary>
[CodeGenClass("Unit", Title = "Business object selection.")]
[CodeGenCategory("Primary", Title = "Business record and cardinality.")]
public class UnitConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the declared business root record.</summary>
    [JsonPropertyName("record")]
    [CodeGenProperty("Primary", Title = "Business root record.", Description = "Inferred only when exactly one non-metadata, non-child record is eligible.")]
    public string? Record { get; set; }

    /// <summary>Gets or sets candidate units per file.</summary>
    [JsonPropertyName("cardinality")]
    [CodeGenProperty("Primary", Title = "File unit cardinality.", Options = ["Many", "One", "Optional", "OneOrMore"], Description = "Defaults to Many.")]
    public string? Cardinality { get; set; }

    /// <summary>Gets the resolved root type.</summary>
    public RecordConfig RecordConfig => Root!.Records!.Single(x => x.Name == Record);

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        Cardinality ??= "Many";
        if (Cardinality is not ("Many" or "One" or "Optional" or "OneOrMore"))
            throw new CodeGenException(this, nameof(Cardinality), "Require Many, One, Optional, or OneOrMore.");

        return Task.CompletedTask;
    }
}
