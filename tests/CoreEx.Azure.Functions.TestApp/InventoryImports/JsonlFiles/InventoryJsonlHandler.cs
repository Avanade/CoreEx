namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Implements integration-specific validation and execution in a fresh business-unit scope.</summary>
/// <remarks>This handwritten handler is preserved by regeneration.</remarks>
[ScopedService]
public partial class InventoryJsonlHandler(IInventoryImportService service) : FileUnitHandler<InventoryJsonlAdjustment>
{
    /// <inheritdoc/>
    protected override Task<Result> OnExecuteAsync(FileUnitArgs<InventoryJsonlAdjustment> args, CancellationToken cancellationToken)
        => service.ApplyAsync(new() { CommandId = args.Unit.CommandId, Sku = args.Unit.Sku, Quantity = args.Unit.Quantity }, args.Context.Identity, cancellationToken);
}
