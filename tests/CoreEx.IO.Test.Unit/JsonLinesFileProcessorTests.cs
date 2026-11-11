namespace CoreEx.IO.Test.Unit;

[TestFixture]
public partial class JsonLinesFileProcessorTests
{
    private static string Fixture(string name)
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !System.IO.File.Exists(Path.Combine(root.FullName, "CoreEx.slnx")))
            root = root.Parent;

        return System.IO.File.ReadAllText(Path.Combine(root?.FullName ?? throw new DirectoryNotFoundException(), "docs", "design", "io-multiformat", "phase0-fixtures", name));
    }

    private static FileProcessingContext Context(FileProcessingMode mode = FileProcessingMode.ValidateOnly, FileErrorMode errors = FileErrorMode.CollectErrors)
        => new(new("orders", "1", "immutable/orders.jsonl", "attempt", "revision-1"), mode, errors);

    private static Task<Stream> Open(string text) => Task.FromResult<Stream>(new ChunkStream(Encoding.UTF8.GetBytes(text)));

    private static ServiceProvider Provider() => new ServiceCollection().AddExecutionContext().AddScoped<Stamp>().BuildServiceProvider();

    [TestCase(FileProcessingMode.ValidateOnly, 1, 0)]
    [TestCase(FileProcessingMode.ValidateThenExecute, 2, 2)]
    [TestCase(FileProcessingMode.ValidateAndExecute, 1, 2)]
    public async Task CanonicalNestedGraphAndReplay(FileProcessingMode mode, int expectedOpens, int executed)
    {
        await using var services = Provider();
        var processor = new Probe(services);
        var opens = 0;
        var context = Context(mode);
        var text = Fixture("valid.jsonl");
        var summary = await processor.ProcessAsync(_ => { opens++; return Open(text); }, context);
        summary.Outcome.Should().Be(FileProcessingOutcome.Succeeded);
        summary.ValidationCompleted.Should().BeTrue();
        AssertCounts(summary, 2, 0, 2, 0, 2, executed);
        summary.Progress.BytesRead.Should().Be(Encoding.UTF8.GetByteCount(text));
        opens.Should().Be(expectedOpens);
        processor.Diagnostics.Should().BeEmpty();
        processor.Observed.Should().HaveCount(2);
        processor.Observed[0].Unit.SubmittedAt?.Offset.Should().Be(TimeSpan.FromHours(10));
        processor.Observed[0].Unit.BillingAddress?.Code.Should().Be("B1");
        processor.Observed[0].Unit.ShippingAddress?.Code.Should().Be("S1");
        processor.Observed[0].Unit.Tags.Should().Equal("priority", "web");
        processor.Observed[1].Unit.Tags.Should().BeEmpty();
        processor.Executed.Select(x => x.Info.Ordinal).Should().Equal(Enumerable.Range(1, executed).Select(x => (long)x));
        processor.Configured.Should().HaveCount(2 + executed);
        processor.Configured.Select(x => x.UnitContext).Distinct().Should().HaveCount(2 + executed);
        processor.Handlers.Distinct().Should().HaveCount(2 + executed);
        context.AcceptedMetadata.Should().NotBeNull().And.BeEmpty();
        processor.Completed.Should().Be(1);
        if (mode == FileProcessingMode.ValidateThenExecute)
        {
            processor.Executed[0].Unit.Should().NotBeSameAs(processor.Observed[0].Unit);
            processor.Executed[0].Context.Should().NotBeSameAs(context);
            processor.Executed.Select(x => x.Info.SourceByteOffset).Should().Equal(processor.Observed.Select(x => x.Info.SourceByteOffset));
        }
    }

    [TestCase("invalid-child.jsonl", "file.validation", 2, 1, 1)]
    [TestCase("malformed-line.jsonl", "jsonl.record.invalid", 3, 1, 2)]
    [TestCase("missing-child.jsonl", "file.structure.cardinality", 1, 1, 0)]
    public async Task RejectionNeverReplaysAndCountersMatchContract(string file, string code, int records, int invalid, int valid)
    {
        await using var services = Provider();
        var processor = new Probe(services);
        var opens = 0;
        var summary = await processor.ProcessAsync(_ => { opens++; return Open(Fixture(file)); }, Context(FileProcessingMode.ValidateThenExecute));
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.ValidationCompleted.Should().BeTrue();
        AssertCounts(summary, records, invalid, records, invalid, valid, 0);
        opens.Should().Be(1);
        processor.Diagnostics.Select(x => x.Code).Should().Equal(code);
        var diagnostic = processor.Diagnostics.Single();
        diagnostic.UnitOrdinal.Should().Be(code == "jsonl.record.invalid" ? 2 : 1);
        diagnostic.Location?.LineNumber.Should().Be(diagnostic.UnitOrdinal);
        if (file == "invalid-child.jsonl")
        {
            diagnostic.Location?.PropertyPath.Should().Be("Lines[0].Quantity");
            diagnostic.Location?.SourcePath.Should().Be("/Lines/0/Quantity");
        }
        else if (file == "missing-child.jsonl")
        {
            diagnostic.Location?.PropertyPath.Should().Be("Lines");
            diagnostic.Location?.SourcePath.Should().Be("/Lines");
        }

        processor.Executed.Should().BeEmpty();
        processor.ContentFailed.Should().Be(1);
    }

    [TestCase(FileErrorMode.CollectErrors, 2, 1, true)]
    [TestCase(FileErrorMode.FailFast, 1, 0, false)]
    public async Task SinglePassKeepsHonestPartialExecution(FileErrorMode errors, int read, int executed, bool complete)
    {
        await using var services = Provider();
        var processor = new Probe(services);
        var context = Context(FileProcessingMode.ValidateAndExecute, errors);
        var summary = await processor.ProcessAsync(_ => Open(Fixture("invalid-child.jsonl")), context);
        AssertCounts(summary, read, 1, read, 1, executed, executed);
        summary.ValidationCompleted.Should().Be(complete);
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        processor.Executed.Select(x => x.Info.Ordinal).Should().Equal(executed == 0 ? [] : new long[] { 2 });
        context.AcceptedMetadata.Should().BeNull();
    }

    [TestCase("{\"Id\":\"A\",\"Id\":\"B\"}", "jsonl.record.invalid", null, "")]
    [TestCase("[]", "json.unit.type", null, "")]
    [TestCase(" ", "jsonl.line.blank", null, "")]
    public async Task RecoverableBadLineDoesNotLoseNextUnit(string first, string code, string? property, string source)
    {
        var processor = new Probe();
        var summary = await processor.ProcessAsync(_ => Open(first + "\n" + Fixture("valid.jsonl")), Context());
        AssertCounts(summary, 3, 1, 3, 1, 2, 0);
        summary.ValidationCompleted.Should().BeTrue();
        processor.Diagnostics.Select(x => x.Code).Should().Equal(code);
        processor.Diagnostics[0].Location?.PropertyPath.Should().Be(property);
        processor.Diagnostics[0].Location?.SourcePath.Should().Be(source);
        processor.Diagnostics[0].Message.ToString().Should().NotContain("private-data");
    }

    [Test]
    public async Task UnknownMembersAreAnchoredToDeclaredParentWithoutEchoingNames()
    {
        var text = Fixture("valid.jsonl").Replace("\"Code\":\"B1\"", "\"Code\":\"B1\",\"private-data\":123", StringComparison.Ordinal);
        var processor = new Probe();
        await processor.ProcessAsync(_ => Open(text), Context());
        processor.Diagnostics.Select(x => x.Code).Should().Equal("json.member.unexpected");
        processor.Diagnostics[0].Location?.PropertyPath.Should().Be("BillingAddress");
        processor.Diagnostics[0].Location?.SourcePath.Should().Be("/BillingAddress");
        processor.Diagnostics[0].Message.ToString().Should().NotContain("private-data");
    }

    [TestCase("\"2\"", "json.field.type")]
    [TestCase("2.0", "json.field.type")]
    [TestCase("2147483648", "json.field.type")]
    [TestCase("null", "file.validation")]
    [TestCase("0", "file.validation")]
    public async Task StrictNumericMappingSuppressesRequiredCascade(string quantity, string code)
    {
        var text = Fixture("valid.jsonl").Split('\n')[0].Replace("\"Quantity\":2", "\"Quantity\":" + quantity, StringComparison.Ordinal);
        var processor = new Probe();
        var summary = await processor.ProcessAsync(_ => Open(text), Context());
        AssertCounts(summary, 1, 1, 1, 1, 0, 0);
        processor.Diagnostics.Select(x => x.Code).Should().Equal(code);
        processor.Diagnostics[0].Location?.PropertyPath.Should().Be("Lines[0].Quantity");
        processor.Diagnostics[0].Location?.SourcePath.Should().Be("/Lines/0/Quantity");
    }

    [TestCase("\"BillingAddress\":{\"Code\":\"B1\"}", "\"BillingAddress\":{}", "BillingAddress.Code", "/BillingAddress/Code", "file.validation")]
    [TestCase("\"BillingAddress\":{\"Code\":\"B1\"}", "\"BillingAddress\":null", "BillingAddress", "/BillingAddress", "file.validation")]
    [TestCase("\"ShippingAddress\":{\"Code\":\"S1\"}", "\"ShippingAddress\":{\"Code\":false}", "ShippingAddress.Code", "/ShippingAddress/Code", "json.field.type")]
    [TestCase("\"Tags\":[\"priority\",\"web\"]", "\"Tags\":[null]", "Tags[0]", "/Tags/0", "json.field.type")]
    [TestCase("\"Lines\":[{\"Sku\":\"X\",\"Quantity\":2},{\"Sku\":\"Y\",\"Quantity\":1}]", "\"Lines\":[]", "Lines", "/Lines", "file.structure.cardinality")]
    [TestCase("\"Lines\":[{\"Sku\":\"X\",\"Quantity\":2},{\"Sku\":\"Y\",\"Quantity\":1}]", "\"Lines\":null", "Lines", "/Lines", "file.structure.cardinality")]
    public async Task NestedOccurrencesRetainDistinctPaths(string before, string after, string property, string source, string code)
    {
        var processor = new Probe();
        var text = Fixture("valid.jsonl").Split('\n')[0].Replace(before, after, StringComparison.Ordinal);
        await processor.ProcessAsync(_ => Open(text), Context());
        processor.Diagnostics.Select(x => x.Code).Should().Equal(code);
        processor.Diagnostics.Single().Location?.PropertyPath.Should().Be(property);
        processor.Diagnostics.Single().Location?.SourcePath.Should().Be(source);
    }

    [Test]
    public async Task ActualByteOffsetsIncludeCrLfAndMultibyteTextButNotReadAhead()
    {
        var text = Fixture("valid.jsonl").Replace("\"A\"", "\"é🙂\"", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal).TrimEnd('\r', '\n');
        var processor = new Probe();
        var summary = await processor.ProcessAsync(_ => Open(text), Context());
        summary.IsSuccess.Should().BeTrue();
        processor.Observed.Select(x => x.Info.SourceByteOffset).Should().Equal(0, Encoding.UTF8.GetByteCount(text[..(text.IndexOf('\n') + 1)]));
        processor.Observed.Select(x => x.Info.Location.LineNumber).Should().Equal(1, 2);
        summary.Progress.BytesRead.Should().Be(Encoding.UTF8.GetByteCount(text));
    }

    [TestCase("", JsonLinesBlankLinePolicy.Reject, 0, 0)]
    [TestCase("\n", JsonLinesBlankLinePolicy.Reject, 1, 1)]
    [TestCase(" \r\n\t\n", JsonLinesBlankLinePolicy.Skip, 0, 0)]
    public async Task EmptyAndBlankFraming(string text, JsonLinesBlankLinePolicy policy, int records, int errors)
    {
        var processor = new Probe(options: new() { BlankLinePolicy = policy });
        var summary = await processor.ProcessAsync(_ => Open(text), Context());
        summary.Progress.RecordsRead.Should().Be(records);
        summary.Progress.ErrorCount.Should().Be(errors);
        summary.ValidationCompleted.Should().BeTrue();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task MalformedUtf8InConsumedStringsIsLocalizedAndIgnoredStringsAreNotDecoded(bool ignored)
    {
        var valid = Fixture("valid.jsonl").Split('\n')[0];
        var text = valid.Replace("\"Id\":\"A\"", ignored ? "\"unknown\":\"marker\",\"Id\":\"A\"" : "\"Id\":\"marker\"", StringComparison.Ordinal);
        var index = text.IndexOf("marker", StringComparison.Ordinal);
        var bytes = Encoding.UTF8.GetBytes(text[..index]).Concat(new byte[] { 0xc3, 0x28 })
            .Concat(Encoding.UTF8.GetBytes(text[(index + "marker".Length)..] + "\n" + valid)).ToArray();
        var processor = new Probe(options: new() { Json = new() { UnknownMemberPolicy = JsonUnknownMemberPolicy.Ignore } });
        var context = Context();
        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(new ChunkStream(bytes)), context);
        summary.ValidationCompleted.Should().BeTrue();
        AssertCounts(summary, 2, ignored ? 0 : 1, 2, ignored ? 0 : 1, ignored ? 2 : 1, 0);
        processor.Diagnostics.Select(x => x.Code).Should().Equal(ignored ? [] : new[] { "json.field.type" });
        if (!ignored)
        {
            processor.Diagnostics[0].Location?.PropertyPath.Should().Be("Id");
            processor.Diagnostics[0].Location?.SourcePath.Should().Be("/Id");
        }
    }

    [Test]
    public async Task AllBomsAreRejected()
    {
        byte[][] boms = [[0xef, 0xbb, 0xbf], [0xff, 0xfe], [0xfe, 0xff], [0, 0, 0xfe, 0xff], [0xff, 0xfe, 0, 0]];
        foreach (var bom in boms)
        {
            var processor = new Probe();
            var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(new ChunkStream([.. bom, .. "{}"u8.ToArray()])), Context());
            summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
            summary.ValidationCompleted.Should().Be(bom[0] != 0xef);
            processor.Diagnostics.Select(x => x.Code).Should().Equal(bom[0] == 0xef ? "jsonl.encoding.bom" : "jsonl.record.invalid");
        }
    }

    [TestCase(FileProcessingMode.ValidateThenExecute)]
    [TestCase(FileProcessingMode.ValidateAndExecute)]
    public async Task BusinessFailurePreservesOriginalResultAndStops(FileProcessingMode mode)
    {
        await using var services = Provider();
        var failure = Result.Fail(new BusinessException("Application rule."));
        var processor = new Probe(services) { Execute = (args, _) => Task.FromResult(args.Info.Ordinal == 2 ? failure : Result.Success) };
        var context = Context(mode);
        var summary = await processor.ProcessAsync(_ => Open(Fixture("valid.jsonl")), context);
        summary.Outcome.Should().Be(FileProcessingOutcome.ExecutionFailed);
        summary.ExecutionResult.Error.Should().BeSameAs(failure.Error);
        processor.Failed?.Result.Error.Should().BeSameAs(failure.Error);
        summary.Progress.UnitsAttempted.Should().Be(2);
        summary.Progress.UnitsExecuted.Should().Be(1);
        summary.ValidationCompleted.Should().Be(mode == FileProcessingMode.ValidateThenExecute);
        processor.Completed.Should().Be(0);
        processor.ContentFailed.Should().Be(0);
    }

    [Test]
    public async Task CancellationAndOperationalHookFaultsPropagateWithoutFinalizers()
    {
        var error = new IOException("Report fault.");
        var processor = new Probe { Observe = (_, _) => Task.FromException(error) };
        var context = Context();
        Func<Task> run = () => processor.ProcessAsync(_ => Open(Fixture("valid.jsonl")), context);
        (await run.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(error);
        context.Progress.UnitsValidated.Should().Be(1);
        processor.Completed.Should().Be(0);
        processor.ContentFailed.Should().Be(0);

        using var cancellation = new CancellationTokenSource();
        processor = new Probe { Observe = (_, _) => { cancellation.Cancel(); return Task.CompletedTask; } };
        context = Context();
        run = () => processor.ProcessAsync(_ => Open(Fixture("valid.jsonl")), context, cancellation.Token);
        await run.Should().ThrowAsync<OperationCanceledException>();
        context.Progress.UnitsValidated.Should().Be(1);
        processor.Completed.Should().Be(0);
        processor.ContentFailed.Should().Be(0);
    }

    [Test]
    public async Task SourceFaultRetainsProgressAndDisposesOwnedStream()
    {
        var fault = new IOException("Source failed.");
        var text = Fixture("valid.jsonl");
        var stream = new ChunkStream(Encoding.UTF8.GetBytes(text)) { FaultAt = text.IndexOf('\n') + 1, Fault = fault };
        var processor = new Probe();
        var context = Context();
        Func<Task> run = () => processor.ProcessAsync(_ => Task.FromResult<Stream>(stream), context);
        (await run.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(fault);
        stream.Disposed.Should().BeTrue();
        context.Progress.UnitsValidated.Should().Be(1);
        processor.ContentFailed.Should().Be(0);
    }

    private static void AssertCounts(FileProcessingSummary summary, int records, int invalidRecords, int units, int invalidUnits, int validated, int executed)
    {
        summary.Progress.RecordsRead.Should().Be(records);
        summary.Progress.InvalidRecords.Should().Be(invalidRecords);
        summary.Progress.UnitsRead.Should().Be(units);
        summary.Progress.InvalidUnits.Should().Be(invalidUnits);
        summary.Progress.UnitsValidated.Should().Be(validated);
        summary.Progress.UnitsExecuted.Should().Be(executed);
    }

    private sealed class Stamp
    {
        public Probe? Owner { get; set; }
    }

    private sealed class Probe : JsonLinesFileProcessor<OrdersJsonlOrder>
    {
        public Probe(IServiceProvider? services = null, JsonLinesFileOptions? options = null, bool validator = true)
            : base(() => new("Order", OrdersJsonlOrderMapping.Create()), options, validator ? new OrdersJsonlOrderValidator() : null, services,
                services is null ? null : scoped => new Handler(scoped.GetRequiredService<Stamp>())) { }

        public List<FileDiagnostic> Diagnostics { get; } = [];
        public List<FileUnitArgs<OrdersJsonlOrder>> Observed { get; } = [];
        public List<FileUnitArgs<OrdersJsonlOrder>> Executed { get; } = [];
        public List<FileUnitContextArgs<OrdersJsonlOrder>> Configured { get; } = [];
        public List<object> Handlers { get; } = [];
        public Func<FileUnitArgs<OrdersJsonlOrder>, CancellationToken, Task>? Observe { get; init; }
        public Func<FileUnitArgs<OrdersJsonlOrder>, CancellationToken, Task<Result>>? Execute { get; init; }
        public Func<FileUnitValidationArgs<OrdersJsonlOrder>, CancellationToken, Task>? Validate { get; init; }
        public Func<FileDiagnosticArgs, CancellationToken, Task>? Diagnose { get; init; }
        public int Completed { get; private set; }
        public int ContentFailed { get; private set; }
        public FileExecutionFailedArgs? Failed { get; private set; }

        protected override void OnConfigureUnitContext(FileUnitContextArgs<OrdersJsonlOrder> args)
        {
            Configured.Add(args);
            args.UnitContext.ServiceProvider.ThrowIfNull().GetRequiredService<Stamp>().Owner = this;
        }

        protected override async Task OnValidatedUnitAsync(FileUnitArgs<OrdersJsonlOrder> args, CancellationToken cancellationToken)
        {
            Observed.Add(args);
            if (Observe is not null)
                await Observe(args, cancellationToken);
        }

        protected override Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        {
            Diagnostics.Add(args.Diagnostic);
            return Diagnose?.Invoke(args, cancellationToken) ?? Task.CompletedTask;
        }

        protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            Completed++;
            return Task.CompletedTask;
        }

        protected override Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        {
            ContentFailed++;
            return Task.CompletedTask;
        }

        protected override Task OnExecutionFailedAsync(FileExecutionFailedArgs args, CancellationToken cancellationToken)
        {
            Failed = args;
            return Task.CompletedTask;
        }
    }

    private sealed class Handler(Stamp stamp) : FileUnitHandler<OrdersJsonlOrder>
    {
        protected override Task OnValidateAsync(FileUnitValidationArgs<OrdersJsonlOrder> args, CancellationToken cancellationToken)
        {
            var probe = stamp.Owner.ThrowIfNull();
            probe.Handlers.Add(stamp);
            return probe.Validate?.Invoke(args, cancellationToken) ?? Task.CompletedTask;
        }

        protected override Task<Result> OnExecuteAsync(FileUnitArgs<OrdersJsonlOrder> args, CancellationToken cancellationToken)
        {
            var probe = stamp.Owner.ThrowIfNull();
            probe.Handlers.Add(stamp);
            probe.Executed.Add(args);
            return probe.Execute?.Invoke(args, cancellationToken) ?? Task.FromResult(Result.Success);
        }
    }

    private sealed class ChunkStream(byte[] bytes) : Stream
    {
        private int _position;
        private int _read;
        public int FaultAt { get; init; } = int.MaxValue;
        public Exception? Fault { get; init; }
        public bool Disposed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_position >= FaultAt)
                throw Fault ?? new IOException();

            var count = Math.Min(Math.Min(buffer.Length, bytes.Length - _position), Math.Min(FaultAt - _position, new[] { 1, 3, 2, 7 }[_read++ % 4]));
            bytes.AsMemory(_position, count).CopyTo(buffer);
            _position += count;
            return ValueTask.FromResult(count);
        }

        protected override void Dispose(bool disposing) { Disposed = true; base.Dispose(disposing); }
        public override void Flush() => throw new NotSupportedException();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
