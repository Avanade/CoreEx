namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Implements integration-specific validation and execution in a fresh business-unit scope.</summary>
/// <remarks>Each group's bin quantities become one atomic inventory adjustment; bins are descriptive input, not separately persisted stock.</remarks>
[ScopedService]
public partial class InventoryCsvBatchHandler(IInventoryImportService service) : FileUnitHandler<InventoryCsvBatchAdjustmentGroup>
{
    /// <inheritdoc/>
    protected override Task OnValidateAsync(FileUnitValidationArgs<InventoryCsvBatchAdjustmentGroup> args, CancellationToken cancellationToken)
        => InventoryBatchTotals.ValidateAsync(args.Unit.Line.Select(x => x.Quantity), args, cancellationToken);

    /// <inheritdoc/>
    protected override Task<Result> OnExecuteAsync(FileUnitArgs<InventoryCsvBatchAdjustmentGroup> args, CancellationToken cancellationToken)
        => service.ApplyAsync(new()
        {
            CommandId = args.Unit.Record.CommandId, Sku = args.Unit.Record.Sku,
            Quantity = InventoryBatchTotals.Sum(args.Unit.Line.Select(x => x.Quantity))
        }, args.Context.Identity, cancellationToken);
}
