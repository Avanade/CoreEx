namespace CoreEx.Azure.Functions.Test.Aspire;

public partial class BlobRuntimeTests
{
    [Test]
    public async Task CatalogBlobNotification_PublishesBinaryCommandAndCompletesReport()
    {
        var storage = new BlobServiceClient("UseDevelopmentStorage=true");
        var input = storage.GetBlobContainerClient("catalog-imports");
        var reports = storage.GetBlobContainerClient("catalog-import-results");
        await input.CreateIfNotExistsAsync();
        await reports.CreateIfNotExistsAsync();

        var sku = $"SKU-{Runtime.NewGuid():N}";
        var blob = input.GetBlobClient($"{sku}.csv");
        await blob.UploadAsync(BinaryData.FromString($"Sku,UnitPrice\n{sku},0\n"));
        await NotifyInventoryAsync(blob, "ImportCatalog");

        const string connection = "Endpoint=sb://localhost:5682;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        await using var client = new ServiceBusClient(connection);
        await using var receiver = client.CreateReceiver("catalog-item-upserts");
        var message = await Test.EventuallyAsync(async token =>
        {
            var observed = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2), token);
            return observed?.Body.ToString().Contains(sku, StringComparison.Ordinal) == true ? observed : null;
        }, value => value is not null, timeout: TimeSpan.FromSeconds(60));

        message.Should().NotBeNull();
        var envelope = (message ?? throw new InvalidOperationException("The catalog command did not arrive.")).ToCloudEvent();
        envelope.Type.Should().Be("northstar.catalog.item.upsert.v1");
        envelope.Source.Should().Be(new Uri("urn:northstar:supplier:catalog"));
        using var body = JsonDocument.Parse(message.Body.ToMemory());
        body.RootElement.GetProperty("sku").GetString().Should().Be(sku);
        body.RootElement.GetProperty("unitPrice").GetDecimal().Should().Be(0m);
        body.RootElement.GetProperty("commandId").GetString().Should().Be(envelope.Id);
        await receiver.CompleteMessageAsync(message);

        var terminal = await Test.EventuallyAsync(async token =>
        {
            await foreach (var item in reports.GetBlobsAsync(new GetBlobsOptions { Traits = BlobTraits.Metadata }, token))
            {
                if (!item.Metadata.TryGetValue("outcome", out var outcome) || outcome != "Succeeded")
                    continue;

                var content = (await reports.GetBlobClient(item.Name).DownloadContentAsync(token)).Value.Content.ToString();
                if (content.Contains(blob.Name, StringComparison.Ordinal))
                    return content;
            }

            return null;
        }, value => value is not null, timeout: TimeSpan.FromSeconds(60));
        terminal.Should().Contain("\"sendsReturned\":1");
    }
}
