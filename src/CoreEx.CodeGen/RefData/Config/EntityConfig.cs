namespace CoreEx.CodeGen.RefData.Config;

/// <summary>
/// Provides the entity code-generation configuration.
/// </summary>
[CodeGenClass("Entity", Title = "Reference-data entity configuration.")]
[CodeGenCategory("Primary", Title = "Provides the _primary_ configuration.")]
[CodeGenCategory("API", Title = "Provides the configuration for the generated API code.")]
[CodeGenCategory("Repository", Title = "Provides the configuration for the generated repository code.")]
[CodeGenCategory("Mapping", Title = "Provides the configuration for the generated mapping code.")]
[CodeGenCategory("Mutable", Title = "Provides the configuration for the generated mutability code.")]
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
    [CodeGenProperty("Primary", Title = "The pluralized reference-data entity (contract) name.", IsImportant = true, Description = "Defaults to `{Name}` with the last word pluralized.")]
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
    [CodeGenProperty("Primary", Title = "The reference-data identifier type.", Options = ["String", "Guid", "Int32", "Int64"], Description = "Defaults to root `{IdType}`.")]
    public string? IdType { get; set; }

    /// <summary>
    /// Gets or sets the default collection sort order.
    /// </summary>
    [JsonPropertyName("collectionSortOrder")]
    [CodeGenProperty("Primary", Title = "The collection sort order.", Options = ["Code", "Id", "Text", "SortOrder"], Description = "This is the collection sort order. Defaults to root `{CollectionSortOrder}`.")]
    public string? CollectionSortOrder { get; set; }

    #region API

    /// <summary>
    /// Gets or sets the route suffix.
    /// </summary>
    [JsonPropertyName("route")]
    [CodeGenProperty("API", Title = "The route suffix.", IsImportant = true, Description = "Defaults to `{Plural}` and root `{RouteConvention}` configuration.")]
    public string? Route { get; set; }

    /// <summary>
    /// Gets or sets the optional attribute.
    /// </summary>
    [JsonPropertyName("attribute")]
    [CodeGenProperty("API", Title = "The optional API controller operation attribute.", Description = "This is the attribute applied as-is to the generated `ReferenceDataController` operation. This is useful for adding the likes of `[Authorize]`.")]
    public string? Attribute { get; set; }

    #endregion

    #region Repository

    /// <summary>
    /// Gets or sets the repository implementation.
    /// </summary>
    [JsonPropertyName("repository")]
    [CodeGenProperty("Repository", Title = "The repository implementation.", IsImportant = true, Options = ["None", "EntityFramework", "Cosmos"], Description = "Defaults to root `{Repository}`.")]
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

    /// <summary>
    /// Gets or sets the pluralized entity name.
    /// </summary>
    [JsonPropertyName("modelPlural")]
    [CodeGenProperty("Repository", Title = "The pluralized reference-data model (persistence) name.", IsImportant = true, Description = "Defaults to `{Model}` with the last word pluralized.")]
    public string? ModelPlural { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the Cosmos persistence model should also be generated.
    /// </summary>
    [JsonPropertyName("cosmosPersistenceModel")]
    [CodeGenProperty("Repository", Title = "Indicates whether the Cosmos persistence model should also be generated.", Description = "Defaults to root `{CosmosPersistenceModel}`.")]
    public bool? CosmosPersistenceModel { get; set; }

    #endregion

    #region Mapping

    /// <summary>
    /// Gets or sets the mapper name.
    /// </summary>
    [JsonPropertyName("mapper")]
    [CodeGenProperty("Mapping", Title = "The mapper name.", IsImportant = true, Description = "This is the .NET mapper name used within the generated code. Defaults to root `{Name}Mapper`.")]
    public string? Mapper { get; set; }

    #endregion

    #region Mutable

    /// <summary>
    /// Gets or sets the reference-data entity mutability.
    /// </summary>
    [JsonPropertyName("mutability")]
    [CodeGenProperty("Mutable", Title = "The reference-data entity mutability.", Options = ["None", "CreateUpdate", "CreateUpdateDelete"], Description = "Defaults to `None`. Requires a `Repository` of `EntityFramework` or `Cosmos`.")]
    public string? Mutability { get; set; }

    /// <summary>
    /// Gets or sets the validator type name.
    /// </summary>
    [JsonPropertyName("validator")]
    [CodeGenProperty("Mutable", Title = "The validator type name used during mutability operations.", Description = "Defaults to `ReferenceDataValidator<{Name}>`. Must have a default constructor.")]
    public string? Validator { get; set; }

    /// <summary>
    /// Gets or sets the optional attribute.
    /// </summary>
    [JsonPropertyName("mutableAttribute")]
    [CodeGenProperty("Mutable", Title = "The mutable API controller class attribute.", Description = "Defaults to `[Authorize]`. A configured value replaces the default attribute applied as-is to the generated `{Name}Controller` class.")]
    public string? MutableAttribute { get; set; }

    #endregion

    #region Exclude

    /// <summary>
    /// Indicates whether to exclude the generation of the API.
    /// </summary>
    [JsonPropertyName("excludeApi")]
    [CodeGenProperty("Exclude", Title = "Indicates whether to exclude the generation of the API.", IsImportant = true, Description = "Defaults to `false`.")]
    public bool? ExcludeApi { get; set; }

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
    /// Gets or sets the .NET type of the identifier.
    /// </summary>
    public string? IdDotNetType { get; set; }

    /// <summary>
    /// Gets or sets the contract inherits base class name.
    /// </summary>
    public string? Inherits { get; set; }

    /// <summary>
    /// Gets or sets the contract collection's base class name.
    /// </summary>
    /// <remarks>Must agree with <see cref="Inherits"/>'s <see cref="IdType"/>: <c>CoreEx.RefData.ReferenceDataCollection&lt;TRef&gt;</c> (single type parameter) only accepts a <c>TRef</c> implementing
    /// <c>IReferenceData&lt;string&gt;</c>, so a non-<c>String</c> <see cref="IdType"/> must instead use the two-type-parameter <c>CoreEx.RefData.ReferenceDataCollection&lt;TId, TRef&gt;</c> - otherwise
    /// the generated collection fails to compile (<c>CS0311</c>) against its own entity's <see cref="Inherits"/> base.</remarks>
    public string? CollectionInherits { get; set; }

    /// <summary>
    /// Gets the C# expression the generated mapper uses to convert the persistence model's <c>Id</c> to the contract's <c>Id</c>.
    /// </summary>
    /// <remarks>A Cosmos DB document <c>id</c> is always a <see cref="string"/> (see <c>CosmosDbModelBase.Id</c>) - independent of the configured <see cref="IdType"/> - so a <see cref="Repository"/> of
    /// <c>Cosmos</c> with a non-<c>String</c> <see cref="IdType"/> requires parsing the persistence model's <c>string</c> <c>Id</c> into the contract's actual <see cref="IdType"/>; a straight assignment
    /// would otherwise fail to compile (e.g. assigning a <see cref="string"/> to a <see cref="Guid"/>-typed <c>Id</c>). Every other combination (including all <c>EntityFramework</c>-backed entities, whose
    /// persistence model's <c>Id</c> column type is expected to already agree with <see cref="IdType"/>) is a direct assignment.</remarks>
    public string? MapperIdExpression { get; set; }

    /// <summary>
    /// Indicates whether the entity is mutable; see <see cref="Mutability"/>.
    /// </summary>
    public bool IsMutable => Mutability switch
    {
        "CreateUpdate" => true,
        "CreateUpdateDelete" => true,
        _ => false
    };

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        Text = DefaultWhereNull(Text, () => OnRamp.Utility.StringConverter.ToSentenceCase(Name!));
        Model = DefaultWhereNull(Model, () => Name);
        IdType = DefaultWhereNull(IdType, () => Root?.IdType);
        CollectionSortOrder = DefaultWhereNull(CollectionSortOrder, () => Root!.CollectionSortOrder);
        Repository = DefaultWhereNull(Repository, () => Root!.Repository);
        Mutability = DefaultWhereNull(Mutability, () => "None");
        Mapper = DefaultWhereNull(Mapper, () => $"{Name}Mapper");
        ExcludeMapper = DefaultWhereNull(ExcludeMapper, () => false);
        Validator = DefaultWhereNull(Validator, () => $"ReferenceDataValidator<{Name}>");
        MutableAttribute = DefaultWhereNull(MutableAttribute, () => "[Authorize]");

        if (IsMutable && Repository is not ("EntityFramework" or "Cosmos"))
            throw new CodeGenException(this, nameof(Mutability), $"Mutability '{Mutability}' requires a '{nameof(Repository)}' of 'EntityFramework' or 'Cosmos'; '{Repository}' is not supported.");

        Plural = DefaultWhereNull(Plural, () =>
        {
            // Best guess by pluralizing the last word of the name.
            var words = OnRamp.Utility.StringConverter.ToSentenceCase(Name!)!.Split(' ').ToList();
            words[^1] = OnRamp.Utility.StringConverter.ToPlural(words[^1]);
            return string.Concat(words);
        });

        ModelPlural = DefaultWhereNull(ModelPlural, () =>
        {
            // Best guess by pluralizing the last word of the name.
            var words = OnRamp.Utility.StringConverter.ToSentenceCase(Model!)!.Split(' ').ToList();
            words[^1] = OnRamp.Utility.StringConverter.ToPlural(words[^1]);
            return string.Concat(words);
        });

        RepositoryName = DefaultWhereNull(RepositoryName, () => Repository switch
        {
            "EntityFramework" => Root!.EntityFrameworkRepositoryName,
            "Cosmos" => Root!.CosmosRepositoryName,
            _ => "??"
        });

        CosmosPersistenceModel = DefaultWhereNull(CosmosPersistenceModel, () => Root!.CosmosPersistenceModel);
        if (CosmosPersistenceModel == true && Repository != "Cosmos") // If the repository is not Cosmos, then we cannot generate the persistence model.
            CosmosPersistenceModel = false;

        Route = DefaultWhereNull(Route, () => Root!.RouteConvention switch
        {
            "KebabCase" => OnRamp.Utility.StringConverter.ToKebabCase(Plural!),
            "SnakeCase" => OnRamp.Utility.StringConverter.ToSnakeCase(Plural!),
            "CamelCase" => OnRamp.Utility.StringConverter.ToCamelCase(Plural!),
            _ => Plural!.ToLower(),
        });

        IdDotNetType = IdType switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Guid" => "Guid",
            _ => "string"
        };

        Inherits = IdType switch
        {
            "Int32" => $"ReferenceData<int, {Name}>",
            "Int64" => $"ReferenceData<long, {Name}>",
            "Guid" => $"ReferenceData<Guid, {Name}>",
            _ => $"ReferenceData<{Name}>"
        };

        CollectionInherits = IdType switch
        {
            "Int32" => $"ReferenceDataCollection<int, {Name}>",
            "Int64" => $"ReferenceDataCollection<long, {Name}>",
            "Guid" => $"ReferenceDataCollection<Guid, {Name}>",
            _ => $"ReferenceDataCollection<{Name}>"
        };

        MapperIdExpression = Repository == "Cosmos"
            ? IdType switch
            {
                "Guid" => "global::System.Guid.Parse(source.Id!)",
                "Int32" => "int.Parse(source.Id!, global::System.Globalization.CultureInfo.InvariantCulture)",
                "Int64" => "long.Parse(source.Id!, global::System.Globalization.CultureInfo.InvariantCulture)",
                _ => "source.Id!"
            }
            : "source.Id!";

        // Load the properties configuration.
        Properties = await PrepareCollectionAsync(Properties).ConfigureAwait(false);
    }
}
