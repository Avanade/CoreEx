namespace CoreEx.Azure.Functions.Test.Aspire;

public partial class BlobRuntimeTests
{
    private const string InventoryConnection = "Server=tcp:127.0.0.1,14335;Database=CoreExInventory;User Id=sa;Password=CoreEx_Emulator!2026;TrustServerCertificate=True;Connect Timeout=10";
    private BlobContainerClient _inventoryInputs = null!;
    private BlobContainerClient _inventoryReports = null!;

    private async Task PrepareInventoryAsync(BlobServiceClient storage)
    {
        await using (var master = new SqlConnection(new SqlConnectionStringBuilder(InventoryConnection) { InitialCatalog = "master" }.ToString()))
        {
            await master.OpenAsync();
            await using var command = master.CreateCommand();
            command.CommandText = "CREATE DATABASE CoreExInventory";
            await command.ExecuteNonQueryAsync();
        }

        await using var script = typeof(BlobRuntimeTests).Assembly.GetManifestResourceStream("CoreEx.Azure.Functions.Test.Aspire.InventorySchema.sql")
            ?? throw new InvalidOperationException("The inventory schema fixture is missing.");
        using var reader = new StreamReader(script);
        foreach (var batch in (await reader.ReadToEndAsync()).Replace("\r\n", "\n").Split("\nGO\n", StringSplitOptions.RemoveEmptyEntries))
            await InventoryExecuteAsync(batch);

        _inventoryInputs = storage.GetBlobContainerClient("inventory-adjustments");
        _inventoryReports = storage.GetBlobContainerClient("inventory-results");
        await _inventoryInputs.CreateIfNotExistsAsync();
        await _inventoryReports.CreateIfNotExistsAsync();
    }

    [Test]
    public async Task InventoryCsv_GeneratedProcessorExecutesWithRealSqlOutboxAndIsolatedBlobReports()
    {
        var sku = Runtime.NewGuid().ToString("N");
        await SeedInventoryAsync(sku);
        var blob = await UploadInventoryAsync($"CommandId,Sku,Quantity\none,{sku},2\ntwo,{sku},3\n");
        var first = await SubmitInventoryAsync(blob);
        first.Summary.GetProperty("outcome").GetString().Should().Be("Succeeded");
        first.Summary.GetProperty("progress").GetProperty("unitsExecuted").GetInt64().Should().Be(2);
        (await InventoryQuantityAsync(sku)).Should().Be(5);
        (await InventoryOutboxCountAsync(sku)).Should().Be(2);

        var duplicate = await SubmitInventoryAsync(blob, [first.Blob.Name]);
        duplicate.Blob.Name.Should().NotBe(first.Blob.Name);
        duplicate.Summary.GetProperty("outcome").GetString().Should().Be("Succeeded");
        (await InventoryQuantityAsync(sku)).Should().Be(5);
        (await InventoryOutboxCountAsync(sku)).Should().Be(2);
        var identity = JsonSerializer.Deserialize<FileProcessingIdentity>(first.Summary.GetProperty("identity"), JsonDefaults.SerializerOptions)
            ?? throw new InvalidDataException("The report identity is missing.");
        first.Blob.Name.Should().Be(InventoryImportIdentity.ReportName(identity));
        identity.SourceId.Should().Be(blob.Uri.AbsoluteUri);
        identity.SourceRevision.Should().Be((await blob.GetPropertiesAsync()).Value.ETag.ToString());
        (await InventoryScalarAsync("SELECT COUNT(DISTINCT ScopeId) FROM Inventory.Receipts WHERE UnitKey IN (@one, @two)",
            new SqlParameter("@one", InventoryImportIdentity.UnitKey(identity, "runtime-tenant", "one")),
            new SqlParameter("@two", InventoryImportIdentity.UnitKey(identity, "runtime-tenant", "two")))).Should().Be(2);
        await blob.UploadAsync(BinaryData.FromString($"CommandId,Sku,Quantity\none,{sku},4\n"), overwrite: true);
        var changed = await SubmitInventoryAsync(blob);
        changed.Blob.Name.Should().NotBe(first.Blob.Name).And.NotBe(duplicate.Blob.Name);
        changed.Summary.GetProperty("identity").GetProperty("sourceRevision").GetString().Should().NotBe(identity.SourceRevision);
        (await InventoryQuantityAsync(sku)).Should().Be(9);
        (await InventoryOutboxCountAsync(sku)).Should().Be(3);
    }

    [Test]
    public async Task InventoryCsv_LateInvalidContentProducesBlobDiagnosticsAndZeroWrites()
    {
        var sku = Runtime.NewGuid().ToString("N");
        await SeedInventoryAsync(sku);
        var blob = await UploadInventoryAsync($"CommandId,Sku,Quantity\none,{sku},2\ntwo,{sku},private-invalid-value\n");
        var report = await SubmitInventoryAsync(blob);
        report.Summary.GetProperty("outcome").GetString().Should().Be("ContentFailed");
        report.Summary.GetProperty("progress").TryGetProperty("unitsAttempted", out _).Should().BeFalse("zero-valued counters are omitted from reports");
        (await InventoryQuantityAsync(sku)).Should().Be(0);
        (await InventoryOutboxCountAsync(sku)).Should().Be(0);
        var content = (await report.Blob.DownloadContentAsync()).Value.Content.ToString();
        content.Should().Contain("csv.field.invalid").And.NotContain("private-invalid-value");
    }

    [Test]
    public async Task InventoryCsv_PartialFailureRetriesWithoutRepeatingCommittedEffects()
    {
        var firstSku = Runtime.NewGuid().ToString("N");
        var missingSku = Runtime.NewGuid().ToString("N");
        await SeedInventoryAsync(firstSku);
        var blob = await UploadInventoryAsync($"CommandId,Sku,Quantity\none,{firstSku},2\ntwo,{missingSku},3\nthree,{firstSku},4\n");
        var failed = await SubmitInventoryAsync(blob);
        failed.Summary.GetProperty("outcome").GetString().Should().Be("ExecutionFailed");
        failed.Summary.GetProperty("executionErrorType").GetString().Should().Be(nameof(NotFoundException));
        await WaitForInventoryPoisonAsync(blob);
        (await InventoryQuantityAsync(firstSku)).Should().Be(2);
        (await InventoryOutboxCountAsync(firstSku)).Should().Be(1);

        await SeedInventoryAsync(missingSku);
        var priorReports = await InventoryReportNamesAsync();
        var repaired = await SubmitInventoryAsync(blob, priorReports);
        repaired.Summary.GetProperty("outcome").GetString().Should().Be("Succeeded");
        (await InventoryQuantityAsync(firstSku)).Should().Be(6);
        (await InventoryQuantityAsync(missingSku)).Should().Be(3);
        (await InventoryOutboxCountAsync(firstSku)).Should().Be(2);
        (await InventoryOutboxCountAsync(missingSku)).Should().Be(1);
    }

    [Test]
    public async Task InventoryCsv_ReusedCommandWithDifferentContentFailsAtWriteTime()
    {
        var sku = Runtime.NewGuid().ToString("N");
        await SeedInventoryAsync(sku);
        var blob = await UploadInventoryAsync($"CommandId,Sku,Quantity\nsame,{sku},2\nsame,{sku},3\n");
        var failed = await SubmitInventoryAsync(blob);
        failed.Summary.GetProperty("outcome").GetString().Should().Be("ExecutionFailed");
        failed.Summary.GetProperty("executionErrorType").GetString().Should().Be(nameof(BusinessException));
        await WaitForInventoryPoisonAsync(blob);
        (await InventoryQuantityAsync(sku)).Should().Be(2);
        (await InventoryOutboxCountAsync(sku)).Should().Be(1);
    }

    [Test]
    public async Task InventoryCsv_OutboxFailureRollsBackMutationAndReceiptBeforeNativeRetry()
    {
        var sku = Runtime.NewGuid().ToString("N");
        await SeedInventoryAsync(sku);
        await InventoryExecuteAsync("""
            CREATE TRIGGER Inventory.FailOutbox ON Inventory.Outbox AFTER INSERT AS
            BEGIN
              THROW 51000, 'Outbox unavailable for rollback evidence.', 1;
            END
            """);
        BlobClient? blob = null;
        try
        {
            blob = await UploadInventoryAsync($"CommandId,Sku,Quantity\none,{sku},2\n");
            await NotifyInventoryAsync(blob);
            await WaitForInventoryPoisonAsync(blob);
            await AssertUnfinishedInventoryReportsAsync(blob);
            (await InventoryQuantityAsync(sku)).Should().Be(0);
            (await InventoryOutboxCountAsync(sku)).Should().Be(0);
            var identity = new FileProcessingIdentity(InventoryProcessor.DefinitionName, InventoryProcessor.DefinitionVersion,
                blob.Uri.AbsoluteUri, "ignored", (await blob.GetPropertiesAsync()).Value.ETag.ToString());
            (await InventoryScalarAsync("SELECT COUNT(*) FROM Inventory.Receipts WHERE UnitKey = @key",
                new SqlParameter("@key", InventoryImportIdentity.UnitKey(identity, "runtime-tenant", "one")))).Should().Be(0);
        }
        finally
        {
            await InventoryExecuteAsync("DROP TRIGGER Inventory.FailOutbox");
        }

        var recovered = await SubmitInventoryAsync(blob ?? throw new InvalidOperationException("The input was not uploaded."), await InventoryReportNamesAsync());
        recovered.Summary.GetProperty("outcome").GetString().Should().Be("Succeeded");
        (await InventoryQuantityAsync(sku)).Should().Be(2);
    }

    [Test]
    public async Task InventoryCsv_ReportStorageFailureUsesNativeRetryAndPoisonHandling()
    {
        var sku = Runtime.NewGuid().ToString("N");
        await SeedInventoryAsync(sku);
        await _inventoryReports.DeleteAsync();
        try
        {
            var blob = await UploadInventoryAsync($"CommandId,Sku,Quantity\none,{sku},2\n");
            await NotifyInventoryAsync(blob);
            await WaitForInventoryPoisonAsync(blob);
            (await InventoryQuantityAsync(sku)).Should().Be(0);
            (await _inventoryReports.ExistsAsync()).Value.Should().BeFalse();
        }
        finally
        {
            await _inventoryReports.CreateIfNotExistsAsync();
        }
    }

    [Test]
    public async Task InventoryCsv_ReportFailureAfterCommitDoesNotUndoEffectsAndRetryDeduplicates()
    {
        var firstSku = Runtime.NewGuid().ToString("N");
        var secondSku = Runtime.NewGuid().ToString("N");
        await SeedInventoryAsync(firstSku);
        await SeedInventoryAsync(secondSku);
        var blob = await UploadInventoryAsync($"CommandId,Sku,Quantity\none,{firstSku},2\ntwo,{secondSku},3\n");
        try
        {
            await using (var connection = new SqlConnection(InventoryConnection))
            {
                await connection.OpenAsync();
                await using var transaction = connection.BeginTransaction();
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "UPDATE Inventory.Items SET Quantity = Quantity WHERE TenantId = 'runtime-tenant' AND Sku = @sku";
                command.Parameters.AddWithValue("@sku", secondSku);
                await command.ExecuteNonQueryAsync();
                await NotifyInventoryAsync(blob);
                await Test.EventuallyAsync(_ => InventoryQuantityAsync(firstSku), quantity => quantity == 2, timeout: TimeSpan.FromSeconds(20));
                await _inventoryReports.DeleteAsync();
                await transaction.CommitAsync();
            }

            await WaitForInventoryPoisonAsync(blob);
            (await InventoryQuantityAsync(firstSku)).Should().Be(2);
            (await InventoryQuantityAsync(secondSku)).Should().Be(3);
            (await InventoryOutboxCountAsync(firstSku)).Should().Be(1);
            (await InventoryOutboxCountAsync(secondSku)).Should().Be(1);
        }
        finally
        {
            await _inventoryReports.CreateIfNotExistsAsync();
        }

        var retry = await SubmitInventoryAsync(blob);
        retry.Summary.GetProperty("outcome").GetString().Should().Be("Succeeded");
        (await InventoryQuantityAsync(firstSku)).Should().Be(2);
        (await InventoryQuantityAsync(secondSku)).Should().Be(3);
        (await InventoryOutboxCountAsync(firstSku)).Should().Be(1);
        (await InventoryOutboxCountAsync(secondSku)).Should().Be(1);
    }

    private async Task<BlobClient> UploadInventoryAsync(string csv)
    {
        var blob = _inventoryInputs.GetBlobClient($"{Runtime.NewGuid():N}/inventory.csv");
        await blob.UploadAsync(BinaryData.FromString(csv));
        return blob;
    }

    private async Task NotifyInventoryAsync(BlobClient blob, string functionName = "ImportInventoryFile")
    {
        var properties = (await blob.GetPropertiesAsync()).Value;
        var notification = EventGridExtensionMethods.CreateBlobCreatedEvent(blob.BlobContainerName, blob.Name, blob.Uri,
            properties.ETag.ToString(), properties.ContentType, properties.ContentLength);
        using var response = await Test.FunctionsWebhook("functions").PostBlobCreatedAsync("Host.Functions." + functionName, notification);
        response.IsSuccessStatusCode.Should().BeTrue("the webhook returned {0}: {1}", response.StatusCode, await response.Content.ReadAsStringAsync());
    }

    private async Task<(BlobClient Blob, JsonElement Summary)> SubmitInventoryAsync(BlobClient blob, IReadOnlyCollection<string>? previous = null, string functionName = "ImportInventoryFile")
    {
        var revision = (await blob.GetPropertiesAsync()).Value.ETag.ToString();
        await NotifyInventoryAsync(blob, functionName);
        var result = await Test.EventuallyAsync(async token =>
        {
            await foreach (var item in _inventoryReports.GetBlobsAsync(BlobTraits.Metadata, cancellationToken: token))
            {
                if (previous?.Contains(item.Name) is true || !item.Metadata.TryGetValue("outcome", out var outcome) || outcome == "InProgress")
                    continue;

                var reportBlob = _inventoryReports.GetBlobClient(item.Name);
                var content = (await reportBlob.DownloadContentAsync(token)).Value.Content.ToString();
                using var document = JsonDocument.Parse(content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[^1]);
                var identity = document.RootElement.GetProperty("identity");
                if (identity.GetProperty("sourceId").GetString() == blob.Uri.AbsoluteUri && identity.GetProperty("sourceRevision").GetString() == revision)
                    return (Blob: reportBlob, Summary: document.RootElement.Clone());
            }

            return (Blob: (BlobClient?)null, Summary: default(JsonElement));
        }, value => value.Blob is not null, timeout: TimeSpan.FromSeconds(90));
        return (result.Blob ?? throw new TimeoutException("No finalized inventory report was produced."), result.Summary);
    }

    private async Task<List<string>> InventoryReportNamesAsync()
    {
        var names = new List<string>();
        await foreach (var item in _inventoryReports.GetBlobsAsync())
            names.Add(item.Name);
        return names;
    }

    private async Task AssertUnfinishedInventoryReportsAsync(BlobClient input)
    {
        var count = 0;
        await foreach (var item in _inventoryReports.GetBlobsAsync(BlobTraits.Metadata))
        {
            var content = (await _inventoryReports.GetBlobClient(item.Name).DownloadContentAsync()).Value.Content.ToString();
            using var first = JsonDocument.Parse(content.Split('\n', StringSplitOptions.RemoveEmptyEntries)[0]);
            if (first.RootElement.GetProperty("identity").GetProperty("sourceId").GetString() != input.Uri.AbsoluteUri)
                continue;

            count++;
            item.Metadata["outcome"].Should().Be("InProgress");
            content.Should().NotContain("\"kind\":\"summary\"");
        }

        count.Should().BeGreaterThan(0);
    }

    private async Task WaitForInventoryPoisonAsync(BlobClient blob)
    {
        var queue = new QueueClient("UseDevelopmentStorage=true", "webjobs-blobtrigger-poison", new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        var poison = await Test.EventuallyAsync(async token =>
        {
            if (!(await queue.ExistsAsync(token)).Value)
                return false;
            var messages = await queue.PeekMessagesAsync(32, token);
            return messages.Value.Any(message => message.Body.ToString().Contains(blob.Name, StringComparison.Ordinal));
        }, value => value, timeout: TimeSpan.FromSeconds(90));
        poison.Should().BeTrue();
    }

    private static Task SeedInventoryAsync(string sku)
        => InventoryExecuteAsync("INSERT INTO Inventory.Items (TenantId, Sku, Quantity) VALUES ('runtime-tenant', @sku, 0)", new SqlParameter("@sku", sku));

    private static Task<long> InventoryQuantityAsync(string sku)
        => InventoryScalarAsync("SELECT Quantity FROM Inventory.Items WHERE TenantId = 'runtime-tenant' AND Sku = @sku", new SqlParameter("@sku", sku));

    private static Task<long> InventoryOutboxCountAsync(string sku)
        => InventoryScalarAsync("SELECT COUNT(*) FROM Inventory.Outbox WHERE TenantId = 'runtime-tenant' AND Event LIKE @sku", new SqlParameter("@sku", "%" + sku + "%"));

    private static async Task InventoryExecuteAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(InventoryConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<long> InventoryScalarAsync(string sql, params SqlParameter[] parameters)
    {
        await using var connection = new SqlConnection(InventoryConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddRange(parameters);
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }
}
