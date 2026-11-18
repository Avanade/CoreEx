namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

public partial class InventoryJsonlBatchProcessor
{
    /// <inheritdoc/>
    protected override void OnConfigureUnitContext(FileUnitContextArgs<InventoryJsonlBatchAdjustment> args)
        => args.UnitContext.TenantId = Services.GetRequiredService<InventoryImportOptions>().TenantId;

    /// <inheritdoc/>
    protected override Task OnValidatedUnitAsync(FileUnitArgs<InventoryJsonlBatchAdjustment> args, CancellationToken cancellationToken)
    {
        ((InventoryJsonlBatchContext)args.Context).Totals.Add(InventoryBatchTotals.Sum(args.Unit.Lines.ThrowIfNull().Select(x => x.Quantity)));
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override Task OnValidateFileAsync(FileValidationArgs args, CancellationToken cancellationToken)
    {
        var context = (InventoryJsonlBatchContext)args.Context;
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
