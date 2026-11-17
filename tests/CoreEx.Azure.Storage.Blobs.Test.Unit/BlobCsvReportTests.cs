namespace CoreEx.Azure.Storage.Blobs.Test.Unit;

[TestFixture]
public class BlobCsvReportTests
{
    [Test]
    public async Task Report_CopiesColumnsEscapesFieldsAndCompletesWithoutASummaryRow()
    {
        var transport = new RecordingTransport();
        string[] columns = ["supplier", "message", "quantity", "optional"];
        var report = new BlobCsvReport(CreateClient(transport), columns, CsvFormulaHandling.Reject);
        columns[0] = "changed";
        await report.StartAsync();
        await report.WriteAsync(["ACME, Ltd", "A \"quoted\" message\r\nwith another line", 2, null]);
        await report.CompleteAsync("ContentFailed");
        transport.CreationCondition.Should().Be("*");
        transport.ContentType.Should().Be("text/csv; charset=utf-8");
        transport.Blocks.Should().Equal("supplier,message,quantity,optional\r\n", "\"ACME, Ltd\",\"A \"\"quoted\"\" message\r\nwith another line\",2,\r\n");
        transport.AppendPositions.Should().Equal("0", Encoding.UTF8.GetByteCount(transport.Blocks[0]).ToString());
        transport.MatchConditions.Should().Equal("\"1\"", "\"2\"", "\"3\"");
        transport.Outcomes.Should().Equal("InProgress", "ContentFailed");
        Func<Task> lateRow = () => report.WriteAsync(["unexpected", "", 0, null]);
        await lateRow.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Scalars_AreFormattedInvariantlyAndUnicodeIsPreserved()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var transport = new RecordingTransport();
            var report = new BlobCsvReport(CreateClient(transport), ["amount", "description", "enabled"], CsvFormulaHandling.Reject);
            await report.StartAsync();
            await report.WriteAsync([1.25m, "caf\u00e9", true]);
            transport.Blocks[1].Should().Be("1.25,caf\u00e9,True\r\n");
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }

    [TestCase("=1+1")]
    [TestCase(" +1")]
    [TestCase("-1")]
    [TestCase("@command")]
    [TestCase("\ttext")]
    [TestCase(" \rtext")]
    [TestCase("\ntext")]
    public async Task FormulaPolicy_IsExplicitAndAppliedBeforeAppending(string value)
    {
        foreach (var policy in new[] { CsvFormulaHandling.Preserve, CsvFormulaHandling.PrefixApostrophe, CsvFormulaHandling.Reject })
        {
            var transport = new RecordingTransport();
            var report = new BlobCsvReport(CreateClient(transport), ["value"], policy);
            await report.StartAsync();
            if (policy == CsvFormulaHandling.Reject)
            {
                Func<Task> write = () => report.WriteAsync([value]);
                await write.Should().ThrowAsync<ArgumentException>();
                transport.Blocks.Should().Equal("value\r\n");
                Func<Task> complete = () => report.CompleteAsync("Succeeded");
                await complete.Should().ThrowAsync<InvalidOperationException>();
            }
            else
            {
                await report.WriteAsync([value]);
                var expected = policy == CsvFormulaHandling.PrefixApostrophe ? "'" + value : value;
                if (expected.Contains('\r') || expected.Contains('\n'))
                    expected = "\"" + expected + "\"";

                transport.Blocks[1].Should().Be(expected + "\r\n");
            }
        }
    }

    [Test]
    public async Task FormulaPolicy_AlsoRejectsUnsafeHeadersBeforeCreatingABlob()
    {
        var transport = new RecordingTransport();
        var report = new BlobCsvReport(CreateClient(transport), ["=unsafe"], CsvFormulaHandling.Reject);
        Func<Task> start = () => report.StartAsync();
        await start.Should().ThrowAsync<ArgumentException>();
        transport.Requests.Should().Be(0);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task InvalidRow_DoesNotAppendPartialValues(bool unsupportedType)
    {
        var transport = new RecordingTransport();
        var report = new BlobCsvReport(CreateClient(transport), ["value", "other"], CsvFormulaHandling.Reject);
        await report.StartAsync();
        Func<Task> write = () => report.WriteAsync(unsupportedType ? ["valid", new object()] : ["wrong-width"]);
        await write.Should().ThrowAsync<ArgumentException>();
        transport.Blocks.Should().Equal("value,other\r\n");
    }

    [Test]
    public async Task HeaderAppendFailure_LeavesInProgressAndPreventsDataOrFinalization()
    {
        var transport = new RecordingTransport { FailAppend = true };
        var report = new BlobCsvReport(CreateClient(transport), ["value"], CsvFormulaHandling.Reject);
        Func<Task> start = () => report.StartAsync();
        await start.Should().ThrowAsync<RequestFailedException>();
        transport.Outcomes.Should().Equal("InProgress");
        transport.FailAppend = false;
        Func<Task> write = () => report.WriteAsync(["data"]);
        Func<Task> complete = () => report.CompleteAsync("Succeeded");
        await write.Should().ThrowAsync<InvalidOperationException>();
        await complete.Should().ThrowAsync<InvalidOperationException>();
        transport.Blocks.Should().BeEmpty();
    }

    [Test]
    public async Task InvalidUnicode_DoesNotSilentlyReplaceOrAppendTheValue()
    {
        var transport = new RecordingTransport();
        var report = new BlobCsvReport(CreateClient(transport), ["value"], CsvFormulaHandling.Reject);
        await report.StartAsync();
        Func<Task> write = () => report.WriteAsync(["unpaired-surrogate:\uD800"]);
        await write.Should().ThrowAsync<EncoderFallbackException>();
        transport.Blocks.Should().Equal("value\r\n");
    }

    [Test]
    public async Task CancelledCompletion_DoesNotPublishATerminalOutcome()
    {
        var transport = new RecordingTransport();
        var report = new BlobCsvReport(CreateClient(transport), ["value"], CsvFormulaHandling.Reject);
        await report.StartAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> complete = () => report.CompleteAsync("Succeeded", cancellation.Token);
        await complete.Should().ThrowAsync<OperationCanceledException>();
        transport.Outcomes.Should().Equal("InProgress");
        transport.Blocks.Should().Equal("value\r\n");
    }

    private static AppendBlobClient CreateClient(RecordingTransport transport)
    {
        var options = new BlobClientOptions { Transport = transport };
        options.Retry.MaxRetries = 0;
        return new AppendBlobClient(new Uri("https://example.invalid/reports/attempt.csv"), options);
    }
}
