namespace CoreEx.UnitTesting.Test.Unit;

public class BlobTestHelpersTests
{
    [Test]
    public async Task FileReportingBridge_StreamsBothFormatsAndCompletesMetadata()
    {
        var container = new RecordingBlobContainerClient();
        var jsonl = new BlobJsonlFileReportAdapter(new CoreEx.Azure.Storage.Blobs.BlobJsonlReport(container.GetAppendBlobClient("report.jsonl")));
        var csv = new BlobCsvFileReportAdapter(
            new CoreEx.Azure.Storage.Blobs.BlobCsvReport(container.GetAppendBlobClient("report.csv"), ["Code"], CoreEx.Azure.Storage.Blobs.CsvFormulaHandling.Reject),
            args => [args.Diagnostic.Code]);
        var processor = new CsvFileProcessor<ReportRecord>(() => new CsvFileMapping<ReportRecord>().MapProperty("Number", x => x.Number, CsvFieldConverters.Nullable<int>()));
        var context = new FileProcessingContext(new("catalog", "1", "source", "attempt", "revision"), FileProcessingMode.ValidateOnly, FileErrorMode.CollectErrors);
        Task<Stream> Open(CancellationToken _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes("Number\ninvalid\n")));

        var result = await FileProcessingReporter.RunAsync(processor, Open, context, new CompositeFileProcessingReport(jsonl, csv));

        result.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        container.Blobs["report.jsonl"].Content.ToString().Should().Contain("\"kind\":\"attempt\"").And.Contain("\"kind\":\"diagnostic\"").And.Contain("\"kind\":\"outcome\"");
        container.Blobs["report.jsonl"].Metadata["outcome"].Should().Be("ContentFailed");
        container.Blobs["report.csv"].Content.ToString().Should().Be("Code\r\ncsv.field.invalid\r\n");
        container.Blobs["report.csv"].Metadata["outcome"].Should().Be("ContentFailed");
    }

    private sealed class ReportRecord
    {
        public int? Number { get; set; }
    }

    [Test]
    public async Task InputFixture_CopiesBytesAndOpensIndependentReplayStreams()
    {
        var bytes = Encoding.UTF8.GetBytes("supplier,sku\nACME,100\n");
        var client = new StubBlobClient(new BinaryData(bytes), new Uri("https://storage.test/imports/catalog.csv"), new ETag("\"revision\""));
        bytes[0] = 0;
        var source = await CoreEx.Azure.Storage.Blobs.BlobReadSource.CreateAsync(client);
        await using var first = await source.OpenReadAsync();
        await using var second = await source.OpenReadAsync();
        first.ReadByte().Should().Be('s');
        second.Position.Should().Be(0);
        using var reader = new StreamReader(second);
        (await reader.ReadToEndAsync()).Should().Be("supplier,sku\nACME,100\n");
        source.ETag.Should().Be(new ETag("\"revision\""));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task FromResource_LoadsEmbeddedBytesAndPreservesSourceIdentity(bool useMarker)
    {
        var uri = new Uri("https://storage.test/imports/catalog.csv");
        var etag = new ETag("\"resource-revision\"");
        var client = useMarker
            ? StubBlobClient.FromResource<BlobTestHelpersTests>("catalog-valid.csv", uri, etag)
            : StubBlobClient.FromResource("catalog-valid.csv", uri, etag);
        var source = await CoreEx.Azure.Storage.Blobs.BlobReadSource.CreateAsync(client);

        await using var stream = await source.OpenReadAsync();
        using var reader = new StreamReader(stream);
        (await reader.ReadToEndAsync()).Should().Be("supplier,sku\nACME,100\n");
        source.SourceUri.Should().Be(uri);
        source.ETag.Should().Be(etag);
    }

    [Test]
    public async Task Reports_CaptureOpaqueContentAndMetadataAcrossClientInstances()
    {
        var container = new RecordingBlobContainerClient();
        var first = container.GetAppendBlobClient("first.jsonl");
        var report = new CoreEx.Azure.Storage.Blobs.BlobJsonlReport(first);
        await report.StartAsync();
        await report.WriteAsync(new { kind = "diagnostic", code = "catalog.duplicate" });
        await report.CompleteAsync(new { kind = "terminal" }, "ContentFailed");
        var recorded = container.Blobs["first.jsonl"];
        recorded.ContentType.Should().Be("application/x-ndjson");
        recorded.Content.ToString().Should().Contain("\"code\":\"catalog.duplicate\"");
        recorded.Blocks.Should().HaveCount(2);
        recorded.Metadata["outcome"].Should().Be("ContentFailed");

        var sameName = new CoreEx.Azure.Storage.Blobs.BlobJsonlReport(container.GetAppendBlobClient("first.jsonl"));
        Func<Task> create = () => sameName.StartAsync();
        (await create.Should().ThrowAsync<RequestFailedException>()).Which.Status.Should().Be(409);
        container.Blobs.Should().ContainSingle();
    }

    [Test]
    public async Task ReportFailureHook_DoesNotRecordAnUnsuccessfulAppend()
    {
        var container = new RecordingBlobContainerClient();
        var report = new CoreEx.Azure.Storage.Blobs.BlobJsonlReport(container.GetAppendBlobClient("failed.jsonl"));
        await report.StartAsync();
        var failure = new IOException("Report write failed.");
        container.BeforeAppend = _ => throw failure;
        Func<Task> write = () => report.WriteAsync(new { message = "not accepted" });
        (await write.Should().ThrowAsync<IOException>()).Which.Should().BeSameAs(failure);
        container.Blobs["failed.jsonl"].Blocks.Should().BeEmpty();
        container.Blobs["failed.jsonl"].Metadata["outcome"].Should().Be("InProgress");
    }

    [Test]
    public async Task CancelledInput_DoesNotInvokeTheReadHook()
    {
        var client = new StubBlobClient(new BinaryData("fixture"));
        var calls = 0;
        client.BeforeRead = () => calls++;
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Func<Task> read = async () => { await using var ignored = (await client.DownloadStreamingAsync(cancellationToken: cancellation.Token)).Value.Content; };
        await read.Should().ThrowAsync<OperationCanceledException>();
        calls.Should().Be(0);
    }
}
