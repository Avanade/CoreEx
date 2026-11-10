namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines one host-neutral flat, unordered routed, or hierarchical CSV integration.</summary>
[CodeGenClass("CodeGeneration", Title = "File processing code-generation.")]
[CodeGenCategory("Primary", Title = "Definition identity.")]
[CodeGenCategory("Format", Title = "Input format.")]
[CodeGenCategory("Processing", Title = "Default invocation policy and operating budgets.")]
[CodeGenCategory("Limits", Title = "Format-neutral processing budgets.")]
[CodeGenCategory("Csv", Title = "CSV parser, routing, and assembly settings.")]
[CodeGenCategory("Paths", Title = "Destination project and subfolder.")]
[CodeGenCategory("Namespaces", Title = "Explicit generated namespace.")]
[CodeGenCategory("Collections", Title = "Typed wire records.")]
public class CodeGenConfig : ConfigRootBase<CodeGenConfig>
{
    private readonly string _codeGenName = Assembly.GetEntryAssembly()?.GetName().Name ?? "??";

    /// <summary>Gets the entry assembly name for the initiating code-generation.</summary>
    public string CodeGenName => _codeGenName;

    /// <summary>Gets or sets the integration name, type prefix, folder default, and processing identity.</summary>
    [JsonPropertyName("name")]
    [CodeGenProperty("Primary", Title = "Integration name.", IsMandatory = true)]
    public string? Name { get; set; }

    /// <summary>Gets or sets the opaque definition revision.</summary>
    [JsonPropertyName("version")]
    [CodeGenProperty("Primary", Title = "Definition revision.", IsMandatory = true, Description = "Not a package/source version, layout selector, or business deduplication key.")]
    public string? Version { get; set; }

    /// <summary>Gets or sets the existing runtime project directory.</summary>
    [JsonPropertyName("runtimeProjectPath")]
    [CodeGenProperty("Paths", Title = "Runtime project directory.", IsMandatory = true, Description = "Relative to the CodeGen output/project directory, or absolute. No Functions project is required.")]
    public string? RuntimeProjectPath { get; set; }

    /// <summary>Gets or sets the runtime subfolder.</summary>
    [JsonPropertyName("runtimePath")]
    [CodeGenProperty("Paths", Title = "Runtime subfolder.", Description = "Defaults to Files/<Name>; must remain inside runtimeProjectPath.")]
    public string? RuntimePath { get; set; }

    /// <summary>Gets or sets the generated namespace.</summary>
    [JsonPropertyName("runtimeNamespace")]
    [CodeGenProperty("Namespaces", Title = "Generated namespace.", Description = "Defaults to project directory name plus runtimePath segments. Explicit override avoids assumptions about MSBuild RootNamespace.")]
    public string? RuntimeNamespace { get; set; }

    /// <summary>Gets or sets the selected input format; omitted values use CSV during migration.</summary>
    [JsonPropertyName("format")]
    [CodeGenProperty("Format", Title = "Input format.", Options = ["Csv"], Description = "Defaults to Csv. Other formats are introduced in later phases.")]
    public string? Format { get; set; }

    /// <summary>Gets or sets shared operating budgets.</summary>
    [JsonPropertyName("limits")]
    [CodeGenProperty("Limits", Title = "Format-neutral processing budgets.")]
    public LimitsConfig? Limits { get; set; }

    /// <summary>Gets or sets CSV-specific parsing and assembly settings.</summary>
    [JsonPropertyName("csv")]
    [CodeGenProperty("Csv", Title = "CSV parser, routing, and assembly settings.")]
    public CsvDefinitionConfig? Csv { get; set; }

    /// <summary>Gets or sets file-level metadata declarations.</summary>
    [JsonPropertyName("metadata")]
    [CodeGenPropertyCollection("Processing", Title = "File metadata declarations.")]
    public List<MetadataConfig>? Metadata { get; set; }

    /// <summary>Gets or sets the default processing mode.</summary>
    [JsonPropertyName("mode")]
    [CodeGenProperty("Processing", Title = "Default processing mode.", Options = ["ValidateOnly", "ValidateThenExecute", "ValidateAndExecute"], Description = "Defaults to ValidateOnly. Valid-unit reporting is an optional processor hook in every mode; ValidateAndExecute validates and executes each unit in one pass.")]
    public string? Mode { get; set; }

    /// <summary>Gets or sets the default content-error policy.</summary>
    [JsonPropertyName("errorMode")]
    [CodeGenProperty("Processing", Title = "Default content policy.", Options = ["FailFast", "CollectErrors"], Description = "Defaults to FailFast.")]
    public string? ErrorMode { get; set; }

    /// <summary>Gets or sets typed wire records.</summary>
    [JsonPropertyName("records")]
    [CodeGenPropertyCollection("Collections", Title = "Wire record definitions.", IsMandatory = true)]
    public List<RecordConfig>? Records { get; set; }

    /// <summary>Gets the resolved destination project.</summary>
    public DirectoryInfo RuntimeDirectory { get; private set; } = null!;

    /// <summary>Gets the resolved output folder.</summary>
    public DirectoryInfo OutputDirectory { get; private set; } = null!;

    /// <summary>Gets whether routed hierarchy processing is required.</summary>
    public bool IsHierarchical => Csv?.Structure is not null;

    /// <summary>Gets whether records are routed without a hierarchy, each forming its own business unit.</summary>
    public bool IsUnordered => Csv?.Structure is null && Csv?.DiscriminatorIndex.HasValue == true;

    /// <summary>Gets whether exact discriminator routing is configured.</summary>
    public bool IsRouted => IsHierarchical || IsUnordered;

    /// <summary>Gets the business-unit type.</summary>
    public string UnitType => IsUnordered ? Name + "Record" : Csv?.Structure?.RootNode?.TypeName ?? Records![0].TypeName;

    /// <summary>Gets assembled positions, excluding leaves.</summary>
    public IEnumerable<NodeConfig> Groups => Csv?.Structure?.Nodes.Where(x => x.HasChildren) ?? [];

    /// <summary>Gets the safe definition literal.</summary>
    public string NameLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Name!);

    /// <summary>Gets the safe revision literal.</summary>
    public string VersionLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Version!);

    /// <summary>Gets the generated top-level names, including handwritten extension-point ownership.</summary>
    public IEnumerable<string> TypeNames => Records!.SelectMany(x => new[] { x.TypeName, x.TypeName + "Mapping", x.TypeName + "Validator" })
        .Concat(Groups.Select(x => x.TypeName)).Concat(IsUnordered ? new[] { Name + "Record", Name + "HandlerBase" } : [])
        .Concat(new[] { Name + "Context", Name + "Processor", Name + "Handler" });

    /// <summary>Gets every owned file path for batch collision checks.</summary>
    public IEnumerable<string> OutputFiles => TypeNames.Select(x => Path.Combine(OutputDirectory.FullName, x + (x == Name + "Handler" ? ".cs" : ".g.cs")));

    /// <inheritdoc/>
    protected override async Task PrepareAsync()
    {
        RejectUnknownProperties();
        Name = ConfigValidation.Identifier(this, nameof(Name), Name);
        Version = RequireText(nameof(Version), Version);
        var basis = CodeGenArgs?.OutputDirectory ?? throw new CodeGenException(this, nameof(RuntimeProjectPath), "CodeGen output/project directory is required.");
        RuntimeDirectory = new DirectoryInfo(Path.GetFullPath(RequireText(nameof(RuntimeProjectPath), RuntimeProjectPath), basis.FullName));
        if (!RuntimeDirectory.Exists || RuntimeDirectory.GetFiles("*.csproj").Length != 1)
            throw new CodeGenException(this, nameof(RuntimeProjectPath), "Expected an existing directory containing exactly one runtime .csproj.");

        RuntimePath = RequireText(nameof(RuntimePath), DefaultWhereNull(RuntimePath, () => Path.Combine("Files", Name)));
        RuntimePath = RuntimePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        OutputDirectory = new DirectoryInfo(Path.GetFullPath(RuntimePath, RuntimeDirectory.FullName));
        if (Path.IsPathRooted(RuntimePath) || !OutputDirectory.FullName.StartsWith(RuntimeDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new CodeGenException(this, nameof(RuntimePath), "Output must be a nonempty subfolder inside the runtime project.");

        RuntimeNamespace = DefaultWhereNull(RuntimeNamespace, () => RuntimeDirectory.Name + "." + string.Join('.', Path.GetRelativePath(RuntimeDirectory.FullName, OutputDirectory.FullName).Split(Path.DirectorySeparatorChar)));
        RuntimeNamespace = ConfigValidation.Namespace(this, nameof(RuntimeNamespace), RuntimeNamespace);
        Format = DefaultWhereNull(Format, () => "Csv");
        if (Format != "Csv")
            throw new CodeGenException(this, nameof(Format), "Only Csv is supported in this phase.");

        Mode = DefaultWhereNull(Mode, () => "ValidateOnly");
        ErrorMode = DefaultWhereNull(ErrorMode, () => "FailFast");
        if (Mode is not ("ValidateOnly" or "ValidateThenExecute" or "ValidateAndExecute"))
            throw new CodeGenException(this, nameof(Mode), "The processing mode must be ValidateOnly, ValidateThenExecute, or ValidateAndExecute.");

        if (ErrorMode is not ("FailFast" or "CollectErrors"))
            throw new CodeGenException(this, nameof(ErrorMode), "The content policy must be FailFast or CollectErrors.");

        Limits ??= new();
        await Limits.PrepareAsync(this, this).ConfigureAwait(false);
        Csv ??= new();
        await Csv.PrepareAsync(this, this).ConfigureAwait(false);
        if (Metadata?.Any(x => x is null) == true)
            throw new CodeGenException(this, nameof(Metadata), "Null metadata declarations are not supported.");

        Metadata = await PrepareCollectionAsync(Metadata).ConfigureAwait(false);
        if (Metadata.Count > 0)
        {
            if (!IsRouted)
                throw new CodeGenException(this, nameof(Metadata), "CSV metadata declarations require routed input.");

            if (Metadata.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != Metadata.Count
                || Metadata.Select(x => x.Record).Distinct(StringComparer.Ordinal).Count() != Metadata.Count)
                throw new CodeGenException(this, nameof(Metadata), "Metadata names and wire records must be unique.");

            if (Metadata.Any(x => Records?.Any(r => r is not null && r.Name == x.Record) != true))
                throw new CodeGenException(this, nameof(Metadata), "Each metadata declaration must reference a declared wire record.");

            if (Metadata.Count(x => x.Csv?.Position == "First") > 1 || Metadata.Count(x => x.Csv?.Position == "Last") > 1)
                throw new CodeGenException(this, nameof(Metadata), "Only one metadata declaration may use First and one may use Last position.");

        }

        if (Records is null || Records.Count == 0 || Records.Any(x => x is null))
            throw new CodeGenException(this, nameof(Records), "At least one non-null record is required.");

        Records = await PrepareCollectionAsync(Records).ConfigureAwait(false);
        if (Records.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Records.Count)
            throw new CodeGenException(this, nameof(Records), "Record names must be unique ignoring case.");

        if (!IsRouted)
        {
            if (Records.Count != 1 || Metadata.Count > 0 || Csv.DiscriminatorIndex.HasValue || Records.Any(x => x.Csv?.Match is not null || x.ErrorMode is not null))
                throw new CodeGenException(this, nameof(Csv), "Flat input requires one record and no routing settings.");

            if (Csv.HeaderPolicy == "Exact" && Records[0].Properties!.Any(x => x.Csv?.Index.HasValue == true))
                throw new CodeGenException(this, nameof(Csv), "Exact header validation requires every flat property to use a named source field.");
        }
        else
        {
            if (Csv.HeaderPolicy != "AllowAdditionalColumns")
                throw new CodeGenException(this, nameof(Csv), "Named header policies apply only to flat files.");

            if (!Csv.DiscriminatorIndex.HasValue || Csv.DiscriminatorIndex < 0 || Records.Any(x => string.IsNullOrWhiteSpace(x.Csv?.Match) || x.Properties!.Any(p => p.Csv?.Index is null || p.Csv.Index == Csv.DiscriminatorIndex))
                || Records.Select(x => x.Csv!.Match).Distinct(StringComparer.Ordinal).Count() != Records.Count)
                throw new CodeGenException(this, nameof(Csv), "Routed files require a nonnegative discriminator, exact unique matches, and positional fields excluding the discriminator.");

            if (Csv.Structure is not null)
                await Csv.Structure.PrepareAsync(this, this).ConfigureAwait(false);
            else if (Records.Count(x => x.Csv?.Position == "First" && !Metadata.Any(m => m.Record == x.Name)) + Metadata.Count(x => x.Csv?.Position == "First") > 1
                || Records.Count(x => x.Csv?.Position == "Last" && !Metadata.Any(m => m.Record == x.Name)) + Metadata.Count(x => x.Csv?.Position == "Last") > 1)
                throw new CodeGenException(this, nameof(Records), "Only one business or metadata record may use First and one may use Last position.");

            if (IsUnordered && Metadata.Any(m => m.RecordConfig.Csv?.Cardinality != "Many" || m.RecordConfig.Csv?.Position != "Any"))
                throw new CodeGenException(this, nameof(Metadata), "Metadata occurrence and position rules belong to the metadata declaration, not its unordered record route.");

            if (Csv.Structure is not null)
            {
                var used = Csv.Structure.Nodes.Select(x => x.Record).Concat(Metadata?.Select(x => x.Record) ?? []).ToArray();
                if (used.Distinct(StringComparer.Ordinal).Count() != used.Length || used.Length != Records.Count)
                    throw new CodeGenException(this, nameof(Csv), "Use every declared record exactly once across the business structure and metadata.");
            }
        }

        if (TypeNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != TypeNames.Count())
            throw new CodeGenException(this, nameof(Records), "Generated type/file names collide.");

        foreach (var typeName in TypeNames)
            ConfigValidation.GeneratedTypeName(this, nameof(Records), typeName);
    }
}
