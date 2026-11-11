namespace CoreEx.Azure.Functions.Test.Aspire;

[TestFixture]
[NonParallelizable]
public partial class BlobRuntimeTests : WithAspireTester<Projects.CoreEx_Azure_Functions_Test_AppHost>
{
    private BlobContainerClient _inputs = null!;
    private BlobContainerClient _reports = null!;

    [OneTimeSetUp]
    public async Task StartAsync()
    {
        IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Should().NotContain(endpoint =>
            new[] { 7099, 5682, 5310, 10000, 10001, 10002, 14335 }.Contains(endpoint.Port), "the fixture owns its host and emulators");
        try
        {
            Test.EnableHostDiagnostics();
            await Test.WaitForResourceAsync("functions", TimeSpan.FromMinutes(4));
            await Test.WaitForFunctionsAsync("functions",
                ["ImportBlob", "ImportInventoryFile", "ImportInventoryJson", "ImportInventoryJsonl", "ImportInventoryCsvBatch", "ImportInventoryJsonBatch", "ImportInventoryJsonlBatch"], TimeSpan.FromSeconds(90));
            var storage = new BlobServiceClient("UseDevelopmentStorage=true");
            _inputs = storage.GetBlobContainerClient("imports");
            _reports = storage.GetBlobContainerClient("import-results");
            await _inputs.CreateIfNotExistsAsync();
            await _reports.CreateIfNotExistsAsync();
            await PrepareInventoryAsync(storage);
        }
        catch
        {
            Test.Checkpoint("Blob runtime startup failed.");
            throw;
        }
    }

    [OneTimeTearDown]
    public async Task StopAsync() => await DisposeAsync();

    [TearDown]
    public void CaptureLogs() => Test.Checkpoint("Blob runtime case completed.");

    [TestCase("{\"name\":\"example\"}", "accepted", "example")]
    [TestCase("{}", "rejected", null)]
    [TestCase("{", "rejected", null)]
    public async Task BlobNotification_BindsSdkClientAndPersistsContentOutcome(string content, string status, string? name)
    {
        var blob = _inputs.GetBlobClient($"{Runtime.NewGuid():N}/file.json");
        await blob.UploadAsync(BinaryData.FromString(content));
        var (reportBlob, report) = await SubmitAsync(blob);
        report.BlobName.Should().Be(blob.Name);
        report.Status.Should().Be(status);
        report.Name.Should().Be(name);
        reportBlob.Name.Should().Be(BlobImportReport.GetReportName(blob.Name, report.ETag));
    }

    [Test]
    public async Task DuplicateNotificationAndOverwrite_KeepStableSeparateRevisionReports()
    {
        var blob = _inputs.GetBlobClient($"{Runtime.NewGuid():N}/file.json");
        await blob.UploadAsync(BinaryData.FromString("{\"name\":\"first\"}"));
        var (firstBlob, first) = await SubmitAsync(blob);
        var (_, replay) = await SubmitAsync(blob);
        replay.Should().Be(first);
        await blob.UploadAsync(BinaryData.FromString("{\"name\":\"second\"}"), overwrite: true);
        var (secondBlob, second) = await SubmitAsync(blob);
        second.Name.Should().Be("second");
        second.ETag.Should().NotBe(first.ETag);
        secondBlob.Name.Should().NotBe(firstBlob.Name);
        (await firstBlob.DownloadContentAsync()).Value.Content.ToObjectFromJson<BlobImportReport>(JsonDefaults.SerializerOptions).Should().Be(first);
    }

    [Test]
    public async Task OutputStorageFailure_IsRetriedAndMovedToBlobPoisonQueue()
    {
        await _reports.DeleteAsync();
        try
        {
            var blob = _inputs.GetBlobClient($"{Runtime.NewGuid():N}/operational-failure.json");
            await blob.UploadAsync(BinaryData.FromString("{\"name\":\"example\"}"));
            var properties = (await blob.GetPropertiesAsync()).Value;
            await NotifyAsync(blob, properties);
            var queue = new QueueClient("UseDevelopmentStorage=true", "webjobs-blobtrigger-poison", new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
            var poison = await Test.EventuallyAsync(async token =>
            {
                if (!(await queue.ExistsAsync(token)).Value)
                    return null;

                var messages = await queue.PeekMessagesAsync(32, token);
                return messages.Value.FirstOrDefault(message => message.Body.ToString().Contains(blob.Name, StringComparison.Ordinal));
            }, value => value is not null, timeout: TimeSpan.FromSeconds(90));
            poison.Should().NotBeNull();
            (await _reports.ExistsAsync()).Value.Should().BeFalse();
        }
        finally
        {
            await _reports.CreateIfNotExistsAsync();
        }
    }

    private async Task<(BlobClient Blob, BlobImportReport Report)> SubmitAsync(BlobClient blob)
    {
        var properties = (await blob.GetPropertiesAsync()).Value;
        var reportBlob = _reports.GetBlobClient(BlobImportReport.GetReportName(blob.Name, properties.ETag.ToString()));
        await NotifyAsync(blob, properties);
        var report = await Test.EventuallyAsync(async token =>
        {
            if (!(await reportBlob.ExistsAsync(token)).Value)
                return null;

            return (await reportBlob.DownloadContentAsync(token)).Value.Content.ToObjectFromJson<BlobImportReport>(JsonDefaults.SerializerOptions)
                ?? throw new InvalidOperationException("The report contained JSON null.");
        }, value => value is not null, timeout: TimeSpan.FromSeconds(60));
        report.Should().NotBeNull();
        report!.ETag.Should().Be(properties.ETag.ToString());
        return (reportBlob, report);
    }

    private async Task NotifyAsync(BlobClient blob, BlobProperties properties)
    {
        var notification = EventGridExtensionMethods.CreateBlobCreatedEvent(_inputs.Name, blob.Name, blob.Uri,
            properties.ETag.ToString(), properties.ContentType, properties.ContentLength);
        using var response = await Test.FunctionsWebhook("functions").PostBlobCreatedAsync("Host.Functions.ImportBlob", notification);
        response.IsSuccessStatusCode.Should().BeTrue("the webhook returned {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());
    }
}
