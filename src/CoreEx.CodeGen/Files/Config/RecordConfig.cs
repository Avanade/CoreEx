namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares one typed wire record and optional exact discriminator route.</summary>
[CodeGenClass("Record", Title = "File record.")]
[CodeGenCategory("Primary", Title = "Record identity.")]
[CodeGenCategory("Csv", Title = "CSV routing.")]
[CodeGenCategory("Jsonl", Title = "JSONL routing.")]
[CodeGenCategory("Collections", Title = "Mapped properties.")]
public class RecordConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the logical record key and generated type suffix.</summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "Logical record name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>Gets or sets CSV-specific routing settings.</summary>
    [JsonPropertyName("csv")]
    [CodeGenProperty("Csv", Title = "CSV routing settings.")]
    public CsvRecordConfig? Csv { get; set; }

    /// <summary>Gets or sets an explicit JSONL root route.</summary>
    [JsonPropertyName("jsonl")]
    [CodeGenProperty("Jsonl", Title = "JSONL root discriminator match.")]
    public JsonLinesRecordConfig? Jsonl { get; set; }

    /// <summary>Gets or sets mapped properties.</summary>
    [JsonPropertyName("properties")]
    [CodeGenPropertyCollection("Collections", Title = "Wire properties.", IsMandatory = true)]
    public List<PropertyConfig>? Properties { get; set; }

    /// <summary>Gets the definition-prefixed type name.</summary>
    public string TypeName => Root!.Name + Name;

    /// <summary>Gets a safe route-key literal.</summary>
    public string KeyLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Name!);

    /// <summary>Gets a safe discriminator literal.</summary>
    public string MatchLiteral => Root!.IsJsonl && Jsonl is null ? "null" : OnRamp.Utility.CSharpSource.StringLiteral(Jsonl?.Match ?? Csv?.Match ?? string.Empty);

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        RejectUnknownProperties();
        if (Root!.IsJson ? Csv is not null || !Root.IsJsonl && Jsonl is not null : Jsonl is not null)
            throw new CodeGenException(this, nameof(Csv), "Record routing settings do not apply to this format.");

        Name = ConfigValidation.Identifier(this, nameof(Name), Name);
        if (Jsonl is not null)
            await Jsonl.PrepareAsync(Root!, this).ConfigureAwait(false);

        if (Csv is not null)
            await Csv.PrepareAsync(Root!, this).ConfigureAwait(false);

        if (Properties is null || Properties.Count == 0 || Properties.Any(x => x is null))
            throw new CodeGenException(this, nameof(Properties), "At least one non-null mapped property is required.");

        Properties = await PrepareCollectionAsync(Properties).ConfigureAwait(false);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var sourceNames = new HashSet<string>(Root!.IsJson ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
        var indices = new HashSet<int>();
        foreach (var property in Properties)
        {
            if (!names.Add(property.PropertyName) || property.PropertyName == TypeName || typeof(object).GetMembers().Any(x => x.Name == property.PropertyName))
                throw new CodeGenException(property, nameof(property.Property), "Duplicate generated property or conflict with its containing/inherited type; set property to a distinct valid identifier.");

            if (!sourceNames.Add(property.Name!) || property.Csv?.Index.HasValue == true && !indices.Add(property.Csv.Index.Value))
                throw new CodeGenException(property, nameof(property.Name), "Duplicate source field or CSV selector.");
        }
    }
}
