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

            summary.Outcome.Should().Be(mode == FileProcessingMode.ValidateOnly ? FileProcessingOutcome.Validated : FileProcessingOutcome.Executed);
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
        summary.Outcome.Should().Be(FileProcessingOutcome.Rejected);
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.InvalidRecords.Should().Be(invalid);
        input.Streams.Should().ContainSingle();
        environment.Executed.Should().BeEmpty();
        processor.Events.Should().Equal("eof", "rejected");
    }

    [TestCase(CsvRecordCardinality.Many, "", true)]
    [TestCase(CsvRecordCardinality.Many, "A,one\nA,two\n", true)]
    [TestCase(CsvRecordCardinality.One, "", false)]
    [TestCase(CsvRecordCardinality.One, "A,one\n", true)]
    [TestCase(CsvRecordCardinality.One, "A,one\nA,two\n", false)]
    [TestCase(CsvRecordCardinality.Optional, "", true)]
    [TestCase(CsvRecordCardinality.Optional, "A,one\nA,two\n", false)]
    [TestCase(CsvRecordCardinality.OneOrMore, "", false)]
    [TestCase(CsvRecordCardinality.OneOrMore, "A,one\nA,two\n", true)]
    public async Task Cardinality_IsPerFile(CsvRecordCardinality cardinality, string text, bool success)
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
            [key == "alpha" ? AlphaRoute(cardinality: CsvRecordCardinality.One) : BetaRoute(cardinality: CsvRecordCardinality.One)]), reconcile: false);
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
        var processor = new Processor(definition: () => Definition(alphaCardinality: CsvRecordCardinality.One), reconcile: false);
        var summary = await processor.ProcessAsync(new Input(text).OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.Rejected);
        processor.Diagnostics.Should().Contain(x => x.Code == code && x.RecordType == owner && x.Location != null && x.Location.LineNumber == line);
    }

    [Test]
    public async Task OptionalBoundaries_AreNotRequiredButConstrainPositionsWhenPresent()
    {
        CsvRoutedDefinition<Unit> Definition() => new(0,
        [
            new CsvRoutedRecord<Alpha, Unit>("first", "H", AlphaMapping, x => x, cardinality: CsvRecordCardinality.Optional, position: CsvRecordPosition.First),
            AlphaRoute(),
            new CsvRoutedRecord<Beta, Unit>("last", "T", BetaMapping, x => x, cardinality: CsvRecordCardinality.Optional, position: CsvRecordPosition.Last)
        ]);
        var accepted = new Processor(definition: Definition, reconcile: false);
        (await accepted.ProcessAsync(new Input("A,one\n").OpenAsync, Context()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        var rejected = new Processor(definition: Definition, reconcile: false);
        await rejected.ProcessAsync(new Input("A,one\nH,late\nT,0\nA,after\n").OpenAsync, Context()).ConfigureAwait(false);
        rejected.Diagnostics.Select(x => x.Code).Should().Contain("csv.structure.first-position").And.Contain("csv.structure.after-last");
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
        var processor = new Processor(definition: () => new(0, [AlphaRoute(cardinality: CsvRecordCardinality.One), BetaRoute(cardinality: CsvRecordCardinality.One)]), reconcile: false);
        var context = Context();
        var summary = await processor.ProcessAsync(new Input("#comment\n\n").OpenAsync, context).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(0);
        processor.Diagnostics.Should().HaveCount(2).And.OnlyContain(x => x.Code == "csv.structure.required" && x.Location != null && x.Location.LineNumber == 1);
        context.EndOfFileLineNumber.Should().Be(1);
    }

    [TestCase(FileErrorMode.FailFast, FileErrorMode.CollectErrors, 3, true)]
    [TestCase(FileErrorMode.CollectErrors, FileErrorMode.FailFast, 1, false)]
    public async Task RoutePolicy_OverridesFilePolicyForMappingAndValidation(FileErrorMode fileMode, FileErrorMode routeMode, int read, bool complete)
    {
        var processor = new Processor(definition: () => new(0, [AlphaRoute(errorMode: routeMode), BetaRoute(errorMode: routeMode)]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("B,bad\nA,\nB,1\n").OpenAsync, Context(errors: fileMode)).ConfigureAwait(false);
        summary.Progress.RecordsRead.Should().Be(read);
        summary.ValidationCompleted.Should().Be(complete);
        processor.Events.Should().Equal(complete ? ["eof", "rejected"] : ["rejected"]);
    }

    [Test]
    public async Task PositionRule_UsesBoundaryOwnerPolicyNotCurrentRecordPolicy()
    {
        var processor = new Processor(definition: () => new(0,
        [
            new CsvRoutedRecord<Beta, Unit>("trailer", "T", BetaMapping, x => x, cardinality: CsvRecordCardinality.Optional,
                position: CsvRecordPosition.Last, errorMode: FileErrorMode.FailFast),
            AlphaRoute(errorMode: FileErrorMode.CollectErrors)
        ]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("T,0\nA,one\nA,two\n").OpenAsync, Context()).ConfigureAwait(false);
        summary.Progress.RecordsRead.Should().Be(2);
        summary.ValidationCompleted.Should().BeFalse();
    }

    [Test]
    public async Task MissingRule_UsesMissingRoutePolicyAtEof()
    {
        var processor = new Processor(definition: () => new(0,
            [AlphaRoute(cardinality: CsvRecordCardinality.One, errorMode: FileErrorMode.FailFast), BetaRoute(cardinality: CsvRecordCardinality.One)]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("").OpenAsync, Context()).ConfigureAwait(false);
        summary.ValidationCompleted.Should().BeFalse();
        processor.Diagnostics.Should().ContainSingle().Which.RecordType.Should().Be("alpha");
        processor.Events.Should().Equal("rejected");
    }

    [Test]
    public async Task CustomValidation_UsesRoutePolicyAndScopedDiagnosticLocation()
    {
        await using var environment = new Environment { Reject = true };
        var processor = new Processor(environment, () => new(0, [AlphaRoute(errorMode: FileErrorMode.FailFast)]), reconcile: false);
        var summary = await processor.ProcessAsync(new Input("\nA,one\nA,two\n").OpenAsync, Context()).ConfigureAwait(false);
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
    public async Task ByteLimit_IsTerminalAndPreventsReplay()
    {
        await using var environment = new Environment();
        var input = new Input("A," + new string('x', 500) + "\n");
        var processor = new Processor(environment, () => new(0, [AlphaRoute()]), new() { MaxInputBytes = 32 }, false);
        var summary = await processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateThenExecute)).ConfigureAwait(false);
        summary.ValidationCompleted.Should().BeFalse();
        summary.Progress.BytesRead.Should().Be(33);
        summary.Progress.InvalidRecords.Should().Be(0);
        processor.Diagnostics.Should().ContainSingle().Which.Code.Should().Be("csv.limit.input-bytes");
        input.Streams.Should().ContainSingle();
        environment.Executed.Should().BeEmpty();
        processor.Events.Should().Equal("rejected");
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
            [new CsvRoutedRecord<Alpha, Unit>("first", "H", AlphaMapping, x => x, cardinality: CsvRecordCardinality.One, position: CsvRecordPosition.First),
             new CsvRoutedRecord<Alpha, Unit>("other", "O", AlphaMapping, x => x, cardinality: CsvRecordCardinality.Optional, position: CsvRecordPosition.First)]);
        Action manyBoundary = () => new CsvRoutedRecord<Alpha, Unit>("header", "H", AlphaMapping, x => x, position: CsvRecordPosition.First);
        empty.Should().Throw<ArgumentException>();
        keys.Should().Throw<ArgumentException>();
        matches.Should().Throw<ArgumentException>();
        positions.Should().Throw<ArgumentException>();
        manyBoundary.Should().Throw<ArgumentException>();
    }

    private static ContextState Context(FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errors = FileErrorMode.CollectErrors)
        => new(new("mixed", "1", "mixed.csv", "attempt", "revision"), mode, errors);
    private static CsvFileMapping<Alpha> AlphaMapping() => new CsvFileMapping<Alpha>().MapProperty("Text", x => x.Text, CsvFieldConverters.String, 1);
    private static CsvFileMapping<Beta> BetaMapping() => new CsvFileMapping<Beta>().MapProperty("Number", x => x.Number, CsvFieldConverters.Nullable<int>(), 1);
    private static CsvRoutedRecord<Unit> AlphaRoute(CsvRecordCardinality cardinality = CsvRecordCardinality.Many, FileErrorMode? errorMode = null)
        => new CsvRoutedRecord<Alpha, Unit>("alpha", "A", AlphaMapping, x => x, new AlphaValidator(), cardinality, errorMode: errorMode);
    private static CsvRoutedRecord<Unit> BetaRoute(CsvRecordCardinality cardinality = CsvRecordCardinality.Many, FileErrorMode? errorMode = null)
        => new CsvRoutedRecord<Beta, Unit>("beta", "B", BetaMapping, x => x, cardinality: cardinality, errorMode: errorMode);
    private static CsvRoutedDefinition<Unit> Definition(CsvRecordCardinality alphaCardinality = CsvRecordCardinality.Many) => new(0,
    [
        new CsvRoutedRecord<Alpha, Unit>("header", "H", AlphaMapping, x => x, cardinality: CsvRecordCardinality.One, position: CsvRecordPosition.First),
        AlphaRoute(alphaCardinality),
        BetaRoute(),
        new CsvRoutedRecord<Beta, Unit>("trailer", "T", BetaMapping, x => x, cardinality: CsvRecordCardinality.One, position: CsvRecordPosition.Last)
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
        protected override void OnConfigureUnitContext(FileProcessingContext fileContext, Unit unit, ExecutionContext? parent, ExecutionContext unitContext)
        {
            unitContext.TenantId = "trusted";
            if (fileContext is ContextState state)
                state.HasBoundaries = _reconcile;
        }
        protected override async Task OnDiagnosticAsync(FileProcessingContext context, FileDiagnostic diagnostic, CancellationToken cancellationToken)
        {
            Ambient.Add(ExecutionContext.Current);
            Diagnostics.Add(diagnostic);
            if (Diagnostic is not null)
                await Diagnostic(diagnostic, cancellationToken).ConfigureAwait(false);
        }
        protected override async Task OnValidateFileAsync(FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync, CancellationToken cancellationToken)
        {
            Events.Add("eof");
            Ambient.Add(ExecutionContext.Current);
            if (_reconcile && context is ContextState state && !state.HasValidationErrors && state.Total != state.Trailer)
                await reportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "file.total", "The declared total does not match."), cancellationToken).ConfigureAwait(false);
        }
        protected override Task OnRejectedAsync(FileProcessingContext context, FileProcessingSummary summary, CancellationToken cancellationToken)
        {
            Events.Add("rejected");
            Ambient.Add(ExecutionContext.Current);
            return Task.CompletedTask;
        }
        protected override Task OnCompletedAsync(FileProcessingContext context, FileProcessingSummary summary, CancellationToken cancellationToken)
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
        protected override async Task OnValidateAsync(Unit unit, FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync, CancellationToken cancellationToken)
        {
            var state = (ContextState)context;
            _environment.Validated.Add((unit, state, Count(unit, state)));
            if (_environment.Reject)
                await reportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "business.rule", "The record is not accepted."), cancellationToken).ConfigureAwait(false);

            if (unit is Beta { Number: { } number })
            {
                if (state.HasBoundaries && state.GetRecordCount("trailer") > 0)
                    state.Trailer = number;
                else
                    state.Total += number;
            }
        }
        protected override Task<Result> OnExecuteAsync(Unit unit, FileProcessingContext context, CancellationToken cancellationToken)
        {
            var state = (ContextState)context;
            _environment.Executed.Add((unit, state, Count(unit, state)));
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
        protected override async Task OnValidateAsync(Unit unit, FileProcessingContext context, Func<FileDiagnostic, CancellationToken, Task> reportDiagnosticAsync, CancellationToken cancellationToken)
        {
            ambient.TenantId = unit is Alpha ? "a" : "b";
            await Task.Yield();
            ExecutionContext.Current.Should().BeSameAs(ambient);
            ambient.TenantId.Should().Be(unit is Alpha ? "a" : "b");
        }
        protected override async Task<Result> OnExecuteAsync(Unit unit, FileProcessingContext context, CancellationToken cancellationToken)
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
