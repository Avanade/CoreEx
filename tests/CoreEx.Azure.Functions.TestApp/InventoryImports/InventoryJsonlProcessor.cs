namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

public partial class InventoryJsonlProcessor
{
    /// <inheritdoc/>
    protected override void OnConfigureUnitContext(FileUnitContextArgs<InventoryJsonlAdjustment> args)
        => args.UnitContext.TenantId = Services.GetRequiredService<InventoryImportOptions>().TenantId;

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
