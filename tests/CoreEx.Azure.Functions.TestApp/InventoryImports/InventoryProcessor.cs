namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

public partial class InventoryProcessor
{
    /// <inheritdoc/>
    protected override void OnConfigureUnitContext(FileProcessingContext fileContext, InventoryAdjustment unit, CoreEx.ExecutionContext? parent, CoreEx.ExecutionContext unitContext)
        => unitContext.TenantId = Services.GetRequiredService<InventoryImportOptions>().TenantId;

    /// <inheritdoc/>
    protected override Task OnDiagnosticAsync(FileProcessingContext context, FileDiagnostic diagnostic, CancellationToken cancellationToken)
        => Services.GetRequiredService<InventoryImportReport>().WriteAsync(new
        {
            kind = "diagnostic", context.Identity, diagnostic.Code, diagnostic.Category,
            message = diagnostic.Message.ToString(), diagnostic.Location
        }, cancellationToken);

    /// <inheritdoc/>
    protected override Task OnRejectedAsync(FileProcessingContext context, FileProcessingSummary summary, CancellationToken cancellationToken)
        => Services.GetRequiredService<InventoryImportReport>().FinishAsync(summary, cancellationToken);

    /// <inheritdoc/>
    protected override Task OnCompletedAsync(FileProcessingContext context, FileProcessingSummary summary, CancellationToken cancellationToken)
        => Services.GetRequiredService<InventoryImportReport>().FinishAsync(summary, cancellationToken);
}
