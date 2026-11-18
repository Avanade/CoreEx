namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines a nested collection occurrence and optional business count constraint.</summary>
[CodeGenClass("Collection", Title = "Nested collection occurrence.")]
[CodeGenCategory("Primary", Title = "Element type and occurrence bounds.")]
public class CollectionConfig : ConfigBase<CodeGenConfig, PropertyConfig>
{
    /// <summary>Gets or sets the element record reference.</summary>
    [JsonPropertyName("record")]
    [CodeGenProperty("Primary", Title = "Element record.", Description = "Exactly one of record/type is required.")]
    public string? Record { get; set; }

    /// <summary>Gets or sets the scalar element type.</summary>
    [JsonPropertyName("type")]
    [CodeGenProperty("Primary", Title = "Scalar element type.", Options = ["String", "Integer", "Int64", "Decimal", "Double", "Boolean", "Guid", "DateOnly", "DateTime", "DateTimeOffset"], Description = "Exactly one of record/type is required. Null elements are rejected.")]
    public string? Type { get; set; }

    /// <summary>Gets or sets the occurrence cardinality.</summary>
    [JsonPropertyName("cardinality")]
    [CodeGenProperty("Primary", Title = "Per-parent cardinality.", Options = ["Many", "One", "Optional", "OneOrMore"], Description = "Defaults to Many; missing/null counts as zero.")]
    public string? Cardinality { get; set; }

    /// <summary>Gets or sets the numeric maximum element count.</summary>
    [JsonPropertyName("maxItems")]
    [CodeGenProperty("Primary", Title = "Optional maximum elements.", Description = "No maximum by default. An explicit positive count is checked by the generated validator after deserialization.")]
    public int? MaxItems { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        Cardinality ??= "Many";
        if ((Record is null) == (Type is null) || MaxItems <= 0 || Cardinality is not ("Many" or "One" or "Optional" or "OneOrMore"))
            throw new CodeGenException(this, nameof(Record), "Require exactly one element record/type, positive maxItems, and a valid cardinality.");

        if (Record is not null)
            ConfigValidation.Identifier(this, nameof(Record), Record);

        if (Type is not null && Type is not ("String" or "Integer" or "Int64" or "Decimal" or "Double" or "Boolean" or "Guid" or "DateOnly" or "DateTime" or "DateTimeOffset"))
            throw new CodeGenException(this, nameof(Type), "Unsupported scalar element type.");

        return Task.CompletedTask;
    }
}
