namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Implements integration-specific validation and execution in a fresh business-unit scope.</summary>
/// <remarks>Each object's bin quantities become one atomic inventory adjustment.</remarks>
[ScopedService]
public partial class InventoryJsonBatchHandler(IInventoryImportService service) : FileUnitHandler<InventoryJsonBatchAdjustment>
{
    /// <inheritdoc/>
    protected override Task OnValidateAsync(FileUnitValidationArgs<InventoryJsonBatchAdjustment> args, CancellationToken cancellationToken)
        => InventoryBatchTotals.ValidateAsync(args.Unit.Lines.ThrowIfNull().Select(x => x.Quantity), args, cancellationToken);

    /// <inheritdoc/>
    protected override Task<Result> OnExecuteAsync(FileUnitArgs<InventoryJsonBatchAdjustment> args, CancellationToken cancellationToken)
        => service.ApplyAsync(new()
        {
            CommandId = args.Unit.CommandId, Sku = args.Unit.Sku,
            Quantity = InventoryBatchTotals.Sum(args.Unit.Lines.ThrowIfNull().Select(x => x.Quantity))
        }, args.Context.Identity, cancellationToken);
}
