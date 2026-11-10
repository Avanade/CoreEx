namespace CoreEx.CodeGen.Test.Unit;

[TestFixture]
public class UnorderedFileGenerationTests
{
    private string _directory = null!;
    private string _runtime = null!;
    private string _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = FindRepository();
        _directory = Path.Combine(_repository, ".artifacts", "UnorderedFileCodeGen." + Guid.NewGuid().ToString("N"));
        _runtime = Path.Combine(_directory, "Runtime");
        Directory.CreateDirectory(_runtime);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "GlobalUsing.cs"), """
            global using CoreEx;
            global using CoreEx.DependencyInjection;
            global using CoreEx.IO;
            global using CoreEx.IO.Csv;
            global using CoreEx.Results;
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

    private void Write(string name, string content) => System.IO.File.WriteAllText(Path.Combine(_directory, name), content);

    private static string Definition(string name = "Mixed") => $$"""
        name: {{name}}
        version: r1
        runtimeProjectPath: Runtime
        runtimeNamespace: Consumer.{{name}}
        csv:
          discriminatorIndex: 0
        mode: ValidateThenExecute
        errorMode: CollectErrors
        records:
        - name: Header
          csv: { match: H, cardinality: One, position: First }
          properties:
          - { name: Batch, csv: { index: 1 }, required: true }
        - name: Alpha
          csv: { match: A }
          properties:
          - { name: Text, csv: { index: 1 }, required: true }
        - name: Beta
          csv: { match: B, cardinality: OneOrMore }
          properties:
          - { name: Number, type: Integer, csv: { index: 1 }, required: true }
        - name: Trailer
          csv: { match: T, cardinality: One, position: Last }
          properties:
          - { name: Total, type: Integer, csv: { index: 1 }, required: true }
        """;

    [Test]
    public async Task ValidateAndSimulationProduceNoFiles()
    {
        Write("mixed-file.yaml", Definition());
        var statistics = await FileGenerationBatch.GenerateAsync(Args(), validateOnly: true);
        var config = (Files.Config.CodeGenConfig)statistics.RootConfig!;
        config.IsUnordered.Should().BeTrue();
        config.IsHierarchical.Should().BeFalse();
        config.UnitType.Should().Be("MixedRecord");
        config.Records![1].Csv!.Cardinality.Should().Be("Many");
        config.Records[1].Csv!.Position.Should().Be("Any");
        await FileGenerationBatch.GenerateAsync(Args(simulation: true));
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task GeneratedTypedDispatch_IsDeterministicAndPreservesHandwrittenHandler()
    {
        Write("mixed-file.yaml", Definition());
        await FileGenerationBatch.GenerateAsync(Args());
        var output = Path.Combine(_runtime, "Files", "Mixed");
        System.IO.File.ReadAllText(Path.Combine(output, "MixedAlpha.g.cs")).Should().Contain("class MixedAlpha : MixedRecord");
        System.IO.File.ReadAllText(Path.Combine(output, "MixedContext.g.cs")).Should().Contain("class MixedContext : CsvRoutedContext");
        System.IO.File.ReadAllText(Path.Combine(output, "MixedProcessor.g.cs")).Should()
            .Contain("CsvRoutedFileProcessor<MixedRecord>")
            .And.Contain("CsvRecordCardinality.OneOrMore")
            .And.Contain("CsvRecordPosition.First")
            .And.Contain("CsvRecordPosition.Last");
        var dispatch = System.IO.File.ReadAllText(Path.Combine(output, "MixedHandlerBase.g.cs"));
        dispatch.Should().Contain("MixedAlpha => OnExecuteAlphaAsync(args.As<MixedAlpha>(), cancellationToken)")
            .And.Contain("MixedBeta => OnValidateBetaAsync(args.As<MixedBeta>(), cancellationToken)")
            .And.NotContain("(MixedAlpha)").And.NotContain("(MixedBeta)");
        var starter = Path.Combine(output, "MixedHandler.cs");
        System.IO.File.AppendAllText(starter, "\n// Preserved application implementation.\n");
        await FileGenerationBatch.GenerateAsync(Args(unchanged: true));
        System.IO.File.ReadAllText(starter).Should().Contain("class MixedHandler : MixedHandlerBase").And.Contain("Preserved application implementation.");
        Directory.GetFiles(output, "*Group.g.cs").Should().BeEmpty();
    }

    [TestCase("discriminatorIndex: 0", "discriminatorIndex: -1")]
    [TestCase("discriminatorIndex: 0", "")]
    [TestCase("csv: { match: A }", "csv: { match: H }")]
    [TestCase("csv: { match: A }", "csv: { match: ' ' }")]
    [TestCase("csv: { index: 1 }", "csv: { index: 0 }")]
    [TestCase("csv: { index: 1 }", "csv: { column: Value }")]
    [TestCase("position: First", "position: Middle")]
    [TestCase("cardinality: One, position: First", "cardinality: Many, position: First")]
    [TestCase("cardinality: One, position: Last", "cardinality: Optional, position: First")]
    [TestCase("name: Alpha", "name: Record")]
    [TestCase("name: Alpha", "name: HandlerBase")]
    [TestCase("csv: { match: B, cardinality: OneOrMore }", "csv: { match: B, cardinality: Unknown }")]
    public async Task InvalidUnorderedDefinitions_FailBeforeAnyWrites(string original, string replacement)
    {
        Write("mixed-file.yaml", Definition().Replace("\r\n", "\n").Replace(original, replacement));
        var action = () => FileGenerationBatch.GenerateAsync(Args());
        await action.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task HierarchicalDefinitions_RejectRouteCardinalityAndPosition()
    {
        Write("mixed-file.yaml", Definition().Replace("discriminatorIndex: 0", """
            discriminatorIndex: 0
            structure:
              root: { record: Alpha, cardinality: Many }
            """));
        var action = () => FileGenerationBatch.GenerateAsync(Args());
        await action.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task GeneratedMetadataRoutes_CompileWithUnorderedProcessor()
    {
        Write("metadata-file.yaml", """
            name: Metadata
            version: r1
            runtimeProjectPath: Runtime
            runtimeNamespace: Consumer.Metadata
            csv:
              discriminatorIndex: 0
            metadata:
            - { name: Header, record: Header, required: true, csv: { position: First } }
            records:
            - name: Header
              csv: { match: H }
              properties:
              - { name: Batch, csv: { index: 1 }, required: true }
            - name: Order
              csv: { match: O }
              properties:
              - { name: Id, type: Integer, csv: { index: 1 }, required: true }
            """);

        await FileGenerationBatch.GenerateAsync(Args());
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Program.cs"), "Console.WriteLine(\"Generated metadata definition compiled.\");");

        var output = await RunDotNetAsync("build", Path.Combine(_runtime, "Runtime.csproj"), "-v:q",
            "-p:CoreExLocalTargetFramework=net" + Environment.Version.Major + ".0");

        output.Should().Contain("Build succeeded.");
    }

    [Test]
    public async Task FlatDefinitions_RejectRouteCardinality()
    {
        Write("flat-file.yaml", """
            name: Flat
            version: r1
            runtimeProjectPath: Runtime
            records:
            - name: Row
              csv: { cardinality: One }
              properties:
              - { name: Text, csv: { index: 0 } }
            """);
        var action = () => FileGenerationBatch.GenerateAsync(Args());
        await action.Should().ThrowAsync<CodeGenException>();
    }

    [Test]
    public async Task GeneratedUnorderedDefinitions_CompileAndExecuteTypedRowsInSeparateScopes()
    {
        Write("mixed-file.yaml", Definition());
        Write("other-file.yaml", Definition("Other"));
        Write("flat-file.yaml", """
            name: Flat
            version: r1
            runtimeProjectPath: Runtime
            runtimeNamespace: Consumer.Flat
            records:
            - name: Row
              properties:
              - { name: Text, csv: { index: 0 } }
            """);
        Write("nested-file.yaml", """
            name: Nested
            version: r1
            runtimeProjectPath: Runtime
            runtimeNamespace: Consumer.Nested
            csv:
              discriminatorIndex: 0
              structure:
                root:
                  record: Root
                  cardinality: One
                  children:
                  - { record: Child, cardinality: Many }
            records:
            - name: Root
              csv: { match: R }
              properties:
              - { name: Text, csv: { index: 1 } }
            - name: Child
              csv: { match: C }
              properties:
              - { name: Text, csv: { index: 1 } }
            """);
        await FileGenerationBatch.GenerateAsync(Args());
        System.IO.File.WriteAllText(Path.Combine(_runtime, "MixedContext.cs"), """
            namespace Consumer.Mixed;
            public partial class MixedContext
            {
                public int Sum { get; set; }
                public int? Declared { get; set; }
            }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "MixedProcessor.cs"), """
            namespace Consumer.Mixed;
            public partial class MixedProcessor
            {
                protected override Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
                {
                    if (args.Context is MixedContext typed && !typed.HasValidationErrors && typed.Sum != typed.Declared)
                        return args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "file.total", "Total does not match."), cancellationToken);

                    return Task.CompletedTask;
                }
            }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Recorder.cs"), """
            namespace Consumer.Mixed;
            public class Recorder
            {
                public List<MixedRecord> Validated { get; } = [];
                public List<MixedRecord> Executed { get; } = [];
                public List<CoreEx.ExecutionContext> Scopes { get; } = [];
                public List<MixedContext> ExecutionContexts { get; } = [];
                public int Disposed { get; set; }
                public bool FailBeta { get; set; }
                public Result Failure { get; } = Result.NotFoundError();
            }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "MixedHandler.Custom.cs"), """
            namespace Consumer.Mixed;
            public partial class MixedHandler : IDisposable
            {
                private readonly Recorder _recorder;
                public MixedHandler(Recorder recorder, CoreEx.ExecutionContext ambient)
                {
                    _recorder = recorder;
                    recorder.Scopes.Add(ambient);
                }
                protected override Task OnValidateHeaderAsync(FileUnitValidationArgs<MixedHeader> args, CancellationToken cancellationToken)
                    => Validate(args.Unit, cancellationToken);
                protected override Task OnValidateAlphaAsync(FileUnitValidationArgs<MixedAlpha> args, CancellationToken cancellationToken)
                    => Validate(args.Unit, cancellationToken);
                protected override Task OnValidateBetaAsync(FileUnitValidationArgs<MixedBeta> args, CancellationToken cancellationToken)
                {
                    ((MixedContext)args.Context).Sum += args.Unit.Number ?? 0;
                    return Validate(args.Unit, cancellationToken);
                }
                protected override Task OnValidateTrailerAsync(FileUnitValidationArgs<MixedTrailer> args, CancellationToken cancellationToken)
                {
                    ((MixedContext)args.Context).Declared = args.Unit.Total;
                    return Validate(args.Unit, cancellationToken);
                }
                private Task Validate(MixedRecord unit, CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _recorder.Validated.Add(unit);
                    return Task.CompletedTask;
                }
                protected override Task<Result> OnExecuteHeaderAsync(FileUnitArgs<MixedHeader> args, CancellationToken cancellationToken) => Execute(args.Unit, ((MixedContext)args.Context), cancellationToken);
                protected override Task<Result> OnExecuteAlphaAsync(FileUnitArgs<MixedAlpha> args, CancellationToken cancellationToken) => Execute(args.Unit, ((MixedContext)args.Context), cancellationToken);
                protected override Task<Result> OnExecuteBetaAsync(FileUnitArgs<MixedBeta> args, CancellationToken cancellationToken) => Execute(args.Unit, ((MixedContext)args.Context), cancellationToken);
                protected override Task<Result> OnExecuteTrailerAsync(FileUnitArgs<MixedTrailer> args, CancellationToken cancellationToken) => Execute(args.Unit, ((MixedContext)args.Context), cancellationToken);
                private Task<Result> Execute(MixedRecord unit, MixedContext context, CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (context.Sum != 0 || context.Declared is not null) throw new Exception("Validation state leaked into replay.");
                    _recorder.Executed.Add(unit);
                    _recorder.ExecutionContexts.Add(context);
                    return Task.FromResult(unit is MixedBeta && _recorder.FailBeta ? _recorder.Failure : Result.Success);
                }
                public void Dispose() => _recorder.Disposed++;
            }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Program.cs"), """
            var services = new ServiceCollection();
            services.AddExecutionContext();
            services.AddSingleton<Consumer.Mixed.Recorder>();
            services.AddDynamicServicesUsing<Consumer.Mixed.MixedProcessor>();
            await using var provider = services.BuildServiceProvider();
            var recorder = provider.GetRequiredService<Consumer.Mixed.Recorder>();
            var mixed = provider.GetRequiredService<Consumer.Mixed.MixedProcessor>();
            var other = provider.GetRequiredService<Consumer.Other.OtherProcessor>();
            const string text = "H,batch\nB,2\nA,one\nB,3\nT,5\n";
            static Task<Stream> Open(string input) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(input)));
            var context = Consumer.Mixed.MixedProcessor.CreateContext("source", "attempt", "revision");
            var opens = 0;
            var summary = await mixed.ProcessAsync(_ => { opens++; return Open(text); }, context);
            if (summary.Outcome != FileProcessingOutcome.Succeeded || opens != 2 || summary.Progress.UnitsExecuted != 5) throw new Exception("Each routed row must execute once after acceptance.");
            if (context.GetRecordCount("Beta") != 2 || recorder.Validated.Count != 5 || recorder.Executed.Count != 5) throw new Exception("Typed records were not counted/dispatched.");
            if (recorder.Executed[1] is not Consumer.Mixed.MixedBeta { Number: 2 } || recorder.Executed[2] is not Consumer.Mixed.MixedAlpha { Text: "one" }) throw new Exception("Heterogeneous row types were lost.");
            if (ReferenceEquals(recorder.Validated[0], recorder.Executed[0])) throw new Exception("Replay reused validation records.");
            if (recorder.Disposed != 10 || recorder.Scopes.Distinct().Count() != 10) throw new Exception("Each row and pass needs a fresh disposed scope.");
            if (ReferenceEquals(context, recorder.ExecutionContexts[0]) || !ReferenceEquals(context.Identity, recorder.ExecutionContexts[0].Identity)) throw new Exception("Replay context identity/state incorrect.");
            var validateOnly = await other.ProcessAsync(_ => Open(text), Consumer.Other.OtherProcessor.CreateContext("source", "attempt", mode: FileProcessingMode.ValidateOnly));
            if (!validateOnly.IsSuccess || validateOnly.Progress.UnitsExecuted != 0) throw new Exception("Validation-only required execution overrides.");
            try
            {
                await other.ProcessAsync(_ => Open(text), Consumer.Other.OtherProcessor.CreateContext("source", "attempt"));
                throw new Exception("Unimplemented execution must fail explicitly.");
            }
            catch (NotSupportedException) { }
            var before = recorder.Executed.Count;
            foreach (var invalid in new[] { text.Replace("T,5", "T,8"), text.Replace("B,2", "B,bad"), text.Replace("T,5\n", "") })
            {
                opens = 0;
                var rejected = await mixed.ProcessAsync(_ => { opens++; return Open(invalid); }, Consumer.Mixed.MixedProcessor.CreateContext("source", "attempt"));
                if (rejected.Outcome != FileProcessingOutcome.ContentFailed || opens != 1 || recorder.Executed.Count != before) throw new Exception("Invalid file performed writes.");
            }
            recorder.FailBeta = true;
            var failed = await mixed.ProcessAsync(_ => Open(text), Consumer.Mixed.MixedProcessor.CreateContext("source", "attempt"));
            if (failed.Outcome != FileProcessingOutcome.ExecutionFailed || failed.Progress.UnitsAttempted != 2 || failed.Progress.UnitsExecuted != 1 || failed.ExecutionResult != recorder.Failure) throw new Exception("Original business Result/counts not preserved.");
            var opened = false;
            try
            {
                await mixed.ProcessAsync(_ => { opened = true; return Open(text); }, Consumer.Other.OtherProcessor.CreateContext("source", "attempt"));
                throw new Exception("Wrong generated context accepted.");
            }
            catch (ArgumentException) when (!opened) { }
            var flat = provider.GetRequiredService<Consumer.Flat.FlatProcessor>();
            var nested = provider.GetRequiredService<Consumer.Nested.NestedProcessor>();
            if (!(await flat.ProcessAsync(_ => Open("flat\n"), Consumer.Flat.FlatProcessor.CreateContext("source", "attempt"))).IsSuccess) throw new Exception("Flat coexistence failed.");
            if (!(await nested.ProcessAsync(_ => Open("R,root\nC,child\n"), Consumer.Nested.NestedProcessor.CreateContext("source", "attempt"))).IsSuccess) throw new Exception("Hierarchy coexistence failed.");
            Console.WriteLine("Generated unordered definitions passed.");
            """);
        var output = await RunDotNetAsync("run", "--project", Path.Combine(_runtime, "Runtime.csproj"), "-v:q", "-p:CoreExLocalTargetFramework=net" + Environment.Version.Major + ".0");
        output.Should().Contain("Generated unordered definitions passed.");
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
