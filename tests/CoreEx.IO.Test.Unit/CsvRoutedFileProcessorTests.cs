namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class CsvRoutedFileProcessorTests
{
    [TestCase(FileProcessingMode.ValidateOnly, 1, 0)]
    [TestCase(FileProcessingMode.ValidateThenExecute, 2, 5)]
    public async Task HeterogeneousUnorderedRecords_AreIndependentScopedUnits(FileProcessingMode mode, int opens, int executions)
    {
        await using var environment = new Environment();
        using var ambient = new ExecutionContext { TenantId = "file", ServiceProvider = environment.Provider };
        ExecutionContext.SetCurrent(ambient);
        try
        {
            var input = new Input("#comment\nH,batch\nB,2\nA,one\nB,3\nT,5\n");
            var processor = new Processor(environment);
            var context = Context(mode);
            var summary = await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);

            summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
            summary.Progress.RecordsRead.Should().Be(5);
            summary.Progress.UnitsValidated.Should().Be(5);
            summary.Progress.UnitsExecuted.Should().Be(executions);
            summary.Progress.BytesRead.Should().Be(Encoding.UTF8.GetByteCount(input.Text));
            context.GetRecordCount("alpha").Should().Be(1);
            context.GetRecordCount("beta").Should().Be(2);
            environment.Validated.Select(x => x.Unit.GetType()).Should().Equal(typeof(Alpha), typeof(Beta), typeof(Alpha), typeof(Beta), typeof(Beta));
            environment.Handlers.Should().HaveCount(5 + executions).And.OnlyContain(x => x.Disposed);
            environment.Handlers.Select(x => x.Ambient).Should().OnlyHaveUniqueItems();
            input.Streams.Should().HaveCount(opens).And.OnlyContain(x => x.Disposed);
            processor.Events.Should().Equal("eof", "completed");
            processor.Ambient.Should().OnlyContain(x => ReferenceEquals(x, ambient));
            if (mode == FileProcessingMode.ValidateThenExecute)
            {
                environment.Executed[0].Unit.Should().NotBeSameAs(environment.Validated[0].Unit);
                environment.Executed[0].Context.Should().NotBeSameAs(context);
                environment.Executed[0].Context.Identity.Should().BeSameAs(context.Identity);
                environment.Executed[0].Context.Total.Should().Be(0);
                environment.Executed.Select(x => x.Context).Should().OnlyContain(x => !ReferenceEquals(x, context));
                environment.Executed.Select(x => x.Recognized).Should().Equal(1, 1, 1, 2, 1);
            }

            ExecutionContext.Current.Should().BeSameAs(ambient);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [TestCase("H,batch\nB,2\nA,one\nT,bad\n", 1)]
    [TestCase("H,batch\nB,2\nA,one\n", 0)]
    [TestCase("H,batch\nB,2\nA,one\nT,7\n", 0)]
    public async Task MappingMissingAndReconciliationFailures_PreventEveryExecution(string text, int invalid)
    {
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var input = new Input(text);
        var summary = await processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateThenExecute)).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.InvalidRecords.Should().Be(invalid);
        input.Streams.Should().ContainSingle();
        environment.Executed.Should().BeEmpty();
        processor.Events.Should().Equal("eof", "rejected");
    }

    [TestCase(FileCardinality.Many, "", true)]
    [TestCase(FileCardinality.Many, "A,one\nA,two\n", true)]
    [TestCase(FileCardinality.One, "", false)]
    [TestCase(FileCardinality.One, "A,one\n", true)]
    [TestCase(FileCardinality.One, "A,one\nA,two\n", false)]
    [TestCase(FileCardinality.Optional, "", true)]
    [TestCase(FileCardinality.Optional, "A,one\nA,two\n", false)]
    [TestCase(FileCardinality.OneOrMore, "", false)]
    [TestCase(FileCardinality.OneOrMore, "A,one\nA,two\n", true)]
    public async Task Cardinality_IsPerFile(FileCardinality cardinality, string text, bool success)
    {
        var processor = new Processor(definition: () => new(0, [AlphaRoute(cardinality: cardinality)]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input(text).OpenAsync, Context()).ConfigureAwait(false);
        summary.IsSuccess.Should().Be(success);
        summary.ValidationCompleted.Should().BeTrue();
    }

    [TestCase("A,\n", "csv.validation", "alpha")]
    [TestCase("B,bad\n", "csv.field.invalid", "beta")]
    public async Task InvalidRecognizedRecords_CountForRequiredOccurrence(string text, string code, string key)
    {
        var processor = new Processor(definition: () => new(0,
            [key == "alpha" ? AlphaRoute(cardinality: FileCardinality.One) : BetaRoute(cardinality: FileCardinality.One)]), reconcile: false);
        var context = Context();
        var summary = await processor.ProcessAsync(new Input(text).OpenAsync, context).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(0);
        context.GetRecordCount(key).Should().Be(1);
        context.HasValidationErrors.Should().BeTrue();
        processor.Diagnostics.Should().ContainSingle(x => x.Code == code && x.RecordType == key);
        processor.Diagnostics.Should().NotContain(x => x.Code == "csv.structure.required");
    }

    [TestCase("A,one\nH,late\n", "csv.structure.before-first", "header", 1)]
    [TestCase("H,one\nH,two\n", "csv.structure.duplicate", "header", 2)]
    [TestCase("H,one\nH,two\n", "csv.structure.first-position", "header", 2)]
    [TestCase("T,1\nA,after\n", "csv.structure.after-last", "trailer", 2)]
    [TestCase("T,1\nX,unknown\n", "csv.structure.after-last", "trailer", 2)]
    [TestCase("H,h\nA,a\nA,b\nT,1\n", "csv.structure.duplicate", "alpha", 3)]
    public async Task PositionRules_HaveOwnerAndPhysicalLocations(string text, string code, string owner, int line)
    {
        var processor = new Processor(definition: () => Definition(alphaCardinality: FileCardinality.One), reconcile: false);
        var summary = await processor.ProcessAsync(new Input(text).OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        processor.Diagnostics.Should().Contain(x => x.Code == code && x.RecordType == owner && x.Location != null && x.Location.LineNumber == line);
    }

    [Test]
    public async Task OptionalBoundaries_AreNotRequiredButConstrainPositionsWhenPresent()
    {
        CsvRoutedDefinition<Unit> Definition() => new(0,
        [
            new CsvRoutedRecord<Alpha, Unit>("first", "H", AlphaMapping, x => x, cardinality: FileCardinality.Optional, position: FileRecordPosition.First),
            AlphaRoute(),
            new CsvRoutedRecord<Beta, Unit>("last", "T", BetaMapping, x => x, cardinality: FileCardinality.Optional, position: FileRecordPosition.Last)
        ]);
        var accepted = new Processor(definition: Definition, reconcile: false);
        (await accepted.ProcessAsync(new Input("A,one\n").OpenAsync, Context()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        var rejected = new Processor(definition: Definition, reconcile: false);
        await rejected.ProcessAsync(new Input("A,one\nH,late\nT,0\nA,after\n").OpenAsync, Context()).ConfigureAwait(false);
        rejected.Diagnostics.Select(x => x.Code).Should().Contain("csv.structure.first-position").And.Contain("csv.structure.after-last");
    }

    [Test]
    public async Task MetadataRoutes_AreCountedButNeverValidatedOrExecutedAsBusinessUnits()
    {
        await using var environment = new Environment();
        var processor = new Processor(environment, () => new(0,
        [
            new CsvRoutedRecord<Alpha, Unit>("header", "H", AlphaMapping, x => x, new AlphaValidator()),
            AlphaRoute(),
            new CsvRoutedRecord<Beta, Unit>("trailer", "T", BetaMapping, x => x)
        ],
        [
            new("Header", "header", true, FileRecordPosition.First),
            new("Trailer", "trailer", true, FileRecordPosition.Last)
        ]), reconcile: false);
        var context = Context(FileProcessingMode.ValidateThenExecute);
        var input = new Input("H,batch\nA,one\nT,1\n");

        var summary = await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        summary.Progress.RecordsRead.Should().Be(3);
        summary.Progress.UnitsRead.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(1);
        summary.Progress.UnitsExecuted.Should().Be(1);
        environment.Validated.Should().ContainSingle().Which.Unit.Should().BeOfType<Alpha>().Which.Text.Should().Be("one");
        environment.Executed.Should().ContainSingle().Which.Unit.Should().BeOfType<Alpha>().Which.Text.Should().Be("one");
        context.GetRecordCount("header").Should().Be(1);
        context.GetRecordCount("alpha").Should().Be(1);
        context.GetRecordCount("trailer").Should().Be(1);
        context.GetMetadataLineNumber("Header").Should().Be(1);
        context.GetMetadataLineNumber("Trailer").Should().Be(3);
        context.GetAcceptedMetadata<Alpha>("Header")?.Text.Should().Be("batch");
        context.GetAcceptedMetadata<Beta>("Trailer")?.Number.Should().Be(1);
        environment.Executed[0].Context.GetAcceptedMetadata<Alpha>("Header")?.Text.Should().Be("batch");
        input.Streams.Should().HaveCount(2);
    }

    [TestCase("A,one\n", "csv.metadata.required")]
    [TestCase("H,batch\nH,again\nA,one\n", "csv.metadata.duplicate")]
    [TestCase("A,one\nH,late\n", "csv.metadata.first-position")]
    [TestCase("H,batch\nA,one\nT,1\nA,late\n", "csv.metadata.after-last")]
    public async Task MetadataRules_RejectInvalidOccurrencesAndPositions(string text, string expectedCode)
    {
        var processor = new Processor(definition: () => new(0,
        [
            new CsvRoutedRecord<Alpha, Unit>("header", "H", AlphaMapping, x => x, new AlphaValidator()),
            AlphaRoute(),
            new CsvRoutedRecord<Beta, Unit>("trailer", "T", BetaMapping, x => x)
        ],
        [
            new("Header", "header", true, FileRecordPosition.First),
            new("Trailer", "trailer", true, FileRecordPosition.Last)
        ]), reconcile: false);

        var summary = await processor.ProcessAsync(new Input(text).OpenAsync, Context()).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        processor.Diagnostics.Should().Contain(x => x.Code == expectedCode);
    }

    [TestCase("", "csv.discriminator.missing")]
    [TestCase(" ", "csv.discriminator.missing")]
    [TestCase("a", "csv.discriminator.unknown")]
    [TestCase(" A", "csv.discriminator.unknown")]
    public async Task Discriminator_IsExactAndDoesNotLeakInput(string match, string code)
    {
        var processor = new Processor(definition: () => new(0, [AlphaRoute()]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input(match + ",private-value\n").OpenAsync, Context()).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(1);
        processor.Diagnostics.Should().ContainSingle().Which.Code.Should().Be(code);
        processor.Diagnostics[0].Location?.FieldIndex.Should().Be(0);
        processor.Diagnostics[0].Message.ToString().Should().NotContain("private-value");
    }

    [Test]
    public async Task MissingDiscriminatorField_ReportsConfiguredIndex()
    {
        var processor = new Processor(definition: () => new(3, [AlphaRoute()]), reconcile: false);
        await processor.ProcessAsync(new Input("A,value\n").OpenAsync, Context()).ConfigureAwait(false);
        processor.Diagnostics.Should().ContainSingle().Which.Location?.FieldIndex.Should().Be(3);
    }

    [Test]
    public async Task MalformedRecords_DoNotTrustRecoveredDiscriminator()
    {
        var processor = new Processor(definition: () => new(0, [AlphaRoute()]), reconcile: false);
        var context = Context();
        var summary = await processor.ProcessAsync(new Input("A,\"bad\"text\nA,good\n").OpenAsync, context).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(1);
        context.GetRecordCount("alpha").Should().Be(1);
        processor.Diagnostics.Should().ContainSingle().Which.Code.Should().Be("csv.record.invalid");
    }

    [Test]
    public async Task MissingOccurrences_UseEofAnchorWithoutInventingInvalidRecords()
    {
        var processor = new Processor(definition: () => new(0, [AlphaRoute(cardinality: FileCardinality.One), BetaRoute(cardinality: FileCardinality.One)]), reconcile: false);
        var context = Context();
        var summary = await processor.ProcessAsync(new Input("#comment\n\n").OpenAsync, context).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(0);
        processor.Diagnostics.Should().HaveCount(2).And.OnlyContain(x => x.Code == "csv.structure.required" && x.Location != null && x.Location.LineNumber == 1);
        context.EndOfFileLineNumber.Should().Be(1);
    }

    [TestCase(FileErrorMode.CollectErrors, 3, true)]
    [TestCase(FileErrorMode.FailFast, 1, false)]
    public async Task FilePolicy_AppliesToMappingAndValidation(FileErrorMode fileMode, int read, bool complete)
    {
        var processor = new Processor(definition: () => new(0, [AlphaRoute(), BetaRoute()]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("B,bad\nA,\nB,1\n").OpenAsync, Context(errors: fileMode)).ConfigureAwait(false);
        summary.Progress.RecordsRead.Should().Be(read);
        summary.ValidationCompleted.Should().Be(complete);
        processor.Events.Should().Equal(complete ? ["eof", "rejected"] : ["rejected"]);
    }

    [TestCase(FileErrorMode.FailFast, 2, false)]
    [TestCase(FileErrorMode.CollectErrors, 3, true)]
    public async Task PositionRule_UsesFilePolicy(FileErrorMode fileMode, int records, bool complete)
    {
        var processor = new Processor(definition: () => new(0,
        [
            new CsvRoutedRecord<Beta, Unit>("trailer", "T", BetaMapping, x => x, cardinality: FileCardinality.Optional,
                position: FileRecordPosition.Last),
            AlphaRoute()
        ]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("T,0\nA,one\nA,two\n").OpenAsync, Context(errors: fileMode)).ConfigureAwait(false);
        summary.Progress.RecordsRead.Should().Be(records);
        summary.ValidationCompleted.Should().Be(complete);
        processor.Diagnostics.Select(x => x.RecordType).Should().OnlyContain(x => x == "trailer");
    }

    [TestCase(FileErrorMode.FailFast, 1, false)]
    [TestCase(FileErrorMode.CollectErrors, 2, true)]
    public async Task MissingRule_UsesFilePolicyAtEof(FileErrorMode fileMode, int errors, bool complete)
    {
        var processor = new Processor(definition: () => new(0,
            [AlphaRoute(cardinality: FileCardinality.One), BetaRoute(cardinality: FileCardinality.One)]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("").OpenAsync, Context(errors: fileMode)).ConfigureAwait(false);
        summary.ValidationCompleted.Should().Be(complete);
        processor.Diagnostics.Should().HaveCount(errors);
        processor.Diagnostics[0].RecordType.Should().Be("alpha");
        processor.Events.Should().Equal(complete ? ["eof", "rejected"] : ["rejected"]);
    }

    [Test]
    public async Task CustomValidation_UsesFilePolicyAndScopedDiagnosticLocation()
    {
        await using var environment = new Environment { Reject = true };
        var processor = new Processor(environment, () => new(0, [AlphaRoute()]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("\nA,one\nA,two\n").OpenAsync, Context(errors: FileErrorMode.FailFast)).ConfigureAwait(false);
        summary.Progress.RecordsRead.Should().Be(1);
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(0);
        processor.Diagnostics.Should().ContainSingle().Which.RecordType.Should().Be("alpha");
        processor.Diagnostics[0].Location?.LineNumber.Should().Be(2);
        environment.Handlers.Should().ContainSingle().Which.Disposed.Should().BeTrue();
    }

    [Test]
    public async Task ExecutionFailure_PreservesResultAndStopsWithoutTerminalReport()
    {
        await using var environment = new Environment { Fail = true };
        var processor = new Processor(environment, () => new(0, [AlphaRoute()]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("A,one\nA,two\n").OpenAsync, Context(FileProcessingMode.ValidateThenExecute)).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ExecutionFailed);
        summary.ExecutionResult.Should().Be(environment.Failure);
        summary.Progress.UnitsAttempted.Should().Be(1);
        summary.Progress.UnitsExecuted.Should().Be(0);
        processor.Events.Should().Equal("eof");
    }

    [TestCase("A,one\nA,two\n", "A,one\n")]
    [TestCase("A,one\n", "A,one\nA,two\n")]
    [TestCase("B,1\n", "B,bad\n")]
    [TestCase("A,one\n", "X,unknown\n")]
    public async Task ReplayDrift_IsOperationalFaultNotLateContentRejection(string validation, string replay)
    {
        await using var environment = new Environment();
        var processor = new Processor(environment, () => new(0, [AlphaRoute(), BetaRoute()]), reconcile: false);
        var opens = 0;
        Task<Stream> OpenAsync(CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(++opens == 1 ? validation : replay)));
        Func<Task> action = () => processor.ProcessAsync(OpenAsync, Context(FileProcessingMode.ValidateThenExecute));
        await action.Should().ThrowAsync<InvalidDataException>().ConfigureAwait(false);
        processor.Diagnostics.Should().BeEmpty();
        processor.Events.Should().Equal("eof");
    }

    [Test]
    public async Task Diagnostics_ApplyBackpressureAndCancellationDisposesSource()
    {
        using var cancellation = new CancellationTokenSource();
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var processor = new Processor(definition: () => new(0, [AlphaRoute()]), reconcile: false)
        {
            Diagnostic = async (_, ct) =>
            {
                entered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
            }
        };
        var input = new Input("X,one\nA,two\n");
        var context = Context();
        var task = processor.ProcessAsync(input.OpenAsync, context, cancellation.Token);
        await entered.Task.ConfigureAwait(false);
        context.Progress.RecordsRead.Should().Be(1);
        cancellation.Cancel();
        Func<Task> action = () => task;
        await action.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        input.Streams.Should().ContainSingle().Which.Disposed.Should().BeTrue();
        processor.Events.Should().BeEmpty();
    }

    [Test]
    public async Task ContextReuseAndNamedMappings_FailBeforeOpeningInput()
    {
        var input = new Input("A,one\n");
        var context = Context();
        var processor = new Processor(definition: () => new(0, [AlphaRoute()]), reconcile: false);
        await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);
        Func<Task> reuse = () => processor.ProcessAsync(input.OpenAsync, context);
        await reuse.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        var invalid = new Processor(definition: () => new(0,
            [new CsvRoutedRecord<Alpha, Unit>("alpha", "A", () => new CsvFileMapping<Alpha>().MapProperty("Text", x => x.Text, CsvFieldConverters.String), x => x)]), reconcile: false);
        Func<Task> named = () => invalid.ProcessAsync(input.OpenAsync, Context());
        await named.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        input.Streams.Should().ContainSingle();
    }

    [Test]
    public async Task ConcurrentInvocations_KeepRoutingCountsAndScopesIsolated()
    {
        await using var environment = new Environment();
        var processor = new CsvRoutedFileProcessor<Unit>(() => new(0, [AlphaRoute(), BetaRoute()]), serviceProvider: environment.Provider,
            handlerFactory: sp => sp.GetRequiredService<ConcurrentHandler>());
        var summaries = await Task.WhenAll(Enumerable.Range(0, 20).Select(i =>
            processor.ProcessAsync(new Input(i % 2 == 0 ? "A,one\n" : "B,1\n").OpenAsync, Context(FileProcessingMode.ValidateThenExecute)))).ConfigureAwait(false);
        summaries.Should().HaveCount(20).And.OnlyContain(x => x.IsSuccess && x.Progress.UnitsExecuted == 1);
    }

    [Test]
    public async Task LargeInvalidFile_DoesNotThrowPerContentFailure()
    {
        var count = 0;
        void Observe(object? sender, FirstChanceExceptionEventArgs args) => Interlocked.Increment(ref count);
        var processor = new CsvRoutedFileProcessor<Unit>(() => new(0, [AlphaRoute(), BetaRoute()]));
        var input = new Input(string.Concat(Enumerable.Repeat("A,\nB,bad\nX,unknown\n", 3000)));
        AppDomain.CurrentDomain.FirstChanceException += Observe;
        FileProcessingSummary summary;
        try
        {
            summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= Observe;
        }

        summary.Progress.InvalidRecords.Should().Be(9000);
        summary.Progress.ErrorCount.Should().Be(9000);
        count.Should().Be(0);
    }

    [Test]
    public async Task CompletedRecords_AreNotRetainedByRoutingOrContext()
    {
        var references = new List<WeakReference>();
        var processor = new CsvRoutedFileProcessor<Unit>(() => new(0,
        [
            new CsvRoutedRecord<Alpha, Unit>("alpha", "A", AlphaMapping, value =>
            {
                references.Add(new(value));
                return value;
            })
        ]));
        var context = Context();
        var summary = await processor.ProcessAsync(new Input(string.Concat(Enumerable.Repeat("A,value\n", 2000))).OpenAsync, context).ConfigureAwait(false);
        summary.Progress.UnitsValidated.Should().Be(2000);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        references.Should().HaveCount(2000);
        references.Take(1998).Should().OnlyContain(x => !x.IsAlive);
        GC.KeepAlive(processor);
        GC.KeepAlive(context);
    }

    [Test]
    public async Task RouteValidators_AreNotRepeatedOnExecutionReplay()
    {
        await using var environment = new Environment();
        var validator = new AlphaValidator();
        var processor = new Processor(environment, () => new(0,
            [new CsvRoutedRecord<Alpha, Unit>("alpha", "A", AlphaMapping, x => x, validator)]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("A,one\nA,two\n").OpenAsync, Context(FileProcessingMode.ValidateThenExecute)).ConfigureAwait(false);
        summary.Progress.UnitsExecuted.Should().Be(2);
        validator.Calls.Should().Be(2);
        environment.Validated.Should().HaveCount(2);
    }

    [Test]
    public void Definition_RejectsAmbiguousAndInvalidRules()
    {
        Action empty = () => new CsvRoutedDefinition<Unit>(0, []);
        Action keys = () => new CsvRoutedDefinition<Unit>(0, [AlphaRoute(), new CsvRoutedRecord<Beta, Unit>("ALPHA", "B", BetaMapping, x => x)]);
        Action matches = () => new CsvRoutedDefinition<Unit>(0, [AlphaRoute(), new CsvRoutedRecord<Beta, Unit>("beta", "A", BetaMapping, x => x)]);
        Action positions = () => new CsvRoutedDefinition<Unit>(0,
            [new CsvRoutedRecord<Alpha, Unit>("first", "H", AlphaMapping, x => x, cardinality: FileCardinality.One, position: FileRecordPosition.First),
             new CsvRoutedRecord<Alpha, Unit>("other", "O", AlphaMapping, x => x, cardinality: FileCardinality.Optional, position: FileRecordPosition.First)]);
        Action metadataRouteSettings = () => new CsvRoutedDefinition<Unit>(0,
            [new CsvRoutedRecord<Alpha, Unit>("header", "H", AlphaMapping, x => x, cardinality: FileCardinality.One, position: FileRecordPosition.First)],
            [new("Header", "header", false, FileRecordPosition.First)]);
        Action manyBoundary = () => new CsvRoutedRecord<Alpha, Unit>("header", "H", AlphaMapping, x => x, position: FileRecordPosition.First);
        empty.Should().Throw<ArgumentException>();
        keys.Should().Throw<ArgumentException>();
        matches.Should().Throw<ArgumentException>();
        positions.Should().Throw<ArgumentException>();
        metadataRouteSettings.Should().Throw<ArgumentException>();
        manyBoundary.Should().Throw<ArgumentException>();
    }

    private static ContextState Context(FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errors = FileErrorMode.CollectErrors)
        => new(new("mixed", "1", "mixed.csv", "attempt", "revision"), mode, errors);
    private static CsvFileMapping<Alpha> AlphaMapping() => new CsvFileMapping<Alpha>().MapProperty("Text", x => x.Text, CsvFieldConverters.String, 1);
    private static CsvFileMapping<Beta> BetaMapping() => new CsvFileMapping<Beta>().MapProperty("Number", x => x.Number, CsvFieldConverters.Nullable<int>(), 1);
    private static CsvRoutedRecord<Unit> AlphaRoute(FileCardinality cardinality = FileCardinality.Many)
        => new CsvRoutedRecord<Alpha, Unit>("alpha", "A", AlphaMapping, x => x, new AlphaValidator(), cardinality);
    private static CsvRoutedRecord<Unit> BetaRoute(FileCardinality cardinality = FileCardinality.Many)
        => new CsvRoutedRecord<Beta, Unit>("beta", "B", BetaMapping, x => x, cardinality: cardinality);
    private static CsvRoutedDefinition<Unit> Definition(FileCardinality alphaCardinality = FileCardinality.Many) => new(0,
    [
        new CsvRoutedRecord<Alpha, Unit>("header", "H", AlphaMapping, x => x, cardinality: FileCardinality.One, position: FileRecordPosition.First),
        AlphaRoute(alphaCardinality),
        BetaRoute(),
        new CsvRoutedRecord<Beta, Unit>("trailer", "T", BetaMapping, x => x, cardinality: FileCardinality.One, position: FileRecordPosition.Last)
    ]);

    public abstract class Unit;
    public sealed class Alpha : Unit
    {
        public string? Text { get; set; }
    }
    public sealed class Beta : Unit
    {
        public int? Number { get; set; }
    }
    private sealed class AlphaValidator : Validator<Alpha>
    {
        public AlphaValidator() => Property(x => x.Text).NotNull();
        public int Calls { get; private set; }
        protected override Task OnValidateAsync(ValidationContext<Alpha> context, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }
    private sealed class ContextState(FileProcessingIdentity identity, FileProcessingMode mode, FileErrorMode errors) : CsvRoutedContext(identity, mode, errors)
    {
        public int Total { get; set; }
        public int? Trailer { get; set; }
        public bool HasBoundaries { get; set; }
    }
    private sealed class Processor : CsvRoutedFileProcessor<Unit>
    {
        private readonly bool _reconcile;
        public Processor(Environment? environment = null, Func<CsvRoutedDefinition<Unit>>? definition = null, CsvFileOptions? options = null, bool reconcile = true)
            : base(definition ?? (() => Definition()), options ?? new() { Comment = '#' }, serviceProvider: environment?.Provider,
                handlerFactory: environment is null ? null : sp => sp.GetRequiredService<Handler>()) => _reconcile = reconcile;
        public List<FileDiagnostic> Diagnostics { get; } = [];
        public List<string> Events { get; } = [];
        public List<ExecutionContext?> Ambient { get; } = [];
        public Func<FileDiagnostic, CancellationToken, Task>? Diagnostic { get; init; }
        protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
            => new ContextState(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);
        protected override void OnConfigureUnitContext(FileUnitContextArgs<Unit> args)
        {
            args.UnitContext.TenantId = "trusted";
            if (args.Context is ContextState state)
                state.HasBoundaries = _reconcile;
        }
        protected override async Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Ambient.Add(ExecutionContext.Current);
            Diagnostics.Add(args.Diagnostic);
            if (Diagnostic is not null)
                await Diagnostic(args.Diagnostic, cancellationToken).ConfigureAwait(false);
        }
        protected override async Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
        {
            Events.Add("eof");
            Ambient.Add(ExecutionContext.Current);
            if (_reconcile && args.Context is ContextState state && !state.HasValidationErrors && state.Total != state.Trailer)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "file.total", "The declared total does not match."), cancellationToken).ConfigureAwait(false);
        }
        protected override Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Events.Add("rejected");
            Ambient.Add(ExecutionContext.Current);
            return Task.CompletedTask;
        }
        protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Events.Add("completed");
            Ambient.Add(ExecutionContext.Current);
            return Task.CompletedTask;
        }
    }
    private sealed class Environment : IAsyncDisposable
    {
        public Environment()
        {
            var services = new ServiceCollection();
            services.AddExecutionContext();
            services.AddSingleton(this);
            services.AddScoped<Handler>();
            services.AddScoped<ConcurrentHandler>();
            Provider = services.BuildServiceProvider();
        }
        public ServiceProvider Provider { get; }
        public List<Handler> Handlers { get; } = [];
        public List<(Unit Unit, ContextState Context, long Recognized)> Validated { get; } = [];
        public List<(Unit Unit, ContextState Context, long Recognized)> Executed { get; } = [];
        public bool Reject { get; init; }
        public bool Fail { get; init; }
        public Result Failure { get; } = Result.NotFoundError();
        public ValueTask DisposeAsync() => Provider.DisposeAsync();
    }
    private sealed class Handler : FileUnitHandler<Unit>, IDisposable
    {
        private readonly Environment _environment;
        public Handler(Environment environment, ExecutionContext ambient)
        {
            _environment = environment;
            Ambient = ambient;
            ambient.TenantId.Should().Be("trusted");
            environment.Handlers.Add(this);
        }
        public ExecutionContext Ambient { get; }
        public bool Disposed { get; private set; }
        protected override async Task OnValidateAsync(FileUnitValidationArgs<Unit> args, CancellationToken cancellationToken)
        {
            var state = (ContextState)args.Context;
            _environment.Validated.Add((args.Unit, state, Count(args.Unit, state)));
            if (_environment.Reject)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "business.rule", "The record is not accepted."), cancellationToken).ConfigureAwait(false);

            if (args.Unit is Beta { Number: { } number })
            {
                if (state.HasBoundaries && state.GetRecordCount("trailer") > 0)
                    state.Trailer = number;
                else
                    state.Total += number;
            }
        }
        protected override Task<Result> OnExecuteAsync(FileUnitArgs<Unit> args, CancellationToken cancellationToken)
        {
            var state = (ContextState)args.Context;
            _environment.Executed.Add((args.Unit, state, Count(args.Unit, state)));
            return Task.FromResult(_environment.Fail ? _environment.Failure : Result.Success);
        }
        private static long Count(Unit unit, ContextState context) => unit switch
        {
            Alpha { Text: "batch" } => context.GetRecordCount("header"),
            Alpha => context.GetRecordCount("alpha"),
            Beta => context.HasBoundaries && context.GetRecordCount("trailer") > 0 ? context.GetRecordCount("trailer") : context.GetRecordCount("beta"),
            _ => throw new InvalidOperationException()
        };
        public void Dispose() => Disposed = true;
    }
    private sealed class ConcurrentHandler(ExecutionContext ambient) : FileUnitHandler<Unit>
    {
        protected override async Task OnValidateAsync(FileUnitValidationArgs<Unit> args, CancellationToken cancellationToken)
        {
            ambient.TenantId = args.Unit is Alpha ? "a" : "b";
            await Task.Yield();
            ExecutionContext.Current.Should().BeSameAs(ambient);
            ambient.TenantId.Should().Be(args.Unit is Alpha ? "a" : "b");
        }
        protected override async Task<Result> OnExecuteAsync(FileUnitArgs<Unit> args, CancellationToken cancellationToken)
        {
            ambient.TenantId.Should().BeNull();
            await Task.Yield();
            ExecutionContext.Current.Should().BeSameAs(ambient);
            return Result.Success;
        }
    }
    private sealed class Input(string text)
    {
        public string Text { get; } = text;
        public List<OwnedStream> Streams { get; } = [];
        public Task<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = new OwnedStream(Encoding.UTF8.GetBytes(Text));
            Streams.Add(stream);
            return Task.FromResult<Stream>(stream);
        }
    }
    private sealed class OwnedStream(byte[] bytes) : MemoryStream(bytes, writable: false)
    {
        public bool Disposed { get; private set; }
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
