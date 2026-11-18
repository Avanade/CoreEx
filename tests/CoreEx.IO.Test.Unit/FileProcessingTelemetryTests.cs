namespace CoreEx.IO.Test.Unit;

[TestFixture]
[NonParallelizable]
public class FileProcessingTelemetryTests
{
    [Test]
    public async Task AttemptsProduceBoundedMetricsAndTracePartialProgressWithoutSourceIdentity()
    {
        var activities = new List<System.Diagnostics.Activity>();
        using var tracing = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == FileProcessingTelemetry.MeterName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activities.Add
        };
        System.Diagnostics.ActivitySource.AddActivityListener(tracing);
        var outcomes = new List<string>();
        using var metrics = new System.Diagnostics.Metrics.MeterListener();
        metrics.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == FileProcessingTelemetry.MeterName && instrument.Name == "file.files.processed")
                listener.EnableMeasurementEvents(instrument);
        };
        metrics.SetMeasurementEventCallback<long>((_, value, tags, _) =>
        {
            value.Should().Be(1);
            tags.Length.Should().Be(1);
            tags[0].Key.Should().Be("file.outcome");
            outcomes.Add((string)tags[0].Value!);
        });
        metrics.Start();

        var processor = new CsvFileProcessor<Row>(() => new CsvFileMapping<Row>().MapProperty("Value", x => x.Value, CsvFieldConverters.Nullable<int>(), 0));
        FileProcessingContext Context() => new(new("Telemetry", "1", "private-source", "private-attempt"));
        Task<Stream> Open(string input) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(input)));
        (await processor.ProcessAsync(_ => Open("1\n"), Context())).IsSuccess.Should().BeTrue();
        (await processor.ProcessAsync(_ => Open("invalid\n"), Context())).Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        var failure = new IOException("source failed");
        var faultContext = Context();
        var fault = () => processor.ProcessAsync(_ => Task.FromException<Stream>(failure), faultContext);
        (await fault.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(failure);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var cancelled = () => processor.ProcessAsync(_ => Open("1\n"), Context(), cancellation.Token);
        await cancelled.Should().ThrowAsync<OperationCanceledException>();
        var reportingContext = Context();
        var reportingFault = () => new ReportingFaultProcessor().ProcessAsync(_ => Open("1\n"), reportingContext);
        await reportingFault.Should().ThrowAsync<IOException>();
        reportingContext.Progress.UnitsValidated.Should().Be(1);

        outcomes.Should().Equal("succeeded", "content-failed", "faulted", "cancelled", "faulted");
        activities.Should().HaveCount(5);
        activities.Select(x => x.GetTagItem("file.outcome")).Should().Equal("succeeded", "content-failed", "faulted", "cancelled", "faulted");
        activities[0].GetTagItem("file.records_read").Should().Be(1L);
        activities[1].GetTagItem("file.error_count").Should().Be(1L);
        foreach (var activity in activities)
        {
            activity.GetTagItem("file.definition").Should().Be("Telemetry");
            activity.TagObjects.Select(x => x.Value).Should().NotContain("private-source").And.NotContain("private-attempt");
        }
    }

    [TestCase('\ud800', '"', null)]
    [TestCase(',', '\udfff', null)]
    [TestCase(',', '"', '\ud800')]
    [TestCase(',', ',', null)]
    [TestCase(',', '"', ',')]
    [TestCase('\n', '"', null)]
    public void InvalidDialectIsRejectedBeforeOpening(char delimiter, char quote, char? comment)
    {
        var act = () => new CsvFileProcessor<Row>(() => new CsvFileMapping<Row>(), options: new() { Delimiter = delimiter, Quote = quote, Comment = comment });
        act.Should().Throw<ArgumentException>();
    }

    [TestCase(false, "succeeded")]
    [TestCase(true, "execution-failed")]
    public async Task ExecutionOutcomesRetainCountsAndOriginalFailure(bool fail, string outcome)
    {
        var activities = new List<System.Diagnostics.Activity>();
        using var tracing = new System.Diagnostics.ActivityListener
        {
            ShouldListenTo = source => source.Name == FileProcessingTelemetry.MeterName,
            Sample = (ref System.Diagnostics.ActivityCreationOptions<System.Diagnostics.ActivityContext> _) => System.Diagnostics.ActivitySamplingResult.AllData,
            ActivityStopped = activities.Add
        };
        System.Diagnostics.ActivitySource.AddActivityListener(tracing);
        var failure = Result.Fail(new BusinessException("Unit failed."));
        var services = new ServiceCollection().AddExecutionContext();
        services.AddScoped(_ => new Handler(fail ? failure : Result.Success));
        await using var provider = services.BuildServiceProvider();
        var processor = new CsvFileProcessor<Row>(() => new CsvFileMapping<Row>().MapProperty("Value", x => x.Value, CsvFieldConverters.Nullable<int>(), 0),
            serviceProvider: provider, handlerFactory: scoped => scoped.GetRequiredService<Handler>());
        var context = new FileProcessingContext(new("Telemetry", "1", "source", "attempt"), FileProcessingMode.ValidateThenExecute);
        var summary = await processor.ProcessAsync(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("1\n"))), context);
        summary.Progress.UnitsAttempted.Should().Be(1);
        summary.Progress.UnitsExecuted.Should().Be(fail ? 0 : 1);
        if (fail)
            summary.ExecutionResult.Error.Should().BeSameAs(failure.Error);

        activities.Should().ContainSingle();
        activities[0].GetTagItem("file.outcome").Should().Be(outcome);
        activities[0].GetTagItem("file.units_attempted").Should().Be(1L);
        activities[0].GetTagItem("file.units_executed").Should().Be(fail ? 0L : 1L);
    }

    [Test]
    public async Task OutcomeLogsUseHostLoggerAndOmitSourceAndInvalidValues()
    {
        var logs = new Logs();
        await using var provider = new ServiceCollection().AddLogging(builder => builder.AddProvider(logs).SetMinimumLevel(LogLevel.Information)).BuildServiceProvider();
        var processor = new CsvFileProcessor<Row>(() => new CsvFileMapping<Row>().MapProperty("Value", x => x.Value, CsvFieldConverters.Nullable<int>(), 0), serviceProvider: provider);
        var context = new FileProcessingContext(new("Telemetry", "1", "private-source", "private-attempt"));
        await processor.ProcessAsync(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("private-invalid-value\n"))), context);
        logs.Entries.Should().Contain(x => x.Level == LogLevel.Warning && x.Message.Contains("content-failed") && x.Message.Contains("records: 1"));
        logs.Entries.Should().NotContain(x => x.Message.Contains("private-source") || x.Message.Contains("private-attempt") || x.Message.Contains("private-invalid-value"));
    }

    private sealed class Logs : ILoggerProvider, ILogger
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));

        public void Dispose() { }
    }

    private sealed class Handler(Result result) : FileUnitHandler<Row>
    {
        protected override Task<Result> OnExecuteAsync(FileUnitArgs<Row> args, CancellationToken cancellationToken) => Task.FromResult(result);
    }

    private sealed class ReportingFaultProcessor() : CsvFileProcessor<Row>(() => new CsvFileMapping<Row>().MapProperty("Value", x => x.Value, CsvFieldConverters.Nullable<int>(), 0))
    {
        protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
            => Task.FromException(new IOException("Report persistence failed."));
    }

    private sealed class Row
    {
        public int? Value { get; set; }
    }
}
