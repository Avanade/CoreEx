namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Demonstrates a thin native Blob boundary with deployment-selected input routing.</summary>
public sealed class InventoryBlobFunctions(InventoryBlobImport import, CoreEx.ExecutionContext executionContext)
{
    /// <summary>Gets the input-only route in the existing test host's ImportStorage account.</summary>
    public const string InputPath = "inventory-adjustments/{name}";

    /// <summary>Processes the selected current revision, leaving operational failures and cancellation to the Functions host.</summary>
    [Function(nameof(ImportInventoryFile))]
    public Task ImportInventoryFile([BlobTrigger(InputPath, Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
        => ProcessAsync(blob, InventoryProcessor.DefinitionName, cancellationToken);

    /// <summary>Processes independent adjustments in a JSON array.</summary>
    [Function(nameof(ImportInventoryJson))]
    public Task ImportInventoryJson([BlobTrigger("inventory-json/{name}", Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
        => ProcessAsync(blob, InventoryJsonProcessor.DefinitionName, cancellationToken);

    /// <summary>Processes one independent adjustment per JSONL line.</summary>
    [Function(nameof(ImportInventoryJsonl))]
    public Task ImportInventoryJsonl([BlobTrigger("inventory-jsonl/{name}", Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
        => ProcessAsync(blob, InventoryJsonlProcessor.DefinitionName, cancellationToken);

    /// <summary>Processes CSV adjustment groups with child rows and header/trailer metadata.</summary>
    [Function(nameof(ImportInventoryCsvBatch))]
    public Task ImportInventoryCsvBatch([BlobTrigger("inventory-csv-batch/{name}", Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
        => ProcessAsync(blob, InventoryCsvBatchProcessor.DefinitionName, cancellationToken);

    /// <summary>Processes a JSON envelope with nested adjustment lines and header/trailer metadata.</summary>
    [Function(nameof(ImportInventoryJsonBatch))]
    public Task ImportInventoryJsonBatch([BlobTrigger("inventory-json-batch/{name}", Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
        => ProcessAsync(blob, InventoryJsonBatchProcessor.DefinitionName, cancellationToken);

    /// <summary>Processes routed JSONL metadata and adjustment objects containing child items.</summary>
    [Function(nameof(ImportInventoryJsonlBatch))]
    public Task ImportInventoryJsonlBatch([BlobTrigger("inventory-jsonl-batch/{name}", Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
        => ProcessAsync(blob, InventoryJsonlBatchProcessor.DefinitionName, cancellationToken);

    /// <summary>Preserves the worker attempt identity and cancellation at every native route.</summary>
    private Task ProcessAsync(BlobClient blob, string definitionName, CancellationToken cancellationToken)
        => import.ProcessDefinitionAsync(blob, executionContext.Attributes["invocation-id"] as string
            ?? throw new InvalidOperationException("The worker invocation identity is required."), definitionName, cancellationToken);
}
