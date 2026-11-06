namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Demonstrates a thin native Blob boundary with deployment-selected input routing.</summary>
public sealed class InventoryBlobFunctions(InventoryBlobImport import, CoreEx.ExecutionContext executionContext)
{
    /// <summary>Gets the input-only route in the existing test host's ImportStorage account.</summary>
    public const string InputPath = "inventory-adjustments/{name}";

    /// <summary>Processes the selected current revision, leaving operational failures and cancellation to the Functions host.</summary>
    [Function(nameof(ImportInventoryFile))]
    public Task ImportInventoryFile([BlobTrigger(InputPath, Connection = "ImportStorage", Source = BlobTriggerSource.EventGrid)] BlobClient blob, CancellationToken cancellationToken)
        => import.ProcessAsync(blob, executionContext.Attributes["invocation-id"] as string
            ?? throw new InvalidOperationException("The worker invocation identity is required."), cancellationToken);
}
