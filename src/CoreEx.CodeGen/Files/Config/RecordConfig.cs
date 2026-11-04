namespace CoreEx.CodeGen.Files.Config;

/// <summary>Declares one typed wire record and optional exact discriminator route.</summary>
[CodeGenClass("Record", Title = "File record.")]
[CodeGenCategory("Primary", Title = "Record identity.")]
[CodeGenCategory("Structure", Title = "Routing and continuation.")]
[CodeGenCategory("Collections", Title = "Mapped properties.")]
public class RecordConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>Gets or sets the logical record key and generated type suffix.</summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "Logical record name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>Gets or sets the exact discriminator value.</summary>
    [JsonPropertyName("match")]
    [CodeGenProperty("Structure", Title = "Exact discriminator.", Description = "Required for hierarchical files; forbidden for flat files.")]
    public string? Match { get; set; }

    /// <summary>Gets or sets the route continuation override.</summary>
    [JsonPropertyName("errorMode")]
    [CodeGenProperty("Structure", Title = "Route error policy.", Options = ["FailFast", "CollectErrors"])]
    public string? ErrorMode { get; set; }

    /// <summary>Gets or sets mapped properties.</summary>
    [JsonPropertyName("properties")]
    [CodeGenPropertyCollection("Collections", Title = "Wire properties.", IsMandatory = true)]
    public List<PropertyConfig>? Properties { get; set; }

    /// <summary>Gets the definition-prefixed type name.</summary>
    public string TypeName => Root!.Name + Name;

    /// <summary>Gets a safe route-key literal.</summary>
    public string KeyLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Name!);

    /// <summary>Gets a safe discriminator literal.</summary>
    public string MatchLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Match ?? string.Empty);

    /// <summary>Gets the optional policy expression.</summary>
    public string ErrorModeExpression => ErrorMode is null ? "null" : "FileErrorMode." + ErrorMode;

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        RejectUnknownProperties();
        Name = ConfigValidation.Identifier(this, nameof(Name), Name);
        if (Properties is null || Properties.Count == 0 || Properties.Any(x => x is null))
            throw new CodeGenException(this, nameof(Properties), "At least one non-null mapped property is required.");

        Properties = await PrepareCollectionAsync(Properties).ConfigureAwait(false);
        var names = new HashSet<string>(StringComparer.Ordinal);
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var indices = new HashSet<int>();
        var labels = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var property in Properties)
        {
            if (!names.Add(property.Name!) || property.Name == TypeName || typeof(object).GetMembers().Any(x => x.Name == property.Name))
                throw new CodeGenException(property, nameof(property.Name), "Duplicate property or conflict with its containing/inherited type.");

            if (!labels.Add(property.Column ?? property.Name!) || property.Column is not null && !columns.Add(property.Column) || property.Index.HasValue && !indices.Add(property.Index.Value))
                throw new CodeGenException(property, nameof(property.Index), "Duplicate CSV selector.");
        }
    }
}
