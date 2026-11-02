namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class BlobFunctionsTests
{
    [Test]
    public void Invocation_DelegatesOriginalBlobAndTokenWithScopedContext()
    {
        using var test = FunctionTester.Create<BlobFunctions>();
        var service = new RecordingService();
        test.ReplaceSingleton<IBlobImportService>(service);
        var blob = new BlobClient(new Uri("https://example.invalid/imports/file.json"));
        using var cancellation = new CancellationTokenSource();
        test.Trigger<BlobFunctions>().Run(f => f.ImportBlob(blob, cancellation.Token)).AssertSuccess();
        service.Blob.Should().BeSameAs(blob);
        service.Token.Should().Be(cancellation.Token);
        service.InvocationId.Should().NotBeNullOrEmpty();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OperationalFailureAndCancellation_BubbleFromInvocation(bool cancelled)
    {
        using var test = FunctionTester.Create<BlobFunctions>();
        test.ReplaceSingleton<IBlobImportService>(new RecordingService
        {
            Error = cancelled ? new OperationCanceledException() : new RequestFailedException(503, "Storage unavailable.")
        });
        var result = test.Trigger<BlobFunctions>().Run(f => f.ImportBlob(new BlobClient(new Uri("https://example.invalid/imports/file.json")), CancellationToken.None));
        if (cancelled)
            result.AssertException<OperationCanceledException>();
        else
            result.AssertException<RequestFailedException>();
    }

    [Test]
    public async Task RevisionChangedDuringRead_UsesIfMatchAndDoesNotWriteReport()
    {
        var blob = new ChangedBlob();
        var service = new BlobImportService(new BlobServiceClient(new Uri("https://example.invalid")), NullLogger<BlobImportService>.Instance);
        Func<Task> run = () => service.ProcessAsync(blob);
        var error = await run.Should().ThrowAsync<RequestFailedException>();
        error.Which.Status.Should().Be(412);
        blob.Condition.Should().Be(new ETag("\"observed\""));
    }

    [Test]
    public void ReportIdentity_IsStableAndSeparatesNamesAndRevisions()
    {
        BlobImportReport.GetReportName("path/file.json", "\"v1\"").Should().Be(BlobImportReport.GetReportName("path/file.json", "\"v1\""));
        BlobImportReport.GetReportName("path/file.json", "\"v1\"").Should().NotBe(BlobImportReport.GetReportName("path/file.json", "\"v2\""));
        BlobImportReport.GetReportName("other/file.json", "\"v1\"").Should().NotBe(BlobImportReport.GetReportName("path/file.json", "\"v1\""));
        BlobImportReport.GetReportName("path/file.json", "\"v1\"").Length.Should().Be(69);
    }

    private sealed class RecordingService : IBlobImportService
    {
        public BlobClient? Blob { get; private set; }
        public CancellationToken Token { get; private set; }
        public string? InvocationId { get; private set; }
        public Exception? Error { get; init; }

        public Task ProcessAsync(BlobClient blob, CancellationToken cancellationToken = default)
        {
            Blob = blob;
            Token = cancellationToken;
            InvocationId = CoreEx.ExecutionContext.Current.Attributes["invocation-id"] as string;
            return Error is null ? Task.CompletedTask : Task.FromException(Error);
        }
    }

    private sealed class ChangedBlob : BlobClient
    {
        public ETag? Condition { get; private set; }
        public override string Name => "file.json";

        public override Task<Response<BlobProperties>> GetPropertiesAsync(BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default)
            => Task.FromResult(Response.FromValue(BlobsModelFactory.BlobProperties(eTag: new ETag("\"observed\"")), new TestResponse()));

        public override Task<Response<BlobDownloadResult>> DownloadContentAsync(BlobDownloadOptions options, CancellationToken cancellationToken = default)
        {
            Condition = options.Conditions.IfMatch;
            return Task.FromException<Response<BlobDownloadResult>>(new RequestFailedException(412, "The blob was overwritten after its properties were read."));
        }
    }

    private sealed class TestResponse : Response
    {
        public override int Status => 200;
        public override string ReasonPhrase => "OK";
        public override Stream? ContentStream { get; set; }
        public override string ClientRequestId { get; set; } = "";
        public override void Dispose() { }
        protected override bool ContainsHeader(string name) => false;
        protected override IEnumerable<HttpHeader> EnumerateHeaders() => [];
        protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string? value) { value = null; return false; }
        protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string>? values) { values = null; return false; }
    }
}
