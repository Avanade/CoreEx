namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class CsvFileProcessorTests
{
    [Test]
    public async Task SinglePassExecution_PreservesSuccessfulWritesWhenLaterUnitFailsValidation()
    {
        await using var environment = new Environment { InvalidAt = 2 };
        var input = new Input("Id,Name,Quantity\n1,A,1\n2,B,2\n");
        var processor = new Processor(environment);
        var context = Context(FileProcessingMode.ValidateAndExecute, FileErrorMode.CollectErrors);

        var summary = await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.IsSuccess.Should().BeFalse();
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.RecordsRead.Should().Be(2);
        summary.Progress.UnitsRead.Should().Be(2);
        summary.Progress.UnitsValidated.Should().Be(1);
        summary.Progress.InvalidUnits.Should().Be(1);
        summary.Progress.UnitsAttempted.Should().Be(1);
        summary.Progress.UnitsExecuted.Should().Be(1);
        environment.Executed.Select(x => x.Record.Id).Should().Equal(1);
        input.Streams.Should().ContainSingle();
    }

    [Test]
    public async Task ValidFixture_ReplaysFreshModelsAndContextsInDistinctScopes()
    {
        await using var environment = new Environment();
        using var parent = new ExecutionContext { ServiceProvider = environment.Provider, TenantId = "file" };
        ExecutionContext.SetCurrent(parent);
        try
        {
            var input = new Input(File.ReadAllText(Fixture("flat-valid.csv")));
            var context = Context();
            var processor = new Processor(environment);
            var summary = await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);

            summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
            summary.Progress.RecordsRead.Should().Be(2);
            summary.Progress.UnitsValidated.Should().Be(2);
            summary.Progress.UnitsAttempted.Should().Be(2);
            summary.Progress.UnitsExecuted.Should().Be(2);
            summary.Progress.BytesRead.Should().Be(Encoding.UTF8.GetByteCount(input.Text));
            input.Streams.Should().HaveCount(2).And.OnlyContain(x => x.Disposed && x.AsyncReadCount > 0);
            environment.Validated.Select(x => x.Record.Name).Should().Equal("Example, Inc", "Two\nphysical lines");
            environment.Executed.Select(x => x.Record.Quantity).Should().Equal(2, 0);
            environment.Executed[0].Record.Should().NotBeSameAs(environment.Validated[0].Record);
            environment.Executed[0].Context.Should().NotBeSameAs(context);
            environment.Executed.Should().OnlyContain(x => x.Context.Identity == context.Identity);
            environment.Handlers.Should().HaveCount(4).And.OnlyContain(x => x.Disposed);
            environment.Handlers.Select(x => x.Context).Should().OnlyHaveUniqueItems();
            processor.Events.Should().Equal("eof", "completed");
            processor.FileAmbient.Should().OnlyContain(x => ReferenceEquals(x, parent));
            ExecutionContext.Current.Should().BeSameAs(parent);
            parent.ServiceProvider.Should().BeSameAs(environment.Provider);
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task LateMappingFailure_RejectsEveryExecutionAndSanitizesDiagnostics()
    {
        await using var environment = new Environment();
        var input = new Input(File.ReadAllText(Fixture("flat-invalid.csv")));
        var processor = new Processor(environment);
        var summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.ErrorCount.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(1);
        environment.Executed.Should().BeEmpty();
        input.Streams.Should().ContainSingle().Which.Disposed.Should().BeTrue();
        var diagnostic = processor.Diagnostics.Should().ContainSingle().Subject;
        diagnostic.Code.Should().Be("csv.field.invalid");
        diagnostic.Location.Should().NotBeNull();
        diagnostic.Location.LineNumber.Should().Be(3);
        diagnostic.Location.FieldIndex.Should().Be(2);
        diagnostic.Location.PropertyPath.Should().Be("Quantity");
        diagnostic.Message.ToString().Should().NotContain("not-an-integer");
        processor.Events.Should().Equal("diagnostic", "eof", "rejected");
    }

    [Test]
    public async Task EndOfFileError_RejectsAcceptedFlatRecordsWithoutInventingInvalidRecords()
    {
        await using var environment = new Environment();
        var processor = new Processor(environment) { Reconcile = true };
        var input = new Input("Id,Name,Quantity\n1,A,2\n2,B,3\n");
        var summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.UnitsValidated.Should().Be(2);
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.ErrorCount.Should().Be(1);
        input.Streams.Should().ContainSingle();
        environment.Executed.Should().BeEmpty();
        processor.Events.Should().Equal("eof", "diagnostic", "rejected");
    }

    [TestCase(FileErrorMode.FailFast, false, 1)]
    [TestCase(FileErrorMode.CollectErrors, true, 2)]
    public async Task ErrorPolicy_ReportsThenStopsOrContinues(FileErrorMode mode, bool completed, int errors)
    {
        var processor = new Processor();
        var input = new Input("Id,Name,Quantity\n1,A,bad\n2,B,bad\n3,C,1\n");
        var summary = await processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateOnly, mode)).ConfigureAwait(false);
        summary.ValidationCompleted.Should().Be(completed);
        summary.Progress.ErrorCount.Should().Be(errors);
        summary.Progress.InvalidRecords.Should().Be(errors);
        summary.Progress.RecordsRead.Should().Be(completed ? 3 : 1);
        processor.Events.Contains("eof").Should().Be(completed);
        input.Streams.Should().ContainSingle().Which.Disposed.Should().BeTrue();
    }

    [TestCase("Name,quantity,ID\nA,0,1\n", FileProcessingOutcome.Succeeded, 1)]
    [TestCase("Id,Name,Quantity,ID\n1,A,0,1\n", FileProcessingOutcome.ContentFailed, 0)]
    [TestCase("Id,Name\n1,A\n", FileProcessingOutcome.ContentFailed, 0)]
    [TestCase("", FileProcessingOutcome.ContentFailed, 0)]
    [TestCase("Id,Name,Quantity\n", FileProcessingOutcome.Succeeded, 0)]
    [TestCase("Id,Name,Quantity\n1,A\n", FileProcessingOutcome.ContentFailed, 1)]
    public async Task Headers_AreCaseInsensitiveAndFailureIsContentNotAnException(string csv, FileProcessingOutcome outcome, int records)
    {
        var processor = new Processor();
        var summary = await processor.ProcessAsync(new Input(csv).OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        summary.Outcome.Should().Be(outcome);
        summary.Progress.RecordsRead.Should().Be(records);
        if (outcome == FileProcessingOutcome.ContentFailed)
            processor.Diagnostics.Should().NotBeEmpty();
    }

    [Test]
    public async Task ExactHeaders_AllowReorderingButRejectUnmappedColumns()
    {
        static CsvFileMapping<Record> ExactMapping() => new CsvFileMapping<Record>(CsvHeaderPolicy.Exact)
            .MapProperty("Id", x => x.Id, CsvFieldConverters.Nullable<int>())
            .MapProperty("Name", x => x.Name, CsvFieldConverters.String)
            .MapProperty("Quantity", x => x.Quantity, CsvFieldConverters.Nullable<int>());

        var accepted = new Processor(mappingFactory: ExactMapping);
        var valid = await accepted.ProcessAsync(new Input("Name,quantity,ID\nA,0,1\n").OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        valid.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        valid.Progress.UnitsValidated.Should().Be(1);

        var rejectedProcessor = new Processor(mappingFactory: ExactMapping);
        var rejected = await rejectedProcessor.ProcessAsync(new Input("Id,Name,Quantity,Extra\n1,A,0,x\n").OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        rejected.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        rejected.Progress.UnitsValidated.Should().Be(0);
        rejectedProcessor.Diagnostics.Select(x => x.Code).Should().Contain("csv.header.unexpected");
    }

    [TestCase("1,A,0,x")]
    [TestCase("1,A")]
    public async Task ExactHeaders_RejectRowsWithDifferentFieldCountsWithoutReportingThem(string invalidRow)
    {
        static CsvFileMapping<Record> ExactMapping() => new CsvFileMapping<Record>(CsvHeaderPolicy.Exact)
            .MapProperty("Id", x => x.Id, CsvFieldConverters.Nullable<int>())
            .MapProperty("Name", x => x.Name, CsvFieldConverters.String)
            .MapProperty("Quantity", x => x.Quantity, CsvFieldConverters.Nullable<int>());

        var processor = new Processor(mappingFactory: ExactMapping);
        var input = new Input($"Id,Name,Quantity\n1,First,1\n{invalidRow}\n3,Last,3\n");
        var summary = await processor.ProcessAsync(input.OpenAsync,
            Context(FileProcessingMode.ValidateOnly, FileErrorMode.CollectErrors)).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(2);
        processor.Diagnostics.Select(x => x.Code).Should().Contain("csv.record.field_count");
        processor.Reported.Select(x => x.Record.Id).Should().Equal(1, 3);
    }

    [Test]
    public async Task ValidatedUnitHook_ObservesOnlyRowsPassingAllUnitValidation()
    {
        await using var environment = new Environment { InvalidAt = 2 };
        var processor = new Processor(environment);
        var input = new Input("Id,Name,Quantity\n1,A,1\n2,B,2\n3,C,3\n");
        var context = Context(FileProcessingMode.ValidateOnly, FileErrorMode.CollectErrors);
        var summary = await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.IsSuccess.Should().BeFalse();
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.RecordsRead.Should().Be(3);
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(2);
        summary.Progress.UnitsAttempted.Should().Be(0);
        processor.Reported.Select(x => x.Record.Id).Should().Equal(1, 3);
        processor.Reported.Select(x => x.Info.Location.LineNumber).Should().Equal(2, 4);
        processor.Reported.Select(x => x.Info.RecordType).Should().OnlyContain(x => x == null);
        environment.Executed.Should().BeEmpty();
        input.Streams.Should().ContainSingle();
        processor.Events.Should().Equal("diagnostic", "eof", "rejected");
    }

    [Test]
    public async Task ValidatedUnitHook_EofErrorRetainsCompletedValidUnitReports()
    {
        var processor = new Processor { Reconcile = true };
        var input = new Input("Id,Name,Quantity\n1,A,1\n2,B,2\n");
        var summary = await processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.InvalidRecords.Should().Be(0);
        summary.Progress.UnitsValidated.Should().Be(2);
        processor.Reported.Select(x => x.Record.Id).Should().Equal(1, 2);
        processor.Events.Should().Equal("eof", "diagnostic", "rejected");
    }

    [Test]
    public async Task ValidatedUnitHook_CompletesSuccessfullyWithoutExecuting()
    {
        var processor = new Processor();
        var summary = await processor.ProcessAsync(new Input("Id,Name,Quantity\n1,A,1\n").OpenAsync,
            Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        summary.IsSuccess.Should().BeTrue();
        summary.Progress.UnitsValidated.Should().Be(1);
        processor.Reported.Select(x => x.Record.Id).Should().Equal(1);
        processor.Events.Should().Equal("eof", "completed");
    }

    [TestCase(FileProcessingMode.ValidateOnly, 1, 0)]
    [TestCase(FileProcessingMode.ValidateThenExecute, 2, 1)]
    [TestCase(FileProcessingMode.ValidateAndExecute, 1, 1)]
    public async Task ValidatedUnitHook_IsIndependentOfProcessingMode(FileProcessingMode mode, int expectedOpens, int expectedExecutions)
    {
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var input = new Input("Id,Name,Quantity\n1,A,1\n");

        var summary = await processor.ProcessAsync(input.OpenAsync, Context(mode)).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        processor.Reported.Select(x => x.Record.Id).Should().Equal(1);
        input.Streams.Should().HaveCount(expectedOpens);
        environment.Executed.Should().HaveCount(expectedExecutions);
    }

    [TestCase(FileProcessingMode.ValidateAndExecute, false)]
    [TestCase(FileProcessingMode.ValidateAndExecute, true)]
    [TestCase(FileProcessingMode.ValidateThenExecute, false)]
    [TestCase(FileProcessingMode.ValidateThenExecute, true)]
    public async Task ExecutionProgress_IsPublishedToBothContextsAndSnapshotsRemainImmutable(FileProcessingMode mode, bool fail)
    {
        await using var environment = new Environment { FailAt = fail ? 2 : null };
        var context = Context(mode);
        var initial = context.Progress;
        var processor = new Processor(environment);

        var summary = await processor.ProcessAsync(new Input("Id,Name,Quantity\n1,A,1\n2,B,2\n").OpenAsync, context).ConfigureAwait(false);

        summary.Outcome.Should().Be(fail ? FileProcessingOutcome.ExecutionFailed : FileProcessingOutcome.Succeeded);
        context.Progress.UnitsAttempted.Should().Be(2);
        context.Progress.UnitsExecuted.Should().Be(fail ? 1 : 2);
        environment.Executed.Should().HaveCount(2);
        environment.Executed.Should().OnlyContain(x => ReferenceEquals(x.Context.Progress, context.Progress));
        initial.UnitsRead.Should().Be(0);
        initial.UnitsAttempted.Should().Be(0);
        initial.UnitsExecuted.Should().Be(0);
    }

    [Test]
    public async Task ValidatedUnitHook_FailFastRetainsEarlierReports()
    {
        await using var environment = new Environment { InvalidAt = 2 };
        var processor = new Processor(environment);
        var context = Context(FileProcessingMode.ValidateOnly, FileErrorMode.FailFast);
        var summary = await processor.ProcessAsync(new Input("Id,Name,Quantity\n1,A,1\n2,B,2\n3,C,3\n").OpenAsync, context).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeFalse();
        summary.Progress.RecordsRead.Should().Be(2);
        summary.Progress.UnitsValidated.Should().Be(1);
        summary.Progress.InvalidRecords.Should().Be(1);
        processor.Reported.Select(x => x.Record.Id).Should().Equal(1);
        environment.Executed.Should().BeEmpty();
    }

    [Test]
    public async Task ValidatedUnitHook_PropagatesReportFault()
    {
        var failure = new IOException("Report write failed.");
        var processor = new Processor
        {
            ValidatedCallback = (_, _, _) => Task.FromException(failure)
        };
        var context = Context(FileProcessingMode.ValidateOnly);
        Func<Task> run = () => processor.ProcessAsync(new Input("Id,Name,Quantity\n1,A,1\n").OpenAsync, context);

        (await run.Should().ThrowAsync<IOException>().ConfigureAwait(false)).Which.Should().BeSameAs(failure);
        context.Progress.UnitsValidated.Should().Be(1);
        processor.Reported.Should().BeEmpty();
        processor.Events.Should().BeEmpty();
    }

    [Test]
    public async Task PositionalMapping_DoesNotDiscardFirstRecordAndSkipsComments()
    {
        var options = new CsvFileOptions { Comment = '#' };
        var processor = new Processor(positional: true, options: options);
        var summary = await processor.ProcessAsync(new Input("#comment\n1,A,2\n\n2,B,0\n").OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        summary.Progress.RecordsRead.Should().Be(2);
        summary.Progress.UnitsValidated.Should().Be(2);
    }

    [Test]
    public async Task CoreExValidator_UsesMessagesAndRequirednessWithoutPerRecordExceptions()
    {
        var processor = new Processor(validator: new RecordValidator());
        var summary = await processor.ProcessAsync(new Input("Id,Name,Quantity\n1,A,0\n2,,1\n3,B,\n").OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.UnitsValidated.Should().Be(1);
        summary.Progress.InvalidRecords.Should().Be(2);
        processor.Diagnostics.Select(x => x.Category).Should().OnlyContain(x => x == FileDiagnosticCategory.Validation);
        processor.Diagnostics.Select(x => x.Location?.LineNumber).Should().Equal(3, 4);
    }

    [Test]
    public async Task ScopedValidation_DiagnosticsUseFileAmbientAndAreAwaitedBeforeNextEmission()
    {
        await using var environment = new Environment { ReportWarnings = true };
        using var parent = new ExecutionContext { ServiceProvider = environment.Provider };
        ExecutionContext.SetCurrent(parent);
        try
        {
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var processor = new Processor(environment) { DiagnosticCallback = async (_, ct) =>
            {
                ExecutionContext.Current.Should().BeSameAs(parent);
                entered.TrySetResult();
                await release.Task.WaitAsync(ct).ConfigureAwait(false);
                ExecutionContext.Current.Should().BeSameAs(parent);
            } };
            var task = processor.ProcessAsync(new Input("Id,Name,Quantity\n1,A,1\n2,B,2\n").OpenAsync, Context(FileProcessingMode.ValidateOnly));
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            environment.Validated.Should().ContainSingle();
            task.IsCompleted.Should().BeFalse();
            processor.Diagnostics.Should().ContainSingle();
            release.SetResult();
            var summary = await task.ConfigureAwait(false);
            summary.Progress.WarningCount.Should().Be(2);
            summary.IsSuccess.Should().BeTrue();
            processor.FileAmbient.Should().OnlyContain(x => ReferenceEquals(x, parent));
        }
        finally
        {
            ExecutionContext.Reset();
        }
    }

    [Test]
    public async Task ExecutionResultFailure_PreservesFailureAndStopsWithoutTerminalReport()
    {
        await using var environment = new Environment { FailAt = 2 };
        var processor = new Processor(environment);
        var input = new Input("Id,Name,Quantity\n1,A,1\n2,B,1\n3,C,1\n");
        var summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);

        summary.Outcome.Should().Be(FileProcessingOutcome.ExecutionFailed);
        summary.ExecutionResult.Error.Should().BeSameAs(environment.Failure.Error);
        summary.Progress.UnitsAttempted.Should().Be(2);
        summary.Progress.UnitsExecuted.Should().Be(1);
        environment.Executed.Select(x => x.Record.Id).Should().Equal(1, 2);
        processor.Events.Should().Equal("eof");
        input.Streams.Should().HaveCount(2).And.OnlyContain(x => x.Disposed);
    }

    [TestCase("diagnostic")]
    [TestCase("completed")]
    [TestCase("rejected")]
    [TestCase("eof")]
    public async Task ReportingFaults_PropagateWithoutFallbackTerminalHook(string hook)
    {
        var error = new IOException("Report write failed.");
        var processor = new Processor { FaultHook = hook, Fault = error };
        var input = new Input(hook is "diagnostic" or "rejected" ? "Id,Name,Quantity\n1,A,bad\n" : "Id,Name,Quantity\n1,A,1\n");
        Func<Task> run = () => processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateOnly));
        var assertion = await run.Should().ThrowAsync<IOException>().ConfigureAwait(false);
        assertion.Which.Should().BeSameAs(error);
        input.Streams.Should().OnlyContain(x => x.Disposed);
        processor.Events.Last().Should().Be(hook);
    }

    [Test]
    public async Task TenThousandInvalidRecords_StreamOrderedDiagnosticsWithoutRetainingThemInFrameworkState()
    {
        var count = 0;
        var processor = new Processor() { RetainDiagnostics = false, DiagnosticCallback = (diagnostic, _) =>
        {
            count++;
            diagnostic.Location?.LineNumber.Should().Be(count + 1);
            return Task.CompletedTask;
        } };
        var csv = "Id,Name,Quantity\n" + string.Concat(Enumerable.Range(1, 10_000).Select(x => $"{x},A,invalid\n"));
        var context = Context(FileProcessingMode.ValidateOnly);
        var summary = await processor.ProcessAsync(new Input(csv).OpenAsync, context).ConfigureAwait(false);
        count.Should().Be(10_000);
        processor.Diagnostics.Should().BeEmpty();
        summary.Progress.ErrorCount.Should().Be(10_000);
        summary.Progress.InvalidRecords.Should().Be(10_000);
        summary.Progress.RecordsRead.Should().Be(10_000);
        summary.ValidationCompleted.Should().BeTrue();
    }

    [TestCase("\n")]
    [TestCase("\r\n")]
    [TestCase("\r")]
    public async Task QuotedFields_KeepExactTextAndPhysicalLocationsAcrossSingleByteReads(string newline)
    {
        await using var environment = new Environment { ReportWarnings = true };
        var processor = new Processor(environment, options: new() { Comment = '#' });
        var input = new Input($"Id,Name,Quantity{newline}{newline}#comment{newline}1,\"Andr\u00e9, \"\"A\"\"{newline}\ud83d\ude00\",\"2\"{newline}2,B,0")
        {
            ConfigureStream = stream => stream.ChunkSize = 1
        };
        var summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        environment.Executed.Select(x => x.Record.Name).Should().Equal($"Andr\u00e9, \"A\"{newline}\ud83d\ude00", "B");
        processor.Diagnostics.Select(x => x.Location?.LineNumber).Should().Equal(4, 6);
        input.Streams.Should().OnlyContain(x => x.AsyncDisposed && x.AsyncReadCount > 0);
    }

    [TestCase("1,Al\"ice,2\n2,B,3\n", 2, true)]
    [TestCase("1,\"Alice\"x,2\n2,B,3\n", 2, true)]
    [TestCase("1,\"Alice,2", 1, true)]
    [TestCase("1,\"Alice,2\n2,B,3\n", 1, true)]
    public async Task MalformedRecords_AreDiagnosticsAndNeverValidatedOrExecuted(string records, int recordCount, bool completed)
    {
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var input = new Input("Id,Name,Quantity\n" + records);
        var summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().Be(completed);
        summary.Progress.RecordsRead.Should().Be(recordCount);
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.UnitsValidated.Should().Be(recordCount - 1);
        processor.Diagnostics.Should().ContainSingle().Which.Code.Should().Be("csv.record.invalid");
        processor.Diagnostics[0].Location?.LineNumber.Should().Be(2);
        environment.Validated.Should().HaveCount(recordCount - 1);
        environment.Executed.Should().BeEmpty();
        input.Streams.Should().ContainSingle().Which.AsyncDisposed.Should().BeTrue();
    }

    [Test]
    public async Task MalformedHeader_StopsBeforeMappingOrDataValidation()
    {
        var processor = new Processor();
        var summary = await processor.ProcessAsync(new Input("Id,Na\"me,Quantity\n1,A,2\n").OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        summary.ValidationCompleted.Should().BeFalse();
        summary.Progress.RecordsRead.Should().Be(0);
        summary.Progress.ErrorCount.Should().Be(1);
        processor.Diagnostics.Should().ContainSingle().Which.Category.Should().Be(FileDiagnosticCategory.Parsing);
        processor.Events.Should().Equal("diagnostic", "rejected");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task TenThousandBadRecords_DoNotUseExceptionControlFlow(bool malformed)
    {
        var exceptions = 0;
        void Observe(object? sender, FirstChanceExceptionEventArgs args) => Interlocked.Increment(ref exceptions);

        var processor = new Processor() { RetainDiagnostics = false };
        var input = new Input("Id,Name,Quantity\n" + string.Concat(Enumerable.Repeat(malformed ? "1,Al\"ice,2\n" : "1,A,invalid\n", 10_000)));
        AppDomain.CurrentDomain.FirstChanceException += Observe;
        FileProcessingSummary summary;
        try
        {
            summary = await processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= Observe;
        }

        exceptions.Should().Be(0);
        summary.Progress.InvalidRecords.Should().Be(10_000);
        summary.Progress.ErrorCount.Should().Be(10_000);
    }

    [Test]
    public async Task ConverterProgrammingFault_PropagatesOriginalInstanceAndDisposesInput()
    {
        var fault = new InvalidOperationException("Broken custom conversion.");
        var processor = new Processor(mappingFactory: () => new CsvFileMapping<Record>().MapProperty<int?>("Id", x => x.Id,
            (ReadOnlySpan<char> _, out int? value) => throw fault));
        var input = new Input("Id\n1\n");
        Func<Task> run = () => processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateOnly));
        (await run.Should().ThrowAsync<InvalidOperationException>().ConfigureAwait(false)).Which.Should().BeSameAs(fault);
        input.Streams.Should().ContainSingle().Which.AsyncDisposed.Should().BeTrue();
        processor.Events.Should().BeEmpty();
    }

    [Test]
    public async Task MultipleInvalidFields_CountOneInvalidRecordAndPreserveIndividualLocations()
    {
        var processor = new Processor();
        var summary = await processor.ProcessAsync(new Input("Id,Name,Quantity\nbad,A,bad\n").OpenAsync, Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(1);
        summary.Progress.ErrorCount.Should().Be(2);
        processor.Diagnostics.Select(x => x.Location?.FieldIndex).Should().Equal(0, 2);
        processor.Diagnostics.Select(x => x.Location?.FieldName).Should().Equal("Id", "Quantity");
    }

    [TestCase(FileErrorMode.FailFast, 1, false)]
    [TestCase(FileErrorMode.CollectErrors, 2, true)]
    public async Task ScopedValidationErrors_RespectRecordPolicyAndKeepParentAlive(FileErrorMode mode, int records, bool completed)
    {
        await using var environment = new Environment { ReportErrors = true };
        var processor = new Processor(environment);
        var summary = await processor.ProcessAsync(new Input("Id,Name,Quantity\n1,A,1\n2,B,2\n").OpenAsync,
            Context(FileProcessingMode.ValidateThenExecute, mode)).ConfigureAwait(false);
        summary.Progress.InvalidRecords.Should().Be(records);
        summary.Progress.ErrorCount.Should().Be(records * 2);
        summary.ValidationCompleted.Should().Be(completed);
        summary.Progress.UnitsValidated.Should().Be(0);
        environment.Executed.Should().BeEmpty();
        environment.Handlers.Should().HaveCount(records).And.OnlyContain(x => x.Disposed);
    }

    [Test]
    public async Task InvalidStreamFactories_FailExplicitlyWithoutTerminalReports()
    {
        var processor = new Processor();
        Func<Task> nullStream = () => processor.ProcessAsync(_ => Task.FromResult<Stream>(null!), Context(FileProcessingMode.ValidateOnly));
        await nullStream.Should().ThrowAsync<InvalidOperationException>().WithMessage("*returned null*").ConfigureAwait(false);
        await using var closed = new TrackedStream([]);
        closed.Dispose();
        Func<Task> unreadable = () => processor.ProcessAsync(_ => Task.FromResult<Stream>(closed), Context(FileProcessingMode.ValidateOnly));
        await unreadable.Should().ThrowAsync<InvalidOperationException>().WithMessage("*readable stream*").ConfigureAwait(false);
        closed.AsyncDisposed.Should().BeTrue();
        processor.Events.Should().BeEmpty();
    }

    [Test]
    public async Task ExecutionWithoutHandler_FailsBeforeOpeningInput()
    {
        var processor = new Processor();
        var input = new Input("Id,Name,Quantity\n1,A,1\n");
        Func<Task> run = () => processor.ProcessAsync(input.OpenAsync, Context());
        await run.Should().ThrowAsync<InvalidOperationException>().WithMessage("*handler factory*").ConfigureAwait(false);
        input.Streams.Should().BeEmpty();
    }

    [Test]
    public async Task ReusingValidationContextForExecution_FailsBeforeOpeningReplay()
    {
        await using var environment = new Environment();
        var processor = new Processor(environment) { ReuseContext = true };
        var input = new Input("Id,Name,Quantity\n1,A,1\n");
        Func<Task> run = () => processor.ProcessAsync(input.OpenAsync, Context());
        await run.Should().ThrowAsync<InvalidOperationException>().WithMessage("*fresh context*").ConfigureAwait(false);
        input.Streams.Should().ContainSingle();
        environment.Executed.Should().BeEmpty();
        processor.Events.Should().Equal("eof");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ReadFaults_PropagateAndDisposeOwnedStream(bool replay)
    {
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var fault = new IOException("Read failed.");
        var opened = 0;
        var input = new Input("Id,Name,Quantity\n1,A,1\n")
        {
            ConfigureStream = stream => stream.ReadFault = ++opened == (replay ? 2 : 1) ? fault : null
        };
        var context = Context();
        Func<Task> run = () => processor.ProcessAsync(input.OpenAsync, context);
        (await run.Should().ThrowAsync<IOException>().ConfigureAwait(false)).Which.Should().BeSameAs(fault);
        input.Streams.Should().HaveCount(replay ? 2 : 1).And.OnlyContain(x => x.AsyncDisposed);
        processor.Events.Should().Equal(replay ? ["eof"] : []);
        context.Progress.UnitsExecuted.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OpenFaults_PropagateWithoutTerminalReport(bool replay)
    {
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var fault = new IOException("Revision unavailable.");
        var opened = 0;
        var input = new Input("Id,Name,Quantity\n1,A,1\n");
        Task<Stream> OpenAsync(CancellationToken cancellationToken) => ++opened == (replay ? 2 : 1)
            ? Task.FromException<Stream>(fault) : input.OpenAsync(cancellationToken);
        Func<Task> run = () => processor.ProcessAsync(OpenAsync, Context());
        (await run.Should().ThrowAsync<IOException>().ConfigureAwait(false)).Which.Should().BeSameAs(fault);
        input.Streams.Should().OnlyContain(x => x.AsyncDisposed);
        processor.Events.Should().Equal(replay ? ["eof"] : []);
    }

    [Test]
    public async Task ExecutionFault_PropagatesAndDisposesUnitScopeAndReplay()
    {
        var fault = new IOException("Business operation failed.");
        await using var environment = new Environment { ExecutionFault = fault };
        var processor = new Processor(environment);
        var input = new Input("Id,Name,Quantity\n1,A,1\n");
        var context = Context();
        Func<Task> run = () => processor.ProcessAsync(input.OpenAsync, context);
        (await run.Should().ThrowAsync<IOException>().ConfigureAwait(false)).Which.Should().BeSameAs(fault);
        context.Progress.UnitsAttempted.Should().Be(1);
        context.Progress.UnitsExecuted.Should().Be(0);
        environment.Handlers.Should().OnlyContain(x => x.Disposed);
        input.Streams.Should().HaveCount(2).And.OnlyContain(x => x.AsyncDisposed);
        processor.Events.Should().Equal("eof");
    }

    [Test]
    public async Task CancellationBeforeOpening_DoesNotOpenOrReport()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var processor = new Processor();
        var input = new Input("Id,Name,Quantity\n1,A,1\n");
        Func<Task> run = () => processor.ProcessAsync(input.OpenAsync, Context(FileProcessingMode.ValidateOnly), cancellation.Token);
        await run.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        input.Streams.Should().BeEmpty();
        processor.Events.Should().BeEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CancellationDuringBlockedRead_DisposesInputWithoutTerminalReport(bool replay)
    {
        using var cancellation = new CancellationTokenSource();
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var blocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var opened = 0;
        var input = new Input("Id,Name,Quantity\n1,A,1\n")
        {
            ConfigureStream = stream => stream.ReadBlocked = ++opened == (replay ? 2 : 1) ? blocked : null
        };
        var task = processor.ProcessAsync(input.OpenAsync, Context(), cancellation.Token);
        await blocked.Task.WaitAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
        cancellation.Cancel();
        Func<Task> run = () => task;
        await run.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        input.Streams.Should().HaveCount(replay ? 2 : 1).And.OnlyContain(x => x.AsyncDisposed);
        processor.Events.Should().Equal(replay ? ["eof"] : []);
    }

    [Test]
    public async Task CancellationDuringDiagnostic_DisposesValidationScopeAndInput()
    {
        using var cancellation = new CancellationTokenSource();
        await using var environment = new Environment { ReportWarnings = true };
        var processor = new Processor(environment) { DiagnosticCallback = (_, ct) =>
        {
            cancellation.Cancel();
            ct.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        } };
        var input = new Input("Id,Name,Quantity\n1,A,1\n");
        Func<Task> run = () => processor.ProcessAsync(input.OpenAsync, Context(), cancellation.Token);
        await run.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        environment.Handlers.Should().ContainSingle().Which.Disposed.Should().BeTrue();
        input.Streams.Should().ContainSingle().Which.AsyncDisposed.Should().BeTrue();
        processor.Events.Should().Equal("diagnostic");
    }

    [TestCase("Id,Name,Quantity\n")]
    [TestCase("Id,Name,Quantity\n1,A,1\n2,B,2\n")]
    [TestCase("Id,Name,Quantity\n1,A,bad\n")]
    public async Task ReplayDrift_IsAnOperationalFaultNotContentRejection(string replay)
    {
        await using var environment = new Environment();
        var processor = new Processor(environment);
        var accepted = new Input("Id,Name,Quantity\n1,A,1\n");
        var changed = new Input(replay);
        var opened = false;
        Task<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            var input = opened ? changed : accepted;
            opened = true;
            return input.OpenAsync(cancellationToken);
        }

        Func<Task> run = () => processor.ProcessAsync(OpenAsync, Context());
        await run.Should().ThrowAsync<InvalidDataException>().ConfigureAwait(false);
        accepted.Streams.Concat(changed.Streams).Should().OnlyContain(x => x.AsyncDisposed);
        processor.Events.Should().Equal("eof");
    }

    [Test]
    public async Task InvalidUtf8_IsNotSilentlyReplaced()
    {
        byte[] bytes = [.. Encoding.UTF8.GetBytes("Id,Name,Quantity\n1,"), 0xC3, 0x28, .. Encoding.UTF8.GetBytes(",2\n")];
        await using var stream = new TrackedStream(bytes) { ChunkSize = 1 };
        var processor = new Processor();
        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(stream), Context(FileProcessingMode.ValidateOnly)).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeFalse();
        processor.Diagnostics.Should().ContainSingle().Which.Code.Should().Be("csv.encoding.invalid");
        stream.AsyncDisposed.Should().BeTrue();
        processor.Events.Should().Equal("diagnostic", "rejected");
    }

    [Test]
    public async Task ConcurrentInvocations_KeepMappingsContextsAndCountersIsolated()
    {
        var processor = new CsvFileProcessor<Record>(() => Mapping());
        var first = new Input("Id,Name,Quantity\n1,A,1\n");
        var second = new Input("Name,Quantity,Id\nB,2,2\nC,3,3\n");
        var firstContext = Context(FileProcessingMode.ValidateOnly);
        var secondContext = Context(FileProcessingMode.ValidateOnly);
        var summaries = await Task.WhenAll(processor.ProcessAsync(first.OpenAsync, firstContext), processor.ProcessAsync(second.OpenAsync, secondContext)).ConfigureAwait(false);
        summaries.Select(x => x.Progress.RecordsRead).Should().Equal(1, 2);
        summaries.Should().OnlyContain(x => x.Outcome == FileProcessingOutcome.Succeeded);
        firstContext.Progress.RecordsRead.Should().Be(1);
        secondContext.Progress.RecordsRead.Should().Be(2);
    }

    [Test]
    public void Converters_UseInvariantParsingAndKeepBlankDistinctFromInvalid()
    {
        var integer = CsvFieldConverters.Nullable<int>();
        integer("", out var blank).Should().BeTrue();
        blank.Should().BeNull();
        integer("bad", out _).Should().BeFalse();
        integer("2147483648", out _).Should().BeFalse();
        integer("0", out var zero).Should().BeTrue();
        zero.Should().Be(0);
        CsvFieldConverters.Nullable<bool>()("false", out var boolean).Should().BeTrue();
        boolean.Should().BeFalse();
        CsvFieldConverters.Nullable<decimal>()("123.45", out var number).Should().BeTrue();
        number.Should().Be(123.45m);
        CsvFieldConverters.String("  ", out var empty).Should().BeTrue();
        empty.Should().BeNull();
        CsvFieldConverters.String(" A ", out var text).Should().BeTrue();
        text.Should().Be(" A ");
        var date = CsvFieldConverters.DateOnly("yyyyMMdd");
        date("20260228", out var valid).Should().BeTrue();
        valid.Should().Be(new DateOnly(2026, 2, 28));
        date("20260229", out _).Should().BeFalse();
        date("  ", out var nullDate).Should().BeTrue();
        nullDate.Should().BeNull();
    }

    private static string Fixture(string name) => Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", name);
    private static FileProcessingContext Context(FileProcessingMode mode = FileProcessingMode.ValidateThenExecute, FileErrorMode errors = FileErrorMode.CollectErrors)
        => new(new("flat", "1", "store/data.csv", "attempt", "revision"), mode, errors);

    private static CsvFileMapping<Record> Mapping(bool positional = false) => new CsvFileMapping<Record>()
        .MapProperty("Id", x => x.Id, CsvFieldConverters.Nullable<int>(), positional ? 0 : null)
        .MapProperty("Name", x => x.Name, CsvFieldConverters.String, positional ? 1 : null)
        .MapProperty("Quantity", x => x.Quantity, CsvFieldConverters.Nullable<int>(), positional ? 2 : null);

    public sealed class Record
    {
        public int? Id { get; set; }
        public string? Name { get; set; }
        public int? Quantity { get; set; }
    }

    private sealed class RecordValidator : Validator<Record>
    {
        public RecordValidator()
        {
            Property(x => x.Name).Mandatory();
            Property(x => x.Quantity).NotNull();
        }
    }

    private sealed class Processor : CsvFileProcessor<Record>
    {
        public Processor(Environment? environment = null, bool positional = false, CsvFileOptions? options = null, IValidator<Record>? validator = null, Func<CsvFileMapping<Record>>? mappingFactory = null)
            : base(mappingFactory ?? (() => Mapping(positional)), options, validator, environment?.Provider, environment is null ? null : sp => sp.GetRequiredService<Handler>()) { }

        public List<FileDiagnostic> Diagnostics { get; } = [];
        public List<(Record Record, FileUnitInfo Info)> Reported { get; } = [];
        public List<string> Events { get; } = [];
        public List<ExecutionContext?> FileAmbient { get; } = [];
        public bool Reconcile { get; init; }
        public bool ReuseContext { get; init; }
        public bool RetainDiagnostics { get; init; } = true;
        public string? FaultHook { get; init; }
        public Exception Fault { get; init; } = new IOException("Report failed.");
        public Func<FileDiagnostic, CancellationToken, Task>? DiagnosticCallback { get; init; }
        public Func<Record, FileUnitInfo, CancellationToken, Task>? ValidatedCallback { get; init; }

        protected override void OnConfigureUnitContext(FileUnitContextArgs<Record> args)
            => args.UnitContext.TenantId = "trusted-file-tenant";

        protected override FileProcessingContext OnCreateExecutionContext(FileProcessingContext validationContext)
            => ReuseContext ? validationContext : base.OnCreateExecutionContext(validationContext);

        protected override async Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Observe("diagnostic");
            args.Context.Progress.ErrorCount.Should().BeGreaterThanOrEqualTo(args.Diagnostic.IsError ? 1 : 0);
            if (RetainDiagnostics)
                Diagnostics.Add(args.Diagnostic);
            if (DiagnosticCallback is not null)
                await DiagnosticCallback(args.Diagnostic, cancellationToken).ConfigureAwait(false);
        }

        protected override async Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
        {
            Observe("eof");
            if (Reconcile)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Reconciliation, "total", "Totals do not match."), cancellationToken).ConfigureAwait(false);
        }

        protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Observe("completed");
            return Task.CompletedTask;
        }

        protected override async Task OnValidatedUnitAsync(FileUnitArgs<Record> args, CancellationToken cancellationToken)
        {
            args.Context.Progress.UnitsValidated.Should().Be(Reported.Count + 1);
            args.Context.Progress.UnitsAttempted.Should().Be(args.Context.Mode == FileProcessingMode.ValidateAndExecute ? Reported.Count : 0);
            if (ValidatedCallback is not null)
                await ValidatedCallback(args.Unit, args.Info, cancellationToken).ConfigureAwait(false);

            Reported.Add((args.Unit, args.Info));
        }

        protected override Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Observe("rejected");
            return Task.CompletedTask;
        }

        private void Observe(string hook)
        {
            ExecutionContext.TryGetCurrent(out var current);
            FileAmbient.Add(current);
            Events.Add(hook);
            if (FaultHook == hook)
                throw Fault;
        }
    }

    private sealed class Environment : IAsyncDisposable
    {
        public Environment()
        {
            var services = new ServiceCollection().AddExecutionContext();
            services.AddScoped(sp => new Handler(this, sp.GetRequiredService<ExecutionContext>()));
            Provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        }

        public ServiceProvider Provider { get; }
        public List<(Record Record, FileProcessingContext Context)> Validated { get; } = [];
        public List<(Record Record, FileProcessingContext Context)> Executed { get; } = [];
        public List<Handler> Handlers { get; } = [];
        public bool ReportWarnings { get; init; }
        public bool ReportErrors { get; init; }
        public int? InvalidAt { get; init; }
        public int? FailAt { get; init; }
        public Exception? ExecutionFault { get; init; }
        public Result Failure { get; } = Result.Fail(new BusinessException("Unit failed."));
        public ValueTask DisposeAsync() => Provider.DisposeAsync();
    }

    private sealed class Handler : FileUnitHandler<Record>, IAsyncDisposable
    {
        private readonly Environment _environment;
        public Handler(Environment environment, ExecutionContext context)
        {
            _environment = environment;
            Context = context;
            environment.Handlers.Add(this);
            context.TenantId.Should().StartWith("trusted-");
        }

        public ExecutionContext Context { get; }
        public bool Disposed { get; private set; }

        protected override async Task OnValidateAsync(FileUnitValidationArgs<Record> args, CancellationToken cancellationToken)
        {
            ExecutionContext.Current.Should().BeSameAs(Context);
            _environment.Validated.Add((args.Unit, args.Context));
            if (_environment.ReportWarnings)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "warning", "A warning.", MessageType.Warning), cancellationToken).ConfigureAwait(false);

            if (_environment.ReportErrors)
            {
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "rule.one", "First business rule failed."), cancellationToken).ConfigureAwait(false);
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "rule.two", "Second business rule failed."), cancellationToken).ConfigureAwait(false);
            }

            if (_environment.InvalidAt.HasValue && args.Unit.Id == _environment.InvalidAt)
                await args.ReportDiagnosticAsync(new(FileDiagnosticCategory.Validation, "cross-field.invalid", "The fields are inconsistent."), cancellationToken).ConfigureAwait(false);

            ExecutionContext.Current.Should().BeSameAs(Context);
        }

        protected override Task<Result> OnExecuteAsync(FileUnitArgs<Record> args, CancellationToken cancellationToken)
        {
            ExecutionContext.Current.Should().BeSameAs(Context);
            args.Context.Progress.Should().BeSameAs(_environment.Validated[0].Context.Progress);
            args.Context.Progress.UnitsAttempted.Should().Be(_environment.Executed.Count + 1);
            args.Context.Progress.UnitsExecuted.Should().Be(_environment.Executed.Count);
            _environment.Executed.Add((args.Unit, args.Context));
            if (_environment.ExecutionFault is not null)
                throw _environment.ExecutionFault;

            return Task.FromResult(args.Unit.Id == _environment.FailAt ? _environment.Failure : Result.Success);
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
        public Action<TrackedStream>? ConfigureStream { get; init; }
        public Task<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = new TrackedStream(Encoding.UTF8.GetBytes(Text));
            ConfigureStream?.Invoke(stream);
            Streams.Add(stream);
            return Task.FromResult<Stream>(stream);
        }
    }

    private sealed class TrackedStream(byte[] bytes) : Stream
    {
        private int _position;
        public int ChunkSize { get; set; } = int.MaxValue;
        public Exception? ReadFault { get; set; }
        public TaskCompletionSource? ReadBlocked { get; set; }
        public bool Disposed { get; private set; }
        public bool AsyncDisposed { get; private set; }
        public int AsyncReadCount { get; private set; }
        public override bool CanRead => !Disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous I/O was used.");
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            AsyncReadCount++;
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
            if (ReadFault is not null)
                throw ReadFault;

            if (ReadBlocked is not null)
            {
                ReadBlocked.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }

            var count = Math.Min(Math.Min(buffer.Length, ChunkSize), bytes.Length - _position);
            bytes.AsMemory(_position, count).CopyTo(buffer);
            _position += count;
            return count;
        }
        public override ValueTask DisposeAsync()
        {
            AsyncDisposed = true;
            Dispose();
            return ValueTask.CompletedTask;
        }
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
