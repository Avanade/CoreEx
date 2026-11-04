namespace CoreEx.Azure.Functions.TestApp.InventoryImports;

/// <summary>Shares the host-neutral execution boundary between local input and native Blob ingestion.</summary>
public sealed class InventoryImportRunner(InventoryProcessor processor, InventoryImportReport report)
{
    /// <summary>Executes one invocation with explicitly selected definition, immutable revision, and unique attempt identity.</summary>
    public async Task<FileProcessingSummary> RunAsync(Func<CancellationToken, Task<Stream>> openReadAsync, FileProcessingIdentity identity, CancellationToken cancellationToken = default)
    {
        openReadAsync.ThrowIfNull();
        identity.ThrowIfNull();
        if (identity.DefinitionName != InventoryImportIdentity.DefinitionName || identity.DefinitionVersion != InventoryImportIdentity.DefinitionVersion || string.IsNullOrWhiteSpace(identity.SourceRevision))
            throw new ArgumentException("The route requires the inventory definition/version and an immutable source revision.", nameof(identity));

        await report.StartAsync(identity, cancellationToken).ConfigureAwait(false);
        var context = InventoryProcessor.CreateContext(identity.SourceId, identity.AttemptId, identity.SourceRevision);
        var summary = await processor.ProcessAsync(openReadAsync, context, cancellationToken).ConfigureAwait(false);
        if (summary.Outcome == FileProcessingOutcome.ExecutionFailed)
            await report.FinishAsync(summary, cancellationToken).ConfigureAwait(false);

        return summary;
    }
}
