namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Implements integration-specific validation and execution in a fresh business-unit scope.</summary>
/// <remarks>This starter is created only when absent and is never overwritten. Override OnExecuteAsync to call application services and return their Result. The inherited execution fails explicitly until implemented.</remarks>
[ScopedService]
public partial class InventoryHandler : FileUnitHandler<InventoryAdjustment>
{
    private readonly IInventoryImportService _service;

    /// <summary>Initializes the scoped application boundary without transport dependencies.</summary>
    public InventoryHandler(IInventoryImportService service) => _service = service;

    /// <inheritdoc/>
    protected override Task<Result> OnExecuteAsync(InventoryAdjustment unit, FileProcessingContext context, CancellationToken cancellationToken)
        => _service.ApplyAsync(unit, context.Identity, cancellationToken);
}
