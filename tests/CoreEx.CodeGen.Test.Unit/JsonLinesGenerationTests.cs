namespace CoreEx.CodeGen.Test.Unit;

public partial class FileGenerationTests
{
    [TestCase("Csv", "limits: { maxInputBytes: 1 }")]
    [TestCase("Csv", "csv: { maxGroupRecords: 1 }")]
    [TestCase("Jsonl", "json: { maxTokenBytes: 4 }")]
    [TestCase("Jsonl", "json: { maxObjectMembers: 1 }")]
    public async Task RemovedQuotaSettingsRejectBeforeGeneration(string format, string settings)
    {
        Write("retired-file.yaml", $$"""
            name: Retired
            version: '1'
            runtimeProjectPath: Runtime
            format: {{format}}
            {{settings}}
            records:
            - name: Row
              properties:
              - { name: Code }
            """);
        Func<Task> generate = () => FileGenerationBatch.GenerateAsync(Args());
        await generate.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    private string JsonLinesDefinition(string file)
        => System.IO.File.ReadAllText(Path.Combine(_repository, "tests", "CoreEx.CodeGen.Test.Unit", file))
            .Replace("runtimeProjectPath: ../CoreEx.IO.Test.Unit", "runtimeProjectPath: Runtime", StringComparison.Ordinal);

    [Test]
    public async Task NestedJsonLinesConsumersCompileRunAndPreserveHandwrittenHandlers()
    {
        Write("orders-file.yaml", JsonLinesDefinition("orders-jsonl-file.yaml"));
        Write("routed-file.yaml", JsonLinesDefinition("routed-jsonl-file.yaml"));
        Write("scalars-file.yaml", """
            name: Scalars
            version: '1'
            format: Jsonl
            runtimeProjectPath: Runtime
            runtimeNamespace: CoreEx.IO.Test.Unit.Files.Scalars
            records:
            - name: Row
              properties:
              - { name: 'a~/b', property: Code, required: true }
              - { name: When, type: DateTimeOffset, min: '1 January 2024 00:00 +10:00', options: ['15 March 2026 10:30 +10:00'] }
              - { name: Local, type: DateTime, min: '15 March 2026 10:00' }
              - { name: Day, type: DateOnly, options: ['15 March 2026'] }
              - { name: Dates, collection: { type: DateOnly }, format: yyyyMMdd }
              - { name: Instants, collection: { type: DateTimeOffset }, format: "yyyy-MM-dd'T'HH:mm:ssK" }
            """);
        Write("dates-csv-file.yaml", """
            name: DatesCsv
            version: '1'
            runtimeProjectPath: Runtime
            runtimeNamespace: CoreEx.IO.Test.Unit.Files.DatesCsv
            records:
            - name: Row
              properties:
              - { name: Day, type: DateOnly, required: true, min: '1 March 2026' }
              - { name: Moment, type: DateTime, required: true, min: '15 March 2026 10:00' }
              - { name: Instant, type: DateTimeOffset, required: true, options: ['15 March 2026 10:30 +10:00'] }
              - { name: Exact, type: DateOnly, format: yyyyMMdd, required: true }
            """);
        Write("collections-file.yaml", """
            name: Collections
            version: '1'
            format: Json
            runtimeProjectPath: Runtime
            runtimeNamespace: CoreEx.IO.Test.Unit.Files.Collections
            unit: { record: Row, kind: Object }
            records:
            - name: Row
              properties:
              - { name: One, collection: { type: String, cardinality: One, maxItems: 2 } }
              - { name: Optional, collection: { type: String, cardinality: Optional, maxItems: 2 } }
              - { name: Many, collection: { type: String, maxItems: 2 } }
              - { name: AtLeastOne, collection: { type: String, cardinality: OneOrMore } }
            """);
        System.IO.File.AppendAllText(Path.Combine(_runtime, "GlobalUsing.cs"), """

            global using CoreEx.IO.Json;
            global using CoreEx.IO.JsonLines;
            """);
        await FileGenerationBatch.GenerateAsync(Args());
        var handler = Path.Combine(_runtime, "Files", "OrdersJsonl", "OrdersJsonlHandler.cs");
        System.IO.File.AppendAllText(handler, "\n// Handwritten ownership sentinel.\n");
        await FileGenerationBatch.GenerateAsync(Args(unchanged: true));
        System.IO.File.ReadAllText(handler).Should().Contain("Handwritten ownership sentinel.");

        System.IO.File.WriteAllText(Path.Combine(_runtime, "Extensions.cs"), """
            namespace CoreEx.IO.Test.Unit.Files.OrdersJsonl;
            public partial class OrdersJsonlHandler
            {
                protected override Task<CoreEx.Results.Result> OnExecuteAsync(FileUnitArgs<OrdersJsonlOrder> args, CancellationToken cancellationToken)
                {
                    if (args.Unit.Lines?.Sum(x => x.Quantity) is not > 0) throw new Exception("Nested graph lost.");
                    if (args.Unit.SubmittedAt?.Offset != TimeSpan.FromHours(10) && args.Info.Ordinal == 1) throw new Exception("Offset lost.");
                    return Task.FromResult(CoreEx.Results.Result.Success);
                }
            }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "RoutedExtensions.cs"), """
            namespace CoreEx.IO.Test.Unit.Files.RoutedJsonl;
            public partial class RoutedJsonlHandler
            {
                protected override Task<CoreEx.Results.Result> OnExecuteAsync(FileUnitArgs<RoutedJsonlOrder> args, CancellationToken cancellationToken)
                {
                    var context = (RoutedJsonlContext)args.Context;
                    if (context.GetHeaderMetadata()?.BatchId != "B123") throw new Exception("Encountered header missing.");
                    if (context.GetTrailerMetadata() is not null) throw new Exception("Future trailer leaked.");
                    if (context.Mode == FileProcessingMode.ValidateAndExecute && context.AcceptedMetadata is not null) throw new Exception("Accepted metadata leaked.");
                    if (context.Mode == FileProcessingMode.ValidateThenExecute && context.GetAcceptedTrailerMetadata()?.QuantityTotal != 5) throw new Exception("Accepted trailer missing.");
                    return Task.FromResult(CoreEx.Results.Result.Success);
                }
            }
            """);
        System.IO.File.WriteAllText(Path.Combine(_runtime, "ScalarsExtensions.cs"), """
            namespace CoreEx.IO.Test.Unit.Files.Scalars;
            public partial class ScalarsProcessor
            {
                public List<FileDiagnostic> Findings { get; } = [];
                protected override Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
                {
                    Findings.Add(args.Diagnostic);
                    return Task.CompletedTask;
                }
            }
            """);
        var fixtures = Path.Combine(_repository, "docs", "design", "io-multiformat", "phase0-fixtures");
        System.IO.File.Copy(Path.Combine(fixtures, "valid.jsonl"), Path.Combine(_runtime, "valid.jsonl"));
        System.IO.File.Copy(Path.Combine(fixtures, "routed.jsonl"), Path.Combine(_runtime, "routed.jsonl"));
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Program.cs"), $$"""
            await using var services = new ServiceCollection().AddExecutionContext()
                .AddDynamicServicesUsing<CoreEx.IO.Test.Unit.Files.OrdersJsonl.OrdersJsonlProcessor>().BuildServiceProvider();
            var plain = services.GetRequiredService<CoreEx.IO.Test.Unit.Files.OrdersJsonl.OrdersJsonlProcessor>();
            var routed = services.GetRequiredService<CoreEx.IO.Test.Unit.Files.RoutedJsonl.RoutedJsonlProcessor>();
            foreach (var mode in Enum.GetValues<FileProcessingMode>())
            {
                var opens = 0;
                var summary = await plain.ProcessAsync(_ =>
                {
                    opens++;
                    return Task.FromResult<Stream>(File.OpenRead({{OnRamp.Utility.CSharpSource.StringLiteral(Path.Combine(_runtime, "valid.jsonl"))}}));
                }, CoreEx.IO.Test.Unit.Files.OrdersJsonl.OrdersJsonlProcessor.CreateContext("immutable/source", "attempt", "revision", mode));
                if (!summary.IsSuccess || summary.Progress.UnitsValidated != 2 || opens != (mode == FileProcessingMode.ValidateThenExecute ? 2 : 1))
                    throw new Exception("Generated plain lifecycle failed.");
                var context = CoreEx.IO.Test.Unit.Files.RoutedJsonl.RoutedJsonlProcessor.CreateContext("immutable/source", "attempt", "revision", mode);
                summary = await routed.ProcessAsync(_ => Task.FromResult<Stream>(File.OpenRead({{OnRamp.Utility.CSharpSource.StringLiteral(Path.Combine(_runtime, "routed.jsonl"))}})), context);
                if (!summary.IsSuccess || summary.Progress.RecordsRead != 4 || summary.Progress.UnitsRead != 2 || context.GetAcceptedTrailerMetadata()?.QuantityTotal != 5)
                    throw new Exception("Generated routing failed.");
                var copy = context.GetAcceptedHeaderMetadata() ?? throw new Exception("Missing accepted metadata.");
                copy.BatchId = "changed";
                if (context.GetAcceptedHeaderMetadata()?.BatchId != "B123") throw new Exception("Mutable accepted metadata.");
            }
            var scalars = services.GetRequiredService<CoreEx.IO.Test.Unit.Files.Scalars.ScalarsProcessor>();
            var scalarText = "{\"a~/b\":\"ok\",\"When\":\"15 March 2026 10:30 +10:00\",\"Local\":\"15 March 2026 10:30\",\"Day\":\"15 March 2026\",\"Dates\":[\"20260315\"],\"Instants\":[\"2026-03-15T10:30:00+10:00\"]}";
            var scalarSummary = await scalars.ProcessAsync(_ => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(scalarText))),
                CoreEx.IO.Test.Unit.Files.Scalars.ScalarsProcessor.CreateContext("immutable/source", "attempt", "revision"));
            if (!scalarSummary.IsSuccess) throw new Exception("Date bounds/options or scalar-collection conversion failed.");
            scalarSummary = await scalars.ProcessAsync(_ => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(scalarText.Replace("20260315", "2026-03-15")))),
                CoreEx.IO.Test.Unit.Files.Scalars.ScalarsProcessor.CreateContext("immutable/source", "attempt", "revision"));
            if (scalarSummary.IsSuccess || scalars.Findings.Single().Location?.SourcePath != "/Dates/0")
                throw new Exception("Explicit JSON date format fell back to standard parsing.");
            scalars.Findings.Clear();
            scalarSummary = await scalars.ProcessAsync(_ => Task.FromResult<Stream>(new MemoryStream("{}"u8.ToArray())),
                CoreEx.IO.Test.Unit.Files.Scalars.ScalarsProcessor.CreateContext("immutable/source", "attempt", "revision"));
            if (scalarSummary.IsSuccess || scalars.Findings.Single().Location?.PropertyPath != "Code" || scalars.Findings.Single().Location?.SourcePath != "/a~0~1b")
                throw new Exception("Escaped source member and CLR property paths were conflated.");
            var csv = services.GetRequiredService<CoreEx.IO.Test.Unit.Files.DatesCsv.DatesCsvProcessor>();
            var csvText = "Day,Moment,Instant,Exact\n15 March 2026,15 March 2026 10:30,15 March 2026 10:30 +10:00,20260315\n";
            foreach (var exact in new[] { true, false })
            {
                var input = exact ? csvText : csvText.Replace("20260315", "2026-03-15");
                var csvSummary = await csv.ProcessAsync(_ => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input))),
                    CoreEx.IO.Test.Unit.Files.DatesCsv.DatesCsvProcessor.CreateContext("immutable/source", "attempt", "revision"));
                if (csvSummary.IsSuccess != exact) throw new Exception("Generated CSV standard/exact date conversion failed.");
            }
            var collections = services.GetRequiredService<CoreEx.IO.Test.Unit.Files.Collections.CollectionsProcessor>();
            foreach (var (input, valid) in new (string, bool)[]
            {
                ("{\"One\":[\"x\"],\"AtLeastOne\":[\"x\"]}", true),
                ("{\"One\":[\"x\"],\"Optional\":[],\"Many\":[],\"AtLeastOne\":[\"x\"]}", true),
                ("{\"AtLeastOne\":[\"x\"]}", false),
                ("{\"One\":null,\"AtLeastOne\":[\"x\"]}", false),
                ("{\"One\":[],\"AtLeastOne\":[\"x\"]}", false),
                ("{\"One\":[\"x\",\"y\"],\"AtLeastOne\":[\"x\"]}", false),
                ("{\"One\":[\"x\"],\"Optional\":[\"x\",\"y\"],\"AtLeastOne\":[\"x\"]}", false),
                ("{\"One\":[\"x\"],\"Many\":[\"x\",\"y\",\"z\"],\"AtLeastOne\":[\"x\"]}", false),
                ("{\"One\":[\"x\"],\"Many\":[null],\"AtLeastOne\":[\"x\"]}", false),
                ("{\"One\":[\"x\"]}", false),
                ("{\"One\":[\"x\"],\"AtLeastOne\":null}", false),
                ("{\"One\":[\"x\"],\"AtLeastOne\":[]}", false)
            })
            {
                var result = await collections.ProcessAsync(_ => Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(input))),
                    CoreEx.IO.Test.Unit.Files.Collections.CollectionsProcessor.CreateContext("immutable/source", "attempt", "revision"));
                if (result.IsSuccess != valid || !valid && result.Progress.UnitsValidated != 0)
                    throw new Exception("Generated collection validation failed: " + input);
            }
            Console.WriteLine("Generated JSONL consumers passed.");
            """);
        (await RunDotNetAsync("run", "--project", Path.Combine(_runtime, "Runtime.csproj"), "-v:q", "-p:CoreExLocalTargetFramework=net10.0"))
            .Should().Contain("Generated JSONL consumers passed.");
    }

    [TestCase("format: Jsonl", "format: Json\nunit: { record: Order, kind: Object, unitPath: /orders }")]
    [TestCase("format: Jsonl", "format: Xml")]
    [TestCase("format: Jsonl", "format: Jsonl\ncsv: { delimiter: '|' }")]
    [TestCase("format: Jsonl", "format: Jsonl\njson: { kind: Array }")]
    [TestCase("format: Jsonl", "format: Jsonl\njson: { unitPath: /orders }")]
    [TestCase("record: Address, required", "record: Missing, required")]
    [TestCase("record: Address, required", "record: Order, required")]
    [TestCase("record: Address, required", "record: Address, type: String, required")]
    [TestCase("record: Address, required", "record: Address, mandatory: true, required")]
    [TestCase("maxItems: 1000", "maxItems: 0")]
    [TestCase("type: DateTimeOffset", "type: DateTimeOffset, format: yyyyMMdd")]
    [TestCase("type: DateTimeOffset", "type: DateTimeOffset, min: '2024-01-01T00:00:00'")]
    [TestCase("type: DateTimeOffset", "type: DateTimeOffset, options: ['2024-01-01T00:00:00']")]
    [TestCase("collection: { type: String, maxItems: 100 }", "collection: { type: DateTimeOffset }, format: yyyyMMdd")]
    [TestCase("name: Id, required", "name: Id, csv: { index: 0 }, required")]
    [TestCase("name: EffectiveDate, type", "name: effective-date, property: Id, type")]
    public async Task JsonLinesRejectsInvalidConfigurationBeforeWriting(string before, string after)
    {
        Write("orders-file.yaml", JsonLinesDefinition("orders-jsonl-file.yaml").Replace(before, after, StringComparison.Ordinal));
        Func<Task> generate = () => FileGenerationBatch.GenerateAsync(Args());
        await generate.Should().ThrowAsync<CodeGenException>();
        Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
    }

    [Test]
    public async Task JsonLinesRoutesMustBeExplicitUniqueAndNotOnNestedRecords()
    {
        foreach (var definition in new[]
        {
            JsonLinesDefinition("routed-jsonl-file.yaml").Replace("jsonl: { discriminator: type }", ""),
            JsonLinesDefinition("routed-jsonl-file.yaml").Replace("match: trailer", "match: order"),
            JsonLinesDefinition("routed-jsonl-file.yaml").Replace("jsonl: { match: order }", ""),
            JsonLinesDefinition("orders-jsonl-file.yaml").Replace("- name: Address", "- name: Address\n  jsonl: { match: address }")
        })
        {
            Write("bad-file.yaml", definition);
            Func<Task> generate = () => FileGenerationBatch.GenerateAsync(Args());
            await generate.Should().ThrowAsync<CodeGenException>();
            Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
        }
    }

    [Test]
    public async Task JsonDocumentConsumersCompileAndRunRootArrayEnvelopeAndRootObjectInputs()
    {
        const string definition = """
            name: ArrayImport
            version: '1'
            format: Json
            runtimeProjectPath: Runtime
            runtimeNamespace: Consumer.ArrayImport
            unit: { record: Order, unitPath: /orders, cardinality: OneOrMore }
            records:
            - name: Order
              properties:
              - { name: Id, required: true }
              - { name: Address, record: Address, required: true }
            - name: Address
              properties:
              - { name: Code, required: true }
            """;
        Write("array-file.yaml", definition);
        Write("envelope-file.yaml", """
            name: EnvelopeImport
            version: '1'
            format: Json
            runtimeProjectPath: Runtime
            runtimeNamespace: Consumer.EnvelopeImport
            unit: { record: Order, unitPath: /orders, cardinality: OneOrMore }
            metadata:
            - { name: Batch, record: Batch, required: true, json: { path: /batch } }
            records:
            - name: Order
              properties:
              - { name: Id, required: true }
              - { name: Address, record: Address, required: true }
            - name: Address
              properties:
              - { name: Code, required: true }
            - name: Batch
              properties:
              - { name: BatchId, required: true }
            """);
        Write("object-file.yaml", """
            name: ObjectImport
            version: '1'
            format: Json
            runtimeProjectPath: Runtime
            runtimeNamespace: Consumer.ObjectImport
            unit: { record: Order, kind: Object }
            records:
            - name: Order
              properties:
              - { name: Id, required: true }
              - { name: Address, record: Address, required: true }
            - name: Address
              properties:
              - { name: Code, required: true }
            """);
        System.IO.File.AppendAllText(Path.Combine(_runtime, "GlobalUsing.cs"), "\nglobal using CoreEx.IO.Json;\n");
        await FileGenerationBatch.GenerateAsync(Args());
        System.IO.File.WriteAllText(Path.Combine(_runtime, "Program.cs"), """
            using Consumer.ArrayImport;
            using Consumer.EnvelopeImport;
            using Consumer.ObjectImport;
            await using var services = new ServiceCollection().AddExecutionContext()
                .AddDynamicServicesUsing<ArrayImportProcessor, EnvelopeImportProcessor, ObjectImportProcessor>().BuildServiceProvider();
            static Func<CancellationToken, Task<Stream>> Source(string text) => _ =>
                Task.FromResult<Stream>(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text)));
            var array = services.GetRequiredService<ArrayImportProcessor>();
            var result = await array.ProcessAsync(Source("{\"orders\":[{\"Id\":\"A\",\"Address\":{\"Code\":\"X\"}}]}"),
                ArrayImportProcessor.CreateContext("array", "attempt"));
            if (!result.IsSuccess || result.Progress.UnitsValidated != 1) throw new Exception("Root-array consumer failed.");
            var envelope = services.GetRequiredService<EnvelopeImportProcessor>();
            var envelopeContext = EnvelopeImportProcessor.CreateContext("envelope", "attempt");
            result = await envelope.ProcessAsync(Source("{\"orders\":[{\"Id\":\"B\",\"Address\":{\"Code\":\"Y\"}}],\"batch\":{\"BatchId\":\"batch-1\"}}"), envelopeContext);
            if (!result.IsSuccess || envelopeContext.GetAcceptedBatchMetadata()?.BatchId != "batch-1")
                throw new Exception("Envelope metadata consumer failed.");
            var single = services.GetRequiredService<ObjectImportProcessor>();
            result = await single.ProcessAsync(Source("{\"Id\":\"C\",\"Address\":{\"Code\":\"Z\"}}"),
                ObjectImportProcessor.CreateContext("object", "attempt"));
            if (!result.IsSuccess || result.Progress.UnitsValidated != 1) throw new Exception("Root-object consumer failed.");
            Console.WriteLine("Generated JSON document consumers passed.");
            """);
        (await RunDotNetAsync("run", "--project", Path.Combine(_runtime, "Runtime.csproj"), "-v:q", "-p:CoreExLocalTargetFramework=net10.0"))
            .Should().Contain("Generated JSON document consumers passed.");
    }

    [Test]
    public async Task JsonDocumentSelectorsRejectInvalidPointersAndOverlapsBeforeWrites()
    {
        var source = """
            name: Documents
            version: '1'
            format: Json
            runtimeProjectPath: Runtime
            unit: { record: Order, unitPath: /orders }
            metadata:
            - { name: Batch, record: Batch, required: true, json: { path: /batch } }
            records:
            - name: Order
              properties:
              - { name: Id }
            - name: Batch
              properties:
              - { name: Id }
            """;
        foreach (var invalid in new[]
        {
            source.Replace("/orders", "/orders~2", StringComparison.Ordinal),
            source.Replace("/batch", "/orders/0", StringComparison.Ordinal),
            source.Replace("unit: { record: Order, unitPath: /orders }", "unit: { record: Order, kind: Object, unitPath: /orders }", StringComparison.Ordinal)
        })
        {
            Write("bad-file.yaml", invalid);
            Func<Task> generate = () => FileGenerationBatch.GenerateAsync(Args());
            await generate.Should().ThrowAsync<CodeGenException>();
            Directory.Exists(Path.Combine(_runtime, "Files")).Should().BeFalse();
        }
    }
}
