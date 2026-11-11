namespace CoreEx.IO.Test.Unit;

public class JsonDocumentFileProcessorTests
{
    [Test]
    public async Task StreamsNestedDocumentFromIrregularNonSeekableUtf8Input()
    {
        const string text = "{\"orders\":[{\"Id\":\"café 🧀\"},{\"Id\":\"second\"}]}";
        var source = new ChunkedStream(Encoding.UTF8.GetBytes(text), 1);
        var processor = new JsonFileProcessor<Order>(() => Definition("/orders"));
        var context = Context();

        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(source), context);

        summary.IsSuccess.Should().BeTrue();
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.BytesRead.Should().Be(Encoding.UTF8.GetByteCount(text));
        summary.Progress.RecordsRead.Should().Be(2);
        summary.Progress.UnitsRead.Should().Be(2);
        summary.Progress.UnitsValidated.Should().Be(2);
        source.Disposed.Should().BeTrue();
    }

    [Test]
    public async Task PreservesLargeTokensAndSplitSeparatorsForTheSerializer()
    {
        var id = new string('x', 20_000);
        var whitespace = new string(' ', 9_000) + "\n";
        var text = "[" + whitespace + "{\"Id\"" + whitespace + ":" + whitespace + JsonSerializer.Serialize(id) + whitespace + ",\"Quantity\":1}" + whitespace + "]";
        var source = new ChunkedStream(Encoding.UTF8.GetBytes(text), 257);
        var processor = new Probe(() => new("Order", Mapping().MapValue<int>("Quantity", x => x.Quantity, JsonFieldConverters.Integer)));

        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(source), Context());

        summary.IsSuccess.Should().BeTrue();
        summary.Progress.BytesRead.Should().Be(Encoding.UTF8.GetByteCount(text));
        processor.Unit?.Id.Should().Be(id);
        processor.Unit?.Quantity.Should().Be(1);
    }

    [Test]
    public async Task ReportingJsonExceptionsRemainOperationalFailures()
    {
        var failure = new JsonException("Reporting failed.");
        var processor = new Probe(() => Definition("/orders")) { DiagnosticFailure = failure };

        Func<Task> run = () => processor.ProcessAsync(Open("{\"orders\":[{\"Id\":\"first\",\"Id\":\"second\"}]}"), Context(FileErrorMode.CollectErrors));

        (await run.Should().ThrowAsync<JsonException>()).Which.Should().BeSameAs(failure);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ReusedTypesRequireConsistentSerializerContracts(bool conflicting)
    {
        var mapping = new JsonUnitMapping<DatePair>()
            .MapObject("first", x => x.First, new JsonUnitMapping<DateValue>().MapValue<DateOnly>("day", x => x.Day, JsonFieldConverters.DateOnly("yyyyMMdd")))
            .MapObject("second", x => x.Second, new JsonUnitMapping<DateValue>().MapValue<DateOnly>("day", x => x.Day, JsonFieldConverters.DateOnly(conflicting ? "yyyy-MM-dd" : "yyyyMMdd")));
        var processor = new JsonFileProcessor<DatePair>(() => new("Pair", mapping));
        const string text = "[{\"first\":{\"day\":\"20261110\"},\"second\":{\"day\":\"20261110\"}}]";

        if (conflicting)
        {
            Func<Task> run = () => processor.ProcessAsync(Open(text), Context());
            await run.Should().ThrowAsync<InvalidOperationException>().WithMessage("*consistent serializer contract*");
        }
        else
            (await processor.ProcessAsync(Open(text), Context())).IsSuccess.Should().BeTrue();
    }

    [TestCase("{\"orders\":[]}", "file.structure.cardinality", true)]
    [TestCase("{\"orders\":null}", "json.unit.selection", true)]
    [TestCase("{\"other\":[]}", "json.unit.selection", true)]
    [TestCase("{\"orders\":[{\"Id\":\"one\"}]} trailing", "json.syntax.invalid", false)]
    public async Task DistinguishesEmptyMissingWrongKindAndMalformedDocuments(string text, string code, bool validationCompleted)
    {
        var processor = new Probe(() => new("Order", Mapping(), unitPath: "/orders", cardinality: FileCardinality.OneOrMore));
        var summary = await processor.ProcessAsync(Open(text), Context(FileErrorMode.CollectErrors));

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().Be(validationCompleted);
        summary.Progress.ErrorCount.Should().Be(1);
        processor.Diagnostics.Select(x => x.Code).Should().Equal(code);
        if (code == "file.structure.cardinality")
        {
            summary.Progress.RecordsRead.Should().Be(0);
            summary.Progress.InvalidRecords.Should().Be(0);
            summary.Progress.InvalidUnits.Should().Be(0);
        }
    }

    [Test]
    public async Task RejectsDuplicatesInSelectedAndSkippedObjects()
    {
        var selected = new Probe(() => Definition("/orders"));
        var selectedSummary = await selected.ProcessAsync(Open("{\"orders\":[{\"Id\":\"first\",\"Id\":\"second\"}]}"), Context(FileErrorMode.CollectErrors));
        selectedSummary.Progress.InvalidRecords.Should().Be(1);
        selectedSummary.Progress.InvalidUnits.Should().Be(1);
        selectedSummary.Progress.UnitsValidated.Should().Be(0);
        selected.Diagnostics.Select(x => x.Code).Should().Equal("json.syntax.invalid");
        selected.Diagnostics[0].UnitOrdinal.Should().Be(1);
        selected.Diagnostics[0].Location?.SourcePath.Should().Be("/orders/0/Id");

        var skipped = new Probe(() => Definition("/orders"));
        var skippedSummary = await skipped.ProcessAsync(Open("{\"ignored\":{\"x\":1,\"x\":2},\"orders\":[]}"), Context(FileErrorMode.CollectErrors));
        skippedSummary.Progress.InvalidRecords.Should().Be(0);
        skippedSummary.Progress.InvalidUnits.Should().Be(0);
        skipped.Diagnostics.Select(x => x.Code).Should().Equal("json.syntax.invalid");
        skipped.Diagnostics[0].Location?.SourcePath.Should().Be("/ignored/x");
    }

    [Test]
    public async Task RejectsNonObjectArrayItemsWithoutInventingExtraDiagnostics()
    {
        var processor = new Probe(() => Definition("/orders"));
        var summary = await processor.ProcessAsync(Open("{\"orders\":[null,{\"Id\":\"valid\"}]}"), Context(FileErrorMode.CollectErrors));

        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.RecordsRead.Should().Be(2);
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.UnitsRead.Should().Be(2);
        summary.Progress.InvalidUnits.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(1);
        processor.Diagnostics.Select(x => x.Code).Should().Equal("json.unit.type");
        processor.Diagnostics[0].UnitOrdinal.Should().Be(1);
        processor.Diagnostics[0].Location?.SourcePath.Should().Be("/orders/0");
    }

    [Test]
    public async Task MakesLateMetadataAvailableFromTheBeginningOfAcceptedReplayOnly()
    {
        await using var services = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var processor = new MetadataProbe(services, new());
        var context = new JsonFileContext(new("orders", "1", "orders.json", "attempt"), FileProcessingMode.ValidateThenExecute);
        const string text = "{\"orders\":[{\"Id\":\"one\"}],\"batch\":{\"Name\":\"batch-1\"}}";

        var summary = await processor.ProcessAsync(Open(text), context);

        summary.IsSuccess.Should().BeTrue();
        summary.Progress.UnitsValidated.Should().Be(1);
        summary.Progress.UnitsExecuted.Should().Be(1);
        processor.ValidationMetadata.Should().Equal((false, false));
        processor.ExecutionMetadata.Should().Equal((true, false));
        context.GetAcceptedMetadata<Batch>("Batch")?.Name.Should().Be("batch-1");
    }

    [Test]
    public async Task DuplicateMetadataIsNotPublishedOrCountedAsAnInvalidBusinessUnit()
    {
        await using var services = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var processor = new MetadataProbe(services, new());
        var context = new JsonFileContext(new("orders", "1", "orders.json", "attempt"), FileProcessingMode.ValidateOnly, FileErrorMode.CollectErrors);
        const string text = "{\"batch\":{\"Name\":\"first\",\"Name\":\"second\"},\"orders\":[{\"Id\":\"one\"}]}";

        var summary = await processor.ProcessAsync(Open(text), context);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.ErrorCount.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(1);
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.InvalidUnits.Should().Be(0);
        processor.ValidationMetadata.Should().Equal((false, false));
        context.GetMetadata<Batch>("Batch").Should().BeNull();
        context.AcceptedMetadata.Should().BeNull();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DecodesOnlyConsumedStringValues(bool ignored)
    {
        var prefix = Encoding.UTF8.GetBytes(ignored ? "[{\"ignored\":\"" : "[{\"Id\":\"");
        var bytes = prefix.Concat(new byte[] { 0xc3, 0x28 }).Concat(Encoding.UTF8.GetBytes("\"}]")).ToArray();
        var processor = new Probe(() => new("Order", Mapping()), new() { UnknownMemberPolicy = JsonUnknownMemberPolicy.Ignore });

        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(new ChunkedStream(bytes, 1)), Context(FileErrorMode.CollectErrors));

        summary.IsSuccess.Should().Be(ignored);
        summary.Progress.UnitsValidated.Should().Be(ignored ? 1 : 0);
        processor.Diagnostics.Select(x => x.Code).Should().Equal(ignored ? [] : new[] { "json.field.type" });
    }

    [Test]
    public async Task PreservesFullIntegerAndDecimalPrecision()
    {
        var processor = new Probe(() => new("Order", new JsonUnitMapping<Order>()
            .MapValue<long>("LargeValue", x => x.LargeValue, JsonFieldConverters.Int64)
            .MapValue<decimal>("Amount", x => x.Amount, JsonFieldConverters.Decimal)));

        var summary = await processor.ProcessAsync(Open("[{\"LargeValue\":9223372036854775807,\"Amount\":0.1234567890123456789012345678}]"), Context());

        summary.IsSuccess.Should().BeTrue();
        processor.Unit.Should().NotBeNull();
        processor.Unit!.LargeValue.Should().Be(long.MaxValue);
        processor.Unit.Amount.Should().Be(0.1234567890123456789012345678m);
        processor.Unit.WasDeserialized.Should().BeTrue("the standard serializer must own object construction and callbacks");
    }

    [TestCase("a.b")]
    [TestCase("a~/b")]
    [TestCase("a[0]")]
    public async Task SerializerErrorPathsResolveDeclaredEscapedSourceNames(string name)
    {
        var processor = new Probe(() => new("Order", new JsonUnitMapping<Order>()
            .MapValue<int>(name, x => x.Quantity, JsonFieldConverters.Integer)));
        var text = "[{" + JsonSerializer.Serialize(name) + ":\"bad\"}]";

        var summary = await processor.ProcessAsync(Open(text), Context(FileErrorMode.CollectErrors));

        summary.Progress.ErrorCount.Should().Be(1);
        processor.Diagnostics.Single().Code.Should().Be("json.field.type");
        processor.Diagnostics.Single().Location?.PropertyPath.Should().Be("Quantity");
        processor.Diagnostics.Single().Location?.SourcePath.Should().Be("/0/" + name.Replace("~", "~0").Replace("/", "~1"));
    }

    [TestCase("1.0")]
    [TestCase("\"1\"")]
    [TestCase("2147483648")]
    public async Task RejectsNonIntegralQuotedAndOverflowingIntegerValues(string value)
    {
        var processor = new Probe(() => new("Order", new JsonUnitMapping<Order>()
            .MapValue<int>("Quantity", x => x.Quantity, JsonFieldConverters.Integer)));

        var summary = await processor.ProcessAsync(Open("[{\"Quantity\":" + value + "}]"), Context(FileErrorMode.CollectErrors));

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.UnitsValidated.Should().Be(0);
        processor.Diagnostics.Select(x => x.Code).Should().Equal("json.field.type");
    }

    [Test]
    public async Task StreamsNestedValuesAndDateConversions()
    {
        var text = JsonLinesFileProcessorTests.Fixture("valid.jsonl");
        var first = text.Split('\n')[0];
        var processor = new ModelProbe();
        var summary = await processor.ProcessAsync(Open("[" + first + "]"), Context());

        summary.IsSuccess.Should().BeTrue();
        processor.Unit.Should().NotBeNull();
        processor.Unit!.Lines.Should().HaveCount(2);
        processor.Unit.Tags.Should().Equal("priority", "web");
        processor.Unit.EffectiveDate.Should().Be(new DateOnly(2026, 11, 9));
        processor.Unit.SubmittedAt?.Offset.Should().Be(TimeSpan.FromHours(10));
        processor.Unit.BillingAddress?.Code.Should().NotBeNullOrEmpty();
    }

    [TestCase("[null]", "json.field.type", "Values[0]", "/0/Values/0")]
    [TestCase("[1,2,3]", "file.validation", "Values", "/0/Values")]
    [TestCase("[]", "file.validation", "Values", "/0/Values")]
    [TestCase("null", "file.validation", "Values", "/0/Values")]
    public async Task SerializerConvertsValuesAndValidatorChecksCollectionBounds(string values, string code, string property, string source)
    {
        var processor = new Probe(() => new("Order", new JsonUnitMapping<Order>()
            .MapValues<int>("Values", x => x.Values, JsonFieldConverters.Integer)), validator: new OrderValidator());
        var summary = await processor.ProcessAsync(Open("[{\"Values\":" + values + "}]"), Context(FileErrorMode.CollectErrors));

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.InvalidUnits.Should().Be(1);
        processor.Diagnostics.Select(x => x.Code).Should().Equal(code);
        processor.Diagnostics.Single().Location?.PropertyPath.Should().Be(property);
        processor.Diagnostics.Single().Location?.SourcePath.Should().Be(source);
    }

    [Test]
    public async Task ReportsFirstSerializerErrorAndContinuesToTheNextUnit()
    {
        var processor = new Probe(() => new("Order", new JsonUnitMapping<Order>().MapString("Id", x => x.Id)
            .MapValue<int>("Quantity", x => x.Quantity, JsonFieldConverters.Integer)));
        var summary = await processor.ProcessAsync(Open("[{\"Quantity\":\"bad\",\"Id\":false},{\"Id\":\"valid\",\"Quantity\":1}]"), Context(FileErrorMode.CollectErrors));

        summary.Progress.ErrorCount.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(1);
        processor.Unit?.Id.Should().Be("valid");
        processor.Diagnostics.Select(x => x.Location?.PropertyPath).Should().Equal("Quantity");

        processor.Diagnostics.Clear();
        summary = await processor.ProcessAsync(Open("[{\"Quantity\":\"bad\",\"Id\":false"), Context(FileErrorMode.CollectErrors));
        summary.ValidationCompleted.Should().BeFalse();
        processor.Diagnostics.Select(x => x.Code).Should().Equal("json.syntax.invalid");
        processor.Diagnostics.Single().UnitOrdinal.Should().Be(1);
    }

    [Test]
    public async Task PreservesNestedCollectionErrorPaths()
    {
        var child = new JsonUnitMapping<ChildOrder>().MapValue<int>("count", x => x.Quantity, JsonFieldConverters.Integer);
        var processor = new Probe(() => new("Order", new JsonUnitMapping<Order>().MapObjects("children", x => x.Children, child)));
        var summary = await processor.ProcessAsync(Open("[{\"children\":[{\"count\":\"bad\"},null]}]"), Context(FileErrorMode.CollectErrors));

        summary.Progress.ErrorCount.Should().Be(1);
        processor.Diagnostics.Select(x => x.Location?.PropertyPath).Should().Equal("Children[0].Quantity");
        processor.Diagnostics.Select(x => x.Location?.SourcePath).Should().Equal("/0/children/0/count");
    }

    [TestCase("{}")]
    [TestCase("{\"Id\":null,\"Quantity\":null,\"Values\":null}")]
    public async Task AbsentAndNullMembersClearWireInitializers(string text)
    {
        var processor = new Probe(() => new("Order", Mapping().MapValue<int>("Quantity", x => x.Quantity, JsonFieldConverters.Integer)
            .MapValues<int>("Values", x => x.Values, JsonFieldConverters.Integer)));
        var summary = await processor.ProcessAsync(Open("[" + text + "]"), Context());

        summary.IsSuccess.Should().BeTrue();
        processor.Unit.Should().NotBeNull();
        processor.Unit!.Id.Should().BeNull();
        processor.Unit.Quantity.Should().BeNull();
        processor.Unit.Values.Should().BeNull();
    }

    [Test]
    [NonParallelizable]
    public async Task LargeRootObjectDoesNotRetainRawObjectBytesOrADom()
    {
        var processor = new JsonFileProcessor<Order>(() => new("Order", Mapping(), kind: JsonDocumentKind.Object),
            new() { UnknownMemberPolicy = JsonUnknownMemberPolicy.Ignore });
        (await processor.ProcessAsync(Open("{\"Id\":\"warmup\"}"), Context())).IsSuccess.Should().BeTrue();
        var source = new GeneratedDocumentStream(65_536);
        source.Baseline = GC.GetTotalMemory(true);

        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(source), Context());

        summary.IsSuccess.Should().BeTrue();
        summary.Progress.UnitsValidated.Should().Be(1);
        summary.Progress.BytesRead.Should().Be(source.SourceBytes).And.BeGreaterThan(4 * 1024 * 1024);
        source.Sampled.Should().BeTrue();
        source.LiveBytesAtEnd.Should().BeLessThan(1024 * 1024, "a multi-megabyte selected object must not retain its raw bytes or a DOM");
        await TestContext.Out.WriteLineAsync($"Streamed root object: sourceBytes={source.SourceBytes}; additionalLiveManagedBytesNearEnd={source.LiveBytesAtEnd}; limit=1048576. This is managed live-heap evidence, not process RSS.");
    }

    private static JsonFileDefinition<Order> Definition(string unitPath)
        => new("Order", Mapping(), unitPath: unitPath);

    private static JsonUnitMapping<Order> Mapping() => new JsonUnitMapping<Order>().MapString("Id", x => x.Id);

    private static Func<CancellationToken, Task<Stream>> Open(string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return _ => Task.FromResult<Stream>(new ChunkedStream(bytes, 3));
    }

    private static FileProcessingContext Context(FileErrorMode errorMode = FileErrorMode.FailFast)
        => new(new("orders", "1", "orders.json", "attempt"), FileProcessingMode.ValidateOnly, errorMode);

    private sealed class Probe(Func<JsonFileDefinition<Order>> definitionFactory, JsonFileOptions? options = null, IValidator<Order>? validator = null)
        : JsonFileProcessor<Order>(definitionFactory, options, validator)
    {
        public List<FileDiagnostic> Diagnostics { get; } = [];

        public Order? Unit { get; private set; }

        public Exception? DiagnosticFailure { get; init; }

        protected override Task OnValidatedUnitAsync(FileUnitArgs<Order> args, CancellationToken cancellationToken)
        {
            Unit = args.Unit;
            return Task.CompletedTask;
        }

        protected override Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Diagnostics.Add(args.Diagnostic);
            return DiagnosticFailure is { } failure ? Task.FromException(failure) : Task.CompletedTask;
        }
    }

    private sealed class DatePair
    {
        public DateValue? First { get; set; }

        public DateValue? Second { get; set; }
    }

    private sealed class DateValue
    {
        public DateOnly? Day { get; set; }
    }

    private sealed class Order : System.Text.Json.Serialization.IJsonOnDeserialized
    {
        public string? Id { get; set; } = "initial";

        public int? Quantity { get; set; } = 42;

        public long? LargeValue { get; set; }

        public decimal? Amount { get; set; }

        public List<int>? Values { get; set; } = [42];

        public List<ChildOrder>? Children { get; set; }

        public bool WasDeserialized { get; private set; }

        void System.Text.Json.Serialization.IJsonOnDeserialized.OnDeserialized() => WasDeserialized = true;
    }

    private sealed class ChildOrder
    {
        public int? Quantity { get; set; }
    }

    private sealed class OrderValidator : Validator<Order>
    {
        public OrderValidator() => Property(x => x.Values).NotNull().Collection(minCount: 1, maxCount: 2);
    }

    private sealed class ModelProbe() : JsonFileProcessor<OrdersJsonlOrder>(() => new("Order", OrdersJsonlOrderMapping.Create()), validator: new OrdersJsonlOrderValidator())
    {
        public OrdersJsonlOrder? Unit { get; private set; }

        protected override Task OnValidatedUnitAsync(FileUnitArgs<OrdersJsonlOrder> args, CancellationToken cancellationToken)
        {
            Unit = args.Unit;
            return Task.CompletedTask;
        }
    }

    private sealed class Batch
    {
        public string? Name { get; set; }
    }

    private sealed class MetadataProbe(IServiceProvider services, MetadataObservations observations)
        : JsonFileProcessor<Order>(CreateDefinition, serviceProvider: services, handlerFactory: _ => new MetadataHandler(observations))
    {
        public List<(bool Accepted, bool Local)> ValidationMetadata => observations.Validation;
        public List<(bool Accepted, bool Local)> ExecutionMetadata => observations.Execution;

        protected override Task OnValidatedUnitAsync(FileUnitArgs<Order> args, CancellationToken cancellationToken)
        {
            var context = args.Context;
            ValidationMetadata.Add((context.AcceptedMetadata is not null, context.GetMetadata<Batch>("Batch") is not null));
            return Task.CompletedTask;
        }

        private static JsonFileDefinition<Order> CreateDefinition() => new("Order", Mapping(), unitPath: "/orders",
            metadata: [new JsonFileMetadata<Batch>("Batch", "Batch", "/batch", new JsonUnitMapping<Batch>().MapString("Name", x => x.Name), required: true)]);

        private sealed class MetadataHandler : FileUnitHandler<Order>
        {
            private readonly MetadataObservations _observations;

            public MetadataHandler(MetadataObservations observations) => _observations = observations;

            protected override Task<Result> OnExecuteAsync(FileUnitArgs<Order> args, CancellationToken cancellationToken)
            {
                var context = args.Context;
                _observations.Execution.Add((context.AcceptedMetadata is not null, context.GetMetadata<Batch>("Batch") is not null));
                return Task.FromResult(Result.Success);
            }
        }
    }

    private sealed class MetadataObservations
    {
        public List<(bool Accepted, bool Local)> Validation { get; } = [];
        public List<(bool Accepted, bool Local)> Execution { get; } = [];
    }

    private sealed class ChunkedStream(byte[] bytes, int maxChunk) : Stream
    {
        private int _position;

        public bool Disposed { get; private set; }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = Math.Min(Math.Min(buffer.Length, maxChunk), bytes.Length - _position);
            bytes.AsMemory(_position, count).CopyTo(buffer);
            _position += count;
            return ValueTask.FromResult(count);
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class GeneratedDocumentStream(int repetitions) : Stream
    {
        private readonly byte[] _prefix = "{\"ignored\":["u8.ToArray();
        private readonly byte[] _item = Encoding.UTF8.GetBytes("\"" + new string('x', 64) + "\",");
        private readonly byte[] _suffix = "0],\"Id\":\"accepted\"}"u8.ToArray();
        private long _position;

        public long Baseline { get; set; }
        public bool Sampled { get; private set; }
        public long LiveBytesAtEnd { get; private set; }
        public long SourceBytes => _prefix.Length + (long)_item.Length * repetitions + _suffix.Length;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = (int)Math.Min(buffer.Length, SourceBytes - _position);
            var copied = 0;
            var bodyEnd = _prefix.Length + (long)_item.Length * repetitions;
            while (copied < count)
            {
                var part = _position < _prefix.Length ? _prefix : _position < bodyEnd ? _item : _suffix;
                var offset = (int)(_position < _prefix.Length ? _position : _position < bodyEnd ? (_position - _prefix.Length) % _item.Length : _position - bodyEnd);
                var length = Math.Min(part.Length - offset, count - copied);
                part.AsMemory(offset, length).CopyTo(buffer[copied..]);
                _position += length;
                copied += length;
            }

            if (!Sampled && _position >= bodyEnd)
            {
                Sampled = true;
                LiveBytesAtEnd = GC.GetTotalMemory(true) - Baseline;
            }

            return ValueTask.FromResult(count);
        }

        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
