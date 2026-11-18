namespace CoreEx.Azure.Functions.Test.Unit;

public partial class InventoryImportTests
{
    [TestCase("Inventory", "example.csv")]
    [TestCase("InventoryJson", "example.json")]
    [TestCase("InventoryJsonl", "example.jsonl")]
    [TestCase("InventoryCsvBatch", "example-batch.csv")]
    [TestCase("InventoryJsonBatch", "example-batch.json")]
    [TestCase("InventoryJsonlBatch", "example-batch.jsonl")]
    public async Task NativeFunctions_ProcessCheckedInExamplesWithPinnedReplayAndScopedBusinessDelegation(string definition, string example)
    {
        var service = new RecordingService();
        var transport = new RecordingReportTransport();
        var blob = new PinnedBlob(await ReadExampleAsync(example));
        using var cancellation = new CancellationTokenSource();
        using var test = FunctionTester.Create<InventoryBlobFunctions>(additionalConfiguration: new Dictionary<string, string?>
        {
            ["InventoryImportDatabase"] = "Server=unused;Database=unused;Integrated Security=true",
            ["InventoryImportReports"] = "inventory-results",
            ["InventoryImportTenant"] = "trusted-tenant",
            ["ImportStorage"] = "UseDevelopmentStorage=true"
        });
        test.ReplaceSingleton<IInventoryImportService>(service);
        test.ReplaceSingleton(new BlobServiceClient(new Uri("https://example.invalid"), new BlobClientOptions { Transport = transport }));

        var invocation = definition switch
        {
            "Inventory" => test.Trigger<InventoryBlobFunctions>().Run(f => f.ImportInventoryFile(blob, cancellation.Token)),
            "InventoryJson" => test.Trigger<InventoryBlobFunctions>().Run(f => f.ImportInventoryJson(blob, cancellation.Token)),
            "InventoryJsonl" => test.Trigger<InventoryBlobFunctions>().Run(f => f.ImportInventoryJsonl(blob, cancellation.Token)),
            "InventoryCsvBatch" => test.Trigger<InventoryBlobFunctions>().Run(f => f.ImportInventoryCsvBatch(blob, cancellation.Token)),
            "InventoryJsonBatch" => test.Trigger<InventoryBlobFunctions>().Run(f => f.ImportInventoryJsonBatch(blob, cancellation.Token)),
            "InventoryJsonlBatch" => test.Trigger<InventoryBlobFunctions>().Run(f => f.ImportInventoryJsonlBatch(blob, cancellation.Token)),
            _ => throw new ArgumentOutOfRangeException(nameof(definition))
        };
        invocation.AssertSuccess();
        service.Calls.Should().Be(2, "metadata and child items are not independently executed");
        service.Quantities.Should().Equal(2, 3);
        service.Tenants.Should().Equal("trusted-tenant", "trusted-tenant");
        blob.Conditions.Should().Equal(new ETag("\"selected\""), new ETag("\"selected\""));
        blob.ReadTokens.Should().Equal(cancellation.Token, cancellation.Token);
        blob.Streams.Should().OnlyContain(stream => stream.Disposed);
        transport.Finalized.Should().BeTrue();
        using var summary = JsonDocument.Parse(transport.Blocks[^1]);
        summary.RootElement.GetProperty("identity").GetProperty("definitionName").GetString().Should().Be(definition);
        summary.RootElement.GetProperty("progress").GetProperty("unitsExecuted").GetInt64().Should().Be(2);
    }

    [TestCase("InventoryJson", "example.json")]
    [TestCase("InventoryJsonl", "example.jsonl")]
    [TestCase("InventoryCsvBatch", "example-batch.csv")]
    [TestCase("InventoryJsonBatch", "example-batch.json")]
    [TestCase("InventoryJsonlBatch", "example-batch.jsonl")]
    public async Task LateInvalidQuantity_PreventsAllReplayExecution(string definition, string example)
    {
        var content = await ReadExampleAsync(example);
        var invalid = content.Replace(",3\n", ",0\n", StringComparison.Ordinal)
            .Replace("\"Quantity\": 3", "\"Quantity\": 0", StringComparison.Ordinal)
            .Replace("\"Quantity\":3", "\"Quantity\":0", StringComparison.Ordinal);
        invalid.Should().NotBe(content);
        var service = new RecordingService();
        var transport = new RecordingReportTransport();
        await using var provider = CreateProvider(transport, service);
        await using var scope = provider.CreateAsyncScope();
        var opens = 0;
        var summary = await scope.ServiceProvider.GetRequiredService<InventoryImportRunner>().RunAsync(_ =>
        {
            opens++;
            return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(invalid)));
        }, new(definition, "1", "source", "attempt", "revision"));
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.UnitsAttempted.Should().Be(0);
        service.Calls.Should().Be(0);
        opens.Should().Be(1);
        transport.Finalized.Should().BeTrue();
    }

    [TestCase("InventoryCsvBatch", "example-batch.csv", false)]
    [TestCase("InventoryCsvBatch", "example-batch.csv", true)]
    [TestCase("InventoryJsonBatch", "example-batch.json", false)]
    [TestCase("InventoryJsonBatch", "example-batch.json", true)]
    [TestCase("InventoryJsonlBatch", "example-batch.jsonl", false)]
    [TestCase("InventoryJsonlBatch", "example-batch.jsonl", true)]
    public async Task TrailerReconciliation_RejectsWrongCountsOrTotalsBeforeExecution(string definition, string example, bool wrongCount)
    {
        var content = await ReadExampleAsync(example);
        var invalid = wrongCount
            ? content.Replace("T,2,5", "T,3,5", StringComparison.Ordinal).Replace("\"AdjustmentCount\": 2", "\"AdjustmentCount\": 3", StringComparison.Ordinal).Replace("\"AdjustmentCount\":2", "\"AdjustmentCount\":3", StringComparison.Ordinal)
            : content.Replace("T,2,5", "T,2,6", StringComparison.Ordinal).Replace("\"QuantityTotal\": 5", "\"QuantityTotal\": 6", StringComparison.Ordinal).Replace("\"QuantityTotal\":5", "\"QuantityTotal\":6", StringComparison.Ordinal);
        invalid.Should().NotBe(content);
        var service = new RecordingService();
        var transport = new RecordingReportTransport();
        await using var provider = CreateProvider(transport, service);
        await using var scope = provider.CreateAsyncScope();
        var summary = await scope.ServiceProvider.GetRequiredService<InventoryImportRunner>().RunAsync(
            _ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(invalid))), new(definition, "1", "source", "attempt", "revision"));
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.UnitsValidated.Should().Be(2);
        summary.Progress.UnitsAttempted.Should().Be(0);
        summary.Progress.ErrorCount.Should().Be(1);
        service.Calls.Should().Be(0);
        string.Join("", transport.Blocks).Should().Contain("inventory.trailer.mismatch");
    }

    [TestCase("InventoryCsvBatch", "example-batch.csv")]
    [TestCase("InventoryJsonBatch", "example-batch.json")]
    [TestCase("InventoryJsonlBatch", "example-batch.jsonl")]
    public async Task RequiredMetadataAndChildren_AreNotOptional(string definition, string example)
    {
        var content = await ReadExampleAsync(example);
        var invalid = definition switch
        {
            "InventoryCsvBatch" => content.Replace("L,east,1\nL,west,1\n", "", StringComparison.Ordinal),
            "InventoryJsonBatch" => content.Replace("[{ \"Bin\": \"east\", \"Quantity\": 1 }, { \"Bin\": \"west\", \"Quantity\": 1 }]", "[]", StringComparison.Ordinal),
            "InventoryJsonlBatch" => content.Replace("[{\"Bin\":\"east\",\"Quantity\":1},{\"Bin\":\"west\",\"Quantity\":1}]", "[]", StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(definition))
        };
        invalid.Should().NotBe(content);
        var withoutTrailer = definition switch
        {
            "InventoryCsvBatch" => content.Replace("T,2,5\n", "", StringComparison.Ordinal),
            "InventoryJsonBatch" => content.Replace(",\n  \"Trailer\": { \"AdjustmentCount\": 2, \"QuantityTotal\": 5 }", "", StringComparison.Ordinal),
            _ => content.Replace("{\"type\":\"trailer\",\"AdjustmentCount\":2,\"QuantityTotal\":5}\n", "", StringComparison.Ordinal)
        };
        var withoutHeader = definition switch
        {
            "InventoryCsvBatch" => content.Replace("H,batch-one\n", "", StringComparison.Ordinal),
            "InventoryJsonBatch" => content.Replace("  \"Header\": { \"BatchId\": \"batch-one\" },\n", "", StringComparison.Ordinal),
            _ => content.Replace("{\"type\":\"header\",\"BatchId\":\"batch-one\"}\n", "", StringComparison.Ordinal)
        };
        foreach (var input in new[] { invalid, withoutTrailer, withoutHeader })
        {
            input.Should().NotBe(content);
            var service = new RecordingService();
            await using var provider = CreateProvider(new RecordingReportTransport(), service);
            await using var scope = provider.CreateAsyncScope();
            var summary = await scope.ServiceProvider.GetRequiredService<InventoryImportRunner>().RunAsync(
                _ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(input))), new(definition, "1", "source", "attempt", "revision"));
            summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
            service.Calls.Should().Be(0);
        }
    }

    [TestCase("InventoryCsvBatch", "example-batch.csv")]
    [TestCase("InventoryJsonBatch", "example-batch.json")]
    [TestCase("InventoryJsonlBatch", "example-batch.jsonl")]
    public async Task CombinedChildQuantity_IsValidatedBeforeIntegerProjection(string definition, string example)
    {
        var content = (await ReadExampleAsync(example)).Replace(",1\n", ",2147483647\n", StringComparison.Ordinal)
            .Replace("\"Quantity\": 1", "\"Quantity\": 2147483647", StringComparison.Ordinal)
            .Replace("\"Quantity\":1", "\"Quantity\":2147483647", StringComparison.Ordinal);
        var service = new RecordingService();
        var transport = new RecordingReportTransport();
        await using var provider = CreateProvider(transport, service);
        await using var scope = provider.CreateAsyncScope();
        var summary = await scope.ServiceProvider.GetRequiredService<InventoryImportRunner>().RunAsync(
            _ => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(content))), new(definition, "1", "source", "attempt", "revision"));
        summary.Outcome.Should().Be(FileProcessingOutcome.ContentFailed);
        summary.Progress.UnitsAttempted.Should().Be(0);
        service.Calls.Should().Be(0);
        string.Join("", transport.Blocks).Should().Contain("inventory.quantity.overflow");
    }

    private static async Task<string> ReadExampleAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var stream = typeof(InventoryProcessor).Assembly.GetManifestResourceStream("CoreEx.Azure.Functions.TestApp.InventoryImports." + name)
            ?? throw new InvalidOperationException("The checked-in inventory example is missing.");
        using var reader = new StreamReader(stream);
        return (await reader.ReadToEndAsync(cancellationToken)).Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}
