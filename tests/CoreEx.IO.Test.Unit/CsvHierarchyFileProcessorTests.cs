namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class CsvHierarchyFileProcessorTests
{
    [Test]
    public async Task ValidFixture_ReplaysFreshRootGroupsWithOneScopePerGroupAndPass()
    {
        await using var environment = new Environment();
        using var ambient = new ExecutionContext { ServiceProvider = environment.Provider, TenantId = "file" };
        ExecutionContext.SetCurrent(ambient);
        try
        {
            var input = new Input(Fixture("orders-valid.csv"));
            var processor = new Processor(environment);
            var context = Context(FileProcessingMode.ValidateThenExecute);
            var summary = await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);

            summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
            summary.Progress.RecordsRead.Should().Be(7);
            summary.Progress.InvalidRecords.Should().Be(0);
            summary.Progress.UnitsValidated.Should().Be(2);
            summary.Progress.UnitsExecuted.Should().Be(2);
            summary.Progress.BytesRead.Should().Be(Encoding.UTF8.GetByteCount(input.Text));
            environment.Validated.Select(x => x.Unit.Root.Name).Should().Equal("order-1", "order-2");
            environment.Executed.Select(x => x.Unit.Lines.Sum(y => y.Quantity)).Should().Equal(5, 1);
            environment.Executed[0].Unit.Root.Should().NotBeSameAs(environment.Validated[0].Unit.Root);
            environment.Executed[0].Unit.Lines[0].Should().NotBeSameAs(environment.Validated[0].Unit.Lines[0]);
            environment.Executed[0].Context.Should().NotBeSameAs(context);
            environment.Executed[0].Context.Identity.Should().BeSameAs(context.Identity);
            environment.Executed.Select(x => x.Context.Total).Should().Equal(0, 0);
            environment.Executed[0].Context.GetMetadata<Wire>("Header").Should().NotBeSameAs(context.GetMetadata<Wire>("Header"));
            context.GetAcceptedMetadata<Wire>("Header")?.Name.Should().Be("batch-1");
            environment.Executed[0].Context.GetAcceptedMetadata<Wire>("Header")?.Name.Should().Be("batch-1");
            environment.Executed[0].HeaderCount.Should().Be(1);
            environment.Executed.Select(x => x.RootCount).Should().Equal(1, 2);
            environment.Handlers.Should().HaveCount(4).And.OnlyContain(x => x.Disposed);
            environment.Handlers.Select(x => x.Ambient).Should().OnlyHaveUniqueItems();
            processor.Events.Should().Equal("eof", "completed");
            processor.FileAmbient.Should().OnlyContain(x => ReferenceEquals(x, ambient));
            ExecutionContext.Current.Should().BeSameAs(ambient);
            input.Streams.Should().HaveCount(2).And.OnlyContain(x => x.Disposed);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task AcceptedMetadata_IsIsolatedFromPassLocalValuesAndEveryTypedRead()
    {
        var context = Context(FileProcessingMode.ValidateOnly);
        var processor = new Processor();

        var summary = await processor.ProcessAsync(new Input(Fixture("orders-valid.csv")).OpenAsync, context).ConfigureAwait(false);

        summary.IsSuccess.Should().BeTrue();
        var first = context.GetAcceptedMetadata<Wire>("Header") ?? throw new InvalidOperationException("Accepted header is missing.");
        first.Name = "changed copy";
        var wire = context.GetMetadata<Wire>("Header") ?? throw new InvalidOperationException("Pass-local header is missing.");
        wire.Name = "changed wire";
        var second = context.GetAcceptedMetadata<Wire>("Header");
        second.Should().NotBeSameAs(first);
        second?.Name.Should().Be("batch-1");
        var accepted = context.AcceptedMetadata ?? throw new InvalidOperationException("Accepted snapshot is missing.");
        accepted["Header"].GetProperty("Name").GetString().Should().Be("batch-1");
    }

    [TestCase("orders-invalid-structure.csv", 2, 0)]
    [TestCase("orders-invalid-trailer.csv", 0, 2)]
    public async Task InvalidFixture_PreventsAllWrites(string fixture, int invalid, int validated)
    {
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var input = new Input(Fixture(fixture));
        var summary = await processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateThenExecute)).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.InvalidRecords.Should().Be(invalid);
        summary.Progress.UnitsValidated.Should().Be(validated);
        environment.Executed.Should().BeEmpty();
        input.Streams.Should().ContainSingle().Which.Disposed.Should().BeTrue();
        processor.Events.Should().Equal("eof", "rejected");
    }

    [TestCase("h,batch\n", "csv.discriminator.unknown")]
    [TestCase(" H,batch\n", "csv.discriminator.unknown")]
    [TestCase(",batch\n", "csv.discriminator.missing")]
    [TestCase(" ,batch\n", "csv.discriminator.missing")]
    public async Task Routing_IsExactAndNeverLeaksUnknownValues(string csv, string code)
    {
        var processor = new Processor();
        var summary = await processor.ProcessAsync(new Input(csv).OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.InvalidRecords.Should().Be(1);
        var diagnostic = processor.Diagnostics.Single(x => x.Code == code);
        diagnostic.RecordType.Should().BeNull();
        diagnostic.Location?.LineNumber.Should().Be(1);
        diagnostic.Message.ToString().Should().NotContain("batch");
    }

    [Test]
    public async Task MissingDiscriminatorIndex_IsContentFailure()
    {
        var definition = new CsvHierarchyDefinition<Group>(2, [Route("root", "O")], new("root", FileCardinality.One), Project);
        var processor = new Processor(definition: () => definition);
        var summary = await processor.ProcessAsync(new Input("O,one\n").OpenAsync, Context()).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(1);
        processor.Diagnostics[0].Code.Should().Be("csv.discriminator.missing");
        processor.Diagnostics[0].Location?.FieldIndex.Should().Be(2);
    }

    [Test]
    public async Task InvalidKnownRecords_CountWithoutAcceptedEnvelopesOrGroups()
    {
        var processor = new Processor();
        var context = Context();
        var summary = await processor.ProcessAsync(new Input("H,\nO,order\nL,item,private-value\nT,bad,bad\n").OpenAsync, context).ConfigureAwait(false);
        summary.Progress.RecordsRead.Should().Be(4);
        summary.Progress.InvalidRecords.Should().Be(3);
        summary.Progress.UnitsValidated.Should().Be(0);
        context.GetRecordCount("header").Should().Be(1);
        context.GetRecordCount("lines").Should().Be(1);
        context.GetRecordCount("trailer").Should().Be(1);
        context.GetMetadata<Wire>("Header").Should().BeNull();
        context.GetMetadata<Wire>("Trailer").Should().BeNull();
        context.HasValidationErrors.Should().BeTrue();
        processor.Diagnostics.Where(x => x.Code == "csv.field.invalid").Should().HaveCount(3);
        processor.Diagnostics.Should().NotContain(x => x.Code == "csv.structure.required" || x.Code == "csv.metadata.required");
        processor.Diagnostics.Select(x => x.Message.ToString()).Should().OnlyContain(x => !x.Contains("private-value"));
    }

    [TestCase("H,first\nH,second\nO,o\nL,p,1\nT,1,1\n", "csv.metadata.duplicate", 2)]
    [TestCase("O,o\nH,late\nL,p,1\nT,1,1\n", "csv.metadata.first-position", 2)]
    [TestCase("H,h\nO,o\nL,p,1\nT,1,1\nT,1,1\n", "csv.metadata.duplicate", 5)]
    [TestCase("H,h\nO,o\nL,p,1\nT,1,1\nO,late\n", "csv.metadata.after-last", 5)]
    [TestCase("H,h\nL,p,1\nT,0,0\n", "csv.structure.orphan", 2)]
    public async Task EnvelopeAndParentRules_HavePhysicalLocations(string csv, string code, int line)
    {
        var processor = new Processor();
        var summary = await processor.ProcessAsync(new Input(csv).OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        processor.Diagnostics.Should().Contain(x => x.Code == code && x.Location != null && x.Location.LineNumber == line);
    }

    [Test]
    public async Task DuplicateEnvelope_DoesNotReplaceFirstAcceptedValue()
    {
        var processor = new Processor();
        var context = Context();
        await processor.ProcessAsync(new Input("H,first\nH,second\nO,o\nL,p,1\nT,1,1\nT,1,2\n").OpenAsync, context).ConfigureAwait(false);
        context.GetMetadata<Wire>("Header")?.Name.Should().Be("first");
        context.GetMetadata<Wire>("Trailer")?.Total.Should().Be(1);
        context.GetRecordCount("header").Should().Be(2);
        context.GetRecordCount("trailer").Should().Be(2);
    }

    [TestCase("", 1)]
    [TestCase("#comment\n\n", 1)]
    [TestCase("H,\"two\nlines\"\n", 1)]
    public async Task MissingRequiredOccurrences_UseExplicitEofAnchorWithoutInventingInvalidRows(string csv, int eof)
    {
        var processor = new Processor(options: new() { Comment = '#' });
        var context = Context();
        var summary = await processor.ProcessAsync(new Input(csv).OpenAsync, context).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.InvalidRecords.Should().Be(0);
        context.EndOfFileLineNumber.Should().Be(eof);
        processor.Diagnostics.Where(x => x.Code == "csv.structure.required" || x.Code == "csv.metadata.required")
            .Should().OnlyContain(x => x.Location != null && x.Location.LineNumber == eof);
    }

    [Test]
    public async Task MissingChild_AnchorsParentNotFollowingRecordAndDoesNotInventInvalidRow()
    {
        var processor = new Processor();
        var summary = await processor.ProcessAsync(new Input("H,h\nO,one\nO,two\nL,p,1\nT,2,1\n").OpenAsync, Context()).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.UnitsValidated.Should().Be(1);
        processor.Diagnostics.Should().Contain(x => x.Code == "csv.structure.required" && x.RecordType == "lines" && x.Location != null && x.Location.LineNumber == 2);
    }

    [Test]
    public async Task ArbitraryDepth_ProjectsOrderedSubtreesAndPhysicalProvenance()
    {
        CsvAggregateNode? projected = null;
        var root = new CsvHierarchyNode("root", FileCardinality.OneOrMore,
            [new("branch", FileCardinality.OneOrMore, [new("leaf", FileCardinality.OneOrMore)]), new("end", FileCardinality.Optional)]);
        var definition = new CsvHierarchyDefinition<Group>(0, [Route("root", "O"), Route("branch", "B"), Route("leaf", "L"), Route("end", "E")], root, node =>
        {
            projected = node;
            return Project(node);
        });
        var processor = new Processor(definition: () => definition);
        var summary = await processor.ProcessAsync(new Input("#comment\nO,root\nB,first\nL,\"two\nlines\"\nB,second\nL,last\nE,end\n").OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        projected.Should().NotBeNull();
        var node = projected.ThrowIfNull();
        node.GetChildren("branch", x => x.GetRecord<Wire>().Name).Should().Equal("first", "second");
        node.Children[0].Children[0].LineNumber.Should().Be(4);
        node.GetLineNumber(node.Children[1].Children[0].GetRecord<Wire>()).Should().Be(7);
        Action foreign = () => node.GetLineNumber(new Wire());
        foreign.Should().Throw<ArgumentException>();
        Action type = () => node.GetRecord<Group>();
        type.Should().Throw<InvalidOperationException>();
    }

    [TestCase(FileCardinality.Many, "", true)]
    [TestCase(FileCardinality.Many, "L,one\nL,two\n", true)]
    [TestCase(FileCardinality.One, "", false)]
    [TestCase(FileCardinality.One, "L,one\n", true)]
    [TestCase(FileCardinality.One, "L,one\nL,two\n", false)]
    [TestCase(FileCardinality.Optional, "", true)]
    [TestCase(FileCardinality.Optional, "L,one\nL,two\n", false)]
    [TestCase(FileCardinality.OneOrMore, "", false)]
    [TestCase(FileCardinality.OneOrMore, "L,one\nL,two\n", true)]
    public async Task ChildCardinality_IsPerParent(FileCardinality cardinality, string lines, bool accepted)
    {
        var definition = new CsvHierarchyDefinition<Group>(0, [Route("root", "O"), Route("lines", "L")],
            new("root", FileCardinality.OneOrMore, [new("lines", cardinality)]), Project);
        var processor = new Processor(definition: () => definition);
        var summary = await processor.ProcessAsync(new Input($"O,first\n{lines}O,second\n{lines}").OpenAsync, Context()).ConfigureAwait(false);
        summary.IsSuccess.Should().Be(accepted);
        summary.Progress.UnitsValidated.Should().Be(accepted ? 2 : 0);
    }

    [Test]
    public async Task SiblingOrder_CannotReopenEarlierSubtreesAndMissingRequirementsAreReportedOnce()
    {
        var definition = new CsvHierarchyDefinition<Group>(0, [Route("root", "O"), Route("a", "A"), Route("b", "B"), Route("c", "C")],
            new("root", FileCardinality.One, [new("a", FileCardinality.One), new("b", FileCardinality.Many), new("c", FileCardinality.One)]), Project);
        var processor = new Processor(definition: () => definition);
        var summary = await processor.ProcessAsync(new Input("O,one\nC,c\nB,b\nA,a\n").OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        processor.Diagnostics.Count(x => x.Code == "csv.structure.required" && x.RecordType == "a").Should().Be(1);
        processor.Diagnostics.Count(x => x.Code == "csv.structure.child-order").Should().Be(2);
        summary.Progress.InvalidRecords.Should().Be(2);
    }

    [TestCase(FileErrorMode.FailFast, false)]
    [TestCase(FileErrorMode.CollectErrors, true)]
    public async Task FilePolicy_AppliesToMappingAndMissingChildren(FileErrorMode fileMode, bool completed)
    {
        foreach (var csv in new[] { "H,h\nO,o\nL,p,bad\nT,1,0\n", "H,h\nO,o\nT,1,0\n" })
        {
            var processor = new Processor();
            var summary = await processor.ProcessAsync(new Input(csv).OpenAsync, Context(errors: fileMode)).ConfigureAwait(false);
            summary.ValidationCompleted.Should().Be(completed);
            summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
            processor.Events.Contains("eof").Should().Be(completed);
        }
    }

    [Test]
    public async Task FieldValidator_RunsOncePerValidMappedRecordNeverOnReplay()
    {
        await using var environment = new Environment();
        var validator = new WireValidator();
        var processor = new Processor(environment, wireValidator: validator);
        var summary = await processor.ProcessAsync(new Input("H,h\nO,o\nL,p,1\nT,1,1\n").OpenAsync, Context(FileProcessingMode.ValidateThenExecute)).ConfigureAwait(false);
        summary.IsSuccess.Should().BeTrue();
        validator.Calls.Should().Be(1);
    }

    [Test]
    public async Task ScopedGroupErrors_AreNotInvalidPhysicalRecordsAndUseRootLocation()
    {
        await using var environment = new Environment { GroupError = true };
        var processor = new Processor(environment);
        var context = Context();
        var summary = await processor.ProcessAsync(new Input("H,h\nO,o\nL,p,1\nT,1,1\n").OpenAsync, context).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.UnitsValidated.Should().Be(0);
        processor.Diagnostics.Should().Contain(x => x.Code == "group.rule" && x.RecordType == "root" && x.Location != null && x.Location.LineNumber == 2);
        context.HasValidationErrors.Should().BeTrue();
        environment.Handlers.Should().ContainSingle().Which.Disposed.Should().BeTrue();
    }

    [TestCase(FileCardinality.Many, "", true)]
    [TestCase(FileCardinality.Optional, "", true)]
    [TestCase(FileCardinality.Optional, "O,one\nO,two\n", false)]
    [TestCase(FileCardinality.One, "", false)]
    [TestCase(FileCardinality.One, "O,one\n", true)]
    [TestCase(FileCardinality.OneOrMore, "", false)]
    [TestCase(FileCardinality.OneOrMore, "O,one\nO,two\n", true)]
    public async Task RootCardinality_IsFileWide(FileCardinality cardinality, string csv, bool success)
    {
        var processor = new CsvHierarchyFileProcessor<Group>(() => new(0, [Route("root", "O")], new("root", cardinality), Project));
        var summary = await processor.ProcessAsync(new Input(csv).OpenAsync, Context()).ConfigureAwait(false);
        summary.IsSuccess.Should().Be(success);
    }

    [Test]
    public async Task OptionalEnvelopes_CanBeAbsentAndTypedAccessFailsExplicitlyWhenIncorrect()
    {
        var processor = new CsvHierarchyFileProcessor<Group>(() => new(0, [Route("root", "O"), Route("header", "H"), Route("trailer", "T")],
            new("root", FileCardinality.One), Project,
            [new("Header", "header", false, FileRecordPosition.First), new("Trailer", "trailer", false, FileRecordPosition.Last)]));
        var missing = Context();
        (await processor.ProcessAsync(new Input("O,o\n").OpenAsync, missing).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        missing.GetMetadata<Wire>("Header").Should().BeNull();
        missing.GetMetadata<Wire>("Trailer").Should().BeNull();
        var present = Context();
        (await processor.ProcessAsync(new Input("H,h\nO,o\nT,t\n").OpenAsync, present).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        Action wrong = () => present.GetMetadata<Group>("Header");
        wrong.Should().Throw<InvalidOperationException>();
        Action unknown = () => present.GetRecordCount("unknown");
        unknown.Should().Throw<ArgumentException>();
    }

    [Test]
    public async Task SyntaxFailure_DoesNotTrustRecoveredDiscriminatorOrLoseActiveGroupInvalidity()
    {
        var processor = new Processor();
        var context = Context();
        var summary = await processor.ProcessAsync(new Input("H,h\nO,one\nL,ba\"d,1\nO,two\nL,p,1\nT,2,1\n").OpenAsync, context).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(1);
        context.GetRecordCount("lines").Should().Be(1);
        processor.Diagnostics.Should().Contain(x => x.Code == "csv.record.invalid" && x.RecordType == null && x.Location != null && x.Location.LineNumber == 3);
        processor.Diagnostics.Should().NotContain(x => x.Code == "csv.discriminator.unknown");
    }

    [TestCase(FileErrorMode.FailFast, false)]
    [TestCase(FileErrorMode.CollectErrors, true)]
    public async Task FilePolicy_AppliesToGroupFailuresAndMissingRoot(FileErrorMode fileMode, bool complete)
    {
        await using var environment = new Environment { GroupError = true };
        var definition = new CsvHierarchyDefinition<Group>(0,
            [new CsvRecordRoute<Wire>("root", "O", WireMapping)], new("root", FileCardinality.OneOrMore), Project);
        var processor = new Processor(environment, () => definition);
        var summary = await processor.ProcessAsync(new Input("O,one\nO,two\n").OpenAsync, Context(errors: fileMode)).ConfigureAwait(false);
        summary.ValidationCompleted.Should().Be(complete);
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.ErrorCount.Should().Be(complete ? 2 : 1);
        var empty = await processor.ProcessAsync(new Input("").OpenAsync, Context(errors: fileMode)).ConfigureAwait(false);
        empty.ValidationCompleted.Should().Be(complete);
        empty.Progress.InvalidRecords.Should().Be(0);
    }

    [Test]
    public async Task BaseReplayContext_IsFreshWithoutRequiringDerivedProcessor()
    {
        var contexts = new List<CsvHierarchyContext>();
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var processor = new CsvHierarchyFileProcessor<Group>(() => Orders(), serviceProvider: provider, handlerFactory: _ => new SimpleHandler(contexts));
        var validation = new CsvHierarchyContext(new("orders", "1", "orders.csv", "attempt"), FileProcessingMode.ValidateThenExecute, FileErrorMode.CollectErrors);
        var summary = await processor.ProcessAsync(new Input("H,h\nO,o\nL,p,1\nT,1,1\n").OpenAsync, validation).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        contexts.Should().HaveCount(2);
        contexts[0].Should().BeSameAs(validation);
        contexts[1].Should().NotBeSameAs(validation);
        contexts[1].GetMetadata<Wire>("Header").Should().NotBeSameAs(validation.GetMetadata<Wire>("Header"));
        contexts[1].GetAcceptedMetadata<Wire>("Header")?.Name.Should().Be("h");
    }

    [TestCase("")]
    [TestCase("O,one\nO,two\n")]
    public async Task ReplayCountDrift_FaultsInsteadOfReportingSuccess(string replay)
    {
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var processor = new CsvHierarchyFileProcessor<Group>(() => new(0, [Route("root", "O")], new("root", FileCardinality.Many), Project),
            serviceProvider: provider, handlerFactory: _ => new SimpleHandler([]));
        var opens = 0;
        Task<Stream> OpenAsync(CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(++opens == 1 ? "O,one\n" : replay)));
        Func<Task> action = () => processor.ProcessAsync(OpenAsync, Context(FileProcessingMode.ValidateThenExecute));
        await action.Should().ThrowAsync<InvalidDataException>().ConfigureAwait(false);
    }

    [Test]
    public async Task CompletedGroups_AreNotRetainedByFileState()
    {
        var references = new List<WeakReference>();
        var processor = new CsvHierarchyFileProcessor<Group>(() => new(0, [Route("root", "O")], new("root", FileCardinality.Many), node =>
        {
            var group = Project(node);
            references.Add(new WeakReference(group));
            references.Add(new WeakReference(node));
            references.Add(new WeakReference(group.Root));
            return group;
        }));
        var summary = await processor.ProcessAsync(new Input(string.Concat(Enumerable.Repeat("O,one\n", 500))).OpenAsync, Context()).ConfigureAwait(false);
        summary.Progress.UnitsValidated.Should().Be(500);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        references.Should().HaveCount(1500);
        references.Take(1494).Should().OnlyContain(x => !x.IsAlive);
    }

    [Test]
    public async Task ExecutionFailure_PreservesOriginalResultAndStopsWithoutTerminalReport()
    {
        await using var environment = new Environment { Fail = true };
        var processor = new Processor(environment);
        var summary = await processor.ProcessAsync(new Input(Fixture("orders-valid.csv")).OpenAsync, Context(FileProcessingMode.ValidateThenExecute)).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ExecutionFailed);
        summary.ExecutionResult.Should().Be(environment.Failure);
        summary.Progress.UnitsAttempted.Should().Be(1);
        summary.Progress.UnitsExecuted.Should().Be(0);
        processor.Events.Should().Equal("eof");
    }

    [Test]
    public async Task ReplayDrift_IsFaultNotLateContentRejection()
    {
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var opens = 0;
        Task<Stream> OpenAsync(CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(
            ++opens == 1 ? "H,h\nO,o\nL,p,1\nT,1,1\n" : "H,h\nO,o\nL,p,bad\nT,1,1\n")));
        Func<Task> action = () => processor.ProcessAsync(OpenAsync, Context(FileProcessingMode.ValidateThenExecute));
        await action.Should().ThrowAsync<InvalidDataException>().ConfigureAwait(false);
        processor.Diagnostics.Should().BeEmpty();
        processor.Events.Should().Equal("eof");
        environment.Executed.Should().BeEmpty();
    }

    [Test]
    public async Task ProjectionFault_PropagatesAndDisposesInput()
    {
        var fault = new IOException("Application callback failed.");
        var definition = new CsvHierarchyDefinition<Group>(0, [Route("root", "O")], new("root", FileCardinality.One), _ => throw fault);
        var processor = new Processor(definition: () => definition);
        var input = new Input("O,one\n");
        Func<Task> action = () => processor.ProcessAsync(input.OpenAsync, Context());
        (await action.Should().ThrowAsync<IOException>().ConfigureAwait(false)).Which.Should().BeSameAs(fault);
        input.Streams.Should().ContainSingle().Which.Disposed.Should().BeTrue();
        processor.Events.Should().BeEmpty();
    }

    [Test]
    public async Task ReportingFault_PropagatesWithoutTerminalAndDisposesInput()
    {
        var fault = new IOException("Report failed.");
        var processor = new Processor { Diagnostic = (_, _) => throw fault };
        var input = new Input("H,h\nX,unknown\n");
        Func<Task> action = () => processor.ProcessAsync(input.OpenAsync, Context());
        (await action.Should().ThrowAsync<IOException>().ConfigureAwait(false)).Which.Should().BeSameAs(fault);
        processor.Events.Should().BeEmpty();
        input.Streams.Should().ContainSingle().Which.Disposed.Should().BeTrue();
    }

    [Test]
    public async Task DiagnosticBackpressure_UsesFileAmbientAndCancellationStopsWithoutTerminal()
    {
        using var ambient = new ExecutionContext();
        ExecutionContext.SetCurrent(ambient);
        try
        {
            using var cancellation = new CancellationTokenSource();
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var processor = new Processor { Diagnostic = async (_, ct) =>
            {
                ExecutionContext.Current.Should().BeSameAs(ambient);
                entered.SetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, ct).ConfigureAwait(false);
            } };
            var input = new Input("H,h\nX,unknown\nO,o\nL,p,1\nT,1,1\n");
            var context = Context();
            var pending = processor.ProcessAsync(input.OpenAsync, context, cancellation.Token);
            await entered.Task.ConfigureAwait(false);
            context.Progress.RecordsRead.Should().Be(2);
            cancellation.Cancel();
            Func<Task> action = () => pending;
            await action.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
            processor.Events.Should().BeEmpty();
            input.Streams.Should().ContainSingle().Which.Disposed.Should().BeTrue();
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task ContextReuseAndNamedRouteMapping_FailBeforeOpeningInput()
    {
        var context = Context();
        var input = new Input("H,h\nO,o\nL,p,1\nT,1,1\n");
        var processor = new Processor();
        await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);
        Func<Task> reuse = () => processor.ProcessAsync(input.OpenAsync, context);
        await reuse.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        input.Streams.Should().ContainSingle();

        var route = new CsvRecordRoute<Wire>("root", "O", () => new CsvFileMapping<Wire>().MapProperty("Name", x => x.Name, CsvFieldConverters.String));
        var named = new Processor(definition: () => new(0, [route], new("root", FileCardinality.One), Project));
        Func<Task> invalid = () => named.ProcessAsync(input.OpenAsync, Context());
        await invalid.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false);
        input.Streams.Should().ContainSingle();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task IndependentDefinitionsAndFlatProcessor_CoexistAndConcurrentInvocationsAreIsolated(bool reverse)
    {
        var services = new ServiceCollection();
        services.AddExecutionContext();
        foreach (var key in reverse ? new[] { "b", "a" } : new[] { "a", "b" })
        {
            var name = key == "a" ? "one" : "two";
            var match = key == "a" ? "A" : "B";
            services.AddKeyedScoped<FileUnitHandler<Group>>(key, (sp, _) => new NamedGroupHandler(name, sp.GetRequiredService<ExecutionContext>()));
            services.AddKeyedSingleton<CsvHierarchyFileProcessor<Group>>(key, (sp, _) => new(() => new(0, [Route("root", match)],
                new("root", FileCardinality.One), Project), serviceProvider: sp, handlerFactory: scoped => scoped.GetRequiredKeyedService<FileUnitHandler<Group>>(key)));
        }

        services.AddScoped<FlatHandler>();
        services.AddSingleton(sp => new CsvFileProcessor<Wire>(WireMapping, serviceProvider: sp, handlerFactory: scoped => scoped.GetRequiredService<FlatHandler>()));
        await using var provider = services.BuildServiceProvider();
        var tasks = Enumerable.Range(0, 24).Select(async i =>
        {
            var key = i % 2 == 0 ? "a" : "b";
            var text = i % 2 == 0 ? "A,one\n" : "B,two\n";
            return await provider.GetRequiredKeyedService<CsvHierarchyFileProcessor<Group>>(key)
                .ProcessAsync(new Input(text).OpenAsync, Context(FileProcessingMode.ValidateThenExecute)).ConfigureAwait(false);
        }).Append(provider.GetRequiredService<CsvFileProcessor<Wire>>().ProcessAsync(new Input("any,flat\n").OpenAsync,
            new(new("flat", "1", "flat.csv", "attempt"), FileProcessingMode.ValidateThenExecute)));
        var summaries = await Task.WhenAll(tasks).ConfigureAwait(false);
        summaries.Should().HaveCount(25).And.OnlyContain(x => x.Outcome == FileProcessingOutcome.Succeeded && x.Progress.UnitsExecuted == 1);
    }

    [Test]
    public async Task LargeInvalidHierarchy_DoesNotThrowPerContentFailure()
    {
        var processor = new CsvHierarchyFileProcessor<Group>(() => Orders());
        var count = 0;
        void Observe(object? sender, FirstChanceExceptionEventArgs args) => Interlocked.Increment(ref count);
        AppDomain.CurrentDomain.FirstChanceException += Observe;
        try
        {
            var summary = await processor.ProcessAsync(new Input("H,h\nO,o\n" + string.Concat(Enumerable.Repeat("L,p,bad\n", 10_000)) + "T,1,0\n").OpenAsync, Context()).ConfigureAwait(false);
            summary.Progress.InvalidRecords.Should().Be(10_000);
            summary.Progress.ErrorCount.Should().Be(10_000);
            count.Should().Be(0);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= Observe;
        }
    }

    [Test]
    public void Definition_RejectsAmbiguousUnusedAndRepeatedKeys()
    {
        Action duplicateKey = () => new CsvHierarchyDefinition<Group>(0, [Route("root", "O"), Route("ROOT", "L")], new("root", FileCardinality.One), Project);
        Action duplicateMatch = () => new CsvHierarchyDefinition<Group>(0, [Route("root", "O"), Route("lines", "O")], new("root", FileCardinality.One), Project);
        Action unused = () => new CsvHierarchyDefinition<Group>(0, [Route("root", "O"), Route("lines", "L")], new("root", FileCardinality.One), Project);
        Action repeated = () => new CsvHierarchyDefinition<Group>(0, [Route("root", "O")], new("root", FileCardinality.One, [new("root")]), Project);
        Action overlap = () => new CsvHierarchyDefinition<Group>(0, [Route("root", "O"), Route("header", "H")], new("root"), Project,
            [new("Header", "root")]);
        duplicateKey.Should().Throw<ArgumentException>();
        duplicateMatch.Should().Throw<ArgumentException>();
        unused.Should().Throw<ArgumentException>();
        repeated.Should().Throw<ArgumentException>();
        overlap.Should().Throw<ArgumentException>();
    }

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name));
    private static ContextState Context(FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errors = FileErrorMode.CollectErrors)
        => new(new("orders", "1", "orders.csv", "attempt", "revision"), mode, errors);
    private static CsvFileMapping<Wire> WireMapping() => new CsvFileMapping<Wire>().MapProperty("Name", x => x.Name, CsvFieldConverters.String, 1);
    private static CsvRecordRoute Route(string key, string match) => new CsvRecordRoute<Wire>(key, match, WireMapping);
    private static Group Project(CsvAggregateNode node) => new(node.GetRecord<Wire>(), node.GetChildren("lines", x => x.GetRecord<Wire>()));
    private static CsvHierarchyDefinition<Group> Orders(WireValidator? validator = null)
        => new(0,
            [new CsvRecordRoute<Wire>("header", "H", WireMapping, new WireValidator()),
             Route("root", "O"),
             new CsvRecordRoute<Wire>("lines", "L", () => WireMapping().MapProperty("Quantity", x => x.Quantity, CsvFieldConverters.Nullable<int>(), 2), validator),
             new CsvRecordRoute<Wire>("trailer", "T", () => new CsvFileMapping<Wire>()
                .MapProperty("Quantity", x => x.Quantity, CsvFieldConverters.Nullable<int>(), 1).MapProperty("Total", x => x.Total, CsvFieldConverters.Nullable<int>(), 2))],
            new("root", FileCardinality.OneOrMore, [new("lines", FileCardinality.OneOrMore)]), Project,
            [new("Header", "header", true, FileRecordPosition.First), new("Trailer", "trailer", true, FileRecordPosition.Last)]);

    public sealed class Wire
    {
        public string? Name { get; set; }
        public int? Quantity { get; set; }
        public int? Total { get; set; }
    }

    public sealed record Group(Wire Root, IReadOnlyList<Wire> Lines);

    private sealed class WireValidator : Validator<Wire>
    {
        public int Calls { get; private set; }
        public WireValidator() => Property(x => x.Name).Mandatory();
        protected override Task OnValidateAsync(ValidationContext<Wire> context, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class ContextState(FileProcessingIdentity identity, FileProcessingMode mode, FileErrorMode errors) : CsvHierarchyContext(identity, mode, errors)
    {
        public int Total { get; set; }
    }

    private sealed class SimpleHandler(List<CsvHierarchyContext> contexts) : FileUnitHandler<Group>
    {
        protected override Task OnValidateAsync(FileUnitValidationArgs<Group> args, CancellationToken cancellationToken)
        {
            contexts.Add((CsvHierarchyContext)args.Context);
            return Task.CompletedTask;
        }
        protected override Task<Result> OnExecuteAsync(FileUnitArgs<Group> args, CancellationToken cancellationToken)
        {
            contexts.Add((CsvHierarchyContext)args.Context);
            return Task.FromResult(Result.Success);
        }
    }

    private sealed class NamedGroupHandler(string expected, ExecutionContext ambient) : FileUnitHandler<Group>
    {
        protected override async Task OnValidateAsync(FileUnitValidationArgs<Group> args, CancellationToken cancellationToken)
        {
            ambient.TenantId = expected;
            await Task.Yield();
            ExecutionContext.Current.Should().BeSameAs(ambient);
            args.Unit.Root.Name.Should().Be(expected);
            ambient.TenantId.Should().Be(expected);
        }
        protected override async Task<Result> OnExecuteAsync(FileUnitArgs<Group> args, CancellationToken cancellationToken)
        {
            ambient.TenantId.Should().BeNull();
            ambient.TenantId = expected;
            await Task.Yield();
            ExecutionContext.Current.Should().BeSameAs(ambient);
            args.Unit.Root.Name.Should().Be(expected);
            ambient.TenantId.Should().Be(expected);
            return Result.Success;
        }
    }

    private sealed class FlatHandler : FileUnitHandler<Wire>
    {
        protected override Task<Result> OnExecuteAsync(FileUnitArgs<Wire> args, CancellationToken cancellationToken)
        {
            args.Unit.Name.Should().Be("flat");
            return Task.FromResult(Result.Success);
        }
    }

    private sealed class Processor : CsvHierarchyFileProcessor<Group>
    {
        public Processor(Environment? environment = null, Func<CsvHierarchyDefinition<Group>>? definition = null,
            WireValidator? wireValidator = null, CsvFileOptions? options = null)
            : base(definition ?? (() => Orders(wireValidator)), options ?? new() { Comment = '#' }, serviceProvider: environment?.Provider,
                handlerFactory: environment is null ? null : sp => sp.GetRequiredService<Handler>()) { }
        public List<FileDiagnostic> Diagnostics { get; } = [];
        public List<string> Events { get; } = [];
        public List<ExecutionContext?> FileAmbient { get; } = [];
        public Func<FileDiagnostic, CancellationToken, Task>? Diagnostic { get; init; }
        protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
            => new ContextState(validationContext.Identity, validationContext.Mode, validationContext.ErrorMode);
        protected override void OnConfigureUnitContext(FileUnitContextArgs<Group> args)
            => args.UnitContext.TenantId = "trusted";
        protected override async Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Diagnostics.Add(args.Diagnostic);
            if (Diagnostic is not null)
                await Diagnostic(args.Diagnostic, cancellationToken).ConfigureAwait(false);
        }
        protected override async Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
        {
            Observe("eof");
            var hierarchy = (CsvHierarchyContext)args.Context;
            var trailer = hierarchy.GetMetadata<Wire>("Trailer");
            var trailerLine = hierarchy.GetMetadataLineNumber("Trailer");
            if (trailer is not null && !hierarchy.HasValidationErrors && trailer.Quantity != hierarchy.GetRecordCount("root"))
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Reconciliation, "trailer.count", "The root total does not match.", location: new(trailerLine)), cancellationToken).ConfigureAwait(false);

            if (hierarchy is ContextState state && state.Total != 0 && trailer is not null && !hierarchy.HasValidationErrors && trailer.Total != state.Total)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Reconciliation, "trailer.total", "The quantity total does not match.", location: new(trailerLine)), cancellationToken).ConfigureAwait(false);
        }
        protected override Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Observe("rejected");
            return Task.CompletedTask;
        }
        protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Observe("completed");
            return Task.CompletedTask;
        }
        private void Observe(string value)
        {
            Events.Add(value);
            ExecutionContext.TryGetCurrent(out var ambient);
            FileAmbient.Add(ambient);
        }
    }

    private sealed class Environment : IAsyncDisposable
    {
        public Environment() => Provider = new ServiceCollection().AddExecutionContext().AddScoped(sp => new Handler(this, sp.GetRequiredService<ExecutionContext>()))
            .BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        public ServiceProvider Provider { get; }
        public List<(Group Unit, ContextState Context)> Validated { get; } = [];
        public List<(Group Unit, ContextState Context, long HeaderCount, long RootCount)> Executed { get; } = [];
        public List<Handler> Handlers { get; } = [];
        public bool GroupError { get; init; }
        public bool Fail { get; init; }
        public Result Failure { get; } = Result.Fail(new BusinessException("Execution failed."));
        public ValueTask DisposeAsync() => Provider.DisposeAsync();
    }

    private sealed class Handler : FileUnitHandler<Group>, IAsyncDisposable
    {
        private readonly Environment _environment;
        public Handler(Environment environment, ExecutionContext ambient)
        {
            (_environment, Ambient) = (environment, ambient);
            environment.Handlers.Add(this);
            ambient.TenantId.Should().Be("trusted");
        }
        public ExecutionContext Ambient { get; }
        public bool Disposed { get; private set; }
        protected override async Task OnValidateAsync(FileUnitValidationArgs<Group> args, CancellationToken cancellationToken)
        {
            ExecutionContext.Current.Should().BeSameAs(Ambient);
            var state = (ContextState)args.Context;
            state.Total += args.Unit.Lines.Sum(x => x.Quantity ?? 0);
            _environment.Validated.Add((args.Unit, state));
            if (_environment.GroupError)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "group.rule", "The group rule failed."), cancellationToken).ConfigureAwait(false);
        }
        protected override Task<Result> OnExecuteAsync(FileUnitArgs<Group> args, CancellationToken cancellationToken)
        {
            ExecutionContext.Current.Should().BeSameAs(Ambient);
            var state = (ContextState)args.Context;
            _environment.Executed.Add((args.Unit, state, state.GetRecordCount("header"), state.GetRecordCount("root")));
            return Task.FromResult(_environment.Fail ? _environment.Failure : Result.Success);
        }
        public async ValueTask DisposeAsync()
        {
            await Task.Yield();
            Disposed = true;
        }
    }

    private sealed class Input(string text)
    {
        public string Text { get; } = text;
        public List<TrackedStream> Streams { get; } = [];
        public Task<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = new TrackedStream(Encoding.UTF8.GetBytes(Text));
            Streams.Add(stream);
            return Task.FromResult<Stream>(stream);
        }
    }

    private sealed class TrackedStream(byte[] bytes) : MemoryStream(bytes, false)
    {
        public bool Disposed { get; private set; }
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
