namespace CoreEx.CodeGen.RefData.Config;

/// <summary>
/// Provides the root code-generation configuration.
/// </summary>
[CodeGenClass("CodeGeneration", Title = "Reference-data code-generation.", Description = "This configuration is used for generating reference-data related code. This is a well understood, deterministic pattern, that is best suited for scenarios where reference-data capabilities are required in a consistent manner.")]
[CodeGenCategory("Primary", Title = "Provides the _primary_ configuration.")]
[CodeGenCategory("API", Title = "Provides the configuration for the generated API code.")]
[CodeGenCategory("Repository", Title = "Provides the configuration for the generated repository code.")]
[CodeGenCategory("Paths", Title = "Provides the configuration for the paths used in code generation.")]
[CodeGenCategory("Collections", Title = "Provides the collections configuration.")]
public class CodeGenConfig : ConfigRootBase<CodeGenConfig>
{
    private readonly string _codeGenName = Assembly.GetEntryAssembly()?.GetName().Name ?? "??";

    /// <summary>
    /// Gets the entry assembly name for the initiating code-generation.
    /// </summary>
    public string CodeGenName => _codeGenName;

    /// <summary>
    /// Gets or sets the .NET domain name.
    /// </summary>
    [JsonPropertyName("domain")]
    [CodeGenProperty("Primary", Title = "The domain name.", IsImportant = true, Description = "This is the .NET domain name. Attempts to default from the underlying data project file path; uses the second to last segment of the child-most sub-directory by convention. For example, `/xxx/yyy/My.App.Sales.CodeGen`, the domain would be `Sales`.")]
    public string? Domain { get; set; }

    /// <summary>
    /// Gets or sets the default identifier type.
    /// </summary>
    [JsonPropertyName("idType")]
    [CodeGenProperty("Primary", Title = "The default reference data identifier type.", IsImportant = true, Options = ["String", "Guid", "Int32", "Int64"], Description = "Defaults to `String`.")]
    public string? IdType { get; set; }

    /// <summary>
    /// Gets or sets the default collection sort order.
    /// </summary>
    [JsonPropertyName("collectionSortOrder")]
    [CodeGenProperty("Primary", Title = "The default reference data collection sort order.", IsImportant = true, Options = ["Code", "Id", "Text", "SortOrder"], Description = "This is the default collection sort order. Defaults to `Code`.")]
    public string? CollectionSortOrder { get; set; }

    #region API

    /// <summary>
    /// Gets or sets the route prefix.
    /// </summary>
    [JsonPropertyName("route")]
    [CodeGenProperty("API", Title = "The route prefix.", IsImportant = true, Description = "Defaults to `/api/refdata`.")]
    public string? Route { get; set; }

    /// <summary>
    /// Gets or sets the route convention.
    /// </summary>
    [JsonPropertyName("routeConvention")]
    [CodeGenProperty("API", Title = "The route naming convention where not directly specified.", IsImportant = true, Options = ["KebabCase", "SnakeCase", "CamelCase", "Lowercase"], Description = "Defaults to `KebabCase`.")]
    public string? RouteConvention { get; set; }

    #endregion

    #region Repository

    /// <summary>
    /// Gets or sets the default repository implementation.
    /// </summary>
    [JsonPropertyName("repository")]
    [CodeGenProperty("Repository", Title = "The default repository implementation.", IsMandatory = true, Options = ["None", "EntityFramework"])]
    public string? Repository { get; set; }

    /// <summary>
    /// Gets or sets the default Entity Framework repository parameter name.
    /// </summary>
    [JsonPropertyName("entityFrameworkRepositoryName")]
    [CodeGenProperty("Repository", Title = "The default Entity Framework (EF) repository parameter name.", IsImportant = true, Description = "This is the .NET Entity Framework (EF) repository parameter name that should be used within the generated code. Defaults to `ef`.")]
    public string? EntityFrameworkRepositoryName { get; set; }

    #endregion

    #region Paths

    /// <summary>
    /// Gets or sets the relative path to the contracts-related .NET project.
    /// </summary>
    [JsonPropertyName("contractsProjectPath")]
    [CodeGenProperty("Paths", Title = "The relative path for the .NET contracts-related project.", Description = "Defaults to `Contracts`.")]
    public string? ContractsProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the API-related .NET project.
    /// </summary>
    [JsonPropertyName("apiProjectPath")]
    [CodeGenProperty("Paths", Title = "The relative path for the .NET API-related project.", Description = "Defaults to `Api`.")]
    public string? ApiProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the application-related .NET project.
    /// </summary>
    [JsonPropertyName("applicationProjectPath")]
    [CodeGenProperty("Paths", Title = "The relative path for the .NET application-related project.", Description = "Defaults to `Application`.")]
    public string? ApplicationProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the relative path to the data-related .NET project.
    /// </summary>
    [JsonPropertyName("dataProjectPath")]
    [CodeGenProperty("Paths", Title = "The relative path for the .NET data-related project.", Description = "Defaults to automatic inference using expected name of `Infrastructure`.")]
    public string? DataProjectPath { get; set; }

    /// <summary>
    /// Gets or sets the path to append to the <see cref="DataProjectPath"/> for the .NET generated repository code.
    /// </summary>
    [JsonPropertyName("dataRepositoriesPath")]
    [CodeGenProperty("Paths", Title = "The path to append to the `{DotNetDataProjectPath}` for the .NET generated repository code.", Description = "Defaults to `Repositories`.")]
    public string? DataRepositoriesPath { get; set; }

    /// <summary>
    /// Gets or sets the path to append to the <see cref="DataProjectPath"/> for the .NET data models code.
    /// </summary>
    [JsonPropertyName("dataModelsPath")]
    [CodeGenProperty("Paths", Title = "The path to append to the `{DotNetDataProjectPath}` for the .NET generated models code.", Description = "Defaults to `Persistence`.")]
    public string? DataModelsPath { get; set; }

    /// <summary>
    /// Gets or sets the path to append to the <see cref="DataProjectPath"/> for the .NET generated mapping code.
    /// </summary>
    [JsonPropertyName("dataMappingPath")]
    [CodeGenProperty("Paths", Title = "The path to append to the `{DotNetDataProjectPath}` for the .NET generated mapping code.", Description = "Defaults to `Mapping`.")]
    public string? DataMappingPath { get; set; }

    #endregion

    #region Collections

    /// <summary>
    /// Gets the list of configured entities.
    /// </summary>
    [JsonPropertyName("entities")]
    [CodeGenPropertyCollection("Collections", Title = "The entity collection configuration.", IsImportant = true)]
    public List<EntityConfig>? Entities { get; set; }

    /// <summary>
    /// Gets the list of configured entities that have a repository implementation specified (i.e. not 'None').
    /// </summary>
    public List<EntityConfig>? EntitiesWithRepository => Entities?.Where(x => x.Repository != "None").ToList();

    #endregion

    /// <summary>
    /// Gets the <see cref="DirectoryInfo"/> for the initiating code-generation project itself.
    /// </summary>
    public DirectoryInfo? CodeGenDirectory { get; private set; }

    /// <summary>
    /// Gets the <see cref="DirectoryInfo"/> for the 'Contracts' project.
    /// </summary>
    public DirectoryInfo? ContractsDirectory { get; private set; }

    /// <summary>
    /// Gets the <see cref="DirectoryInfo"/> for the 'API' project.
    /// </summary>
    public DirectoryInfo? ApiDirectory { get; private set; }

    /// <summary>
    /// Gets the <see cref="DirectoryInfo"/> for the 'Application' project.
    /// </summary>
    public DirectoryInfo? ApplicationDirectory { get; private set; }

    /// <summary>
    /// Gets the <see cref="DirectoryInfo"/> for the 'Data' project.
    /// </summary>
    public DirectoryInfo? DataDirectory { get; private set; }

    /// <summary>
    /// Gets the .NET namespace for the generated contracts code.
    /// </summary>
    public string? ContractsNamespace => ContractsDirectory?.Name;

    /// <summary>
    /// Gets the .NET namespace for the generated API code.
    /// </summary>
    public string? ApiNamespace => ApiDirectory?.Name;

    /// <summary>
    /// Gets the .NET namespace for the generated application code.
    /// </summary>
    public string? ApplicationNamespace => ApplicationDirectory?.Name;

    /// <summary>
    /// Gets or sets the .NET namespace for the generated data repositories code.
    /// </summary>
    public string? DataRepositoriesNamespace { get; set; }

    /// <summary>
    /// Gets or sets the .NET namespace for the generated data mapping code.
    /// </summary>
    public string? DataMappingNamespace { get; set; }

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        // Get the base paths, etc.
        CodeGenDirectory = CodeGenArgs?.OutputDirectory ?? throw new InvalidOperationException("The 'OutputDirectory' property of the 'CodeGenArgs' is not set.");
        var parts = CodeGenDirectory.Name.Split('.');

        // Using the Contracts project relative path, get the absolute path and ensure it exists.
        if (ContractsProjectPath is not null)
        {
            if (!ContractsProjectPath.StartsWith('.'))
                throw new CodeGenException(this, nameof(ContractsProjectPath), $"'{ContractsProjectPath}' should be a relative path to '{CodeGenArgs.OutputDirectory.FullName}'.");
        }
        else
            ContractsProjectPath = Path.Combine("..", string.Join('.', parts.Take(parts.Length - 1).Append("Contracts")));

        ContractsDirectory = new DirectoryInfo(Path.Combine(CodeGenArgs.OutputDirectory.FullName, ContractsProjectPath));
        if (!ContractsDirectory.Exists)
            throw new CodeGenException(this, nameof(ContractsProjectPath), $"'{ContractsProjectPath}' does not exist relative to '{CodeGenArgs.OutputDirectory.FullName}'.");

        // Using the API project relative path, get the absolute path and ensure it exists.
        if (ApiProjectPath is not null)
        {
            if (!ApiProjectPath.StartsWith('.'))
                throw new CodeGenException(this, nameof(ApiProjectPath), $"'{ApiProjectPath}' should be a relative path to '{CodeGenArgs.OutputDirectory.FullName}'.");
        }
        else
            ApiProjectPath = Path.Combine("..", string.Join('.', parts.Take(parts.Length - 1).Append("Api")));

        ApiDirectory = new DirectoryInfo(Path.Combine(CodeGenArgs.OutputDirectory.FullName, ApiProjectPath));
        if (!ApiDirectory.Exists)
            throw new CodeGenException(this, nameof(ApiProjectPath), $"'{ApiProjectPath}' does not exist relative to '{CodeGenArgs.OutputDirectory.FullName}'.");

        // Using the Application project relative path, get the absolute path and ensure it exists.
        if (ApplicationProjectPath is not null)
        {
            if (!ApplicationProjectPath.StartsWith('.'))
                throw new CodeGenException(this, nameof(ApplicationProjectPath), $"'{ApplicationProjectPath}' should be a relative path to '{CodeGenArgs.OutputDirectory.FullName}'.");
        }
        else
            ApplicationProjectPath = Path.Combine("..", string.Join('.', parts.Take(parts.Length - 1).Append("Application")));

        ApplicationDirectory = new DirectoryInfo(Path.Combine(CodeGenArgs.OutputDirectory.FullName, ApplicationProjectPath));
        if (!ApplicationDirectory.Exists)
            throw new CodeGenException(this, nameof(ApplicationProjectPath), $"'{ApplicationProjectPath}' does not exist relative to '{CodeGenArgs.OutputDirectory.FullName}'.");

        // Using the Data project relative path, get the absolute path and ensure it exists; also default the namespace(s).
        if (DataProjectPath is not null)
        {
            if (!DataProjectPath.StartsWith('.'))
                throw new CodeGenException(this, nameof(DataProjectPath), $"'{DataProjectPath}' should be a relative path to '{CodeGenArgs.OutputDirectory.FullName}'.");
        }
        else
            DataProjectPath = Path.Combine("..", string.Join('.', parts.Take(parts.Length - 1).Append("Infrastructure")));

        DataDirectory = new DirectoryInfo(Path.Combine(CodeGenArgs.OutputDirectory.FullName, DataProjectPath));
        if (!DataDirectory.Exists)
            throw new CodeGenException(this, nameof(DataProjectPath), $"'{DataProjectPath}' does not exist relative to '{CodeGenArgs.OutputDirectory.FullName}'.");

        DataRepositoriesPath = DefaultWhereNull(DataRepositoriesPath, () => "Repositories");
        DataModelsPath = DefaultWhereNull(DataModelsPath, () => "Persistence");
        DataMappingPath = DefaultWhereNull(DataMappingPath, () => "Mapping");

        DataRepositoriesNamespace = $"{DataDirectory.Name}.{DataRepositoriesPath}";
        DataMappingNamespace = $"{DataDirectory.Name}.{DataMappingPath}";

        // Default the domain name from the file path (2nd to last part) if not explicitly set.
        Domain = DefaultWhereNull(Domain, () => parts.Length >= 2 ? parts[^2] : null) ?? throw new CodeGenException(this, nameof(Domain), $"Could not be defaulted from the file path; please explicitly set the property in the configuration.");
        EntityFrameworkRepositoryName = DefaultWhereNull(EntityFrameworkRepositoryName, () => "ef");
        IdType = DefaultWhereNull(IdType, () => "String");
        CollectionSortOrder = DefaultWhereNull(CollectionSortOrder, () => "Code");
        Route = DefaultWhereNull(Route, () => "/api/refdata");
        RouteConvention = DefaultWhereNull(RouteConvention, () => "KebabCase");

        // Load the entities configuration.
        Entities = await PrepareCollectionAsync(Entities).ConfigureAwait(false);
    }
}