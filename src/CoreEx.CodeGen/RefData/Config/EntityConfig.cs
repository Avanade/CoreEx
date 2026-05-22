namespace CoreEx.CodeGen.RefData.Config;

/// <summary>
/// Provides the entity code-generation configuration.
/// </summary>
[CodeGenClass("Entity", Title = "Reference-data entity configuration.")]
[CodeGenCategory("Primary", Title = "Provides the _primary_ configuration.")]
[CodeGenCategory("API", Title = "Provides the configuration for the generated API code.")]
[CodeGenCategory("Repository", Title = "Provides the configuration for the generated repository code.")]
[CodeGenCategory("Mapping", Title = "Provides the configuration for the generated mapping code.")]
[CodeGenCategory("Exclude", Title = "Provides the configuration for code generation exclusion.")]
[CodeGenCategory("Collections", Title = "Provides the collections configuration.")]
public class EntityConfig : ConfigBase<CodeGenConfig, CodeGenConfig>
{
    /// <summary>
    /// Gets or sets the entity name.
    /// </summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "The reference-data entity (contract) name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the pluralized entity name.
    /// </summary>
    [JsonPropertyName("plural")]
    [CodeGenProperty("Primary", Title = "The pluralized reference-data entity (contract) name.", IsImportant = true, Description = "Defaults to `{Name} with the last word pluralized.")]
    public string? Plural { get; set; }

    /// <summary>
    /// Gets or sets the entity text.
    /// </summary>
    [JsonPropertyName("text")]
    [CodeGenProperty("Primary", Title = "The reference-data entity friendly text.", Description = "Defaults to `{Name}` converted to sentence case. This is primarily used in generated code comments.")]
    public string? Text { get; set; }

    /// <summary>
    /// Gets or sets the identifier type.
    /// </summary>
    [JsonPropertyName("idType")]
    [CodeGenProperty("Primary", Title = "The reference-data identifier type.", IsImportant = true, Options = ["String", "Guid", "Int32", "Int64"], Description = "Defaults to root `{IdType}`.")]
    public string? IdType { get; set; }

    /// <summary>
    /// Gets or sets the default collection sort order.
    /// </summary>
    [JsonPropertyName("collectionSortOrder")]
    [CodeGenProperty("Primary", Title = "The collection sort order.", IsImportant = true, Options = ["Code", "Id", "Text", "SortOrder"], Description = "This is the collection sort order. Defaults to root `{CollectionSortOrder}`.")]
    public string? CollectionSortOrder { get; set; }

    #region API

    /// <summary>
    /// Gets or sets the route suffix.
    /// </summary>
    [JsonPropertyName("route")]
    [CodeGenProperty("API", Title = "The route suffix.", IsImportant = true, Description = "Defaults to `{Plural}` and root `{RouteConvention}` configuration.")]
    public string? Route { get; set; }

    #endregion

    #region Repository

    /// <summary>
    /// Gets or sets the repository implementation.
    /// </summary>
    [JsonPropertyName("repository")]
    [CodeGenProperty("Repository", Title = "The repository implementation.", IsImportant = true, Options = ["None", "EntityFramework"], Description = "Defaults to root `{Repository}`.")]
    public string? Repository { get; set; }

    /// <summary>
    /// Gets or sets the repository parameter name.
    /// </summary>
    [JsonPropertyName("repositoryName")]
    [CodeGenProperty("Repository", Title = "The repository parameter name.", IsImportant = true, Description = "This is the .NET repository parameter name that should be used within the generated code. Defaults from root `{Repository}` and related configuration.")]
    public string? RepositoryName { get; set; }

    /// <summary>
    /// Gets or sets the corresponding repository model name.
    /// </summary>
    [JsonPropertyName("model")]
    [CodeGenProperty("Repository", Title = "The corresponding repository model name.", IsImportant = true, Description = "Defaults to `{Name}` (assumes same).")]
    public string? Model { get; set; }

    #endregion

    #region Mapping

    /// <summary>
    /// Gets or sets the mapper name.
    /// </summary>
    [JsonPropertyName("mapper")]
    [CodeGenProperty("Mapping", Title = "The mapper name.", IsImportant = true, Description = "This is the .NET mapper name used within the generated code. Defaults to root `{Name}Mapper`.")]
    public string? Mapper { get; set; }

    #endregion

    #region Exclude

    /// <summary>
    /// Indicates whether to exclude the generation of the mapper.
    /// </summary>
    [JsonPropertyName("excludeMapper")]
    [CodeGenProperty("Exclude", Title = "Indicates whether to exclude the generation of the mapper.", IsImportant = true, Description = "Defaults to `false`.")]
    public bool? ExcludeMapper { get; set; }

    #endregion

    #region Collections

    /// <summary>
    /// Gets the list of configured properties.
    /// </summary>
    [JsonPropertyName("properties")]
    [CodeGenPropertyCollection("Collections", Title = "The property collection configuration.")]
    public List<PropertyConfig>? Properties { get; set; }

    /// <summary>
    /// Gets the list of properties that are not excluded from the generated contract code.
    /// </summary>
    public List<PropertyConfig>? ContractProperties => Properties?.Where(p => !(p.ExcludeContract ?? false)).ToList() ?? [];

    /// <summary>
    /// Gets the list of properties that are not excluded from the generated mapping code.
    /// </summary>
    public List<PropertyConfig>? MappingProperties => Properties?.Where(p => !(p.ExcludeMapping ?? false)).ToList() ?? [];

    #endregion

    /// <summary>
    /// Gets or sets the contract inherits base class name.
    /// </summary>
    public string? Inherits { get; set; }

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        Text = DefaultWhereNull(Text, () => OnRamp.Utility.StringConverter.ToSentenceCase(Name!));
        Model = DefaultWhereNull(Model, () => Name);
        IdType = DefaultWhereNull(IdType, () => Root?.IdType);
        CollectionSortOrder = DefaultWhereNull(CollectionSortOrder, () => Root!.CollectionSortOrder);
        Repository = DefaultWhereNull(Repository, () => Root!.Repository);
        IdType = DefaultWhereNull(IdType, () => Root?.IdType);
        Mapper = DefaultWhereNull(Mapper, () => $"{Name}Mapper");
        ExcludeMapper = DefaultWhereNull(ExcludeMapper, () => false);

        Plural = DefaultWhereNull(Plural, () =>
        {
            // Best guess by pluralizing the last word of the name.
            var words = OnRamp.Utility.StringConverter.ToSentenceCase(Name!)!.Split(' ').ToList();
            words[^1] = OnRamp.Utility.StringConverter.ToPlural(words[^1]);
            return string.Concat(words);
        });

        RepositoryName = DefaultWhereNull(RepositoryName, () => Repository switch
        {
            "EntityFramework" => Root!.EntityFrameworkRepositoryName,
            _ => "??"
        });

        Route = DefaultWhereNull(Route, () => Root!.RouteConvention switch
        {
            "KebabCase" => OnRamp.Utility.StringConverter.ToKebabCase(Plural!),
            "SnakeCase" => OnRamp.Utility.StringConverter.ToSnakeCase(Plural!),
            "CamelCase" => OnRamp.Utility.StringConverter.ToCamelCase(Plural!),
            _ => Plural!.ToLower(),
        });

        Inherits = IdType switch
        {
            "Int32" => $"ReferenceData<int, {Name}>",
            "Int64" => $"ReferenceData<long, {Name}>",
            "Guid" => $"ReferenceData<Guid, {Name}>",
            _ => $"ReferenceData<{Name}>"
        };

        // Load the properties configuration.
        Properties = await PrepareCollectionAsync(Properties).ConfigureAwait(false);
    }
}