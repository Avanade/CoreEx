namespace CoreEx.CodeGen.RefData.Config;

/// <summary>
/// Provides the property code-generation configuration.
/// </summary>
[CodeGenClass("Property", Title = "Reference-data property configuration.")]
[CodeGenCategory("Primary", Title = "Provides the _primary_ configuration.")]
[CodeGenCategory("Repository", Title = "Provides the configuration for the generated repository code.")]
[CodeGenCategory("Exclude", Title = "Provides the configuration for code generation exclusion.")]
public class PropertyConfig : ConfigBase<CodeGenConfig, EntityConfig>
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "The property name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    [JsonPropertyName("type")]
    [CodeGenProperty("Primary", Title = "The property type.", IsImportant = true, Description = "Defaults to `string?`. Supports the following conventions: `^`-prefix for reference-data and `?`-suffix for nullable.")]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the property text.
    /// </summary>
    [JsonPropertyName("text")]
    [CodeGenProperty("Primary", Title = "The property text.", Description = "Defaults to `{Name}` converted to sentence case. This is primarily used in generated code comments.")]
    public string? Text { get; set; }

    #region Repository

    /// <summary>
    /// Gets or sets the corresponding data model name.
    /// </summary>
    [JsonPropertyName("model")]
    [CodeGenProperty("Repository", Title = "The corresponding data model property name.", Description = "Defaults to `{Name}` (assumes same).")]
    public string? Model { get; set; }

    #endregion

    #region Exclude

    /// <summary>
    /// Indicates whether to exclude the property from the generated contract code.
    /// </summary>
    [JsonPropertyName("excludeContract")]
    [CodeGenProperty("Exclude", Title = "Indicates whether to exclude the property from the generated contract code.", Description = "Defaults to `false`.")]
    public bool? ExcludeContract { get; set; }

    /// <summary>
    /// Indicates whether to exclude the property from the generated mapping code.
    /// </summary>
    [JsonPropertyName("excludeMapping")]
    [CodeGenProperty("Exclude", Title = "Indicates whether to exclude the property from the generated mapping code.", Description = "Defaults to `false`.")]
    public bool? ExcludeMapping { get; set; }

    #endregion

    /// <summary>
    /// Indicates whether the type is reference data.
    /// </summary>
    public bool IsRefData { get; set; }

    /// <summary>
    /// Indicates whether the type is nullable.
    /// </summary>
    public bool IsNullable { get; set; }

    /// <inheritdoc/>
    protected override Task PrepareAsync()
    {
        // Determine the type, reference-data and nullability information.
        var orig = Type = DefaultWhereNull(Type, () => "string?");

        if (!string.IsNullOrEmpty(Type) && Type.StartsWith('^'))
        {
            Type = Type[1..];
            IsRefData = true;
        }

        if (!string.IsNullOrEmpty(Type) && Type.EndsWith('?'))
        {
            Type = Type[..^1];
            IsNullable = true;
        }

        if (string.IsNullOrEmpty(Type))
            throw new CodeGenException(this, nameof(Type), $"After reference-data and nullability processing, the resulting type cannot be null or empty; original value: {orig}.");

        // Others...
        Text = DefaultWhereNull(Text, () => OnRamp.Utility.StringConverter.ToSentenceCase(Name!));
        Model = DefaultWhereNull(Model, () => Name);
        ExcludeContract = DefaultWhereNull(ExcludeContract, () => false);
        ExcludeMapping = DefaultWhereNull(ExcludeMapping, () => false);

        return Task.CompletedTask;
    }
}