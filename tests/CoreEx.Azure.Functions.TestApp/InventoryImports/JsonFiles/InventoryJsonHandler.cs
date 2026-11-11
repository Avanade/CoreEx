namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Implements integration-specific validation and execution in a fresh business-unit scope.</summary>
/// <remarks>This handwritten handler is preserved by regeneration.</remarks>
[ScopedService]
public partial class InventoryJsonHandler(IInventoryImportService service) : FileUnitHandler<InventoryJsonAdjustment>
{
    /// <inheritdoc/>
    protected override Task<Result> OnExecuteAsync(FileUnitArgs<InventoryJsonAdjustment> args, CancellationToken cancellationToken)
        => service.ApplyAsync(new() { CommandId = args.Unit.CommandId, Sku = args.Unit.Sku, Quantity = args.Unit.Quantity }, args.Context.Identity, cancellationToken);
}
