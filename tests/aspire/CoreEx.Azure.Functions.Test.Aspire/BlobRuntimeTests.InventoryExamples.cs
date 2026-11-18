namespace CoreEx.Azure.Functions.Test.Aspire;

public partial class BlobRuntimeTests
{
    [TestCase("inventory-json", "example.json", "ImportInventoryJson")]
    [TestCase("inventory-jsonl", "example.jsonl", "ImportInventoryJsonl")]
    [TestCase("inventory-csv-batch", "example-batch.csv", "ImportInventoryCsvBatch")]
    [TestCase("inventory-json-batch", "example-batch.json", "ImportInventoryJsonBatch")]
    [TestCase("inventory-jsonl-batch", "example-batch.jsonl", "ImportInventoryJsonlBatch")]
    public async Task InventoryFormats_ExecuteGeneratedExamplesAndDeduplicateRetries(string containerName, string example, string functionName)
    {
        var sku = Runtime.NewGuid().ToString("N");
        await SeedInventoryAsync(sku);
        await using var stream = typeof(InventoryProcessor).Assembly.GetManifestResourceStream("CoreEx.Azure.Functions.TestApp.InventoryImports." + example)
            ?? throw new InvalidOperationException("The checked-in inventory example is missing.");
        using var reader = new StreamReader(stream);
        var content = (await reader.ReadToEndAsync()).Replace("DEMO", sku, StringComparison.Ordinal);
        var container = new BlobServiceClient("UseDevelopmentStorage=true").GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient($"{Runtime.NewGuid():N}/{example}");
        await blob.UploadAsync(BinaryData.FromString(content));

        var first = await SubmitInventoryAsync(blob, functionName: functionName);
        first.Summary.GetProperty("outcome").GetString().Should().Be("Succeeded");
        first.Summary.GetProperty("progress").GetProperty("unitsExecuted").GetInt64().Should().Be(2);
        (await InventoryQuantityAsync(sku)).Should().Be(5);
        (await InventoryOutboxCountAsync(sku)).Should().Be(2);

        var retry = await SubmitInventoryAsync(blob, [first.Blob.Name], functionName);
        retry.Blob.Name.Should().NotBe(first.Blob.Name);
        retry.Summary.GetProperty("outcome").GetString().Should().Be("Succeeded");
        (await InventoryQuantityAsync(sku)).Should().Be(5);
        (await InventoryOutboxCountAsync(sku)).Should().Be(2);
    }
}
