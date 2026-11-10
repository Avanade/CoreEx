namespace CoreEx.IO.Test.Unit;

[TestFixture]
public class CsvBoundsAndEncodingTests
{
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public async Task TinyInputBudgets_ApplyDuringPrefixReading(int size)
    {
        var exact = new Flat(new FileProcessingOptions { MaxInputBytes = size });
        var summary = await exact.ProcessAsync(new Input(Encoding.UTF8.GetBytes(new string('x', size)), 1).OpenAsync, Context()).ConfigureAwait(false);
        summary.IsSuccess.Should().BeTrue();
        summary.Progress.UnitsValidated.Should().Be(1);
        var excess = new Flat(new FileProcessingOptions { MaxInputBytes = size });
        var rejected = await excess.ProcessAsync(new Input(Encoding.UTF8.GetBytes(new string('x', size + 1)), 1).OpenAsync, Context()).ConfigureAwait(false);
        rejected.ValidationCompleted.Should().BeFalse();
        rejected.Progress.BytesRead.Should().Be(size + 1);
        excess.Diagnostics.Single().Code.Should().Be("file.limit.input-bytes");
    }

    [Test]
    public async Task PartialUtf8Bom_IsInvalidEncodingNotDiscardedInput()
    {
        foreach (var bytes in new byte[][] { [0xef], [0xef, 0xbb] })
        {
            var processor = new Flat();
            var summary = await processor.ProcessAsync(new Input(bytes, 1).OpenAsync, Context()).ConfigureAwait(false);
            summary.ValidationCompleted.Should().BeFalse();
            processor.Diagnostics.Single().Code.Should().Be("csv.encoding.invalid");
        }
    }

    [Test]
    public async Task Utf8Bom_DoesNotPolluteNamedColumnHeadersOrHierarchyDiscriminators()
    {
        var flat = new CsvFileProcessor<Row>(() => new CsvFileMapping<Row>().MapProperty("Value", x => x.Value, CsvFieldConverters.String));
        var flatSummary = await flat.ProcessAsync(new Input([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes("Value\none\n")], 1).OpenAsync, Context()).ConfigureAwait(false);
        flatSummary.IsSuccess.Should().BeTrue();
        flatSummary.Progress.RecordsRead.Should().Be(1);
        var hierarchy = new Hierarchy(new());
        var grouped = await hierarchy.ProcessAsync(new Input([0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes("O,one\nL,line\n")], 1).OpenAsync, HierarchyContext()).ConfigureAwait(false);
        grouped.IsSuccess.Should().BeTrue();
        grouped.Progress.UnitsValidated.Should().Be(1);
    }

    [Test]
    public void RuntimeAndParserAssemblies_AreStrongNamed()
    {
        typeof(CsvFileProcessor<>).Assembly.GetName().GetPublicKeyToken().Should().HaveCount(8);
        typeof(CsvHelper.CsvReader).Assembly.GetName().GetPublicKeyToken().Should().HaveCount(8);
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(4)]
    [TestCase(4096)]
    public async Task InputLimit_AcceptsExactSizeAndRejectsOneExtraByteWithoutEofOrReplay(int chunk)
    {
        var bytes = Encoding.UTF8.GetBytes("one\nsecond");
        foreach (var limit in new[] { bytes.LongLength - 1, bytes.LongLength, bytes.LongLength + 1 })
        {
            var input = new Input(bytes, chunk);
            var processor = new Flat(new FileProcessingOptions { MaxInputBytes = limit });
            var summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);
            if (limit < bytes.LongLength)
            {
                summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
                summary.ValidationCompleted.Should().BeFalse();
                processor.Diagnostics.Should().ContainSingle().Which.Code.Should().Be("file.limit.input-bytes");
                processor.Events.Should().Equal("rejected");
            }
            else
            {
                summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
                summary.Progress.UnitsValidated.Should().Be(2);
                processor.Events.Should().Equal("eof", "completed");
            }

            summary.Progress.BytesRead.Should().Be(bytes.LongLength);
            input.Streams.Should().ContainSingle().Which.Disposed.Should().BeTrue();
            input.Streams[0].BytesRead.Should().BeLessThanOrEqualTo(limit + 1);
        }
    }

    [TestCase("one\nsecond\n")]
    [TestCase("\"unclosed quote\nmany physical lines\n")]
    [TestCase("#comment longer than the budget\n")]
    public async Task InputOverflow_IsNeverSuccessfulTruncationEvenInSkippedOrUnclosedInput(string csv)
    {
        var processor = new Flat(new FileProcessingOptions { MaxInputBytes = 5 }, options: new() { Comment = '#' });
        var summary = await processor.ProcessAsync(new Input(Encoding.UTF8.GetBytes(csv)).OpenAsync, Context()).ConfigureAwait(false);
        summary.ValidationCompleted.Should().BeFalse();
        summary.Progress.ErrorCount.Should().Be(1);
        summary.Progress.InvalidRecords.Should().Be(0);
        processor.Diagnostics.Single().Category.Should().Be(FileDiagnosticCategory.Limit);
        processor.Events.Should().Equal("rejected");
    }

    [Test]
    public async Task ByteLimit_IsEnforcedBeforeDecodingExcessBytesAndBeforeAnyExecution()
    {
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var handler = new Handler();
        var processor = new Flat(new FileProcessingOptions { MaxInputBytes = 4 }, provider, handler);
        var input = new Input([.. Encoding.UTF8.GetBytes("one\n"), 0xff], 1);
        var summary = await processor.ProcessAsync(input.OpenAsync, Context(execute: true)).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        processor.Diagnostics.Single().Code.Should().Be("file.limit.input-bytes");
        handler.Executed.Should().Be(0);
        input.Streams.Should().ContainSingle();
    }

    [TestCase(1)]
    [TestCase(3)]
    [TestCase(5)]
    public async Task GroupLimit_CountsRootAndDescendantsAndResetsBetweenRoots(int maximum)
    {
        var csv = "O,first\n" + string.Concat(Enumerable.Repeat("L,line\n", maximum - 1))
            + "O,second\n" + string.Concat(Enumerable.Repeat("L,line\n", maximum - 1));
        var processor = new Hierarchy(new() { MaxGroupRecords = maximum });
        var summary = await processor.ProcessAsync(new Input(Encoding.UTF8.GetBytes(csv), 1).OpenAsync, HierarchyContext()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        summary.Progress.UnitsValidated.Should().Be(2);

        var overflow = new Hierarchy(new() { MaxGroupRecords = maximum });
        var rejected = await overflow.ProcessAsync(new Input(Encoding.UTF8.GetBytes(csv + "L,excess\n")).OpenAsync, HierarchyContext()).ConfigureAwait(false);
        rejected.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        rejected.ValidationCompleted.Should().BeFalse();
        rejected.Progress.UnitsValidated.Should().Be(1);
        overflow.Diagnostics.Single().Code.Should().Be("csv.limit.group-records");
        overflow.Diagnostics.Single().RecordType.Should().Be("root");
        overflow.Diagnostics.Single().Location?.LineNumber.Should().Be(2 * maximum + 1);
        overflow.Events.Should().Equal("rejected");
    }

    [TestCase("X,unknown\n", FileErrorMode.CollectErrors)]
    [TestCase("L\n", FileErrorMode.CollectErrors)]
    [TestCase("L,ba\"d\n", FileErrorMode.CollectErrors)]
    [TestCase("L,valid\n", FileErrorMode.FailFast)]
    public async Task GroupLimit_IncludesInvalidAndUnknownRecordsAndCannotBeOverridden(string extra, FileErrorMode mode)
    {
        var processor = new Hierarchy(new() { MaxGroupRecords = 2 });
        var context = HierarchyContext(mode);
        var summary = await processor.ProcessAsync(new Input(Encoding.UTF8.GetBytes("O,root\n" + extra + extra + "O,next\n")).OpenAsync, context).ConfigureAwait(false);
        summary.ValidationCompleted.Should().BeFalse();
        processor.Diagnostics.Should().Contain(x => x.Code == "csv.limit.group-records");
        summary.Progress.RecordsRead.Should().Be(3);
        summary.Progress.UnitsExecuted.Should().Be(0);
        processor.Events.Should().Equal("rejected");
    }

    [TestCase(65001, true)]
    [TestCase(65001, false)]
    [TestCase(1200, true)]
    [TestCase(1200, false)]
    [TestCase(1201, true)]
    [TestCase(1201, false)]
    [TestCase(12000, true)]
    [TestCase(12000, false)]
    [TestCase(12001, true)]
    [TestCase(12001, false)]
    public async Task ExplicitEncoding_AcceptsOptionalMatchingBomAndSplitUnicode(int codePage, bool bom)
    {
        var encoding = Encoding.GetEncoding(codePage);
        var content = encoding.GetBytes("κόσμος 😀\n");
        byte[] bytes = bom ? [.. encoding.GetPreamble(), .. content] : content;
        var processor = new Flat(new FileProcessingOptions { MaxInputBytes = bytes.Length }, options: new() { Encoding = encoding });
        var summary = await processor.ProcessAsync(new Input(bytes, 1).OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        processor.Records.Single().Value.Should().Be("κόσμος 😀");
        summary.Progress.BytesRead.Should().Be(bytes.Length);
    }

    [TestCase(1200)]
    [TestCase(1201)]
    [TestCase(12000)]
    [TestCase(12001)]
    public async Task DefaultUtf8_DoesNotAutodetectOtherUnicodeBoms(int codePage)
    {
        var encoding = Encoding.GetEncoding(codePage);
        var processor = new Flat();
        var summary = await processor.ProcessAsync(new Input([.. encoding.GetPreamble(), .. encoding.GetBytes("one\n")], 1).OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeFalse();
        processor.Diagnostics.Single().Code.Should().Be("csv.encoding.bom");
        processor.Records.Should().BeEmpty();
        processor.Events.Should().Equal("rejected");
    }

    [Test]
    public async Task ExplicitUtf16_RejectsOppositeEndianAndUtf8Boms()
    {
        foreach (var prefix in new byte[][] { [0xfe, 0xff], [0xef, 0xbb, 0xbf] })
        {
            var processor = new Flat(options: new() { Encoding = Encoding.Unicode });
            var summary = await processor.ProcessAsync(new Input(prefix, 1).OpenAsync, Context()).ConfigureAwait(false);
            summary.IsSuccess.Should().BeFalse();
            processor.Diagnostics.Single().Code.Should().Be("csv.encoding.bom");
        }
    }

    [TestCase(65001)]
    [TestCase(1200)]
    [TestCase(1201)]
    [TestCase(12000)]
    [TestCase(12001)]
    [TestCase(20127)]
    public async Task MalformedEncoding_OverridesReplacementFallbackAndRejectsOnce(int codePage)
    {
        var encoding = Encoding.GetEncoding(codePage);
        var processor = new Flat(options: new() { Encoding = encoding });
        var input = new Input([.. encoding.GetBytes("valid\n"), 0xff], 1);
        var summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeFalse();
        processor.Diagnostics.Single().Code.Should().Be("csv.encoding.invalid");
        processor.Diagnostics.Single().Location.Should().BeNull();
        processor.Events.Should().Equal("rejected");
        encoding.DecoderFallback.Should().BeOfType<DecoderReplacementFallback>();
        input.Streams.Single().Disposed.Should().BeTrue();
    }

    [Test]
    public async Task EncodingFailures_AreSharedByHierarchyWithoutAcceptedGroups()
    {
        var processor = new Hierarchy(new());
        var summary = await processor.ProcessAsync(new Input([.. Encoding.UTF8.GetBytes("O,root\nL,"), 0xff], 1).OpenAsync, HierarchyContext()).ConfigureAwait(false);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.UnitsValidated.Should().Be(0);
        processor.Diagnostics.Single().Code.Should().Be("csv.encoding.invalid");
        processor.Events.Should().Equal("rejected");
    }

    [Test]
    public async Task BomOnlyAndEmptyInputs_AreEmptyNotPhantomRecords()
    {
        foreach (var bytes in new byte[][] { [], [0xef, 0xbb, 0xbf] })
        {
            var processor = new Flat(new FileProcessingOptions { MaxInputBytes = 3 });
            var summary = await processor.ProcessAsync(new Input(bytes, 1).OpenAsync, Context()).ConfigureAwait(false);
            summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
            summary.Progress.RecordsRead.Should().Be(0);
            summary.Progress.BytesRead.Should().Be(bytes.Length);
        }
    }

    [Test]
    public async Task OnlyLeadingBom_IsConsumedAndItsBytesCountTowardsLimit()
    {
        byte[] bytes = [0xef, 0xbb, 0xbf, .. Encoding.UTF8.GetBytes("one\uFEFFtwo\n")];
        var processor = new Flat();
        (await processor.ProcessAsync(new Input(bytes, 1).OpenAsync, Context()).ConfigureAwait(false)).IsSuccess.Should().BeTrue();
        processor.Records.Single().Value.Should().Be("one\uFEFFtwo");
        var limited = new Flat(new FileProcessingOptions { MaxInputBytes = bytes.Length - 1 });
        (await limited.ProcessAsync(new Input(bytes, 1).OpenAsync, Context()).ConfigureAwait(false)).IsSuccess.Should().BeFalse();
        limited.Diagnostics.Single().Code.Should().Be("file.limit.input-bytes");
    }

    [Test]
    public async Task ReplayBoundsAndEncodingDrift_AreOperationalFaultsNotLateRejections()
    {
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        foreach (var replay in new byte[][] { Encoding.UTF8.GetBytes("one\nexcess\n"), [0xff], [0xff, 0xfe, 0, 0] })
        {
            var processor = new Flat(new FileProcessingOptions { MaxInputBytes = 4 }, provider, new Handler());
            var opens = 0;
            Task<Stream> OpenAsync(CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(++opens == 1 ? Encoding.UTF8.GetBytes("one\n") : replay));
            Func<Task> action = () => processor.ProcessAsync(OpenAsync, Context(execute: true));
            await action.Should().ThrowAsync<InvalidDataException>().ConfigureAwait(false);
            processor.Diagnostics.Should().BeEmpty();
            processor.Events.Should().Equal("eof");
        }
    }

    [Test]
    public async Task GroupOverflow_PreventsReplayAndReplayOverflowCannotBecomeContentRejection()
    {
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var handler = new Handler();
        var processor = new Hierarchy(new() { MaxGroupRecords = 2 }, provider: provider, handler: handler);
        var context = new CsvHierarchyContext(new("groups", "1", "data.csv", "attempt"), FileProcessingMode.ValidateThenExecute, FileErrorMode.CollectErrors);
        var input = new Input(Encoding.UTF8.GetBytes("O,one\nL,line\nL,excess\n"));
        var rejected = await processor.ProcessAsync(input.OpenAsync, context).ConfigureAwait(false);
        rejected.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        handler.Executed.Should().Be(0);
        input.Streams.Should().ContainSingle();

        var replay = new Hierarchy(new() { MaxGroupRecords = 2 }, provider: provider, handler: handler);
        var opens = 0;
        Task<Stream> OpenAsync(CancellationToken cancellationToken) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(
            ++opens == 1 ? "O,one\nL,line\n" : "O,one\nL,line\nL,excess\n")));
        Func<Task> action = () => replay.ProcessAsync(OpenAsync,
            new CsvHierarchyContext(context.Identity, context.Mode, context.ErrorMode));
        await action.Should().ThrowAsync<InvalidDataException>().ConfigureAwait(false);
        replay.Diagnostics.Should().BeEmpty();
        replay.Events.Should().Equal("eof");
    }

    [Test]
    public async Task HierarchyInputOverflow_DoesNotClosePartialRootOrRunEofReconciliation()
    {
        var processor = new Hierarchy(new(), new FileProcessingOptions { MaxInputBytes = 10 });
        var summary = await processor.ProcessAsync(new Input(Encoding.UTF8.GetBytes("O,one\nL,large trailing field\n")).OpenAsync, HierarchyContext()).ConfigureAwait(false);
        summary.ValidationCompleted.Should().BeFalse();
        summary.Progress.UnitsValidated.Should().Be(0);
        processor.Diagnostics.Single().Code.Should().Be("file.limit.input-bytes");
        processor.Events.Should().Equal("rejected");
    }

    [Test]
    public async Task CancellationDuringExactLimitLookahead_DisposesInputWithoutTerminal()
    {
        using var cancellation = new CancellationTokenSource();
        var input = new Input(Encoding.UTF8.GetBytes("one\n"), 1) { BlockAtEnd = true };
        var processor = new Flat(new FileProcessingOptions { MaxInputBytes = 4 });
        var task = processor.ProcessAsync(input.OpenAsync, Context(), cancellation.Token);
        await input.Blocked.Task.ConfigureAwait(false);
        cancellation.Cancel();
        Func<Task> action = () => task;
        await action.Should().ThrowAsync<OperationCanceledException>().ConfigureAwait(false);
        processor.Events.Should().BeEmpty();
        input.Streams.Single().Disposed.Should().BeTrue();
    }

    [TestCase(0, 1)]
    [TestCase(-1, 1)]
    [TestCase(1, 0)]
    [TestCase(1, -1)]
    public void Limits_RequirePositiveValues(long bytes, long records)
    {
        Action create = () => new Flat(new FileProcessingOptions { MaxInputBytes = bytes }, options: new() { MaxGroupRecords = records });
        create.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task RepresentativeWorkloads_RecordAllocationAndTimingWithoutRetainingCompletedUnits()
    {
        await using var provider = new ServiceCollection().AddExecutionContext().BuildServiceProvider();
        var handler = new Handler();
        var processor = new Flat(null, provider, handler);
        var input = new Input(Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat(new string('x', 64) + "\n", 100_000))));
        await processor.ProcessAsync(new Input(Encoding.UTF8.GetBytes("warmup\n")).OpenAsync, Context()).ConfigureAwait(false);
        var before = GC.GetTotalAllocatedBytes(true);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var flat = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);
        timer.Stop();
        await TestContext.Out.WriteLineAsync($"Flat 100k x 64-char rows: {input.Bytes.Length} source bytes, {GC.GetTotalAllocatedBytes(true) - before} allocated bytes, {timer.ElapsedMilliseconds} ms.").ConfigureAwait(false);
        flat.Progress.UnitsValidated.Should().Be(100_000);
        processor.Records.Should().BeEmpty();

        var groupInput = new Input(Encoding.UTF8.GetBytes("O,root\n" + string.Concat(Enumerable.Repeat("L," + new string('x', 1024) + "\n", 9_999))));
        var group = new Hierarchy(new());
        before = GC.GetTotalAllocatedBytes(true);
        timer.Restart();
        var grouped = await group.ProcessAsync(groupInput.OpenAsync, HierarchyContext()).ConfigureAwait(false);
        timer.Stop();
        await TestContext.Out.WriteLineAsync($"One 10k-record root with 1024-char children: {groupInput.Bytes.Length} source bytes, {GC.GetTotalAllocatedBytes(true) - before} allocated bytes, {timer.ElapsedMilliseconds} ms.").ConfigureAwait(false);
        grouped.Progress.UnitsValidated.Should().Be(1);
        grouped.Progress.RecordsRead.Should().Be(10_000);
    }

    [Test]
    public async Task OversizedSource_ReadsAtMostLimitPlusOneAndEmitsNoPerRowExceptions()
    {
        var processor = new Flat(new FileProcessingOptions { MaxInputBytes = 1024 * 1024 });
        var input = new Input(Encoding.UTF8.GetBytes("\"" + new string('x', 2 * 1024 * 1024)));
        var exceptions = 0;
        void Observe(object? sender, FirstChanceExceptionEventArgs args) => Interlocked.Increment(ref exceptions);
        AppDomain.CurrentDomain.FirstChanceException += Observe;
        try
        {
            var summary = await processor.ProcessAsync(input.OpenAsync, Context()).ConfigureAwait(false);
            summary.ValidationCompleted.Should().BeFalse();
            summary.Progress.BytesRead.Should().Be(1024 * 1024 + 1);
            processor.Diagnostics.Single().Code.Should().Be("file.limit.input-bytes");
            exceptions.Should().Be(0);
        }
        finally
        {
            AppDomain.CurrentDomain.FirstChanceException -= Observe;
        }
    }

    [Test]
    public async Task DefaultBounds_AreFiniteAndStopAnOversizedRoot()
    {
        var processingOptions = new FileProcessingOptions();
        processingOptions.MaxInputBytes.Should().Be(64 * 1024 * 1024);
        processingOptions.MaxUnits.Should().Be(100_000);
        processingOptions.MaxDiagnostics.Should().Be(1_000);
        var options = new CsvFileOptions();
        options.MaxGroupRecords.Should().Be(10_000);
        var processor = new Hierarchy(options);
        var input = new Input(Encoding.UTF8.GetBytes("O,root\n" + string.Concat(Enumerable.Repeat("L,line\n", 10_000))));
        var summary = await processor.ProcessAsync(input.OpenAsync, HierarchyContext()).ConfigureAwait(false);
        summary.ValidationCompleted.Should().BeFalse();
        summary.Progress.UnitsValidated.Should().Be(0);
        processor.Diagnostics.Single().Code.Should().Be("csv.limit.group-records");
    }

    private static FileProcessingContext Context(bool execute = false) => new(new("flat", "1", "data.csv", "attempt"),
        execute ? FileProcessingMode.ValidateThenExecute : FileProcessingMode.ValidateOnly, FileErrorMode.CollectErrors);
    private static CsvHierarchyContext HierarchyContext(FileErrorMode mode = FileErrorMode.CollectErrors) => new(new("group", "1", "data.csv", "attempt"), errorMode: mode);
    private static CsvFileMapping<Row> Mapping(int index = 0) => new CsvFileMapping<Row>().MapProperty("Value", x => x.Value, CsvFieldConverters.String, index);

    public sealed class Row
    {
        public string? Value { get; set; }
    }

    private sealed class Handler : FileUnitHandler<Row>
    {
        public int Executed { get; private set; }
        protected override Task<Result> OnExecuteAsync(FileUnitArgs<Row> args, CancellationToken cancellationToken)
        {
            Executed++;
            return Task.FromResult(Result.Success);
        }
    }

    private sealed class Flat : CsvFileProcessor<Row>
    {
        public Flat(FileProcessingOptions? processingOptions = null, IServiceProvider? provider = null, Handler? handler = null, CsvFileOptions? options = null)
            : this(processingOptions, provider, handler, options, []) { }
        private Flat(FileProcessingOptions? processingOptions, IServiceProvider? provider, Handler? handler, CsvFileOptions? options, List<Row> records)
            : base(() => Mapping(), processingOptions, options, validator: handler is null ? new CaptureValidator(records) : null,
                serviceProvider: provider, handlerFactory: handler is null ? null : _ => handler) => Records = records;
        public List<FileDiagnostic> Diagnostics { get; } = [];
        public List<string> Events { get; } = [];
        public List<Row> Records { get; }
        protected override Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Diagnostics.Add(args.Diagnostic);
            return Task.CompletedTask;
        }
        protected override Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
        {
            Events.Add("eof");
            return Task.CompletedTask;
        }
        protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Events.Add("completed");
            return Task.CompletedTask;
        }
        protected override Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Events.Add("rejected");
            return Task.CompletedTask;
        }
    }

    private sealed class CaptureValidator(List<Row> records) : Validator<Row>
    {
        protected override Task OnValidateAsync(ValidationContext<Row> context, CancellationToken cancellationToken)
        {
            records.Add(context.Value);
            return Task.CompletedTask;
        }
    }

    private sealed class Hierarchy(CsvFileOptions options, FileProcessingOptions? processingOptions = null, IServiceProvider? provider = null, Handler? handler = null) : CsvHierarchyFileProcessor<Row>(() => new(0,
        [new CsvRecordRoute<Row>("root", "O", () => Mapping(1)), new CsvRecordRoute<Row>("line", "L", () => Mapping(1))],
        new("root", CsvRecordCardinality.OneOrMore, new CsvHierarchyNode("line")), node => node.GetRecord<Row>()), processingOptions, options,
        serviceProvider: provider, handlerFactory: handler is null ? null : _ => handler)
    {
        public List<FileDiagnostic> Diagnostics { get; } = [];
        public List<string> Events { get; } = [];
        protected override Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Diagnostics.Add(args.Diagnostic);
            return Task.CompletedTask;
        }
        protected override Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
        {
            Events.Add("eof");
            return Task.CompletedTask;
        }
        protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Events.Add("completed");
            return Task.CompletedTask;
        }
        protected override Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Events.Add("rejected");
            return Task.CompletedTask;
        }
    }

    private sealed class Input(byte[] bytes, int chunk = 4096)
    {
        public byte[] Bytes { get; } = bytes;
        public List<TrackedStream> Streams { get; } = [];
        public bool BlockAtEnd { get; init; }
        public TaskCompletionSource Blocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<Stream> OpenAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var stream = new TrackedStream(this, chunk);
            Streams.Add(stream);
            return Task.FromResult<Stream>(stream);
        }
    }

    private sealed class TrackedStream(Input input, int chunk) : Stream
    {
        public long BytesRead { get; private set; }
        public bool Disposed { get; private set; }
        public override bool CanRead => !Disposed;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new InvalidOperationException("Synchronous reading is not allowed.");
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (BytesRead == input.Bytes.Length && input.BlockAtEnd)
            {
                input.Blocked.TrySetResult();
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            }

            var count = Math.Min(Math.Min(chunk, buffer.Length), input.Bytes.Length - (int)BytesRead);
            input.Bytes.AsMemory((int)BytesRead, count).CopyTo(buffer);
            BytesRead += count;
            return count;
        }
        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }
}
