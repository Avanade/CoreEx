namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

public partial class InventoryCsvBatchProcessor
{
    /// <inheritdoc/>
    protected override void OnConfigureUnitContext(FileUnitContextArgs<InventoryCsvBatchAdjustmentGroup> args)
        => args.UnitContext.TenantId = Services.GetRequiredService<InventoryImportOptions>().TenantId;

    /// <inheritdoc/>
    protected override Task OnValidatedUnitAsync(FileUnitArgs<InventoryCsvBatchAdjustmentGroup> args, CancellationToken cancellationToken)
    {
        ((InventoryCsvBatchContext)args.Context).Totals.Add(InventoryBatchTotals.Sum(args.Unit.Line.Select(x => x.Quantity)));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
    {
        var context = (InventoryCsvBatchContext)args.Context;
        var trailer = context.GetTrailerMetadata();
        return context.Totals.ReconcileAsync(args, trailer?.AdjustmentCount, trailer?.QuantityTotal, cancellationToken);
    }

    /// <inheritdoc/>
    protected override Task OnDiagnosticAsync(FileDiagnosticArgs args, CancellationToken cancellationToken)
        => Services.GetRequiredService<InventoryImportReport>().WriteDiagnosticAsync(args, cancellationToken);

    /// <inheritdoc/>
    protected override Task OnContentFailedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        => Services.GetRequiredService<InventoryImportReport>().FinishAsync(args.Summary, cancellationToken);

    /// <inheritdoc/>
    protected override Task OnCompletedAsync(FileSummaryArgs args, CancellationToken cancellationToken)
        => Services.GetRequiredService<InventoryImportReport>().FinishAsync(args.Summary, cancellationToken);
}
