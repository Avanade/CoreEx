namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines one host-neutral CSV, JSON document, or JSONL integration.</summary>
[CodeGenClass("CodeGeneration", Title = "File processing code-generation.")]
[CodeGenCategory("Primary", Title = "Definition identity.")]
[CodeGenCategory("Format", Title = "Input format.")]
[CodeGenCategory("Processing", Title = "Default invocation policy.")]
[CodeGenCategory("Csv", Title = "CSV parser, routing, and assembly settings.")]
[CodeGenCategory("Json", Title = "JSON mapping, document selection, and JSONL framing.")]
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
    [CodeGenProperty("Format", Title = "Input format.", Options = ["Csv", "Json", "Jsonl"], Description = "Defaults to Csv. XML is not supported.")]
    public string? Format { get; set; }

    /// <summary>Gets or sets CSV-specific parsing and assembly settings.</summary>
    [JsonPropertyName("csv")]
    [CodeGenProperty("Csv", Title = "CSV parser, routing, and assembly settings.")]
    public CsvDefinitionConfig? Csv { get; set; }

    /// <summary>Gets or sets shared JSON mapping settings.</summary>
    [JsonPropertyName("json")]
    [CodeGenProperty("Json", Title = "Shared JSON mapping settings.", Description = "Shared JSON token conversion, unknown-member policy, and parser depth.")]
    public JsonConfig? Json { get; set; }

    /// <summary>Gets or sets JSONL framing/routing settings.</summary>
    [JsonPropertyName("jsonl")]
    [CodeGenProperty("Json", Title = "JSONL framing and routing.")]
    public JsonLinesConfig? Jsonl { get; set; }

    /// <summary>Gets or sets JSON document or JSONL business record selection.</summary>
    [JsonPropertyName("unit")]
    [CodeGenProperty("Json", Title = "Business object selection.")]
    public UnitConfig? Unit { get; set; }

    /// <summary>Gets whether nested JSONL generation is selected.</summary>
    public bool IsJsonl => Format == "Jsonl";

    /// <summary>Gets whether streamed JSON document input is selected.</summary>
    public bool IsJsonDocument => Format == "Json";

    /// <summary>Gets whether either JSON input format is selected.</summary>
    public bool IsJson => IsJsonl || IsJsonDocument;

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
    public string UnitType => IsJson ? Unit!.RecordConfig.TypeName : IsUnordered ? Name + "Record" : Csv?.Structure?.RootNode?.TypeName ?? Records![0].TypeName;

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
        if (Format is not ("Csv" or "Json" or "Jsonl"))
            throw new CodeGenException(this, nameof(Format), "Only Csv, Json, and Jsonl are currently supported.");

        if (IsJson && Csv is not null || !IsJson && (Json is not null || Jsonl is not null || Unit is not null)
            || IsJsonDocument && Jsonl is not null)
            throw new CodeGenException(this, nameof(Format), "Format-specific blocks do not apply to the selected format.");

        Mode = DefaultWhereNull(Mode, () => "ValidateOnly");
        ErrorMode = DefaultWhereNull(ErrorMode, () => "FailFast");
        if (Mode is not ("ValidateOnly" or "ValidateThenExecute" or "ValidateAndExecute"))
            throw new CodeGenException(this, nameof(Mode), "The processing mode must be ValidateOnly, ValidateThenExecute, or ValidateAndExecute.");

        if (ErrorMode is not ("FailFast" or "CollectErrors"))
            throw new CodeGenException(this, nameof(ErrorMode), "The content policy must be FailFast or CollectErrors.");

        if (IsJson)
        {
            await PrepareJsonAsync().ConfigureAwait(false);
            ValidateTypeNames();
            return;
        }

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
            if (Records.Count != 1 || Metadata.Count > 0 || Csv.DiscriminatorIndex.HasValue || Records.Any(x => x.Csv?.Match is not null))
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

        ValidateTypeNames();
    }

    /// <summary>Checks all generated names after format-specific preparation.</summary>
    private void ValidateTypeNames()
    {
        if (TypeNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != TypeNames.Count())
            throw new CodeGenException(this, nameof(Records), "Generated type/file names collide.");

        foreach (var typeName in TypeNames)
            ConfigValidation.GeneratedTypeName(this, nameof(Records), typeName);
    }

    /// <summary>Resolves reusable nested types separately from their independently named occurrences.</summary>
    private async Task PrepareJsonAsync()
    {
        Json ??= new();
        if (IsJsonl)
            Jsonl ??= new();

        Unit ??= new();
        await Json.PrepareAsync(this, this).ConfigureAwait(false);
        if (IsJsonl)
            await Jsonl!.PrepareAsync(this, this).ConfigureAwait(false);

        await Unit.PrepareAsync(this, this).ConfigureAwait(false);
        if (Records is null || Records.Count == 0 || Records.Any(x => x is null))
            throw new CodeGenException(this, nameof(Records), "At least one non-null record is required.");

        if (Metadata?.Any(x => x is null) == true)
            throw new CodeGenException(this, nameof(Metadata), "Null metadata declarations are not supported.");

        Metadata = await PrepareCollectionAsync(Metadata).ConfigureAwait(false);
        Records = await PrepareCollectionAsync(Records).ConfigureAwait(false);
        if (Records.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Records.Count)
            throw new CodeGenException(this, nameof(Records), "Record names must be unique ignoring case.");

        var records = Records.ToDictionary(x => x.Name!, StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        void Visit(RecordConfig record)
        {
            if (!visiting.Add(record.Name!))
                throw new CodeGenException(record, nameof(Records), "Cyclic nested record references are not supported.");

            foreach (var name in record.Properties!.Select(x => x.Record ?? x.Collection?.Record).Where(x => x is not null))
            {
                if (!records.TryGetValue(name!, out var child))
                    throw new CodeGenException(record, nameof(Records), "Nested references must name a declared record.");

                if (!visited.Contains(name!))
                    Visit(child);
            }

            visiting.Remove(record.Name!);
            visited.Add(record.Name!);
        }

        foreach (var record in Records)
            Visit(record);

        var nested = Records.SelectMany(x => x.Properties!).Select(x => x.Record ?? x.Collection?.Record).Where(x => x is not null).ToHashSet(StringComparer.Ordinal);
        var eligible = Records.Where(x => !nested.Contains(x.Name) && !Metadata.Any(m => m.Record == x.Name)).ToArray();
        if (Unit.Record is null && eligible.Length == 1)
            Unit.Record = eligible[0].Name;

        if (Unit.Record is null || !records.ContainsKey(Unit.Record) || Metadata.Any(x => x.Record == Unit.Record))
            throw new CodeGenException(this, nameof(Unit), "Specify a declared non-metadata unit.record; inference requires one eligible business record.");

        if (IsJsonDocument)
        {
            if (Metadata.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != Metadata.Count
                || Metadata.Select(x => x.Record).Distinct(StringComparer.Ordinal).Count() != Metadata.Count
                || Metadata.Any(x => !records.ContainsKey(x.Record!)))
                throw new CodeGenException(this, nameof(Metadata), "Metadata names/records must be unique and declared.");

            var unitSegments = ParsePointer(Unit.UnitPath ?? string.Empty);
            foreach (var metadata in Metadata)
            {
                var metadataSegments = ParsePointer(metadata.Json!.Path!);
                if (Overlaps(unitSegments, metadataSegments) || Metadata.Any(other => !ReferenceEquals(other, metadata)
                    && Overlaps(metadataSegments, ParsePointer(other.Json!.Path!))))
                    throw new CodeGenException(metadata, nameof(Metadata), "Unit and metadata JSON Pointers must be disjoint and non-overlapping.");
            }

            return;
        }

        if (Metadata.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != Metadata.Count
            || Metadata.Select(x => x.Record).Distinct(StringComparer.Ordinal).Count() != Metadata.Count || Metadata.Any(x => !records.ContainsKey(x.Record!))
            || Metadata.Count(x => x.Jsonl?.Position == "First") > 1 || Metadata.Count(x => x.Jsonl?.Position == "Last") > 1)
            throw new CodeGenException(this, nameof(Metadata), "Metadata names/records must be unique and declared, with at most one First and Last.");

        var routes = Metadata.Select(x => x.RecordConfig).Prepend(Unit.RecordConfig).ToArray();
        if (Jsonl!.Discriminator is null)
        {
            if (Metadata.Count != 0 || Records.Any(x => x.Jsonl is not null))
                throw new CodeGenException(this, nameof(Jsonl), "Metadata and matches require explicit jsonl.discriminator.");
        }
        else if (routes.Any(x => x.Jsonl is null) || routes.Select(x => x.Jsonl!.Match).Distinct(StringComparer.Ordinal).Count() != routes.Length
            || Records.Except(routes).Any(x => x.Jsonl is not null))
            throw new CodeGenException(this, nameof(Jsonl), "Use exact unique matches on the business/metadata roots only; nested types are not routes.");
    }

    /// <summary>Parses and decodes one literal RFC 6901 selector.</summary>
    private string[] ParsePointer(string pointer)
    {
        if (pointer.Length == 0)
            return [];

        if (pointer[0] != '/')
            throw new CodeGenException(this, nameof(Unit), "A JSON Pointer must be empty or begin with '/'.");

        var segments = pointer[1..].Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            var value = new System.Text.StringBuilder(segments[index].Length);
            for (var position = 0; position < segments[index].Length; position++)
            {
                var character = segments[index][position];
                if (character != '~')
                    value.Append(character);
                else if (++position < segments[index].Length && segments[index][position] is '0' or '1')
                    value.Append(segments[index][position] == '0' ? '~' : '/');
                else
                    throw new CodeGenException(this, nameof(Unit), "A JSON Pointer contains an invalid escape.");
            }

            segments[index] = value.ToString();
        }

        return segments;
    }

    /// <summary>Checks whether either decoded selector is equal to or nested within the other.</summary>
    private static bool Overlaps(IReadOnlyList<string> left, IReadOnlyList<string> right)
    {
        var length = Math.Min(left.Count, right.Count);
        for (var index = 0; index < length; index++)
            if (!string.Equals(left[index], right[index], StringComparison.Ordinal))
                return false;

        return true;
    }
}
