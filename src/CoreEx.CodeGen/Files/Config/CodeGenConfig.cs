namespace CoreEx.CodeGen.Files.Config;

/// <summary>Defines one host-neutral flat, unordered routed, or hierarchical CSV integration.</summary>
[CodeGenClass("CodeGeneration", Title = "File processing code-generation.")]
[CodeGenCategory("Primary", Title = "Definition identity.")]
[CodeGenCategory("Format", Title = "CSV dialect and explicit encoding.")]
[CodeGenCategory("Processing", Title = "Default invocation policy and operating budgets.")]
[CodeGenCategory("Paths", Title = "Destination project and subfolder.")]
[CodeGenCategory("Namespaces", Title = "Explicit generated namespace.")]
[CodeGenCategory("Structure", Title = "Routing and hierarchy.")]
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

    /// <summary>Gets or sets the delimiter.</summary>
    [JsonPropertyName("delimiter")]
    [CodeGenProperty("Format", Title = "Delimiter.", Description = "One character; defaults to comma.")]
    public string? Delimiter { get; set; }

    /// <summary>Gets or sets the quote.</summary>
    [JsonPropertyName("quote")]
    [CodeGenProperty("Format", Title = "Quote.", Description = "One character; defaults to double quote.")]
    public string? Quote { get; set; }

    /// <summary>Gets or sets the optional comment marker.</summary>
    [JsonPropertyName("comment")]
    [CodeGenProperty("Format", Title = "Comment marker.", Description = "One character at record start; omitted by default.")]
    public string? Comment { get; set; }

    /// <summary>Gets or sets the explicit Unicode encoding.</summary>
    [JsonPropertyName("encoding")]
    [CodeGenProperty("Format", Title = "Explicit encoding.", Options = ["Utf8", "Utf16LE", "Utf16BE", "Utf32LE", "Utf32BE"], Description = "Defaults to Utf8. Strict runtime decoding; no BOM-based encoding switching.")]
    public string? Encoding { get; set; }

    /// <summary>Gets or sets the flat named-header policy.</summary>
    [JsonPropertyName("headerPolicy")]
    [CodeGenProperty("Format", Title = "Named header policy.", Options = ["AllowAdditionalColumns", "Exact"], Description = "Flat named-column mappings only; defaults to AllowAdditionalColumns. Exact rejects extra header columns and rows whose field count differs from the header.")]
    public string? HeaderPolicy { get; set; }

    /// <summary>Gets or sets the default processing mode.</summary>
    [JsonPropertyName("mode")]
    [CodeGenProperty("Processing", Title = "Default processing mode.", Options = ["ValidateOnly", "ValidateAndReport", "ValidateThenExecute"], Description = "Defaults to ValidateOnly. ValidateAndReport invokes the handwritten valid-unit report hook without executing business units.")]
    public string? Mode { get; set; }

    /// <summary>Gets or sets the default content-error policy.</summary>
    [JsonPropertyName("errorMode")]
    [CodeGenProperty("Processing", Title = "Default content policy.", Options = ["FailFast", "CollectErrors"], Description = "Defaults to FailFast.")]
    public string? ErrorMode { get; set; }

    /// <summary>Gets or sets the byte budget.</summary>
    [JsonPropertyName("maxInputBytes")]
    [CodeGenProperty("Processing", Title = "Positive input-byte budget.", Description = "Defaults to 67108864 (64 MiB).")]
    public long? MaxInputBytes { get; set; }

    /// <summary>Gets or sets the per-root record budget.</summary>
    [JsonPropertyName("maxGroupRecords")]
    [CodeGenProperty("Processing", Title = "Positive records-per-root budget.", Description = "Defaults to 10000, including the root.")]
    public long? MaxGroupRecords { get; set; }

    /// <summary>Gets or sets the discriminator field index.</summary>
    [JsonPropertyName("discriminatorIndex")]
    [CodeGenProperty("Structure", Title = "Zero-based discriminator index.", Description = "Selects unordered routing without structure; required with structure; omitted for flat input.")]
    public int? DiscriminatorIndex { get; set; }

    /// <summary>Gets or sets the optional hierarchy.</summary>
    [JsonPropertyName("structure")]
    [CodeGenProperty("Structure", Title = "Business-root hierarchy.", Description = "Omit for unordered routed records (with discriminatorIndex) or one flat record (without discriminatorIndex).")]
    public StructureConfig? Structure { get; set; }

    /// <summary>Gets or sets typed wire records.</summary>
    [JsonPropertyName("records")]
    [CodeGenPropertyCollection("Collections", Title = "Wire record definitions.", IsMandatory = true)]
    public List<RecordConfig>? Records { get; set; }

    /// <summary>Gets the resolved destination project.</summary>
    public DirectoryInfo RuntimeDirectory { get; private set; } = null!;

    /// <summary>Gets the resolved output folder.</summary>
    public DirectoryInfo OutputDirectory { get; private set; } = null!;

    /// <summary>Gets whether routed hierarchy processing is required.</summary>
    public bool IsHierarchical => Structure is not null;

    /// <summary>Gets whether records are routed without a hierarchy, each forming its own business unit.</summary>
    public bool IsUnordered => Structure is null && DiscriminatorIndex.HasValue;

    /// <summary>Gets whether exact discriminator routing is configured.</summary>
    public bool IsRouted => IsHierarchical || IsUnordered;

    /// <summary>Gets the business-unit type.</summary>
    public string UnitType => IsUnordered ? Name + "Record" : Structure?.RootNode?.TypeName ?? Records![0].TypeName;

    /// <summary>Gets assembled positions, excluding leaves.</summary>
    public IEnumerable<NodeConfig> Groups => Structure?.Nodes.Where(x => x.HasChildren) ?? [];

    /// <summary>Gets the safe definition literal.</summary>
    public string NameLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Name!);

    /// <summary>Gets the safe revision literal.</summary>
    public string VersionLiteral => OnRamp.Utility.CSharpSource.StringLiteral(Version!);

    /// <summary>Gets the configured encoding expression.</summary>
    public string EncodingExpression => Encoding switch
    {
        "Utf8" => "new UTF8Encoding(false, true)",
        "Utf16LE" => "new UnicodeEncoding(false, false, true)",
        "Utf16BE" => "new UnicodeEncoding(true, false, true)",
        "Utf32LE" => "new UTF32Encoding(false, false, true)",
        "Utf32BE" => "new UTF32Encoding(true, false, true)",
        _ => throw new InvalidOperationException("Unprepared encoding.")
    };

    /// <summary>Gets safe dialect literals; indexing produces a char without ad-hoc escaping.</summary>
    public string DelimiterExpression => OnRamp.Utility.CSharpSource.StringLiteral(Delimiter!) + "[0]";

    /// <summary>Gets the safe quote expression.</summary>
    public string QuoteExpression => OnRamp.Utility.CSharpSource.StringLiteral(Quote!) + "[0]";

    /// <summary>Gets the optional safe comment expression.</summary>
    public string CommentExpression => Comment is null ? "null" : OnRamp.Utility.CSharpSource.StringLiteral(Comment) + "[0]";

    /// <summary>Gets the configured named-header policy expression.</summary>
    public string HeaderPolicyExpression => "CsvHeaderPolicy." + HeaderPolicy;

    /// <summary>Gets whether exact header validation is enabled for generated flat mappings.</summary>
    public bool IsExactHeaderPolicy => HeaderPolicy == "Exact";

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
        Mode = DefaultWhereNull(Mode, () => "ValidateOnly");
        ErrorMode = DefaultWhereNull(ErrorMode, () => "FailFast");
        HeaderPolicy = DefaultWhereNull(HeaderPolicy, () => "AllowAdditionalColumns");
        Encoding = DefaultWhereNull(Encoding, () => "Utf8");
        if (Mode is not ("ValidateOnly" or "ValidateAndReport" or "ValidateThenExecute"))
            throw new CodeGenException(this, nameof(Mode), "The processing mode must be ValidateOnly, ValidateAndReport, or ValidateThenExecute.");

        if (ErrorMode is not ("FailFast" or "CollectErrors"))
            throw new CodeGenException(this, nameof(ErrorMode), "The content policy must be FailFast or CollectErrors.");

        MaxInputBytes = DefaultWhereNull(MaxInputBytes, () => 64 * 1024 * 1024);
        MaxGroupRecords = DefaultWhereNull(MaxGroupRecords, () => 10_000);
        if (MaxInputBytes <= 0 || MaxGroupRecords <= 0)
            throw new CodeGenException(this, nameof(MaxInputBytes), "Input and group budgets must be positive.");

        Delimiter = DefaultWhereNull(Delimiter, () => ",");
        Quote = DefaultWhereNull(Quote, () => "\"");
        if (new[] { Delimiter, Quote, Comment }.OfType<string>().Any(x => x.Length != 1 || x[0] is '\r' or '\n' or '\0' || char.IsSurrogate(x[0]))
            || Delimiter == Quote || Comment is not null && (Comment == Delimiter || Comment == Quote))
            throw new CodeGenException(this, nameof(Delimiter), "Dialect markers must be distinct single non-newline, non-null characters.");

        if (Records is null || Records.Count == 0 || Records.Any(x => x is null))
            throw new CodeGenException(this, nameof(Records), "At least one non-null record is required.");

        Records = await PrepareCollectionAsync(Records).ConfigureAwait(false);
        if (Records.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Records.Count)
            throw new CodeGenException(this, nameof(Records), "Record names must be unique ignoring case.");

        if (HeaderPolicy is not ("AllowAdditionalColumns" or "Exact"))
            throw new CodeGenException(this, nameof(HeaderPolicy), "The named header policy must be AllowAdditionalColumns or Exact.");

        if (!IsRouted)
        {
            if (Records.Count != 1 || DiscriminatorIndex.HasValue || Records.Any(x => x.Match is not null || x.ErrorMode is not null))
                throw new CodeGenException(this, nameof(Structure), "Flat input requires one record and no routing settings.");

            if (HeaderPolicy == "Exact" && Records[0].Properties!.Any(x => x.Index.HasValue))
                throw new CodeGenException(this, nameof(HeaderPolicy), "Exact header validation requires every flat property to use a named column selector.");
        }
        else
        {
            if (HeaderPolicy != "AllowAdditionalColumns")
                throw new CodeGenException(this, nameof(HeaderPolicy), "Named header policies apply only to flat files.");

            if (!DiscriminatorIndex.HasValue || DiscriminatorIndex < 0 || Records.Any(x => string.IsNullOrWhiteSpace(x.Match) || x.Properties!.Any(p => !p.Index.HasValue || p.Index == DiscriminatorIndex))
                || Records.Select(x => x.Match).Distinct(StringComparer.Ordinal).Count() != Records.Count)
                throw new CodeGenException(this, nameof(DiscriminatorIndex), "Routed files require a nonnegative discriminator, exact unique matches, and positional fields excluding the discriminator.");

            if (Structure is not null)
                await Structure.PrepareAsync(this, this).ConfigureAwait(false);
            else if (Records.Count(x => x.Position == "First") > 1 || Records.Count(x => x.Position == "Last") > 1)
                throw new CodeGenException(this, nameof(Records), "Only one First and one Last record type may be configured.");
        }

        if (TypeNames.Distinct(StringComparer.OrdinalIgnoreCase).Count() != TypeNames.Count())
            throw new CodeGenException(this, nameof(Records), "Generated type/file names collide.");

        foreach (var typeName in TypeNames)
            ConfigValidation.GeneratedTypeName(this, nameof(Records), typeName);
    }
}
