namespace CoreEx.IO.Test.Unit;

public partial class JsonLinesFileProcessorTests
{
    [TestCase("{\"Id\":\"12345678", "jsonl.record.invalid", true)]
    [TestCase("{\"Id\":123456789", "jsonl.record.invalid", true)]
    [TestCase("{\"\\uDFFF\":1}", "jsonl.record.invalid", true)]
    [TestCase("{\"ignored\":{\"x\":1,\"x\":2}}", "jsonl.record.invalid", true)]
    public async Task InvalidAndUnfinishedTokensRecoverAtTheNextLine(string first, string code, bool complete)
    {
        var processor = new Probe(options: new()
        {
            Json = new() { UnknownMemberPolicy = JsonUnknownMemberPolicy.Ignore }
        });
        var summary = await processor.ProcessAsync(_ => Open(first + "\n" + Fixture("valid.jsonl")), Context());
        summary.ValidationCompleted.Should().Be(complete);
        summary.Progress.UnitsValidated.Should().Be(complete ? 2 : 0);
        processor.Diagnostics.Select(x => x.Code).Should().Equal(code);
    }

    [Test]
    public async Task CollectionMaxItemsCountsEverySourceElementAtTheBoundary()
    {
        var line = Fixture("valid.jsonl").Split('\n')[0];
        foreach (var count in new[] { 100, 101 })
        {
            var text = line.Replace("[\"priority\",\"web\"]", "[" + string.Join(',', Enumerable.Repeat("\"x\"", count)) + "]", StringComparison.Ordinal);
            var processor = new Probe();
            var summary = await processor.ProcessAsync(_ => Open(text), Context());
            summary.IsSuccess.Should().Be(count == 100);
            if (count > 100)
            {
                processor.Diagnostics.Single().Code.Should().Be("file.structure.cardinality");
                processor.Diagnostics.Single().Location?.SourcePath.Should().Be("/Tags");
                AssertCounts(summary, 1, 1, 1, 1, 0, 0);
            }
        }
    }

    [TestCase("2026-03-15T10:30:00+10:00", true)]
    [TestCase("2026-03-15T10:30:00Z", true)]
    [TestCase("2026-03-15T10:30:00.1234567-03:30", true)]
    [TestCase("2026-03-15T10:30:00", false)]
    [TestCase("2026-03-15T10:30Z", true)]
    [TestCase("2026-03-15T10:30:00.12345678Z", true)]
    [TestCase("15 March 2026 10:30 +10:00", true)]
    public void StandardDateTimeOffsetRequiresExplicitOffsetAndRetainsIt(string text, bool valid)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(text));
        JsonFieldConverters.DateTimeOffset()(document.RootElement, out var value).Should().Be(valid);
        if (valid)
            value.Offset.Should().Be(DateTimeOffset.Parse(text, CultureInfo.InvariantCulture).Offset);
    }

    [TestCase("2026-03-15T10:30:00", true)]
    [TestCase("2026-03-15T10:30:00.1234567", true)]
    [TestCase("2026-03-15T10:30:00Z", false)]
    [TestCase("2026-03-15T10:30:00+10:00", false)]
    [TestCase("2026-03-15", true)]
    [TestCase("15 March 2026 10:30", true)]
    [TestCase("Sun, 15 Mar 2026 10:30:00 GMT", false)]
    public void StandardJsonDateTimeRejectsOffsets(string text, bool valid)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(text));
        JsonFieldConverters.DateTime()(document.RootElement, out var value).Should().Be(valid);
        if (valid)
            value.Kind.Should().Be(DateTimeKind.Unspecified);
    }

    [Test]
    public void SuppliedDateFormatsRemainExactForCsvAndJson()
    {
        using var document = JsonDocument.Parse("""["20260315","2026-03-15","202603151030","2026-03-15T10:30","202603151030+10:00","2026-03-15T10:30+10:00","15 March 2026"]""");
        var values = document.RootElement;
        JsonFieldConverters.DateOnly("yyyyMMdd")(values[0], out _).Should().BeTrue();
        JsonFieldConverters.DateOnly("yyyyMMdd")(values[1], out _).Should().BeFalse();
        JsonFieldConverters.DateOnly()(values[6], out _).Should().BeTrue();
        JsonFieldConverters.DateTime("yyyyMMddHHmm")(values[2], out _).Should().BeTrue();
        JsonFieldConverters.DateTime("yyyyMMddHHmm")(values[3], out _).Should().BeFalse();
        JsonFieldConverters.DateTimeOffset("yyyyMMddHHmmzzz")(values[4], out _).Should().BeTrue();
        JsonFieldConverters.DateTimeOffset("yyyyMMddHHmmzzz")(values[5], out _).Should().BeFalse();
        CsvFieldConverters.DateOnly("yyyyMMdd")("20260315", out _).Should().BeTrue();
        CsvFieldConverters.DateOnly("yyyyMMdd")("2026-03-15", out _).Should().BeFalse();
        CsvFieldConverters.DateOnly()("15 March 2026", out _).Should().BeTrue();
        CsvFieldConverters.DateTime("yyyyMMddHHmm")("202603151030", out _).Should().BeTrue();
        CsvFieldConverters.DateTime("yyyyMMddHHmm")("2026-03-15T10:30", out _).Should().BeFalse();
        CsvFieldConverters.DateTime()("15 March 2026 10:30", out _).Should().BeTrue();
        CsvFieldConverters.DateTimeOffset("yyyyMMddHHmmzzz")("202603151030+10:00", out _).Should().BeTrue();
        CsvFieldConverters.DateTimeOffset("yyyyMMddHHmmzzz")("2026-03-15T10:30+10:00", out _).Should().BeFalse();
        CsvFieldConverters.DateTimeOffset()("15 March 2026 10:30 +10:00", out var instant).Should().BeTrue();
        instant?.Offset.Should().Be(TimeSpan.FromHours(10));
        CsvFieldConverters.DateTimeOffset()("15 March 2026 10:30", out _).Should().BeFalse();
    }

    [TestCase("\\uD800", true)]
    [TestCase("\\uDFFF", true)]
    [TestCase("\\uD800", false)]
    [TestCase("\\uDFFF", false)]
    public async Task StringContentIsDecodedOnlyWhenConsumed(string content, bool ignored)
    {
        var valid = Fixture("valid.jsonl").Split('\n')[0];
        var text = valid.Replace("\"Id\":\"A\"", ignored ? "\"Id\":\"A\",\"ignored\":\"" + content + "\"" : "\"Id\":\"" + content + "\"", StringComparison.Ordinal);
        var processor = new Probe(options: new() { Json = new() { UnknownMemberPolicy = JsonUnknownMemberPolicy.Ignore } });
        var summary = await processor.ProcessAsync(_ => Open(text), Context());
        summary.IsSuccess.Should().Be(ignored);
        summary.Progress.UnitsValidated.Should().Be(ignored ? 1 : 0);
        processor.Diagnostics.Select(x => x.Code).Should().Equal(ignored ? [] : new[] { "json.field.type" });
    }

    [Test]
    public void StrictNumericAndBooleanConversionsPreservePrecision()
    {
        using var document = JsonDocument.Parse("""[9223372036854775807,79228162514264337593543950335,1e400,true,"true",1,"1"]""");
        var values = document.RootElement;
        JsonFieldConverters.Int64(values[0], out var integer).Should().BeTrue();
        integer.Should().Be(long.MaxValue);
        JsonFieldConverters.Decimal(values[1], out var dec).Should().BeTrue();
        dec.Should().Be(decimal.MaxValue);
        JsonFieldConverters.Double(values[2], out _).Should().BeFalse();
        JsonFieldConverters.Boolean(values[3], out var boolean).Should().BeTrue();
        boolean.Should().BeTrue();
        JsonFieldConverters.Boolean(values[4], out _).Should().BeFalse();
        JsonFieldConverters.Boolean(values[5], out _).Should().BeFalse();
        JsonFieldConverters.Int64(values[6], out _).Should().BeFalse();
    }

    [TestCase(null)]
    [TestCase("null")]
    [TestCase("[]")]
    public async Task OptionalCollectionsKeepAbsentNullAndEmptyDistinct(string? value)
    {
        var text = Fixture("valid.jsonl").Split('\n')[0];
        text = text.Replace(",\"Tags\":[\"priority\",\"web\"]", value is null ? "" : ",\"Tags\":" + value, StringComparison.Ordinal);
        var processor = new Probe();
        (await processor.ProcessAsync(_ => Open(text), Context())).IsSuccess.Should().BeTrue();
        if (value == "[]")
            processor.Observed.Single().Unit.Tags.Should().BeEmpty();
        else
            processor.Observed.Single().Unit.Tags.Should().BeNull();
    }

    [Test]
    public async Task DiagnosticScopedValidationAndExecutionFaultsPropagate()
    {
        await using var services = Provider();
        var failure = new IOException("Application callback failed.");
        var processors = new[]
        {
            new Probe(services) { Diagnose = (_, _) => Task.FromException(failure) },
            new Probe(services) { Validate = (_, _) => Task.FromException(failure) },
            new Probe(services) { Execute = (_, _) => Task.FromException<Result>(failure) }
        };
        for (var index = 0; index < processors.Length; index++)
        {
            var context = Context(FileProcessingMode.ValidateAndExecute);
            var processor = processors[index];
            Func<Task> run = () => processor.ProcessAsync(_ => Open(index == 0 ? "bad\n" : Fixture("valid.jsonl")), context);
            (await run.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(failure);
            processor.Completed.Should().Be(0);
            processor.ContentFailed.Should().Be(0);
            processor.Failed.Should().BeNull();
            context.Progress.UnitsAttempted.Should().Be(index == 2 ? 1 : 0);
        }
    }

    [TestCase("diagnostic")]
    [TestCase("validation")]
    [TestCase("execution")]
    public async Task CallbackCancellationDoesNotInvokeOutcomeFinalizers(string callback)
    {
        await using var services = Provider();
        using var cancellation = new CancellationTokenSource();
        Task Cancel()
        {
            cancellation.Cancel();
            return Task.FromCanceled(cancellation.Token);
        }

        var processor = new Probe(services)
        {
            Diagnose = callback == "diagnostic" ? (_, _) => Cancel() : null,
            Validate = callback == "validation" ? (_, _) => Cancel() : null,
            Execute = callback == "execution" ? async (_, _) => { await Cancel(); return Result.Success; } : null
        };
        var context = Context(FileProcessingMode.ValidateAndExecute);
        Func<Task> run = () => processor.ProcessAsync(_ => Open(callback == "diagnostic" ? "bad\n" : Fixture("valid.jsonl")), context, cancellation.Token);
        await run.Should().ThrowAsync<OperationCanceledException>();
        context.Progress.UnitsAttempted.Should().Be(callback == "execution" ? 1 : 0);
        processor.Completed.Should().Be(0);
        processor.ContentFailed.Should().Be(0);
        processor.Failed.Should().BeNull();
    }

    [Test]
    public async Task BusinessFailureTakesPrecedenceOverEarlierCollectedContentError()
    {
        await using var services = Provider();
        var failure = Result.Fail(new BusinessException("Application rule."));
        var processor = new Probe(services) { Execute = (_, _) => Task.FromResult(failure) };
        var summary = await processor.ProcessAsync(_ => Open(Fixture("invalid-child.jsonl")), Context(FileProcessingMode.ValidateAndExecute));
        summary.Outcome.Should().Be(FileProcessingOutcome.ExecutionFailed);
        summary.ValidationCompleted.Should().BeFalse();
        summary.ExecutionResult.Error.Should().BeSameAs(failure.Error);
        processor.Diagnostics.Select(x => x.Code).Should().Equal("file.validation");
        AssertCounts(summary, 2, 1, 2, 1, 1, 0);
        summary.Progress.UnitsAttempted.Should().Be(1);
        processor.Failed?.Info.Ordinal.Should().Be(2);
        processor.ContentFailed.Should().Be(0);
    }

    [Test]
    public async Task EmptyReplayStillOpensTwiceAndDetectedDriftFaultsInsteadOfCompleting()
    {
        await using var services = Provider();
        var processor = new Probe(services);
        var opens = 0;
        var summary = await processor.ProcessAsync(_ => { opens++; return Open(""); }, Context(FileProcessingMode.ValidateThenExecute));
        summary.IsSuccess.Should().BeTrue();
        opens.Should().Be(2);
        processor.Executed.Should().BeEmpty();

        processor = new Probe(services);
        opens = 0;
        var context = Context(FileProcessingMode.ValidateThenExecute);
        Func<Task> run = () => processor.ProcessAsync(_ => Open(++opens == 1 ? Fixture("valid.jsonl") : ""), context);
        await run.Should().ThrowAsync<InvalidDataException>();
        context.Progress.UnitsValidated.Should().Be(2);
        processor.Completed.Should().Be(0);
        processor.ContentFailed.Should().Be(0);
    }

    [Test]
    public void CsvDateTimeOffsetExactConversionDoesNotAssumeLocalTime()
    {
        var convert = CsvFieldConverters.DateTimeOffset("yyyy-MM-dd'T'HH:mm:ssK");
        convert("2026-03-15T10:30:00+10:00", out var value).Should().BeTrue();
        value?.Offset.Should().Be(TimeSpan.FromHours(10));
        convert("2026-03-15T10:30:00", out _).Should().BeFalse();
        convert(" ", out value).Should().BeTrue();
        value.Should().BeNull();
    }
}
