namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Uses the shared CoreEx SQL Server unit of work and outbox for each accepted business unit.</summary>
public sealed class InventoryImportService(SqlServerDatabase database, SqlServerUnitOfWork unitOfWork, InventoryImportOptions options, CoreEx.ExecutionContext executionContext) : IInventoryImportService
{
    private readonly string _scopeId = Runtime.NewGuid().ToString("N");

    /// <inheritdoc/>
    public Task<Result> ApplyAsync(InventoryAdjustment adjustment, FileProcessingIdentity identity, CancellationToken cancellationToken = default)
    {
        adjustment.ThrowIfNull();
        identity.ThrowIfNull();
        if (executionContext.TenantId != options.TenantId)
            throw new InvalidOperationException("The trusted unit tenant must be established before resolving the application service.");

        if (string.IsNullOrWhiteSpace(adjustment.CommandId) || string.IsNullOrWhiteSpace(adjustment.Sku) || adjustment.Quantity is not > 0)
            return Task.FromResult(Result.BusinessError("Command, item and positive quantity are required at write time."));

        return unitOfWork.TransactionAsync(async ct =>
        {
            var key = InventoryImportIdentity.UnitKey(identity, options.TenantId, adjustment.CommandId);
            var content = JsonSerializer.Serialize(new { adjustment.Sku, adjustment.Quantity }, JsonDefaults.SerializerOptions);
            var lookup = database.Statement(new SqlStatement(CommandType.Text,
                "SELECT Content FROM Inventory.Receipts WITH (UPDLOCK, HOLDLOCK) WHERE UnitKey = @key"));
            lookup.Parameters.AddParameter("key", key);
            var previous = await lookup.ScalarAsync<string?>(ct).ConfigureAwait(false);
            if (previous is not null)
                return previous == content ? Result.Success : Result.BusinessError("The command identity was reused with different content.");

            var update = database.Statement(new SqlStatement(CommandType.Text,
                "UPDATE Inventory.Items SET Quantity = Quantity + @quantity WHERE TenantId = @tenant AND Sku = @sku"));
            update.Parameters.AddParameter("quantity", adjustment.Quantity.Value);
            update.Parameters.AddParameter("tenant", options.TenantId);
            update.Parameters.AddParameter("sku", adjustment.Sku);
            if (await update.NonQueryAsync(ct).ConfigureAwait(false) != 1)
                return Result.NotFoundError("The inventory item does not exist.");

            var receipt = database.Statement(new SqlStatement(CommandType.Text,
                "INSERT INTO Inventory.Receipts (UnitKey, Content, ScopeId) VALUES (@key, @content, @scope)"));
            receipt.Parameters.AddParameter("key", key);
            receipt.Parameters.AddParameter("content", content);
            receipt.Parameters.AddParameter("scope", _scopeId);
            await receipt.NonQueryAsync(ct).ConfigureAwait(false);
            unitOfWork.Events.Add(new EventData
            {
                DomainName = "Inventory", Entity = "Item", Action = "Adjusted", Key = adjustment.Sku
            }.WithValue(new { adjustment.CommandId, adjustment.Sku, adjustment.Quantity }));
            return Result.Success;
        }, cancellationToken);
    }
}
