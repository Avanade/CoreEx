namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Coordinates the explicitly routed generated definitions and their durable reports.</summary>
public sealed class InventoryImportRunner(IServiceProvider services, InventoryImportReport report)
{
    /// <summary>Executes one invocation with explicitly selected definition, immutable revision, and unique attempt identity.</summary>
    public async Task<FileProcessingSummary> RunAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingIdentity identity, CancellationToken cancellationToken = default)
    {
        openReadAsync.ThrowIfNull();
        identity.ThrowIfNull();
        if (string.IsNullOrWhiteSpace(identity.SourceRevision))
            throw new ArgumentException("The route requires an immutable source revision.", nameof(identity));

        (FileProcessor Processor, FileProcessingContext Context) selected = identity.DefinitionName switch
        {
            InventoryProcessor.DefinitionName => (services.GetRequiredService<InventoryProcessor>(), InventoryProcessor.CreateContext(identity.SourceId, identity.AttemptId, identity.SourceRevision)),
            InventoryJsonProcessor.DefinitionName => (services.GetRequiredService<InventoryJsonProcessor>(), InventoryJsonProcessor.CreateContext(identity.SourceId, identity.AttemptId, identity.SourceRevision)),
            InventoryJsonlProcessor.DefinitionName => (services.GetRequiredService<InventoryJsonlProcessor>(), InventoryJsonlProcessor.CreateContext(identity.SourceId, identity.AttemptId, identity.SourceRevision)),
            InventoryCsvBatchProcessor.DefinitionName => (services.GetRequiredService<InventoryCsvBatchProcessor>(), InventoryCsvBatchProcessor.CreateContext(identity.SourceId, identity.AttemptId, identity.SourceRevision)),
            InventoryJsonBatchProcessor.DefinitionName => (services.GetRequiredService<InventoryJsonBatchProcessor>(), InventoryJsonBatchProcessor.CreateContext(identity.SourceId, identity.AttemptId, identity.SourceRevision)),
            InventoryJsonlBatchProcessor.DefinitionName => (services.GetRequiredService<InventoryJsonlBatchProcessor>(), InventoryJsonlBatchProcessor.CreateContext(identity.SourceId, identity.AttemptId, identity.SourceRevision)),
            _ => throw new ArgumentException("The route requires a registered inventory definition.", nameof(identity))
        };
        if (selected.Context.Identity.DefinitionVersion != identity.DefinitionVersion)
            throw new ArgumentException("The definition version does not match the selected route.", nameof(identity));

        await report.StartAsync(identity, cancellationToken).ConfigureAwait(false);
        var summary = await selected.Processor.ProcessAsync(openReadAsync, selected.Context, cancellationToken).ConfigureAwait(false);
        if (summary.Outcome == FileProcessingOutcome.ExecutionFailed)
            await report.FinishAsync(summary, cancellationToken).ConfigureAwait(false);

        return summary;
    }
}
