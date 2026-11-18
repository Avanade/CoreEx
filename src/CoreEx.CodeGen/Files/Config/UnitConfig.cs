namespace CoreEx.CodeGen.Files.Config;

/// <summary>Identifies the single JSON/JSONL business record and its selection and occurrence requirements.</summary>
[CodeGenClass("Unit", Title = "Business unit selection.")]
[CodeGenCategory("Primary", Title = "Business record, selection, and cardinality.")]
public class UnitConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the declared business root record.</summary>
    [JsonPropertyName("record")]
    [CodeGenProperty("Primary", Title = "Business root record.", Description = "Inferred only when exactly one non-metadata, non-child record is eligible.")]
    public string? Record { get; set; }

    /// <summary>Gets or sets candidate units per file.</summary>
    [JsonPropertyName("cardinality")]
    [CodeGenProperty("Primary", Title = "File unit cardinality.", Options = ["Many", "One", "Optional", "OneOrMore"], Description = "Defaults to One for root Object input; otherwise Many.")]
    public string? Cardinality { get; set; }

    /// <summary>Gets or sets the selected JSON document value kind.</summary>
    [JsonPropertyName("kind")]
    [CodeGenProperty("Primary", Title = "JSON document value kind.", Options = ["Array", "Object"], Description = "Defaults to Array; applies only to JSON documents.")]
    public string? Kind { get; set; }

    /// <summary>Gets or sets the literal-member JSON Pointer selecting the business value.</summary>
    [JsonPropertyName("unitPath")]
    [CodeGenProperty("Primary", Title = "JSON document unit path.", Description = "Defaults to the root pointer; applies only to JSON documents.")]
    public string? UnitPath { get; set; }

    /// <summary>Gets the resolved root type.</summary>
    public RecordConfig RecordConfig => Root!.Records!.Single(x => x.Name == Record);

    /// <summary>Gets the selected JSON document kind literal.</summary>
    public string KindLiteral => Kind ?? "Array";

    /// <summary>Gets the unit selector as a safe C# string literal.</summary>
    public string UnitPathLiteral => OnRamp.Utility.CSharpSource.StringLiteral(UnitPath ?? string.Empty);

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        if (Root!.IsJsonl && (Kind is not null || UnitPath is not null))
            throw new CodeGenException(this, nameof(Kind), "kind and unitPath apply only to JSON document input.");

        if (Root.IsJson)
        {
            Kind ??= "Array";
            UnitPath ??= string.Empty;
            if (Kind is not ("Array" or "Object"))
                throw new CodeGenException(this, nameof(Kind), "Require Array or Object.");
        }

        Cardinality ??= Root.IsJsonDocument && Kind == "Object" ? "One" : "Many";
        if (Cardinality is not ("Many" or "One" or "Optional" or "OneOrMore"))
            throw new CodeGenException(this, nameof(Cardinality), "Require Many, One, Optional, or OneOrMore.");

        if (Root.IsJsonDocument && Kind == "Object" && (UnitPath?.Length != 0 || Cardinality != "One"))
            throw new CodeGenException(this, nameof(Kind), "Object input selects the root and requires unit cardinality One.");

        return Task.CompletedTask;
    }
}
