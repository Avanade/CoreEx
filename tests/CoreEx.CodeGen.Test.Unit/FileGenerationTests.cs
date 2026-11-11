namespace CoreEx.CodeGen.Test.Unit;

[TestFixture]
public partial class FileGenerationTests
{
    private string _directory = null!;
    private string _runtime = null!;
    private string _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = FindRepository();
        _directory = Path.Combine(Path.GetTempPath(), "CoreEx.FileCodeGen." + Guid.NewGuid().ToString("N"));
        _runtime = Path.Combine(_directory, "Runtime");
        Directory.CreateDirectory(_runtime);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "GlobalUsing.cs"), """
            global using CoreEx;
            global using CoreEx.DependencyInjection;
            global using CoreEx.IO;
            global using CoreEx.IO.Csv;
            global using CoreEx.Validation;
            global using Microsoft.Extensions.DependencyInjection;
            global using System.Globalization;
            global using System.Text;
            global using System.Text.RegularExpressions;
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Runtime.csproj"), $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net{Environment.Version.Major}.0</TargetFramework>
                <OutputType>Exe</OutputType>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
                <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
                <UseSharedCompilation>false</UseSharedCompilation>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{Path.Combine(_repository, "src", "CoreEx.IO", "CoreEx.IO.csproj")}" />
                <ProjectReference Include="{Path.Combine(_repository, "src", "CoreEx.Validation", "CoreEx.Validation.csproj")}" />
                <ProjectReference Include="{Path.Combine(_repository, "src", "CoreEx.RefData", "CoreEx.RefData.csproj")}" />
              </ItemGroup>
            </Project>
            """);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, true);
    }

    private CodeGeneratorArgs Args(bool simulation = false, bool unchanged = false) => new("file-script.yaml", null)
    {
        OutputDirectory = new(_directory),
        Assemblies = { typeof(CodeGenConsole).Assembly },
        Logger = NullLogger.Instance,
        IsSimulation = simulation,
        ExpectNoChanges = unchanged
    };

    private string Write(string name, string content)
    {
        var path = Path.Combine(_directory, name);
        System.IO.File.WriteAllText(path, content);
        return path;
    }

    private static string Flat(string name = "Flat") => $$"""
        name: {{name}}
        version: '2026-01'
        runtimeProjectPath: Runtime
        runtimeNamespace: Consumer.{{name}}
        errorMode: CollectErrors
        records:
        - name: Row
          properties:
          - { name: Code, required: true, maxLength: 10 }
          - { name: Count, type: Integer, required: true }
          - { name: Date, type: DateOnly, format: yyyyMMdd }
          - { name: Timestamp, type: DateTime, format: yyyyMMdd }
        """;

    private static string Hierarchy(string name) => $$"""
        name: {{name}}
        version: r1
        runtimeProjectPath: Runtime
        runtimeNamespace: Consumer.{{name}}
        csv:
          discriminatorIndex: 0
          structure:
            root:
              record: Order
              cardinality: OneOrMore
              children:
              - record: Line
                cardinality: OneOrMore
                children:
                - { record: Detail, cardinality: Optional }
        metadata:
        - { name: Header, record: Header, required: true, csv: { position: First } }
        - { name: Trailer, record: Trailer, required: true, csv: { position: Last } }
        records:
        - name: Header
          csv: { match: H }
          properties:
          - { name: Batch, csv: { index: 1 }, required: true }
        - name: Order
          csv: { match: O }
          properties:
          - { name: Id, type: Integer, csv: { index: 1 }, required: true }
        - name: Line
          csv: { match: L }
          properties:
          - { name: Sku, csv: { index: 1 }, required: true }
        - name: Detail
          csv: { match: D }
          properties:
          - { name: Text, csv: { index: 1 } }
        - name: Trailer
          csv: { match: T }
          properties:
          - { name: Count, type: Integer, csv: { index: 1 } }
        """;

    private static string Unordered(string name) => $$"""
        name: {{name}}
        version: r1
        runtimeProjectPath: Runtime
        runtimeNamespace: Consumer.{{name}}
        csv:
          discriminatorIndex: 0
        metadata:
        - { name: Header, record: Header, required: true, csv: { position: First } }
        records:
        - name: Header
          csv: { match: H }
          properties:
          - { name: batch_id, required: true, csv: { index: 1 } }
        - name: Order
          csv: { match: O }
          properties:
          - { name: id, type: Integer, required: true, csv: { index: 1 } }
        """;

    [Test]
    public async Task ValidateAndSimulationDoNotWrite()
    {
        Write("flat-file.yaml", Flat());
        Directory.CreateDirectory(Path.Combine(_directory, "ignored"));
        System.IO.File.WriteAllText(Path.Combine(_directory, "ignored", "invalid-file.yaml"), "not: a definition");
        Write("not-a-file.yml", "also: ignored");
        await FileGenerationBatch.GenerateAsync(Args(), validateOnly: true);
        await FileGenerationBatch.GenerateAsync(Args(simulation: true));
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task GeneratedFilesHaveBannerAndScopedNullableDirectives()
    {
        Write("flat-file.yaml", Flat());
        Write("nested-file.yaml", Hierarchy("Nested"));
        await FileGenerationBatch.GenerateAsync(Args());
        var generatorName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "??";
        var prefix = $"// <auto-generated/>\n\n/*\n * This file is automatically generated by '{generatorName}'; any changes will be lost.\n */\n\n#nullable enable\n\n";
        var files = Directory.GetFiles(Path.Combine(_runtime, "Files"), "*.g.cs", SearchOption.AllDirectories);
        files.Should().NotBeEmpty();
        foreach (var file in files)
        {
            var content = System.IO.File.ReadAllText(file).Replace("\r\n", "\n");
            content.Should().StartWith(prefix, "every generated artifact must identify its generator");
            content.Should().EndWith("\n\n#nullable restore\n");
            content.Should().NotContain("System.").And.NotContain("CoreEx.IO.").And.NotContain("CoreEx.Validation.").And.NotContain("Microsoft.Extensions.");
        }

        System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatHandler.cs")).Should().NotContain("<auto-generated");
        System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatProcessor.g.cs")).Should()
            .Contain("public override Task<FileProcessingSummary> ProcessAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingContext context, CancellationToken cancellationToken = default)")
            .And.Contain("if (context.ThrowIfNull() is not FlatContext")
            .And.NotContain("context.ThrowIfNull();")
            .And.NotContain("IServiceProvider Services")
            .And.NotContain("Services =");
        Directory.GetFiles(Path.Combine(_runtime, "Files"), "*ServiceCollectionExtensions.g.cs", SearchOption.AllDirectories).Should().BeEmpty();
        System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatProcessor.g.cs")).Should().Contain("[ScopedService]");
        System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatHandler.cs")).Should().Contain("[ScopedService]");
        var validator = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatRowValidator.g.cs"));
        validator.Should().Contain("Property(x => x.Code).WithText(\"Code\").NotNull().MaximumLength(10);")
            .And.NotContain("Property(x => x.Date)")
            .And.NotContain("Property(x => x.Timestamp)");
        var metadataContext = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Nested", "NestedContext.g.cs"));
        metadataContext.Should().Contain("GetHeaderMetadata()").And.Contain("GetAcceptedHeaderMetadata()")
            .And.Contain("GetTrailerMetadata()").And.Contain("GetAcceptedTrailerMetadata()");
        var hierarchyProcessor = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Nested", "NestedProcessor.g.cs"));
        hierarchyProcessor.Should().Contain("new CsvMetadataDefinition[]").And.Contain("FileRecordPosition.First").And.Contain("FileRecordPosition.Last");
    }

    [Test]
    public async Task FlatDefinitionGeneratesExactHeaderAndConfiguredMode()
    {
        var definition = Flat().Replace("errorMode: CollectErrors", "errorMode: CollectErrors\ncsv: { headerPolicy: Exact }\nmode: ValidateThenExecute", StringComparison.Ordinal);
        Write("configured-mode-file.yaml", definition);
        await FileGenerationBatch.GenerateAsync(Args());

        System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatRowMapping.g.cs")).Should()
            .Contain("new CsvFileMapping<FlatRow>(CsvHeaderPolicy.Exact)");
        System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatProcessor.g.cs")).Should()
            .Contain("FileProcessingMode mode = FileProcessingMode.ValidateThenExecute");
    }

    [Test]
    public async Task NamedValidationLabelsUseSourceColumnWithoutChangingPropertyIdentity()
    {
        Write("flat-file.yaml", Flat().Replace("name: Code,", "name: 'Supplier code', property: Code,"));
        await FileGenerationBatch.GenerateAsync(Args());
        var validator = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatRowValidator.g.cs"));
        validator.Should().Contain("Property(x => x.Code).WithText(\"Supplier code\").NotNull().MaximumLength(10)");
    }

    [Test]
    public async Task UnorderedDefinitionGeneratesDeclaredMetadataRoutes()
    {
        Write("unordered-file.yaml", Unordered("Unordered"));

        await FileGenerationBatch.GenerateAsync(Args());

        var processor = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Unordered", "UnorderedProcessor.g.cs"));
        processor.Should().Contain("new CsvMetadataDefinition[]")
            .And.Contain("new(\"Header\", \"Header\", true, FileRecordPosition.First)")
            .And.Contain("cardinality: FileCardinality.Many, position: FileRecordPosition.Any");
        var context = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Unordered", "UnorderedContext.g.cs"));
        context.Should().Contain("GetHeaderMetadata()").And.Contain("GetAcceptedHeaderMetadata()");
    }

    [Test]
    public async Task SourceNamesDerivePropertiesWithoutChangingSelectors()
    {
        Write("names-file.yaml", """
            name: Names
            version: r1
            runtimeProjectPath: Runtime
            records:
            - name: Row
              properties:
              - { name: order_id }
              - { name: effective-date, type: DateOnly, format: yyyyMMdd }
              - { name: customer_ref, property: CustomerReference }
            """);

        await FileGenerationBatch.GenerateAsync(Args());

        var record = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Names", "NamesRow.g.cs"));
        record.Should().Contain("string? OrderId").And.Contain("DateOnly? EffectiveDate").And.Contain("string? CustomerReference");
        var mapping = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Names", "NamesRowMapping.g.cs"));
        mapping.Should().Contain(".MapProperty(\"order_id\", x => x.OrderId")
            .And.Contain(".MapProperty(\"effective-date\", x => x.EffectiveDate")
            .And.Contain(".MapProperty(\"customer_ref\", x => x.CustomerReference");
    }

    [Test]
    public async Task LegacyColumnAndStructureEnvelopeKeysAreRejected()
    {
        var legacyColumn = Flat().Replace("{ name: Code,", "{ name: Code, column: Code,", StringComparison.Ordinal);
        Write("legacy-file.yaml", legacyColumn);
        var action = () => FileGenerationBatch.GenerateAsync(Args());
        await action.Should().ThrowAsync<CodeGenException>().WithMessage("*column*Unknown property*");
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();

        Write("legacy-file.yaml", Hierarchy("Legacy").Replace("  structure:\n    root:", "  structure:\n    header: { record: Header }\n    root:", StringComparison.Ordinal));
        var structureAction = () => FileGenerationBatch.GenerateAsync(Args());
        await structureAction.Should().ThrowAsync<CodeGenException>().WithMessage("*header*Unknown property*");
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task ValidateAndExecuteIsAcceptedAsTheSinglePassMode()
    {
        Write("single-pass-file.yaml", Flat().Replace("runtimeNamespace:", "mode: ValidateAndExecute\nruntimeNamespace:", StringComparison.Ordinal));
        await FileGenerationBatch.GenerateAsync(Args());
        System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Flat", "FlatProcessor.g.cs"))
            .Should().Contain("FileProcessingMode mode = FileProcessingMode.ValidateAndExecute");
    }

    [Test]
    public async Task NormalizedSourceNameCollisionsRequireExplicitPropertyOverride()
    {
        Write("names-file.yaml", """
            name: Names
            version: r1
            runtimeProjectPath: Runtime
            records:
            - name: Row
              properties:
              - { name: order_id }
              - { name: order-id }
            """);

        var act = () => FileGenerationBatch.GenerateAsync(Args());
        await act.Should().ThrowAsync<CodeGenException>().WithMessage("*Duplicate generated property*set property to a distinct valid identifier*");
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [TestCase("delimiter")]
    [TestCase("quote")]
    [TestCase("comment")]
    public async Task SurrogateDialectIsRejectedBeforeWrites(string property)
    {
        Write("flat-file.yaml", Flat() + "\n" + property + ": \"\\uD800\"\n");
        var act = () => FileGenerationBatch.GenerateAsync(Args());
        await act.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task GenerationIsIdempotentAndPreservesHandwrittenHandler()
    {
        Write("flat-file.yaml", Flat());
        await FileGenerationBatch.GenerateAsync(Args());
        var handler = Path.Combine(_runtime, "Files", "Flat", "FlatHandler.cs");
        System.IO.File.AppendAllText(handler, "\n// Preserved application behavior.\n");
        await FileGenerationBatch.GenerateAsync(Args(unchanged: true));
        System.IO.File.ReadAllText(handler).Should().Contain("Preserved application behavior.");
        var processor = Path.Combine(_runtime, "Files", "Flat", "FlatProcessor.g.cs");
        System.IO.File.AppendAllText(processor, "\n// Output drift.\n");
        var act = () => FileGenerationBatch.GenerateAsync(Args(unchanged: true));
        await act.Should().ThrowAsync<CodeGenChangesFoundException>();
    }

    [Test]
    public async Task InvalidLaterDefinitionPreventsAllWrites()
    {
        Write("a-file.yaml", Flat());
        Write("z-file.yaml", Flat("Other") + "\nunknownSetting: true");
        var act = () => FileGenerationBatch.GenerateAsync(Args());
        await act.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [TestCase("missing.yaml")]
    [TestCase("missing-file.yaml")]
    public async Task ExplicitSelectionRequiresExistingSuffix(string selected)
    {
        Write("valid-file.yaml", Flat());
        var act = () => FileGenerationBatch.GenerateAsync(Args(), [selected]);
        await act.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task EmptyDiscoveryFails()
    {
        var act = () => FileGenerationBatch.GenerateAsync(Args());
        await act.Should().ThrowAsync<CodeGenException>();
    }

    [Test]
    public async Task FileConsoleDoesNotRequireRefDataAndHonorsExplicitSelection()
    {
        Write("valid-file.yaml", Flat());
        Write("invalid-file.yaml", "unknown: true");
        var console = CodeGenConsole.Create();
        console.Args.OutputDirectory = new(_directory);
        console.Args.ConfigFileName = Path.Combine(_directory, "ref-data.yaml");
        var result = await console.RunAsync(["file", "valid-file.yaml", "--validate"]);
        result.Should().Be(0);
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task CountConsoleDoesNotLoadRefData()
    {
        var directory = Directory.CreateDirectory(Path.Combine(_directory, "tools", "CodeGen"));
        Write("sample.cs", "// Count fixture.\n");
        var console = CodeGenConsole.Create();
        console.Args.OutputDirectory = directory;
        console.Args.ConfigFileName = Path.Combine(directory.FullName, "missing.yaml");
        (await console.RunAsync(["count"])).Should().Be(0);
    }

    [Test]
    public async Task RefDataGenerationRemainsStable()
    {
        var codegen = Directory.CreateDirectory(Path.Combine(_directory, "Consumer.CodeGen"));
        foreach (var suffix in new[] { "Contracts", "Application", "Infrastructure", "Api" })
            Directory.CreateDirectory(Path.Combine(_directory, "Consumer." + suffix));

        var configPath = Path.Combine(codegen.FullName, "ref-data.yaml");
        System.IO.File.WriteAllText(configPath, "repository: None\nentities:\n- name: Status\n- name: Country\n  properties:\n  - { name: Region, type: string? }\n");
        var args = new CodeGeneratorArgs("ref-data-script.yaml", configPath).AddAssembly(typeof(CodeGenConsole).Assembly);
        args.OutputDirectory = codegen;
        args.Logger = NullLogger.Instance;
        var generator = await CodeGenerator.CreateAsync(args);
        var config = await generator.LoadConfigAsync();
        var first = await generator.GenerateAsync(config);
        first.CreatedCount.Should().BeGreaterThan(0);
        args.ExpectNoChanges = true;
        generator = await CodeGenerator.CreateAsync(args);
        var second = await generator.GenerateAsync();
        second.UpdatedCount.Should().Be(0);
        var contract = System.IO.File.ReadAllText(Path.Combine(_directory, "Consumer.Contracts", "Status.g.cs"));
        contract.Should().Contain("public partial class Status");
        var console = CodeGenConsole.Create();
        console.Args.OutputDirectory = codegen;
        console.Args.ConfigFileName = configPath;
        (await console.RunAsync(["refdata", "--validate"])).Should().Be(0);
    }

    [TestCase("name: Other", "name: Flat")]
    [TestCase("runtimeNamespace: Consumer.Other", "runtimeNamespace: Consumer.Flat\nruntimePath: Files/Flat")]
    public async Task BatchCollisionsFailBeforeWrites(string original, string replacement)
    {
        Write("a-file.yaml", Flat());
        Write("b-file.yaml", Flat("Other").Replace(original, replacement));
        var act = () => FileGenerationBatch.GenerateAsync(Args());
        await act.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [TestCase("type: Integer", "type: Typo")]
    [TestCase("version: '2026-01'", "version: ''")]
    [TestCase("runtimeProjectPath: Runtime", "runtimeProjectPath: missing")]
    [TestCase("runtimeNamespace: Consumer.Flat", "runtimeNamespace: Invalid.class")]
    [TestCase("runtimeNamespace: Consumer.Flat", "runtimeNamespace: Consumer.Flat\nruntimePath: ../outside")]
    [TestCase("name: Count", "name: Code")]
    [TestCase("name: Count", "name: Code")]
    [TestCase("name: Count, type: Integer, required: true", "name: Count, type: Integer, required: true, column: Count")]
    [TestCase("errorMode: CollectErrors", "errorMode: Typo")]
    [TestCase("runtimeNamespace: Consumer.Flat", "runtimeNamespace: Consumer.Flat\nmode: Typo")]
    [TestCase("runtimeNamespace: Consumer.Flat", "runtimeNamespace: Consumer.Flat\ncsv: { encoding: Typo }")]
    [TestCase("type: Integer", "type: Integer, referenceData: { type: Consumer.Country }")]
    [TestCase("name: Code, required: true, maxLength: 10", "name: Code, required: true, maxLength: 10, column: Code")]
    [TestCase("name: Code, required: true, maxLength: 10", "name: Code, referenceData: { type: 'Bad<T>' }, required: true, maxLength: 10")]
    public async Task InvalidConfigurationIsRejected(string original, string replacement)
    {
        Write("flat-file.yaml", Flat().Replace(original, replacement));
        var act = () => FileGenerationBatch.GenerateAsync(Args());
        await act.Should().ThrowAsync<CodeGenException>();
    }

    [TestCase("Date", "Time")]
    [TestCase("Date", "Only")]
    [TestCase("G", "uid")]
    [TestCase("ReadOnly", "Span")]
    [TestCase("Csv", "FileOptions")]
    [TestCase("File", "ProcessingContext")]
    [TestCase("File", "UnitArgs")]
    [TestCase("File", "UnitValidationArgs")]
    [TestCase("File", "RecordPosition")]
    [TestCase("Generated", "RegexAttribute")]
    public async Task ReservedGeneratedTypeNamesAreRejectedBeforeBatchWrites(string definition, string record)
    {
        Write("a-file.yaml", Flat());
        Write("z-file.yaml", Flat(definition).Replace("name: Row", "name: " + record));
        foreach (var validateOnly in new[] { true, false })
        {
            var act = () => FileGenerationBatch.GenerateAsync(Args(), validateOnly: validateOnly);
            await act.Should().ThrowAsync<CodeGenException>().WithMessage("*" + definition + record + "*reserved*");
            Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
        }
    }

    [Test]
    public async Task DateConversionUsesRuntimeConvertersWithoutGeneratedHelperCollisions()
    {
        Write("a-file.yaml", Flat());
        Write("z-file.yaml", Flat("Convert").Replace("name: Timestamp", "name: RowMapping"));
        await FileGenerationBatch.GenerateAsync(Args());
        var mapping = System.IO.File.ReadAllText(Path.Combine(_runtime, "Files", "Convert", "ConvertRowMapping.g.cs"));
        mapping.Should().Contain("CsvFieldConverters.DateTime(\"yyyyMMdd\")").And.NotContain("private static bool Convert");
    }

    [TestCase("cardinality: OneOrMore", "cardinality: Invalid")]
    [TestCase("match: L", "match: O")]
    [TestCase("discriminatorIndex: 0", "discriminatorIndex: 1")]
    [TestCase("record: Detail", "record: Missing")]
    [TestCase("record: Detail", "record: Order")]
    [TestCase("name: Header, record: Header, required: true", "name: Header, record: Header, required: true, unsupported: true")]
    public async Task InvalidHierarchyIsRejectedBeforeWrites(string original, string replacement)
    {
        Write("orders-file.yaml", Hierarchy("Orders").Replace(original, replacement));
        var act = () => FileGenerationBatch.GenerateAsync(Args());
        await act.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public void ConfigurationReference_IncludesGroupedSettingsAndConversionPolicies()
    {
        var directory = Path.Combine(_directory, "docs");
        Directory.CreateDirectory(directory);
        OnRamp.Utility.MarkdownDocumentationGenerator.Generate<Files.Config.CodeGenConfig>(null, directory);

        (string Page, string[] Properties)[] expected =
        [
            ("CodeGeneration", ["format", "csv", "metadata", "mode", "errorMode"]),
            ("Csv", ["delimiter", "encoding", "discriminatorIndex", "structure"]),
            ("CsvMetadata", ["position"]),
            ("CsvProperty", ["index"]),
            ("CsvRecord", ["match", "cardinality", "position"]),
            ("Metadata", ["name", "record", "required", "csv"]),
            ("Property", ["name", "property", "csv", "format"]),
            ("Record", ["name", "csv", "properties"])
        ];
        foreach (var (page, properties) in expected)
        {
            var content = System.IO.File.ReadAllText(Path.Combine(directory, page + ".md"));
            foreach (var property in properties)
                content.Should().Contain("`" + property + "`", $"{page} must document {property}");
        }

        foreach (var file in Directory.GetFiles(directory, "*.md"))
        {
            var checkedIn = Path.Combine(_repository, "src", "CoreEx.CodeGen", "Files", "docs", Path.GetFileName(file));
            System.IO.File.ReadAllText(checkedIn).Replace("\r\n", "\n").TrimEnd()
                .Should().Be(System.IO.File.ReadAllText(file).Replace("\r\n", "\n").TrimEnd(), "checked-in reference pages must match generated output");
        }

        System.IO.File.ReadAllText(Path.Combine(directory, "CodeGeneration.md")).Should().NotContain("## Structure");
        System.IO.File.ReadAllText(Path.Combine(directory, "Record.md")).Should().NotContain("`errorMode`");
    }

    [Test]
    [TestCase("type: Integer, regex: '[a-z]'")]
    [TestCase("minLength: 4, maxLength: 2")]
    [TestCase("regex: '['")]
    [TestCase("type: Boolean, min: 0")]
    [TestCase("type: Integer, min: 3, max: 2")]
    [TestCase("type: Integer, options: [null]")]
    [TestCase("options: []")]
    [TestCase("type: Decimal, precision: 2, scale: 3")]
    [TestCase("type: Decimal, precision: 30")]
    [TestCase("type: Double, min: 1e999")]
    [TestCase("type: Integer, min: '1'")]
    public async Task InvalidValidationRulesPreventWrites(string rules)
    {
        Write("rules-file.yaml", Flat().Replace("name: Code, required: true, maxLength: 10", "name: Code, " + rules));
        var act = () => FileGenerationBatch.GenerateAsync(Args());
        await act.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task GeneratedFlatAndTwoNestedHierarchiesCompileAndRunTogether()
    {
        Write("flat-file.yaml", Flat().Replace("name: Code, required: true", "name: 'Supplier code', property: Code, referenceData: { type: Consumer.Country }, required: true")
            .Replace("version: '2026-01'", "version: 'rev\"\\\u96ea'")
            + "\n  - { name: Alternate, referenceData: { type: Consumer.Country, allowInactive: true } }\n");
        Write("orders-file.yaml", Hierarchy("Orders"));
        Write("returns-file.yaml", Hierarchy("Returns"));
        Write("rules-file.yaml", """
            name: Rules
            version: '1'
            runtimeProjectPath: Runtime
            runtimeNamespace: Consumer.Rules
            records:
            - name: Row
              properties:
              - { name: Text, csv: { index: 0 }, required: true, minLength: 2, maxLength: 4, regex: '^[A-Z]+$', min: AA, max: ZZ, options: [AB, CD] }
              - { name: Mail, csv: { index: 1 }, email: true }
              - { name: Amount, csv: { index: 2 }, type: Decimal, precision: 5, scale: 2, min: 0, max: 999.99, options: [12.34] }
              - { name: Large, csv: { index: 3 }, type: Int64, min: 2147483648, max: 9223372036854775807, options: [4294967296] }
              - { name: Ratio, csv: { index: 4 }, type: Double, min: -1.5, max: 1.5, options: [0.5] }
              - { name: Day, csv: { index: 5 }, type: DateOnly, format: yyyyMMdd, min: '20260101', max: '20261231', options: ['20260615'] }
              - { name: Timestamp, csv: { index: 6 }, type: DateTime, format: yyyyMMddHHmmssfffffff, min: '202601010000000000000', options: ['202606151314151234567'] }
              - { name: Flag, csv: { index: 7 }, type: Boolean, mandatory: true, options: [true] }
              - { name: Id, csv: { index: 8 }, type: Guid, options: ['01234567-89ab-cdef-0123-456789abcdef'] }
              - { name: Empty, csv: { index: 9 }, maxLength: 0, regex: '' }
              - { name: Number, csv: { index: 10 }, type: Integer, max: 10, options: [0, 10] }
            """);
        await FileGenerationBatch.GenerateAsync(Args());
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Country.cs"), """
            namespace Consumer;
            public class Country : CoreEx.RefData.ReferenceData<Country> { }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Countries.cs"), """
            namespace Consumer;
            public class Countries() : CoreEx.RefData.ReferenceDataCollection<Country>(codeComparer: StringComparer.OrdinalIgnoreCase) { }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "CountryProvider.cs"), """
            namespace Consumer;
            public class CountryProvider : CoreEx.RefData.IReferenceDataProvider
            {
                public IEnumerable<(Type, Type)> Types => [(typeof(Country), typeof(Countries))];
                public Task<CoreEx.RefData.Abstractions.IReferenceDataCollection> GetAsync(Type type, CancellationToken cancellationToken = default)
                    => Task.FromResult<CoreEx.RefData.Abstractions.IReferenceDataCollection>(new Countries
                    {
                        new Country { Id = "1", Code = "A", Text = "Active" },
                        new Country { Id = "2", Code = "D", Text = "Inactive", IsInactive = true }
                    });
            }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "FlatProcessor.cs"), """
            namespace Consumer.Flat;
            public partial class FlatProcessor
            {
                public int Diagnostics { get; private set; }
                protected override Task OnDiagnosticAsync(CoreEx.IO.FileDiagnosticArgs args, CancellationToken cancellationToken)
                {
                    if (args.Diagnostic.Location?.PropertyPath != "code") throw new Exception($"Unexpected diagnostic {args.Diagnostic.Code} at {args.Diagnostic.Location?.PropertyPath}: {args.Diagnostic.Message}");
                    if (args.Diagnostic.Message.ToString()?.Contains("Supplier code") != true) throw new Exception("Validation did not retain the source-column label.");
                    Diagnostics++;
                    return Task.CompletedTask;
                }
            }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Program.cs"), """
            var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            Microsoft.Extensions.DependencyInjection.CoreExExtensions.AddExecutionContext(services);
            Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<Consumer.CountryProvider>(services);
            Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<CoreEx.RefData.IReferenceDataCache>(services,
                new CoreEx.RefData.ReferenceDataHybridCache(new CoreEx.Caching.MemoryOnlyHybridCache()));
            services.AddDynamicServicesUsing<Consumer.Flat.FlatProcessor>();
            await using var provider = Microsoft.Extensions.DependencyInjection.ServiceCollectionContainerBuilderExtensions.BuildServiceProvider(services);
            using var fileScope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(provider);
            using var fileContext = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<CoreEx.ExecutionContext>(fileScope.ServiceProvider);
            var orchestrator = new CoreEx.RefData.ReferenceDataOrchestrator(provider, Microsoft.Extensions.Logging.Abstractions.NullLogger<CoreEx.RefData.ReferenceDataOrchestrator>.Instance);
            orchestrator.Register<Consumer.CountryProvider>();
            await orchestrator.GetByTypeAsync<Consumer.Country>();
            CoreEx.RefData.ReferenceDataOrchestrator.SetCurrent(orchestrator);
            var flat = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Consumer.Flat.FlatProcessor>(provider);
            var orders = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Consumer.Orders.OrdersProcessor>(provider);
            var returns = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Consumer.Returns.ReturnsProcessor>(provider);
            static Task<Stream> Open(string text) => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)));
            const string columns = "Supplier code,Count,Date,Timestamp,Alternate\n";
            if (Consumer.Flat.FlatProcessor.DefinitionVersion != "rev\"\\\u96ea") throw new Exception("Definition revision literal did not round-trip.");
            var flatResult = await flat.ProcessAsync(_ => Open(columns + "a,0,20260615,20260615,D\nA,0,,,\n"), Consumer.Flat.FlatProcessor.CreateContext("source", "attempt"));
            if (flatResult.Outcome != CoreEx.IO.FileProcessingOutcome.Succeeded) throw new Exception("Flat validation failed.");
            foreach (var code in new[] { "Z", "D", "" })
            {
                var invalidCode = await flat.ProcessAsync(_ => Open(columns + code + ",0,,,\n"), Consumer.Flat.FlatProcessor.CreateContext("source", "attempt"));
                if (invalidCode.Outcome != CoreEx.IO.FileProcessingOutcome.ContentFailed) throw new Exception("Invalid reference code accepted.");
            }
            if (flat.Diagnostics != 3) throw new Exception("Reference-data diagnostics must be individual and retain property paths.");
            var exceptions = 0;
            EventHandler<System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs> countExceptions = (_, _) => exceptions++;
            AppDomain.CurrentDomain.FirstChanceException += countExceptions;
            try
            {
                var invalidInput = columns + string.Concat(Enumerable.Repeat("Z,0,,,\n", 10_000));
                var rejected = await flat.ProcessAsync(_ => Open(invalidInput), Consumer.Flat.FlatProcessor.CreateContext("source", "attempt"));
                if (rejected.IsSuccess || flat.Diagnostics != 10_003) throw new Exception("Invalid reference codes were not streamed individually.");
            }
            finally { AppDomain.CurrentDomain.FirstChanceException -= countExceptions; }
            if (exceptions != 0) throw new Exception("Reference-data content validation used exceptions.");
            var row = new Consumer.Flat.FlatRow { Code = "a", Count = 0 };
            await new Consumer.Flat.FlatRowValidator().ValidateAsync(row);
            if (row.Code != "a") throw new Exception("Validation rewrote the wire code.");
            const string input = "H,batch\nO,1\nL,sku\nD,text\nT,1\n";
            var orderResult = await orders.ProcessAsync(_ => Open(input), Consumer.Orders.OrdersProcessor.CreateContext("source", "attempt"));
            var returnResult = await returns.ProcessAsync(_ => Open(input), Consumer.Returns.ReturnsProcessor.CreateContext("source", "attempt"));
            if (!orderResult.IsSuccess || !returnResult.IsSuccess) throw new Exception("Hierarchy validation failed.");
            var rootThrows = false;
            try
            {
                await returns.ProcessAsync(_ => Open(input), Consumer.Returns.ReturnsProcessor.CreateContext("source", "attempt", mode: CoreEx.IO.FileProcessingMode.ValidateThenExecute));
            }
            catch (NotSupportedException) { rootThrows = true; }
            if (!rootThrows) throw new Exception("Unimplemented execution must fail explicitly.");
            var opened = false;
            try
            {
                var invalid = new Consumer.Flat.FlatContext(new("Flat", "wrong", "source", "attempt"));
                await flat.ProcessAsync(_ => { opened = true; return Open(""); }, invalid);
                throw new Exception("Mismatched identity was accepted.");
            }
            catch (ArgumentException) when (!opened) { }
            var rules = new Consumer.Rules.RulesRowValidator();
            var validRow = new Consumer.Rules.RulesRow
            {
                Text = "AB", Mail = "a@example.com", Amount = 12.34m, Large = 4294967296L, Ratio = 0.5,
                Day = new DateOnly(2026, 6, 15), Timestamp = new DateTime(2026, 6, 15, 13, 14, 15).AddTicks(1234567),
                Flag = true, Id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), Empty = "", Number = 0
            };
            if ((await rules.ValidateAsync(validRow)).HasErrors) throw new Exception("Valid declarative rules rejected.");
            var mutations = new Action<Consumer.Rules.RulesRow>[]
            {
                x => x.Text = "a", x => x.Mail = "invalid", x => x.Amount = 12.345m,
                x => x.Large = 1, x => x.Ratio = 2, x => x.Day = new DateOnly(2025, 1, 1),
                x => x.Timestamp = DateTime.MinValue, x => x.Flag = false, x => x.Id = Guid.Empty,
                x => x.Empty = "x", x => x.Number = 11
            };
            var restore = new Action<Consumer.Rules.RulesRow>[]
            {
                x => x.Text = "AB", x => x.Mail = "a@example.com", x => x.Amount = 12.34m,
                x => x.Large = 4294967296L, x => x.Ratio = 0.5, x => x.Day = new DateOnly(2026, 6, 15),
                x => x.Timestamp = new DateTime(2026, 6, 15, 13, 14, 15).AddTicks(1234567), x => x.Flag = true,
                x => x.Id = Guid.Parse("01234567-89ab-cdef-0123-456789abcdef"), x => x.Empty = "", x => x.Number = 0
            };
            for (var i = 0; i < mutations.Length; i++)
            {
                mutations[i](validRow);
                if (!(await rules.ValidateAsync(validRow)).HasErrors) throw new Exception("Invalid rule accepted: " + i);
                restore[i](validRow);
            }
            Console.WriteLine("Generated definitions passed.");
            """);
        var result = await RunDotNetAsync("run", "--project", Path.Combine(_runtime, "Runtime.csproj"), "-v:q", "-p:CoreExLocalTargetFramework=net" + Environment.Version.Major + ".0");
        result.Should().Contain("Generated definitions passed.");
    }

    private static string FindRepository()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (System.IO.File.Exists(Path.Combine(directory.FullName, "CoreEx.slnx")))
                return directory.FullName;

        throw new DirectoryNotFoundException("Cannot locate CoreEx.slnx.");
    }

    private static async Task<string> RunDotNetAsync(params string[] args)
    {
        using var process = new Process { StartInfo = new("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false } };
        process.StartInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        process.StartInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";
        foreach (var argument in args)
            process.StartInfo.ArgumentList.Add(argument);

        process.StartInfo.ArgumentList.Add("--disable-build-servers");
        process.Start();
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        var text = await output + await error;
        process.ExitCode.Should().Be(0, text);
        return text;
    }
}
