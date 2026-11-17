namespace CoreEx.Azure.Storage.Blobs.Test.Unit;

[TestFixture]
public class BlobJsonlReportTests
{
    [Test]
    public async Task Report_IsCreateOnlyCompactConditionalAndExplicitlyFinalized()
    {
        var transport = new RecordingTransport();
        var report = CreateReport(transport, new JsonSerializerOptions { WriteIndented = true });
        await report.StartAsync();
        transport.CreationCondition.Should().Be("*");
        transport.Outcomes.Should().Equal("InProgress");
        transport.ContentType.Should().Be("application/x-ndjson");
        await report.WriteAsync(new { message = "first\nsecond" });
        await report.CompleteAsync(new { kind = "summary" }, "ContentFailed");
        transport.Blocks.Should().Equal("{\"message\":\"first\\nsecond\"}\n", "{\"kind\":\"summary\"}\n");
        transport.AppendPositions.Should().Equal("0", Encoding.UTF8.GetByteCount(transport.Blocks[0]).ToString());
        transport.MatchConditions.Should().Equal("\"1\"", "\"2\"", "\"3\"");
        transport.Outcomes.Should().Equal("InProgress", "ContentFailed");
        Func<Task> lateWrite = () => report.WriteAsync(new { late = true });
        await lateWrite.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FailedAppendOrFinalization_FaultsInstanceWithoutBlindRetry(bool failFinalization)
    {
        var transport = new RecordingTransport();
        var report = CreateReport(transport);
        await report.StartAsync();
        transport.FailAppend = !failFinalization;
        transport.FailMetadata = failFinalization;
        Func<Task> complete = () => report.CompleteAsync(new { kind = "summary" }, "Succeeded");
        await complete.Should().ThrowAsync<RequestFailedException>();
        transport.Outcomes.Should().Equal("InProgress");
        var requests = transport.Requests;
        await complete.Should().ThrowAsync<InvalidOperationException>();
        transport.Requests.Should().Be(requests);
    }

    [Test]
    public async Task CreationConflict_DoesNotAppendOrOverwrite()
    {
        var transport = new RecordingTransport { FailCreate = true };
        var report = CreateReport(transport);
        Func<Task> start = () => report.StartAsync();
        (await start.Should().ThrowAsync<RequestFailedException>()).Which.Status.Should().Be(409);
        Func<Task> write = () => report.WriteAsync(new { unexpected = true });
        await write.Should().ThrowAsync<InvalidOperationException>();
        transport.Blocks.Should().BeEmpty();
        transport.Requests.Should().Be(1);
    }

    [Test]
    public async Task Cancellation_LeavesAttemptUnfinishedAndPreventsReuse()
    {
        var transport = new RecordingTransport();
        var report = CreateReport(transport);
        await report.StartAsync();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> write = () => report.WriteAsync(new { cancelled = true }, cancellation.Token);
        await write.Should().ThrowAsync<OperationCanceledException>();
        Func<Task> complete = () => report.CompleteAsync(new { kind = "summary" }, "Succeeded");
        await complete.Should().ThrowAsync<InvalidOperationException>();
        transport.Outcomes.Should().Equal("InProgress");
    }

    [Test]
    public async Task ConcurrentOperations_AreRejectedWithoutCorruptingTheActiveWrite()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new RecordingTransport { OnAppend = () => { entered.SetResult(); return release.Task; } };
        var report = CreateReport(transport);
        await report.StartAsync();
        var active = report.WriteAsync(new { first = true });
        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Func<Task> overlap = () => report.WriteAsync(new { overlapping = true });
            await overlap.Should().ThrowAsync<InvalidOperationException>();
        }
        finally
        {
            release.TrySetResult();
            await active;
        }

        transport.OnAppend = null;
        await report.CompleteAsync(new { kind = "summary" }, "Succeeded");
        transport.Blocks.Should().HaveCount(2);
    }

    [Test]
    public void Names_FrameComponentsAndIsolateAttemptsAndRevisions()
    {
        BlobJsonlReport.CreateName("ab", "c").Should().NotBe(BlobJsonlReport.CreateName("a", "bc"));
        BlobJsonlReport.CreateName("source", "revision", "one").Should().NotBe(BlobJsonlReport.CreateName("source", "revision", "two"));
        BlobJsonlReport.CreateName("source", "revision", "one").Should().NotBe(BlobJsonlReport.CreateName("source", "other", "one"));
    }

    private static BlobJsonlReport CreateReport(RecordingTransport transport, JsonSerializerOptions? options = null)
    {
        var clientOptions = new BlobClientOptions { Transport = transport };
        clientOptions.Retry.MaxRetries = 0;
        return new BlobJsonlReport(new AppendBlobClient(new Uri("https://example.invalid/reports/attempt.jsonl"), clientOptions), options);
    }
}
