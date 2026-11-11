namespace CoreEx.IO.Test.Unit;

public partial class JsonLinesFileProcessorTests
{
    [Test]
    public async Task SourceAdmissionAndUnitCountAreNotFrameworkQuotas()
    {
        var line = Encoding.UTF8.GetBytes(new string(' ', 700) + "{}\n");
        const int count = 100_001;
        var processor = new JsonLinesFileProcessor<OrdersJsonlOrder>(() => new("Order", new JsonUnitMapping<OrdersJsonlOrder>()));
        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(new RepeatedLineStream(line, count)), Context());
        summary.IsSuccess.Should().BeTrue();
        summary.Progress.UnitsValidated.Should().Be(count);
        summary.Progress.BytesRead.Should().Be((long)line.Length * count).And.BeGreaterThan(64 * 1024 * 1024);
    }

    [Test]
    public async Task LargeLinesTokensMembersAndCollectionsHaveNoImplicitQuotas()
    {
        var mapping = new JsonUnitMapping<OrdersJsonlOrder>().MapString("Id", x => x.Id)
            .MapValues<string>("Tags", x => x.Tags, JsonFieldConverters.String);
        var processor = new JsonLinesFileProcessor<OrdersJsonlOrder>(() => new("Order", mapping),
            new() { Json = new() { UnknownMemberPolicy = JsonUnknownMemberPolicy.Ignore } });
        var text = "{\"Id\":\"" + new string('x', 1024 * 1024 + 1) + "\",\"Tags\":[" +
            string.Join(',', Enumerable.Repeat("\"a\"", 10_001)) + "]," +
            string.Join(',', Enumerable.Range(0, 1025).Select(x => $"\"extra{x}\":0")) + "}";
        var summary = await processor.ProcessAsync(_ => Open(text), Context());
        summary.IsSuccess.Should().BeTrue();
        summary.Progress.UnitsValidated.Should().Be(1);
    }

    [Test]
    public async Task BuiltInDepthFailureIsRecoverableAtTheNextLine()
    {
        var processor = new Probe(options: new()
        {
            Json = new() { MaxDepth = 3, UnknownMemberPolicy = JsonUnknownMemberPolicy.Ignore }
        }, validator: false);
        var summary = await processor.ProcessAsync(_ => Open("{\"ignored\":[[[]]]}\n" + Fixture("valid.jsonl").Split('\n')[0]), Context());
        summary.ValidationCompleted.Should().BeTrue();
        summary.Progress.RecordsRead.Should().Be(2);
        summary.Progress.UnitsValidated.Should().Be(1);
        processor.Diagnostics.Select(x => x.Code).Should().Equal("jsonl.record.invalid");
        processor.Diagnostics[0].Location?.LineNumber.Should().Be(1);
    }

    [TestCase(1000)]
    [TestCase(100000)]
    public async Task IncrementalSourceDoesNotRequireFileMaterialization(int units)
    {
        var line = Encoding.UTF8.GetBytes(Fixture("valid.jsonl").Split('\n')[0] + "\n");
        var processor = new StreamingProbe();
        var before = GC.GetTotalAllocatedBytes(true);
        var timer = System.Diagnostics.Stopwatch.StartNew();
        var source = new RepeatedLineStream(line, units);
        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(source), Context());
        timer.Stop();
        summary.IsSuccess.Should().BeTrue();
        summary.Progress.UnitsValidated.Should().Be(units);
        summary.Progress.BytesRead.Should().Be((long)units * line.Length);
        processor.Seen.Should().Be(units);
        source.Disposed.Should().BeTrue();
        await TestContext.Out.WriteLineAsync($"JSONL net10 Debug: units={units}; sourceBytes={(long)units * line.Length}; cumulativeManagedBytes={GC.GetTotalAllocatedBytes(true) - before}; elapsedMs={timer.ElapsedMilliseconds}. No absolute allocation/time assertion or peak-heap claim.");
    }

    private sealed class StreamingProbe() : JsonLinesFileProcessor<OrdersJsonlOrder>(() => new("Order", OrdersJsonlOrderMapping.Create()), validator: new OrdersJsonlOrderValidator())
    {
        public long Seen { get; private set; }
        protected override Task OnValidatedUnitAsync(FileUnitArgs<OrdersJsonlOrder> args, CancellationToken cancellationToken)
        {
            Seen++;
            return Task.CompletedTask;
        }
    }

    private sealed class RepeatedLineStream(byte[] line, int units) : Stream
    {
        private long _position;
        public bool Disposed { get; private set; }
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = (int)Math.Min(buffer.Length, (long)line.Length * units - _position);
            var copied = 0;
            while (copied < count)
            {
                var start = (int)(_position % line.Length);
                var length = Math.Min(line.Length - start, count - copied);
                line.AsMemory(start, length).CopyTo(buffer[copied..]);
                _position += length;
                copied += length;
            }

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
