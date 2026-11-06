namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Owns inventory write-time invariants and durable transactional deduplication.</summary>
public interface IInventoryImportService
{
    /// <summary>Atomically applies a command, records its durable receipt, and enqueues an outbox event.</summary>
    Task<Result> ApplyAsync(InventoryAdjustment adjustment, FileProcessingIdentity identity, CancellationToken cancellationToken = default);
}
