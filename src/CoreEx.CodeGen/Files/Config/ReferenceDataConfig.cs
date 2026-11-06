namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares validation of a CSV string code against an existing reference-data collection.</summary>
[CodeGenClass("ReferenceData", Title = "Reference-data code validation.")]
[CodeGenCategory("Validation", Title = "Typed code lookup.")]
public class ReferenceDataConfig : ConfigBase<CodeGenConfig, PropertyConfig>
{
    /// <summary>Gets or sets the fully qualified reference-data CLR type.</summary>
    [JsonPropertyName("type")]
    [CodeGenProperty("Validation", Title = "Reference-data CLR type.", IsMandatory = true, Description = "An existing IReferenceData type, for example Contoso.Contracts.ReferenceData.Country. The wire property remains string?.")]
    public string? Type { get; set; }

    /// <summary>Gets or sets whether inactive codes are accepted.</summary>
    [JsonPropertyName("allowInactive")]
    [CodeGenProperty("Validation", Title = "Allow inactive codes.", Description = "Defaults to false. Unknown codes always fail validation.")]
    public bool? AllowInactive { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        RejectUnknownProperties();
        Type = ConfigValidation.Namespace(this, nameof(Type), Type);
        AllowInactive = DefaultWhereNull(AllowInactive, () => false);
        if (Parent!.Type != "String")
            throw new CodeGenException(this, nameof(Type), "Reference-data code validation requires a String wire property.");

        return Task.CompletedTask;
    }
}
