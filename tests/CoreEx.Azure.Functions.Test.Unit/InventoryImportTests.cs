namespace CoreEx.Azure.Functions.Test.Unit;

[TestFixture]
public class InventoryImportTests
{
    [Test]
    public async Task GeneratedHandler_PreservesOriginalFailureTrustedTenantAndStopsFollowingUnits()
    {
        var failure = new BusinessException("Application invariant failed.");
        var service = new RecordingService(failure);
        var transport = new RecordingReportTransport();
        await using var provider = CreateProvider(transport, service);
        await using var scope = provider.CreateAsyncScope();
        var summary = await RunAsync(scope.ServiceProvider, "CommandId,Sku,Quantity\none,A,2\ntwo,A,3\n");
        summary.ExecutionResult.Error.Should().BeSameAs(failure);
        summary.Progress.UnitsAttempted.Should().Be(1);
        service.Calls.Should().Be(1);
        service.Tenants.Should().Equal("trusted-tenant");
        transport.Blocks.Should().Contain(block => block.Contains("ExecutionFailed", StringComparison.Ordinal));
        transport.Finalized.Should().BeTrue();
    }

    [TestCase("", "A", "1")]
    [TestCase("one", "", "1")]
    [TestCase("one", "A", "")]
    [TestCase("one", "A", "0")]
    [TestCase("one", "A", "-1")]
    public async Task GeneratedValidation_RejectsBeforeBusinessExecution(string commandId, string sku, string quantity)
    {
        var service = new RecordingService();
        var transport = new RecordingReportTransport();
        await using var provider = CreateProvider(transport, service);
        await using var scope = provider.CreateAsyncScope();
        var summary = await RunAsync(scope.ServiceProvider, $"CommandId,Sku,Quantity\n{commandId},{sku},{quantity}\n");
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.UnitsAttempted.Should().Be(0);
        service.Calls.Should().Be(0);
        transport.Finalized.Should().BeTrue();
    }

    [Test]
    public async Task BlobAdapter_PinsBothReadsPropagatesCancellationAndRemovesCredentialsFromIdentity()
    {
        var service = new RecordingService();
        var transport = new RecordingReportTransport();
        var blob = new PinnedBlob("CommandId,Sku,Quantity\none,A,2\n");
        await using var provider = CreateProvider(transport, service);
        await using var scope = provider.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource();
        await scope.ServiceProvider.GetRequiredService<InventoryBlobImport>().ProcessAsync(blob, "component", cancellation.Token);
        blob.Conditions.Should().Equal(new ETag("\"selected\""), new ETag("\"selected\""));
        blob.PropertyTokens.Should().Equal(cancellation.Token);
        blob.ReadTokens.Should().Equal(cancellation.Token, cancellation.Token);
        blob.Streams.Should().OnlyContain(stream => stream.Disposed);
        service.Calls.Should().Be(1);
        string.Join("", transport.Blocks).Should().Contain("https://example.invalid/inventory/A.csv").And.NotContain("private-sas");
    }

    [Test]
    public async Task BlobRevisionChangedBetweenPasses_IsOperationalAndDoesNotFinalizeReportOrExecute()
    {
        var service = new RecordingService();
        var transport = new RecordingReportTransport();
        var blob = new PinnedBlob("CommandId,Sku,Quantity\none,A,2\n") { FailRead = 2 };
        await using var provider = CreateProvider(transport, service);
        await using var scope = provider.CreateAsyncScope();
        Func<Task> import = () => scope.ServiceProvider.GetRequiredService<InventoryBlobImport>().ProcessAsync(blob, "changed");
        await import.Should().ThrowAsync<RequestFailedException>();
        service.Calls.Should().Be(0);
        transport.Finalized.Should().BeFalse();
    }

    [Test]
    public async Task WrongRoute_IsRejectedBeforeOpeningInputOrReport()
    {
        var transport = new RecordingReportTransport();
        await using var provider = CreateProvider(transport, new RecordingService());
        await using var scope = provider.CreateAsyncScope();
        var opened = false;
        Func<Task> run = () => scope.ServiceProvider.GetRequiredService<InventoryImportRunner>().RunAsync(_ =>
        {
            opened = true;
            return Task.FromResult<Stream>(new MemoryStream());
        }, new("other", "1", "source", "attempt", "revision"));
        await run.Should().ThrowAsync<ArgumentException>();
        opened.Should().BeFalse();
        transport.Requests.Should().Be(0);
    }

    [Test]
    public async Task Cancellation_PropagatesWithoutFinalizingOrExecuting()
    {
        var service = new RecordingService();
        var transport = new RecordingReportTransport();
        await using var provider = CreateProvider(transport, service);
        await using var scope = provider.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource();
        Func<Task> run = () => scope.ServiceProvider.GetRequiredService<InventoryImportRunner>().RunAsync(ct =>
        {
            cancellation.Cancel();
            return Task.FromCanceled<Stream>(ct);
        }, Identity("cancelled"), cancellation.Token);
        await run.Should().ThrowAsync<OperationCanceledException>();
        service.Calls.Should().Be(0);
        transport.Finalized.Should().BeFalse();
    }

    [Test]
    public void BlobRoute_UsesAnInputOnlyPathAndNativeEventGridBinding()
    {
        var parameter = typeof(InventoryBlobFunctions).GetMethod(nameof(InventoryBlobFunctions.ImportInventoryFile))?.GetParameters()[0];
        var trigger = parameter?.GetCustomAttributes(typeof(BlobTriggerAttribute), false).Cast<BlobTriggerAttribute>().Single();
        trigger.Should().NotBeNull();
        trigger.BlobPath.Should().Be(InventoryBlobFunctions.InputPath);
        trigger.Connection.Should().Be("ImportStorage");
        trigger.Source.Should().Be(BlobTriggerSource.EventGrid);
    }

    [Test]
    public void Identity_SeparatesReportAttemptsButKeepsDurableCommandKeysStable()
    {
        var first = Identity("one");
        var retry = Identity("two");
        InventoryImportIdentity.ReportName(first).Should().NotBe(InventoryImportIdentity.ReportName(retry));
        InventoryImportIdentity.UnitKey(first, "tenant", "command").Should().Be(InventoryImportIdentity.UnitKey(retry, "tenant", "command"));
        InventoryImportIdentity.UnitKey(first, "tenant", "command").Should().NotBe(InventoryImportIdentity.UnitKey(first, "other-tenant", "command"));
        var changedRevision = new FileProcessingIdentity(first.DefinitionName, first.DefinitionVersion, first.SourceId, "one", "other-revision");
        InventoryImportIdentity.UnitKey(first, "tenant", "command").Should().NotBe(InventoryImportIdentity.UnitKey(changedRevision, "tenant", "command"));
    }

    private static FileProcessingIdentity Identity(string attempt)
        => new(InventoryProcessor.DefinitionName, InventoryProcessor.DefinitionVersion, "local:test-feed", attempt, "immutable-revision");

    private static Task<FileProcessingSummary> RunAsync(IServiceProvider provider, string csv)
        => provider.GetRequiredService<InventoryImportRunner>().RunAsync(_ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(csv))), Identity("component"));

    private static ServiceProvider CreateProvider(RecordingReportTransport transport, RecordingService service)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddExecutionContext();
        services.AddInventoryImport(_ => new("Server=unused;Database=unused;Integrated Security=true", "inventory-results", "trusted-tenant"));
        services.AddDynamicServicesUsing<InventoryProcessor>();
        services.AddSingleton(new BlobServiceClient(new Uri("https://example.invalid"), new BlobClientOptions { Transport = transport }));
        services.AddScoped<IInventoryImportService>(_ => service);
        return services.BuildServiceProvider();
    }

    private sealed class RecordingService(Exception? failure = null) : IInventoryImportService
    {
        public int Calls { get; private set; }
        public List<string?> Tenants { get; } = [];

        public Task<Result> ApplyAsync(InventoryAdjustment adjustment, FileProcessingIdentity identity, CancellationToken cancellationToken = default)
        {
            Calls++;
            Tenants.Add(CoreEx.ExecutionContext.Current.TenantId);
            return Task.FromResult(failure is null ? Result.Success : new Result(failure));
        }
    }

    private sealed class RecordingReportTransport : HttpPipelineTransport
    {
        public List<string> Blocks { get; } = [];
        public bool Finalized { get; private set; }
        public int Requests { get; private set; }
        public override Request CreateRequest() => HttpClientTransport.Shared.CreateRequest();
        public override void Process(HttpMessage message) => ProcessAsync(message).AsTask().GetAwaiter().GetResult();

        public override async ValueTask ProcessAsync(HttpMessage message)
        {
            Requests++;
            message.CancellationToken.ThrowIfCancellationRequested();
            var query = message.Request.Uri.Query;
            if (query.Contains("comp=appendblock", StringComparison.Ordinal) && message.Request.Content is not null)
            {
                using var buffer = new MemoryStream();
                await message.Request.Content.WriteToAsync(buffer, message.CancellationToken);
                Blocks.Add(Encoding.UTF8.GetString(buffer.ToArray()));
            }

            Finalized |= query.Contains("comp=metadata", StringComparison.Ordinal);
            message.Response = new TestResponse(Finalized ? 200 : 201);
        }
    }

    private sealed class PinnedBlob(string content) : BlobClient
    {
        public List<ETag?> Conditions { get; } = [];
        public List<CancellationToken> PropertyTokens { get; } = [];
        public List<CancellationToken> ReadTokens { get; } = [];
        public List<ObservedStream> Streams { get; } = [];
        public int? FailRead { get; init; }
        public override Uri Uri => new("https://example.invalid/inventory/A.csv?sig=private-sas");

        public override Task<Response<BlobProperties>> GetPropertiesAsync(BlobRequestConditions? conditions = null, CancellationToken cancellationToken = default)
        {
            PropertyTokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobProperties(eTag: new ETag("\"selected\"")), new TestResponse()));
        }

        public override Task<Response<BlobDownloadStreamingResult>> DownloadStreamingAsync(BlobDownloadOptions? options = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReadTokens.Add(cancellationToken);
            Conditions.Add(options?.Conditions.IfMatch);
            if (Conditions.Count == FailRead)
                throw new RequestFailedException(412, "Selected revision no longer exists.");

            var stream = new ObservedStream(Encoding.UTF8.GetBytes(content));
            Streams.Add(stream);
            return Task.FromResult(Response.FromValue(BlobsModelFactory.BlobDownloadStreamingResult(content: stream), new TestResponse()));
        }
    }

    private sealed class ObservedStream(byte[] content) : MemoryStream(content, writable: false)
    {
        public bool Disposed { get; private set; }
        public override ValueTask DisposeAsync()
        {
            Disposed = true;
            return base.DisposeAsync();
        }
    }

    private sealed class TestResponse(int status = 200) : Response
    {
        public override int Status => status;
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
