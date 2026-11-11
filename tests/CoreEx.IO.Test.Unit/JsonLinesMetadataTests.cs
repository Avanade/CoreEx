namespace CoreEx.IO.Test.Unit;

public partial class JsonLinesFileProcessorTests
{
    [TestCase(FileProcessingMode.ValidateOnly, 0)]
    [TestCase(FileProcessingMode.ValidateThenExecute, 2)]
    [TestCase(FileProcessingMode.ValidateAndExecute, 2)]
    public async Task RoutedMetadataAvailabilityAndFreshAcceptedCopies(FileProcessingMode mode, int executed)
    {
        await using var services = Provider();
        var calls = new List<FileUnitArgs<RoutedJsonlOrder>>();
        var processor = new RoutedProbe(services, calls);
        var context = new RoutedState(Context(mode).Identity, mode);
        var summary = await processor.ProcessAsync(_ => Open(Fixture("routed.jsonl")), context);
        summary.IsSuccess.Should().BeTrue();
        AssertCounts(summary, 4, 0, 2, 0, 2, executed);
        context.GetRecordCount("Header").Should().Be(1);
        context.GetRecordCount("Order").Should().Be(2);
        context.GetRecordCount("Trailer").Should().Be(1);
        context.EndOfFileLineNumber.Should().Be(4);
        processor.UnitObservations.Should().Equal((true, false, false), (true, false, false));
        processor.ExecutionObservations.Should().Equal(Enumerable.Repeat((true, false, mode == FileProcessingMode.ValidateThenExecute), executed));
        context.GetAcceptedHeaderMetadata()?.BatchId.Should().Be("B123");
        context.GetAcceptedTrailerMetadata()?.QuantityTotal.Should().Be(5);
        var copy = context.GetAcceptedHeaderMetadata().ThrowIfNull();
        copy.BatchId = "changed";
        context.GetAcceptedHeaderMetadata()?.BatchId.Should().Be("B123");
        context.GetHeaderMetadata().ThrowIfNull().BatchId = "also changed";
        context.GetAcceptedHeaderMetadata()?.BatchId.Should().Be("B123");
        processor.Diagnostics.Should().BeEmpty();
    }

    [TestCase("{\"type\":\"unknown\"}", "jsonl.discriminator.unknown")]
    [TestCase("{\"type\":7}", "jsonl.discriminator.invalid")]
    [TestCase("{\"type\":[\"order\"]}", "jsonl.discriminator.invalid")]
    [TestCase("{\"type\":{\"other\":\"order\"}}", "jsonl.discriminator.invalid")]
    [TestCase("{}", "jsonl.discriminator.invalid")]
    [TestCase("{\"type\":\"order\",\"type\":\"header\"}", "jsonl.record.invalid")]
    [TestCase("bad json", "jsonl.record.invalid")]
    public async Task UnroutableRecordsDoNotInventUnits(string text, string code)
    {
        var processor = new RoutedProbe();
        var summary = await processor.ProcessAsync(_ => Open(text + "\n" + Fixture("routed.jsonl")), new RoutedState(Context().Identity, FileProcessingMode.ValidateOnly));
        summary.Progress.RecordsRead.Should().Be(5);
        summary.Progress.InvalidRecords.Should().Be(2);
        summary.Progress.UnitsRead.Should().Be(2);
        summary.Progress.InvalidUnits.Should().Be(0);
        summary.Progress.UnitsValidated.Should().Be(2);
        processor.Diagnostics.Should().Contain(x => x.Code == code && x.UnitOrdinal == null);
        processor.Diagnostics.Should().Contain(x => x.Code == "jsonl.metadata.first-position");
        summary.ValidationCompleted.Should().BeTrue();
    }

    [Test]
    public async Task TrailingMalformedLineParticipatesInLastPosition()
    {
        var processor = new RoutedProbe();
        var summary = await processor.ProcessAsync(_ => Open(Fixture("routed.jsonl") + "bad\n"), new RoutedState(Context().Identity, FileProcessingMode.ValidateOnly));
        AssertCounts(summary, 5, 1, 2, 0, 2, 0);
        processor.Diagnostics.Select(x => x.Code).Should().Equal("jsonl.metadata.after-last", "jsonl.record.invalid");
        summary.ValidationCompleted.Should().BeTrue();
    }

    [Test]
    public async Task DuplicateMetadataIsNeverPublishedAndRequiredMissingDoesNotCascade()
    {
        var text = Fixture("routed.jsonl");
        var header = text.Split('\n')[0] + "\n";
        var processor = new RoutedProbe();
        var summary = await processor.ProcessAsync(_ => Open(header + text), new RoutedState(Context().Identity, FileProcessingMode.ValidateOnly));
        summary.Progress.InvalidRecords.Should().Be(1);
        processor.Diagnostics.Select(x => x.Code).Should().Equal("jsonl.metadata.duplicate", "jsonl.metadata.first-position");
        processor = new RoutedProbe();
        summary = await processor.ProcessAsync(_ => Open(text[..text.LastIndexOf("{\"type\":\"trailer\"", StringComparison.Ordinal)]), new RoutedState(Context().Identity, FileProcessingMode.ValidateOnly));
        processor.Diagnostics.Select(x => x.Code).Should().Equal("file.metadata.required");
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.InvalidUnits.Should().Be(0);
    }

    [TestCase(FileProcessingMode.ValidateThenExecute, 0)]
    [TestCase(FileProcessingMode.ValidateAndExecute, 2)]
    public async Task LateReconciliationFailurePreservesModeSpecificEffects(FileProcessingMode mode, int executed)
    {
        await using var services = Provider();
        var calls = new List<FileUnitArgs<RoutedJsonlOrder>>();
        var processor = new RoutedProbe(services, calls);
        var context = new RoutedState(Context(mode).Identity, mode);
        var summary = await processor.ProcessAsync(_ => Open(Fixture("routed.jsonl").Replace("\"orderCount\":2", "\"orderCount\":3", StringComparison.Ordinal)), context);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        AssertCounts(summary, 4, 0, 2, 0, 2, executed);
        summary.ValidationCompleted.Should().BeTrue();
        processor.Diagnostics.Select(x => x.Code).Should().Equal("file.reconciliation.count");
        context.AcceptedMetadata.Should().BeNull();
    }

    [Test]
    public async Task FreezePreservesDeclaredMetadataWithoutImposingContentQuotas()
    {
        var processor = new RoutedProbe(expandMetadata: true);
        var context = new RoutedState(Context().Identity, FileProcessingMode.ValidateOnly);
        var summary = await processor.ProcessAsync(_ => Open(Fixture("routed.jsonl")), context);
        summary.IsSuccess.Should().BeTrue();
        context.GetAcceptedHeaderMetadata()?.BatchId.Should().HaveLength(65537);
        processor.Diagnostics.Should().BeEmpty();
    }

    [Test]
    public async Task MetadataReplaySkipsValidationAndSnapshotUsesSerializerRules()
    {
        await using var services = Provider();
        var validator = new MetadataCounter();
        var processor = new JsonLinesFileProcessor<OrdersJsonlOrder>(() => new("Order", OrdersJsonlOrderMapping.Create(), match: "order", metadata:
        [
            new JsonLinesMetadata<ExtraMetadata>("Header", "Header", "header", new JsonUnitMapping<ExtraMetadata>().MapString("Kept", x => x.Kept), validator)
        ]), options: new() { Discriminator = "type" }, serviceProvider: services, handlerFactory: _ => new Handler(new Stamp()));
        var context = new JsonLinesContext(Context().Identity, FileProcessingMode.ValidateThenExecute);
        var summary = await processor.ProcessAsync(_ => Open("{\"type\":\"header\",\"Kept\":\"ok\"}\n"), context);
        summary.IsSuccess.Should().BeTrue();
        validator.Calls.Should().Be(1);
        context.GetMetadata<ExtraMetadata>("Header")?.Transient.Should().HaveLength(100000);
        context.GetAcceptedMetadata<ExtraMetadata>("Header")?.Kept.Should().Be("ok");
        context.GetAcceptedMetadata<ExtraMetadata>("Header")?.Additional.Should().Be("included");
        context.GetAcceptedMetadata<ExtraMetadata>("Header")?.Transient.Should().BeNull();
    }

    private sealed class ExtraMetadata
    {
        public string? Kept { get; set; }

        public string? Additional { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public string? Transient { get; set; }
    }

    private sealed class MetadataCounter : Validator<ExtraMetadata>
    {
        public int Calls { get; private set; }
        protected override Task OnValidateAsync(ValidationContext<ExtraMetadata> context, CancellationToken cancellationToken)
        {
            Calls++;
            context.Value.ThrowIfNull().Additional = "included";
            context.Value.ThrowIfNull().Transient = new string('x', 100000);
            return Task.CompletedTask;
        }
    }

    private sealed class RoutedState(FileProcessingIdentity identity, FileProcessingMode mode) : RoutedJsonlContext(identity, mode, FileErrorMode.CollectErrors)
    {
        public int OrderCount { get; set; }
        public int Quantity { get; set; }
    }

    private sealed class RoutedProbe : JsonLinesFileProcessor<RoutedJsonlOrder>
    {
        private readonly bool _expandMetadata;
        public RoutedProbe(IServiceProvider? services = null, List<FileUnitArgs<RoutedJsonlOrder>>? calls = null, bool expandMetadata = false)
            : base(CreateDefinition, new() { Discriminator = "type" }, new RoutedJsonlOrderValidator(), services,
                services is null ? null : _ => new RoutedHandler(calls ?? throw new InvalidOperationException()))
            => _expandMetadata = expandMetadata;

        public List<FileDiagnostic> Diagnostics { get; } = [];
        public List<(bool Header, bool Trailer, bool Accepted)> UnitObservations { get; } = [];
        public List<(bool Header, bool Trailer, bool Accepted)> ExecutionObservations { get; } = [];
        protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
            => new RoutedState(validationContext.Identity, validationContext.Mode);

        protected override void OnConfigureUnitContext(FileUnitContextArgs<RoutedJsonlOrder> args)
        {
            if (args.Context is RoutedState state && state.Progress.UnitsAttempted > state.Progress.UnitsExecuted)
                ExecutionObservations.Add((state.GetHeaderMetadata() is not null, state.GetTrailerMetadata() is not null, state.AcceptedMetadata is not null));
        }

        protected override Task OnValidatedUnitAsync(FileUnitArgs<RoutedJsonlOrder> args, CancellationToken cancellationToken)
        {
            var context = (RoutedState)args.Context;
            context.OrderCount++;
            context.Quantity += args.Unit.Quantity ?? 0;
            UnitObservations.Add((context.GetHeaderMetadata() is not null, context.GetTrailerMetadata() is not null, context.AcceptedMetadata is not null));
            if (_expandMetadata)
                context.GetHeaderMetadata().ThrowIfNull().BatchId = new string('x', 65537);

            return Task.CompletedTask;
        }

        protected override async Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
        {
            var context = (RoutedState)args.Context;
            if (context.HasValidationErrors)
                return;

            var trailer = context.GetTrailerMetadata();
            if (trailer?.OrderCount != context.OrderCount)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Structure, "file.reconciliation.count", "The order count does not match.", location: new(sourcePath: "/orderCount")), cancellationToken);
            else if (trailer.QuantityTotal != context.Quantity)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Structure, "file.reconciliation.quantity", "The quantity total does not match.", location: new(sourcePath: "/quantityTotal")), cancellationToken);
        }

        protected override Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Diagnostics.Add(args.Diagnostic);
            return Task.CompletedTask;
        }

        private static JsonLinesDefinition<RoutedJsonlOrder> CreateDefinition() => new("Order", RoutedJsonlOrderMapping.Create(), match: "order", metadata:
        [
            new JsonLinesMetadata<RoutedJsonlHeader>("Header", "Header", "header", RoutedJsonlHeaderMapping.Create(), new RoutedJsonlHeaderValidator(), true, FileRecordPosition.First),
            new JsonLinesMetadata<RoutedJsonlTrailer>("Trailer", "Trailer", "trailer", RoutedJsonlTrailerMapping.Create(), new RoutedJsonlTrailerValidator(), true, FileRecordPosition.Last)
        ]);
    }

    private sealed class RoutedHandler(List<FileUnitArgs<RoutedJsonlOrder>> calls) : FileUnitHandler<RoutedJsonlOrder>
    {
        protected override Task<Result> OnExecuteAsync(FileUnitArgs<RoutedJsonlOrder> args, CancellationToken cancellationToken)
        {
            calls.Add(args);
            return Task.FromResult(Result.Success);
        }
    }
}
