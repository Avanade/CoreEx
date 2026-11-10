namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares one typed wire record and optional exact discriminator route.</summary>
[CodeGenClass("Record", Title = "File record.")]
[CodeGenCategory("Primary", Title = "Record identity.")]
[CodeGenCategory("Csv", Title = "CSV routing.")]
[CodeGenCategory("Structure", Title = "Route continuation policy.")]
[CodeGenCategory("Collections", Title = "Mapped properties.")]
public class RecordConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the logical record key and generated type suffix.</summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "Logical record name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>Gets or sets the route continuation override.</summary>
    [JsonPropertyName("errorMode")]
    [CodeGenProperty("Structure", Title = "Route error policy.", Options = ["FailFast", "CollectErrors"])]
    public string? ErrorMode { get; set; }

    /// <summary>Gets or sets CSV-specific routing settings.</summary>
    [JsonPropertyName("csv")]
    [CodeGenProperty("Csv", Title = "CSV routing settings.")]
    public CsvRecordConfig? Csv { get; set; }

    /// <summary>Gets or sets mapped properties.</summary>
    [JsonPropertyName("properties")]
    [CodeGenPropertyCollection("Collections", Title = "Wire properties.", IsMandatory = true)]
    public List<PropertyConfig>? Properties { get; set; }

    /// <summary>Gets the definition-prefixed type name.</summary>
    public string TypeName => Root!.Name + Name;

    /// <summary>Gets a safe route-key literal.</summary>
    public string KeyLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Name!);

    /// <summary>Gets a safe discriminator literal.</summary>
    public string MatchLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Csv?.Match ?? string.Empty);

    /// <summary>Gets the optional policy expression.</summary>
    public string ErrorModeExpression => ErrorMode is null ? "null" : "FileErrorMode." + ErrorMode;

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        RejectUnknownProperties();
        Name = ConfigValidation.Identifier(this, nameof(Name), Name);
        if (Csv is not null)
            await Csv.PrepareAsync(Root!, this).ConfigureAwait(false);

        if (Properties is null || Properties.Count == 0 || Properties.Any(x => x is null))
            throw new CodeGenException(this, nameof(Properties), "At least one non-null mapped property is required.");

        Properties = await PrepareCollectionAsync(Properties).ConfigureAwait(false);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var sourceNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var indices = new HashSet<int>();
        foreach (var property in Properties)
        {
            if (!names.Add(property.PropertyName) || property.PropertyName == TypeName || typeof(object).GetMembers().Any(x => x.Name == property.PropertyName))
                throw new CodeGenException(property, nameof(property.Property), "Duplicate generated property or conflict with its containing/inherited type; set property to a distinct valid identifier.");

            if (property.IsDateTime && property.Converter == TypeName + "Mapping")
                throw new CodeGenException(property, nameof(property.Property), $"Generated conversion helper '{property.Converter}' conflicts with its containing mapping type.");

            if (!sourceNames.Add(property.Name!) || property.Csv?.Index.HasValue == true && !indices.Add(property.Csv.Index.Value))
                throw new CodeGenException(property, nameof(property.Name), "Duplicate source field or CSV selector.");
        }
    }
}
